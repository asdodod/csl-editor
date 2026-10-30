using UnityEngine;

namespace SaberWorkshop;

// Menu-space dimensions, in the same units as the game's curved UI.
public static class WorkbenchLayout
{
    public static readonly Vector2 Size=new Vector2(250,126);
    public static readonly Vector3 Position=new Vector3(0,1.2f,2);
    public static readonly Vector3 PreviewOffset=new Vector3(1.3f,.02f,-.4f);
    public const float ActualSizeScale=1f;
    public static void PlacePreviewFrame(Transform preview,Transform? wall,float zoom){preview.SetParent(wall,true);preview.SetPositionAndRotation(Vector3.zero,wall?wall!.rotation:Quaternion.identity);preview.localScale=Vector3.one*(zoom/(wall?wall!.lossyScale.x:1));}
    public const float Scale=.018f;
    public const float Radius=120;
    public static void Place(Transform wall){wall.SetPositionAndRotation(Position,Quaternion.identity);wall.localScale=Vector3.one*Scale;}
    public static Vector3 PreviewCenter(Transform? wall)=>wall?wall!.position+wall.rotation*PreviewOffset:Position+PreviewOffset;
}
