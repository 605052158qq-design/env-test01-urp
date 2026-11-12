#ifndef ENV_MATERIAL_COLOR_RECOVERY_INCLUDED
#define ENV_MATERIAL_COLOR_RECOVERY_INCLUDED

// Shared by URP Lit and ENV stylized water. A renderer opts in through its
// Rendering Layer Mask: hui1..hui7 map to bits 1..7. The game supplies active
// gemstone reveal circles in world XZ coordinates.
float _ENVMaterialGrayEnabled;
uint _ENVMaterialGrayLayerMask;
int _ENVMaterialRevealCount;
float4 _ENVMaterialRevealCircles[7]; // xyz = world XZ + radius, w = signed layer bit
float _ENVMaterialRevealSoftness;
float _ENVMaterialRingWidth;
float _ENVMaterialRingIntensity;

half3 ApplyENVMaterialColorRecovery(half3 sourceColor, float3 positionWS)
{
    if (_ENVMaterialGrayEnabled < 0.5)
        return sourceColor;

    uint rendererLayers = GetMeshRenderingLayer();
    if ((rendererLayers & _ENVMaterialGrayLayerMask) == 0)
        return sourceColor;

    half revealed = 0.0h;
    half ring = 0.0h;
    [unroll(7)]
    for (int i = 0; i < 7; ++i)
    {
        if (i >= _ENVMaterialRevealCount)
            break;

        float4 circle = _ENVMaterialRevealCircles[i];
        uint circleLayer = (uint)abs(circle.w);
        if ((rendererLayers & circleLayer) == 0)
            continue;

        float distanceXZ = distance(positionWS.xz, circle.xy);
        float radius = max(circle.z, 0.001);
        float softness = max(_ENVMaterialRevealSoftness, 0.001);
        half fade = (half)(1.0 - smoothstep(max(0.0, radius - softness), radius + softness, distanceXZ));
        revealed = max(revealed, fade);

        if (circle.w < 0.0)
        {
            float width = max(_ENVMaterialRingWidth, 0.001);
            half edge = (half)(1.0 - smoothstep(0.0, width, abs(distanceXZ - radius)));
            ring = max(ring, edge);
        }
    }

    half luminance = dot(sourceColor, half3(0.2126h, 0.7152h, 0.0722h));
    half3 recovered = lerp(luminance.xxx, sourceColor, revealed);
    return recovered + ring * _ENVMaterialRingIntensity;
}

#endif
