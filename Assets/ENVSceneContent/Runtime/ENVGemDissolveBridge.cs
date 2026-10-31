using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Connects the EXISTING ENVGem activation to _Dissolve on its EXISTING renderers.
/// No material clones, no shared-material edits, no duplicate gameplay gem.
/// Works with ENVShaderKit/Gem/FacetedGlow (V3) or any shader exposing _Dissolve.
/// </summary>
[DisallowMultipleComponent]
public sealed class ENVGemDissolveBridge : MonoBehaviour
{
    public ENVGem gem;
    [Tooltip("Gem mesh renderers only; exclude particle and terrain renderers.")]
    public Renderer[] gemRenderers;
    [Tooltip("Drag the already placed VFX_GemActivation instance; do not create another one.")]
    public ParticleSystem activationFx;
    [Tooltip("Optional. Assign T_V3_WarpNoise for faceted gem dissolve.")]
    public Texture2D dissolveNoise;
    [Min(0.05f)] public float dissolveSeconds = 1.3f;
    public AnimationCurve dissolveCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    public bool hideMeshAfterDissolve = false;

    private static readonly int DissolveId = Shader.PropertyToID("_Dissolve");
    private static readonly int NoiseId = Shader.PropertyToID("_DissolveNoiseTex");
    private readonly List<Slot> slots = new List<Slot>();
    private Coroutine running;
    private bool finished;
    private bool subscribed;
    private bool warned;

    private sealed class Slot
    {
        public Renderer renderer;
        public int materialIndex;
        public MaterialPropertyBlock block = new MaterialPropertyBlock();
    }

    private void Awake()
    {
        if (gem == null) gem = GetComponent<ENVGem>();
        if (gemRenderers == null || gemRenderers.Length == 0)
        {
            var all = GetComponentsInChildren<Renderer>(true);
            var list = new List<Renderer>();
            foreach (var r in all)
                if (r is MeshRenderer || r is SkinnedMeshRenderer) list.Add(r);
            gemRenderers = list.ToArray();
        }
        CacheMaterialSlots();
        SetDissolve(0f);
        if (activationFx != null) activationFx.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    private void OnEnable()
    {
        if (gem == null) gem = GetComponent<ENVGem>();
        if (gem != null && !subscribed)
        {
            if (gem.onActivated == null) gem.onActivated = new UnityEvent();
            gem.onActivated.AddListener(OnGemActivated);
            subscribed = true;
        }
    }

    private void Start()
    {
        if (slots.Count == 0) CacheMaterialSlots();
        if (slots.Count == 0 && !warned)
        {
            warned = true;
            Debug.LogWarning("[ENV] This gem has no renderer whose shader exposes _Dissolve. " +
                             "Assign V3 FacetedGlow to the actual gem material (not the VFX), or use a compatible dissolve shader.", this);
        }
    }

    private void OnDisable()
    {
        if (subscribed && gem != null && gem.onActivated != null) gem.onActivated.RemoveListener(OnGemActivated);
        subscribed = false;
        if (running != null) { StopCoroutine(running); running = null; }
    }

    private void Update()
    {
        // Supports ENVWorldProgress.ResetProgress() without overriding material assets.
        if (finished && gem != null && !gem.Activated)
        {
            finished = false;
            foreach (var r in gemRenderers)
                if (r != null && hideMeshAfterDissolve) r.enabled = true;
            SetDissolve(0f);
        }
    }

    [ContextMenu("Preview Dissolve (Play Mode)")]
    public void PreviewDissolve()
    {
        if (!Application.isPlaying) { Debug.LogWarning("Enter Play mode to preview dissolve.", this); return; }
        OnGemActivated();
    }

    private void OnGemActivated()
    {
        if (activationFx != null)
        {
            activationFx.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            activationFx.Play(true);
        }
        if (running != null) StopCoroutine(running);
        running = StartCoroutine(DissolveRoutine());
    }

    private IEnumerator DissolveRoutine()
    {
        float elapsed = 0f;
        while (elapsed < dissolveSeconds)
        {
            elapsed += Time.deltaTime;
            SetDissolve(dissolveCurve.Evaluate(Mathf.Clamp01(elapsed / dissolveSeconds)));
            yield return null;
        }
        // > 1 ensures even white noise pixels are clipped.
        SetDissolve(1.01f);
        if (hideMeshAfterDissolve)
            foreach (var r in gemRenderers) if (r != null) r.enabled = false;
        finished = true;
        running = null;
    }

    private void CacheMaterialSlots()
    {
        slots.Clear();
        if (gemRenderers == null) return;
        foreach (var renderer in gemRenderers)
        {
            if (renderer == null) continue;
            Material[] materials = renderer.sharedMaterials;
            for (int i = 0; i < materials.Length; i++)
            {
                if (materials[i] == null || !materials[i].HasProperty(DissolveId)) continue;
                slots.Add(new Slot { renderer = renderer, materialIndex = i });
            }
        }
    }

    private void SetDissolve(float amount)
    {
        foreach (var slot in slots)
        {
            if (slot.renderer == null) continue;
            slot.renderer.GetPropertyBlock(slot.block, slot.materialIndex);
            slot.block.SetFloat(DissolveId, amount);
            if (dissolveNoise != null) slot.block.SetTexture(NoiseId, dissolveNoise);
            slot.renderer.SetPropertyBlock(slot.block, slot.materialIndex);
        }
    }
}
