using UnityEngine;

/// <summary>Bridge from an external SSR/planar RT source to the shared water shader.
/// This class DOES NOT render SSR. It only binds the output of an existing producer.
/// No material clones or changes to saved .mat assets. Attach to the water root.
/// </summary>
[DisallowMultipleComponent]
public sealed class ENVReflectionRTBinding : MonoBehaviour
{
    public Renderer[] waterRenderers;
    [Tooltip("Optional RT produced by an SSR or planar reflection camera.")]
    public RenderTexture explicitReflectionRT;
    [Tooltip("When using a third-party renderer feature that exports a global texture, enter its real property name.")]
    public string globalTextureProperty = "_SSRTexture";
    public bool useGlobalWhenNoExplicitRT = false;

    private static readonly int ReflectionId = Shader.PropertyToID("_ReflectionRT");
    private static readonly int EnabledId = Shader.PropertyToID("_UseReflectionRT");
    private MaterialPropertyBlock block;

    private void Awake()
    {
        block = new MaterialPropertyBlock();
        if (waterRenderers == null || waterRenderers.Length == 0)
            waterRenderers = GetComponentsInChildren<Renderer>();
    }
    private void LateUpdate()
    {
        Texture texture = explicitReflectionRT;
        if (texture == null && useGlobalWhenNoExplicitRT && !string.IsNullOrEmpty(globalTextureProperty))
            texture = Shader.GetGlobalTexture(globalTextureProperty);
        if (block == null) block = new MaterialPropertyBlock();
        if (waterRenderers == null) return;
        foreach (Renderer r in waterRenderers)
        {
            if (r == null) continue;
            r.GetPropertyBlock(block);
            block.SetTexture(ReflectionId, texture != null ? texture : Texture2D.blackTexture);
            block.SetFloat(EnabledId, texture != null ? 1f : 0f);
            r.SetPropertyBlock(block);
        }
    }
    private void OnDisable()
    {
        if (block == null || waterRenderers == null) return;
        foreach (Renderer r in waterRenderers)
        {
            if (r == null) continue;
            r.GetPropertyBlock(block);
            block.SetFloat(EnabledId, 0f);
            r.SetPropertyBlock(block);
        }
    }
}
