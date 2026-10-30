using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;
using UnityEditor;
public static class PrepareWorkshopAssets {
 public static void Run(){try{
  AssetDatabase.Refresh();var importer=(ModelImporter)AssetImporter.GetAtPath("Assets/Reference/saberMesh.fbx");importer.isReadable=true;importer.SaveAndReimport();
  var asset=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Reference/saberMesh.fbx");var instance=UnityEngine.Object.Instantiate(asset);
  var filters=instance.GetComponentsInChildren<MeshFilter>();var combine=filters.Select(f=>new CombineInstance{mesh=f.sharedMesh,transform=f.transform.localToWorldMatrix}).ToArray();var mesh=new Mesh();mesh.CombineMeshes(combine,true,true);mesh.RecalculateBounds();
  var b=mesh.bounds;Debug.Log("FBX_REFERENCE_BOUNDS "+b+" root rotation "+instance.transform.eulerAngles);
  var size=b.size;int axis=size.z>=size.x&&size.z>=size.y?2:size.y>=size.x?1:0;var rotation=Quaternion.FromToRotation(axis==2?Vector3.forward:axis==1?Vector3.up:Vector3.right,Vector3.forward);
  mesh.vertices=mesh.vertices.Select(v=>rotation*v).ToArray();mesh.normals=mesh.normals.Select(n=>rotation*n).ToArray();mesh.RecalculateBounds();Debug.Log("REFERENCE_ALIGNED_BOUNDS "+mesh.bounds);
  var args=Environment.GetCommandLineArgs();string output=args[Array.IndexOf(args,"-workshopAssets")+1];Directory.CreateDirectory(output);
  File.WriteAllText(Path.Combine(output,"size-guide.json"),JsonConvert.SerializeObject(new{Positions=mesh.vertices.SelectMany(v=>new[]{v.x,v.y,v.z}),Normals=mesh.normals.SelectMany(v=>new[]{v.x,v.y,v.z}),UV=mesh.uv.SelectMany(v=>new[]{v.x,v.y}),Indices=mesh.triangles}));
  UnityEngine.Object.DestroyImmediate(instance);UnityEngine.Object.DestroyImmediate(mesh);
  var shader=AssetDatabase.LoadAssetAtPath<Shader>("Assets/WorkshopGlow.shader");var material=new Material(shader);AssetDatabase.CreateAsset(material,"Assets/WorkshopToolMaterial.mat");AssetDatabase.SaveAssets();
  var manifest=BuildPipeline.BuildAssetBundles(output,new[]{new AssetBundleBuild{assetBundleName="workshop-tools.bundle",assetNames=new[]{"Assets/WorkshopToolMaterial.mat"}}},BuildAssetBundleOptions.ForceRebuildAssetBundle|BuildAssetBundleOptions.ChunkBasedCompression,BuildTarget.StandaloneWindows64);
  if(!manifest)throw new Exception("Tool bundle build failed");Debug.Log("WORKSHOP_ASSETS_READY");EditorApplication.Exit(0);
 }catch(Exception e){Debug.LogError(e);EditorApplication.Exit(1);}}
}
