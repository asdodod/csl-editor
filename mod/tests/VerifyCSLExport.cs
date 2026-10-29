using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using Object=UnityEngine.Object;

public static class VerifyCSLExport
{
    public static void Run(){
        string folder=Path.GetFullPath(Path.Combine(Application.dataPath,"../../../.local/workshop-test-game/CustomSabers/Workshop"));
        try{foreach(var file in Directory.GetFiles(folder,"*.saber")){
            var bundle=AssetBundle.LoadFromFile(file);if(!bundle)throw new Exception("Unity rejected "+file);
            try{var prefab=bundle.LoadAsset<GameObject>("_CustomSaber");if(!prefab)throw new Exception("Missing prefab");var instance=Object.Instantiate(prefab);try{
                var renderers=instance.GetComponentsInChildren<MeshRenderer>(false);if(renderers.Length<2)throw new Exception("Missing active meshes");
                foreach(var renderer in renderers.Where(r=>r.enabled)){var filter=renderer.GetComponent<MeshFilter>();if(!filter)continue;var mesh=filter.sharedMesh;if(!mesh||mesh.vertexCount==0)throw new Exception("Missing renderable mesh data: "+renderer.name);if(mesh.triangles.Any(i=>i<0||i>=mesh.vertexCount))throw new Exception("Invalid triangle indices");}
                Debug.Log("CSL_EXPORT_VERIFIED "+Path.GetFileName(file)+" active meshes="+renderers.Length);
            }finally{Object.DestroyImmediate(instance);}}finally{bundle.Unload(true);}
        }Debug.Log("CSL_EXPORT_VERIFICATION_COMPLETE");}catch(Exception e){Debug.LogError("CSL_EXPORT_VERIFICATION_FAILED "+e);EditorApplication.Exit(1);}
    }
}
