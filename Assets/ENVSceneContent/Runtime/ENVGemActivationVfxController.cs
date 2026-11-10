using System.Collections;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Lives on VFX_GemActivation. It keeps the idle stars alive until the real
/// gem is activated, then fades every particle renderer while a short,
/// fixed-direction scatter plays before the VFX is hidden.
/// </summary>
[DisallowMultipleComponent]
public sealed class ENVGemActivationVfxController : MonoBehaviour
{
    public ParticleSystem[] idleParticles;
    public ParticleSystem dissolveScatter;
    [Min(0.05f)] public float fadeSeconds = 1.25f;
    [Min(0.05f)] public float hideAfterSeconds = 1.55f;

    private ENVGem gem;
    private ParticleSystemRenderer[] particleRenderers;
    private Material[] runtimeMaterials;
    private Color[] originalColors;
    private int[] colorPropertyIds;
    private Coroutine fadeRoutine;
    private bool subscribed;
    private bool hidden;

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");

    private void Awake()
    {
        gem = GetComponentInParent<ENVGem>();
        if (idleParticles == null || idleParticles.Length == 0)
            idleParticles = GetComponentsInChildren<ParticleSystem>(true);
        CacheRendererMaterials();
    }

    private void OnEnable()
    {
        if (gem == null) gem = GetComponentInParent<ENVGem>();
        if (gem != null && !subscribed)
        {
            if (gem.onActivated == null) gem.onActivated = new UnityEvent();
            gem.onActivated.AddListener(PlayDissolveAndHide);
            subscribed = true;
        }
    }

    private void OnDisable()
    {
        if (subscribed && gem != null && gem.onActivated != null) gem.onActivated.RemoveListener(PlayDissolveAndHide);
        subscribed = false;
        if (fadeRoutine != null) { StopCoroutine(fadeRoutine); fadeRoutine = null; }
    }

    private void Update()
    {
        if (hidden && gem != null && !gem.Activated) RestoreIdle();
    }

    public void PlayDissolveAndHide()
    {
        if (fadeRoutine != null) StopCoroutine(fadeRoutine);
        SetRenderersVisible(true);
        SetRendererAlpha(1f);
        foreach (ParticleSystem idle in idleParticles)
            if (idle != null && idle != dissolveScatter)
                idle.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        if (dissolveScatter != null)
        {
            dissolveScatter.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            dissolveScatter.Play(true);
        }
        hidden = false;
        fadeRoutine = StartCoroutine(FadeAndHide());
    }

    private IEnumerator FadeAndHide()
    {
        float elapsed = 0f;
        while (elapsed < fadeSeconds)
        {
            elapsed += Time.deltaTime;
            SetRendererAlpha(1f - Mathf.Clamp01(elapsed / fadeSeconds));
            yield return null;
        }
        float extra = Mathf.Max(0f, hideAfterSeconds - fadeSeconds);
        if (extra > 0f) yield return new WaitForSeconds(extra);
        foreach (ParticleSystem system in GetComponentsInChildren<ParticleSystem>(true))
            if (system != null) system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        SetRendererAlpha(0f);
        SetRenderersVisible(false);
        hidden = true;
        fadeRoutine = null;
    }

    private void RestoreIdle()
    {
        hidden = false;
        SetRenderersVisible(true);
        SetRendererAlpha(1f);
        foreach (ParticleSystem idle in idleParticles)
            if (idle != null && idle != dissolveScatter)
            {
                idle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                idle.Play(true);
            }
    }

    private void CacheRendererMaterials()
    {
        particleRenderers = GetComponentsInChildren<ParticleSystemRenderer>(true);
        runtimeMaterials = new Material[particleRenderers.Length];
        originalColors = new Color[particleRenderers.Length];
        colorPropertyIds = new int[particleRenderers.Length];
        for (int i = 0; i < particleRenderers.Length; i++)
        {
            ParticleSystemRenderer renderer = particleRenderers[i];
            if (renderer == null || renderer.sharedMaterial == null) continue;
            Material material = renderer.material;
            runtimeMaterials[i] = material;
            int propertyId = material.HasProperty(BaseColorId) ? BaseColorId : material.HasProperty(ColorId) ? ColorId : 0;
            colorPropertyIds[i] = propertyId;
            originalColors[i] = propertyId != 0 ? material.GetColor(propertyId) : Color.white;
        }
    }

    private void SetRendererAlpha(float alpha)
    {
        if (runtimeMaterials == null) return;
        for (int i = 0; i < runtimeMaterials.Length; i++)
        {
            Material material = runtimeMaterials[i];
            if (material == null || colorPropertyIds[i] == 0) continue;
            Color color = originalColors[i];
            color.a *= alpha;
            material.SetColor(colorPropertyIds[i], color);
        }
    }

    private void SetRenderersVisible(bool visible)
    {
        if (particleRenderers == null) return;
        foreach (ParticleSystemRenderer renderer in particleRenderers)
            if (renderer != null) renderer.enabled = visible;
    }
}
