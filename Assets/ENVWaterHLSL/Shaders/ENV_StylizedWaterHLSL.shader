Shader "ENV/Water/StylizedWaterHLSL"
{
    Properties
    {
        // Names are the exact reference names exported by the supplied StylizedWater.shadergraph.
        [Header(Water Color)]
        _ShallowWater("ShallowWater", Color) = (0,0.60987,1,0)
        _DeepWater("DeepWater", Color) = (0.02179,0.2295,0.67925,0)
        _WaterDepth("WaterDepth", Float) = 10
        [Header(Normal)]
        _WaterSpeed("WaterSpeed", Float) = 1
        _NormalStrength("NormalStrength", Range(0,1)) = 1
        _SurfaceScale("SurfaceScale", Range(0,10)) = 1
        _Shineness("Shineness", Range(0,1)) = 1
        [Header(Refraction)]
        _RefractionStrength("RefractionStrength", Range(0,0.025)) = 0.01
        [Header(Foam)]
        _FoamColor("FoamColor", Color) = (1,1,1,1)
        _FoamAmount("FoamAmount", Range(0,50)) = 1
        _FoamCutoff("FoamCutoff", Range(0,50)) = 1
        [Header(Wave)]
        _WaveAmplitude("WaveAmplitude", Range(0,1)) = 0.1
        _WaveFrequency("WaveFrequency", Range(0,1)) = 1
        _WaveSpeed("WaveSpeed", Range(0,1)) = 1
        [Header(Interaction)]
        _InteractionRippleCutoff("InteractionRippleCutoff", Range(0,1)) = 0.25
        _RenderTexture("RenderTexture", 2D) = "black" {}
        _InteractorPosition("InteractorPosition", Vector) = (0,0,0,0)
        _RTCamera("RTCamera", Float) = 0
        [Header(Animated Emissive Ripples)]
        [NoScaleOffset]_RippleNoiseTex("Seamless Caustic Noise", 2D) = "black" {}
        [HDR]_RippleColor("Ripple Emission Color HDR", Color) = (0.4, 0.9, 1.4, 1)
        _RippleIntensity("Ripple Emission Strength", Range(0,5)) = 1.35
        _RippleTiling("Ripple World Tiling", Range(0.01,3)) = 0.22
        _RippleSpeed("Ripple Scroll XY", Vector) = (0.035, 0.025, 0, 0)
        _RippleThreshold("Ripple Line Threshold", Range(0,1)) = 0.4
        [Header(Shared River Fountain Polar Mode)]
        [Toggle] _UsePolarUV("Use Polar UV For Fountain", Float) = 0
        _PolarCenter("Polar UV Center", Vector) = (0.5,0.5,0,0)
        _PolarRadialTiling("Polar Radial Tiling", Range(0.1,40)) = 6
        _PolarAngularTiling("Polar Angular Tiling", Range(0.1,16)) = 2
        _PolarRotationSpeed("Polar Rotation Speed", Range(-2,2)) = 0.04
        _PolarRingSpeed("Polar Ring Speed", Range(-5,5)) = 0.65
        _PolarRingWidth("Polar Ring Width", Range(0.02,0.9)) = 0.24
        _PolarRingIntensity("Polar Ring Emission", Range(0,5)) = 0.55
        [Header(External SSR or Planar Reflection RT)]
        [Toggle] _UseReflectionRT("Use Reflection RenderTexture", Float) = 0
        [NoScaleOffset] _ReflectionRT("Reflection RT From External Renderer", 2D) = "black" {}
        _ReflectionIntensity("Reflection Intensity", Range(0,1)) = 0.28
        _ReflectionFresnel("Reflection Fresnel Power", Range(0.25,8)) = 3
        [Toggle] _ReflectionFlipY("Reflection RT Flip Y", Float) = 0
        [Toggle] _ReflectionUseAlpha("Reflection RT Uses Alpha As Hit Mask", Float) = 0
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent" }
        Pass
        {
            Name "UniversalForward"
            Tags { "LightMode"="UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off
            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"

            TEXTURE2D(_RenderTexture); SAMPLER(sampler_RenderTexture);
            TEXTURE2D(_RippleNoiseTex); SAMPLER(sampler_RippleNoiseTex);
            TEXTURE2D(_ReflectionRT); SAMPLER(sampler_ReflectionRT);
            CBUFFER_START(UnityPerMaterial)
                half4 _ShallowWater, _DeepWater, _FoamColor;
                float _WaterDepth, _WaterSpeed, _NormalStrength, _SurfaceScale, _Shineness;
                float _RefractionStrength, _FoamAmount, _FoamCutoff;
                float _WaveAmplitude, _WaveFrequency, _WaveSpeed;
                float _InteractionRippleCutoff, _RTCamera;
                float4 _InteractorPosition;
                float4 _RenderTexture_ST;
                half4 _RippleColor;
                float _RippleIntensity;
                float _RippleTiling;
                float4 _RippleSpeed;
                float _RippleThreshold;
                float _UsePolarUV;
                float4 _PolarCenter;
                float _PolarRadialTiling, _PolarAngularTiling;
                float _PolarRotationSpeed, _PolarRingSpeed;
                float _PolarRingWidth, _PolarRingIntensity;
                float _UseReflectionRT, _ReflectionIntensity, _ReflectionFresnel;
                float _ReflectionFlipY, _ReflectionUseAlpha;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float2 uv : TEXCOORD1;
                float4 screenPos : TEXCOORD2;
                float eyeDepth : TEXCOORD3;
            };

            // Small deterministic world-space noise. No FBX normals/tangents are used.
            float Hash21(float2 p) { return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453); }
            float Noise(float2 p)
            {
                float2 i = floor(p), f = frac(p);
                f = f*f*(3.0-2.0*f);
                return lerp(lerp(Hash21(i), Hash21(i+float2(1,0)), f.x),
                            lerp(Hash21(i+float2(0,1)), Hash21(i+1.0), f.x), f.y);
            }
            float Height(float2 worldXZ)
            {
                float t = _Time.y * _WaterSpeed;
                float scale = max(_SurfaceScale, 0.05);
                float2 p = worldXZ * (0.36*scale);
                float n1 = Noise(p + float2(t*.12, t*.07));
                float n2 = Noise(p*1.91 + float2(-t*.09, t*.18));
                return (n1+n2-1.0)*0.5;
            }
            float3 WaterNormalWorldUp(float2 worldXZ)
            {
                // A flat world-up normal when NormalStrength==0; no tangent-space conversion.
                float stepSize = 0.12;
                float dx = Height(worldXZ+float2(stepSize,0))-Height(worldXZ-float2(stepSize,0));
                float dz = Height(worldXZ+float2(0,stepSize))-Height(worldXZ-float2(0,stepSize));
                float3 n = normalize(float3(-dx*2.6, stepSize*2.0, -dz*2.6));
                return normalize(lerp(float3(0,1,0), n, saturate(_NormalStrength)));
            }
            Varyings Vert(Attributes v)
            {
                Varyings o;
                float3 ws = TransformObjectToWorld(v.positionOS.xyz);
                // Small optional vertical ripple; keep all lighting normals world-up.
                float f = max(_WaveFrequency, 0.01);
                float phase = (ws.x+ws.z*.71)*f*1.8 + _Time.y*_WaveSpeed*2.2;
                ws.y += sin(phase)*_WaveAmplitude*0.12;
                o.positionWS = ws;
                o.uv = v.uv;
                o.positionCS = TransformWorldToHClip(ws);
                o.screenPos = ComputeScreenPos(o.positionCS);
                o.eyeDepth = -TransformWorldToView(ws).z;
                return o;
            }
            half4 Frag(Varyings i) : SV_Target
            {
                float2 uv = i.screenPos.xy / max(i.screenPos.w, 0.0001);
                float rawDepth = SampleSceneDepth(uv);
                float sceneEyeDepth = LinearEyeDepth(rawDepth, _ZBufferParams);
                float waterThickness = max(0, sceneEyeDepth - i.eyeDepth);
                float depthFactor = saturate(waterThickness/max(_WaterDepth,0.001));
                float3 normalWS = WaterNormalWorldUp(i.positionWS.xz);

                float3 waterCol = lerp(_ShallowWater.rgb, _DeepWater.rgb, depthFactor);
                // Refraction uses URP Opaque Texture when enabled in the pipeline.
                float2 refractedUV = saturate(uv + normalWS.xz * _RefractionStrength);
                float3 sceneCol = SampleSceneColor(refractedUV);
                waterCol = lerp(waterCol, sceneCol, saturate(_RefractionStrength*5.0));

                float2 noiseUV = i.positionWS.xz * max(_SurfaceScale,0.05) * .5;
                float movingNoise = Noise(noiseUV+_Time.y*_WaterSpeed*.15);
                float foamReach = max(_FoamAmount*0.075,0.02);
                float edge = 1.0-smoothstep(0,foamReach,waterThickness);
                float foamPattern = smoothstep(saturate(_FoamCutoff/50.0),
                                              saturate(_FoamCutoff/50.0)+0.16,movingNoise);
                float foam = saturate(edge * (0.50+foamPattern*0.50));
                // Optional interaction texture + radial ripple, active only when RTCamera is enabled.
                float rtMask = SAMPLE_TEXTURE2D(_RenderTexture, sampler_RenderTexture,
                                        TRANSFORM_TEX(i.uv,_RenderTexture)).r;
                if (_RTCamera > 0.5)
                {
                    foam = max(foam, smoothstep(_InteractionRippleCutoff, 1.0, rtMask)*.65);
                    float d = distance(i.positionWS.xz,_InteractorPosition.xz);
                    float ring = sin(d*8.0-_Time.y*5.0)*0.5+0.5;
                    foam = max(foam, ring*exp(-d*1.2)*0.45);
                }
                waterCol = lerp(waterCol,_FoamColor.rgb,foam);

                Light light = GetMainLight(TransformWorldToShadowCoord(i.positionWS));
                float diffuse = saturate(dot(normalWS,light.direction));
                float3 h = normalize(light.direction + normalize(GetWorldSpaceViewDir(i.positionWS)));
                float spec = pow(saturate(dot(normalWS,h)),lerp(12.0,128.0,saturate(_Shineness)));
                waterCol *= (0.63+diffuse*.37)*lerp(float3(1,1,1),light.color,0.4);
                waterCol += light.color*spec*(0.1+_Shineness*0.42);

                // Two scrolling caustic samples: additive/emissive, independent of specular.
                // World-space XZ avoids stretched FBX UVs and incorrect imported normals.
                // Normal river: XZ world-space tiling. Fountain: optional polar sampling
                // mapped from the mesh UV0 (requires fountain's radial UV layout).
                float2 cuv = i.positionWS.xz * _RippleTiling;
                float fountainRadial = 0.0;
                if (_UsePolarUV > 0.5)
                {
                    float2 localUV = i.uv - _PolarCenter.xy;
                    float radius = length(localUV);
                    float angle = atan2(localUV.y, localUV.x) * (1.0 / 6.28318530718);
                    cuv = float2(frac(angle * _PolarAngularTiling + _Time.y * _PolarRotationSpeed),
                                 radius * _PolarRadialTiling - _Time.y * _PolarRingSpeed);
                    // A thicker, independent radial emissive ring complementing caustics.
                    float phase = frac(radius * _PolarRadialTiling - _Time.y * _PolarRingSpeed);
                    float w = max(_PolarRingWidth, 0.02);
                    fountainRadial = 1.0 - smoothstep(0.0, w, min(phase, 1.0 - phase));
                }
                float2 flow = _Time.y * _RippleSpeed.xy;
                float drift = Height(i.positionWS.xz) * 0.065;
                float2 distortion = float2(drift, -drift);
                float r1 = SAMPLE_TEXTURE2D(_RippleNoiseTex, sampler_RippleNoiseTex,
                                             cuv + flow + distortion).r;
                float r2 = SAMPLE_TEXTURE2D(_RippleNoiseTex, sampler_RippleNoiseTex,
                                             cuv * 1.47 - flow.yx * 0.71 - distortion).r;
                float caustic = saturate(r1 * 0.75 + r2 * 0.35);
                float threshold = saturate(_RippleThreshold);
                float rippleMask = smoothstep(threshold, min(threshold + 0.24, 1.0), caustic);
                waterCol += _RippleColor.rgb * rippleMask * _RippleIntensity;
                if (_UsePolarUV > 0.5)
                    waterCol += _RippleColor.rgb * fountainRadial * _PolarRingIntensity;

                // Only consumes an RT supplied by a separate reflection producer.
                // Sampling a black unassigned RT is disabled by default.
                if (_UseReflectionRT > 0.5 && _ReflectionIntensity > 0.001)
                {
                    float2 reflectionUV = uv;
                    if (_ReflectionFlipY > 0.5) reflectionUV.y = 1.0 - reflectionUV.y;
                    reflectionUV = saturate(reflectionUV + normalWS.xz * 0.006);
                    float4 reflectionSample = SAMPLE_TEXTURE2D(_ReflectionRT, sampler_ReflectionRT, reflectionUV);
                    float3 V = SafeNormalize(GetWorldSpaceViewDir(i.positionWS));
                    float ndotv = saturate(dot(normalWS, V));
                    float fresnel = pow(1.0 - ndotv, max(_ReflectionFresnel, 0.25));
                    float validity = (_ReflectionUseAlpha > 0.5) ? saturate(reflectionSample.a) : 1.0;
                    float w = saturate(_ReflectionIntensity * (0.2 + 0.8 * fresnel) * validity);
                    waterCol = lerp(waterCol, reflectionSample.rgb, w);
                }
                return half4(waterCol, 0.94);
            }
            ENDHLSL
        }
    }
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
