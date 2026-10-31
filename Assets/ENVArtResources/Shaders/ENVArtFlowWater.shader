Shader "ENVArt/Water/FlowStylized"
{
 Properties
 {
  _BaseMap("Texture",2D)="white"{}
  _BaseColor("Water Tint",Color)=(0.13,0.73,0.91,0.9)
  _FlowSpeed("Flow UV XY",Vector)=(0.05,0.02,0,0)
  _RippleStrength("Ripple Distortion",Range(0,0.2))=0.03
  _RippleFrequency("Ripple Frequency",Range(0,30))=9
  _RippleSpeed("Ripple Speed",Range(0,10))=2
  _FoamStrength("Foam Line Intensity",Range(0,1))=0.12
  _Alpha("Opacity",Range(0,1))=0.85
 }
 SubShader
 {
  Tags {"RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent"}
  Pass
  {
   Name "Water"
   Tags {"LightMode"="UniversalForward"}
   Blend SrcAlpha OneMinusSrcAlpha
   ZWrite Off
   Cull Back
   HLSLPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
   CBUFFER_START(UnityPerMaterial)
    float4 _BaseMap_ST;
    half4 _BaseColor;
    float4 _FlowSpeed;
    float _RippleStrength;
    float _RippleFrequency;
    float _RippleSpeed;
    float _FoamStrength;
    float _Alpha;
   CBUFFER_END
   struct appdata {float4 positionOS:POSITION;float2 uv:TEXCOORD0;};
   struct v2f {float4 positionCS:SV_POSITION;float2 uv:TEXCOORD0;float3 worldPos:TEXCOORD1;};
   v2f vert(appdata v){v2f o;VertexPositionInputs p=GetVertexPositionInputs(v.positionOS.xyz);o.positionCS=p.positionCS;o.worldPos=p.positionWS;o.uv=TRANSFORM_TEX(v.uv,_BaseMap);return o;}
   half4 frag(v2f i):SV_Target {
    float t=_Time.y;
    float s=sin((i.worldPos.x+i.worldPos.z)*_RippleFrequency*.3-t*_RippleSpeed);
    float2 uv=i.uv+_FlowSpeed.xy*t+float2(s,cos(i.worldPos.z*_RippleFrequency*.5-t))*_RippleStrength;
    half4 albedo=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,uv);
    half foam=pow(saturate(s*.5+.5),7)*_FoamStrength;
    return half4(saturate(albedo.rgb*_BaseColor.rgb+foam.xxx),saturate(albedo.a*_BaseColor.a*_Alpha));
   }
   ENDHLSL
  }
 }
}
