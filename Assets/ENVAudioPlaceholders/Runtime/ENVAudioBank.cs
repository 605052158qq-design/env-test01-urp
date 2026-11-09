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
    [Range(0f, 1f)] public float musicVolume = 0.42f;
    [Range(0f, 1f)] public float waterVolume = 0.20f;
    [Range(0f, 1f)] public float waterfallVolume = 0.16f;
    [Range(0f, 1f)] public float campfireVolume = 0.14f;
    [Range(0f, 1f)] public float gemTouchVolume = 0.72f;

    private AudioSource musicSource;
    private AudioSource waterSource;
    private AudioSource waterfallSource;
    private AudioSource campfireSource;
    private AudioSource gemTouchSource;
    private bool gameplayAudioStarted;

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
    /// Water, waterfall, and campfire are deliberately 2D in this prototype: the
    /// map has no stable authored source markers for them yet, so they remain
    /// audible rather than disappearing when the player is away from the origin.
    /// </summary>
    public void StartGameplayAudio()
    {
        if (gameplayAudioStarted) return;
        gameplayAudioStarted = true;
        PlayLoop(ref musicSource, "BGM", A_backgroundMusic, musicVolume);
        PlayLoop(ref waterSource, "Ocean ambience", C_water, waterVolume);
        PlayLoop(ref waterfallSource, "Waterfall ambience", D_waterfall, waterfallVolume);
        PlayLoop(ref campfireSource, "Campfire ambience", E_campfire, campfireVolume);
    }

    public void StopGameplayAudio()
    {
        gameplayAudioStarted = false;
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
        if (clip == null) return;
        if (source == null)
        {
            source = CreateSource(sourceName);
            source.loop = true;
        }
        source.clip = clip;
        source.volume = volume;
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

    private static void Stop(AudioSource source)
    {
        if (source != null) source.Stop();
    }
}

public enum ENVAudioSlot
{
    A_Music, B_Footsteps, C_Water, D_Waterfall, E_Campfire, F_Gem
}
