using System;
using UnityEngine;
using UnityEngine.EventSystems;
public sealed class PreviewTap : MonoBehaviour,IPointerDownHandler,IPointerUpHandler,IDragHandler {
 public Action Tap;Vector2 origin;float began;bool dragged;
 public void OnPointerDown(PointerEventData e){origin=e.position;began=Time.unscaledTime;dragged=false;}
 public void OnDrag(PointerEventData e){if((e.position-origin).sqrMagnitude>36)dragged=true;}
 public void OnPointerUp(PointerEventData e){if(!dragged&&Time.unscaledTime-began<.3f)Tap?.Invoke();}
}
