using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Components;
using BeatSaberMarkupLanguage.Components.Settings;
using BeatSaberMarkupLanguage;
using BeatSaberMarkupLanguage.GameplaySetup;
using UnityEngine;
using UnityEngine.Rendering;
using Zenject;
using Object=UnityEngine.Object;

namespace SaberWorkshop;

public sealed partial class EditorController : NotifiableBase,IInitializable,IDisposable
{
    private readonly GameplaySetup gameplaySetup;
    private readonly ColorSchemesSettings colorSchemes;
    private WorkshopWall? workspace;
    private readonly WorkshopState state=WorkshopState.Instance;
    private GameObject? preview;
    private List<Renderer> renderers=new List<Renderer>();
    private List<string> sources=new List<string>(),projects=new List<string>(),textures=new List<string>(),properties=new List<string>(),textureSlots=new List<string>();
    private int partIndex,materialIndex,propertyIndex,slotIndex,sectionIndex;
    private bool refreshing,advanced,visible=true,busy,editorOpen;
    private float previewScale=.65f;
    private Color left=new Color32(0xC8,0x14,0x14,0xFF),right=new Color32(0x28,0x8E,0xD2,0xFF);
    [UIComponent("source-list")] private DropDownListSetting sourceList=null!;
    [UIComponent("project-list")] private DropDownListSetting projectList=null!;
    [UIComponent("gameplay-project-list")] private DropDownListSetting gameplayProjectList=null!;
    [UIComponent("part-tree")] private CustomListTableData partTree=null!;
    [UIComponent("material-list")] private DropDownListSetting materialList=null!;
    [UIComponent("texture-material-list")] private DropDownListSetting textureMaterialList=null!;
    [UIComponent("shader-list")] private DropDownListSetting shaderList=null!;
    [UIComponent("property-list")] private DropDownListSetting propertyList=null!;
    [UIComponent("texture-slot-list")] private DropDownListSetting textureSlotList=null!;
    [UIComponent("texture-file-list")] private DropDownListSetting textureFileList=null!;
    [UIComponent("section-list")] private DropDownListSetting sectionList=null!;
    public EditorController(GameplaySetup gameplaySetup,ColorSchemesSettings colorSchemes){this.gameplaySetup=gameplaySetup;this.colorSchemes=colorSchemes;}
    public void Initialize(){gameplaySetup.AddTab("Saber Workshop","SaberWorkshop.Gameplay.bsml",this,MenuType.Solo);RefreshFiles();}
    public void Dispose(){DisposeEffectDraft();gameplaySetup.RemoveTab("Saber Workshop");if(preview)Object.Destroy(preview);workspace?.Dispose();}
    [UIAction("OpenEditor")] public void OpenEditor(){Guard(()=>{
        if(workspace==null)workspace=new WorkshopWall(this);
        workspace.Show();EditorOpened();
        if(!state.Ready){state.CreateStarter();partIndex=0;BuildPreview();SetStatus("Flux created. Select a part to edit.");}
    });}
    [UIAction("CloseEditor")] public void CloseEditor(){workspace?.Hide();EditorClosed();}
    [UIAction("RecenterWall")] public void RecenterWall(){workspace?.Recenter();if(preview)PlacePreview();}
    public void EditorOpened(){editorOpen=true;var scheme=colorSchemes.GetOverrideColorScheme();if(scheme!=null){left=scheme.saberAColor;right=scheme.saberBColor;}RefreshFiles();if(preview){preview!.SetActive(visible&&!ShaderInspector);PlacePreview();ApplyColors();}RefreshSelection();}
    public void EditorClosed(){editorOpen=false;if(preview)preview!.SetActive(false);}
    [UIAction("#post-parse")] private void Parsed(){RefreshFiles();RefreshSelection();}

