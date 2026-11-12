Shader "ENVShaderKit/Water/FallingSheetDownward"
{
    // The vertical prefab's V runs from lip (0) to impact point (1).
    // Style lives in fixed shader constants rather than material inspector sliders.
    Properties
    {
        [NoScaleOffset] _BaseMap("Flow texture (optional)", 2D) = "white" {}
        [NoScaleOffset] _MaskMap("Edge mask (optional)", 2D) = "white" {}
        [NoScaleOffset] _NoiseMap("Warp noise", 2D) = "gray" {}
        [NoScaleOffset] _FoamMap("Foam wisps", 2D) = "white" {}
        [Header(Motion Controls)]
        _FlowSpeed("White Flow Speed", Range(0.0, 1.5)) = 0.30
        _DistortionStrength("Distortion Strength", Range(0.0, 0.30)) = 0.11
        _DistortionSpeed("Distortion Speed", Range(0.0, 0.50)) = 0.04
        _WhiteStreakSpacing("White Streak Spacing", Range(0.6, 2.2)) = 1.0
        [Header(Colours)]
        [HDR] _DeepColor("Deep Water", Color) = (0.025, 0.34, 0.62, 1)
        [HDR] _AquaColor("Aqua Water", Color) = (0.06, 0.76, 0.93, 1)
        [HDR] _FoamColor("Foam Highlight", Color) = (0.82, 1.22, 1.35, 1)
        [Header(Opacity)]
        _BodyOpacity("Water Body Opacity", Range(0.0, 1.0)) = 0.58
        _WhiteOpacity("White Flow Opacity", Range(0.0, 1.0)) = 0.72
        [Header(Texture Tiling)]
        _FlowTextureTiling("Flow Texture Tiling XY", Vector) = (1.0, 1.35, 0, 0)
        _NoiseTextureTiling("Noise Texture Tiling XY", Vector) = (2.15, 3.6, 0, 0)
        _FoamTextureTiling("Foam Texture Tiling XY", Vector) = (1.5, 2.45, 0, 0)
        [Header(Ribbon Shape)]
        _RibbonDensity("Ribbon Density", Range(2.0, 18.0)) = 8.5
        _RibbonNoiseWarp("Ribbon Noise Warp", Range(0.0, 2.0)) = 0.82
        _RibbonCenterJitter("Ribbon Center Jitter", Range(0.0, 0.5)) = 0.25
        _RibbonWidthMin("Ribbon Width Min", Range(0.01, 0.45)) = 0.10
        _RibbonWidthMax("Ribbon Width Max", Range(0.01, 0.45)) = 0.29
        _RibbonWobbleAmplitude("Ribbon Wobble", Range(0.0, 0.12)) = 0.035
        _RibbonWobbleFrequency("Ribbon Wobble Frequency", Range(0.0, 20.0)) = 7.0
        _RibbonWobbleVariation("Ribbon Wobble Variation", Range(0.0, 20.0)) = 5.0
        _RibbonEdgeSoftness("Ribbon Edge Softness", Range(0.001, 0.20)) = 0.075
        [Header(Breakup and Edges)]
        _BreakupMin("White Breakup Min", Range(0.0, 1.0)) = 0.27
        _BreakupMax("White Breakup Max", Range(0.0, 1.0)) = 0.76
        _SideFadeMin("Side Fade Min", Range(0.0, 0.10)) = 0.005
        _SideFadeMax("Side Fade Max", Range(0.02, 0.25)) = 0.105
        _TopFade("Top Fade", Range(0.0, 0.25)) = 0.09
        _BottomFade("Bottom Fade", Range(0.0, 0.25)) = 0.12
        _LowerFoamStrength("Bottom Foam Strength", Range(0.0, 1.5)) = 0.72
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent" }
        Pass
        {
            Name "StylizedWaterfallDownward"
            Tags { "LightMode"="UniversalForward" }
            Blend One OneMinusSrcAlpha
            ZWrite Off
            Cull Off
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            TEXTURE2D(_BaseMap);  SAMPLER(sampler_BaseMap);
            TEXTURE2D(_MaskMap);  SAMPLER(sampler_MaskMap);
            TEXTURE2D(_NoiseMap); SAMPLER(sampler_NoiseMap);
            TEXTURE2D(_FoamMap);  SAMPLER(sampler_FoamMap);
            CBUFFER_START(UnityPerMaterial)
                float _FlowSpeed;
                float _DistortionStrength;
                float _DistortionSpeed;
                float _WhiteStreakSpacing;
                float4 _DeepColor;
                float4 _AquaColor;
                float4 _FoamColor;
                float _BodyOpacity;
                float _WhiteOpacity;
                float4 _FlowTextureTiling;
                float4 _NoiseTextureTiling;
                float4 _FoamTextureTiling;
                float _RibbonDensity;
                float _RibbonNoiseWarp;
                float _RibbonCenterJitter;
                float _RibbonWidthMin;
                float _RibbonWidthMax;
                float _RibbonWobbleAmplitude;
                float _RibbonWobbleFrequency;
                float _RibbonWobbleVariation;
                float _RibbonEdgeSoftness;
                float _BreakupMin;
                float _BreakupMax;
                float _SideFadeMin;
                float _SideFadeMax;
                float _TopFade;
                float _BottomFade;
                float _LowerFoamStrength;
            CBUFFER_END
            struct Attributes { float3 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; };
            Varyings vert(Attributes input) { Varyings output; output.positionCS = TransformObjectToHClip(input.positionOS); output.uv = input.uv; return output; }

            float Hash11(float p) { p=frac(p*0.1031); p*=p+33.33; p*=p+p; return frac(p); }
            float FlowRibbon(float x, float flowY, float noise)
            {
                // Larger spacing means fewer bright ribbons and wider gaps.
                float lanes=x*(_RibbonDensity/_WhiteStreakSpacing)+noise*_RibbonNoiseWarp, laneId=floor(lanes), lane=frac(lanes)-0.5;
                float seed=Hash11(laneId+17.0), centre=(Hash11(laneId+4.0)-0.5)*_RibbonCenterJitter;
                float width=lerp(_RibbonWidthMin,_RibbonWidthMax,seed)+sin(flowY*(_RibbonWobbleFrequency+seed*_RibbonWobbleVariation)+seed*12.0)*_RibbonWobbleAmplitude;
                return 1.0-smoothstep(width,width+_RibbonEdgeSoftness,abs(lane-centre));
            }

            half4 frag(Varyings input) : SV_Target
            {
                float2 uv=input.uv;
                float time=_Time.y*_FlowSpeed;
                float distortionTime=_Time.y*_DistortionSpeed;
                float2 nUV=uv*_NoiseTextureTiling.xy+float2(0.0,-distortionTime);
                float n0=SAMPLE_TEXTURE2D(_NoiseMap,sampler_NoiseMap,nUV).r;
                float n1=SAMPLE_TEXTURE2D(_NoiseMap,sampler_NoiseMap,nUV*2.37+float2(0.31,0.17)).r;
                float warp=(n0-0.5)*_DistortionStrength+(n1-0.5)*(_DistortionStrength*0.36);
                float flowY=uv.y-time+(n0-0.5)*0.055;
                float2 flowUV=float2(uv.x+warp,flowY);
                float3 flowTexture=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,flowUV*_FlowTextureTiling.xy).rgb;
                float mainFlow=dot(flowTexture,float3(0.299,0.587,0.114));
                float foamNoise=SAMPLE_TEXTURE2D(_FoamMap,sampler_FoamMap,flowUV*_FoamTextureTiling.xy+float2(0.0,-distortionTime*0.42)).r;
                float edgeMask=SAMPLE_TEXTURE2D(_MaskMap,sampler_MaskMap,uv).r;
                float wideRibbon=FlowRibbon(flowUV.x*0.92,flowY*0.72,n0);
                float fineRibbon=FlowRibbon(flowUV.x*1.76+0.19,flowY*1.31,n1);
                float broken=smoothstep(_BreakupMin,_BreakupMax,n0*0.64+n1*0.36);
                float whiteFlow=saturate(fineRibbon*broken*(0.54+foamNoise*0.78));
                float blueFlow=saturate(wideRibbon*(0.35+mainFlow*0.68)+fineRibbon*0.22);
                float side=min(uv.x,1.0-uv.x);
                float topFade=smoothstep(0.0,_TopFade*(0.5+n0*0.5),uv.y);
                float bottomFade=1.0-smoothstep(1.0-_BottomFade*(0.65+n1*0.35),1.0,uv.y);
                float coverage=smoothstep(_SideFadeMin+n1*0.035,_SideFadeMax+n0*0.045,side)*topFade*bottomFade*edgeMask;
                float lowerFoam=smoothstep(0.78,1.0,uv.y)*smoothstep(0.36,0.68,foamNoise);
                whiteFlow=saturate(whiteFlow+lowerFoam*_LowerFoamStrength);
                float body=saturate(0.18+blueFlow*0.92+mainFlow*0.18);
                float alpha=coverage*saturate(_BodyOpacity*body+whiteFlow*_WhiteOpacity);
                float3 colour=lerp(_DeepColor.rgb,_AquaColor.rgb,saturate(blueFlow*1.18+mainFlow*0.16));
                colour=lerp(colour,_FoamColor.rgb,whiteFlow);
                return half4(colour*alpha,alpha);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
