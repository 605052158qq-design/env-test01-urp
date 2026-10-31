Shader "ENVArt/FX/ParticleAdditive"
{
 Properties { _MainTex("Sprite",2D)="white"{} _TintColor("Tint",Color)=(1,1,1,1) }
 SubShader
 {
  Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent" "IgnoreProjector"="True" }
  Pass
  {
   Name "AdditiveParticle"
   Tags { "LightMode"="UniversalForward" }
   Blend SrcAlpha One
   ZWrite Off
   Cull Off
   HLSLPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
   CBUFFER_START(UnityPerMaterial)
    float4 _TintColor;
   CBUFFER_END
   struct appdata { float4 positionOS:POSITION; half4 color:COLOR; float2 uv:TEXCOORD0; };
   struct v2f { float4 positionCS:SV_POSITION; half4 color:COLOR; float2 uv:TEXCOORD0; };
   v2f vert(appdata v) {v2f o; o.positionCS=TransformObjectToHClip(v.positionOS.xyz); o.color=v.color; o.uv=v.uv; return o;}
   half4 frag(v2f i):SV_Target {return SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,i.uv)*i.color*_TintColor;}
   ENDHLSL
  }
 }
}
