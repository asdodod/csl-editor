using System;
using System.Linq;
using Newtonsoft.Json;
using SaberWorkshop;
using UnityEditor;
using UnityEngine;

public static class VerifyProfileEnds {
 static void Check(bool value,string message){if(!value)throw new Exception(message);Debug.Log("ENDS_CHECK "+message);}
 public static void Run(){try{
  var part=new ProfilePart{Length=.2f};var initial=WorkshopState.BuildMesh(part);var original=initial.bounds;UnityEngine.Object.DestroyImmediate(initial);
  ProfileEnds.Point(part,true);Check(part.Sections.Count==3&&ProfileEnds.Tapered(part,true),"short top tip added without turning the whole cylinder into a cone");Check(Math.Abs(ProfileEnds.Length(part,true)-20)<.001f,"initial tip length is 20 mm");
  ProfileEnds.SetLength(part,true,35);Check(Math.Abs(ProfileEnds.Length(part,true)-35)<.001f,"top tip length edits the real profile in millimetres");
  ProfileEnds.Point(part,false);Check(ProfileEnds.Tapered(part,false)&&Math.Abs(ProfileEnds.Length(part,true)-35)<.001f,"bottom tip does not alter the top");
  ProfileEnds.SetLength(part,false,12);Check(Math.Abs(ProfileEnds.Length(part,false)-12)<.001f,"bottom tip has independent length");
  var saved=JsonConvert.DeserializeObject<ProfilePart>(JsonConvert.SerializeObject(part));Check(ProfileEnds.Tapered(saved,true)&&ProfileEnds.Tapered(saved,false),"both ends survive project serialization");
  ProfileEnds.Flatten(part,true);Check(!ProfileEnds.Tapered(part,true)&&ProfileEnds.Tapered(part,false),"top cone removed while bottom stays pointed");
  ProfileEnds.Flatten(part,false);Check(!ProfileEnds.Tapered(part,false),"bottom cone removed independently");
  var flat=WorkshopState.BuildMesh(part);Check(Math.Abs(flat.bounds.size.z-original.size.z)<.000001f&&Math.Abs(flat.bounds.size.x-original.size.x)<.000001f,"flattening preserves total height and body width");UnityEngine.Object.DestroyImmediate(flat);
  ProfileEnds.Point(part,true);ProfileEnds.SetLength(part,true,100000);Check(part.Sections.Zip(part.Sections.Skip(1),(a,b)=>a.T<b.T).All(x=>x),"oversized tip stops before the neighbouring section");
  var mesh=WorkshopState.BuildMesh(part);Check(mesh.vertices.All(v=>!float.IsNaN(v.x)&&!float.IsNaN(v.y)&&!float.IsNaN(v.z)),"tip geometry remains finite");UnityEngine.Object.DestroyImmediate(mesh);
  var shortPart=new ProfilePart{Length=.02f};ProfileEnds.Point(shortPart,true);float shortTop=ProfileEnds.Length(shortPart,true);ProfileEnds.Point(shortPart,false);ProfileEnds.SetLength(shortPart,false,2);Check(Math.Abs(ProfileEnds.Length(shortPart,true)-shortTop)<.001f,"very short parts keep separate top and bottom shoulders");
  var cone=new ProfilePart();cone.Sections[1].Radius=0;float baseRadius=cone.Sections[0].Radius;ProfileEnds.Flatten(cone,false);Check(cone.Sections[0].Radius==baseRadius,"flattening the base of an existing cone does not erase its body");
  var imported=new ProfilePart{Geometry=new MeshData()};bool rejected=false;try{ProfileEnds.Flatten(imported,true);}catch(InvalidOperationException){rejected=true;}Check(rejected,"imported mesh geometry is not silently changed by profile controls");
  Debug.Log("PROFILE_ENDS_VERIFIED");EditorApplication.Exit(0);
 }catch(Exception e){Debug.LogError("PROFILE_ENDS_FAILED "+e);EditorApplication.Exit(1);}}
}
