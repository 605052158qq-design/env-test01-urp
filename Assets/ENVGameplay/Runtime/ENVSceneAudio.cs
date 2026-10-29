using UnityEngine;

/// <summary>L15: put on actual river, waterfall, fire, lamp etc; assign customer's AudioClip directly.</summary>
[RequireComponent(typeof(AudioSource))]
[DisallowMultipleComponent]
public sealed class ENVSceneAudio : MonoBehaviour
{
    public AudioClip customerAudio;
    public bool isMusic = false;
    public bool loop = true;
    [Range(0f, 1f)] public float volume = 0.65f;
    [Min(0.01f)] public float minDistance = 2f;
    [Min(0.5f)] public float maxDistance = 20f;
    public bool playOnStart = true;
    private AudioSource source;
    private void Awake() { source = GetComponent<AudioSource>(); Configure(); }
    private void OnValidate() { if (maxDistance < minDistance) maxDistance = minDistance; }
    private void Configure()
    {
        if (source == null) source = GetComponent<AudioSource>();
        if (source == null) return;
        source.playOnAwake = false;
        source.clip = customerAudio;
        source.loop = loop;
        source.volume = volume;
        source.spatialBlend = isMusic ? 0f : 1f;
        source.rolloffMode = AudioRolloffMode.Linear;
        source.minDistance = minDistance;
        source.maxDistance = Mathf.Max(minDistance, maxDistance);
        source.dopplerLevel = 0f;
    }
    private void Start() { if (playOnStart) Play(); }
    public void Play() { Configure(); if (source != null && source.clip != null) source.Play(); }
    public void Stop() { if (source != null) source.Stop(); }
}
