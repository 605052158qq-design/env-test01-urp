using UnityEngine;

/// <summary>
/// Supplies the embedded URP Lit pass with the seven gemstone reveal circles.
/// Rendering-layer bits 1..7 are hui1..hui7; default-layer renderers, UI and the
/// player never enter the shader's grey branch.
/// </summary>
[DisallowMultipleComponent]
public sealed class ENVMaterialColorRecovery : MonoBehaviour
{
    [Tooltip("Bits 1..7 correspond to the hui1..hui7 Rendering Layer Masks.")]
    public int greyRenderingLayerMask = 0xFE;
    [Range(0.01f, 10f)] public float edgeSoftness = 1.5f;
    [Range(0.01f, 10f)] public float ringWidth = 0.6f;
    [Range(0f, 3f)] public float ringIntensity = 0.8f;

    private const int MaxGems = 7;
    private static readonly int EnabledId = Shader.PropertyToID("_ENVMaterialGrayEnabled");
    private static readonly int LayersId = Shader.PropertyToID("_ENVMaterialGrayLayerMask");
    private static readonly int CountId = Shader.PropertyToID("_ENVMaterialRevealCount");
    private static readonly int CirclesId = Shader.PropertyToID("_ENVMaterialRevealCircles");
    private static readonly int SoftnessId = Shader.PropertyToID("_ENVMaterialRevealSoftness");
    private static readonly int RingWidthId = Shader.PropertyToID("_ENVMaterialRingWidth");
    private static readonly int RingIntensityId = Shader.PropertyToID("_ENVMaterialRingIntensity");
    private readonly Vector4[] circles = new Vector4[MaxGems];

    private void OnEnable() { ApplyGlobals(); }
    private void Update() { ApplyGlobals(); }
    private void OnDisable()
    {
        Shader.SetGlobalFloat(EnabledId, 0f);
        Shader.SetGlobalInt(CountId, 0);
    }

    private void ApplyGlobals()
    {
        Shader.SetGlobalFloat(EnabledId, 1f);
        Shader.SetGlobalInt(LayersId, greyRenderingLayerMask);
        Shader.SetGlobalFloat(SoftnessId, edgeSoftness);
        Shader.SetGlobalFloat(RingWidthId, ringWidth);
        Shader.SetGlobalFloat(RingIntensityId, ringIntensity);

        int count = 0;
        ENVWorldProgress world = ENVWorldProgress.Instance;
        if (world != null)
        {
            foreach (ENVGem gem in world.Gems)
            {
                if (gem == null || !gem.Activated || count >= MaxGems) continue;
                int bit = Mathf.Clamp(gem.sequenceIndex + 1, 1, 7);
                float layerBit = 1 << bit;
                if (gem.RadiusNow < gem.revealMaxRadius - 0.01f) layerBit = -layerBit;
                Vector3 point = gem.RevealPosition;
                circles[count++] = new Vector4(point.x, point.z, gem.RadiusNow, layerBit);
            }
        }

        Shader.SetGlobalInt(CountId, count);
        Shader.SetGlobalVectorArray(CirclesId, circles);
    }
}
