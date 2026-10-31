Shader "ENVShaderKit/FX/EmissivePulse"
{
 Properties
 {
  _BaseMap("Optional Texture",2D)="white"{}
  _BaseColor("Base Color",Color)=(0.9,0.6,0.22,1)
  [HDR]_EmissionColor("Emission HDR Color",Color)=(1.6,0.65,0.18,1)
  _EmissionStrength("Emission Strength",Range(0,8))=1.65
  _PulseSpeed("Pulse Speed",Range(0,8))=0.55
  _PulseAmplitude("Pulse Amount",Range(0,1))=0.06
 }
 SubShader
 {
  Tags {"RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry"}
  Pass
  {
   Name "Forward" Tags {"LightMode"="UniversalForward"}
   ZWrite On Cull Back
   HLSLPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
   CBUFFER_START(UnityPerMaterial)
    float4 _BaseMap_ST;
    half4 _BaseColor,_EmissionColor;
    half _EmissionStrength,_PulseSpeed,_PulseAmplitude;
   CBUFFER_END
   struct Attributes {float3 positionOS:POSITION;float2 uv:TEXCOORD0;};
   struct Varyings {float4 positionCS:SV_POSITION;float2 uv:TEXCOORD0;};
   Varyings vert(Attributes v) {Varyings o;o.positionCS=TransformObjectToHClip(v.positionOS);o.uv=TRANSFORM_TEX(v.uv,_BaseMap);return o;}
   half4 frag(Varyings i):SV_Target
   {
    half3 tex=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,i.uv).rgb;
    half p=1.0h+sin(_Time.y*_PulseSpeed)*_PulseAmplitude;
    half3 color=tex*_BaseColor.rgb+_EmissionColor.rgb*_EmissionStrength*p;
    return half4(color,1);
   }
   ENDHLSL
  }
 }
 FallBack Off
}
