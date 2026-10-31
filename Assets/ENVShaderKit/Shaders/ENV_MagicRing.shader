Shader "ENVShaderKit/FX/MagicRing"
{
 Properties
 {
  [HDR]_RingColor("Ring HDR Color",Color)=(0.9,1.3,2,1)
  _Radius("Radius in UV",Range(0,0.75))=0.32
  _Width("Band Width",Range(0.005,0.25))=0.035
  _Softness("Softness",Range(0.002,0.15))=0.035
  _Intensity("Intensity",Range(0,5))=1.5
  _AutoExpandSpeed("Auto Expand Speed (0=manual)",Range(0,2))=0
 }
 SubShader
 {
  Tags {"RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent" "IgnoreProjector"="True"}
  Pass
  {
   Name "Forward" Tags {"LightMode"="UniversalForward"}
   Blend SrcAlpha One ZWrite Off Cull Off
   HLSLPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   CBUFFER_START(UnityPerMaterial)
    half4 _RingColor;
    float _Radius,_Width,_Softness,_Intensity,_AutoExpandSpeed;
   CBUFFER_END
   struct Attributes {float3 positionOS:POSITION;float2 uv:TEXCOORD0;};
   struct Varyings {float4 positionCS:SV_POSITION;float2 uv:TEXCOORD0;};
   Varyings vert(Attributes v) { Varyings o;o.positionCS=TransformObjectToHClip(v.positionOS);o.uv=v.uv;return o; }
   half4 frag(Varyings i):SV_Target
   {
    float r=length(i.uv-0.5);
    float radius=(_AutoExpandSpeed>0.001)?frac(_Time.y*_AutoExpandSpeed)*0.62:_Radius;
    float outer=1.0-smoothstep(radius,radius+max(_Softness,0.002),r);
    float inner=smoothstep(radius-_Width-_Softness,radius-_Width,r);
    float ring=saturate(outer*inner);
    return half4(_RingColor.rgb*_Intensity,ring*_RingColor.a);
   }
   ENDHLSL
  }
 }
 FallBack Off
}
