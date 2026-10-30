using System.Collections.Generic;
using System.Linq;
using HMUI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace SaberWorkshop;

// Small original line icons, rendered with the game's curved UI material.
internal static class WorkshopDecor {
 private static readonly Dictionary<string,Sprite> icons=new Dictionary<string,Sprite>();
 public static void Apply(GameObject root){
  foreach(var title in root.GetComponentsInChildren<TextMeshProUGUI>(true).Where(t=>t.text=="PRESETS"||t.text=="PREVIEW")){
   var panel=title.transform.parent.gameObject;var image=panel.GetComponent<ImageView>()??panel.AddComponent<ImageView>();image.sprite=BeatSaberMarkupLanguage.Utilities.ImageResources.WhitePixel;image.material=BeatSaberMarkupLanguage.Utilities.ImageResources.NoGlowMat;image.color=new Color(.03f,.045f,.065f,1);image.raycastTarget=false;
  }
  foreach(var button in root.GetComponentsInChildren<Button>(true)){
   if(button.name=="BSMLButton")WorkshopSurface.Button(button,BeatSaberMarkupLanguage.Utilities.ImageResources.WhitePixel,BeatSaberMarkupLanguage.Utilities.ImageResources.NoGlowMat);
   var text=button.GetComponentInChildren<TextMeshProUGUI>(true);if(!text)continue;
   string key=text.text switch {"Hide / show"=>"eye","Duplicate"=>"copy","Delete"=>"delete","Add part"=>"plus",_=>""};if(key=="")continue;
   var icon=new GameObject("Action icon",typeof(RectTransform),typeof(LayoutElement)).AddComponent<ImageView>();icon.GetComponent<LayoutElement>().ignoreLayout=true;icon.transform.SetParent(button.transform,false);icon.sprite=Icon(key);icon.material=BeatSaberMarkupLanguage.Utilities.ImageResources.NoGlowMat;icon.color=new Color(.45f,.85f,.93f);icon.raycastTarget=false;
   var rect=(RectTransform)icon.transform;rect.anchorMin=rect.anchorMax=new Vector2(0,.5f);rect.sizeDelta=new Vector2(3.1f,3.1f);rect.anchoredPosition=new Vector2(3.6f,0);text.margin=new Vector4(4,0,0,0);
  }
 }
 private static Sprite Icon(string key){
  if(icons.TryGetValue(key,out var sprite))return sprite;
  var lines=new List<Vector4>();
  void Line(float x,float y,float u,float v)=>lines.Add(new Vector4(x,y,u,v));
  void Box(float x,float y,float u,float v){Line(x,y,u,y);Line(u,y,u,v);Line(u,v,x,v);Line(x,v,x,y);}
  if(key=="plus"){Line(.2f,.5f,.8f,.5f);Line(.5f,.2f,.5f,.8f);}
  if(key=="copy"){Box(.16f,.34f,.66f,.85f);Box(.35f,.16f,.85f,.66f);}
  if(key=="delete"){Line(.2f,.74f,.8f,.74f);Line(.37f,.87f,.63f,.87f);Box(.29f,.16f,.71f,.72f);Line(.44f,.28f,.44f,.6f);Line(.57f,.28f,.57f,.6f);}
  if(key=="eye"){Line(.08f,.5f,.3f,.72f);Line(.3f,.72f,.7f,.72f);Line(.7f,.72f,.92f,.5f);Line(.92f,.5f,.7f,.28f);Line(.7f,.28f,.3f,.28f);Line(.3f,.28f,.08f,.5f);Box(.44f,.42f,.57f,.58f);}
  var texture=new Texture2D(40,40,TextureFormat.RGBA32,false);var pixels=new Color[1600];
  for(int y=0;y<40;y++)for(int x=0;x<40;x++){var p=new Vector2((x+.5f)/40,(y+.5f)/40);float distance=lines.Min(l=>{var a=new Vector2(l.x,l.y);var d=new Vector2(l.z,l.w)-a;return Vector2.Distance(p,a+d*Mathf.Clamp01(Vector2.Dot(p-a,d)/d.sqrMagnitude));});pixels[y*40+x]=new Color(1,1,1,Mathf.Clamp01((.042f-distance)*40));}
  texture.SetPixels(pixels);texture.Apply();sprite=Sprite.Create(texture,new Rect(0,0,40,40),Vector2.one*.5f,40);icons[key]=sprite;return sprite;
 }
}
