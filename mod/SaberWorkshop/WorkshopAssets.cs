using System;
using System.IO;
using System.Collections.Generic;
using System.Reflection;
using Newtonsoft.Json;
using UnityEngine;

namespace SaberWorkshop;

public static class WorkshopAssets
{
    private static Material? template;
    private static Material? guideTemplate;
    private static bool attempted;
    private static MeshData? guide;
    public static Material? NewMaterial(){
        if(!attempted){attempted=true;using(var stream=Assembly.GetExecutingAssembly().GetManifestResourceStream("SaberWorkshop.workshop-tools.bundle")){
            if(stream!=null){using(var memory=new MemoryStream()){stream.CopyTo(memory);var bundle=AssetBundle.LoadFromMemory(memory.ToArray());if(bundle){template=bundle.LoadAsset<Material>("Assets/WorkshopToolMaterial.mat");guideTemplate=bundle.LoadAsset<Material>("Assets/WorkshopGuideMaterial.mat");bundle.Unload(false);}}}
        }}return template?new Material(template!):null;
    }
    public static GameObject AttachGuide(GameObject preview){
        if(guide==null){using(var stream=Assembly.GetExecutingAssembly().GetManifestResourceStream("SaberWorkshop.size-guide.json"))using(var reader=new StreamReader(stream!))guide=JsonConvert.DeserializeObject<MeshData>(reader.ReadToEnd())!;}
        var root=new GameObject("Sizing guide");root.transform.SetParent(preview.transform,false);root.AddComponent<SizeGuideMarker>();var owner=root.AddComponent<OwnedAssets>();
        var mesh=guide!.Create();mesh.name="Complete saberMesh.fbx";owner.Assets.Add(mesh);
        var init=NewMaterial();if(init)OwnedAssets.Release(init!);var material=guideTemplate?new Material(guideTemplate!):NewMaterial()!;material.name="Size guide";owner.Assets.Add(material);
        foreach(bool left in new[]{true,false}){var go=new GameObject(left?"Left size guide":"Right size guide");go.layer=12;go.transform.SetParent(root.transform,false);go.transform.localPosition=new Vector3(left?-.18f:.18f,0,0);go.transform.localRotation=Quaternion.Euler(-90,0,0);go.AddComponent<MeshFilter>().sharedMesh=mesh;go.AddComponent<MeshRenderer>().sharedMaterial=material;}
        return root;
    }
}
public sealed class SizeGuideMarker : MonoBehaviour { }
