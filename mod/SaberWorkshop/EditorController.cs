using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Components;
using BeatSaberMarkupLanguage.Components.Settings;
using BeatSaberMarkupLanguage.Settings;
using UnityEngine;
using UnityEngine.Rendering;
using Zenject;
using Object=UnityEngine.Object;

namespace SaberWorkshop;

public sealed class EditorController : NotifiableBase,IInitializable,IDisposable
{
    private readonly BSMLSettings settings;
    private readonly WorkshopState state=WorkshopState.Instance;
    private GameObject? preview;
    private List<Renderer> renderers=new List<Renderer>();
    private List<string> sources=new List<string>(),projects=new List<string>(),textures=new List<string>(),properties=new List<string>(),textureSlots=new List<string>();
    private int partIndex,materialIndex,propertyIndex,slotIndex,sectionIndex;
    private bool refreshing,advanced,visible=true,busy;
    private float previewScale=.65f;
    private Color left=new Color(.5f,0,1),right=new Color(.5f,0,1);
    [UIComponent("source-list")] private DropDownListSetting sourceList=null!;
    [UIComponent("project-list")] private DropDownListSetting projectList=null!;
    [UIComponent("part-list")] private DropDownListSetting partList=null!;
    [UIComponent("material-part-list")] private DropDownListSetting materialPartList=null!;
    [UIComponent("texture-part-list")] private DropDownListSetting texturePartList=null!;
    [UIComponent("material-list")] private DropDownListSetting materialList=null!;
    [UIComponent("texture-material-list")] private DropDownListSetting textureMaterialList=null!;
    [UIComponent("shader-list")] private DropDownListSetting shaderList=null!;
    [UIComponent("property-list")] private DropDownListSetting propertyList=null!;
    [UIComponent("texture-slot-list")] private DropDownListSetting textureSlotList=null!;
    [UIComponent("texture-file-list")] private DropDownListSetting textureFileList=null!;
    [UIComponent("section-list")] private DropDownListSetting sectionList=null!;
    public EditorController(BSMLSettings settings){this.settings=settings;}
    public void Initialize(){settings.AddSettingsMenu("Saber Workshop","SaberWorkshop.Editor.bsml",this);RefreshFiles();}
    public void Dispose(){settings.RemoveSettingsMenu(this);if(preview)Object.Destroy(preview);}
    [UIAction("#post-parse")] private void Parsed(){RefreshFiles();RefreshSelection();}

    public string Status {get;private set;}="Создайте меч или откройте .saber. Все изменения видны в игровом рендере.";
    public string ProjectName {get=>state.Project.Name;set{state.Project.Name=(value??"Новый меч").Substring(0,Math.Min(120,(value??"Новый меч").Length));NotifyPropertyChanged();}}
    public bool PlayEnabled {get=>state.PlayEnabled;set{if(value&&!state.Ready){SetStatus("Сначала создайте или откройте меч.");return;}state.PlayEnabled=value;NotifyPropertyChanged();SetStatus(value?"Ваш меч будет использован на следующей карте.":"Игровая замена выключена.");}}
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
    public bool IsProfile=>Part!=null;
    public string MaterialInfo=>Material?Material!.name+"\n"+Material.shader.name:"Выберите деталь и слой.";
    public string PropertyHelp {get{if(!Material||Property=="")return "Выберите свойство.";int index=Material!.shader.FindPropertyIndex(Property);var type=Material.shader.GetPropertyType(index);return Property+(type==ShaderPropertyType.Range?" · диапазон "+Material.shader.GetPropertyRangeLimits(index):"");}}
    public string TextureInfo {get{if(!Material||TextureSlot=="")return "У выбранного шейдера нет текстурных слотов.";var texture=Material!.GetTexture(TextureSlot);return TextureSlot+" · "+(texture?texture!.name+" · "+texture.width+" × "+texture.height:"не задана");}}

