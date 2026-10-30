using System;
using System.Linq;
using UnityEngine;
namespace SaberWorkshop;

// End edits modify the real profile points, so saved projects, the point editor
// and CSL export all describe the same geometry. No hidden preview-only tip.
public static class ProfileEnds {
 static void Check(ProfilePart part){if(part.Geometry!=null||part.Shape=="ring"||part.Particles!=null||part.Trail!=null||part.Sections.Count<2)throw new InvalidOperationException("Choose a blade, handle or profile part.");}
 static Section[] Points(ProfilePart part)=>part.Sections.OrderBy(p=>p.T).ToArray();
 static float Metres(ProfilePart part)=>part.Length*part.HeightScale;
 public static bool Tapered(ProfilePart part,bool top){var points=Points(part);if(points.Length<2)return false;var end=points[top?points.Length-1:0];var shoulder=points[top?points.Length-2:1];return end.Radius<shoulder.Radius*.99f;}
 public static float Length(ProfilePart part,bool top){var points=Points(part);if(points.Length<2)return 0;return Mathf.Abs(points[top?points.Length-1:0].T-points[top?points.Length-2:1].T)*Metres(part)*1000;}
 public static void Flatten(ProfilePart part,bool top){Check(part);var points=Points(part);var end=points[top?points.Length-1:0];var shoulder=points[top?points.Length-2:1];if(shoulder.Radius>0){end.Radius=shoulder.Radius;end.Aspect=shoulder.Aspect;}part.Caps=true;}
 public static void Point(ProfilePart part,bool top){
  Check(part);var points=Points(part);var end=points[top?points.Length-1:0];var shoulder=points[top?points.Length-2:1];
  // A two-point cylinder must not turn into a cone along its entire length.
  float span=Mathf.Abs(end.T-shoulder.T);if(points.Length==2||(points.Length==3&&Tapered(part,!top))||span*Metres(part)>.04f){
   if(part.Sections.Count>=128)throw new InvalidOperationException("Remove a shape point before adding another end section.");
   float distance=Mathf.Min(.02f/Mathf.Max(.0001f,Metres(part)),span*.5f);float t=end.T+(top?-distance:distance);float blend=Mathf.InverseLerp(shoulder.T,end.T,t);
   var added=new Section{T=t,Radius=shoulder.Radius,Aspect=shoulder.Aspect,X=Mathf.Lerp(shoulder.X,end.X,blend),Y=Mathf.Lerp(shoulder.Y,end.Y,blend)};part.Sections.Add(added);part.Sections=part.Sections.OrderBy(p=>p.T).ToList();shoulder=added;
  }
  end.Radius=0;end.Aspect=shoulder.Aspect;part.Caps=true;
 }
 public static void SetLength(ProfilePart part,bool top,float millimetres){
  Check(part);if(float.IsNaN(millimetres)||float.IsInfinity(millimetres))throw new InvalidOperationException("Enter a valid end length.");
  if(part.Sections.Count==2||(part.Sections.Count==3&&Tapered(part,!top)))Point(part,top);
  var points=Points(part);var end=points[top?points.Length-1:0];var shoulder=points[top?points.Length-2:1];const float epsilon=.000001f;
  float limit=top?points[points.Length-3].T:points[2].T;float low=top?limit+epsilon:end.T+epsilon;float high=top?end.T-epsilon:limit-epsilon;
  if(high<=low)return;float length=Mathf.Max(.0001f,millimetres*.001f)/Mathf.Max(.0001f,Metres(part));shoulder.T=Mathf.Clamp(end.T+(top?-length:length),low,high);
 }
}
