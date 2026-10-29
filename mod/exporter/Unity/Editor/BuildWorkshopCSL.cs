using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
public static class BuildWorkshopCSL {
 static string Arg(string key){var args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,key);if(i<0||i+1==args.Length)throw new Exception("Missing "+key);return args[i+1];}
 static float F(JToken t,float fallback=0){return t==null?fallback:(float)t;}
 static Vector3 V(JToken t){return new Vector3(F(t[0]),F(t[1]),F(t[2]));}
 public static void Run(){try{Build();Debug.Log("WORKSHOP_NATIVE_EXPORT_COMPLETE");EditorApplication.Exit(0);}catch(Exception e){Debug.LogError("WORKSHOP_NATIVE_EXPORT_FAILED "+e);EditorApplication.Exit(1);}}
 static void Build(){
  var input=JObject.Parse(File.ReadAllText(Arg("-workshopInput")));var project=input["Project"];if(!string.IsNullOrEmpty((string)project["SourceFile"]))throw new Exception("Imported bundle export is paused. Save the Workshop project.");
  AssetDatabase.Refresh();var shader=AssetDatabase.LoadAssetAtPath<Shader>("Assets/WorkshopGlow.shader");if(!shader)throw new Exception("Missing Workshop shader");
  if(AssetDatabase.IsValidFolder("Assets/Generated"))AssetDatabase.DeleteAsset("Assets/Generated");AssetDatabase.CreateFolder("Assets","Generated");
  var root=new GameObject("_CustomSaber");var descriptor=root.AddComponent<SaberDescriptor>();descriptor.SaberName=(string)project["Name"];descriptor.AuthorName="Creator";descriptor.Description="Created in Saber Workshop";
  try{int index=0;foreach(string side in new[]{"LeftSaber","RightSaber"}){
   var hand=new GameObject(side).transform;hand.SetParent(root.transform,false);hand.localScale=new Vector3(F(project["SaberWidth"],1),F(project["SaberWidth"],1),F(project["SaberLength"],1));
   foreach(var item in input["Parts"]){var part=item["Part"];string handSide=(string)part["Side"]??"both";if(handSide!="both"&&handSide!=(side=="LeftSaber"?"left":"right"))continue;
    var surface=part["Surface"];string sourceShader=(string)surface["Shader"]??"";
    if(sourceShader!="BeatSaber/Unlit Glow"&&sourceShader!="Unlit/Color"&&sourceShader!="Workshop/Glow"&&!sourceShader.StartsWith("Custom/Saber"))throw new Exception("Native export currently supports basic glow materials. Unsupported: "+sourceShader);
    var data=item["Mesh"];var positions=data["Positions"].Values<float>().ToArray();var normals=data["Normals"].Values<float>().ToArray();var uv=data["UV"].Values<float>().ToArray();int count=positions.Length/3;
    var mesh=new Mesh{name=(string)part["Name"],indexFormat=UnityEngine.Rendering.IndexFormat.UInt32};mesh.vertices=Enumerable.Range(0,count).Select(i=>new Vector3(positions[i*3],positions[i*3+1],positions[i*3+2])).ToArray();mesh.normals=Enumerable.Range(0,count).Select(i=>new Vector3(normals[i*3],normals[i*3+1],normals[i*3+2])).ToArray();mesh.uv=Enumerable.Range(0,count).Select(i=>new Vector2(uv[i*2],uv[i*2+1])).ToArray();mesh.triangles=data["Indices"].Values<int>().ToArray();mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,"Assets/Generated/Mesh"+index+".asset");
    var mat=new Material(shader){name=(string)surface["Name"]??"Material"};var color=surface["Colors"]?["_Color"];mat.SetColor("_Color",color==null?Color.white:new Color(F(color[0]),F(color[1]),F(color[2]),F(color[3],1)));mat.SetFloat("_Glow",F(surface["Numbers"]?["_Glow"],1));mat.SetFloat("_CustomColors",(bool?)surface["UsePlayerColor"]==true?1:0);
    var textures=surface["Textures"] as JObject;if(textures!=null&&textures.Properties().Any(x=>x.Name!="_MainTex"&& !string.IsNullOrEmpty((string)x.Value["Png"])))throw new Exception("This export supports the Main texture slot.");
    var tex=textures?["_MainTex"];if(tex!=null){string png=(string)tex["Png"];if(!string.IsNullOrEmpty(png)){var texture=new Texture2D(2,2,TextureFormat.RGBA32,false);if(!texture.LoadImage(Convert.FromBase64String(png)))throw new Exception("Invalid image");AssetDatabase.CreateAsset(texture,"Assets/Generated/Texture"+index+".asset");mat.mainTexture=texture;}mat.mainTextureScale=new Vector2(F(tex["Scale"]?[0],1),F(tex["Scale"]?[1],1));mat.mainTextureOffset=new Vector2(F(tex["Offset"]?[0]),F(tex["Offset"]?[1]));}
    AssetDatabase.CreateAsset(mat,"Assets/Generated/Material"+index+".mat");var go=new GameObject((string)part["Name"]);go.transform.SetParent(hand,false);go.transform.localPosition=V(part["Position"]);go.transform.localEulerAngles=V(part["Rotation"]);go.AddComponent<MeshFilter>().sharedMesh=mesh;go.AddComponent<MeshRenderer>().sharedMaterial=mat;go.SetActive((bool?)part["Visible"]!=false);index++;
   }
  }
  PrefabUtility.SaveAsPrefabAsset(root,"Assets/_CustomSaber.prefab");AssetDatabase.SaveAssets();string folder=Arg("-workshopOutput");Directory.CreateDirectory(folder);
  var result=BuildPipeline.BuildAssetBundles(folder,new[]{new AssetBundleBuild{assetBundleName="workshop.saber",assetNames=new[]{"Assets/_CustomSaber.prefab"}}},BuildAssetBundleOptions.ForceRebuildAssetBundle|BuildAssetBundleOptions.ChunkBasedCompression,BuildTarget.StandaloneWindows64);if(!result||!File.Exists(Path.Combine(folder,"workshop.saber")))throw new Exception("AssetBundle build failed");
  }finally{UnityEngine.Object.DestroyImmediate(root);}
 }
}
