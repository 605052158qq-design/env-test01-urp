using UnityEngine;

/// <summary>
/// A small, inspectable colour-recovery proof for one real scene object.  It is
/// deliberately separate from the fullscreen recovery pass: artists can replace
/// its renderers with the exact dock / bridge pieces when the final split arrives.
/// </summary>
[DisallowMultipleComponent]
public sealed class ENVSingleObjectColorRecovery : MonoBehaviour
{
    [Tooltip("The gemstone that restores this object's colour.")]
    public ENVGem sourceGem;
    [Tooltip("Actual renderers to desaturate until the gem is activated.")]
    public Renderer[] targets;

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");
    private MaterialPropertyBlock block;
    private bool restored;

    private void Awake()
    {
        if (targets == null || targets.Length == 0)
            targets = GetComponentsInChildren<Renderer>(true);
        block = new MaterialPropertyBlock();
    }

    private void OnEnable()
    {
        if (sourceGem != null && sourceGem.Activated) RestoreColour();
        else ApplyGrayscale();
    }

    private void Update()
    {
        if (!restored && sourceGem != null && sourceGem.Activated) RestoreColour();
    }

    public void ApplyGrayscale()
    {
        restored = false;
        EnsureBlock();
        if (targets == null) return;
        foreach (Renderer target in targets)
        {
            if (target == null || target.sharedMaterial == null) continue;
            Material material = target.sharedMaterial;
            Color original;
            int property;
            if (material.HasProperty(BaseColorId)) { original = material.GetColor(BaseColorId); property = BaseColorId; }
            else if (material.HasProperty(ColorId)) { original = material.GetColor(ColorId); property = ColorId; }
            else continue;
            float luminance = original.r * 0.2126f + original.g * 0.7152f + original.b * 0.0722f;
            target.GetPropertyBlock(block);
            block.SetColor(property, new Color(luminance, luminance, luminance, original.a));
            target.SetPropertyBlock(block);
        }
    }

    public void RestoreColour()
    {
        restored = true;
        EnsureBlock();
        if (targets == null) return;
        foreach (Renderer target in targets)
        {
            if (target == null) continue;
            target.GetPropertyBlock(block);
            block.Clear();
            target.SetPropertyBlock(block);
        }
    }

    private void EnsureBlock()
    {
        if (block == null) block = new MaterialPropertyBlock();
    }
}
