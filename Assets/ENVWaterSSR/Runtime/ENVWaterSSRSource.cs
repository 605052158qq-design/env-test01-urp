using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Mark a real water MeshRenderer for ENVWaterSSRFeature. Add to the SAME water object;
/// no duplicate water mesh/material is created.
/// </summary>
[ExecuteAlways]
[DisallowMultipleComponent]
public sealed class ENVWaterSSRSource : MonoBehaviour
{
    public Renderer targetRenderer;
    internal static readonly List<ENVWaterSSRSource> Active = new List<ENVWaterSSRSource>();

    private void Reset() { targetRenderer = GetComponent<Renderer>(); }
    private void OnEnable() { if (targetRenderer == null) targetRenderer = GetComponent<Renderer>(); if (!Active.Contains(this)) Active.Add(this); }
    private void OnDisable() { Active.Remove(this); }
}
