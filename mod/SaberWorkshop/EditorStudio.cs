using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using BeatSaberMarkupLanguage.Attributes;
using Newtonsoft.Json;
using UnityEngine;
using HMUI;
namespace SaberWorkshop;
public sealed partial class EditorController {
 private string inspector="shape";
 [UIComponent("preset-dialog")] private ModalView presetDialog=null!;
 [UIComponent("effect-list")] private BeatSaberMarkupLanguage.Components.Settings.DropDownListSetting effectList=null!;
 public bool ShapeInspector=>inspector=="shape";public bool ColorInspector=>inspector=="color";public bool TextureInspector=>inspector=="texture";public bool ShaderInspector=>inspector=="shader";public bool IsParticle=>Part?.Particles!=null;public bool IsTrail=>Part?.Trail!=null;public bool IsSolidPart=>Part!=null&&!IsParticle&&!IsTrail;
 public bool SaberPreviewArea=>!ShaderInspector;
 public string PreviewTitle=>ShaderInspector?"MATERIAL PREVIEW":"SABER PREVIEW";
 public bool HasSelection=>Selected;public bool NoSelection=>!Selected;
 private bool showTransform,showShapeDetails;
 public bool ShowTransform {get=>showTransform;set{showTransform=value;NotifyPropertyChanged();}}
 public bool ShowShapeDetails {get=>showShapeDetails;set{showShapeDetails=value;NotifyPropertyChanged();}}
 private bool showShapePoints;public bool ShowShapePoints {get=>showShapePoints;set{showShapePoints=value;NotifyPropertyChanged();}}
 public List<object> ColorSources {get;}=new List<object>{"RGB","Note colour"};
 public List<object> CrossSections {get;}=new List<object>{"Round","Flat","Square","Triangle"};
 public string CrossSection {get=>Part==null?"Round":Part.Sides==3?"Triangle":Part.Sides==4?"Square":Part.Sections.All(s=>s.Aspect<.5f)?"Flat":"Round";set{if(refreshing||!IsProfile)return;Checkpoint("cross-section");Part!.Sides=value=="Triangle"?3:value=="Square"?4:32;foreach(var s in Part.Sections)s.Aspect=value=="Flat"?.25f:1;RebuildShape();}}
 public string ShapeTabLabel=>TabLabel("shape","Part");public string ColorTabLabel=>TabLabel("color","Material");public string TextureTabLabel=>TabLabel("texture","Images");public string ShaderTabLabel=>TabLabel("shader","Effects");
 private string TabLabel(string key,string label)=>inspector==key?"<color=#62DCEB>"+label+"</color>":label;
 public string ColorSource {get=>PlayerColor?"Note colour":"RGB";set{PlayerColor=value=="Note colour";NotifyPropertyChanged();}}
 [UIAction("InspectShape")] public void InspectShape()=>SetInspector("shape");[UIAction("InspectColor")] public void InspectColor()=>SetInspector("color");[UIAction("InspectTexture")] public void InspectTexture()=>SetInspector("texture");
 private void SetInspector(string value){inspector=value;if(preview)preview!.SetActive(visible&&editorOpen&&!ShaderInspector);NotifyPropertyChanged(nameof(SaberPreviewArea));NotifyPropertyChanged(nameof(PreviewTitle));foreach(string s in new[]{nameof(ShapeInspector),nameof(ColorInspector),nameof(TextureInspector),nameof(ShaderInspector),nameof(ShapeTabLabel),nameof(ColorTabLabel),nameof(TextureTabLabel),nameof(ShaderTabLabel)})NotifyPropertyChanged(s);}
 [UIAction("NewPresetDialog")] public void NewPresetDialog(){if(!presetDialog)return;if(preview)preview!.SetActive(false);presetDialog.Show(true,false);}
 [UIAction("ClosePresetDialog")] public void ClosePresetDialog(){if(presetDialog)presetDialog.Hide(true);if(preview)preview!.SetActive(visible&&editorOpen&&!ShaderInspector);}
 [UIAction("ShaderWorkshop")] public void ShaderWorkshop(){if(!Selected){SetStatus("Select a part first.");return;}RefreshEffects();EnsureEffectDraft();RefreshEffectTargets();SetInspector("shader");}
 [UIAction("CloseShaderWorkshop")] public void CloseShaderWorkshop(){SetInspector("color");}
 private float Dimension(int axis){var f=Selected?Selected!.GetComponent<MeshFilter>():null;return f&&f!.sharedMesh?f.sharedMesh.bounds.size[axis]*1000:0;}
 private void Resize(int axis,float mm){if(refreshing||Part==null||IsParticle)return;float size=Dimension(axis);if(size<.001f)return;Checkpoint("dimension"+axis);float factor=Mathf.Clamp(mm,.05f,3000)/size;if(axis==0)Part.WidthScale=Mathf.Clamp(Part.WidthScale*factor,.01f,100);else if(axis==1)Part.DepthScale=Mathf.Clamp(Part.DepthScale*factor,.01f,100);else Part.HeightScale=Mathf.Clamp(Part.HeightScale*factor,.01f,100);RebuildShape();}
 public float PartWidth {get=>Dimension(0);set=>Resize(0,value);}public float PartDepth {get=>Dimension(1);set=>Resize(1,value);}public float PartHeight {get=>Dimension(2);set=>Resize(2,value);}
 private string DimensionText(int axis)=>Dimension(axis).ToString("0.###",System.Globalization.CultureInfo.InvariantCulture);
 private void SetDimensionText(int axis,string value){if(refreshing)return;if(float.TryParse((value??"").Replace(',','.'),System.Globalization.NumberStyles.Float,System.Globalization.CultureInfo.InvariantCulture,out float mm)&&!float.IsNaN(mm)&&!float.IsInfinity(mm)&&mm>0)Resize(axis,mm);else SetStatus("Enter a positive size in millimetres.");}
 public string PartWidthText {get=>DimensionText(0);set=>SetDimensionText(0,value);}public string PartDepthText {get=>DimensionText(1);set=>SetDimensionText(1,value);}public string PartHeightText {get=>DimensionText(2);set=>SetDimensionText(2,value);}
 public float PointWidth {get=>(Section?.Radius??0)*2000*(Part?.WidthScale??1);set{if(refreshing||Section==null||Part==null)return;Checkpoint("point-width");Section.Radius=Mathf.Clamp(value,.05f,600)/(2000*Part.WidthScale);RebuildShape();}}
 public float PointDepth {get=>(Section?.Radius??0)*(Section?.Aspect??1)*2000*(Part?.DepthScale??1);set{if(refreshing||Section==null||Part==null||Section.Radius<=0)return;Checkpoint("point-depth");Section.Aspect=Mathf.Clamp(value/(Section.Radius*2000*Part.DepthScale),.01f,100);RebuildShape();}}
 [UIAction("OutlinePair")] public void OutlinePair(){Guard(()=>{if(Part==null||IsParticle)throw new InvalidOperationException("Select a solid part first.");Checkpoint("outline-pair");if(state.Project.Parts.Count>=400)throw new InvalidOperationException("Maximum 400 parts.");var outer=Part;outer.Inverted=true;outer.OutlineThickness=0;var m=state.DefaultMaterial(false);m.color=Color.black;outer.Surface=WorkshopState.CaptureMaterial(m);outer.Surface.UsePlayerColor=false;OwnedAssets.Release(m);var inner=JsonConvert.DeserializeObject<ProfilePart>(JsonConvert.SerializeObject(outer))!;inner.Id=Guid.NewGuid().ToString("N");inner.Name=outer.Name+" inner";inner.WidthScale*=.86f;inner.DepthScale*=.86f;inner.HeightScale*=.99f;var fill=state.DefaultMaterial(true);inner.Surface=WorkshopState.CaptureMaterial(fill);inner.Surface.UsePlayerColor=true;OwnedAssets.Release(fill);state.Project.Parts.Add(inner);BuildPreview();partIndex=renderers.Count-1;RefreshSelection();SetStatus("Outline pair created. Copy stays in place; width, depth and height are independent.");});}
 public string EffectName {get=>effectDraft?effectDraft!.name:"My effect";set{if(refreshing||!effectDraft)return;effectDraft!.name=value;DraftChanged();}}
 public bool HasEffect=>effectDraft&&effectDraft!.shader.name=="Workshop/Effect";
 private float EffectNumber(string key,float fallback)=>HasEffect?effectDraft!.GetFloat(key):fallback;
 private void SetEffectNumber(string key,float value){if(refreshing||!HasEffect)return;effectDraft!.SetFloat(key,value);DraftChanged();}
 public float EffectScale {get=>EffectNumber("_Scale",4);set=>SetEffectNumber("_Scale",Mathf.Clamp(value,.1f,30));}
 public float EffectSpeed {get=>EffectNumber("_Speed",.3f);set=>SetEffectNumber("_Speed",Mathf.Clamp(value,0,5));}
 public float EffectDepth {get=>EffectNumber("_Depth",1);set=>SetEffectNumber("_Depth",Mathf.Clamp(value,.05f,3));}
 public float EffectBrightness {get=>EffectNumber("_Brightness",1.5f);set=>SetEffectNumber("_Brightness",Mathf.Clamp(value,0,10));}
 public float EffectGlow {get=>EffectNumber("_Glow",1);set=>SetEffectNumber("_Glow",Mathf.Clamp(value,0,10));}
 public Color EffectColor {get=>HasEffect?effectDraft!.GetColor("_Color"):Color.magenta;set{if(refreshing||!HasEffect)return;effectDraft!.SetColor("_Color",value);DraftChanged();}}
 public Color EffectAccent {get=>HasEffect?effectDraft!.GetColor("_Accent"):Color.white;set{if(refreshing||!HasEffect)return;effectDraft!.SetColor("_Accent",value);DraftChanged();}}
 public List<object> FlowDirections {get;}=new List<object>{"Up","Down","Across"};
 public string FlowDirection {get{if(!HasEffect)return "Up";var v=effectDraft!.GetVector("_Flow");return v.x!=0?"Across":v.z<0?"Down":"Up";}set{if(refreshing||!HasEffect)return;effectDraft!.SetVector("_Flow",value=="Across"?new Vector4(1,0,0,0):new Vector4(0,0,value=="Down"?-1:1,0));DraftChanged();}}
 [UIAction("TextureEffect")] public void TextureEffect()=>CreateEffect(0);[UIAction("PlasmaEffect")] public void PlasmaEffect()=>CreateEffect(1);[UIAction("SpaceEffect")] public void SpaceEffect()=>CreateEffect(2);
 private void CreateEffect(int mode){Guard(()=>{var m=WorkshopAssets.EffectMaterial();m.name=mode==0?"My texture":mode==1?"My plasma":"My space";m.SetFloat("_Mode",mode);SetEffectDraft(m);SetStatus("Editing material preview. Choose a part and press Apply to part.");});}
 private void AssignEffect(SurfaceEdit surface,Material m){if(!Selected){OwnedAssets.Release(m);return;}if(Part!=null)Part.Surface=surface;else{var hand=preview!.transform.Find("LeftSaber");state.Project.Surfaces["LeftSaber/"+WorkshopState.PathOf(Selected!.transform,hand)+"#"+materialIndex]=surface;}var mats=Selected!.sharedMaterials;mats[materialIndex]=m;Selected.sharedMaterials=mats;preview!.GetComponent<OwnedAssets>().Assets.Add(m);ChangedMaterial();RefreshSelection();}
 public List<object> SavedEffects {get;private set;}=new List<object>();public string SavedEffectChoice {get;set;}="";
 private static string EffectFolder=>Path.Combine(WorkshopState.DataRoot,"Shaders");
 private void RefreshEffects(){Directory.CreateDirectory(EffectFolder);SavedEffects=Directory.GetFiles(EffectFolder,"*.effect.json").Select(p=>(object)Path.GetFileName(p).Replace(".effect.json","")).ToList();if(!SavedEffects.Contains(SavedEffectChoice))SavedEffectChoice=Choice(SavedEffects,0);UpdateList(effectList,SavedEffects,SavedEffectChoice);}
 [UIAction("SaveEffect")] public void SaveEffect(){Guard(()=>{if(!HasEffect)throw new InvalidOperationException("Choose a shader example first.");string name=string.Concat(EffectName.Where(c=>!Path.GetInvalidFileNameChars().Contains(c))).Trim();if(name=="")name="My effect";Directory.CreateDirectory(EffectFolder);var file=WorkshopState.SafePath(EffectFolder,name.Substring(0,Math.Min(72,name.Length))+".effect.json");File.WriteAllText(file,JsonConvert.SerializeObject(CaptureDraft(),Formatting.Indented));RefreshEffects();SetStatus("Effect saved: "+name);});}
 [UIAction("LoadEffect")] public void LoadEffect(){Guard(()=>{var path=WorkshopState.SafePath(EffectFolder,SavedEffectChoice+".effect.json");if(new FileInfo(path).Length>24*1024*1024)throw new InvalidDataException("Effect file too large.");var surface=JsonConvert.DeserializeObject<SurfaceEdit>(File.ReadAllText(path),new JsonSerializerSettings{TypeNameHandling=TypeNameHandling.None,MaxDepth=64})!;if(surface.Shader!="Workshop/Effect")throw new InvalidDataException("Choose a Workshop effect file.");var check=new WorkshopProject();check.Surfaces["effect"]=surface;WorkshopState.Validate(check);var m=WorkshopAssets.EffectMaterial();state.ApplyMaterial(m,surface);SetEffectDraft(m,surface.Textures.TryGetValue("_MainTex",out var image)?image:null);SetStatus("Material loaded into preview.");});}
 [UIAction("AddParticles")] public void AddParticles(){Guard(()=>{Checkpoint("particles");var p=state.AddPart("Spark emitter",true);p.Shape="particles";p.Particles=new ParticleEdit();var m=WorkshopAssets.EffectMaterial(true);p.Surface=WorkshopState.CaptureMaterial(m);p.Surface.UsePlayerColor=true;OwnedAssets.Release(m);BuildPreview();partIndex=renderers.Count-1;RefreshSelection();SetInspector("shape");});}
 private void ChangeParticles(Action<ParticleEdit> change){if(refreshing||Part?.Particles==null)return;Checkpoint("particle-settings");change(Part.Particles);foreach(var id in preview!.GetComponentsInChildren<PartIdentity>(true).Where(i=>i.Id==Part.Id)){var system=id.GetComponent<ParticleSystem>();if(system)ParticleFactory.Configure(system,Part.Particles,false);}NotifyValues();}
 public List<object> ParticleStyles {get;}=new List<object>{"Sparks","Mist","Dust"};
 public string ParticleStyle {get=>Part?.Particles?.Style??"Sparks";set=>ChangeParticles(p=>{p.Style=value;if(value=="Mist"){p.Size=.02f;p.Speed=.03f;p.Life=2;}else if(value=="Dust"){p.Size=.002f;p.Speed=.01f;p.Life=3;}});}
 public float ParticleRate {get=>Part?.Particles?.Rate??20;set=>ChangeParticles(p=>p.Rate=Mathf.Clamp(value,0,500));}public float ParticleLife {get=>Part?.Particles?.Life??.6f;set=>ChangeParticles(p=>p.Life=Mathf.Clamp(value,.05f,10));}public float ParticleSpeed {get=>Part?.Particles?.Speed??.15f;set=>ChangeParticles(p=>p.Speed=Mathf.Clamp(value,0,5));}public float ParticleSize {get=>(Part?.Particles?.Size??.004f)*1000;set=>ChangeParticles(p=>p.Size=Mathf.Clamp(value,.1f,100)*.001f);}public float ParticleSpread {get=>Part?.Particles?.Spread??20;set=>ChangeParticles(p=>p.Spread=Mathf.Clamp(value,0,90));}
 private void NotifyModern(){NotifyTrailValues();foreach(string key in new[]{nameof(HasSelection),nameof(NoSelection),nameof(PartWidthText),nameof(PartDepthText),nameof(PartHeightText),nameof(CrossSection),nameof(PartWidth),nameof(PartDepth),nameof(PartHeight),nameof(PointWidth),nameof(PointDepth),nameof(IsTrail),nameof(IsSolidPart),nameof(IsParticle),nameof(ColorSource),nameof(HasEffect),nameof(EffectColor),nameof(EffectGlow),nameof(EffectName),nameof(EffectScale),nameof(EffectSpeed),nameof(EffectDepth),nameof(EffectBrightness),nameof(EffectAccent),nameof(FlowDirection),nameof(ParticleStyle),nameof(ParticleRate),nameof(ParticleLife),nameof(ParticleSpeed),nameof(ParticleSize),nameof(ParticleSpread)})NotifyPropertyChanged(key);}
}
