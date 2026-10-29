Shader "Hidden/ENV/ColorRecovery"
{
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        ZTest Always ZWrite Off Cull Off
        Pass
        {
            Name "WorldColorRecovery"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            int _ENVCount;
            float4 _ENVCircles[16]; // world XZ, animated radius, active ring
            float _ENVSoftness;
            float _ENVGray;
            float _ENVRingWidth;
            float _ENVRingStrength;

            half4 Frag(Varyings IN) : SV_Target
            {
                float2 uv = IN.texcoord;
                half4 col = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv);
                #if UNITY_REVERSED_Z
                    real depth = SampleSceneDepth(uv);
                    if (depth <= 0.0001) return col;
                #else
                    real depth = SampleSceneDepth(uv);
                    if (depth >= 0.9999) return col;
                    depth = lerp(UNITY_NEAR_CLIP_VALUE, 1.0, depth);
                #endif
                float3 ws = ComputeWorldSpacePosition(uv, depth, UNITY_MATRIX_I_VP);
                float revealed = 0;
                float ring = 0;
                [unroll(16)]
                for (int i = 0; i < 16; ++i)
                {
                    if (i >= _ENVCount) break;
                    float4 c = _ENVCircles[i];
                    float distanceXZ = distance(ws.xz, c.xy);
                    float fade = 1.0 - smoothstep(max(0, c.z - _ENVSoftness), c.z + _ENVSoftness, distanceXZ);
                    revealed = max(revealed, fade);
                    if (c.w > 0.5 && c.z > 0.05)
                    {
                        float width = max(_ENVRingWidth, 0.01);
                        ring = max(ring, (1.0 - smoothstep(0, width, abs(distanceXZ - c.z))) * fade);
                    }
                }
                half luminance = dot(col.rgb, half3(0.2126, 0.7152, 0.0722));
                half grayFactor = (1.0h - (half)revealed) * (half)_ENVGray;
                col.rgb = lerp(col.rgb, luminance.xxx, saturate(grayFactor));
                col.rgb += ring * _ENVRingStrength;
                return col;
            }
            ENDHLSL
        }
    }
}
