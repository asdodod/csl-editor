using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;
using SaberWorkshop;
using Object=UnityEngine.Object;

public static class VerifyStudio {
 static void Check(bool value,string message){if(!value)throw new Exception(message);Debug.Log("STUDIO_CHECK "+message);}
 public static void Run(){try{Verify();Debug.Log("WORKSHOP_STUDIO_VERIFIED");EditorApplication.Exit(0);}catch(Exception e){Debug.LogError("STUDIO_FAILED "+e);EditorApplication.Exit(1);}}
 static void Verify(){
  var state=WorkshopState.Instance;state.New();state.Project.Name="Studio validation";
  var blade=state.AddPart("Flow blade",true);blade.Length=.7f;blade.WidthScale=1.25f;blade.DepthScale=2;blade.HeightScale=1.5f;blade.Sections.ForEach(s=>{s.Radius=.04f;s.Aspect=.4f;});
  var mesh=WorkshopState.BuildMesh(blade);Check((mesh.bounds.size-new Vector3(.1f,.064f,1.05f)).sqrMagnitude<.000001f,"width depth and height scale independently");
  var clone=JsonConvert.DeserializeObject<ProfilePart>(JsonConvert.SerializeObject(blade));clone.Inverted=true;clone.Id=Guid.NewGuid().ToString("N");clone.WidthScale*=.86f;clone.DepthScale*=.86f;clone.HeightScale*=.99f;
  var inner=WorkshopState.BuildMesh(clone);Check(clone.Position.SequenceEqual(blade.Position)&&clone.Rotation.SequenceEqual(blade.Rotation)&&inner.bounds.size.x<mesh.bounds.size.x&&inner.bounds.size.y<mesh.bounds.size.y&&inner.bounds.size.z<mesh.bounds.size.z,"inverted copy stays aligned while dimensions shrink");Object.DestroyImmediate(inner);Object.DestroyImmediate(mesh);
  var effect=WorkshopAssets.EffectMaterial();blade.Surface=WorkshopState.CaptureMaterial(effect);blade.Surface.UsePlayerColor=false;Object.DestroyImmediate(effect);
  var emitter=state.AddPart("Particles",true);emitter.Particles=new ParticleEdit{Rate=60,Life=1.5f,Size=.008f,Speed=.12f};var pm=WorkshopAssets.EffectMaterial(true);emitter.Surface=WorkshopState.CaptureMaterial(pm);emitter.Surface.UsePlayerColor=true;Object.DestroyImmediate(pm);
  var trailPart=state.AddPart("Trail",true);trailPart.Trail=new TrailEdit{Width=.12f,Time=.27f,Taper=.4f};trailPart.Position[2]=.5f;trailPart.Rotation[0]=90;var trailMaterial=WorkshopAssets.TrailMaterial();trailPart.Surface=WorkshopState.CaptureMaterial(trailMaterial);Object.DestroyImmediate(trailMaterial);
  var saved=state.Save();state.LoadProject(saved).GetAwaiter().GetResult();blade=state.Project.Parts[0];Check(blade.Sections.Count==2&&blade.DepthScale==2&&blade.Sections[0].Aspect==.4f&&state.Project.Parts[1].Particles.Rate==60,"dimensions material and particle settings survive save and load");
  var pair=state.InstantiatePair();var system=pair.GetComponentsInChildren<ParticleSystem>()[0];system.Simulate(.5f,true,true,true);Check(system.particleCount>0&&system.particleCount<100,"particle emitter produces particles at the configured rate");Check(Math.Abs(system.main.startSize.constant-.008f)<.00001f,"particle size is applied");int live=system.particleCount;ParticleFactory.Configure(system,new ParticleEdit{Rate=20,Life=1.5f,Size=.01f},false);Check(system.particleCount==live,"editing particles retains live simulation");
  var trail=pair.GetComponentsInChildren<TrailRenderer>()[0];Check(Math.Abs(trail.widthMultiplier-.12f)<.00001f&&Math.Abs(trail.time-.27f)<.00001f&&Math.Abs(trail.widthCurve.Evaluate(1)-.4f)<.00001f,"trail width fade time and taper are independent and survive save/load");trail.AddPositions(new[]{new Vector3(-.2f,.3f,0),new Vector3(0,.35f,0),new Vector3(.2f,.4f,0)});var baked=new Mesh();var trailCamera=new GameObject("Trail geometry check").AddComponent<Camera>();trailCamera.transform.position=new Vector3(0,.3f,-2);trail.BakeMesh(baked,trailCamera,true);Object.DestroyImmediate(trailCamera.gameObject);Check(baked.vertexCount>0,"trail creates a ribbon without any texture images");Object.DestroyImmediate(baked);int points=trail.positionCount;TrailFactory.Configure(trail,new TrailEdit{Width=.2f,Time=.3f});Check(trail.positionCount==points&&points>=3,"changing trail settings retains its path");
  WorkshopState.ApplyPlayerColor(pair.transform.Find("LeftSaber").gameObject,Color.cyan);Check(system.GetComponent<ParticleSystemRenderer>().sharedMaterial.color==Color.cyan,"particle material follows note colour");
  var presentation=pair.AddComponent<PreviewPresentation>();var renderer=pair.GetComponentsInChildren<MeshRenderer>()[0];int original=renderer.sharedMaterial.renderQueue;presentation.Apply();Check(WorkshopState.CaptureMaterial(renderer.sharedMaterial).RenderQueue==original&&renderer.sharedMaterial.renderQueue==4100,"preview draw order is excluded from saved materials");
  var guide=WorkshopAssets.AttachGuide(pair);presentation.Apply();Check(guide.GetComponentsInChildren<Renderer>().All(r=>r.sharedMaterial.renderQueue==4099),"FBX guide has a separate preview draw order");guide.SetActive(false);
  var camera=new GameObject("Studio render").AddComponent<Camera>();camera.transform.position=new Vector3(0,.525f,-2);camera.orthographic=true;camera.orthographicSize=.65f;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;
  var target=new RenderTexture(384,768,24);camera.targetTexture=target;
  foreach(int mode in new[]{1,2}){
   foreach(var r in pair.GetComponentsInChildren<MeshRenderer>()){var m=r.sharedMaterial;m.SetFloat("_Mode",mode);m.SetFloat("_PreviewTime",0);}
   var first=Render(camera,target);foreach(var r in pair.GetComponentsInChildren<MeshRenderer>())r.sharedMaterial.SetFloat("_PreviewTime",1.4f);var second=Render(camera,target);
   int lit=first.Count(c=>c.maxColorComponent>.04f),changed=first.Zip(second,(a,b)=>Math.Abs(a.r-b.r)+Math.Abs(a.g-b.g)+Math.Abs(a.b-b.b)>.04f).Count(x=>x);
   Check(lit>500&&changed>100,(mode==1?"plasma":"volume")+" renders and flows over time: lit="+lit+" changed="+changed);
  }
  // Snapshot uses production geometry and surfaces, then passes through the real Unity exporter.
  var parts=state.Project.Parts.Select(p=>{Mesh m=p.Particles==null&&p.Trail==null?WorkshopState.BuildMesh(p):null;try{return new{Part=p,Mesh=m?MeshData.Capture(m):null};}finally{if(m)Object.DestroyImmediate(m);}}).ToArray();
  File.WriteAllText(Path.Combine(WorkshopState.DataRoot,"studio-export-test.json"),JsonConvert.SerializeObject(new{Project=state.Project,Parts=parts}));
  RenderTexture.active=null;Object.DestroyImmediate(target);Object.DestroyImmediate(camera.gameObject);Object.DestroyImmediate(pair);
 }
 static Color[] Render(Camera camera,RenderTexture rt){camera.Render();RenderTexture.active=rt;var image=new Texture2D(rt.width,rt.height,TextureFormat.RGBA32,false);image.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0);image.Apply();var pixels=image.GetPixels();Object.DestroyImmediate(image);return pixels;}
}
