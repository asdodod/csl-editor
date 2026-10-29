using System;
using System.Collections.Generic;
using UnityEngine;

namespace SaberWorkshop;

public class WorkshopProject
{
    public int Version=1;
    public string Name="New saber";
    public string SourceFile="";
    public string SourceHash="";
    public Dictionary<string,string> ShaderFiles=new Dictionary<string,string>();
    public bool LinkHands=true;
    public float SaberLength=1;public float SaberWidth=1;
    public Dictionary<string,SurfaceEdit> Surfaces=new Dictionary<string,SurfaceEdit>();
    public Dictionary<string,PartEdit> Transforms=new Dictionary<string,PartEdit>();
    public Dictionary<string,string> Labels=new Dictionary<string,string>();
    public List<ProfilePart> Parts=new List<ProfilePart>();
}
public class PartEdit
{
    public float[] Position={0,0,0};public float[] Rotation={0,0,0};public float[] Scale={1,1,1};public bool Visible=true;
}
public class SurfaceEdit
{
    public string Name="";
    public bool Reset;
    public int RenderQueue=-1;
    public string[] Keywords=Array.Empty<string>();
    public bool UsePlayerColor;
    public string Shader="";
    public Dictionary<string,float> Numbers=new Dictionary<string,float>();
    public Dictionary<string,float[]> Colors=new Dictionary<string,float[]>();
    public Dictionary<string,float[]> Vectors=new Dictionary<string,float[]>();
    public Dictionary<string,TextureEdit> Textures=new Dictionary<string,TextureEdit>();
}
public class TextureEdit
{
    public string Png="";public float[] Scale={1,1};public float[] Offset={0,0};
}
public class ProfilePart
{
    public string Id=Guid.NewGuid().ToString("N");public string Name="Part";public string Shape="profile";
    public float Length=.2f;public int Sides=32;public bool Caps=true;public string Side="both";public bool Visible=true;
    public float[] Position={0,0,0};public float[] Rotation={0,0,0};
    public List<Section> Sections=new List<Section>{new Section{T=0,Radius=.018f},new Section{T=1,Radius=.018f}};
    public SurfaceEdit Surface=new SurfaceEdit();
}
public class Section
{
    public float T;public float Radius=.01f;public float X;public float Y;
}
public sealed class OwnedAssets : MonoBehaviour
{
    public readonly List<UnityEngine.Object> Assets=new List<UnityEngine.Object>();
    public static void Release(UnityEngine.Object asset){if(!asset)return;if(Application.isPlaying)Destroy(asset);else DestroyImmediate(asset);}
    private void OnDestroy(){foreach(var asset in Assets)if(asset)Release(asset);Assets.Clear();}
}
