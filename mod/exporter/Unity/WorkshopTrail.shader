Shader "Workshop/Trail" {
Properties {_Color("Colour",Color)=(1,1,1,1) _MainTex("Optional image",2D)="white" {} _Glow("Bloom",Float)=1 _CustomColors("Note colours",Float)=1}
SubShader {Tags {"Queue"="Transparent" "RenderType"="Transparent"} Pass {Blend SrcAlpha One ZWrite Off Cull Off
CGPROGRAM
#pragma vertex vert
#pragma fragment frag
#pragma multi_compile_instancing
#include "UnityCG.cginc"
float4 _Color,_MainTex_ST;float _Glow;sampler2D _MainTex;
struct appdata {float4 vertex:POSITION;float2 uv:TEXCOORD0;float4 color:COLOR;UNITY_VERTEX_INPUT_INSTANCE_ID};struct v2f {float4 pos:SV_POSITION;float2 uv:TEXCOORD0;float4 color:COLOR;UNITY_VERTEX_OUTPUT_STEREO};
v2f vert(appdata v){v2f o;UNITY_SETUP_INSTANCE_ID(v);UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);o.pos=UnityObjectToClipPos(v.vertex);o.uv=v.uv;o.color=v.color;return o;}
float4 frag(v2f i):SV_Target{UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);float4 image=tex2D(_MainTex,i.uv*_MainTex_ST.xy+_MainTex_ST.zw);float softEdge=smoothstep(0,.08,i.uv.y)*smoothstep(0,.08,1-i.uv.y);return float4(image.rgb*_Color.rgb*i.color.rgb,image.a*_Color.a*i.color.a*softEdge*_Glow);}
ENDCG
} } }
