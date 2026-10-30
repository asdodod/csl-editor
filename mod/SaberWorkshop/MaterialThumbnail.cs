using UnityEngine;
using UnityEngine.UI;
namespace SaberWorkshop;

public sealed class MaterialThumbnail : MonoBehaviour {
 Camera camera=null!;GameObject stage=null!,shape=null!;RenderTexture target=null!;Mesh? ownedMesh;Mesh? lastSource;string lastForm="";
 public void Show(Material material,Mesh? source,string form){
  if(!stage){
   stage=new GameObject("Workshop material sample");stage.transform.position=new Vector3(50000,50000,50000);
   var cameraObject=new GameObject("Material card camera");cameraObject.transform.SetParent(stage.transform,false);camera=cameraObject.AddComponent<Camera>();camera.enabled=false;camera.cullingMask=1<<31;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.018f,.025f,.04f);camera.nearClipPlane=.01f;camera.farClipPlane=8;camera.orthographic=true;camera.orthographicSize=.65f;camera.transform.localPosition=new Vector3(0,0,-3);
   target=new RenderTexture(512,320,24,SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.RGB111110Float)?RenderTextureFormat.RGB111110Float:RenderTextureFormat.RGB565);GetComponent<RawImage>().texture=target;camera.targetTexture=target;
   shape=new GameObject("Material sample",typeof(MeshFilter),typeof(MeshRenderer));shape.layer=31;shape.transform.SetParent(stage.transform,false);
  }
  if(ownedMesh&&lastSource==source&&lastForm==form){shape.GetComponent<MeshRenderer>().sharedMaterial=material;WorkshopState.SetBounds(material,ownedMesh!.bounds);return;}
  lastSource=source;lastForm=form;
  if(ownedMesh)OwnedAssets.Release(ownedMesh!);
  if(form=="Selected part"&&source)ownedMesh=Object.Instantiate(source!);
  else if(form=="Sphere") {var sphere=GameObject.CreatePrimitive(PrimitiveType.Sphere);sphere.SetActive(false);ownedMesh=Object.Instantiate(sphere.GetComponent<MeshFilter>().sharedMesh);OwnedAssets.Release(sphere);}
  else {var part=new ProfilePart{Length=1};part.Sections[0].Radius=.045f;part.Sections[1].Radius=.045f;if(form=="Ring"){part.Shape="ring";part.Sections[0].Radius=.35f;part.Sections[1].Radius=.055f;}ownedMesh=WorkshopState.BuildMesh(part);}
  var mesh=ownedMesh!;shape.GetComponent<MeshFilter>().sharedMesh=mesh;shape.GetComponent<MeshRenderer>().sharedMaterial=material;
  WorkshopState.SetBounds(material,mesh.bounds);shape.transform.localRotation=form=="Sphere"?Quaternion.identity:Quaternion.Euler(-90,0,-15);
  float scale=1.05f/Mathf.Max(.001f,Mathf.Max(mesh.bounds.size.x,Mathf.Max(mesh.bounds.size.y,mesh.bounds.size.z)));shape.transform.localScale=Vector3.one*scale;shape.transform.localPosition=-(shape.transform.localRotation*(mesh.bounds.center*scale));
 }
 void LateUpdate(){if(camera&&stage&&gameObject.activeInHierarchy)camera.Render();}
 public void RenderNow(){if(camera)camera.Render();}
 void OnDestroy(){if(camera)camera.targetTexture=null;if(target){target.Release();OwnedAssets.Release(target);}if(ownedMesh)OwnedAssets.Release(ownedMesh!);if(stage)OwnedAssets.Release(stage);}
}
