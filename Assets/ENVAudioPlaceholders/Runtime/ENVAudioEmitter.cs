using UnityEngine;

/// <summary>
/// Attach to the real scene object or drag its sample prefab under an existing object.
/// References one ENVAudioBank; accepts UnityEvent calls via Play()/Stop().
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(AudioSource))]
public sealed class ENVAudioEmitter : MonoBehaviour
{
    public ENVAudioBank bank;
    public ENVAudioSlot slot = ENVAudioSlot.C_Water;
    public bool playOnStart = true;
    public bool loop = true;
    public bool is2D = false;
    [Range(0f,1f)] public float volume = 0.55f;
    [Min(0.1f)] public float minDistance = 2f;
    [Min(1f)] public float maxDistance = 30f;

    private AudioSource source;
    private void Awake()
    {
        source = GetComponent<AudioSource>();
        if (bank == null) bank = FindObjectOfType<ENVAudioBank>();
        Apply();
    }
    private void Start() { if (playOnStart) Play(); }
    public void Apply()
    {
        if (source == null) source = GetComponent<AudioSource>();
        if (source == null) return;
        source.playOnAwake = false;
        source.clip = bank != null ? bank.Get(slot) : null;
        source.loop = loop;
        source.volume = volume;
        source.spatialBlend = is2D ? 0f : 1f;
        source.rolloffMode = AudioRolloffMode.Logarithmic;
        source.minDistance = minDistance;
        source.maxDistance = Mathf.Max(minDistance,maxDistance);
        source.dopplerLevel = 0;
    }
    public void Play()
    {
        Apply();
        if (source != null && source.clip != null) source.Play();
        else Debug.LogWarning("[ENV Audio] Slot " + slot + " has no AudioClip. Place ENVAudioBank in scene and assign A-F.",this);
    }
    public void PlayOneShot()
    {
        Apply();
        if (source != null && source.clip != null) source.PlayOneShot(source.clip, volume);
    }
    public void Stop() { if (source != null) source.Stop(); }
}
