using System;
using SaberWorkshop;
using UnityEditor;
using UnityEngine;

public static class VerifyWorkbenchLayout
{
    public static void Run(){try{Verify();Debug.Log("WORKBENCH_FPFC_POSE_VERIFIED");EditorApplication.Exit(0);}catch(Exception e){Debug.LogError("WORKBENCH_POSE_FAILED "+e);EditorApplication.Exit(1);}}
    static void Verify(){
        var go=new GameObject("FPFC workbench",typeof(RectTransform));
        try{
            var rect=(RectTransform)go.transform;rect.sizeDelta=WorkbenchLayout.Size;
            rect.SetPositionAndRotation(new Vector3(0,-3,0),Quaternion.Euler(45,10,0));
            WorkbenchLayout.Place(rect);
            var corners=new Vector3[4];rect.GetWorldCorners(corners);
            foreach(var corner in corners)if(corner.y<.05f)throw new Exception("Workbench intersects floor: "+corner);
            if(rect.position!=new Vector3(0,1.2f,2)||rect.rotation!=Quaternion.identity)throw new Exception("Incorrect static menu pose");
            var preview=WorkbenchLayout.PreviewCenter(rect);
            if(preview.y<1||preview.z>=rect.position.z)throw new Exception("Preview is below floor or behind wall");
            if((WorkbenchLayout.PreviewCenter(null)-preview).sqrMagnitude>.00001f)throw new Exception("Camera-less fallback differs from static pose");
            if(WorkbenchLayout.Radius!=120)throw new Exception("Wrong curved canvas radius");
            var size=new Vector3(.4f,1.2f,.1f);float fit=WorkbenchLayout.PreviewFit(new Bounds(Vector3.zero,size));if(size.y*fit>WorkbenchLayout.PreviewSize.y+.0001f||size.x*fit>WorkbenchLayout.PreviewSize.x+.0001f)throw new Exception("Preview does not fit reserved area");
            Debug.Log("Workbench bottom="+corners[0].y+"; preview="+preview+"; radius="+WorkbenchLayout.Radius);
        }finally{UnityEngine.Object.DestroyImmediate(go);}
    }
}
