Shader "Workshop/Effect" {
Properties {
_Color("Base colour",Color)=(0.2,0.02,0.6,1) _Accent("Accent",Color)=(0.9,0.2,1,1)
_MainTex("Image",2D)="white" {} _Mode("Effect",Float)=2 _Scale("Pattern scale",Float)=4 _Speed("Flow speed",Float)=0.3 _Depth("Volume depth",Float)=1 _Brightness("Brightness",Float)=1.5 _Glow("Bloom",Float)=1 _CustomColors("Note colours",Float)=0 _Flow("Direction",Vector)=(0,0,1,0) _BoundsMin("Bounds minimum",Vector)=(0,0,0,0) _BoundsSize("Bounds size",Vector)=(1,1,1,0) [Enum(UnityEngine.Rendering.CullMode)] _Cull("Cull",Float)=2
[HideInInspector] _PreviewTime("Preview time override",Float)=-1
}
SubShader {Tags {"RenderType"="Opaque"} Pass {Cull [_Cull]
CGPROGRAM
#pragma target 3.0
#pragma vertex vert
#pragma fragment frag
#pragma multi_compile_instancing
#include "UnityCG.cginc"
float4 _Color,_Accent,_Flow,_BoundsMin,_BoundsSize;float _Mode,_Scale,_Speed,_Depth,_Brightness,_Glow,_PreviewTime;sampler2D _MainTex;float4 _MainTex_ST;
struct appdata {float4 vertex:POSITION;float2 uv:TEXCOORD0;UNITY_VERTEX_INPUT_INSTANCE_ID};
struct v2f {float4 pos:SV_POSITION;float3 local:TEXCOORD0;float2 uv:TEXCOORD1;UNITY_VERTEX_OUTPUT_STEREO};
v2f vert(appdata v){v2f o;UNITY_SETUP_INSTANCE_ID(v);UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);o.pos=UnityObjectToClipPos(v.vertex);o.local=v.vertex.xyz;o.uv=TRANSFORM_TEX(v.uv,_MainTex);return o;}
float hash(float3 p){return frac(sin(dot(p,float3(127.1,311.7,74.7)))*43758.5453);}
float noise(float3 p){float3 i=floor(p),f=frac(p);f=f*f*(3-2*f);return lerp(lerp(lerp(hash(i),hash(i+float3(1,0,0)),f.x),lerp(hash(i+float3(0,1,0)),hash(i+float3(1,1,0)),f.x),f.y),lerp(lerp(hash(i+float3(0,0,1)),hash(i+float3(1,0,1)),f.x),lerp(hash(i+float3(0,1,1)),hash(i+1),f.x),f.y),f.z);}
float cloud(float3 p){return .57*noise(p)+.28*noise(p*2.07+3.1)+.15*noise(p*4.13+7.7);}
float4 frag(v2f i):SV_Target {UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);float t=(_PreviewTime<0?_Time.y:_PreviewTime)*_Speed;float3 p=(i.local-_BoundsMin.xyz)/max(abs(_BoundsSize.xyz),.00001);float3 flow=_Flow.xyz*t;
if(_Mode<.5){float4 tex=tex2D(_MainTex,i.uv-float2(_Flow.x,_Flow.z)*t);return float4(tex.rgb*_Color.rgb*_Brightness,tex.a*_Glow);}
if(_Mode<1.5){float n=cloud(p*_Scale-flow);float vein=pow(saturate(1-abs(sin((p.z*_Scale+p.x*2+n*1.8-flow.z)*6.283))),14);float3 c=lerp(_Color.rgb*.1,_Accent.rgb,vein)+_Color.rgb*n*.25;return float4(c*_Brightness,_Glow*saturate(vein+n*.2));}
float3 camera=mul(unity_WorldToObject,float4(_WorldSpaceCameraPos,1)).xyz;
float3 origin=(camera-_BoundsMin.xyz)/max(abs(_BoundsSize.xyz),.00001);float3 ray=normalize(p-origin);
float3 inv=1/(ray+float3(abs(ray.x)<.00001?.00001:0,abs(ray.y)<.00001?.00001:0,abs(ray.z)<.00001?.00001:0));
float3 ta=-origin*inv,tb=(1-origin)*inv;float3 nearBox=min(ta,tb),farBox=max(ta,tb);float entry=max(0,max(nearBox.x,max(nearBox.y,nearBox.z)));float exit=min(farBox.x,min(farBox.y,farBox.z));
float3 sum=0;float density=0;
[unroll] for(int k=0;k<20;k++){float distance=lerp(entry,exit,(k+.5)/20.0);float3 q=origin+ray*distance;float3 v=q*_Scale*float3(1,_Depth,1)-flow;float n=cloud(v);float fog=pow(saturate((n-.25)*1.5),2);float star=pow(saturate(1-length(frac(v*9)-.5)*8),8)*step(.72,hash(floor(v*9)));sum+=lerp(_Color.rgb,_Accent.rgb,saturate(n*1.5))*fog*.1+star*.5;density+=fog*.06+star;}
return float4(sum*_Brightness,_Glow*saturate(density));}
ENDCG
} } }
