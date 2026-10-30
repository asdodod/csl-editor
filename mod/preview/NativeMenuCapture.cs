using System;
using System.IO;
using System.Linq;
using HMUI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor.SceneManagement;
using SaberWorkshop;

// Same XML/layout fixture as the basic harness, with actual game graphics,
// fonts, shader materials and curved canvas. BSML setting-prefab internals and
// VR raycasting are still outside this fixture's coverage.
public static class NativeMenuCapture {
 public static void Run(string output,TMP_FontAsset font,Material textMaterial,Material ui){
  foreach(var page in new[]{"part","effects","help"}){
   EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
   var root=new GameObject("Native markup preview",typeof(RectTransform),typeof(Canvas));var canvas=root.GetComponent<Canvas>();canvas.renderMode=RenderMode.WorldSpace;canvas.additionalShaderChannels=AdditionalCanvasShaderChannels.TexCoord1|AdditionalCanvasShaderChannels.TexCoord2;((RectTransform)root.transform).sizeDelta=new Vector2(250,126);
   var view=root.AddComponent<PreviewMenu>();view.Initialize(File.ReadAllText("Assets/MenuPreview/Editor.bsml"));
   if(page=="effects"){view.Set("HasEffect",true);view.Activate("ShaderInspector");}else if(page=="help")view.Action("OpenHelp");
   Canvas.ForceUpdateCanvases();foreach(var t in root.GetComponentsInChildren<PreviewTextSize>(true))t.Apply();Canvas.ForceUpdateCanvases();
   // Mask caches its sibling Graphic using CLR ?? rather than Unity's null
   // check, so replacing a Graphic requires recreating the fixture's mask.
   var masks=root.GetComponentsInChildren<Mask>(true).Select(m=>new{Node=m.gameObject,Show=m.showMaskGraphic}).ToArray();foreach(var mask in root.GetComponentsInChildren<Mask>(true))UnityEngine.Object.DestroyImmediate(mask);
   var pixel=Sprite.Create(Texture2D.whiteTexture,new Rect(0,0,1,1),Vector2.one*.5f,1);
   var background=new GameObject("Curved background",typeof(RectTransform)).GetComponent<RectTransform>();background.SetParent(root.transform,false);background.SetAsFirstSibling();background.sizeDelta=new Vector2(250,126);WorkshopSurface.Image(background,pixel,ui,new Color(.025f,.035f,.05f));
   foreach(var sizing in root.GetComponentsInChildren<PreviewTextSize>(true)){
    var old=sizing.Label;var parent=sizing.Parent;string text=old.text;var align=old.alignment;float size=sizing.FontSize;bool wrap=old.horizontalOverflow==HorizontalWrapMode.Wrap;
    UnityEngine.Object.DestroyImmediate(sizing.gameObject);var label=parent.gameObject.AddComponent<CurvedTextMeshPro>();label.font=font;label.fontSharedMaterial=textMaterial;label.fontSize=size;label.text=text;label.color=new Color(.88f,.92f,.96f);label.raycastTarget=false;label.enableWordWrapping=wrap;
    label.alignment=align==TextAnchor.MiddleCenter?TextAlignmentOptions.Midline:align==TextAnchor.UpperLeft?TextAlignmentOptions.TopLeft:TextAlignmentOptions.MidlineLeft;
   }
   foreach(var old in root.GetComponentsInChildren<Image>(true).Where(i=>!(i is ImageView)).ToArray()){
    var rect=(RectTransform)old.transform;var colour=old.color;bool raycast=old.raycastTarget;UnityEngine.Object.DestroyImmediate(old);var image=WorkshopSurface.Image(rect,pixel,ui,colour);image.raycastTarget=raycast;
   }
   foreach(var b in root.GetComponentsInChildren<Button>(true))WorkshopSurface.Button(b,pixel,ui);
   // Curvature belongs to the root canvas, never a reparented text subtree.
   root.AddComponent<CurvedCanvasSettings>().SetRadius(120);
   foreach(var raw in root.GetComponentsInChildren<RawImage>(true)){raw.material=ui;raw.gameObject.AddComponent<CurvedRawImageMesh>();}
   foreach(var saved in masks)saved.Node.AddComponent<Mask>().showMaskGraphic=saved.Show;
   foreach(var graphic in root.GetComponentsInChildren<MaskableGraphic>(true))graphic.RecalculateMasking();
   var camera=new GameObject("Native menu camera").AddComponent<Camera>();camera.transform.position=new Vector3(0,0,-240);camera.fieldOfView=48;camera.nearClipPlane=.1f;camera.farClipPlane=1000;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.012f,.02f,.035f);
   Canvas.ForceUpdateCanvases();foreach(var t in root.GetComponentsInChildren<TMP_Text>())t.ForceMeshUpdate();Canvas.ForceUpdateCanvases();foreach(var sample in root.GetComponentsInChildren<MaterialThumbnail>())sample.RenderNow();
   if(page=="part")foreach(var state in root.GetComponentsInChildren<WorkshopButtonState>())if(state.Face.canvasRenderer.GetColor()!=Color.white)throw new Exception("Button vertex colour is multiplied by a stale tint: "+state.name);
   NativeWidgetProbe.Capture(camera,Path.Combine(output,"menu-"+page+".png"));
   if(page=="part"){camera.transform.position=new Vector3(55,10,-240);camera.transform.LookAt(Vector3.zero);NativeWidgetProbe.Capture(camera,Path.Combine(output,"menu-angle.png"));}
  }
  File.WriteAllText(Path.Combine(output,"coverage.txt"),"Actual game ImageView, CurvedTextMeshPro, Teko font/atlas, UINoGlow material and CurvedCanvasSettings.\nSame Editor.bsml and WorkshopSurface button code as the mod.\nSetting controls use the layout fixture, not complete BSML game prefabs. VR input and game scene composition still need an in-game check.\n");
  EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
  Debug.Log("NATIVE_MENU_CAPTURE_COMPLETE three XML views and angled view");
 }
}