    private void SetStatus(string text){Status=text;NotifyPropertyChanged(nameof(Status));}
    private void Guard(Action action){try{action();}catch(Exception e){Plugin.Log.Error(e.ToString());SetStatus(e.Message);}}
    private async Task Run(Func<Task> action){if(busy)return;busy=true;try{await action();}catch(Exception e){Plugin.Log.Error(e.ToString());SetStatus(e.Message);}finally{busy=false;}}
    [UIAction("RefreshFiles")] public void RefreshFiles(){Guard(()=>{
        WorkshopState.EnsureFolders();sources=WorkshopState.SaberFiles().ToList();projects=Directory.GetFiles(WorkshopState.ProjectFolder,"*.workshop.json").OrderBy(x=>x).ToList();textures=Directory.GetFiles(WorkshopState.TextureFolder).Where(x=>new[]{".png",".jpg",".jpeg"}.Contains(Path.GetExtension(x).ToLowerInvariant())).OrderBy(x=>x).ToList();
        SourceOptions=sources.Select(x=>(object)x.Substring(Path.Combine(WorkshopState.GameRoot,"CustomSabers").Length+1)).ToList();ProjectOptions=projects.Select(x=>(object)Path.GetFileName(x)).ToList();TextureFileOptions=textures.Select(x=>(object)Path.GetFileName(x)).ToList();
        if(!SourceOptions.Contains(SourceChoice))SourceChoice=Choice(SourceOptions,0);if(!ProjectOptions.Contains(ProjectChoice))ProjectChoice=Choice(ProjectOptions,0);if(!TextureFileOptions.Contains(TextureFileChoice))TextureFileChoice=Choice(TextureFileOptions,0);
        UpdateList(sourceList,SourceOptions,SourceChoice);UpdateList(projectList,ProjectOptions,ProjectChoice);UpdateList(textureFileList,TextureFileOptions,TextureFileChoice);
    });}
    [UIAction("OpenSource")] public async void OpenSource(){await Run(async()=>{if(SourceChoice=="")throw new InvalidOperationException("Положите .saber в CustomSabers и обновите файлы.");SetStatus("Загрузка Unity AssetBundle…");await state.OpenSaber(SourceChoice);BuildPreview();SetStatus("Открыт "+state.Project.Name+". Геометрия, текстуры и анимации загружены Unity.");});}
    [UIAction("OpenProject")] public async void OpenProject(){await Run(async()=>{if(ProjectChoice=="")throw new InvalidOperationException("Нет сохранённых проектов.");await state.LoadProject(ProjectChoice);BuildPreview();SetStatus("Проект открыт.");});}
    [UIAction("New")] public void New(){Guard(()=>{state.CreateStarter();partIndex=0;BuildPreview();SetStatus("Новый Flux. Выбирайте детали, форму и материалы в разделах ниже.");});}
    [UIAction("Save")] public void Save(){Guard(()=>{var path=state.Save();RefreshFiles();SetStatus("Сохранено: UserData/SaberWorkshop/Projects/"+Path.GetFileName(path));});}
    [UIAction("ShowPreview")] public void ShowPreview(){Guard(()=>{if(!state.Ready)throw new InvalidOperationException("Сначала создайте или откройте меч.");if(!preview)BuildPreview();visible=true;PlacePreview();preview!.SetActive(true);NotifyPropertyChanged(nameof(PreviewVisible));});}
    private void PlacePreview(){var camera=Camera.main??Resources.FindObjectsOfTypeAll<Camera>().FirstOrDefault(c=>c.isActiveAndEnabled&&c.stereoEnabled);if(!camera||!preview)return;var yaw=Quaternion.Euler(0,camera.transform.eulerAngles.y,0);preview!.transform.position=camera.transform.position+yaw*new Vector3(.8f,-.48f,1.6f);preview.transform.rotation=yaw;preview.transform.localScale=Vector3.one*previewScale;}
    private void BuildPreview(){if(preview)Object.Destroy(preview);preview=state.InstantiatePair();preview.SetActive(visible);PlacePreview();RefreshSelection();ApplyColors();NotifyPropertyChanged(nameof(ProjectName));NotifyPropertyChanged(nameof(PlayEnabled));}
    public bool PreviewVisible {get=>visible;set{visible=value;if(preview)preview!.SetActive(value);NotifyPropertyChanged();}}
    public float PreviewScale {get=>previewScale;set{previewScale=Mathf.Clamp(value,.2f,2);if(preview)preview!.transform.localScale=Vector3.one*previewScale;}}
    public Color LeftColor {get=>left;set{left=value;ApplyColors();}}
    public Color RightColor {get=>right;set{right=value;ApplyColors();}}
    private void ApplyColors(){if(!preview)return;var l=preview!.transform.Find("LeftSaber");var r=preview.transform.Find("RightSaber");if(l)WorkshopState.ApplyPlayerColor(l.gameObject,left);if(r)WorkshopState.ApplyPlayerColor(r.gameObject,right);}
    private void RefreshSelection(){
        if(refreshing)return;refreshing=true;try{
            if(preview){var hand=preview!.transform.Find("LeftSaber");renderers=hand?hand.GetComponentsInChildren<Renderer>(true).ToList():new List<Renderer>();}else renderers.Clear();
            PartOptions=renderers.Select((r,i)=>(object)$"{i+1}. {r.name}").ToList();partIndex=Mathf.Clamp(partIndex,0,Math.Max(0,renderers.Count-1));
            MaterialOptions=Selected?Selected!.sharedMaterials.Select((m,i)=>(object)$"{i+1}. {(m?m.name:"Пустой слой")}").ToList():new List<object>();materialIndex=Mathf.Clamp(materialIndex,0,Math.Max(0,MaterialOptions.Count-1));
            properties.Clear();textureSlots.Clear();PropertyOptions=new List<object>();TextureSlotOptions=new List<object>();
            if(Material){var shader=Material!.shader;for(int i=0;i<shader.GetPropertyCount();i++){string key=shader.GetPropertyName(i),label=Label(key,shader.GetPropertyDescription(i));var type=shader.GetPropertyType(i);if(type==ShaderPropertyType.Texture){textureSlots.Add(key);TextureSlotOptions.Add(label+" · "+key);}else if(advanced||Common(key)){properties.Add(key);PropertyOptions.Add(label+(advanced?" · "+key:""));}}}
            propertyIndex=Mathf.Clamp(propertyIndex,0,Math.Max(0,properties.Count-1));slotIndex=Mathf.Clamp(slotIndex,0,Math.Max(0,textureSlots.Count-1));
            ShaderOptions=state.Shaders().Select(s=>(object)s.name).ToList();ShaderChoice=Material?Material!.shader.name:Choice(ShaderOptions,0);
            SectionOptions=Part!=null?Part.Sections.Select((s,i)=>(object)$"{i+1} · {Math.Round(s.T*100)}% длины").ToList():new List<object>();sectionIndex=Mathf.Clamp(sectionIndex,0,Math.Max(0,SectionOptions.Count-1));
            foreach(var list in new[]{partList,materialPartList,texturePartList})UpdateList(list,PartOptions,PartChoice);
            foreach(var list in new[]{materialList,textureMaterialList})UpdateList(list,MaterialOptions,MaterialChoice);
            UpdateList(shaderList,ShaderOptions,ShaderChoice);UpdateList(propertyList,PropertyOptions,PropertyChoice);UpdateList(textureSlotList,TextureSlotOptions,TextureSlotChoice);UpdateList(sectionList,SectionOptions,SectionChoice);
        }finally{refreshing=false;}NotifyValues();
    }
    private void UpdateList(DropDownListSetting list,List<object> options,string value){if(!list)return;bool old=refreshing;refreshing=true;try{list.Values=options;list.UpdateChoices();if(options.Count>0)list.Value=value;}finally{refreshing=old;}}
    private void NotifyValues(){foreach(string name in new[]{nameof(IsNumber),nameof(IsColor),nameof(IsVector),nameof(IsProfile),nameof(MaterialInfo),nameof(PropertyHelp),nameof(TextureInfo),nameof(NumberValue),nameof(ColorValue),nameof(ColorIntensity),nameof(AlphaValue),nameof(VectorX),nameof(VectorY),nameof(VectorZ),nameof(VectorW),nameof(TileX),nameof(TileY),nameof(OffsetX),nameof(OffsetY),nameof(PartLength),nameof(SectionRadius),nameof(SectionPosition),nameof(SectionX),nameof(SectionY),nameof(Sides),nameof(PositionX),nameof(PositionY),nameof(PositionZ),nameof(RotationX),nameof(RotationY),nameof(RotationZ),nameof(PartVisible)})NotifyPropertyChanged(name);}
    private static bool Common(string key)=>new[]{"_Color","_TintColor","_StarNestMainColor","_Glow","_BSGlow","_ALTGlow","_Bloom","_CustomColors","_Opacity","_Intensity","_Brightness","_Metallic","_Glossiness"}.Contains(key);
    private static string Label(string key,string fallback){return new Dictionary<string,string>{{"_Color","Цвет"},{"_TintColor","Оттенок"},{"_StarNestMainColor","Цвет внутренней плазмы"},{"_Glow","Свечение"},{"_BSGlow","Свечение"},{"_CustomColors","Цвет игрока: 0 выкл., 1 вкл."},{"_Opacity","Непрозрачность"},{"_Intensity","Яркость"},{"_Brightness","Яркость текстуры"},{"_AlphaTexture","Узор / маска"},{"_AlphaTex","Узор / маска"},{"_MainTex","Основная картинка"},{"_Displacement","Искажение / огонь"},{"_DisplaceTex","Искажение / огонь"},{"_FadeMask","Маска затухания"},{"_MaskTex","Маска"},{"_textureScroll","Скорость узора X / Y"},{"_fademaskScroll","Скорость маски X / Y"},{"_Metallic","Металличность"},{"_Glossiness","Гладкость"},{"_Markiplier","Резкость контура"}}.TryGetValue(key,out var label)?label:string.IsNullOrEmpty(fallback)?key:fallback;}
    private SurfaceEdit? Edit {get{if(!Material||!Selected)return null;if(Part!=null)return Part.Surface;var hand=preview!.transform.Find("LeftSaber");var key="LeftSaber/"+WorkshopState.PathOf(Selected!.transform,hand)+"#"+materialIndex;if(!state.Project.Surfaces.TryGetValue(key,out var edit)){edit=new SurfaceEdit();state.Project.Surfaces[key]=edit;}return edit;}}
    private void ChangedMaterial(){if(!Material||Edit==null)return;Guard(()=>{state.ApplyMaterial(Material!,Edit!);if(state.Project.LinkHands&&Part==null){var hand=preview!.transform.Find("LeftSaber");var path=WorkshopState.PathOf(Selected!.transform,hand);state.Project.Surfaces["RightSaber/"+path+"#"+materialIndex]=Edit!;var rightHand=preview.transform.Find("RightSaber");var partner=rightHand.GetComponentsInChildren<Renderer>(true).FirstOrDefault(r=>WorkshopState.PathOf(r.transform,rightHand)==path);if(partner&&partner!.sharedMaterials.Length>materialIndex)state.ApplyMaterial(partner.sharedMaterials[materialIndex],Edit!);}else if(Part!=null){var partner=preview!.transform.Find("RightSaber").GetComponentsInChildren<PartIdentity>(true).FirstOrDefault(x=>x.Id==Part.Id);if(partner){var renderer=partner!.GetComponent<Renderer>();if(renderer)state.ApplyMaterial(renderer.sharedMaterial,Edit!);}}NotifyValues();});}
    [UIAction("ApplyShader")] public void ApplyShader(){if(Edit==null)return;Edit!.Shader=ShaderChoice;ChangedMaterial();RefreshSelection();}
    public float NumberValue {get=>Material&&IsNumber?Material!.GetFloat(Property):0;set{if(refreshing||!IsNumber||Edit==null||float.IsNaN(value)||float.IsInfinity(value))return;Edit!.Numbers[Property]=Mathf.Clamp(value,-100000,100000);ChangedMaterial();}}
    private Color RawColor=>Material&&IsColor?Material!.GetColor(Property):Color.white;
    public Color ColorValue {get{var c=RawColor;float max=Mathf.Max(1,Mathf.Max(c.r,Mathf.Max(c.g,c.b)));return new Color(c.r/max,c.g/max,c.b/max,c.a);}set{if(refreshing||!IsColor||Edit==null)return;float intensity=ColorIntensity;Edit!.Colors[Property]=new[]{value.r*intensity,value.g*intensity,value.b*intensity,RawColor.a};ChangedMaterial();}}
    public float ColorIntensity {get{var c=RawColor;return Mathf.Max(1,Mathf.Max(c.r,Mathf.Max(c.g,c.b)));}set{if(refreshing||!IsColor||Edit==null)return;var c=ColorValue;Edit!.Colors[Property]=new[]{c.r*value,c.g*value,c.b*value,c.a};ChangedMaterial();}}
    public float AlphaValue {get=>RawColor.a;set{if(refreshing||!IsColor||Edit==null)return;var c=RawColor;Edit!.Colors[Property]=new[]{c.r,c.g,c.b,Mathf.Clamp01(value)};ChangedMaterial();}}
    private float Vector(int axis)=>Material&&IsVector?Material!.GetVector(Property)[axis]:0;
    private void SetVector(int axis,float value){if(refreshing||!IsVector||Edit==null)return;var v=Material!.GetVector(Property);v[axis]=value;Edit!.Vectors[Property]=new[]{v.x,v.y,v.z,v.w};ChangedMaterial();}
    public float VectorX {get=>Vector(0);set=>SetVector(0,value);}public float VectorY {get=>Vector(1);set=>SetVector(1,value);}public float VectorZ {get=>Vector(2);set=>SetVector(2,value);}public float VectorW {get=>Vector(3);set=>SetVector(3,value);}
    private TextureEdit? TexEdit {get{if(Edit==null||TextureSlot=="")return null;if(!Edit!.Textures.TryGetValue(TextureSlot,out var t)){var scale=Material!.GetTextureScale(TextureSlot);var offset=Material.GetTextureOffset(TextureSlot);t=new TextureEdit{Scale=new[]{scale.x,scale.y},Offset=new[]{offset.x,offset.y}};Edit.Textures[TextureSlot]=t;}return t;}}
    [UIAction("ApplyTexture")] public void ApplyTexture(){Guard(()=>{if(TexEdit==null)throw new InvalidOperationException("Выберите текстурный слот.");var file=WorkshopState.SafePath(WorkshopState.TextureFolder,TextureFileChoice);if(!File.Exists(file)||new FileInfo(file).Length>12*1024*1024)throw new InvalidDataException("Картинка не найдена или больше 12 МБ");TexEdit!.Png=Convert.ToBase64String(File.ReadAllBytes(file));ChangedMaterial();SetStatus("Картинка применена к слою и будет встроена в проект.");});}
    [UIAction("GeneratePlasma")] public void GeneratePlasma(){Guard(()=>{
        WorkshopState.EnsureFolders();string name="Plasma-"+DateTime.UtcNow.ToString("yyyyMMdd-HHmmss")+".png";string path=WorkshopState.SafePath(WorkshopState.TextureFolder,name);
        var texture=new Texture2D(256,512,TextureFormat.RGBA32,false);var pixels=new Color32[256*512];
        for(int y=0;y<512;y++)for(int x=0;x<256;x++){
            float u=(float)x/256*Mathf.PI*2,v=(float)y/512*Mathf.PI*2;
            float flow=Mathf.Sin(u*7+Mathf.Sin(v*3)*1.7f+Mathf.Cos(u*3-v*2)*.8f);
            float vein=Mathf.Pow(1-Mathf.Abs(flow),7),cloud=(Mathf.Sin(u*3+v*2)*Mathf.Cos(u*5-v*7)+1)*.5f;
            byte intensity=(byte)Mathf.RoundToInt(Mathf.Clamp01(.025f+.16f*cloud+.9f*vein)*255);pixels[y*256+x]=new Color32(intensity,intensity,intensity,255);
        }
        texture.SetPixels32(pixels);texture.Apply();try{File.WriteAllBytes(path,texture.EncodeToPNG());}finally{Object.Destroy(texture);}RefreshFiles();TextureFileChoice=name;UpdateList(textureFileList,TextureFileOptions,name);SetStatus("Плазменный узор готов. Выберите слот внутреннего слоя и нажмите «Применить картинку».");
    });}
    public float TileX {get=>Material&&TextureSlot!=""?Material!.GetTextureScale(TextureSlot).x:1;set{if(TexEdit==null)return;TexEdit!.Scale[0]=value;ChangedMaterial();}}
    public float TileY {get=>Material&&TextureSlot!=""?Material!.GetTextureScale(TextureSlot).y:1;set{if(TexEdit==null)return;TexEdit!.Scale[1]=value;ChangedMaterial();}}
    public float OffsetX {get=>Material&&TextureSlot!=""?Material!.GetTextureOffset(TextureSlot).x:0;set{if(TexEdit==null)return;TexEdit!.Offset[0]=value;ChangedMaterial();}}
    public float OffsetY {get=>Material&&TextureSlot!=""?Material!.GetTextureOffset(TextureSlot).y:0;set{if(TexEdit==null)return;TexEdit!.Offset[1]=value;ChangedMaterial();}}

