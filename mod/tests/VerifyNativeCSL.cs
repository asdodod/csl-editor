using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
public static class VerifyNativeCSL {
 public static void Run(){try{Check();Debug.Log("NATIVE_CSL_RENDER_VERIFIED");EditorApplication.Exit(0);}catch(Exception e){Debug.LogError("NATIVE_CSL_RENDER_FAILED "+e);EditorApplication.Exit(1);}}
 static void Check(){
 var args=Environment.GetCommandLineArgs();string path=args[Array.IndexOf(args,"-workshopBundle")+1];var bundle=AssetBundle.LoadFromFile(path);if(!bundle)throw new Exception("Bundle rejected");
 var prefab=bundle.LoadAsset<GameObject>("_CustomSaber");if(!prefab||!prefab.GetComponent<SaberDescriptor>())throw new Exception("Descriptor not recognized");
 var root=UnityEngine.Object.Instantiate(prefab);if(root.GetComponentsInChildren<MonoBehaviour>(true).Any(x=>!x))throw new Exception("Missing script");
 if(root.GetComponentsInChildren<MeshRenderer>().Length!=10)throw new Exception("Expected five parts per hand");
 foreach(var filter in root.GetComponentsInChildren<MeshFilter>()){var mesh=filter.sharedMesh;if(mesh.vertexCount==0||mesh.triangles.Any(i=>i<0||i>=mesh.vertexCount))throw new Exception("Invalid mesh");}
 root.transform.rotation=Quaternion.Euler(-90,0,0);root.transform.Find("LeftSaber").localPosition=new Vector3(-.12f,0,0);root.transform.Find("RightSaber").localPosition=new Vector3(.12f,0,0);
 var camera=new GameObject("Render verification").AddComponent<Camera>();camera.transform.position=new Vector3(0,.4f,-2);camera.orthographic=true;camera.orthographicSize=.8f;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;
 var rt=new RenderTexture(256,512,24);camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;var image=new Texture2D(256,512,TextureFormat.RGBA32,false);image.ReadPixels(new Rect(0,0,256,512),0,0);image.Apply();int lit=image.GetPixels().Count(c=>Mathf.Max(c.r,Mathf.Max(c.g,c.b))>.05f);if(lit<100)throw new Exception("Blank render: "+lit);Debug.Log("Native bundle rendered pixels="+lit);File.WriteAllBytes(Path.Combine(Path.GetDirectoryName(path),"Flux-render.png"),image.EncodeToPNG());
 RenderTexture.active=null;UnityEngine.Object.DestroyImmediate(image);UnityEngine.Object.DestroyImmediate(camera.gameObject);UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(root);bundle.Unload(true);
 }
}
