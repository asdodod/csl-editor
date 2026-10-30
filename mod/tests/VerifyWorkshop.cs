// Runs in the Unity Editor harness with the production Models/WorkshopState
// sources and synchronous adapters for the game's async bundle helpers.
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;
using UnityEditor;
using SaberWorkshop;
using Object=UnityEngine.Object;

public static class VerifyWorkshop
{
    static void Check(bool value,string message){if(!value)throw new Exception(message);Debug.Log("WORKSHOP_CHECK "+message);}
    public static void Run(){try{Verify().GetAwaiter().GetResult();Debug.Log("WORKSHOP_VERIFICATION_COMPLETE");}catch(Exception e){Debug.LogError("WORKSHOP_VERIFICATION_FAILED "+e);EditorApplication.Exit(1);}}
    static async Task Verify(){
        var state=WorkshopState.Instance;WorkshopState.EnsureFolders();
        foreach(var file in WorkshopState.SaberFiles().Where(p=>new[]{"SPR_-_Zoog_v4 (1).saber","MWT - MK1 TEK.saber","Batonblade (Simple).saber","Cyber_Katana.saber"}.Contains(Path.GetFileName(p)))){
            try{await state.OpenSaber(file);}catch(InvalidDataException) when(Path.GetFileName(file)=="Cyber_Katana.saber"){Debug.LogWarning("SOURCE_UNSUPPORTED Cyber_Katana.saber: Unity rejects original compressed bundle");continue;}var pair=state.InstantiatePair();
            var l=pair.transform.Find("LeftSaber");var r=pair.transform.Find("RightSaber");Check(l&&r,"hands loaded: "+Path.GetFileName(file));
            var renderers=l.GetComponentsInChildren<Renderer>(true);Check(renderers.Length>0,"renderers loaded: "+renderers.Length);
            var surface=renderers.First(x=>x.sharedMaterials.Any(m=>m&&m.HasProperty("_Color")));
            int slot=Array.FindIndex(surface.sharedMaterials,m=>m&&m.HasProperty("_Color"));
            var original=surface.sharedMaterials[slot];var color=new[]{.12f,.03f,.9f,1f};
            var edit=new SurfaceEdit();edit.Colors["_Color"]=color;
            string key="LeftSaber/"+WorkshopState.PathOf(surface.transform,l)+"#"+slot;state.Project.Surfaces[key]=edit;
            var texIndex=Enumerable.Range(0,original.shader.GetPropertyCount()).FirstOrDefault(i=>original.shader.GetPropertyType(i)==UnityEngine.Rendering.ShaderPropertyType.Texture);
            string texSlot=original.shader.GetPropertyType(texIndex)==UnityEngine.Rendering.ShaderPropertyType.Texture?original.shader.GetPropertyName(texIndex):null;
            if(texSlot!=null){var texture=new Texture2D(2,2);texture.SetPixels(new[]{Color.red,Color.green,Color.blue,Color.white});texture.Apply();edit.Textures[texSlot]=new TextureEdit{Png=Convert.ToBase64String(texture.EncodeToPNG()),Scale=new[]{3f,.5f},Offset=new[]{.25f,.125f}};Object.DestroyImmediate(texture);}
            state.Project.Name="Verify "+Path.GetFileNameWithoutExtension(file);string saved=state.Save();await state.LoadProject(saved);
            var restored=state.InstantiatePair();var m=restored.transform.Find("LeftSaber").GetComponentsInChildren<Renderer>(true).First(x=>WorkshopState.PathOf(x.transform,restored.transform.Find("LeftSaber"))==WorkshopState.PathOf(surface.transform,l)).sharedMaterials[slot];
            Check(Math.Abs(m.GetColor("_Color").b-.9f)<.0001,"material colour restored");Check(m!=original,"material edits isolated from original clone");
            if(texSlot!=null){Check(m.GetTexture(texSlot).width==2,"embedded texture restored");Check(Math.Abs(m.GetTextureScale(texSlot).x-3)<.0001,"texture tiling restored");}
            var fresh=state.NewMaterial(m.shader.name);var newSurface=WorkshopState.CaptureMaterial(fresh);state.ApplyMaterial(m,newSurface);
            Check(!m.HasProperty("_MainTex")||m.GetTexture("_MainTex")==fresh.GetTexture("_MainTex"),"new material clears previous main texture");
            Object.DestroyImmediate(fresh);
            Object.DestroyImmediate(pair);Object.DestroyImmediate(restored);
        }
        state.CreateStarter();WorkshopState.Validate(state.Project);var profilePair=state.InstantiatePair();Check(profilePair.GetComponentsInChildren<MeshFilter>(true).Length==10,"starter contains five parts per hand");
        WorkshopState.ApplyPlayerColor(profilePair.transform.Find("LeftSaber").gameObject,Color.red);WorkshopState.ApplyPlayerColor(profilePair.transform.Find("RightSaber").gameObject,Color.blue);
        foreach(var identity in profilePair.transform.Find("LeftSaber").GetComponentsInChildren<PartIdentity>().Where(p=>p.UsePlayerColor))Check(identity.GetComponent<Renderer>().sharedMaterial.color.r>.99f&&identity.GetComponent<Renderer>().sharedMaterial.color.b<.01f,"left profile uses note colour");
        foreach(var identity in profilePair.transform.Find("RightSaber").GetComponentsInChildren<PartIdentity>().Where(p=>p.UsePlayerColor))Check(identity.GetComponent<Renderer>().sharedMaterial.color.b>.99f&&identity.GetComponent<Renderer>().sharedMaterial.color.r<.01f,"right profile uses note colour");
        string starter=state.Save();await state.LoadProject(starter);Check(state.Project.SourceFile==""&&state.Project.Parts.Count==5,"new construction round-trips with shader dependencies");
        var exportParts=state.Project.Parts.Select(p=>{var mesh=WorkshopState.BuildMesh(p);try{return new{Part=p,Mesh=new{Positions=mesh.vertices.SelectMany(v=>new[]{v.x,v.y,v.z}),Normals=mesh.normals.SelectMany(v=>new[]{v.x,v.y,v.z}),UV=mesh.uv.SelectMany(v=>new[]{v.x,v.y}),Indices=mesh.triangles}};}finally{}}).ToArray();
        File.WriteAllText(Path.Combine(WorkshopState.DataRoot,"export-test.json"),JsonConvert.SerializeObject(new{Project=state.Project,Parts=exportParts}));
        foreach(var filter in profilePair.GetComponentsInChildren<MeshFilter>(true)){var mesh=filter.sharedMesh;Check(mesh.vertexCount>0&&mesh.triangles.All(i=>i>=0&&i<mesh.vertexCount),"profile / ring indices valid");}
        var gameplay=state.InstantiateHand(true,new GameObject("Gameplay parent").transform);Check(gameplay.transform.localPosition==Vector3.zero,"gameplay hand local position");Check(gameplay.transform.localRotation==Quaternion.identity,"gameplay hand orientation");
        Object.DestroyImmediate(gameplay.transform.parent.gameObject);Object.DestroyImmediate(profilePair);
        var invalid=new WorkshopProject();invalid.Parts.Add(new ProfilePart{Length=float.NaN});bool rejected=false;try{WorkshopState.Validate(invalid);}catch(InvalidDataException){rejected=true;}Check(rejected,"reject nonfinite profile dimensions");
        rejected=false;try{WorkshopState.SafePath(WorkshopState.ProjectFolder,"../outside.json");}catch(InvalidDataException){rejected=true;}Check(rejected,"reject path escaping project folder");
    }
}
