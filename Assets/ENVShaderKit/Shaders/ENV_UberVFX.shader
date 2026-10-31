Shader "ENVShaderKit/FX/UberParticleV3"
{
 Properties
 {
  _BaseMap("Main Texture",2D)="white"{}
  _MaskMap("Mask Texture (R)",2D)="white"{}
  _NoiseMap("Noise Texture (R)",2D)="gray"{}
  _FlowMap("Flow Texture (RG)",2D)="gray"{}
  [HDR]_Color("Tint HDR",Color)=(1,1,1,1)
  _MainPan("Main Scroll XY",Vector)=(0,0.2,0,0)
  _MaskPan("Mask Scroll XY",Vector)=(0,0,0,0)
  _PolarBlend("Polar UV 0-Off 1-On",Range(0,1))=0
  _PolarCenter("Polar Center XY",Vector)=(0.5,0.5,0,0)
  _NoiseTiling("Noise Tiling",Range(0.1,25))=4
  _NoiseSpeed("Noise Speed",Vector)=(0.1,0.1,0,0)
  _Distortion("Noise Distortion",Range(0,0.25))=0.04
  _FlowStrength("Flow Map Strength",Range(0,0.5))=0
  _MaskPower("Mask Contrast",Range(0.1,8))=1
  _Dissolve("Dissolve Amount",Range(0,1))=0
  _DissolveEdgeWidth("Dissolve Soft Edge",Range(0.001,0.3))=0.1
  [HDR]_DissolveEdgeColor("Dissolve Edge Color",Color)=(1,0.6,0.15,1)
  _Opacity("Opacity",Range(0,1))=1
  _SoftParticleDistance("Soft Particles (0 Off, Requires Depth)",Range(0,5))=0
  [Enum(UnityEngine.Rendering.CullMode)] _Cull("Cull",Float)=0
 }
 SubShader
 {
  Tags {"RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True"}
  Pass
  {
   Name "Forward" Tags {"LightMode"="UniversalForward"}
   Blend SrcAlpha OneMinusSrcAlpha ZWrite Off Cull [_Cull]
   HLSLPROGRAM
   #pragma target 3.5
   #pragma vertex vert
   #pragma fragment frag
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
   TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
   TEXTURE2D(_MaskMap); SAMPLER(sampler_MaskMap);
   TEXTURE2D(_NoiseMap); SAMPLER(sampler_NoiseMap);
   TEXTURE2D(_FlowMap); SAMPLER(sampler_FlowMap);
   CBUFFER_START(UnityPerMaterial)
    float4 _BaseMap_ST,_MaskMap_ST,_MainPan,_MaskPan,_PolarCenter,_NoiseSpeed;
    half4 _Color,_DissolveEdgeColor;
    float _PolarBlend,_NoiseTiling,_Distortion,_FlowStrength,_MaskPower,_Dissolve,_DissolveEdgeWidth,_Opacity,_SoftParticleDistance,_Cull;
   CBUFFER_END
   struct Attributes {float3 positionOS:POSITION;float2 uv:TEXCOORD0;half4 color:COLOR;};
   struct Varyings {float4 positionCS:SV_POSITION;float2 uv:TEXCOORD0;half4 color:COLOR;float4 screenPos:TEXCOORD1;};
   float2 PolarUV(float2 uv,float2 center)
   {
    float2 delta=uv-center;
    return float2(atan2(delta.y,delta.x)/6.2831853+0.5,length(delta)*2.0);
   }
   Varyings vert(Attributes v)
   {
    Varyings o;
    o.positionCS=TransformObjectToHClip(v.positionOS);
    o.screenPos=ComputeScreenPos(o.positionCS);
    o.uv=v.uv;
    o.color=v.color;
    return o;
   }
   half4 frag(Varyings i):SV_Target
   {
    float2 uv=lerp(i.uv,PolarUV(i.uv,_PolarCenter.xy),_PolarBlend);
    float2 nuv=uv*_NoiseTiling+_Time.y*_NoiseSpeed.xy;
    float noise=SAMPLE_TEXTURE2D(_NoiseMap,sampler_NoiseMap,nuv).r;
    float2 flow=(SAMPLE_TEXTURE2D(_FlowMap,sampler_FlowMap,uv).rg-0.5)*2.0;
    float2 distorted=uv+(noise-0.5)*_Distortion+flow*_FlowStrength;
    float2 mainUV=TRANSFORM_TEX(distorted,_BaseMap)+_Time.y*_MainPan.xy;
    float2 maskUV=TRANSFORM_TEX(uv,_MaskMap)+_Time.y*_MaskPan.xy;
    half4 main=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,mainUV);
    half mask=pow(saturate(SAMPLE_TEXTURE2D(_MaskMap,sampler_MaskMap,maskUV).r),_MaskPower);
    half alpha=main.a*mask*_Color.a*i.color.a*_Opacity;
    half edge=0;
    if(_Dissolve>0.001)
    {
     alpha*=smoothstep(_Dissolve-_DissolveEdgeWidth,_Dissolve+_DissolveEdgeWidth,noise);
     edge=(1.0h-smoothstep(_Dissolve,_Dissolve+max(_DissolveEdgeWidth,0.001),noise));
    }
    if(_SoftParticleDistance>0.001)
    {
     float2 suv=i.screenPos.xy/max(i.screenPos.w,0.0001);
     float sceneEye=LinearEyeDepth(SampleSceneDepth(suv),_ZBufferParams);
     alpha*=saturate((sceneEye-i.screenPos.w)/_SoftParticleDistance);
    }
    half3 rgb=main.rgb*_Color.rgb*i.color.rgb+edge*_DissolveEdgeColor.rgb;
    return half4(rgb,saturate(alpha));
   }
   ENDHLSL
  }
 }
 FallBack Off
}
