using UnityEngine;

/// <summary>A06/A13: point at EXISTING lamp/window/fairy renderers. Preserves their assigned material & texture.</summary>
[DisallowMultipleComponent]
public sealed class ENVEmissiveLight : MonoBehaviour
{
    public Renderer[] targets;
    [ColorUsage(true, true)] public Color emissionColor = new Color(3f, 1.6f, 0.4f, 1f);
    public bool pulse = false;
    [Min(0f)] public float pulseSpeed = 2f;
    [Range(0f, 1f)] public float pulseDepth = 0.18f;
    public Light optionalRealLight;
    [Min(0f)] public float lightIntensity = 1f;
    private MaterialPropertyBlock block;
    private float baseLight;
    private static readonly int EmissionID = Shader.PropertyToID("_EmissionColor");
    private void Awake() { block = new MaterialPropertyBlock(); if (optionalRealLight != null) baseLight = optionalRealLight.intensity; }
    private void OnEnable()
    {
        // Require explicit target renderer(s): never light up the whole building accidentally.
        if (targets == null || targets.Length == 0) return;
        // URP/Lit emission must be compiled with _EMISSION keyword.
        foreach (var r in targets)
        {
            if (r == null) continue;
            foreach (var mat in r.sharedMaterials)
                if (mat != null && mat.shader != null && mat.shader.name == "Universal Render Pipeline/Lit") mat.EnableKeyword("_EMISSION");
        }
        Apply(1f);
    }
    private void Update()
    {
        if (!pulse) return;
        float v = 1f - pulseDepth * 0.5f + pulseDepth * 0.5f * Mathf.Sin(Time.time * pulseSpeed);
        Apply(v);
    }
    private void Apply(float intensity)
    {
        if (block == null) block = new MaterialPropertyBlock();
        if (targets != null) foreach (var r in targets)
        {
            if (r == null) continue;
            int submeshCount = r.sharedMaterials.Length;
            for (int i = 0; i < submeshCount; i++)
            {
                r.GetPropertyBlock(block, i);
                block.SetColor(EmissionID, emissionColor * intensity);
                r.SetPropertyBlock(block, i);
            }
        }
        if (optionalRealLight != null) optionalRealLight.intensity = lightIntensity * intensity;
    }
}
