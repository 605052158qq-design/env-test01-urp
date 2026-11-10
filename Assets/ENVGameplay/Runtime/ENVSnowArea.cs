using UnityEngine;

/// <summary>A12: turn the REAL scene snow ParticleSystem on only when the player enters mountain marker.</summary>
[RequireComponent(typeof(BoxCollider))]
[DisallowMultipleComponent]
public sealed class ENVSnowArea : MonoBehaviour
{
    public Transform player;
    public ParticleSystem snowEffect;
    public bool stopOnExit = true;
    private bool inside;
    private BoxCollider box;
    private void Start()
    {
        box = GetComponent<BoxCollider>(); box.isTrigger = true;
        if (player == null) { var cc = FindObjectOfType<CharacterController>(); if (cc != null) player = cc.transform; }
        if (snowEffect != null) snowEffect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }
    private void Update()
    {
        if (player == null || box == null || snowEffect == null) return;
        bool now = box.bounds.Contains(player.position + Vector3.up * 0.5f);
        if (now == inside) return;
        inside = now;
        if (inside) snowEffect.Play();
        else if (stopOnExit) snowEffect.Stop(true, ParticleSystemStopBehavior.StopEmitting);
    }
}

/// <summary>Applies the requested larger snowflake treatment to the authored snowfall VFX at runtime.</summary>
public static class ENVSnowParticleTuner
{
    private const float SnowSizeMultiplier = 1.5f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void ApplyToLoadedScene()
    {
        foreach (ParticleSystem system in Object.FindObjectsOfType<ParticleSystem>(true))
        {
            if (!BelongsToSnowfall(system.transform)) continue;
            ParticleSystem.MainModule main = system.main;
            ParticleSystem.MinMaxCurve size = main.startSize;
            switch (size.mode)
            {
                case ParticleSystemCurveMode.Constant:
                    size.constant *= SnowSizeMultiplier;
                    break;
                case ParticleSystemCurveMode.TwoConstants:
                    size.constantMin *= SnowSizeMultiplier;
                    size.constantMax *= SnowSizeMultiplier;
                    break;
                default:
                    size.curveMultiplier *= SnowSizeMultiplier;
                    break;
            }
            main.startSize = size;
        }
    }

    private static bool BelongsToSnowfall(Transform current)
    {
        while (current != null)
        {
            if (current.name.IndexOf("snow", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                current.name.IndexOf("雪", System.StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
            current = current.parent;
        }
        return false;
    }
}
