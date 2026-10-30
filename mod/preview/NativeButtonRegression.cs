using System;
using System.Reflection;
using HMUI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using SaberWorkshop;

// Reproduce the game-only failure missed by the plain UGUI fixture: a real
// NoTransitionsButton state clip turns its legacy underline back on at Start.
public static class NativeButtonRegression {
 public static void Run(TMP_FontAsset font,Material textMaterial,Material ui){
  var root=new GameObject("Button regression",typeof(RectTransform),typeof(Canvas));
  try{
   var buttonObject=new GameObject("BSMLButton",typeof(RectTransform));buttonObject.transform.SetParent(root.transform,false);((RectTransform)buttonObject.transform).sizeDelta=new Vector2(28,8);
   var button=buttonObject.AddComponent<NoTransitionsButton>();
   var content=new GameObject("Content",typeof(RectTransform),typeof(HorizontalLayoutGroup));content.transform.SetParent(button.transform,false);
   var caption=new GameObject("Text",typeof(RectTransform));caption.transform.SetParent(content.transform,false);var text=caption.AddComponent<CurvedTextMeshPro>();text.font=font;text.fontSharedMaterial=textMaterial;text.text="Add part";text.fontStyle=FontStyles.Italic|FontStyles.UpperCase;
   var underline=new GameObject("Underline",typeof(RectTransform)).AddComponent<ImageView>();underline.transform.SetParent(button.transform,false);
   var clip=new AnimationClip{legacy=true};clip.SetCurve("Underline",typeof(ImageView),"m_Enabled",AnimationCurve.Constant(0,1,1));
   var animation=buttonObject.AddComponent<ButtonStaticAnimations>();
   var flags=BindingFlags.Instance|BindingFlags.NonPublic;var type=typeof(ButtonStaticAnimations);
   type.GetField("_button",flags).SetValue(animation,button);type.GetField("_normalClip",flags).SetValue(animation,clip);type.GetField("_highlightedClip",flags).SetValue(animation,clip);
   underline.enabled=false;type.GetMethod("Start",flags).Invoke(animation,null);
   if(!underline.enabled)throw new Exception("Failed to reproduce native animation restoring underline");
   int clicks=0;button.onClick.AddListener(()=>clicks++);
   var pixel=Sprite.Create(Texture2D.whiteTexture,new Rect(0,0,1,1),Vector2.one*.5f,1);WorkshopSurface.Button(button,pixel,ui);
   type.GetMethod("Start",flags).Invoke(animation,null);type.GetMethod("HandleButtonSelectionStateDidChange",flags).Invoke(animation,new object[]{NoTransitionsButton.SelectionState.Highlighted});
   if(animation.enabled||underline.enabled)throw new Exception("Native animation restored old button decoration after styling");
   if(text.fontStyle!=FontStyles.Normal||text.alignment!=TextAlignmentOptions.Midline)throw new Exception("Practice text style leaked into workshop");
   button.onClick.Invoke();if(clicks!=1)throw new Exception("Styling changed original click binding");
   WorkshopSurface.Button(button,pixel,ui);if(button.GetComponents<WorkshopButtonState>().Length!=1)throw new Exception("Repeated styling duplicated button state");
   Debug.Log("NATIVE_BUTTON_LIFECYCLE_VERIFIED underline regression, game text style, click binding and repeat application");
   UnityEngine.Object.DestroyImmediate(clip);UnityEngine.Object.DestroyImmediate(pixel);
  }finally{UnityEngine.Object.DestroyImmediate(root);}
 }
}
