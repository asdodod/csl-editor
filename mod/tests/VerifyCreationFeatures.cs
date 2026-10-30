using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;
using SaberWorkshop;
public static class VerifyCreationFeatures {
 static void Check(bool value,string message){if(!value)throw new Exception(message);Debug.Log("CREATION_CHECK "+message);}
 public static void Run(){try{Verify();Debug.Log("CREATION_FEATURES_VERIFIED");EditorApplication.Exit(0);}catch(Exception e){Debug.LogError("CREATION_FEATURES_FAILED "+e);EditorApplication.Exit(1);}}
 static void Verify(){
  var state=WorkshopState.Instance;state.New();state.Project.Name="Empty verification";Check(state.Ready&&!state.HasGeometry,"empty preset is editable without enabling an empty gameplay model");string empty=state.Save();state.LoadProject(empty).GetAwaiter().GetResult();Check(state.Project.Created&&state.Project.Parts.Count==0,"empty preset round-trip");
  state.CreateStarter();var source=state.Project.Parts[1];var outline=JsonConvert.DeserializeObject<ProfilePart>(JsonConvert.SerializeObject(source));outline.Id=Guid.NewGuid().ToString("N");outline.Name="White outline";outline.Inverted=true;outline.OutlineThickness=.001f;outline.Surface.UsePlayerColor=false;state.Project.Parts.Add(outline);
  var body=WorkshopState.BuildMesh(source);var hull=WorkshopState.BuildMesh(outline);var a=body.triangles;var b=hull.triangles;
  Check(a.Length==b.Length&&a.Select((x,i)=>i%3==0?b[i]==x:i%3==1?b[i+1]==x:b[i-1]==x).All(x=>x),"outline triangles have reversed winding");
  Check(body.vertices.Select((v,i)=>Math.Abs(Vector3.Distance(v,hull.vertices[i])-.001f)<.00001f||body.normals[i].sqrMagnitude<.01f).All(x=>x),"outline expands by one millimetre");
  Check(body.normals.Select((n,i)=>n.sqrMagnitude<.01f||Vector3.Dot(n,hull.normals[i])<-.99f).All(x=>x),"outline normals inverted");
  state.Project.Name="Outline verification";string saved=state.Save();state.LoadProject(saved).GetAwaiter().GetResult();Check(state.Project.Parts.Last().Inverted&&Math.Abs(state.Project.Parts.Last().OutlineThickness-.001f)<.000001,"outline settings round-trip");
  var pair=state.InstantiatePair();var guide=WorkshopAssets.AttachGuide(pair);Check(guide.GetComponentsInChildren<MeshFilter>().All(f=>f.sharedMesh.GetTopology(0)==MeshTopology.Triangles&&f.sharedMesh.bounds.size.z>1&&f.sharedMesh.bounds.max.z>.9f),"FBX guide contains the full handle and blade in forward orientation");Check(pair.transform.Find("LeftSaber").GetComponentsInChildren<Renderer>().Length==6,"guide excluded from editable hand parts");guide.SetActive(false);Check(!guide.activeInHierarchy,"guide can be hidden");guide.SetActive(true);Check(guide.activeInHierarchy,"guide can be shown");
  var mat=WorkshopAssets.NewMaterial();Check(mat&&mat.HasProperty("_Cull")&&mat.GetFloat("_Cull")==2,"native material uses back-face culling");
  var imported=new ProfilePart{Geometry=MeshData.Capture(body),Inverted=true,OutlineThickness=.002f};var importedMesh=WorkshopState.BuildMesh(imported);Check(importedMesh.vertexCount==body.vertexCount,"mesh outline keeps source vertices");
  var parts=state.Project.Parts.Select(p=>{var mesh=WorkshopState.BuildMesh(p);try{return new{Part=p,Mesh=MeshData.Capture(mesh)};}finally{UnityEngine.Object.DestroyImmediate(mesh);}}).ToArray();
  File.WriteAllText(Path.Combine(WorkshopState.DataRoot,"outline-export-test.json"),JsonConvert.SerializeObject(new{Project=state.Project,Parts=parts}));
  state.New();state.Project.Name="Outline pair verification";var outer=state.AddPart("Blade",true);outer.Position=new[]{.01f,.02f,.08f};outer.Rotation=new[]{0f,10f,0f};var inner=state.CreateOutlinePair(outer);
  Check(state.Project.Parts.Count==2&&inner.Id!=outer.Id,"outline pair creates two independently selectable parts");
  Check(inner.Position.SequenceEqual(outer.Position)&&inner.Rotation.SequenceEqual(outer.Rotation)&&!ReferenceEquals(inner.Position,outer.Position),"outline pair stays aligned with independent transforms");
  var outerMesh=WorkshopState.BuildMesh(outer);var innerMesh=WorkshopState.BuildMesh(inner);Check(innerMesh.bounds.size.x<outerMesh.bounds.size.x&&innerMesh.bounds.size.y<outerMesh.bounds.size.y&&innerMesh.bounds.size.z<outerMesh.bounds.size.z,"outline inner geometry is smaller on every axis");
  Check(outer.Inverted&&inner.Inverted&&!outer.Surface.UsePlayerColor&&inner.Surface.UsePlayerColor,"outline shell and inner colour sources remain independent");
  string pairSaved=state.Save();state.LoadProject(pairSaved).GetAwaiter().GetResult();Check(state.Project.Parts.Count==2&&Math.Abs(state.Project.Parts[1].WidthScale-.86f)<.000001f,"outline pair survives save and load");UnityEngine.Object.DestroyImmediate(outerMesh);UnityEngine.Object.DestroyImmediate(innerMesh);
  UnityEngine.Object.DestroyImmediate(importedMesh);UnityEngine.Object.DestroyImmediate(mat);UnityEngine.Object.DestroyImmediate(pair);UnityEngine.Object.DestroyImmediate(body);UnityEngine.Object.DestroyImmediate(hull);
 }
}
