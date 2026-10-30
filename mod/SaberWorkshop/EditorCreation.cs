using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using BeatSaberMarkupLanguage.Attributes;
using Newtonsoft.Json;
using UnityEngine;

namespace SaberWorkshop;

public sealed partial class EditorController
{
    private GameObject? guidePreview;
    private bool showSizeGuide;
    public bool ShowSizeGuide {get=>showSizeGuide;set{showSizeGuide=value;if(guidePreview)guidePreview!.SetActive(value);NotifyPropertyChanged();}}
    public string NewPresetName {get;set;}="My saber";
    public List<object> StarterOptions {get;}=new List<object>{"Straight","Segmented","Empty"};
    public string StarterChoice {get;set;}="Straight";
    public float OutlineWidth {get=>(Part?.OutlineThickness??0)*1000;set{if(refreshing||Part==null)return;Checkpoint("outline-width");Part.OutlineThickness=Mathf.Clamp(value,0,50)*.001f;RebuildShape();}}
    public bool InvertedFaces {get=>Part?.Inverted??false;set{if(refreshing||Part==null)return;Checkpoint("inverted");Part.Inverted=value;RebuildShape();}}
    private string AvailablePresetName(string requested){WorkshopState.EnsureFolders();string name=string.Concat((requested??"").Where(c=>!Path.GetInvalidFileNameChars().Contains(c))).Trim();if(name=="")name="My saber";name=name.Substring(0,Math.Min(72,name.Length));string candidate=name;int i=2;while(File.Exists(Path.Combine(WorkshopState.ProjectFolder,candidate+".workshop.json")))candidate=name+" ("+(i++)+")";return candidate;}
    [UIAction("CreatePreset")] public void CreatePreset(){Guard(()=>{
        string name=AvailablePresetName(NewPresetName);
        if(StarterChoice=="Segmented")state.CreateStarter();else {state.New();if(StarterChoice!="Empty"){
            var handle=state.AddPart("Handle",false);handle.Length=.22f;handle.Position[2]=-.17f;
            var blade=state.AddPart("Blade",true);blade.Length=1;blade.Position[2]=.08f;blade.Sections=new List<Section>{new Section{T=0,Radius=.007f},new Section{T=.96f,Radius=.006f},new Section{T=1,Radius=.0002f}};
        }}
        state.Project.Name=name;undo.Clear();redo.Clear();partIndex=0;state.Save();BuildPreview();RefreshFiles();ProjectChoice=name;UpdateList(projectList,ProjectOptions,name);UpdateList(gameplayProjectList,ProjectOptions,name);SetStatus("Preset created: "+name);
    });}
    [UIAction("SaveCopy")] public void SaveCopy(){Guard(()=>{if(!state.Ready)throw new InvalidOperationException("Create a preset first.");string previous=state.Project.Name;try{state.Project.Name=AvailablePresetName(NewPresetName);state.Save();}catch{state.Project.Name=previous;throw;}RefreshFiles();ProjectChoice=state.Project.Name;UpdateList(projectList,ProjectOptions,ProjectChoice);UpdateList(gameplayProjectList,ProjectOptions,ProjectChoice);NotifyPropertyChanged(nameof(ProjectName));SetStatus("Saved copy: "+state.Project.Name);});}
    [UIAction("AddOutline")] public void AddOutline(){Guard(()=>{
        if(!Selected)throw new InvalidOperationException("Select a base part first.");if(Part?.Inverted==true)throw new InvalidOperationException("Select the base part, not its outline.");Checkpoint("outline");ProfilePart p;
        if(Part!=null)p=JsonConvert.DeserializeObject<ProfilePart>(JsonConvert.SerializeObject(Part))!;
        else{var filter=Selected!.GetComponent<MeshFilter>();if(!filter||!filter!.sharedMesh)throw new InvalidOperationException("Select a mesh part.");p=new ProfilePart{Shape="mesh",Geometry=MeshData.Capture(filter.sharedMesh)};var hand=preview!.transform.Find("LeftSaber");var matrix=hand.worldToLocalMatrix*Selected.transform.localToWorldMatrix;var positions=p.Geometry.Positions;for(int i=0;i<positions.Length;i+=3){var v=matrix.MultiplyPoint3x4(new Vector3(positions[i],positions[i+1],positions[i+2]));positions[i]=v.x;positions[i+1]=v.y;positions[i+2]=v.z;}}
        p.Id=Guid.NewGuid().ToString("N");p.Name=DisplayName(Selected!)+" outline";p.Inverted=true;p.OutlineThickness=.001f;var material=state.DefaultMaterial(true);p.Surface=WorkshopState.CaptureMaterial(material);p.Surface.UsePlayerColor=false;OwnedAssets.Release(material);state.Project.Parts.Add(p);BuildPreview();partIndex=renderers.Count-1;RefreshSelection();SetStatus("Inverted outline added. Set its width and material.");
    });}
}
