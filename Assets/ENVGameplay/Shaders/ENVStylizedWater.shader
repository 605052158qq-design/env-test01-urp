Shader "ENV/StylizedFlowWaterURP"
{
    Properties
    {
        _BaseMap("Water Texture", 2D) = "white" {}
        _BaseColor("Water Color", Color) = (0.14,0.75,0.95,0.85)
        _FlowSpeed("UV Flow XY", Vector) = (0.06,0.025,0,0)
        _RippleStrength("Ripple Strength", Range(0,0.2)) = 0.035
        _RippleFrequency("Ripple Frequency", Range(0,30)) = 9
        _RippleSpeed("Ripple Speed", Range(0,10)) = 2.0
        _FoamStrength("Foam Lines", Range(0,1)) = 0.1
        _Alpha("Opacity", Range(0,1)) = 0.85
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
        LOD 100
        Pass
        {
            Name "FlowWater"
            Tags { "LightMode"="UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off Cull Back
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
            struct Attributes { float4 positionOS : POSITION; float2 uv:TEXCOORD0; };
            struct Varyings { float4 positionHCS:SV_POSITION; float2 uv:TEXCOORD0; float3 ws:TEXCOORD1; };
            Varyings vert(Attributes v)
            {
                Varyings o;
                VertexPositionInputs pos = GetVertexPositionInputs(v.positionOS.xyz);
                o.positionHCS = pos.positionCS;
                o.ws = pos.positionWS;
                o.uv = TRANSFORM_TEX(v.uv, _BaseMap);
                return o;
            }
            half4 frag(Varyings i): SV_Target
            {
                float t = _Time.y;
                float2 uv = i.uv + _FlowSpeed.xy * t;
                float wave = sin(length(i.ws.xz * 0.45) * _RippleFrequency - t * _RippleSpeed);
                float wave2 = sin(i.ws.x * (_RippleFrequency * 0.45) + i.ws.z * 1.3 + t * 1.6);
                uv += (float2(wave, wave2) * _RippleStrength);
                half4 tex = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv);
                half foam = (1.0 - smoothstep(0.7, 1.0, wave * 0.5 + 0.5)) * _FoamStrength;
                half3 color = saturate(tex.rgb * _BaseColor.rgb + foam.xxx);
                return half4(color, saturate(tex.a * _Alpha * _BaseColor.a));
            }
            ENDHLSL
        }
    }
}
