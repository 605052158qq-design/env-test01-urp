Shader "Hidden/ENV/WaterSSRTrace"
{
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent" }
        Pass
        {
            Name "TraceWaterReflection"
            ZWrite Off
            ZTest Always
            Cull Off
            Blend Off

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"

            float _ENVSSRSteps;
            float _ENVSSRMaxDistance;
            float _ENVSSRThickness;
            float _ENVSSROffset;
            float _ENVSSREdgeFade;
            float _ENVSSRDebugMode;

            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float4 screenPosition : TEXCOORD1;
            };
            Varyings Vert(Attributes v)
            {
                Varyings o;
                o.positionWS = TransformObjectToWorld(v.positionOS.xyz);
                o.positionCS = TransformWorldToHClip(o.positionWS);
                o.screenPosition = ComputeScreenPos(o.positionCS);
                return o;
            }

            half4 Frag(Varyings i) : SV_Target
            {
                // Mode 1 isolates missing mesh registration or a missing draw call.
                if (_ENVSSRDebugMode > 0.5 && _ENVSSRDebugMode < 1.5)
                    return half4(1, 0, 1, 1);
                float2 startUV = i.screenPosition.xy / max(i.screenPosition.w, 1e-5);
                float opaqueDepth = LinearEyeDepth(SampleSceneDepth(startUV), _ZBufferParams);
                float waterEyeDepth = -TransformWorldToView(i.positionWS).z;
                // Don't trace if opaque geometry in front of the water fully occludes this pixel.
                clip(opaqueDepth + 0.045 - waterEyeDepth);
                // Mode 2 isolates depth-occlusion rejection.
                if (_ENVSSRDebugMode > 1.5 && _ENVSSRDebugMode < 2.5)
                    return half4(0, 1, 1, 1);

                // Explicit world-up normal: ignore the model's potentially incorrect vertex normals.
                float3 normalWS = float3(0,1,0);
                float3 towardSurface = normalize(i.positionWS - _WorldSpaceCameraPos.xyz);
                float3 reflectedRay = normalize(reflect(towardSurface, normalWS));
                float3 origin = i.positionWS + normalWS * _ENVSSROffset;

                int steps = (int)clamp(_ENVSSRSteps, 8.0, 64.0);
                float maxDistance = max(_ENVSSRMaxDistance, 1.0);
                float previousT = 0.0;
                float hit = 0.0;
                float2 hitUV = 0;

                [loop]
                for (int s = 0; s < steps; s++)
                {
                    float t = pow(((float)s + 1.0) / steps, 1.45) * maxDistance;
                    float3 rayWS = origin + reflectedRay * t;
                    float4 projected = TransformWorldToHClip(rayWS);
                    if (projected.w <= 0.0001) break;
                    float4 projectedScreen = ComputeScreenPos(projected);
                    float2 uv = projectedScreen.xy / projectedScreen.w;
                    if (any(uv < 0.002) || any(uv > 0.998)) break;
                    float sceneRaw = SampleSceneDepth(uv);
                    #if UNITY_REVERSED_Z
                    if (sceneRaw < 0.00001) { previousT = t; continue; }
                    #else
                    if (sceneRaw > 0.99999) { previousT = t; continue; }
                    #endif
                    float depthScene = LinearEyeDepth(sceneRaw, _ZBufferParams);
                    float depthRay = -TransformWorldToView(rayWS).z;
                    float delta = depthRay - depthScene;
                    if (delta >= 0.0 && delta <= _ENVSSRThickness)
                    {
                        hitUV = uv;
                        hit = 1.0;
                        break;
                    }
                    // A large depth crossing can be refined to avoid missing thin geometry.
                    if (delta > _ENVSSRThickness && previousT > 0.0)
                    {
                        float lo = previousT, hi = t;
                        [unroll(4)] for (int k = 0; k < 4; k++)
                        {
                            float mid = (lo + hi) * 0.5;
                            float4 c = TransformWorldToHClip(origin + reflectedRay * mid);
                            if (c.w <= 0.0001) break;
                            float4 sp = ComputeScreenPos(c);
                            float2 q = sp.xy / sp.w;
                            if (any(q < 0.002) || any(q > 0.998)) break;
                            float dScene = LinearEyeDepth(SampleSceneDepth(q), _ZBufferParams);
                            float dRay = -TransformWorldToView(origin + reflectedRay * mid).z;
                            if (dRay >= dScene) { hi = mid; hitUV = q; } else lo = mid;
                        }
                        if (hitUV.x > 0 && hitUV.y > 0) hit = 1.0;
                        break;
                    }
                    previousT = t;
                }
                // Mode 3: green=ray hit, red=miss. SSR miss is black-alpha in normal mode.
                if (_ENVSSRDebugMode > 2.5)
                    return hit > 0.5 ? half4(0, 1, 0, 1) : half4(1, 0, 0, 1);
                if (hit < 0.5) return half4(0,0,0,0);
                float edge = min(min(hitUV.x, hitUV.y), min(1.0-hitUV.x, 1.0-hitUV.y));
                float edgeFade = smoothstep(0, max(_ENVSSREdgeFade, 0.0001), edge);
                return half4(SampleSceneColor(hitUV), edgeFade);
            }
            ENDHLSL
        }
    }
    Fallback Off
}
