using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Components;
using HMUI;
using UnityEngine;

namespace SaberWorkshop;

public sealed partial class EditorController
{
    private readonly List<string> undo=new List<string>(),redo=new List<string>();
    private string lastChange="";private float lastChangeTime=-10;
    private void Checkpoint(string key){
        if(lastChange==key&&Time.realtimeSinceStartup-lastChangeTime<.6f){lastChangeTime=Time.realtimeSinceStartup;return;}
        var json=JsonConvert.SerializeObject(state.Project);if(undo.Count==0||undo[undo.Count-1]!=json)undo.Add(json);if(undo.Count>16)undo.RemoveAt(0);redo.Clear();lastChange=key;lastChangeTime=Time.realtimeSinceStartup;
    }
    [UIAction("Undo")] public void Undo()=>RestoreHistory(undo,redo);
    [UIAction("Redo")] public void Redo()=>RestoreHistory(redo,undo);
    private void RestoreHistory(List<string> from,List<string> to){if(from.Count==0)return;Guard(()=>{var previous=JsonConvert.DeserializeObject<WorkshopProject>(from[from.Count-1])!;if(previous.SourceFile!=state.Project.SourceFile)throw new InvalidOperationException("Reopen that source file to restore its edits.");to.Add(JsonConvert.SerializeObject(state.Project));from.RemoveAt(from.Count-1);state.Project=previous;lastChange="";BuildPreview();SetStatus("Restored.");});}
    private string TintKey=>Material&&Material!.HasProperty("_Color")?"_Color":Material&&Material!.HasProperty("_StarNestMainColor")?"_StarNestMainColor":"_TintColor";
    private string GlowKey=>Material?new[]{"_Glow","_BSGlow","_ALTGlow","_Bloom"}.FirstOrDefault(k=>Material!.HasProperty(k))??"":"";
    public bool HasTint=>Material&&Material!.HasProperty(TintKey);
    public bool HasGlow=>GlowKey!="";
    public float SaberLength {get=>state.Project.SaberLength;set{Checkpoint("saber-length");state.Project.SaberLength=Mathf.Clamp(value,.1f,3);BuildPreview();}}
    public float SaberWidth {get=>state.Project.SaberWidth;set{Checkpoint("saber-width");state.Project.SaberWidth=Mathf.Clamp(value,.1f,3);BuildPreview();}}
    public Color TintColor {get=>HasTint?Material!.GetColor(TintKey):Color.white;set{if(refreshing||!HasTint||Edit==null)return;Checkpoint("tint");PlayerColor=false;Edit!.Colors[TintKey]=new[]{value.r,value.g,value.b,value.a};ChangedMaterial();}}
    public float Glow {get=>HasGlow?Material!.GetFloat(GlowKey):0;set{if(refreshing||!HasGlow||Edit==null)return;Checkpoint("glow");Edit!.Numbers[GlowKey]=Mathf.Clamp(value,0,10);ChangedMaterial();Plugin.Log.Debug("Material glow set to "+Material!.GetFloat(GlowKey)+" on "+Selected!.name);}}
    public string PartName {get=>Selected?DisplayName(Selected!):"";set{if(!Selected||refreshing)return;Checkpoint("rename");if(Part!=null){Part.Name=value;Selected!.name=value;}else state.Project.Labels[PartKey()]=value;RefreshTree();}}
    private string PartKey(){var hand=preview!.transform.Find("LeftSaber");return WorkshopState.PathOf(Selected!.transform,hand);}
    private string DisplayName(Renderer renderer){var hand=preview!.transform.Find("LeftSaber");var key=WorkshopState.PathOf(renderer.transform,hand);if(state.Project.Labels.TryGetValue(key,out var label))return label;if(state.Project.SourceFile.IndexOf("Zoog",StringComparison.OrdinalIgnoreCase)<0)return renderer.name;return renderer.name switch {"Sphere"=>"Blade outline","Sphere.002"=>"Blade plasma","Cylinder.001"=>"Handle plasma","BezierCircle"=>"Fire ring","BezierCircle.001"=>"Energy ring","Sphere.003"=>"Orb",_=>renderer.name};}
    private void RefreshTree(){if(!partTree)return;var rows=new List<CustomListTableData.CustomCellInfo>();for(int i=0;i<renderers.Count;i++){var r=renderers[i];string stateText=r.gameObject.activeInHierarchy&&r.enabled?"":"[hidden] ";rows.Add(new CustomListTableData.CustomCellInfo(stateText+DisplayName(r)));}partTree.Data=rows;partTree.TableView.ReloadData();}
    [UIAction("SelectTreePart")] public void SelectTreePart(TableView table,int index){if(index<0||index>=renderers.Count)return;partIndex=index;materialIndex=propertyIndex=sectionIndex=slotIndex=0;RefreshSelection();NotifyPropertyChanged(nameof(PartName));}
    [UIAction("TogglePart")] public void TogglePart(){Checkpoint("visibility");PartVisible=!PartVisible;RefreshTree();}
    [UIAction("AddBlade")] public void AddBlade(){Guard(()=>{Checkpoint("add-blade");var p=state.AddPart("Blade",true);p.Length=1;p.Position[2]=.08f;p.Sections=new List<Section>{new Section{T=0,Radius=.008f},new Section{T=.94f,Radius=.006f},new Section{T=1,Radius=.0002f}};BuildPreview();partIndex=renderers.Count-1;RefreshSelection();});}
    [UIAction("AddHandle")] public void AddHandle(){Guard(()=>{Checkpoint("add-handle");var p=state.AddPart("Handle",false);p.Length=.22f;p.Position[2]=-.17f;p.Sections=new List<Section>{new Section{T=0,Radius=.012f},new Section{T=.05f,Radius=.018f},new Section{T=.95f,Radius=.018f},new Section{T=1,Radius=.012f}};BuildPreview();partIndex=renderers.Count-1;RefreshSelection();});}
    [UIAction("DuplicatePart")] public void DuplicatePart(){Guard(()=>{if(Part==null)throw new InvalidOperationException("Duplicate is currently available for created profiles.");Checkpoint("duplicate");var p=JsonConvert.DeserializeObject<ProfilePart>(JsonConvert.SerializeObject(Part))!;p.Id=Guid.NewGuid().ToString("N");p.Name+=" copy";p.Position[0]+=.025f;state.Project.Parts.Add(p);BuildPreview();partIndex=renderers.Count-1;RefreshSelection();});}
}
