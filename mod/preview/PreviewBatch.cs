using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static class PreviewBatch {
 static PreviewMenu view;static Camera camera;static string output;
 static string Arg(string key){var a=Environment.GetCommandLineArgs();int i=Array.IndexOf(a,key);return i>=0?a[i+1]:null;}
 [MenuItem("Saber Workshop/Open offline menu")]
 public static void Open(){Create(Path.GetFullPath("Assets/MenuPreview/Editor.bsml"));EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(),"Assets/MenuPreview/MenuPreview.unity");Selection.activeGameObject=view.gameObject;}
 static void Create(string path){
  EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
  new GameObject("Event system",typeof(EventSystem),typeof(StandaloneInputModule));
  var canvas=new GameObject("Offline Workshop",typeof(RectTransform),typeof(Canvas),typeof(GraphicRaycaster));var rect=(RectTransform)canvas.transform;rect.sizeDelta=new Vector2(250,126);canvas.GetComponent<Canvas>().renderMode=RenderMode.WorldSpace;canvas.AddComponent<Image>().color=new Color(.025f,.035f,.05f);
  view=canvas.AddComponent<PreviewMenu>();view.Initialize(File.ReadAllText(path));
  camera=new GameObject("Preview camera").AddComponent<Camera>();camera.transform.position=new Vector3(0,0,-150);camera.orthographic=true;camera.orthographicSize=75;camera.nearClipPlane=.1f;camera.farClipPlane=300;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.018f,.025f,.04f);
 }
 public static void Run(){try{
  output=Arg("-previewOutput");Directory.CreateDirectory(output);Create(Arg("-previewMarkup"));
  Capture("01-part.png");
  var list=view.GetComponentsInChildren<RectTransform>().First(r=>r.name=="part-tree");if(list.rect.width<50||list.GetComponentsInChildren<Button>().Length<5)throw new Exception("Parts list did not lay out");
  foreach(var slider in view.Sliders.Values.Where(s=>s.gameObject.activeInHierarchy))if(slider.handleRect.rect.height>((RectTransform)slider.transform).rect.height+.1f)throw new Exception("Slider thumb overlaps its caption");
  view.Sliders["PartWidth"].value=32;
  if(Math.Abs(view.Number("PartWidth")-32)>.001)throw new Exception("Drag binding failed");
  var pointer=new PointerEventData(EventSystem.current){position=new Vector2(100,100)};var tap=view.Sliders["PartWidth"].GetComponent<PreviewTap>();tap.OnPointerDown(pointer);tap.OnPointerUp(pointer);
  if(!view.Modals["number-dialog"].activeSelf)throw new Exception("Short tap did not open keypad");
  view.Action("NumClear");view.Action("Num4");view.Action("Num2");Capture("02-number-entry.png");view.Action("ApplyNumber");
  if(Math.Abs(view.Number("PartWidth")-42)>.001)throw new Exception("Numeric entry failed");
  view.Activate("ColorInspector");Capture("03-material.png");view.Set("HasEffect",true);view.Activate("ShaderInspector");Capture("04-effects.png");
  view.Activate("ShapeInspector");view.Set("ShowTransform",true);Capture("05-transform.png");
  EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(),"Assets/MenuPreview/MenuPreview.unity");
  File.WriteAllText(Path.Combine(output,"checks.txt"),"Same Editor.bsml loaded.\nUGUI layout and clipping rendered.\nSlider binding: 32 mm.\nShort tap opens keypad.\nNumeric entry: 42 mm.\nGame-specific BSML prefabs and VR input still require an in-game check.\n");
  EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
  Debug.Log("MENU_PREVIEW_VERIFIED: slider, keypad, five rendered views");EditorApplication.Exit(0);
 }catch(Exception e){Debug.LogError("MENU_PREVIEW_FAILED "+e);EditorApplication.Exit(1);}}
 static void Capture(string name){
  Canvas.ForceUpdateCanvases();foreach(var text in view.GetComponentsInChildren<PreviewTextSize>(true))text.Apply();Canvas.ForceUpdateCanvases();
  foreach(var scroll in view.GetComponentsInChildren<ScrollRect>())if(scroll.verticalScrollbar)scroll.verticalScrollbar.size=Mathf.Clamp01(scroll.viewport.rect.height/Mathf.Max(1,scroll.content.rect.height));
  var rt=new RenderTexture(2000,1120,24);camera.targetTexture=rt;camera.Render();var previous=RenderTexture.active;RenderTexture.active=rt;
  var image=new Texture2D(rt.width,rt.height,TextureFormat.RGBA32,false);image.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0);image.Apply();File.WriteAllBytes(Path.Combine(output,name),image.EncodeToPNG());
  RenderTexture.active=previous;camera.targetTexture=null;UnityEngine.Object.DestroyImmediate(image);UnityEngine.Object.DestroyImmediate(rt);
 }
}
