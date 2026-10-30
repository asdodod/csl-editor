using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Components.Settings;
using UnityEngine;
using UnityEngine.UI;
namespace SaberWorkshop;

public sealed partial class EditorController {
 [UIComponent("material-thumbnail")] private RawImage materialImage=null!;
 [UIComponent("material-target-list")] private DropDownListSetting materialTargetList=null!;
 private readonly List<int> effectTargetIndices=new List<int>();
 private Material? effectDraft;private TextureEdit? draftImage;private string previewForm="Sphere";
 public List<object> MaterialPreviewForms {get;}=new List<object>{"Selected part","Blade","Ring","Sphere"};
 public List<object> EffectTargets {get;private set;}=new List<object>();
 private string effectTargetChoice="";
 public string EffectTarget {get=>effectTargetChoice;set{effectTargetChoice=value;if(!refreshing&&previewForm=="Selected part")RefreshMaterialThumbnail();}}
 public string MaterialPreviewForm {get=>previewForm;set{previewForm=value;RefreshMaterialThumbnail();}}
 private void EnsureEffectDraft(){if(!effectDraft)SetEffectDraft(WorkshopAssets.EffectMaterial());}
 private void SetEffectDraft(Material material,TextureEdit? image=null){if(effectDraft)OwnedAssets.Release(effectDraft!);effectDraft=material;draftImage=image;DraftChanged();}
 private SurfaceEdit CaptureDraft(){var surface=WorkshopState.CaptureMaterial(effectDraft!);surface.UsePlayerColor=false;if(draftImage!=null)surface.Textures["_MainTex"]=draftImage;return surface;}
 [UIAction("UseDraftImage")] public void UseDraftImage(){Guard(()=>{EnsureEffectDraft();string path=WorkshopState.SafePath(WorkshopState.TextureFolder,TextureFileChoice);if(!File.Exists(path)||new FileInfo(path).Length>12*1024*1024)throw new InvalidDataException("Choose an image up to 12 MB.");draftImage=new TextureEdit{Png=Convert.ToBase64String(File.ReadAllBytes(path))};state.ApplyMaterial(effectDraft!,CaptureDraft());DraftChanged();SetStatus("Image loaded into material preview.");});}
 private void DraftChanged(){RefreshMaterialThumbnail();NotifyValues();}
 private void RefreshEffectTargets(){effectTargetIndices.Clear();for(int i=0;i<renderers.Count;i++)if(renderers[i].GetComponent<MeshFilter>()||renderers[i] is SkinnedMeshRenderer)effectTargetIndices.Add(i);EffectTargets=effectTargetIndices.Select(i=>(object)$"{i+1}. {DisplayName(renderers[i])}").ToList();EffectTarget=Choice(EffectTargets,effectTargetIndices.IndexOf(partIndex));UpdateList(materialTargetList,EffectTargets,EffectTarget);}
 private void RefreshMaterialThumbnail(){if(!materialImage||!effectDraft)return;var sample=materialImage.GetComponent<MaterialThumbnail>()??materialImage.gameObject.AddComponent<MaterialThumbnail>();var renderer=Selected;int index=EffectTargets.IndexOf(EffectTarget);if(previewForm=="Selected part"&&index>=0&&index<effectTargetIndices.Count)renderer=renderers[effectTargetIndices[index]];var mesh=renderer?renderer!.GetComponent<MeshFilter>()?.sharedMesh??(renderer as SkinnedMeshRenderer)?.sharedMesh:null;sample.Show(effectDraft!,mesh,previewForm);}
 [UIAction("ApplyEffectDraft")] public void ApplyEffectDraft(){Guard(()=>{
  EnsureEffectDraft();int index=EffectTargets.IndexOf(EffectTarget);if(index<0||index>=renderers.Count)throw new InvalidOperationException("Choose a part for this material.");
  partIndex=effectTargetIndices[index];materialIndex=0;RefreshSelection();if(IsParticle||IsTrail)throw new InvalidOperationException("Choose a blade, handle or ring for this shader.");
  Checkpoint("apply-material");AssignEffect(CaptureDraft(),new Material(effectDraft!));SetStatus("Material applied to "+DisplayName(Selected!)+".");SetInspector("shape");
 });}
 private void DisposeEffectDraft(){if(effectDraft)OwnedAssets.Release(effectDraft!);effectDraft=null;}
}
