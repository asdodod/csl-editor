using System.Collections.Generic;
using UnityEngine;
namespace SaberWorkshop;
public sealed class PreviewPresentation : MonoBehaviour {
 private static readonly Dictionary<Material,int> originalQueues=new Dictionary<Material,int>();
 private readonly List<Material> owned=new List<Material>();
 public static int OriginalQueue(Material m)=>originalQueues.TryGetValue(m,out var queue)?queue:m.renderQueue;
 public void Apply(){foreach(var r in GetComponentsInChildren<Renderer>(true)){r.sortingOrder=20;bool guide=r.GetComponentInParent<SizeGuideMarker>();foreach(var m in r.sharedMaterials){if(!m)continue;if(!originalQueues.ContainsKey(m)){originalQueues[m]=m.renderQueue;owned.Add(m);}else if(m.renderQueue!=4100&&m.renderQueue!=4101)originalQueues[m]=m.renderQueue;m.renderQueue=guide?4101:4100;}}}
 private void OnDestroy(){foreach(var m in owned)originalQueues.Remove(m);}
}