    [UIAction("AddProfile")] public void AddProfile(){Guard(()=>{if(state.Project.Parts.Count>=400)throw new InvalidOperationException("Максимум 400 созданных деталей.");state.AddPart("Профиль "+(state.Project.Parts.Count+1),true);BuildPreview();partIndex=renderers.Count-1;RefreshSelection();});}
    [UIAction("AddRing")] public void AddRing(){Guard(()=>{var p=state.AddPart("Кольцо",true);p.Shape="ring";p.Sections[0].Radius=.045f;p.Sections[1].Radius=.002f;p.Position[2]=.04f;BuildPreview();partIndex=renderers.Count-1;RefreshSelection();});}
    [UIAction("AddInner")] public void AddInner(){Guard(()=>{
        if(!Selected)throw new InvalidOperationException("Выберите деталь для внутреннего слоя.");var original=Part;
        if(original==null)throw new InvalidOperationException("Для исходного .saber выберите уже существующий внутренний слой в «Материалы». Новые внутренние профили создаются для ваших деталей.");
        var copy=Newtonsoft.Json.JsonConvert.DeserializeObject<ProfilePart>(Newtonsoft.Json.JsonConvert.SerializeObject(original))!;copy.Id=Guid.NewGuid().ToString("N");copy.Name=original.Name+" · плазма";foreach(var s in copy.Sections)s.Radius*=.85f;state.Project.Parts.Add(copy);BuildPreview();partIndex=renderers.Count-1;RefreshSelection();SetStatus("Добавлен внутренний слой. Выберите его шейдер и картинку в разделах материалов и текстур.");
    });}
    [UIAction("DeletePart")] public void DeletePart(){if(Part==null)return;var id=Part.Id;state.Project.Parts.RemoveAll(p=>p.Id==id);BuildPreview();}
    private Section? Section=>Part?.Sections[Mathf.Clamp(sectionIndex,0,Part.Sections.Count-1)];
    [UIAction("AddSection")] public void AddSection(){if(Part==null||Part.Shape=="ring"||Part.Sections.Count>=128)return;var list=Part.Sections;int index=0;float gap=-1;for(int i=0;i<list.Count-1;i++)if(list[i+1].T-list[i].T>gap){gap=list[i+1].T-list[i].T;index=i;}var a=list[index];var b=list[index+1];list.Insert(index+1,new Section{T=(a.T+b.T)/2,Radius=(a.Radius+b.Radius)/2,X=(a.X+b.X)/2,Y=(a.Y+b.Y)/2});sectionIndex=index+1;RebuildShape();}
    [UIAction("DeleteSection")] public void DeleteSection(){if(Part==null||Part.Shape=="ring"||Part.Sections.Count<=2)return;Part.Sections.RemoveAt(sectionIndex);sectionIndex=Math.Min(sectionIndex,Part.Sections.Count-1);RebuildShape();}
    private void RebuildShape(){if(refreshing)return;Guard(()=>{BuildPreview();});}
    public float PartLength {get=>Part?.Length*1000??0;set{if(Part==null||refreshing)return;Part.Length=Mathf.Clamp(value/1000,.001f,2);RebuildShape();}}
    public float SectionPosition {get=>(Section?.T??0)*100;set{if(Section==null||Part==null||Part.Shape=="ring"||refreshing)return;Section.T=Mathf.Clamp(value/100,sectionIndex>0?Part.Sections[sectionIndex-1].T:0,sectionIndex+1<Part.Sections.Count?Part.Sections[sectionIndex+1].T:1);RebuildShape();}}
    public float SectionRadius {get=>(Section?.Radius??0)*1000;set{if(Section==null||refreshing)return;Section.Radius=Mathf.Clamp(value/1000,0,.3f);RebuildShape();}}
    public float SectionX {get=>(Section?.X??0)*1000;set{if(Section==null||refreshing)return;Section.X=Mathf.Clamp(value/1000,-.3f,.3f);RebuildShape();}}
    public float SectionY {get=>(Section?.Y??0)*1000;set{if(Section==null||refreshing)return;Section.Y=Mathf.Clamp(value/1000,-.3f,.3f);RebuildShape();}}
    public float Sides {get=>Part?.Sides??32;set{if(Part==null||refreshing)return;Part.Sides=Mathf.Clamp(Mathf.RoundToInt(value),3,128);RebuildShape();}}
    private float Coordinate(int axis,bool rotation)=>Part!=null?(rotation?Part.Rotation:Part.Position)[axis]*(rotation?1:1000):Selected?(rotation?Selected!.transform.localEulerAngles[axis]:Selected!.transform.localPosition[axis]*1000):0;
    private void SetCoordinate(int axis,float value,bool rotation){if(refreshing||!Selected)return;if(Part!=null){(rotation?Part.Rotation:Part.Position)[axis]=value*(rotation?1:.001f);RebuildShape();return;}var transform=Selected!.transform;var v=rotation?transform.localEulerAngles:transform.localPosition;v[axis]=value*(rotation?1:.001f);if(rotation)transform.localEulerAngles=v;else transform.localPosition=v;StoreTransform();}
    private void StoreTransform(){var renderer=Selected;if(!renderer)return;var hand=preview!.transform.Find("LeftSaber");string path=WorkshopState.PathOf(renderer!.transform,hand);var e=new PartEdit{Position=WorkshopState.A3(renderer.transform.localPosition),Rotation=WorkshopState.A3(renderer.transform.localEulerAngles),Scale=WorkshopState.A3(renderer.transform.localScale),Visible=renderer.gameObject.activeSelf};state.Project.Transforms["LeftSaber/"+path]=e;if(state.Project.LinkHands){state.Project.Transforms["RightSaber/"+path]=e;var otherHand=preview.transform.Find("RightSaber");var other=otherHand.GetComponentsInChildren<Renderer>(true).FirstOrDefault(r=>WorkshopState.PathOf(r.transform,otherHand)==path);if(other){other!.transform.localPosition=renderer.transform.localPosition;other.transform.localEulerAngles=renderer.transform.localEulerAngles;other.transform.localScale=renderer.transform.localScale;other.gameObject.SetActive(e.Visible);}}}
    public float PositionX {get=>Coordinate(0,false);set=>SetCoordinate(0,value,false);}public float PositionY {get=>Coordinate(1,false);set=>SetCoordinate(1,value,false);}public float PositionZ {get=>Coordinate(2,false);set=>SetCoordinate(2,value,false);}
    public float RotationX {get=>Coordinate(0,true);set=>SetCoordinate(0,value,true);}public float RotationY {get=>Coordinate(1,true);set=>SetCoordinate(1,value,true);}public float RotationZ {get=>Coordinate(2,true);set=>SetCoordinate(2,value,true);}
    public bool PartVisible {get=>Selected&&Selected!.gameObject.activeSelf;set{if(refreshing||!Selected)return;Selected!.gameObject.SetActive(value);if(Part==null)StoreTransform();else {Part.Visible=value;var partner=preview!.transform.Find("RightSaber").GetComponentsInChildren<PartIdentity>(true).FirstOrDefault(x=>x.Id==Part.Id);if(partner)partner!.gameObject.SetActive(value);}}}
}
