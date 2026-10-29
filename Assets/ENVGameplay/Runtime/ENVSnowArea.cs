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
