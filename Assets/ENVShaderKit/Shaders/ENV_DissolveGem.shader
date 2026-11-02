Shader "ENVShaderKit/FX/GemDissolve"
{
    Properties
    {
        _BaseMap("Original Color Texture", 2D) = "white" {}
        [HDR]_BaseColor("Base Color HDR", Color) = (0.10, 0.67, 1, 1)

        [Header(Crystal Body)]
        _BaseAlpha("Body Transparency", Range(0,1)) = 0.42
        [HDR]_RimColor("Fresnel Edge HDR", Color) = (0.25, 0.85, 1.6, 1)
        _FresnelPower("Fresnel Power", Range(0.5,10)) = 3.2
        _RimIntensity("Fresnel Glow", Range(0,8)) = 2.3
        _RimAlphaBoost("Edge Opacity Boost", Range(0,1)) = 0.3
        _InnerGlow("Inner Fill Glow", Range(0,3)) = 0.28

        [Header(Original Dissolve Interface)]
        [HDR]_EdgeColor("Dissolve Edge HDR", Color) = (0.85, 1.4, 2.4, 1)
        _Dissolve("Dissolve 0=solid 1=gone", Range(0,1)) = 0
        _EdgeWidth("Edge Width", Range(0.001,0.25)) = 0.11
        _NoiseScale("Noise Scale", Range(1,60)) = 13
        _EmissionStrength("Dissolve Edge Glow", Range(0,5)) = 2
        [Enum(UnityEngine.Rendering.CullMode)] _Cull("Cull Mode", Float) = 2
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent" }
        Pass
        {
            Name "Forward"
            Tags { "LightMode"="UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull [_Cull]

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                half4 _RimColor;
                half4 _EdgeColor;
                float _BaseAlpha;
                float _FresnelPower;
                float _RimIntensity;
                float _RimAlphaBoost;
                float _InnerGlow;
                float _Dissolve;
                float _EdgeWidth;
                float _NoiseScale;
                float _EmissionStrength;
            CBUFFER_END

            float Hash21(float2 p)
            {
                return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453);
            }
            float Noise2D(float2 p)
            {
                float2 ip = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                float a = Hash21(ip);
                float b = Hash21(ip + float2(1, 0));
                float c = Hash21(ip + float2(0, 1));
                float d = Hash21(ip + float2(1, 1));
                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }

            struct Attributes
            {
                float3 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionOS : TEXCOORD1;
                float3 positionWS : TEXCOORD2;
                half3 normalWS : TEXCOORD3;
            };
            Varyings vert(Attributes v)
            {
                Varyings o;
                o.positionWS = TransformObjectToWorld(v.positionOS);
                o.positionCS = TransformWorldToHClip(o.positionWS);
                o.normalWS = TransformObjectToWorldNormal(v.normalOS);
                o.uv = TRANSFORM_TEX(v.uv, _BaseMap);
                o.positionOS = v.positionOS;
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                // Keep the original externally driven _Dissolve property and noise behavior.
                float n = Noise2D(i.uv * _NoiseScale + i.positionOS.xz * 0.75);
                float dissolve = saturate(_Dissolve);
                clip(n - dissolve + (1.0 - dissolve) * 0.0001);

                // Unlit crystal; the normal is used only for view-dependent Fresnel.
                float3 N = normalize(i.normalWS);
                float3 V = normalize(GetWorldSpaceViewDir(i.positionWS));
                float rim = pow(1.0 - saturate(abs(dot(N, V))), max(0.5, _FresnelPower));

                half4 baseSample = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv);
                half3 baseCol = baseSample.rgb * _BaseColor.rgb;
                half3 interior = baseCol * (1.0 + _InnerGlow * (1.0 - rim));
                half3 rimGlow = _RimColor.rgb * rim * _RimIntensity;

                float edgeWidth = max(_EdgeWidth, 0.001);
                float dissolveRim = (dissolve > 0.0001 && dissolve < 0.9999)
                    ? (1.0 - smoothstep(dissolve, dissolve + edgeWidth, n))
                    : 0.0;
                half3 dissolveGlow = _EdgeColor.rgb * dissolveRim * _EmissionStrength;

                half3 finalRGB = interior + rimGlow + dissolveGlow;
                half finalAlpha = saturate(_BaseAlpha * _BaseColor.a * baseSample.a + rim * _RimAlphaBoost);
                // Dissolve mask is a hard clip, while the surviving crystal remains translucent.
                return half4(finalRGB, finalAlpha);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
