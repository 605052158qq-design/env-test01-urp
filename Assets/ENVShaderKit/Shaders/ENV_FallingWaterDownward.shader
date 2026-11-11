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
            CBUFFER_END
            struct Attributes { float3 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; };
            Varyings vert(Attributes input) { Varyings output; output.positionCS = TransformObjectToHClip(input.positionOS); output.uv = input.uv; return output; }

            float Hash11(float p) { p=frac(p*0.1031); p*=p+33.33; p*=p+p; return frac(p); }
            float FlowRibbon(float x, float flowY, float noise)
            {
                // Larger spacing means fewer bright ribbons and wider gaps.
                float lanes=x*(8.5/_WhiteStreakSpacing)+noise*0.82, laneId=floor(lanes), lane=frac(lanes)-0.5;
                float seed=Hash11(laneId+17.0), centre=(Hash11(laneId+4.0)-0.5)*0.25;
                float width=lerp(0.10,0.29,seed)+sin(flowY*(7.0+seed*5.0)+seed*12.0)*0.035;
                return 1.0-smoothstep(width,width+0.075,abs(lane-centre));
            }

            half4 frag(Varyings input) : SV_Target
            {
                const float BodyOpacity=0.58, WhiteOpacity=0.72;
                const float3 DeepBlue=float3(0.025,0.34,0.62), Aqua=float3(0.06,0.76,0.93), FoamWhite=float3(0.82,1.22,1.35);
                float2 uv=input.uv;
                float time=_Time.y*_FlowSpeed;
                float distortionTime=_Time.y*_DistortionSpeed;
                float2 nUV=float2(uv.x*2.15,uv.y*3.6-distortionTime);
                float n0=SAMPLE_TEXTURE2D(_NoiseMap,sampler_NoiseMap,nUV).r;
                float n1=SAMPLE_TEXTURE2D(_NoiseMap,sampler_NoiseMap,nUV*2.37+float2(0.31,0.17)).r;
                float warp=(n0-0.5)*_DistortionStrength+(n1-0.5)*(_DistortionStrength*0.36);
                float flowY=uv.y-time+(n0-0.5)*0.055;
                float2 flowUV=float2(uv.x+warp,flowY);
                float mainFlow=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,flowUV*float2(1.0,1.35)).r;
                float foamNoise=SAMPLE_TEXTURE2D(_FoamMap,sampler_FoamMap,flowUV*float2(1.5,2.45)+float2(0.0,-distortionTime*0.42)).r;
                float edgeMask=SAMPLE_TEXTURE2D(_MaskMap,sampler_MaskMap,uv).r;
                float wideRibbon=FlowRibbon(flowUV.x*0.92,flowY*0.72,n0);
                float fineRibbon=FlowRibbon(flowUV.x*1.76+0.19,flowY*1.31,n1);
                float broken=smoothstep(0.27,0.76,n0*0.64+n1*0.36);
                float whiteFlow=saturate(fineRibbon*broken*(0.54+foamNoise*0.78));
                float blueFlow=saturate(wideRibbon*(0.35+mainFlow*0.68)+fineRibbon*0.22);
                float side=min(uv.x,1.0-uv.x);
                float topFade=smoothstep(0.0,0.045+n0*0.045,uv.y);
                float bottomFade=1.0-smoothstep(0.91-n1*0.05,1.0,uv.y);
                float coverage=smoothstep(0.005+n1*0.035,0.105+n0*0.045,side)*topFade*bottomFade*edgeMask;
                float lowerFoam=smoothstep(0.78,1.0,uv.y)*smoothstep(0.36,0.68,foamNoise);
                whiteFlow=saturate(whiteFlow+lowerFoam*0.72);
                float body=saturate(0.18+blueFlow*0.92+mainFlow*0.18);
                float alpha=coverage*saturate(BodyOpacity*body+whiteFlow*WhiteOpacity);
                float3 colour=lerp(DeepBlue,Aqua,saturate(blueFlow*1.18+mainFlow*0.16));
                colour=lerp(colour,FoamWhite,whiteFlow);
                return half4(colour*alpha,alpha);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
