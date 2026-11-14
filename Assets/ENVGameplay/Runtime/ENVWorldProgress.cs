using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Seven-gem progress manager. Only the actual gems already in the scene are registered.
/// It does not spawn or replace any art/prefabs. Drop-in replacement for the existing script;
/// keep the existing .meta file so all Unity scene references remain valid.
/// </summary>
[DisallowMultipleComponent]
public sealed class ENVWorldProgress : MonoBehaviour
{
    public static ENVWorldProgress Instance { get; private set; }
    [Tooltip("When disabled, every uncollected gem may be activated in any order.")]
    public bool requireSequence = false;
    [Min(1)] public int expectedGemCount = 7;
    [Tooltip("The ending must not interrupt the final colour-recovery reveal.")]
    [Min(0f)] public float completionDelaySeconds = 8f;
    public UnityEvent onAllGemsActivated;

    private readonly List<ENVGem> gems = new List<ENVGem>();
    public IReadOnlyList<ENVGem> Gems { get { return gems; } }
    public event Action<ENVGem> GemActivated;
    public int RegisteredGemCount { get { return gems.Count; } }
    public bool SetupReady { get { return ValidateSetup(false); } }

    private bool completed;
    private bool warnedMissing;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogError("[ENV World] Duplicate ENVWorldProgress in scene.", this);
            enabled = false;
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        // ENVGem.OnEnable may run before this manager's Awake, depending on script order.
        // Recollect currently active gem components to make the registration deterministic.
        foreach (ENVGem gem in FindObjectsOfType<ENVGem>()) Register(gem);
        ValidateSetup(true);
    }

    private void OnDestroy() { if (Instance == this) Instance = null; }

    public void Register(ENVGem gem)
    {
        if (gem == null || !gem.isActiveAndEnabled || gems.Contains(gem)) return;
        gems.Add(gem);
        SortGems();
        warnedMissing = false;
    }

    public void Unregister(ENVGem gem)
    {
        if (gems.Remove(gem)) warnedMissing = false;
    }

    private void SortGems()
    {
        gems.Sort((a, b) => a.sequenceIndex.CompareTo(b.sequenceIndex));
    }

    private bool ValidateSetup(bool logWarning)
    {
        if (gems.Count != expectedGemCount)
        {
            if (logWarning && !warnedMissing)
            {
                Debug.LogWarning("[ENV World] Found " + gems.Count + "/" + expectedGemCount +
                    " active registered gems. Set ENVGem on all seven real gemstones before testing E interactions.", this);
                warnedMissing = true;
            }
            return false;
        }

        // Keep validation read-only. Register() is responsible for sorting.
        for (int i = 0; i < expectedGemCount; i++)
        {
            ENVGem gem = gems[i];
            if (gem == null || !gem.isActiveAndEnabled || gem.sequenceIndex != i)
            {
                if (logWarning)
                    Debug.LogWarning("[ENV World] Invalid gem registration: indices must be unique 0.." +
                        (expectedGemCount - 1) + ", and all gems must be enabled.", this);
                return false;
            }
        }
        return true;
    }

    public ENVGem NextGem
    {
        get
        {
            if (!ValidateSetup(false)) return null;
            foreach (ENVGem gem in gems)
                if (gem != null && !gem.Activated) return gem;
            return null;
        }
    }

    public bool CanActivate(ENVGem gem)
    {
        if (gem == null || completed || !ValidateSetup(false) || !gems.Contains(gem)) return false;
        return !requireSequence || NextGem == gem;
    }

    public void NotifyActivated(ENVGem gem)
    {
        GemActivated?.Invoke(gem);
        if (completed || !ValidateSetup(false) || gem == null) return;
        // Completion belongs to the seventh collected gem, regardless of its authored
        // sequence index. This keeps the seven locations entirely free-form: a player
        // can collect the purple gem first and still complete once the other six are in.
        foreach (ENVGem registered in gems)
            if (registered == null || !registered.Activated) return;
        completed = true;
        StartCoroutine(FinishAfterColorReveal(gem));
    }

    private IEnumerator FinishAfterColorReveal(ENVGem lastGem)
    {
        float revealSeconds = lastGem != null ? lastGem.revealSeconds : 0f;
        yield return new WaitForSeconds(Mathf.Max(revealSeconds, completionDelaySeconds));
        if (completed && onAllGemsActivated != null) onAllGemsActivated.Invoke();
    }

    public void ResetProgress()
    {
        completed = false;
        foreach (ENVGem gem in gems) if (gem != null) gem.ResetGem();
    }
}
