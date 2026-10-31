Shader "ENVShaderKit/FX/GemDissolve"
{
 Properties
 {
  _BaseMap("Original Color Texture",2D)="white"{}
  _BaseColor("Base Color",Color)=(0.10,0.67,1,1)
  [HDR]_EdgeColor("Dissolve Edge HDR",Color)=(0.85,1.4,2.4,1)
  _Dissolve("Dissolve 0=solid 1=gone",Range(0,1))=0
  _EdgeWidth("Edge Width",Range(0.001,0.25))=0.11
  _NoiseScale("Noise Scale",Range(1,60))=13
  _EmissionStrength("Edge Glow",Range(0,5))=2
 }
 SubShader
 {
  Tags {"RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="AlphaTest"}
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
    half4 _BaseColor,_EdgeColor;
    float _Dissolve,_EdgeWidth,_NoiseScale,_EmissionStrength;
   CBUFFER_END
   float hash21(float2 p) {return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);}
   float noise2D(float2 p)
   {
    float2 i=floor(p);float2 f=frac(p);f=f*f*(3.0-2.0*f);
    float a=hash21(i),b=hash21(i+float2(1,0)),c=hash21(i+float2(0,1)),d=hash21(i+1);
    return lerp(lerp(a,b,f.x),lerp(c,d,f.x),f.y);
   }
   struct Attributes {float3 positionOS:POSITION;float2 uv:TEXCOORD0;};
   struct Varyings {float4 positionCS:SV_POSITION;float2 uv:TEXCOORD0;float3 positionOS:TEXCOORD1;};
   Varyings vert(Attributes v) {Varyings o;o.positionCS=TransformObjectToHClip(v.positionOS);o.uv=TRANSFORM_TEX(v.uv,_BaseMap);o.positionOS=v.positionOS;return o;}
   half4 frag(Varyings i):SV_Target
   {
    float2 nUV=i.uv*_NoiseScale+i.positionOS.xz*0.75;
    float n=noise2D(nUV);
    clip(n-_Dissolve);
    half edge=1.0h-smoothstep(_Dissolve,_Dissolve+max(_EdgeWidth,0.001),n);
    half3 col=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,i.uv).rgb*_BaseColor.rgb;
    col+=_EdgeColor.rgb*edge*_EmissionStrength;
    return half4(col,1);
   }
   ENDHLSL
  }
 }
 FallBack Off
}
