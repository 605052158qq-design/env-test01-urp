using System;
using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

/// <summary>One logical manager, no duplicate art. Register actual gems on the existing map.</summary>
[DisallowMultipleComponent]
public sealed class ENVWorldProgress : MonoBehaviour
{
    public static ENVWorldProgress Instance { get; private set; }
    public bool requireSequence = true;
    public UnityEvent onAllGemsActivated;

    private readonly List<ENVGem> gems = new List<ENVGem>();
    public IReadOnlyList<ENVGem> Gems { get { return gems; } }
    public event Action<ENVGem> GemActivated;
    private bool completed;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Debug.LogError("Duplicate ENVWorldProgress", this); enabled = false; return; }
        Instance = this;
    }
    private void OnDestroy() { if (Instance == this) Instance = null; }

    public void Register(ENVGem gem)
    {
        if (gem == null || gems.Contains(gem)) return;
        gems.Add(gem);
        gems.Sort((a, b) => a.sequenceIndex.CompareTo(b.sequenceIndex));
    }
    public void Unregister(ENVGem gem) { gems.Remove(gem); }

    public ENVGem NextGem
    {
        get
        {
            for (int i = 0; i < gems.Count; i++)
                if (gems[i] != null && gems[i].isActiveAndEnabled && !gems[i].Activated) return gems[i];
            return null;
        }
    }
    public bool CanActivate(ENVGem gem) { return gem != null && (!requireSequence || NextGem == gem); }
    public void NotifyActivated(ENVGem gem)
    {
        GemActivated?.Invoke(gem);
        if (completed || gems.Count == 0) return;
        foreach (var g in gems) if (g != null && !g.Activated) return;
        completed = true;
        StartCoroutine(FinishAfterColorReveal(gem));
    }
    private IEnumerator FinishAfterColorReveal(ENVGem lastGem)
    {
        if (lastGem != null) yield return new WaitForSeconds(lastGem.revealSeconds);
        if (completed && onAllGemsActivated != null) onAllGemsActivated.Invoke();
    }
    public void ResetProgress()
    {
        completed = false;
        foreach (var g in gems) if (g != null) g.ResetGem();
    }
}
