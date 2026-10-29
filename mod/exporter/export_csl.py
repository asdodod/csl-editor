"""Build CSL UnityFS bundles from Workshop projects; never modifies the source."""
import argparse,base64,copy,hashlib,io,json,math,struct,sys,uuid
from pathlib import Path

def main():
    args=argparse.ArgumentParser();args.add_argument('--config',required=True);args.add_argument('--input',required=True);opts=args.parse_args()
    config=json.loads(Path(opts.config).read_text(encoding='utf-8-sig'))
    if config.get('UnityPyPath'):sys.path.insert(0,config['UnityPyPath'])
    import UnityPy
    from PIL import Image
    snapshot=json.loads(Path(opts.input).read_text(encoding='utf-8-sig'));project=snapshot['Project']
    source=Path(config['CustomSabers'])/project['SourceFile'] if project.get('SourceFile') else Path(config['BaseBundle'])
    if project.get('SourceHash') and hashlib.sha256(source.read_bytes()).hexdigest()!=project['SourceHash']:raise ValueError('Source saber changed')
    env=UnityPy.load(str(source));objects={o.path_id:o for o in env.objects}
    needed={'GameObject','Transform','MeshFilter','MeshRenderer','ParticleSystemRenderer','Material','Mesh','MonoBehaviour','AssetBundle','Shader','Texture2D','Animator','Animation'}
    trees={k:o.read_typetree() for k,o in objects.items() if o.type.name in needed}
    ptr=lambda n:{'m_FileID':0,'m_PathID':n};pid=lambda p:p.get('m_PathID',0)
    sf=next(o.assets_file for o in objects.values() if o.type.name=='GameObject')
    counter=max([1000000]+[k for k in objects if 0<k<2**31])+1
    def create(kind,tree):
        nonlocal counter
        original=next(o for o in objects.values() if o.type.name==kind and o.assets_file==sf)
        obj=object.__new__(type(original));obj.__dict__.update(original.__dict__);obj.path_id=counter;counter+=1;sf.objects[obj.path_id]=obj;objects[obj.path_id]=obj;trees[obj.path_id]=tree;return obj.path_id
    gos={k:t for k,t in trees.items() if objects[k].type.name=='GameObject'}
    transforms={k:t for k,t in trees.items() if objects[k].type.name=='Transform'}
    by_go={pid(t['m_GameObject']):k for k,t in transforms.items()}
    root=next(k for k,t in gos.items() if t['m_Name']=='_CustomSaber')
    hands={name:next(k for k,t in gos.items() if t['m_Name']==name) for name in ['LeftSaber','RightSaber']}
    paths={}
    def walk(tr,path):
        paths[path]=tr
        for i,p in enumerate(transforms[tr]['m_Children']):
            child=pid(p);go=pid(transforms[child]['m_GameObject']);walk(child,path+f"{i}:{gos[go]['m_Name']}/")
    for side,go in hands.items():walk(by_go[go],side+'/')
    for go in hands.values():transforms[by_go[go]]['m_LocalScale']={'x':project.get('SaberWidth',1),'y':project.get('SaberWidth',1),'z':project.get('SaberLength',1)}
    def find(path):return paths.get(path.rstrip('/')+'/')
    def quaternion(v):
        x,y,z=[math.radians(n)/2 for n in v]
        def mul(a,b):return [a[3]*b[0]+a[0]*b[3]+a[1]*b[2]-a[2]*b[1],a[3]*b[1]-a[0]*b[2]+a[1]*b[3]+a[2]*b[0],a[3]*b[2]+a[0]*b[1]-a[1]*b[0]+a[2]*b[3],a[3]*b[3]-a[0]*b[0]-a[1]*b[1]-a[2]*b[2]]
        return dict(zip('xyzw',mul(mul([0,math.sin(y),0,math.cos(y)],[math.sin(x),0,0,math.cos(x)]),[0,0,math.sin(z),math.cos(z)])))
    shaders={t.get('m_ParsedForm',{}).get('m_Name',t.get('m_Name','')):k for k,t in trees.items() if objects[k].type.name=='Shader'}
    material_template=copy.deepcopy(next(t for k,t in trees.items() if objects[k].type.name=='Material'))
    texture_template=copy.deepcopy(next(t for k,t in trees.items() if objects[k].type.name=='Texture2D'))
    def material(edit,original=None):
        t=copy.deepcopy(original or material_template)
        name=edit.get('Shader','')
        if name and name not in shaders:
            if name.startswith('Custom/Saber') and not project.get('SourceFile'):name='BeatSaber/Unlit Glow'
            else:raise ValueError('Shader is not available in the source bundle: '+name)
        if name:t['m_Shader']=ptr(shaders[name])
        if edit.get('Name'):t['m_Name']=edit['Name']
        if edit.get('Reset'):
            t['m_SavedProperties']={'m_TexEnvs':[],'m_Ints':[],'m_Floats':[],'m_Colors':[]}
            t['m_CustomRenderQueue']=edit.get('RenderQueue',-1)
            if 'm_ShaderKeywords' in t:t['m_ShaderKeywords']=' '.join(edit.get('Keywords',[]))
            if 'm_ValidKeywords' in t:t['m_ValidKeywords']=edit.get('Keywords',[])
            if 'm_InvalidKeywords' in t:t['m_InvalidKeywords']=[]
        props=t['m_SavedProperties']
        for key,updates in [('m_Floats',edit.get('Numbers',{})),('m_Colors',{**{k:dict(zip('rgba',v)) for k,v in edit.get('Colors',{}).items()},**{k:dict(zip('rgba',v)) for k,v in edit.get('Vectors',{}).items()}})]:
            values=dict(props.get(key,[]));values.update(updates);props[key]=list(values.items())
        texs=dict(props.get('m_TexEnvs',[]))
        for key,image in edit.get('Textures',{}).items():
            tex=copy.deepcopy(texs.get(key,{'m_Texture':ptr(0)}));tex['m_Scale']=dict(zip('xy',image['Scale']));tex['m_Offset']=dict(zip('xy',image['Offset']))
            if image.get('Png'):
                bitmap=Image.open(io.BytesIO(base64.b64decode(image['Png']))).convert('RGBA');w,h=bitmap.size
                if w>4096 or h>4096:raise ValueError('Texture exceeds 4096 x 4096')
                data=bitmap.transpose(Image.Transpose.FLIP_TOP_BOTTOM).tobytes();tt=copy.deepcopy(texture_template)
                tt.update(m_Name='Workshop_'+key,m_Width=w,m_Height=h,m_TextureFormat=4,m_CompleteImageSize=len(data),m_MipCount=1)
                tt['image data']=data
                if 'm_StreamData' in tt:tt['m_StreamData']={'offset':0,'size':0,'path':''}
                tex['m_Texture']=ptr(create('Texture2D',tt))
            texs[key]=tex
        props['m_TexEnvs']=list(texs.items());return create('Material',t)
    # Apply edits using the original child order, before appending new objects.
    for path,edit in project.get('Transforms',{}).items():
        tr=find(path)
        if tr is None:raise ValueError('Part not found: '+path)
        t=transforms[tr];t['m_LocalPosition']=dict(zip('xyz',edit['Position']));t['m_LocalRotation']=quaternion(edit['Rotation']);t['m_LocalScale']=dict(zip('xyz',edit['Scale']));gos[pid(t['m_GameObject'])]['m_IsActive']=edit['Visible']
    for key,edit in project.get('Surfaces',{}).items():
        path,slot=key.rsplit('#',1);tr=find(path)
        if tr is None:raise ValueError('Material part not found: '+path)
        go=gos[pid(transforms[tr]['m_GameObject'])];renderer=next(trees[pid(c['component'])] for c in go['m_Component'] if objects[pid(c['component'])].type.name in {'MeshRenderer','ParticleSystemRenderer'})
        slot=int(slot)
        if slot>=len(renderer['m_Materials']):raise ValueError('Material slot not found: '+key)
        old=trees[pid(renderer['m_Materials'][slot])];renderer['m_Materials'][slot]=ptr(material(edit,old))
    if not project.get('SourceFile'):
        for k,t in gos.items():t['m_IsActive']=k in [root,*hands.values()]
        for k,t in trees.items():
            if objects[k].type.name in {'Animator','Animation'}:t['m_Enabled']=0
    templates={kind:copy.deepcopy(next(t for k,t in trees.items() if objects[k].type.name==kind)) for kind in ['GameObject','Transform','MeshFilter','MeshRenderer','Mesh']}
    for part in snapshot.get('Parts',[]):
        p=part['Part'];geometry=part['Mesh'];positions=geometry['Positions'];normals=geometry['Normals'];uv=geometry['UV'];indices=geometry['Indices'];count=len(positions)//3
        if count>500000 or len(indices)>3000000:raise ValueError('Part geometry is too large')
        for side,hand in hands.items():
            if p.get('Side','both') not in ['both','left' if side=='LeftSaber' else 'right']:continue
            mesh=copy.deepcopy(templates['Mesh']);mesh['m_Name']=p['Name'];data=bytearray()
            for i in range(count):data+=struct.pack('<12f',*positions[i*3:i*3+3],*normals[i*3:i*3+3],1,1,1,1,*uv[i*2:i*2+2])
            channels=[{'stream':0,'offset':0,'format':0,'dimension':0} for _ in range(14)]
            for ch,offset,dim in [(0,0,3),(1,12,3),(3,24,4),(4,40,2)]:channels[ch].update(offset=offset,dimension=dim)
            mesh['m_VertexData'].update(m_VertexCount=count,m_Channels=channels,m_DataSize=bytes(data));mesh.update(m_IndexFormat=1,m_IndexBuffer=list(struct.pack('<'+str(len(indices))+'I',*indices)),m_IsReadable=True)
            if 'm_StreamData' in mesh:mesh['m_StreamData']={'offset':0,'size':0,'path':''}
            for item in mesh.get('m_CompressedMesh',{}).values():
                if isinstance(item,dict):
                    for key,value in item.items():item[key]=[] if isinstance(value,list) else b'' if isinstance(value,bytes) else 0
            mins=[min(positions[a::3],default=0) for a in range(3)];maxs=[max(positions[a::3],default=0) for a in range(3)]
            bounds={'m_Center':dict(zip('xyz',[(a+b)/2 for a,b in zip(mins,maxs)])),'m_Extent':dict(zip('xyz',[(b-a)/2 for a,b in zip(mins,maxs)]))}
            mesh['m_LocalAABB']=bounds;mesh['m_SubMeshes']=[{**mesh['m_SubMeshes'][0],'firstByte':0,'indexCount':len(indices),'baseVertex':0,'firstVertex':0,'vertexCount':count,'localAABB':bounds}]
            meshid=create('Mesh',mesh);matid=material(p['Surface'])
            if p['Surface'].get('UsePlayerColor'):
                floats=dict(trees[matid]['m_SavedProperties']['m_Floats']);floats['_CustomColors']=1;trees[matid]['m_SavedProperties']['m_Floats']=list(floats.items())
            go=copy.deepcopy(templates['GameObject']);go.update(m_Name=p['Name'],m_IsActive=p.get('Visible',True),m_Component=[]);goid=create('GameObject',go)
            tr=copy.deepcopy(templates['Transform']);tr.update(m_GameObject=ptr(goid),m_Father=ptr(by_go[hand]),m_Children=[],m_LocalPosition=dict(zip('xyz',p['Position'])),m_LocalRotation=quaternion(p['Rotation']),m_LocalScale=dict(zip('xyz',[1.,1.,1.])))
            trid=create('Transform',tr);mf=copy.deepcopy(templates['MeshFilter']);mf.update(m_GameObject=ptr(goid),m_Mesh=ptr(meshid));mfid=create('MeshFilter',mf)
            mr=copy.deepcopy(templates['MeshRenderer']);mr.update(m_GameObject=ptr(goid),m_Materials=[ptr(matid)],m_Enabled=1);mrid=create('MeshRenderer',mr)
            go['m_Component']=[{'component':ptr(n)} for n in [trid,mfid,mrid]];transforms[by_go[hand]]['m_Children'].append(ptr(trid))
    for k,t in trees.items():
        if objects[k].type.name=='MonoBehaviour' and 'SaberName' in t:t.update(SaberName=project['Name'],AuthorName=project.get('Author','Creator'),Description='Created in Saber Workshop')
    # Separate CAB identity lets CSL load multiple variants of the same source.
    names={}
    for name in env.file.files:
        import re
        match=re.search(r'CAB-[a-fA-F0-9]{32}',name)
        if match:names.setdefault(match[0],'CAB-'+uuid.uuid4().hex)
    def renamed(value):
        for old,new in names.items():value=value.replace(old,new)
        return value
    for k,t in trees.items():
        if objects[k].type.name=='Shader':continue
        if t.get('m_StreamData',{}).get('path'):t['m_StreamData']['path']=renamed(t['m_StreamData']['path'])
        if objects[k].type.name=='AssetBundle':t['m_Name']='workshop-'+uuid.uuid4().hex
        objects[k].save_typetree(t)
    env.file.files={renamed(name):f for name,f in env.file.files.items()}
    folder=Path(config['CustomSabers'])/'Workshop';folder.mkdir(parents=True,exist_ok=True)
    name=''.join(c for c in project['Name'] if c not in '<>:"/\\|?*' and ord(c)>=32).strip(' .')[:80] or 'Workshop Saber'
    output=folder/(name+'.saber');index=2
    while output.exists():output=folder/(name+f' ({index}).saber');index+=1
    payload=env.file.save(packer='none')
    # Independent parse catches broken object tables before reporting success.
    check=UnityPy.load(payload);assert any(o.type.name=='GameObject' and o.peek_name()=='_CustomSaber' for o in check.objects)
    output.write_bytes(payload)
    print(json.dumps({'ok':True,'file':str(output),'bytes':len(payload)}))

if __name__=='__main__':
    try:main()
    except Exception as error:
        import traceback
        traceback.print_exc(file=sys.stderr)
        print(json.dumps({'ok':False,'error':str(error)}));sys.exit(1)
