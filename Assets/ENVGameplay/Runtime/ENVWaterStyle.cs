using UnityEngine;

/// <summary>A07/A09/A10. On the existing water/fountain/waterfall Renderer using ENV/StylizedFlowWaterURP.</summary>
[DisallowMultipleComponent]
public sealed class ENVWaterStyle : MonoBehaviour
{
    public enum Flow { FountainRipple, River, Waterfall }
    public Flow style = Flow.River;
    public Renderer targetRenderer;
    public bool applyPresetOnEnable = true;
    private MaterialPropertyBlock block;
    private static readonly int FlowID = Shader.PropertyToID("_FlowSpeed");
    private static readonly int RippleID = Shader.PropertyToID("_RippleStrength");
    private static readonly int FrequencyID = Shader.PropertyToID("_RippleFrequency");
    private static readonly int FoamID = Shader.PropertyToID("_FoamStrength");
    private void OnEnable()
    {
        if (applyPresetOnEnable) ApplyStyle();
    }
    [ContextMenu("Apply Water Style Preset")]
    public void ApplyStyle()
    {
        if (targetRenderer == null) targetRenderer = GetComponent<Renderer>();
        if (targetRenderer == null) return;
        if (block == null) block = new MaterialPropertyBlock();
        targetRenderer.GetPropertyBlock(block);
        switch (style)
        {
            case Flow.FountainRipple:
                block.SetVector(FlowID, Vector4.zero);
                block.SetFloat(RippleID, 0.05f);
                block.SetFloat(FrequencyID, 12f);
                block.SetFloat(FoamID, 0.24f);
                break;
            case Flow.Waterfall:
                block.SetVector(FlowID, new Vector4(0f, -0.9f, 0f, 0f));
                block.SetFloat(RippleID, 0.012f);
                block.SetFloat(FrequencyID, 16f);
                block.SetFloat(FoamID, 0.2f);
                break;
            default:
                block.SetVector(FlowID, new Vector4(0.16f, 0.04f, 0f, 0f));
                block.SetFloat(RippleID, 0.02f);
                block.SetFloat(FrequencyID, 8f);
                block.SetFloat(FoamID, 0.08f);
                break;
        }
        targetRenderer.SetPropertyBlock(block);
    }
}
