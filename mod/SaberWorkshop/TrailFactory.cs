using UnityEngine;
namespace SaberWorkshop;
public static class TrailFactory {
 public static void Configure(TrailRenderer trail,TrailEdit edit){trail.time=edit.Time;trail.minVertexDistance=.003f;trail.widthMultiplier=edit.Width;trail.widthCurve=AnimationCurve.Linear(0,1,1,edit.Taper);trail.alignment=edit.Type=="Blade ribbon"?LineAlignment.TransformZ:LineAlignment.View;trail.textureMode=LineTextureMode.Stretch;trail.numCornerVertices=3;trail.numCapVertices=3;trail.autodestruct=false;trail.emitting=true;var gradient=new Gradient();gradient.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(Color.white,1)},new[]{new GradientAlphaKey(1,0),new GradientAlphaKey(.6f,.5f),new GradientAlphaKey(0,1)});trail.colorGradient=gradient;}
}
public sealed class PreviewMotion : MonoBehaviour {
 private void Update(){float t=Time.unscaledTime*2.8f;foreach(string name in new[]{"LeftSaber","RightSaber"}){var hand=transform.Find(name);if(hand)hand.localRotation=Quaternion.Euler(-90+8*Mathf.Sin(t*.7f),0,18*Mathf.Sin(t+(name=="LeftSaber"?0:.7f)));}}
 private void OnDisable(){foreach(string name in new[]{"LeftSaber","RightSaber"}){var hand=transform.Find(name);if(hand)hand.localRotation=Quaternion.Euler(-90,0,0);}foreach(var trail in GetComponentsInChildren<TrailRenderer>(true))trail.Clear();}
}
