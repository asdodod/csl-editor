Shader "Workshop/Particle" {
Properties {_Color("Colour",Color)=(1,1,1,1) _Glow("Bloom",Float)=1 _CustomColors("Note colours",Float)=1}
SubShader {Tags {"Queue"="Transparent" "RenderType"="Transparent"} Pass {Blend SrcAlpha One ZWrite Off Cull Off
CGPROGRAM
#pragma vertex vert
#pragma fragment frag
#pragma multi_compile_instancing
#include "UnityCG.cginc"
float4 _Color;float _Glow;struct appdata {float4 vertex:POSITION;float2 uv:TEXCOORD0;float4 color:COLOR;UNITY_VERTEX_INPUT_INSTANCE_ID};struct v2f{float4 pos:SV_POSITION;float2 uv:TEXCOORD0;float4 color:COLOR;UNITY_VERTEX_OUTPUT_STEREO};
v2f vert(appdata v){v2f o;UNITY_SETUP_INSTANCE_ID(v);UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);o.pos=UnityObjectToClipPos(v.vertex);o.uv=v.uv;o.color=v.color;return o;}
float4 frag(v2f i):SV_Target{UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);float a=pow(saturate(1-length(i.uv-.5)*2),2)*i.color.a;return float4(_Color.rgb*i.color.rgb,a*_Glow);}
ENDCG
} } }
