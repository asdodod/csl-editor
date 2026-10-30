using UnityEngine;

namespace SaberWorkshop;

// Menu-space dimensions, in the same units as the game's curved UI.
public static class WorkbenchLayout
{
    public static readonly Vector2 Size=new Vector2(250,126);
    public static readonly Vector3 Position=new Vector3(0,1.2f,2);
    public static readonly Vector3 PreviewOffset=new Vector3(.82f,.13f,-1);
    public const float Scale=.018f;
    public const float Radius=120;
    public static void Place(Transform wall){wall.SetPositionAndRotation(Position,Quaternion.identity);wall.localScale=Vector3.one*Scale;}
    public static Vector3 PreviewCenter(Transform? wall)=>wall?wall!.position+wall.rotation*PreviewOffset:Position+PreviewOffset;
}