    public string Status {get;private set;}="";
    public string ProjectName {get=>state.Project.Name;set{state.Project.Name=(value??"New saber").Substring(0,Math.Min(120,(value??"New saber").Length));NotifyPropertyChanged();NotifyPropertyChanged(nameof(EditorTitle));}}
    public bool PlayEnabled {get=>state.PlayEnabled;set{if(value&&!state.HasGeometry){SetStatus("Add a blade or handle first.");return;}state.PlayEnabled=value;NotifyPropertyChanged();SetStatus(value?"Enabled for the next map.":"Gameplay model disabled.");}}
    public bool LinkHands {get=>state.Project.LinkHands;set{state.Project.LinkHands=value;NotifyPropertyChanged();}}
    public List<object> SourceOptions {get;private set;}=new List<object>();
    public List<object> ProjectOptions {get;private set;}=new List<object>();
    public List<object> TextureFileOptions {get;private set;}=new List<object>();
    public List<object> PartOptions {get;private set;}=new List<object>();
    public List<object> MaterialOptions {get;private set;}=new List<object>();
    public List<object> ShaderOptions {get;private set;}=new List<object>();
    public List<object> PropertyOptions {get;private set;}=new List<object>();
    public List<object> TextureSlotOptions {get;private set;}=new List<object>();
    public List<object> SectionOptions {get;private set;}=new List<object>();
    public string SourceChoice {get;set;}="";
    public string ProjectChoice {get;set;}="";
    public string TextureFileChoice {get;set;}="";
    public string ShaderChoice {get;set;}="";
    public string PartChoice {get=>Choice(PartOptions,partIndex);set{if(refreshing)return;partIndex=Math.Max(0,PartOptions.IndexOf(value));materialIndex=propertyIndex=slotIndex=sectionIndex=0;RefreshSelection();}}
    public string MaterialChoice {get=>Choice(MaterialOptions,materialIndex);set{if(refreshing)return;materialIndex=Math.Max(0,MaterialOptions.IndexOf(value));propertyIndex=slotIndex=0;RefreshSelection();}}
    public string PropertyChoice {get=>Choice(PropertyOptions,propertyIndex);set{if(refreshing)return;propertyIndex=Math.Max(0,PropertyOptions.IndexOf(value));NotifyValues();}}
    public string TextureSlotChoice {get=>Choice(TextureSlotOptions,slotIndex);set{if(refreshing)return;slotIndex=Math.Max(0,TextureSlotOptions.IndexOf(value));NotifyValues();}}
    public string SectionChoice {get=>Choice(SectionOptions,sectionIndex);set{if(refreshing)return;sectionIndex=Math.Max(0,SectionOptions.IndexOf(value));NotifyValues();}}
    public bool Advanced {get=>advanced;set{advanced=value;propertyIndex=0;RefreshSelection();NotifyPropertyChanged();}}
    private static string Choice(List<object> options,int index)=>options.Count>0?options[Mathf.Clamp(index,0,options.Count-1)].ToString()!:"";
    private Renderer? Selected=>renderers.Count>0?renderers[Mathf.Clamp(partIndex,0,renderers.Count-1)]:null;
    private Material? Material=>Selected&&Selected!.sharedMaterials.Length>0?Selected.sharedMaterials[Mathf.Clamp(materialIndex,0,Selected.sharedMaterials.Length-1)]:null;
    private ProfilePart? Part=>Selected?.GetComponent<PartIdentity>() is PartIdentity identity?state.Project.Parts.FirstOrDefault(p=>p.Id==identity.Id):null;
    private string Property=>properties.Count>0?properties[Mathf.Clamp(propertyIndex,0,properties.Count-1)]:"";
    private string TextureSlot=>textureSlots.Count>0?textureSlots[Mathf.Clamp(slotIndex,0,textureSlots.Count-1)]:"";
    private ShaderPropertyType? PropertyType=>Material&&Property!=""?Material!.shader.GetPropertyType(Material.shader.FindPropertyIndex(Property)):(ShaderPropertyType?)null;
    public bool IsNumber=>PropertyType==ShaderPropertyType.Float||PropertyType==ShaderPropertyType.Range||PropertyType==ShaderPropertyType.Int;
    public bool IsColor=>PropertyType==ShaderPropertyType.Color;
    public bool IsVector=>PropertyType==ShaderPropertyType.Vector;
    public bool IsProfile=>Part!=null&&Part.Shape!="ring"&&Part.Geometry==null&&Part.Particles==null&&Part.Trail==null;
    public bool IsCreatedPart=>Part!=null;
    public string MaterialInfo=>Material?Material!.name+"\n"+Material.shader.name:"Select a part and layer.";
    public string PropertyHelp {get{if(!Material||Property=="")return "Select a property.";int index=Material!.shader.FindPropertyIndex(Property);var type=Material.shader.GetPropertyType(index);return Property+(type==ShaderPropertyType.Range?" · range "+Material.shader.GetPropertyRangeLimits(index):"");}}
    public string TextureInfo {get{if(!Material||TextureSlot=="")return "No 2D texture slots.";var texture=Material!.GetTexture(TextureSlot);return TextureSlot+" · "+(texture?texture!.name+" · "+texture.width+" × "+texture.height:"none");}}

