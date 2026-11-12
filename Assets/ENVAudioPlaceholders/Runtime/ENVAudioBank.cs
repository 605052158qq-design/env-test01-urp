using UnityEngine;

/// <summary>One scene-wide audio library. Replace A-F clips here; every linked emitter changes with it.</summary>
[DisallowMultipleComponent]
public sealed class ENVAudioBank : MonoBehaviour
{
    public AudioClip A_backgroundMusic;
    public AudioClip B_footstep;
    public AudioClip C_water;
    public AudioClip D_waterfall;
    public AudioClip E_campfire;
    public AudioClip F_gemTouch;
    [Header("Menu UI")]
    public AudioClip uiHover;
    public AudioClip uiClick;

    [Header("Gameplay mix")]
    [Range(0f, 1f)] public float musicVolume = 0.26f;
    [Range(0f, 1f)] public float waterVolume = 0.20f;
    [Range(0f, 1f)] public float waterfallVolume = 0.16f;
    [Range(0f, 1f)] public float campfireVolume = 0.14f;
    [Range(0f, 1f)] public float gemTouchVolume = 0.72f;
    [Header("World ambience placement")]
    [Tooltip("Water, waterfall and fire use 3D distance attenuation instead of playing globally.")]
    public bool spatializeWorldAmbience = true;
    public Transform waterAnchor;
    public Transform waterfallAnchor;
    public Transform campfireAnchor;
    [Min(0.1f)] public float ambienceMinDistance = 3f;
    [Min(1f)] public float ambienceMaxDistance = 32f;

    private AudioSource musicSource;
    private AudioSource waterSource;
    private AudioSource waterfallSource;
    private AudioSource campfireSource;
    private AudioSource gemTouchSource;

    public AudioClip Get(ENVAudioSlot slot)
    {
        switch (slot)
        {
            case ENVAudioSlot.A_Music: return A_backgroundMusic;
            case ENVAudioSlot.B_Footsteps: return B_footstep;
            case ENVAudioSlot.C_Water: return C_water;
            case ENVAudioSlot.D_Waterfall: return D_waterfall;
            case ENVAudioSlot.E_Campfire: return E_campfire;
            case ENVAudioSlot.F_Gem: return F_gemTouch;
            default: return null;
        }
    }

    /// <summary>
    /// Starts the supplied world mix only after the player leaves the title menu.
    /// Water, waterfall, and campfire are spatialized by default.  Explicit anchor
    /// references win; otherwise the matching VFX/root name in the loaded scene is
    /// used.  This keeps a distant waterfall from competing with nearby gameplay.
    /// </summary>
    public void StartGameplayAudio()
    {
        PlayLoop(ref musicSource, "BGM", A_backgroundMusic, musicVolume);
        PlayWorldLoop(ref waterSource, "Water ambience", C_water, waterVolume, ref waterAnchor,
            "VFX_RiverAmbient", "River", "Water", "Stream", "Lake", "Ocean", "河", "水");
        PlayWorldLoop(ref waterfallSource, "Waterfall ambience", D_waterfall, waterfallVolume, ref waterfallAnchor,
            "VFX_WaterfallBase", "Waterfall", "瀑");
        PlayWorldLoop(ref campfireSource, "Campfire ambience", E_campfire, campfireVolume, ref campfireAnchor,
            "VFX_Campfire", "Campfire", "Fire", "篝火");
    }

    public void StopGameplayAudio()
    {
        Stop(musicSource); Stop(waterSource); Stop(waterfallSource); Stop(campfireSource);
    }

    public void PlayGemTouch()
    {
        if (F_gemTouch == null) return;
        if (gemTouchSource == null) gemTouchSource = CreateSource("Gem touch");
        gemTouchSource.PlayOneShot(F_gemTouch, gemTouchVolume);
    }

    private void PlayLoop(ref AudioSource source, string sourceName, AudioClip clip, float volume)
    {
        if (clip == null)
        {
            Debug.LogWarning("[ENV Audio] " + sourceName + " has no playable AudioClip assigned.", this);
            return;
        }
        if (source == null)
        {
            source = CreateSource(sourceName);
            source.loop = true;
        }
        source.clip = clip;
        source.volume = volume;
        if (!source.isPlaying) source.Play();
    }

    private void PlayWorldLoop(ref AudioSource source, string sourceName, AudioClip clip, float volume,
        ref Transform anchor, params string[] anchorNames)
    {
        if (clip == null) return;
        if (anchor == null) anchor = FindSceneAnchor(anchorNames);
        if (!spatializeWorldAmbience || anchor == null)
        {
            if (anchor == null)
                Debug.LogWarning("[ENV Audio] " + sourceName + " has no scene anchor. Assign one on ENVAudioBank for 3D distance audio.", this);
            PlayLoop(ref source, sourceName, clip, volume);
            return;
        }
        if (source == null)
        {
            source = CreateSource(sourceName);
            source.loop = true;
        }
        source.transform.SetParent(anchor, false);
        source.transform.localPosition = Vector3.zero;
        source.clip = clip;
        source.volume = volume;
        source.spatialBlend = 1f;
        source.rolloffMode = AudioRolloffMode.Logarithmic;
        source.minDistance = ambienceMinDistance;
        source.maxDistance = Mathf.Max(ambienceMinDistance, ambienceMaxDistance);
        if (!source.isPlaying) source.Play();
    }

    private AudioSource CreateSource(string sourceName)
    {
        GameObject child = new GameObject(sourceName);
        child.transform.SetParent(transform, false);
        AudioSource source = child.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.spatialBlend = 0f;
        source.dopplerLevel = 0f;
        return source;
    }

    private static Transform FindSceneAnchor(string[] names)
    {
        foreach (Transform candidate in Resources.FindObjectsOfTypeAll<Transform>())
        {
            if (candidate == null || !candidate.gameObject.scene.IsValid() || !candidate.gameObject.scene.isLoaded) continue;
            foreach (string name in names)
                if (candidate.name.IndexOf(name, System.StringComparison.OrdinalIgnoreCase) >= 0)
                    return candidate;
        }
        return null;
    }

    private static void Stop(AudioSource source)
    {
        if (source != null) source.Stop();
    }
}

public enum ENVAudioSlot
{
    A_Music, B_Footsteps, C_Water, D_Waterfall, E_Campfire, F_Gem
}
