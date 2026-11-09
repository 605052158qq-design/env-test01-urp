Shader "ENVShaderKit/Water/FallingSheetDownward"
{
 Properties
 {
  _BaseColor("Waterfall Base",Color)=(0.08,0.70,0.96,0.72)
  [HDR]_HighlightColor("White Highlights",Color)=(0.70,0.96,1.2,1)
  _FallSpeed("Downward Flow Speed",Range(0,8))=2.2
  _StreakDensity("Vertical Streaks",Range(1,40))=16
  _StreakStrength("Streak Strength",Range(0,1))=0.65
  _Breakup("Flow Breakup",Range(0,1))=0.3
  _Opacity("Opacity",Range(0,1))=0.75
  [Header(V3 Textures)]
  _BaseMap("Main Texture",2D)="white"{}
  _MaskMap("Transparency Mask R",2D)="white"{}
  _NoiseMap("Noise Texture R",2D)="gray"{}
  _FoamMap("Streak Foam Texture R",2D)="white"{}
  _NoiseScale("Noise Scale",Range(0.2,20))=3
  _NoiseStrength("Noise Warp Strength",Range(0,0.25))=0.07
  _NoiseSpeed("Noise Speed",Range(0,4))=0.6
  _FoamTexStrength("Foam Texture Intensity",Range(0,2))=0.4
  _MaskPower("Mask Contrast",Range(0.2,5))=1
 }
 SubShader
 {
  Tags {"RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent"}
  Pass
  {
   Name "Forward" Tags {"LightMode"="UniversalForward"}
   Blend SrcAlpha OneMinusSrcAlpha ZWrite Off Cull Off
   HLSLPROGRAM
   #pragma target 3.5
   #pragma vertex vert
   #pragma fragment frag
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
   TEXTURE2D(_MaskMap); SAMPLER(sampler_MaskMap);
   TEXTURE2D(_NoiseMap); SAMPLER(sampler_NoiseMap);
   TEXTURE2D(_FoamMap); SAMPLER(sampler_FoamMap);
   CBUFFER_START(UnityPerMaterial)
    half4 _BaseColor,_HighlightColor;
    float4 _BaseMap_ST;
    float _FallSpeed,_StreakDensity,_StreakStrength,_Breakup,_Opacity;
    float _NoiseScale,_NoiseStrength,_NoiseSpeed,_FoamTexStrength,_MaskPower;
   CBUFFER_END
   struct Attributes { float3 positionOS:POSITION; float2 uv:TEXCOORD0; };
   struct Varyings { float4 positionCS:SV_POSITION; float2 uv:TEXCOORD0; };
   Varyings vert(Attributes v) { Varyings o; o.positionCS=TransformObjectToHClip(v.positionOS); o.uv=v.uv; return o; }
   half4 frag(Varyings i):SV_Target
   {
    float2 uv=i.uv; // The generated mesh uses UV.y = 0 at its top and 1 at its bottom.
    float t=_Time.y*_FallSpeed;
    float2 noiseUV=float2(uv.x*_NoiseScale,uv.y*_NoiseScale-_Time.y*_NoiseSpeed);
    float n1=SAMPLE_TEXTURE2D(_NoiseMap,sampler_NoiseMap,noiseUV).r;
    float n2=SAMPLE_TEXTURE2D(_NoiseMap,sampler_NoiseMap,noiseUV*1.9+float2(0.37,0.13)).r;
    float warp=(n1-0.5)*_NoiseStrength+(n2-0.5)*_Breakup*0.09;
    float2 flowingUV=float2(uv.x+warp,uv.y-t*0.15);
    half4 baseTex=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,TRANSFORM_TEX(flowingUV,_BaseMap));
    float streak=pow(saturate(sin((flowingUV.x+warp)*_StreakDensity*6.283185)*0.5+0.5),8.0);
    float fall=sin(flowingUV.y*38.0-t*3.0+flowingUV.x*14.0)*0.5+0.5;
    float foamTex=SAMPLE_TEXTURE2D(_FoamMap,sampler_FoamMap,flowingUV*float2(1.2,2.0)).r;
    float sparkle=saturate(streak*_StreakStrength+fall*0.12+foamTex*_FoamTexStrength*(n1*0.65+n2*0.35));
    float edge=smoothstep(0.0,0.06,uv.x)*(1.0-smoothstep(0.94,1.0,uv.x));
    float mask=pow(saturate(SAMPLE_TEXTURE2D(_MaskMap,sampler_MaskMap,uv).r),_MaskPower);
    float mainFlowMask=dot(baseTex.rgb,float3(0.299,0.587,0.114));
    half3 col=lerp(_BaseColor.rgb,_HighlightColor.rgb,sparkle)*baseTex.rgb;
    half alpha=saturate(_Opacity*_BaseColor.a*edge*mask*baseTex.a*mainFlowMask*(0.60+sparkle*0.65));
    return half4(col,alpha);
   }
   ENDHLSL
  }
 }
 FallBack Off
}