    private void SetStatus(string text){Status=text.Length>90?text.Substring(0,87)+"...":text;NotifyPropertyChanged(nameof(Status));}
    private void Guard(Action action){try{action();}catch(Exception e){Plugin.Log.Error(e.ToString());SetStatus(e.Message);}}
    private async Task Run(Func<Task> action){if(busy)return;busy=true;try{await action();}catch(Exception e){Plugin.Log.Error(e.ToString());SetStatus(e.Message);}finally{busy=false;}}
    [UIAction("RefreshFiles")] public void RefreshFiles(){Guard(()=>{
        WorkshopState.EnsureFolders();sources=WorkshopState.SaberFiles().ToList();projects=Directory.GetFiles(WorkshopState.ProjectFolder,"*.workshop.json").OrderBy(x=>x).ToList();textures=Directory.GetFiles(WorkshopState.TextureFolder).Where(x=>new[]{".png",".jpg",".jpeg"}.Contains(Path.GetExtension(x).ToLowerInvariant())).OrderBy(x=>x).ToList();
        SourceOptions=sources.Select(x=>(object)x.Substring(Path.Combine(WorkshopState.GameRoot,"CustomSabers").Length+1)).ToList();ProjectOptions=projects.Select(x=>(object)Path.GetFileName(x).Replace(".workshop.json","")).ToList();TextureFileOptions=textures.Select(x=>(object)Path.GetFileName(x)).ToList();
        if(!SourceOptions.Contains(SourceChoice))SourceChoice=Choice(SourceOptions,0);if(!ProjectOptions.Contains(ProjectChoice))ProjectChoice=Choice(ProjectOptions,0);if(!TextureFileOptions.Contains(TextureFileChoice))TextureFileChoice=Choice(TextureFileOptions,0);
        UpdateList(sourceList,SourceOptions,SourceChoice);UpdateList(projectList,ProjectOptions,ProjectChoice);UpdateList(gameplayProjectList,ProjectOptions,ProjectChoice);UpdateList(textureFileList,TextureFileOptions,TextureFileChoice);
    });}
    [UIAction("OpenSource")] public async void OpenSource(){await Run(async()=>{if(SourceChoice=="")throw new InvalidOperationException("Add a .saber to CustomSabers, then refresh files.");SetStatus("Loading saber...");await state.OpenSaber(SourceChoice);partIndex=-1;BuildPreview();SetStatus("Opened "+state.Project.Name+"");});}
    [UIAction("OpenProject")] public async void OpenProject(){await Run(async()=>{int index=ProjectOptions.IndexOf(ProjectChoice);if(index<0)throw new InvalidOperationException("No saved presets.");await state.LoadProject(Path.GetFileName(projects[index]));undo.Clear();redo.Clear();partIndex=-1;BuildPreview();ClosePresetDialog();CloseLibrary();SetStatus("Preset opened.");});}
    [UIAction("New")] public void New(){Guard(()=>{state.CreateStarter();partIndex=0;BuildPreview();SetStatus("Flux created.");});}
    [UIAction("Save")] public void Save(){Guard(()=>{var path=state.Save();RefreshFiles();ProjectChoice=Path.GetFileName(path).Replace(".workshop.json","");UpdateList(projectList,ProjectOptions,ProjectChoice);UpdateList(gameplayProjectList,ProjectOptions,ProjectChoice);SetStatus("Preset saved: "+Path.GetFileName(path));});}
    [UIAction("ShowPreview")] public void ShowPreview(){Guard(()=>{if(!state.Ready)throw new InvalidOperationException("Create or open a saber first.");if(!preview)BuildPreview();visible=true;PlacePreview();preview!.SetActive(true);NotifyPropertyChanged(nameof(PreviewVisible));});}
    private void PlacePreview(){
        if(!preview)return;
        var wall=workspace?.Transform;
        var yaw=wall?wall!.rotation:Quaternion.identity;
        preview!.transform.SetParent(wall,true);preview.transform.SetPositionAndRotation(Vector3.zero,yaw);
        preview.transform.localScale=Vector3.one*(previewScale/(wall?wall!.lossyScale.x:1));
        var target=WorkbenchLayout.PreviewCenter(wall);
        var meshes=preview.GetComponentsInChildren<Renderer>(true).Where(r=>(r is MeshRenderer||r is SkinnedMeshRenderer)&&!r.GetComponentInParent<SizeGuideMarker>()&&r.enabled&&VisibleParents(r.transform,preview.transform)).ToArray();var bounds=new Bounds(Vector3.zero,Vector3.zero);
        if(meshes.Length==0&&showSizeGuide&&guidePreview)meshes=guidePreview!.GetComponentsInChildren<Renderer>();
        if(meshes.Length>0){bounds=meshes[0].bounds;foreach(var mesh in meshes.Skip(1))bounds.Encapsulate(mesh.bounds);}float fit=WorkbenchLayout.PreviewFit(bounds);if(fit<1){preview.transform.localScale*=fit;bounds.center*=fit;}preview.transform.position=target-bounds.center;ScalePreviewTrails();
        Plugin.Log.Info("Preview centered at "+target+" in static menu space");
    }
    private static bool VisibleParents(Transform node,Transform root){while(node!=root&&node){if(!node.gameObject.activeSelf)return false;node=node.parent;}return true;}
    private void BuildPreview(){if(preview){preview!.SetActive(false);Object.Destroy(preview);}preview=state.InstantiatePair();guidePreview=WorkshopAssets.AttachGuide(preview);guidePreview.SetActive(showSizeGuide);PlacePreview();RefreshSelection();ApplyColors();PreparePreview();ConfigureMotion();preview.SetActive(visible&&editorOpen&&!ShaderInspector);NotifyPropertyChanged(nameof(ProjectName));NotifyPropertyChanged(nameof(PlayEnabled));}
    public bool PreviewVisible {get=>visible;set{visible=value;if(preview)preview!.SetActive(value);NotifyPropertyChanged();}}
    public float PreviewScale {get=>previewScale;set{previewScale=Mathf.Clamp(value,.2f,2);if(preview)PlacePreview();}}
    public Color LeftColor {get=>left;set{left=value;ApplyColors();}}
    public Color RightColor {get=>right;set{right=value;ApplyColors();}}
    private void PreparePreview(){if(!preview)return;var presentation=preview!.GetComponent<PreviewPresentation>()??preview.AddComponent<PreviewPresentation>();presentation.Apply();foreach(var r in preview.GetComponentsInChildren<Renderer>(true)){var f=r.GetComponent<MeshFilter>();if(f&&f!.sharedMesh)foreach(var m in r.sharedMaterials)if(m)WorkshopState.SetBounds(m,f.sharedMesh.bounds);}}
    private void ApplyColors(){if(!preview)return;var l=preview!.transform.Find("LeftSaber");var r=preview.transform.Find("RightSaber");if(l)WorkshopState.ApplyPlayerColor(l.gameObject,left);if(r)WorkshopState.ApplyPlayerColor(r.gameObject,right);}
    private void RefreshSelection(){
        if(refreshing)return;refreshing=true;try{
            if(preview){var hand=preview!.transform.Find("LeftSaber");renderers=hand?hand.GetComponentsInChildren<Renderer>(true).ToList():new List<Renderer>();}else renderers.Clear();
            PartOptions=renderers.Select((r,i)=>(object)$"{i+1}. {(r.gameObject.activeInHierarchy&&r.enabled?"":"[hidden] ")}{r.name}").ToList();if(partIndex<0)partIndex=Math.Max(0,renderers.FindIndex(r=>r.enabled&&r.gameObject.activeInHierarchy));partIndex=Mathf.Clamp(partIndex,0,Math.Max(0,renderers.Count-1));
            MaterialOptions=Selected?Selected!.sharedMaterials.Select((m,i)=>(object)$"{i+1}. {(m?m.name:"Empty layer")}").ToList():new List<object>();materialIndex=Mathf.Clamp(materialIndex,0,Math.Max(0,MaterialOptions.Count-1));
            properties.Clear();textureSlots.Clear();PropertyOptions=new List<object>();TextureSlotOptions=new List<object>();
            if(Material){var shader=Material!.shader;for(int i=0;i<shader.GetPropertyCount();i++){string key=shader.GetPropertyName(i),label=Label(key,shader.GetPropertyDescription(i));var type=shader.GetPropertyType(i);if(type==ShaderPropertyType.Texture){textureSlots.Add(key);TextureSlotOptions.Add(label+" · "+key);}else if(key!="_PreviewTime"&&(advanced||Common(key))){properties.Add(key);PropertyOptions.Add(label+(advanced?" · "+key:""));}}}
            propertyIndex=Mathf.Clamp(propertyIndex,0,Math.Max(0,properties.Count-1));slotIndex=Mathf.Clamp(slotIndex,0,Math.Max(0,textureSlots.Count-1));
            ShaderOptions=state.Shaders().Select(s=>(object)s.name).ToList();ShaderChoice=Material?Material!.shader.name:Choice(ShaderOptions,0);
            SectionOptions=Part!=null?Part.Sections.Select((s,i)=>(object)$"{i+1} · {Math.Round(s.T*100)}%").ToList():new List<object>();sectionIndex=Mathf.Clamp(sectionIndex,0,Math.Max(0,SectionOptions.Count-1));
            RefreshTree();
            foreach(var list in new[]{materialList,textureMaterialList})UpdateList(list,MaterialOptions,MaterialChoice);
            UpdateList(shaderList,ShaderOptions,ShaderChoice);UpdateList(propertyList,PropertyOptions,PropertyChoice);UpdateList(textureSlotList,TextureSlotOptions,TextureSlotChoice);UpdateList(sectionList,SectionOptions,SectionChoice);
        }finally{refreshing=false;}if(ShaderInspector){RefreshEffectTargets();RefreshMaterialThumbnail();}NotifyValues();
    }
    private void UpdateList(DropDownListSetting list,List<object> options,string value){if(!list)return;bool old=refreshing;refreshing=true;try{list.Values=options;list.UpdateChoices();if(options.Count>0)list.Value=value;}finally{refreshing=old;}}
    private void NotifyValues(){bool old=refreshing;refreshing=true;try{SyncSliderRanges();foreach(string name in new[]{nameof(IsNumber),nameof(IsColor),nameof(IsVector),nameof(IsProfile),nameof(MaterialInfo),nameof(PropertyHelp),nameof(TextureInfo),nameof(NumberValue),nameof(ColorValue),nameof(ColorIntensity),nameof(AlphaValue),nameof(VectorX),nameof(VectorY),nameof(VectorZ),nameof(VectorW),nameof(TileX),nameof(TileY),nameof(OffsetX),nameof(OffsetY),nameof(PartLength),nameof(SectionRadius),nameof(SectionPosition),nameof(SectionX),nameof(SectionY),nameof(Sides),nameof(PositionX),nameof(PositionY),nameof(PositionZ),nameof(RotationX),nameof(RotationY),nameof(RotationZ),nameof(PartVisible),nameof(PartName),nameof(MaterialName),nameof(PlayerColor),nameof(TintColor),nameof(Glow),nameof(HasTint),nameof(HasGlow),nameof(SaberLength),nameof(SaberWidth),nameof(LinkHands),nameof(IsCreatedPart),nameof(InvertedFaces),nameof(OutlineWidth)})NotifyPropertyChanged(name);NotifyModern();NotifyPropertyChanged(nameof(EditorTitle));}finally{refreshing=old;}}
    private static bool Common(string key)=>new[]{"_Color","_TintColor","_StarNestMainColor","_Glow","_BSGlow","_ALTGlow","_Bloom","_CustomColors","_Opacity","_Intensity","_Brightness","_Metallic","_Glossiness"}.Contains(key);
    private static string Label(string key,string fallback){return new Dictionary<string,string>{{"_Color","Color"},{"_TintColor","Tint"},{"_StarNestMainColor","Plasma color"},{"_Glow","Glow"},{"_BSGlow","Glow"},{"_CustomColors","Player color: 0 off, 1 on"},{"_Opacity","Opacity"},{"_Intensity","Brightness"},{"_Brightness","Texture brightness"},{"_AlphaTexture","Pattern / mask"},{"_AlphaTex","Pattern / mask"},{"_MainTex","Main texture"},{"_Displacement","Distortion / fire"},{"_DisplaceTex","Distortion / fire"},{"_FadeMask","Fade mask"},{"_MaskTex","Mask"},{"_textureScroll","Texture speed X / Y"},{"_fademaskScroll","Mask speed X / Y"},{"_Metallic","Metallic"},{"_Glossiness","Smoothness"},{"_Markiplier","Edge sharpness"}}.TryGetValue(key,out var label)?label:string.IsNullOrEmpty(fallback)?key:fallback;}
    private SurfaceEdit? Edit {get{if(!Material||!Selected)return null;if(Part!=null)return Part.Surface;var hand=preview!.transform.Find("LeftSaber");var key="LeftSaber/"+WorkshopState.PathOf(Selected!.transform,hand)+"#"+materialIndex;if(!state.Project.Surfaces.TryGetValue(key,out var edit)){edit=new SurfaceEdit();state.Project.Surfaces[key]=edit;}return edit;}}
    private void ChangedMaterial(){if(!Material||Edit==null)return;Guard(()=>{state.ApplyMaterial(Material!,Edit!);if(state.Project.LinkHands&&Part==null){var hand=preview!.transform.Find("LeftSaber");var path=WorkshopState.PathOf(Selected!.transform,hand);state.Project.Surfaces["RightSaber/"+path+"#"+materialIndex]=Edit!;var rightHand=preview.transform.Find("RightSaber");var partner=rightHand.GetComponentsInChildren<Renderer>(true).FirstOrDefault(r=>WorkshopState.PathOf(r.transform,rightHand)==path);if(partner&&partner!.sharedMaterials.Length>materialIndex)state.ApplyMaterial(partner.sharedMaterials[materialIndex],Edit!);}else if(Part!=null){foreach(var identity in preview!.GetComponentsInChildren<PartIdentity>(true).Where(i=>i.Id==Part.Id))identity.UsePlayerColor=Part.Surface.UsePlayerColor;var partner=preview!.transform.Find("RightSaber").GetComponentsInChildren<PartIdentity>(true).FirstOrDefault(x=>x.Id==Part.Id);if(partner){var renderer=partner!.GetComponent<Renderer>();if(renderer)state.ApplyMaterial(renderer.sharedMaterial,Edit!);}}PreparePreview();ApplyColors();NotifyValues();});}
    [UIAction("ApplyShader")] public void ApplyShader(){if(Edit==null)return;Checkpoint("shader");Edit!.Shader=ShaderChoice;ChangedMaterial();RefreshSelection();}
    public string MaterialName {get=>Material?Material!.name:"";set{if(refreshing||Edit==null)return;Edit!.Name=value;ChangedMaterial();RefreshSelection();}}
    public bool PlayerColor {get=>Part!=null?Part.Surface.UsePlayerColor:Material&&Material!.HasProperty("_CustomColors")&&Material.GetFloat("_CustomColors")>0;set{if(Edit==null||refreshing)return;Edit!.UsePlayerColor=value;if(Material!.HasProperty("_CustomColors"))Edit.Numbers["_CustomColors"]=value?1:0;if(Part!=null){Part.Surface.UsePlayerColor=value;foreach(var id in preview!.GetComponentsInChildren<PartIdentity>(true).Where(i=>i.Id==Part.Id))id.UsePlayerColor=value;}ChangedMaterial();if(value)ApplyColors();}}
    [UIAction("CreateMaterial")] public void CreateMaterial()=>MakeMaterial(false);
    [UIAction("DuplicateMaterial")] public void DuplicateMaterial()=>MakeMaterial(true);
    private void MakeMaterial(bool duplicate){Guard(()=>{
        if(!Material||!Selected)throw new InvalidOperationException("Select a visible part and layer first.");
        Checkpoint("material-create");var material=duplicate?new Material(Material!):state.NewMaterial(ShaderChoice);material.name=duplicate?Material!.name+" copy":"New material";
        var surface=WorkshopState.CaptureMaterial(material);surface.UsePlayerColor=false;if(material.HasProperty("_CustomColors"))surface.Numbers["_CustomColors"]=0;
        if(duplicate){var shader=material.shader;for(int i=0;i<shader.GetPropertyCount();i++)if(shader.GetPropertyType(i)==ShaderPropertyType.Texture&&shader.GetPropertyTextureDimension(i)==TextureDimension.Tex2D){string key=shader.GetPropertyName(i);var tex=material.GetTexture(key);if(tex){var scale=material.GetTextureScale(key);var offset=material.GetTextureOffset(key);surface.Textures[key]=new TextureEdit{Png=WorkshopState.TexturePng(tex!),Scale=new[]{scale.x,scale.y},Offset=new[]{offset.x,offset.y}};}}}
        if(Part!=null)Part.Surface=surface;else{var hand=preview!.transform.Find("LeftSaber");state.Project.Surfaces["LeftSaber/"+WorkshopState.PathOf(Selected!.transform,hand)+"#"+materialIndex]=surface;}
        var mats=Selected!.sharedMaterials;mats[materialIndex]=material;Selected.sharedMaterials=mats;preview!.GetComponent<OwnedAssets>().Assets.Add(material);ChangedMaterial();RefreshSelection();SetStatus(duplicate?"Material duplicated.":"Material created.");
    });}
    public float NumberValue {get=>Material&&IsNumber?Material!.GetFloat(Property):0;set{if(refreshing||!IsNumber||Edit==null||float.IsNaN(value)||float.IsInfinity(value))return;Checkpoint("number:"+Property);Edit!.Numbers[Property]=Mathf.Clamp(value,-100000,100000);ChangedMaterial();}}
    private Color RawColor=>Material&&IsColor?Material!.GetColor(Property):Color.white;
    public Color ColorValue {get{var c=RawColor;float max=Mathf.Max(1,Mathf.Max(c.r,Mathf.Max(c.g,c.b)));return new Color(c.r/max,c.g/max,c.b/max,c.a);}set{if(refreshing||!IsColor||Edit==null)return;Checkpoint("color:"+Property);float intensity=ColorIntensity;Edit!.Colors[Property]=new[]{value.r*intensity,value.g*intensity,value.b*intensity,RawColor.a};ChangedMaterial();}}
    public float ColorIntensity {get{var c=RawColor;return Mathf.Max(1,Mathf.Max(c.r,Mathf.Max(c.g,c.b)));}set{if(refreshing||!IsColor||Edit==null)return;Checkpoint("intensity:"+Property);var c=ColorValue;Edit!.Colors[Property]=new[]{c.r*value,c.g*value,c.b*value,c.a};ChangedMaterial();}}
    public float AlphaValue {get=>RawColor.a;set{if(refreshing||!IsColor||Edit==null)return;Checkpoint("alpha:"+Property);var c=RawColor;Edit!.Colors[Property]=new[]{c.r,c.g,c.b,Mathf.Clamp01(value)};ChangedMaterial();}}
    private float Vector(int axis)=>Material&&IsVector?Material!.GetVector(Property)[axis]:0;
    private void SetVector(int axis,float value){if(refreshing||!IsVector||Edit==null)return;Checkpoint("vector:"+Property+axis);var v=Material!.GetVector(Property);v[axis]=value;Edit!.Vectors[Property]=new[]{v.x,v.y,v.z,v.w};ChangedMaterial();}
    public float VectorX {get=>Vector(0);set=>SetVector(0,value);}public float VectorY {get=>Vector(1);set=>SetVector(1,value);}public float VectorZ {get=>Vector(2);set=>SetVector(2,value);}public float VectorW {get=>Vector(3);set=>SetVector(3,value);}
    private TextureEdit? TexEdit {get{if(Edit==null||TextureSlot=="")return null;if(!Edit!.Textures.TryGetValue(TextureSlot,out var t)){var scale=Material!.GetTextureScale(TextureSlot);var offset=Material.GetTextureOffset(TextureSlot);t=new TextureEdit{Scale=new[]{scale.x,scale.y},Offset=new[]{offset.x,offset.y}};Edit.Textures[TextureSlot]=t;}return t;}}
    [UIAction("ApplyTexture")] public void ApplyTexture(){Guard(()=>{if(TexEdit==null)throw new InvalidOperationException("Select a texture slot.");var file=WorkshopState.SafePath(WorkshopState.TextureFolder,TextureFileChoice);if(!File.Exists(file)||new FileInfo(file).Length>12*1024*1024)throw new InvalidDataException("Image missing or larger than 12 MB");TexEdit!.Png=Convert.ToBase64String(File.ReadAllBytes(file));ChangedMaterial();SetStatus("Texture applied.");});}
    [UIAction("GeneratePlasma")] public void GeneratePlasma(){Guard(()=>{
        WorkshopState.EnsureFolders();string name="Plasma-"+DateTime.UtcNow.ToString("yyyyMMdd-HHmmss")+".png";string path=WorkshopState.SafePath(WorkshopState.TextureFolder,name);
        var texture=new Texture2D(256,512,TextureFormat.RGBA32,false);var pixels=new Color32[256*512];
        for(int y=0;y<512;y++)for(int x=0;x<256;x++){
            float u=(float)x/256*Mathf.PI*2,v=(float)y/512*Mathf.PI*2;
            float flow=Mathf.Sin(u*7+Mathf.Sin(v*3)*1.7f+Mathf.Cos(u*3-v*2)*.8f);
            float vein=Mathf.Pow(1-Mathf.Abs(flow),7),cloud=(Mathf.Sin(u*3+v*2)*Mathf.Cos(u*5-v*7)+1)*.5f;
            byte intensity=(byte)Mathf.RoundToInt(Mathf.Clamp01(.025f+.16f*cloud+.9f*vein)*255);pixels[y*256+x]=new Color32(intensity,intensity,intensity,255);
        }
        texture.SetPixels32(pixels);texture.Apply();try{File.WriteAllBytes(path,texture.EncodeToPNG());}finally{Object.Destroy(texture);}RefreshFiles();TextureFileChoice=name;UpdateList(textureFileList,TextureFileOptions,name);SetStatus("Plasma image ready. Select a slot and apply it.");
    });}
    public float TileX {get=>Material&&TextureSlot!=""?Material!.GetTextureScale(TextureSlot).x:1;set{if(TexEdit==null)return;TexEdit!.Scale[0]=value;ChangedMaterial();}}
    public float TileY {get=>Material&&TextureSlot!=""?Material!.GetTextureScale(TextureSlot).y:1;set{if(TexEdit==null)return;TexEdit!.Scale[1]=value;ChangedMaterial();}}
    public float OffsetX {get=>Material&&TextureSlot!=""?Material!.GetTextureOffset(TextureSlot).x:0;set{if(TexEdit==null)return;TexEdit!.Offset[0]=value;ChangedMaterial();}}
    public float OffsetY {get=>Material&&TextureSlot!=""?Material!.GetTextureOffset(TextureSlot).y:0;set{if(TexEdit==null)return;TexEdit!.Offset[1]=value;ChangedMaterial();}}

