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
}

public enum ENVAudioSlot
{
    A_Music, B_Footsteps, C_Water, D_Waterfall, E_Campfire, F_Gem
}
