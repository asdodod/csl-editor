using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Security.Cryptography;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Rendering;
using AssetBundleLoadingTools.Utilities;
using Object=UnityEngine.Object;

namespace SaberWorkshop;

public sealed class WorkshopState
{
    public static WorkshopState Instance {get;}=new WorkshopState();
    public static string GameRoot=>AppDomain.CurrentDomain.BaseDirectory;
    public static string DataRoot=>Path.Combine(GameRoot,"UserData","SaberWorkshop");
    public static string ProjectFolder=>Path.Combine(DataRoot,"Projects");
    public static string TextureFolder=>Path.Combine(DataRoot,"Textures");
    public WorkshopProject Project=new WorkshopProject();
    public bool PlayEnabled;
    public bool Ready=>template!=null||Project.Parts.Count>0;
    private GameObject? template;
    private readonly Dictionary<string,GameObject> sourceCache=new Dictionary<string,GameObject>();
    private readonly Dictionary<string,Texture2D> textureCache=new Dictionary<string,Texture2D>();
    private readonly Dictionary<string,Shader> shaderCache=new Dictionary<string,Shader>();
    private readonly Dictionary<string,string> shaderFiles=new Dictionary<string,string>();

    public static void EnsureFolders(){Directory.CreateDirectory(ProjectFolder);Directory.CreateDirectory(TextureFolder);}
    public static IEnumerable<string> SaberFiles(){var root=Path.Combine(GameRoot,"CustomSabers");return Directory.Exists(root)?Directory.EnumerateFiles(root,"*.saber",SearchOption.AllDirectories).OrderBy(Path.GetFileName):Enumerable.Empty<string>();}
    public static string SafePath(string root,string relative){var full=Path.GetFullPath(Path.Combine(root,relative));var prefix=Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar)+Path.DirectorySeparatorChar;if(!full.StartsWith(prefix,StringComparison.OrdinalIgnoreCase))throw new InvalidDataException("File is outside the Workshop folder");return full;}
    public void New(){Project=new WorkshopProject();template=null;PlayEnabled=false;}
    public void CreateStarter(){
        New();Project.Name="Flux";
        var handle=AddPart("Handle",false);handle.Length=.23f;handle.Position[2]=-.18f;handle.Sections=new List<Section>{new Section{T=0,Radius=.012f},new Section{T=.08f,Radius=.018f},new Section{T=.9f,Radius=.017f},new Section{T=1,Radius=.012f}};
        for(int i=0;i<3;i++){var p=AddPart("Segment "+(i+1),true);p.Position[2]=.08f+i*.34f;p.Length=.325f;p.Sections=new List<Section>{new Section{T=0,Radius=.001f},new Section{T=.02f,Radius=.007f},new Section{T=.94f,Radius=.006f},new Section{T=1,Radius=.0002f}};}
        var ring=AddPart("Light ring",true);ring.Shape="ring";ring.Position[2]=.045f;ring.Sections[0].Radius=.043f;ring.Sections[1].Radius=.002f;
    }
    public ProfilePart AddPart(string name,bool glow){
        var p=new ProfilePart{Name=name};
        var material=DefaultMaterial(glow);p.Surface=CaptureMaterial(material);p.Surface.UsePlayerColor=glow;OwnedAssets.Release(material);Project.Parts.Add(p);return p;
    }
    public async Task OpenSaber(string file){
        var root=Path.Combine(GameRoot,"CustomSabers");file=SafePath(root,file);
        if(!File.Exists(file)||new FileInfo(file).Length>256L*1024*1024)throw new InvalidDataException("File missing or larger than 256 MB");
        GameObject loaded;
        if(!sourceCache.TryGetValue(file,out loaded!)){
            AssetBundle? bundle=await AssetBundleExtensions.LoadFromFileAsync(file);
            if(!bundle)throw new InvalidDataException("Unable to load bundle. Wait for other loaders and retry.");
            try{
                var asset=await AssetBundleExtensions.LoadAssetAsync<GameObject>(bundle!,"_CustomSaber");
                if(!asset)throw new InvalidDataException("This bundle has no _CustomSaber prefab");
                loaded=Object.Instantiate(asset!);loaded.name=Path.GetFileNameWithoutExtension(file);loaded.SetActive(false);if(Application.isPlaying)Object.DontDestroyOnLoad(loaded);
                var owner=loaded.AddComponent<OwnedAssets>();CloneMaterials(loaded,owner);
                await ShaderRepair.FixShadersOnGameObjectAsync(loaded);
                foreach(var m in loaded.GetComponentsInChildren<Renderer>(true).SelectMany(r=>r.sharedMaterials).Where(m=>m&&m.shader)){shaderCache[m.shader.name]=m.shader;shaderFiles[m.shader.name]=file;}
                sourceCache.Add(file,loaded);
            }finally{bundle!.Unload(false);}
        }
        string hash;using(var sha=SHA256.Create())using(var stream=File.OpenRead(file))hash=BitConverter.ToString(sha.ComputeHash(stream)).Replace("-","").ToLowerInvariant();
        Project=new WorkshopProject{Name=Path.GetFileNameWithoutExtension(file),SourceFile=file.Substring(Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar).Length+1),SourceHash=hash};template=loaded;PlayEnabled=false;
    }
    public static void CloneMaterials(GameObject root,OwnedAssets owner){
        foreach(var renderer in root.GetComponentsInChildren<Renderer>(true)){
            var mats=renderer.sharedMaterials;
            for(int i=0;i<mats.Length;i++){if(!mats[i])continue;var copy=new Material(mats[i]);copy.name=mats[i].name;owner.Assets.Add(copy);mats[i]=copy;}
            renderer.sharedMaterials=mats;
        }
    }
    public GameObject InstantiatePair(Transform? parent=null){
        GameObject root=template?Object.Instantiate(template!,parent):new GameObject(Project.Name);
        if(!template&&parent)root.transform.SetParent(parent,false);
        root.name="Saber Workshop Preview";root.SetActive(false);var owner=root.AddComponent<OwnedAssets>();CloneMaterials(root,owner);
        foreach(bool left in new[]{true,false}){
            string side=left?"LeftSaber":"RightSaber";var hand=root.transform.Find(side);
            if(!hand){hand=new GameObject(side).transform;hand.SetParent(root.transform,false);}
            ApplyEdits(hand,side+"/");
            foreach(var p in Project.Parts.Where(p=>p.Side=="both"||p.Side==(left?"left":"right")))CreatePart(hand,p,owner);
            hand.localPosition=new Vector3(left?-.18f:.18f,0,0);hand.localRotation=Quaternion.Euler(-90,0,0);hand.localScale=new Vector3(Project.SaberWidth,Project.SaberWidth,Project.SaberLength);
        }
        foreach(var t in root.GetComponentsInChildren<Transform>(true))t.gameObject.layer=12;
        root.SetActive(true);return root;
    }
    public GameObject InstantiateHand(bool left,Transform parent){
        var pair=InstantiatePair();var name=left?"LeftSaber":"RightSaber";var hand=pair.transform.Find(name);hand.SetParent(parent,false);hand.localPosition=Vector3.zero;hand.localRotation=Quaternion.identity;hand.localScale=new Vector3(Project.SaberWidth,Project.SaberWidth,Project.SaberLength);
        // Keep the pair's ownership object alive for materials and meshes.
        pair.transform.SetParent(hand,false);pair.transform.localPosition=Vector3.zero;pair.transform.localRotation=Quaternion.identity;pair.transform.localScale=Vector3.one;
        foreach(Transform child in pair.transform)child.gameObject.SetActive(false);return hand.gameObject;
    }
    private void ApplyEdits(Transform hand,string prefix){
        foreach(var renderer in hand.GetComponentsInChildren<Renderer>(true)){
            string path=PathOf(renderer.transform,hand);var mats=renderer.sharedMaterials;
            for(int i=0;i<mats.Length;i++)if(mats[i]&&Project.Surfaces.TryGetValue(prefix+path+"#"+i,out var surface))ApplyMaterial(mats[i],surface);
            if(Project.Transforms.TryGetValue(prefix+path,out var edit)){renderer.transform.localPosition=V3(edit.Position);renderer.transform.localEulerAngles=V3(edit.Rotation);renderer.transform.localScale=V3(edit.Scale);renderer.gameObject.SetActive(edit.Visible);}
        }
    }
    public GameObject CreatePart(Transform parent,ProfilePart p,OwnedAssets owner){
        var go=new GameObject(p.Name);go.transform.SetParent(parent,false);go.transform.localPosition=V3(p.Position);go.transform.localEulerAngles=V3(p.Rotation);
        var mesh=BuildMesh(p);go.AddComponent<MeshFilter>().sharedMesh=mesh;owner.Assets.Add(mesh);
        var m=DefaultMaterial(true);ApplyMaterial(m,p.Surface);owner.Assets.Add(m);go.AddComponent<MeshRenderer>().sharedMaterial=m;
        var identity=go.AddComponent<PartIdentity>();identity.Id=p.Id;identity.UsePlayerColor=p.Surface.UsePlayerColor;go.SetActive(p.Visible);return go;
    }
    public static string PathOf(Transform node,Transform root){var parts=new List<string>();while(node!=root&&node){parts.Add(node.GetSiblingIndex()+":"+node.name);node=node.parent;}parts.Reverse();return string.Join("/",parts);}
    public static Vector3 V3(float[] a)=>new Vector3(a[0],a[1],a[2]);
    public static float[] A3(Vector3 a)=>new[]{a.x,a.y,a.z};
    public IEnumerable<Shader> Shaders()=>Resources.FindObjectsOfTypeAll<Shader>().Where(s=>s&&s.isSupported&&!s.name.StartsWith("Hidden/")).Concat(shaderCache.Values).GroupBy(s=>s.name).Select(g=>g.First()).OrderBy(s=>s.name);
    private Shader? FindShader(string name)=>shaderCache.TryGetValue(name,out var s)?s:Shaders().FirstOrDefault(x=>x.name==name)??Shader.Find(name);
    public Material NewMaterial(string shaderName){var shader=FindShader(shaderName);if(!shader||!shader!.isSupported)throw new InvalidOperationException("Select a supported shader.");return new Material(shader){name="New material"};}
    public static string TexturePng(Texture texture){
        int width=Mathf.Min(2048,texture.width),height=Mathf.Min(2048,texture.height);var previous=RenderTexture.active;var target=RenderTexture.GetTemporary(width,height,0,RenderTextureFormat.ARGB32);Texture2D? image=null;
        try{Graphics.Blit(texture,target);RenderTexture.active=target;image=new Texture2D(width,height,TextureFormat.RGBA32,false);image.ReadPixels(new Rect(0,0,width,height),0,0);image.Apply();return Convert.ToBase64String(image.EncodeToPNG());}
        finally{RenderTexture.active=previous;RenderTexture.ReleaseTemporary(target);if(image)OwnedAssets.Release(image!);}
    }
    public Material DefaultMaterial(bool glow){
        var candidates=Resources.FindObjectsOfTypeAll<Material>().Where(m=>m&&m.shader&&m.shader.isSupported&&m.HasProperty("_Color")).ToList();
        var found=candidates.FirstOrDefault(m=>m.shader.name=="BeatSaber/Unlit Glow")??candidates.FirstOrDefault(m=>m.shader.name.IndexOf("Saber",StringComparison.OrdinalIgnoreCase)>=0)??candidates.FirstOrDefault(m=>m.shader.name=="Custom/SimpleLit");
        Material result;
        if(found)result=new Material(found!);else {var shader=Shader.Find("Unlit/Color");if(!shader)throw new InvalidOperationException("Open a saber to load a compatible material.");result=new Material(shader);}
        result.name="Workshop Material";result.color=glow?Color.white:new Color(.09f,.1f,.13f,1);if(result.HasProperty("_Glow"))result.SetFloat("_Glow",glow?1:0);if(result.HasProperty("_CustomColors"))result.SetFloat("_CustomColors",glow?1:0);return result;
    }
    public static SurfaceEdit CaptureMaterial(Material material){
        var edit=new SurfaceEdit{Name=material.name,Shader=material.shader.name,Reset=true,RenderQueue=material.renderQueue,Keywords=material.shaderKeywords};var shader=material.shader;
        for(int i=0;i<shader.GetPropertyCount();i++){
            string key=shader.GetPropertyName(i);switch(shader.GetPropertyType(i)){
                case ShaderPropertyType.Float:case ShaderPropertyType.Range:case ShaderPropertyType.Int:edit.Numbers[key]=material.GetFloat(key);break;
                case ShaderPropertyType.Color:var c=material.GetColor(key);edit.Colors[key]=new[]{c.r,c.g,c.b,c.a};break;
                case ShaderPropertyType.Vector:var v=material.GetVector(key);edit.Vectors[key]=new[]{v.x,v.y,v.z,v.w};break;
            }
        }return edit;
    }
    public void ApplyMaterial(Material material,SurfaceEdit surface){
        if(!string.IsNullOrEmpty(surface.Name))material.name=surface.Name;
        if(!string.IsNullOrEmpty(surface.Shader)){var shader=FindShader(surface.Shader);if(shader&&shader!.isSupported)material.shader=shader;}
        if(surface.Reset){var defaults=new Material(material.shader);material.CopyPropertiesFromMaterial(defaults);OwnedAssets.Release(defaults);material.shaderKeywords=surface.Keywords??Array.Empty<string>();material.renderQueue=surface.RenderQueue;}
        foreach(var x in surface.Numbers)if(material.HasProperty(x.Key))material.SetFloat(x.Key,x.Value);
        foreach(var x in surface.Colors)if(material.HasProperty(x.Key))material.SetColor(x.Key,new Color(x.Value[0],x.Value[1],x.Value[2],x.Value[3]));
        foreach(var x in surface.Vectors)if(material.HasProperty(x.Key))material.SetVector(x.Key,new Vector4(x.Value[0],x.Value[1],x.Value[2],x.Value[3]));
        foreach(var x in surface.Textures)if(material.HasProperty(x.Key)){
            if(!string.IsNullOrEmpty(x.Value.Png)){
                var key=x.Value.Png;if(!textureCache.TryGetValue(key,out var tex)){var bytes=Convert.FromBase64String(key);if(bytes.Length>12*1024*1024)throw new InvalidDataException("Texture exceeds 12 MB");tex=new Texture2D(2,2,TextureFormat.RGBA32,false);if(!ImageConversion.LoadImage(tex,bytes)){Object.Destroy(tex);throw new InvalidDataException("Invalid PNG or JPG");}if(tex.width>4096||tex.height>4096){Object.Destroy(tex);throw new InvalidDataException("Maximum texture size: 4096 x 4096");}textureCache.Add(key,tex);}material.SetTexture(x.Key,tex);
            }
            material.SetTextureScale(x.Key,new Vector2(x.Value.Scale[0],x.Value.Scale[1]));material.SetTextureOffset(x.Key,new Vector2(x.Value.Offset[0],x.Value.Offset[1]));
        }
    }
    public static void ApplyPlayerColor(GameObject root,Color color){foreach(var renderer in root.GetComponentsInChildren<Renderer>(true)){var identity=renderer.GetComponent<PartIdentity>();foreach(var material in renderer.sharedMaterials.Where(m=>m&&m.HasProperty("_Color"))){bool custom=identity?identity!.UsePlayerColor:material.HasProperty("_CustomColors")?material.GetFloat("_CustomColors")>0:material.HasProperty("_Glow")&&material.GetFloat("_Glow")>0;if(custom)material.SetColor("_Color",color);}}}
    public string Save(){
        foreach(var s in Project.Surfaces.Values.Concat(Project.Parts.Select(x=>x.Surface)))if(shaderFiles.TryGetValue(s.Shader,out var file)){
            string relative=file.Substring(Path.Combine(GameRoot,"CustomSabers").Length+1);using(var sha=SHA256.Create())using(var stream=File.OpenRead(file))Project.ShaderFiles[relative]=BitConverter.ToString(sha.ComputeHash(stream)).Replace("-","").ToLowerInvariant();
        }
        Validate(Project);EnsureFolders();string name=string.Concat(Project.Name.Where(c=>!Path.GetInvalidFileNameChars().Contains(c))).Trim();if(name.Length==0)name="Workshop Saber";name=name.Substring(0,Math.Min(80,name.Length));var path=SafePath(ProjectFolder,name+".workshop.json");var json=JsonConvert.SerializeObject(Project,Formatting.Indented);var temporary=path+".tmp";File.WriteAllText(temporary,json,Encoding.UTF8);if(File.Exists(path))File.Replace(temporary,path,path+".bak");else File.Move(temporary,path);return path;
    }
    public async Task LoadProject(string path){
        path=SafePath(ProjectFolder,path);if(new FileInfo(path).Length>64L*1024*1024)throw new InvalidDataException("Project exceeds 64 MB");
        var p=JsonConvert.DeserializeObject<WorkshopProject>(File.ReadAllText(path),new JsonSerializerSettings{TypeNameHandling=TypeNameHandling.None,MaxDepth=64});Validate(p!);
        var previousProject=Project;var previousTemplate=template;bool previousEnabled=PlayEnabled;
        try{
            GameObject? desired=null;
            if(!string.IsNullOrEmpty(p!.SourceFile)){await OpenSaber(p.SourceFile);if(Project.SourceHash!=p.SourceHash)throw new InvalidDataException("Source saber changed. Reopen the source file.");desired=template;}
            foreach(var file in p.ShaderFiles){await OpenSaber(file.Key);if(Project.SourceHash!=file.Value)throw new InvalidDataException("Shader source changed: "+file.Key);}
            template=desired;Project=p;PlayEnabled=false;
        }catch{Project=previousProject;template=previousTemplate;PlayEnabled=previousEnabled;throw;}
    }
    public static void Validate(WorkshopProject p){
        bool Vec(float[] v,int n)=>v!=null&&v.Length==n&&v.All(x=>!float.IsNaN(x)&&!float.IsInfinity(x)&&Math.Abs(x)<10000);
        if(p==null||p.Version!=1||p.Name==null||p.Name.Length>200||p.Parts==null||p.Parts.Count>400||p.Surfaces==null||p.Transforms==null||p.ShaderFiles==null||p.ShaderFiles.Count>100||p.Surfaces.Count>10000||p.Transforms.Count>10000)throw new InvalidDataException("Invalid Workshop project");
        foreach(var e in p.Transforms.Values)if(e==null||!Vec(e.Position,3)||!Vec(e.Rotation,3)||!Vec(e.Scale,3))throw new InvalidDataException("Invalid transform");
        if(!(p.SaberLength>=.1f&&p.SaberLength<=3&&p.SaberWidth>=.1f&&p.SaberWidth<=3))throw new InvalidDataException("Invalid saber size");
        foreach(var x in p.Parts){if(x.Sections==null||x.Sections.Count<2||x.Sections.Count>128||x.Sides<3||x.Sides>128||!(x.Length>.00001f&&x.Length<=5)||!Vec(x.Position,3)||!Vec(x.Rotation,3))throw new InvalidDataException("Invalid profile");foreach(var s in x.Sections)if(float.IsNaN(s.T)||float.IsNaN(s.Radius)||float.IsNaN(s.X)||float.IsNaN(s.Y)||s.T<0||s.T>1||s.Radius<0||s.Radius>3||Math.Abs(s.X)>3||Math.Abs(s.Y)>3)throw new InvalidDataException("Invalid section");}
        foreach(var s in p.Surfaces.Values.Concat(p.Parts.Select(x=>x.Surface))){if(s==null||s.Numbers.Count>2000||s.Textures.Count>100)throw new InvalidDataException("Invalid material");foreach(var x in s.Numbers.Values)if(float.IsNaN(x)||float.IsInfinity(x))throw new InvalidDataException("Invalid material value");foreach(var x in s.Colors.Values.Concat(s.Vectors.Values))if(!Vec(x,4))throw new InvalidDataException("Invalid color / vector");foreach(var t in s.Textures.Values)if(t==null||!Vec(t.Scale,2)||!Vec(t.Offset,2)||t.Png==null||t.Png.Length>17*1024*1024)throw new InvalidDataException("Invalid texture");}
    }
    public static Mesh BuildMesh(ProfilePart p){
        var vertices=new List<Vector3>();var uv=new List<Vector2>();var indices=new List<int>();int sides=Mathf.Clamp(p.Sides,3,128);
        var rings=p.Sections.OrderBy(s=>s.T).ToArray();
        if(p.Shape=="ring"){
            int minor=12;float radius=rings[0].Radius,tube=rings[1].Radius;
            for(int i=0;i<=sides;i++)for(int j=0;j<=minor;j++){float a=i*Mathf.PI*2/sides,b=j*Mathf.PI*2/minor,r=radius+Mathf.Cos(b)*tube;vertices.Add(new Vector3(Mathf.Cos(a)*r,Mathf.Sin(a)*r,Mathf.Sin(b)*tube));uv.Add(new Vector2((float)i/sides,(float)j/minor));}
            for(int i=0;i<sides;i++)for(int j=0;j<minor;j++){int a=i*(minor+1)+j,b=a+minor+1;indices.AddRange(new[]{a,b,a+1,a+1,b,b+1});}
        }else{
            foreach(var s in rings)for(int i=0;i<=sides;i++){float a=i*Mathf.PI*2/sides;vertices.Add(new Vector3(s.X+Mathf.Cos(a)*s.Radius,s.Y+Mathf.Sin(a)*s.Radius,s.T*p.Length));uv.Add(new Vector2((float)i/sides,s.T));}
            for(int j=0;j<rings.Length-1;j++)for(int i=0;i<sides;i++){int a=j*(sides+1)+i,b=a+sides+1;indices.AddRange(new[]{a,a+1,b,a+1,b+1,b});}
            if(p.Caps)for(int end=0;end<2;end++){int ri=end==0?0:rings.Length-1,center=vertices.Count;var s=rings[ri];vertices.Add(new Vector3(s.X,s.Y,s.T*p.Length));uv.Add(new Vector2(.5f,s.T));for(int i=0;i<sides;i++){int a=ri*(sides+1)+i;indices.AddRange(end==0?new[]{center,a+1,a}:new[]{center,a,a+1});}}
        }
        var mesh=new Mesh{name=p.Name,indexFormat=IndexFormat.UInt32};mesh.SetVertices(vertices);mesh.SetUVs(0,uv);mesh.SetTriangles(indices,0);mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
    }
}
public sealed class PartIdentity : MonoBehaviour {public string Id="";public bool UsePlayerColor;}