    [UIAction("AddProfile")] public void AddProfile(){Guard(()=>{if(state.Project.Parts.Count>=400)throw new InvalidOperationException("Maximum 400 parts.");Checkpoint("add-profile");state.AddPart("Profile "+(state.Project.Parts.Count+1),true);BuildPreview();partIndex=renderers.Count-1;RefreshSelection();});}
    [UIAction("AddRing")] public void AddRing(){Guard(()=>{Checkpoint("add-ring");var p=state.AddPart("Ring",true);p.Shape="ring";p.Sections[0].Radius=.045f;p.Sections[1].Radius=.002f;p.Position[2]=.04f;BuildPreview();partIndex=renderers.Count-1;RefreshSelection();});}
    [UIAction("AddInner")] public void AddInner(){Guard(()=>{
        if(!Selected)throw new InvalidOperationException("Select a part first.");Checkpoint("add-inner");var original=Part;
        if(original==null)throw new InvalidOperationException("Select an existing inner layer in Materials. New inner profiles require a created part.");
        var copy=Newtonsoft.Json.JsonConvert.DeserializeObject<ProfilePart>(Newtonsoft.Json.JsonConvert.SerializeObject(original))!;copy.Id=Guid.NewGuid().ToString("N");copy.Name=original.Name+" · plasma";foreach(var s in copy.Sections)s.Radius*=.85f;state.Project.Parts.Add(copy);BuildPreview();partIndex=renderers.Count-1;RefreshSelection();SetStatus("Inner layer added.");
    });}
    [UIAction("DeletePart")] public void DeletePart(){if(!Selected)return;Checkpoint("delete");if(Part==null){PartVisible=false;RefreshTree();SetStatus("Source part hidden. Undo restores it.");return;}var id=Part.Id;state.Project.Parts.RemoveAll(p=>p.Id==id);BuildPreview();}
    private Section? Section=>Part?.Sections[Mathf.Clamp(sectionIndex,0,Part.Sections.Count-1)];
    [UIAction("AddSection")] public void AddSection(){if(Part==null||Part.Shape=="ring"||Part.Sections.Count>=128)return;Checkpoint("add-section");var list=Part.Sections;int index=0;float gap=-1;for(int i=0;i<list.Count-1;i++)if(list[i+1].T-list[i].T>gap){gap=list[i+1].T-list[i].T;index=i;}var a=list[index];var b=list[index+1];list.Insert(index+1,new Section{T=(a.T+b.T)/2,Radius=(a.Radius+b.Radius)/2,Aspect=(a.Aspect+b.Aspect)/2,X=(a.X+b.X)/2,Y=(a.Y+b.Y)/2});sectionIndex=index+1;RebuildShape();}
    [UIAction("DeleteSection")] public void DeleteSection(){if(Part==null||Part.Shape=="ring"||Part.Sections.Count<=2)return;Checkpoint("remove-section");Part.Sections.RemoveAt(sectionIndex);sectionIndex=Math.Min(sectionIndex,Part.Sections.Count-1);RebuildShape();}
    private void RebuildShape(){if(refreshing)return;Guard(()=>{
        var part=Part;if(part==null||!preview){BuildPreview();return;}
        var owner=preview!.GetComponent<OwnedAssets>();
        foreach(var identity in preview.GetComponentsInChildren<PartIdentity>(true).Where(i=>i.Id==part.Id)){
            identity.transform.localPosition=WorkshopState.V3(part.Position);identity.transform.localEulerAngles=WorkshopState.V3(part.Rotation);
            var filter=identity.GetComponent<MeshFilter>();if(!filter)continue;
            var mesh=WorkshopState.BuildMesh(part);var previous=filter!.sharedMesh;filter.sharedMesh=mesh;owner.Assets.Add(mesh);if(previous){owner.Assets.Remove(previous);OwnedAssets.Release(previous);}
        }
        bool old=refreshing;refreshing=true;try{SectionOptions=part.Sections.Select((point,i)=>(object)$"{i+1} · {Math.Round(point.T*100)}%").ToList();UpdateList(sectionList,SectionOptions,SectionChoice);}finally{refreshing=old;}
        PreparePreview();NotifyValues();
    });}
    public float PartLength {get=>Part?.Length*1000??0;set{if(Part==null||refreshing)return;Checkpoint("length");Part.Length=Mathf.Clamp(value/1000,.001f,2);RebuildShape();}}
    public float SectionPosition {get=>(Section?.T??0)*100;set{if(Section==null||Part==null||Part.Shape=="ring"||refreshing)return;Checkpoint("section:"+sectionIndex);Section.T=Mathf.Clamp(value/100,sectionIndex>0?Part.Sections[sectionIndex-1].T:0,sectionIndex+1<Part.Sections.Count?Part.Sections[sectionIndex+1].T:1);RebuildShape();}}
    public float SectionRadius {get=>(Section?.Radius??0)*1000;set{if(Section==null||refreshing)return;Checkpoint("radius:"+sectionIndex);Section.Radius=Mathf.Clamp(value/1000,0,.3f);RebuildShape();}}
    public float SectionX {get=>(Section?.X??0)*1000;set{if(Section==null||refreshing)return;Checkpoint("section-x:"+sectionIndex);Section.X=Mathf.Clamp(value/1000,-.3f,.3f);RebuildShape();}}
    public float SectionY {get=>(Section?.Y??0)*1000;set{if(Section==null||refreshing)return;Checkpoint("section-y:"+sectionIndex);Section.Y=Mathf.Clamp(value/1000,-.3f,.3f);RebuildShape();}}
    public float Sides {get=>Part?.Sides??32;set{if(Part==null||refreshing)return;Checkpoint("sides");Part.Sides=Mathf.Clamp(Mathf.RoundToInt(value),3,128);RebuildShape();}}
    private float Coordinate(int axis,bool rotation)=>Part!=null?(rotation?Part.Rotation:Part.Position)[axis]*(rotation?1:1000):Selected?(rotation?Selected!.transform.localEulerAngles[axis]:Selected!.transform.localPosition[axis]*1000):0;
    private void SetCoordinate(int axis,float value,bool rotation){if(refreshing||!Selected)return;Checkpoint("transform:"+axis+rotation);if(Part!=null){(rotation?Part.Rotation:Part.Position)[axis]=value*(rotation?1:.001f);RebuildShape();return;}var transform=Selected!.transform;var v=rotation?transform.localEulerAngles:transform.localPosition;v[axis]=value*(rotation?1:.001f);if(rotation)transform.localEulerAngles=v;else transform.localPosition=v;StoreTransform();}
    private void StoreTransform(){var renderer=Selected;if(!renderer)return;var hand=preview!.transform.Find("LeftSaber");string path=WorkshopState.PathOf(renderer!.transform,hand);var e=new PartEdit{Position=WorkshopState.A3(renderer.transform.localPosition),Rotation=WorkshopState.A3(renderer.transform.localEulerAngles),Scale=WorkshopState.A3(renderer.transform.localScale),Visible=renderer.gameObject.activeSelf};state.Project.Transforms["LeftSaber/"+path]=e;if(state.Project.LinkHands){state.Project.Transforms["RightSaber/"+path]=e;var otherHand=preview.transform.Find("RightSaber");var other=otherHand.GetComponentsInChildren<Renderer>(true).FirstOrDefault(r=>WorkshopState.PathOf(r.transform,otherHand)==path);if(other){other!.transform.localPosition=renderer.transform.localPosition;other.transform.localEulerAngles=renderer.transform.localEulerAngles;other.transform.localScale=renderer.transform.localScale;other.gameObject.SetActive(e.Visible);}}}
    public float PositionX {get=>Coordinate(0,false);set=>SetCoordinate(0,value,false);}public float PositionY {get=>Coordinate(1,false);set=>SetCoordinate(1,value,false);}public float PositionZ {get=>Coordinate(2,false);set=>SetCoordinate(2,value,false);}
    public float RotationX {get=>Coordinate(0,true);set=>SetCoordinate(0,value,true);}public float RotationY {get=>Coordinate(1,true);set=>SetCoordinate(1,value,true);}public float RotationZ {get=>Coordinate(2,true);set=>SetCoordinate(2,value,true);}
    public bool PartVisible {get=>Selected&&Selected!.gameObject.activeSelf;set{if(refreshing||!Selected)return;Selected!.gameObject.SetActive(value);if(Part==null)StoreTransform();else {Part.Visible=value;var partner=preview!.transform.Find("RightSaber").GetComponentsInChildren<PartIdentity>(true).FirstOrDefault(x=>x.Id==Part.Id);if(partner)partner!.gameObject.SetActive(value);}}}
}
