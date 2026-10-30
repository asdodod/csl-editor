using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using TMPro;
using HMUI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using SaberWorkshop;
public static class NativeWidgetProbe {
 static string Arg(string key){var a=Environment.GetCommandLineArgs();return a[Array.IndexOf(a,key)+1];}
 public static void Run(){var errors=new List<string>();Application.logMessageReceived+=(message,stack,type)=>{if(type==LogType.Exception||type==LogType.Error)errors.Add(message);};try{
  string directory=Arg("-gameBundles"),output=Arg("-probeOutput");Directory.CreateDirectory(output);
  var lines=new List<string>();var loaded=new List<AssetBundle>();
  lines.Add("ImageView: "+typeof(HMUI.ImageView).Assembly.FullName);lines.Add("Text: "+typeof(HMUI.CurvedTextMeshPro).Assembly.FullName);lines.Add("Slider: "+typeof(HMUI.RangeValuesTextSlider).Assembly.FullName);
  foreach(string pattern in new[]{"*unitybuiltinshaders*.bundle","*monoscripts*.bundle","default_assets*.bundle","textmeshprofonts_assets*.bundle","core_assets*.bundle","maincore_assets*.bundle"})foreach(string file in Directory.GetFiles(directory,pattern)){
   var bundle=AssetBundle.LoadFromFile(file);if(!bundle){lines.Add("REJECTED "+Path.GetFileName(file));continue;}loaded.Add(bundle);lines.Add("BUNDLE "+Path.GetFileName(file));
   foreach(var name in bundle.GetAllAssetNames())lines.Add("  ASSET "+name);
  }
  foreach(var bundle in loaded){
   foreach(var font in bundle.LoadAllAssets<TMP_FontAsset>())if(font)lines.Add("FONT "+font.name+" shader="+(font.material&&font.material.shader?font.material.shader.name:"missing"));
  }
  var fonts=Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
  var mainFont=fonts.First(f=>f.name=="Teko-Medium SDF");
  // The game's Resources database is not mounted in an Editor test project.
  // Supply TMP startup settings locally, keeping the real font/atlas/material.
  var settings=ScriptableObject.CreateInstance<TMP_Settings>();settings.hideFlags=HideFlags.HideAndDontSave;
  Action<string,object> set=(name,value)=>typeof(TMP_Settings).GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(settings,value);
  var style=ScriptableObject.CreateInstance<TMP_StyleSheet>();style.hideFlags=HideFlags.HideAndDontSave;
  set("m_defaultFontAsset",mainFont);set("m_defaultFontSize",3.4f);set("m_defaultAutoSizeMinRatio",.5f);set("m_defaultAutoSizeMaxRatio",2f);set("m_fallbackFontAssets",new List<TMP_FontAsset>());set("m_defaultStyleSheet",style);
  typeof(TMP_Settings).GetField("s_Instance",BindingFlags.Static|BindingFlags.NonPublic).SetValue(null,settings);
  var materials=loaded.SelectMany(b=>b.LoadAllAssets<Material>()).Where(m=>m).ToArray();
  foreach(var m in materials.Where(m=>m.name.Contains("UI")||m.name.Contains("Curved")))lines.Add("MATERIAL "+m.name+" shader="+m.shader.name);
  var ui=materials.First(m=>m.name=="UINoGlow");
  var curved=materials.First(m=>m.name=="Teko-Medium SDF Curved");
  NativeButtonRegression.Run(mainFont,curved,ui);
  RenderNative(output,mainFont,curved,ui);
  NativeMenuCapture.Run(output,mainFont,curved,ui);
  if(errors.Count>0)throw new Exception("Native rendering reported errors: "+string.Join("; ",errors.Distinct()));
  File.WriteAllLines(Path.Combine(output,"inventory.txt"),lines);Debug.Log("NATIVE_UI_PROBE_COMPLETE assemblies and font resources loaded");EditorApplication.Exit(0);
 }catch(Exception e){Debug.LogError("NATIVE_UI_PROBE_FAILED "+e);EditorApplication.Exit(1);}}
 static RectTransform Rect(string name,Transform parent,Vector2 position,Vector2 size){var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();r.SetParent(parent,false);r.anchorMin=r.anchorMax=Vector2.one*.5f;r.sizeDelta=size;r.anchoredPosition=position;return r;}
 static void RenderNative(string output,TMP_FontAsset font,Material fontMaterial,Material ui){
  var root=new GameObject("Native workshop",typeof(RectTransform),typeof(Canvas));var canvas=root.GetComponent<Canvas>();canvas.renderMode=RenderMode.WorldSpace;canvas.additionalShaderChannels=AdditionalCanvasShaderChannels.TexCoord1|AdditionalCanvasShaderChannels.TexCoord2;((RectTransform)root.transform).sizeDelta=SaberWorkshop.WorkbenchLayout.Size;root.AddComponent<CurvedCanvasSettings>().SetRadius(120);
  var pixel=Sprite.Create(Texture2D.whiteTexture,new Rect(0,0,1,1),Vector2.one*.5f,1);
  WorkshopSurface.Image(Rect("Background",root.transform,Vector2.zero,SaberWorkshop.WorkbenchLayout.Size),pixel,ui,new Color(.025f,.035f,.05f));
  Action<Transform,string,Vector2,Vector2,float> text=(parent,value,pos,size,points)=>{var r=Rect(value,parent,pos,size);var t=r.gameObject.AddComponent<CurvedTextMeshPro>();t.font=font;t.fontSharedMaterial=fontMaterial;t.fontSize=points;t.color=new Color(.88f,.92f,.96f);t.text=value;t.alignment=TextAlignmentOptions.MidlineLeft;t.enableWordWrapping=false;t.raycastTarget=false;};
  text(root.transform,"SABER WORKSHOP",new Vector2(-86,52),new Vector2(68,8),5);
  float x=-46;foreach(var title in new[]{"New","Open","Save","Export CSL","Undo","Redo","Help","Close"}){var r=Rect(title,root.transform,new Vector2(x,52),new Vector2(19,8));var b=r.gameObject.AddComponent<Button>();WorkshopSurface.Button(b,pixel,ui);text(r,title,Vector2.zero,new Vector2(16,8),3.4f);x+=21;}
  text(root.transform,"PARTS",new Vector2(-92,35),new Vector2(50,8),4);
  text(root.transform,"SHAPE",new Vector2(-12,35),new Vector2(90,8),4);
  text(root.transform,"SABER PREVIEW",new Vector2(83,35),new Vector2(70,8),4);
  foreach(var row in new[]{"Blade","Handle","Outline","Ring"}){float y=20-Array.IndexOf(new[]{"Blade","Handle","Outline","Ring"},row)*10;text(root.transform,row,new Vector2(-92,y),new Vector2(50,8),4);}
  int index=0;foreach(var row in new[]{"Part name","Height · mm","Width · mm","Depth · mm","Cross section","Colour source","Glow"}){var y=20-index++*10;text(root.transform,row,new Vector2(-40,y),new Vector2(36,8),3.4f);var r=Rect("Control",root.transform,new Vector2(7,y),new Vector2(51,8));WorkshopSurface.Button(r.gameObject.AddComponent<Button>(),pixel,ui);text(r,index==1?"Blade":index==2?"1000":index==5?"Round":"14",Vector2.zero,new Vector2(46,8),3.4f);}
  var camera=new GameObject("Native preview camera").AddComponent<Camera>();camera.transform.position=new Vector3(0,0,-240);camera.orthographic=false;camera.fieldOfView=48;camera.nearClipPlane=.1f;camera.farClipPlane=500;camera.backgroundColor=new Color(.012f,.02f,.035f);camera.clearFlags=CameraClearFlags.SolidColor;
  Canvas.ForceUpdateCanvases();foreach(var t in root.GetComponentsInChildren<TMP_Text>()){t.ForceMeshUpdate();if(t.textInfo.characterCount==0)throw new Exception("Text mesh is empty: "+t.text);}Canvas.ForceUpdateCanvases();
  Capture(camera,Path.Combine(output,"native-front.png"));camera.transform.position=new Vector3(65,12,-180);camera.transform.LookAt(Vector3.zero);Capture(camera,Path.Combine(output,"native-angle.png"));
  Debug.Log("NATIVE_UI_RENDER_COMPLETE real ImageView, CurvedTextMeshPro, game fonts and shaders");
 }
 public static void Capture(Camera camera,string path){var rt=new RenderTexture(2000,1120,24);camera.targetTexture=rt;camera.Render();var old=RenderTexture.active;RenderTexture.active=rt;var image=new Texture2D(rt.width,rt.height,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0);image.Apply();File.WriteAllBytes(path,image.EncodeToPNG());RenderTexture.active=old;camera.targetTexture=null;UnityEngine.Object.DestroyImmediate(image);UnityEngine.Object.DestroyImmediate(rt);}
}
