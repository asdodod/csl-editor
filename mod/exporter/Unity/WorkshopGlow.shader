Shader "Workshop/Glow" {
Properties { _Color("Color", Color)=(1,1,1,1) _Glow("Glow",Float)=1 _CustomColors("Player color",Float)=1 _MainTex("Texture",2D)="white" {} }
SubShader { Tags {"RenderType"="Opaque"} Pass {
CGPROGRAM
#pragma vertex vert
#pragma fragment frag
#pragma multi_compile_instancing
#include "UnityCG.cginc"
sampler2D _MainTex;float4 _MainTex_ST;float4 _Color;float _Glow;
struct appdata {float4 vertex:POSITION;float2 uv:TEXCOORD0;UNITY_VERTEX_INPUT_INSTANCE_ID};
struct v2f {float4 pos:SV_POSITION;float2 uv:TEXCOORD0;UNITY_VERTEX_OUTPUT_STEREO};
v2f vert(appdata v){v2f o;UNITY_SETUP_INSTANCE_ID(v);UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);o.pos=UnityObjectToClipPos(v.vertex);o.uv=TRANSFORM_TEX(v.uv,_MainTex);return o;}
float4 frag(v2f i):SV_Target {UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);float4 tex=tex2D(_MainTex,i.uv);return float4(tex.rgb*_Color.rgb,max(0,_Glow)*tex.a);}
ENDCG
} } }
