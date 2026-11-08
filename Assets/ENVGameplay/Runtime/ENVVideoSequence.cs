using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Video;

/// <summary>L10/L13: Directly assign supplied intro / ending VideoClip. Call Play from Button or UnityEvent.</summary>
[RequireComponent(typeof(VideoPlayer))]
[DisallowMultipleComponent]
public sealed class ENVVideoSequence : MonoBehaviour
{
    public VideoClip suppliedVideo;
    public Camera targetCamera;
    public bool playOnStart = false;
    public UnityEvent onComplete;
    private VideoPlayer player;
    private bool busy;
    public bool IsPlaying { get { return busy; } }

    private void Awake()
    {
        player = GetComponent<VideoPlayer>();
        if (targetCamera == null) targetCamera = Camera.main;
        player.playOnAwake = false;
        player.isLooping = false;
        player.renderMode = VideoRenderMode.CameraNearPlane;
        player.aspectRatio = VideoAspectRatio.FitInside;
        player.audioOutputMode = VideoAudioOutputMode.Direct;
        player.loopPointReached += OnVideoFinished;
        player.errorReceived += OnVideoError;
    }
    private void Start() { if (playOnStart) Play(); }
    public void Play()
    {
        if (busy) return;
        if (suppliedVideo == null) { if (onComplete != null) onComplete.Invoke(); return; }
        busy = true;
        ENVCinematicScreen.Begin();
        player.clip = suppliedVideo;
        player.targetCamera = targetCamera != null ? targetCamera : Camera.main;
        player.Play();
    }

    /// <summary>Ends the current video and invokes the usual completion handoff.</summary>
    public void Skip()
    {
        if (busy) Finish();
    }
    private void OnVideoFinished(VideoPlayer vp) { Finish(); }
    private void OnVideoError(VideoPlayer vp, string message)
    {
        Debug.LogWarning("ENV video failed: " + message, this);
        Finish();
    }
    private void Finish()
    {
        if (!busy) return;
        busy = false;
        ENVCinematicScreen.End();
        player.Stop();
        if (onComplete != null) onComplete.Invoke();
    }
    private void OnDisable() { if (busy) { busy = false; ENVCinematicScreen.End(); if (player != null) player.Stop(); } }
    private void OnDestroy()
    {
        if (player == null) return;
        player.loopPointReached -= OnVideoFinished;
        player.errorReceived -= OnVideoError;
    }
}
