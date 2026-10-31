Shader "ENVShaderKit/Water/StylizedSurface"
{
 Properties
 {
  _ShallowColor("Shallow Color",Color)=(0.11,0.79,0.89,0.9)
  _DeepColor("Deep Color",Color)=(0.015,0.37,0.64,0.9)
  [HDR]_FoamColor("Foam Color",Color)=(0.68,0.98,1.12,1)
  _FlowSpeed("Legacy Flow XY",Vector)=(0.3,0.04,0,0)
  _WaveScale("Wave Scale",Range(0.05,10))=1.1
  _WaveSpeed("Wave Speed",Range(0,10))=1.25
  _WaveStrength("Wave Strength",Range(0,1))=0.3
  _FoamThreshold("Foam Threshold",Range(0,1))=0.8
  _FoamSoftness("Foam Softness",Range(0.01,0.5))=0.14
  _FoamStrength("Foam Strength",Range(0,2))=0.55
  _FresnelPower("Fresnel Power",Range(0.5,8))=3.5
  _FresnelStrength("Fresnel Strength",Range(0,1))=0.2
  _Opacity("Opacity",Range(0,1))=0.85
  [Header(V3 Textures)]
  _BaseMap("Main Texture",2D)="white"{}
  _MaskMap("Transparency Mask R",2D)="white"{}
  _FlowMap("Flow Map RG - linear",2D)="gray"{}
  _NormalMap("Normal Wave RGB - linear",2D)="bump"{}
  _FoamMap("Foam Map R",2D)="white"{}
  _NoiseMap("Noise Distortion R",2D)="gray"{}
  [Header(V3 Flow and Foam)]
  _FlowStrength("FlowMap Strength",Range(0,1))=0
  _FlowSpeedRate("FlowMap Cycles Per Sec",Range(0,4))=0.5
  _NormalStrength("Normal Strength",Range(0,1))=0
  _FoamTextureStrength("Foam Texture Strength",Range(0,2))=0
  _FoamContrast("Foam Texture Contrast",Range(0.5,6))=2
  _DepthFoamEnabled("Depth Intersection Foam (Needs Depth Texture)",Range(0,1))=0
  _DepthFoamDistance("Depth Foam Distance",Range(0.01,5))=0.6
  _NoiseDistortion("Noise Distortion",Range(0,0.15))=0
  [Header(V3 Fountain Polar)]
  _PolarBlend("Polar UV 0=Off 1=On",Range(0,1))=0
  _PolarCenter("Polar UV Center XY",Vector)=(0.5,0.5,0,0)
  _RadialRingStrength("Fountain Moving Rings",Range(0,1))=0
  _RadialRingDensity("Ring Density",Range(1,60))=16
  _RadialRingSpeed("Ring Speed",Range(0,6))=1.2
 }
 SubShader
 {
  Tags {"RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent"}
  Pass
  {
   Name "Forward" Tags {"LightMode"="UniversalForward"}
   Blend SrcAlpha OneMinusSrcAlpha ZWrite Off Cull Back
   HLSLPROGRAM
   #pragma target 3.5
   #pragma vertex vert
   #pragma fragment frag
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
   TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
   TEXTURE2D(_MaskMap); SAMPLER(sampler_MaskMap);
   TEXTURE2D(_FlowMap); SAMPLER(sampler_FlowMap);
   TEXTURE2D(_NormalMap); SAMPLER(sampler_NormalMap);
   TEXTURE2D(_FoamMap); SAMPLER(sampler_FoamMap);
   TEXTURE2D(_NoiseMap); SAMPLER(sampler_NoiseMap);
   CBUFFER_START(UnityPerMaterial)
    half4 _ShallowColor,_DeepColor,_FoamColor;
    float4 _FlowSpeed, _BaseMap_ST, _PolarCenter;
    float _WaveScale,_WaveSpeed,_WaveStrength,_FoamThreshold,_FoamSoftness,_FoamStrength,_FresnelPower,_FresnelStrength,_Opacity;
    float _FlowStrength,_FlowSpeedRate,_NormalStrength,_FoamTextureStrength,_FoamContrast,_DepthFoamEnabled,_DepthFoamDistance,_NoiseDistortion;
    float _PolarBlend,_RadialRingStrength,_RadialRingDensity,_RadialRingSpeed;
   CBUFFER_END
   struct Attributes {float3 positionOS:POSITION;float3 normalOS:NORMAL;float2 uv:TEXCOORD0;};
   struct Varyings {float4 positionCS:SV_POSITION;float3 positionWS:TEXCOORD0;half3 normalWS:TEXCOORD1;float2 uv:TEXCOORD2;float4 screenPos:TEXCOORD3;};
   float2 ToPolar(float2 uv,float2 center)
   {
    float2 delta=uv-center;
    return float2(atan2(delta.y,delta.x)/6.2831853+0.5, length(delta)*2.0);
   }
   Varyings vert(Attributes a)
   {
    Varyings o;
    o.positionWS=TransformObjectToWorld(a.positionOS);
    o.positionCS=TransformWorldToHClip(o.positionWS);
    o.normalWS=TransformObjectToWorldNormal(a.normalOS);
    o.uv=a.uv;
    o.screenPos=ComputeScreenPos(o.positionCS);
    return o;
   }
   half4 frag(Varyings i):SV_Target
   {
    float2 rawUV=i.uv;
    float2 uv=lerp(rawUV,ToPolar(rawUV,_PolarCenter.xy),saturate(_PolarBlend));
    float2 noiseUV=uv*2+_Time.y*float2(0.08,0.06);
    float noise=SAMPLE_TEXTURE2D(_NoiseMap,sampler_NoiseMap,noiseUV).r;
    uv += (noise-0.5)*_NoiseDistortion;
    float2 flowUV=uv;
    float2 flow=(SAMPLE_TEXTURE2D(_FlowMap,sampler_FlowMap,flowUV).rg-0.5)*2.0;
    float phase=frac(_Time.y*_FlowSpeedRate);
    float phase2=frac(phase+0.5);
    float2 panUV=uv+_Time.y*_FlowSpeed.xy*0.1;
    float2 uvA=panUV+flow*phase*_FlowStrength;
    float2 uvB=panUV+flow*phase2*_FlowStrength;
    float blend=saturate(abs(phase-0.5)*2.0);
    float2 baseA=TRANSFORM_TEX(uvA,_BaseMap);
    float2 baseB=TRANSFORM_TEX(uvB,_BaseMap);
    half4 texA=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,baseA);
    half4 texB=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,baseB);
    half4 tex=lerp(texA,texB,blend);
    float2 waveUV=i.positionWS.xz*_WaveScale+_Time.y*_FlowSpeed.xy;
    float wave=sin(waveUV.x*2.1+sin(waveUV.y*1.6)-_Time.y*_WaveSpeed);
    float wave2=sin(waveUV.y*2.8-waveUV.x*0.65+_Time.y*_WaveSpeed*0.64);
    float w=saturate(0.5+0.25*wave+0.25*wave2);
    half3 col=lerp(_DeepColor.rgb,_ShallowColor.rgb,saturate(w*_WaveStrength+0.42));
    col*=tex.rgb;
    float foamLegacy=smoothstep(_FoamThreshold-_FoamSoftness,_FoamThreshold+_FoamSoftness,w)*_FoamStrength;
    float foamA=SAMPLE_TEXTURE2D(_FoamMap,sampler_FoamMap,uvA).r;
    float foamB=SAMPLE_TEXTURE2D(_FoamMap,sampler_FoamMap,uvB).r;
    float foamPattern=pow(saturate(lerp(foamA,foamB,blend)),_FoamContrast);
    float foam=foamLegacy+foamPattern*_FoamTextureStrength;
    if(_DepthFoamEnabled>0.5)
    {
     float2 suv=i.screenPos.xy/max(i.screenPos.w,0.0001);
     float sceneDepth=SampleSceneDepth(suv);
     float sceneEye=LinearEyeDepth(sceneDepth,_ZBufferParams);
     float waterEye=i.screenPos.w;
     float intersection=saturate(1-(sceneEye-waterEye)/max(_DepthFoamDistance,0.001));
     foam+=intersection*_FoamStrength;
    }
    float radialPhase=uv.y*_RadialRingDensity-_Time.y*_RadialRingSpeed*6.2831853;
    float rings=pow(saturate(sin(radialPhase)*0.5+0.5),8.0)*_RadialRingStrength;
    foam+=rings;
    col=lerp(col,_FoamColor.rgb,saturate(foam));
    float2 normalSampleA=SAMPLE_TEXTURE2D(_NormalMap,sampler_NormalMap,uvA).rg*2-1;
    float2 normalSampleB=SAMPLE_TEXTURE2D(_NormalMap,sampler_NormalMap,uvB).rg*2-1;
    float2 normalXY=lerp(normalSampleA,normalSampleB,blend)*_NormalStrength;
    half3 n=normalize(i.normalWS+float3(normalXY.x,0,normalXY.y));
    half3 v=normalize(GetWorldSpaceViewDir(i.positionWS));
    half fresnel=pow(1.0h-saturate(dot(n,v)),_FresnelPower);
    col+=_FoamColor.rgb*fresnel*_FresnelStrength;
    half mask=SAMPLE_TEXTURE2D(_MaskMap,sampler_MaskMap,rawUV).r;
    return half4(col,saturate(_Opacity*tex.a*mask+fresnel*_FresnelStrength*0.2));
   }
   ENDHLSL
  }
 }
 FallBack Off
}
