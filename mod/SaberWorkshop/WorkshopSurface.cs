using HMUI;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
namespace SaberWorkshop;

// Shared by the installed mod and the native Unity preview. Keep geometry and
// state colours here so a preview does not invent a second version of the skin.
public static class WorkshopSurface {
 public static ImageView Image(RectTransform rect,Sprite pixel,Material material,Color colour){
  var image=rect.gameObject.AddComponent<ImageView>();image.useScriptableObjectColors=false;image.gradient=false;
  image.sprite=pixel;image.material=material;image.color=colour;image.raycastTarget=false;return image;
 }
 public static void Button(Button button,Sprite pixel,Material material){
  if(button.GetComponent<WorkshopButtonState>())return;
  // Practice buttons use NoTransitionsButton: its state event samples clips
  // independently of Selectable.transition, re-enabling the old underline.
  foreach(var animation in button.GetComponentsInChildren<ButtonStaticAnimations>(true))animation.enabled=false;
  foreach(var animation in button.GetComponentsInChildren<Animator>(true))animation.enabled=false;
  foreach(var animation in button.GetComponentsInChildren<Animation>(true))animation.enabled=false;
  // Keep the native hierarchy, text and layout. Only replace the visible face.
  // Avoid tint animation on cloned game graphics; the replacement face
  // receives its colours directly in the generated vertices.
  foreach(var graphic in button.GetComponentsInChildren<Graphic>(true))if(!(graphic is TMP_Text))graphic.enabled=false;
  var content=button.transform.Find("Content") as RectTransform;
  if(content!=null){foreach(var group in content.GetComponentsInChildren<LayoutGroup>(true))group.enabled=false;foreach(var fitter in content.GetComponentsInChildren<ContentSizeFitter>(true))fitter.enabled=false;Fill(content,2);}
  foreach(var label in button.GetComponentsInChildren<TMP_Text>(true)){
   label.fontStyle=FontStyles.Normal;label.fontSize=3.4f;label.enableAutoSizing=false;label.enableWordWrapping=false;label.alignment=TextAlignmentOptions.Midline;label.margin=Vector4.zero;
   if(content!=null&&label.transform.IsChildOf(content))Fill(label.rectTransform,0);
  }
  var rect=new GameObject("Workshop button face",typeof(RectTransform),typeof(LayoutElement)).GetComponent<RectTransform>();
  rect.SetParent(button.transform,false);rect.SetAsFirstSibling();rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;
  rect.GetComponent<LayoutElement>().ignoreLayout=true;
  var image=Image(rect,pixel,material,new Color(.12f,.17f,.23f));image.raycastTarget=true;
  button.targetGraphic=image;button.transition=Selectable.Transition.None;
  // Assigning targetGraphic while ColorTint is still active applies the old
  // normal tint immediately. Reset it after switching to vertex-colour states.
  image.canvasRenderer.SetColor(Color.white);
  var states=button.gameObject.AddComponent<WorkshopButtonState>();states.Initialize(image,button);
 }
 static void Fill(RectTransform rect,float inset){rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.pivot=Vector2.one*.5f;rect.offsetMin=new Vector2(inset,0);rect.offsetMax=new Vector2(-inset,0);}
}

public sealed class WorkshopButtonState : MonoBehaviour,IPointerEnterHandler,IPointerExitHandler,IPointerDownHandler,IPointerUpHandler {
 public ImageView Face=null!;public Button Button=null!;bool hover,down,lastInteractable;
 public void Initialize(ImageView face,Button button){Face=face;Button=button;Refresh();}
 void OnEnable(){hover=down=false;Refresh();}
 void OnDisable(){hover=down=false;Refresh();}
 void LateUpdate(){if(Button&&lastInteractable!=Button.IsInteractable())Refresh();}
 public void OnPointerEnter(PointerEventData e){hover=true;Refresh();}
 public void OnPointerExit(PointerEventData e){hover=down=false;Refresh();}
 public void OnPointerDown(PointerEventData e){down=true;Refresh();}
 public void OnPointerUp(PointerEventData e){down=false;Refresh();}
 void Refresh(){lastInteractable=Button&&Button.IsInteractable();if(Face)Face.color=!lastInteractable?new Color(.07f,.09f,.12f):down?new Color(.1f,.28f,.37f):hover?new Color(.24f,.38f,.48f):new Color(.12f,.17f,.23f);}
}
