using HMUI;
using UnityEngine;
using UnityEngine.UI;
namespace SaberWorkshop;

// RawImage does not emit the curve channel used by Beat Saber's UI shader.
// Preserve its RenderTexture and BSML binding, but subdivide the card and emit
// the same radius channel as ImageView so the material preview follows the wall.
[RequireComponent(typeof(RawImage))]
public sealed class CurvedRawImageMesh : BaseMeshEffect {
 readonly CurvedCanvasSettingsHelper settings=new CurvedCanvasSettingsHelper();
 protected override void OnEnable(){base.OnEnable();settings.Reset();}
 public override void ModifyMesh(VertexHelper mesh){
  if(!IsActive())return;var image=(RawImage)graphic;var curve=settings.GetCurvedCanvasSettings(image.canvas);if(!curve||curve.radius<=0)return;
  var rect=image.GetPixelAdjustedRect();var uv=image.uvRect;int segments=Mathf.Max(1,Mathf.CeilToInt(rect.width/10));mesh.Clear();
  for(int i=0;i<=segments;i++){
   float u=(float)i/segments;
   for(int edge=0;edge<2;edge++){var vertex=UIVertex.simpleVert;vertex.position=new Vector3(Mathf.Lerp(rect.xMin,rect.xMax,u),edge==0?rect.yMin:rect.yMax);vertex.color=image.color;vertex.uv0=new Vector2(Mathf.Lerp(uv.xMin,uv.xMax,u),edge==0?uv.yMin:uv.yMax);vertex.uv1=new Vector2(u,1-edge);vertex.uv2=new Vector2(curve.radius,0);mesh.AddVert(vertex);}
   if(i>0){int a=(i-1)*2;mesh.AddTriangle(a,a+1,a+2);mesh.AddTriangle(a+2,a+1,a+3);}
  }
 }
}
