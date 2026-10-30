using System;
using System.Collections.Generic;
using System.Globalization;
using BeatSaberMarkupLanguage.Attributes;
using UnityEngine;
namespace SaberWorkshop;
public sealed partial class EditorController {
 private bool testSwing;
 public bool TestSwing {get=>testSwing;set{testSwing=value;ConfigureMotion();NotifyPropertyChanged();}}
 private void ConfigureMotion(){if(!preview)return;var motion=preview!.GetComponent<PreviewMotion>()??preview.AddComponent<PreviewMotion>();motion.enabled=testSwing;}
 private void ScalePreviewTrails(){if(!preview)return;foreach(var trail in preview!.GetComponentsInChildren<TrailRenderer>(true)){var id=trail.GetComponent<PartIdentity>();var part=id?state.Project.Parts.Find(p=>p.Id==id.Id):null;if(part?.Trail!=null)trail.widthMultiplier=part.Trail.Width*preview.transform.lossyScale.x*(part.Trail.Type=="Blade ribbon"?state.Project.SaberLength:state.Project.SaberWidth);}}
 [UIAction("AddTrail")] public void AddTrail(){Guard(()=>{Checkpoint("add-trail");var p=state.AddPart("Trail",true);p.Shape="trail";p.Trail=new TrailEdit();p.Position[2]=.55f;p.Rotation[0]=90;var material=WorkshopAssets.TrailMaterial();p.Surface=WorkshopState.CaptureMaterial(material);p.Surface.UsePlayerColor=true;OwnedAssets.Release(material);testSwing=true;BuildPreview();partIndex=renderers.Count-1;RefreshSelection();SetInspector("shape");NotifyPropertyChanged(nameof(TestSwing));SetStatus("Trail ready. No images needed; Width and Fade time are independent.");});}
 public List<object> TrailTypes {get;}=new List<object>{"Blade ribbon","Tip streak"};
 public List<object> TrailPresets {get;}=new List<object>{"Short","Balanced","Long","Custom"};
 private void ChangeTrail(Action<TrailEdit> edit,bool custom=true){if(refreshing||Part?.Trail==null)return;Checkpoint("trail-settings");edit(Part.Trail);if(custom)Part.Trail.Preset="Custom";foreach(var id in preview!.GetComponentsInChildren<PartIdentity>(true)){if(id.Id!=Part.Id)continue;id.transform.localPosition=WorkshopState.V3(Part.Position);id.transform.localEulerAngles=WorkshopState.V3(Part.Rotation);var trail=id.GetComponent<TrailRenderer>();if(trail)TrailFactory.Configure(trail,Part.Trail);}ScalePreviewTrails();NotifyValues();}
 public string TrailType {get=>Part?.Trail?.Type??"Blade ribbon";set=>ChangeTrail(t=>{t.Type=value;t.Width=value=="Blade ribbon"?.95f:.02f;Part!.Position[2]=value=="Blade ribbon"?.55f:1;Part.Rotation[0]=value=="Blade ribbon"?90:0;});}
 public string TrailPreset {get=>Part?.Trail?.Preset??"Balanced";set=>ChangeTrail(t=>{t.Preset=value;if(value=="Short"){t.Time=.08f;t.Taper=.35f;}else if(value=="Balanced"){t.Time=.18f;t.Taper=.2f;}else if(value=="Long"){t.Time=.35f;t.Taper=.15f;}},false);}
 public float TrailTime {get=>(Part?.Trail?.Time??.18f)*1000;set=>ChangeTrail(t=>t.Time=Mathf.Clamp(value,20,2000)*.001f);}
 public string TrailWidthText {get=>((Part?.Trail?.Width??.95f)*1000).ToString("0.###",CultureInfo.InvariantCulture);set{if(refreshing)return;if(float.TryParse((value??"").Replace(',','.'),NumberStyles.Float,CultureInfo.InvariantCulture,out float width)&&width>=.1f&&width<=2000)ChangeTrail(t=>t.Width=width*.001f);else SetStatus("Width must be between 0.1 and 2000 mm.");}}
    public float TrailWidth {get=>(Part?.Trail?.Width??.95f)*1000;set=>ChangeTrail(t=>t.Width=Mathf.Clamp(value,.1f,2000)*.001f);}
 public float TrailTaper {get=>(Part?.Trail?.Taper??.2f)*100;set=>ChangeTrail(t=>t.Taper=Mathf.Clamp01(value*.01f));}
 private void NotifyTrailValues(){foreach(string key in new[]{nameof(TrailType),nameof(TrailPreset),nameof(TrailTime),nameof(TrailWidth),nameof(TrailWidthText),nameof(TrailTaper),nameof(TestSwing)})NotifyPropertyChanged(key);}
}
