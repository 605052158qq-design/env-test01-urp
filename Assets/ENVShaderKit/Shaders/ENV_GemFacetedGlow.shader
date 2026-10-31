Shader "ENVShaderKit/Gem/FacetedGlow"
{
 Properties
 {
  _BaseMap("Optional Base Texture",2D)="white"{}
  _BaseColor("Gem Color",Color)=(0.08,0.65,1,1)
  [HDR]_RimColor("Rim HDR Color",Color)=(0.15,0.65,1.5,1)
  _RimPower("Rim Sharpness",Range(0.5,10))=3
  _RimStrength("Rim Strength",Range(0,4))=1.25
  _Emission("Emission Strength",Range(0,5))=0.35
  _PulseSpeed("Pulse Speed",Range(0,8))=1.3
  _PulseAmount("Pulse Amount",Range(0,1))=0.15
  _ToonSteps("Facet Lighting Steps",Range(1,6))=3
  [Header(V3 Dissolve - same original gem shader)]
  _DissolveNoiseTex("Dissolve Noise R",2D)="white"{}
  _Dissolve("Dissolve 0 to 1",Range(0,1))=0
  _DissolveNoiseScale("Dissolve Noise Tiling",Range(1,50))=7
  _DissolveEdgeWidth("Dissolve Edge Width",Range(0.001,0.3))=0.09
  [HDR]_DissolveEdgeColor("Dissolve Edge Color",Color)=(0.6,1.4,2.3,1)
  _DissolveEmission("Dissolve Edge Brightness",Range(0,5))=1.5
 }
 SubShader
 {
  Tags {"RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry"}
  Pass
  {
   Name "Forward" Tags {"LightMode"="UniversalForward"}
   ZWrite On Cull Back
   HLSLPROGRAM
   #pragma target 3.5
   #pragma vertex vert
   #pragma fragment frag
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
   TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
   TEXTURE2D(_DissolveNoiseTex); SAMPLER(sampler_DissolveNoiseTex);
   CBUFFER_START(UnityPerMaterial)
    float4 _BaseMap_ST;
    half4 _BaseColor,_RimColor,_DissolveEdgeColor;
    half _RimPower,_RimStrength,_Emission,_PulseSpeed,_PulseAmount,_ToonSteps;
    float _Dissolve,_DissolveNoiseScale,_DissolveEdgeWidth,_DissolveEmission;
   CBUFFER_END
   struct Attributes {float3 positionOS:POSITION;float3 normalOS:NORMAL;float2 uv:TEXCOORD0;};
   struct Varyings {float4 positionCS:SV_POSITION;float3 positionWS:TEXCOORD0;half3 normalWS:TEXCOORD1;float2 uv:TEXCOORD2;};
   Varyings vert(Attributes v)
   {
    Varyings o;
    o.positionWS=TransformObjectToWorld(v.positionOS);
    o.positionCS=TransformWorldToHClip(o.positionWS);
    o.normalWS=TransformObjectToWorldNormal(v.normalOS);
    o.uv=TRANSFORM_TEX(v.uv,_BaseMap);
    return o;
   }
   half4 frag(Varyings i):SV_Target
   {
    float noise=SAMPLE_TEXTURE2D(_DissolveNoiseTex,sampler_DissolveNoiseTex,i.uv*_DissolveNoiseScale).r;
    // Off by default. Effect can be animated with material.SetFloat or MaterialPropertyBlock.
    if(_Dissolve>0.001)
    {
     clip(noise-_Dissolve);
    }
    half3 n=normalize(i.normalWS);
    half3 viewDir=normalize(GetWorldSpaceViewDir(i.positionWS));
    Light key=GetMainLight();
    half lightValue=saturate(dot(n,key.direction)*0.5h+0.5h);
    half steps=max(1.0h,_ToonSteps);
    half toon=floor(lightValue*steps)/steps;
    half diffuse=lerp(0.53h,1.0h,toon);
    half rim=pow(1.0h-saturate(dot(viewDir,n)),max(_RimPower,0.5h));
    half pulse=1.0h+sin(_Time.y*_PulseSpeed)*_PulseAmount;
    half3 tex=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,i.uv).rgb;
    half3 color=tex*_BaseColor.rgb*diffuse+_RimColor.rgb*(rim*_RimStrength+_Emission)*pulse;
    half dissolveEdge=(1.0h-smoothstep(_Dissolve,_Dissolve+max(_DissolveEdgeWidth,0.001),noise))*step(0.001,_Dissolve);
    color+=_DissolveEdgeColor.rgb*dissolveEdge*_DissolveEmission;
    return half4(color,1);
   }
   ENDHLSL
  }
 }
 FallBack Off
}
