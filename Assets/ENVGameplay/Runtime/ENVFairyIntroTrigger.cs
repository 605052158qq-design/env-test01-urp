using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Video;

/// <summary>L11. Attach to a marker in the blue area. Optional supplied VideoClip plays first, then one real fairy follows.</summary>
[RequireComponent(typeof(BoxCollider))]
[DisallowMultipleComponent]
public sealed class ENVFairyIntroTrigger : MonoBehaviour
{
    public Transform player;
    public ENVFairyFollower fairy;
    public VideoClip suppliedFairyIntroVideo;
    public Camera gameCamera;
    [Tooltip("Provides visible feedback while the final fairy video/model is pending.")]
    public ENVRegionTitleUI titleUi;
    public string triggerMessage = "古树苏醒 · 精灵的光芒正在靠近";
    public UnityEvent onFairyAppeared;
    private bool fired;
    private bool playing;
    private VideoPlayer video;
    private BoxCollider zone;
    private void Awake() { zone = GetComponent<BoxCollider>(); zone.isTrigger = true; }
    private void Start()
    {
        if (player == null) { var cc = FindObjectOfType<CharacterController>(); if (cc != null) player = cc.transform; }
        if (gameCamera == null) gameCamera = Camera.main;
    }
    private void Update()
    {
        if (fired || player == null) return;
        if (zone.bounds.Contains(player.position + Vector3.up * 0.5f)) Activate();
    }
    private void OnTriggerEnter(Collider other) { if (player != null && (other.transform == player || other.transform.IsChildOf(player))) Activate(); }
    private void Activate()
    {
        if (fired) return;
        fired = true;
        if (suppliedFairyIntroVideo == null) { Complete(); return; }
        StartCoroutine(PlayVideo());
    }

    /// <summary>Invoked from the V1 debug GUI; no temporary collider is needed.</summary>
    public void DebugActivate() { Activate(); }
    private IEnumerator PlayVideo()
    {
        playing = true;
        ENVCinematicScreen.Begin();
        video = gameObject.AddComponent<VideoPlayer>();
        video.playOnAwake = false;
        video.clip = suppliedFairyIntroVideo;
        video.renderMode = VideoRenderMode.CameraNearPlane;
        video.targetCamera = gameCamera;
        video.aspectRatio = VideoAspectRatio.FitInside;
        video.isLooping = false;
        video.audioOutputMode = VideoAudioOutputMode.Direct;
        video.Prepare();
        float timeout = 10f;
        while (!video.isPrepared && timeout > 0f) { timeout -= Time.unscaledDeltaTime; yield return null; }
        if (video.isPrepared)
        {
            video.Play();
            while (video != null && video.isPlaying) yield return null;
        }
        if (video != null) Destroy(video);
        playing = false;
        ENVCinematicScreen.End();
        Complete();
    }
    private void Complete()
    {
        if (fairy != null) fairy.BeginFollow();
        if (titleUi == null) titleUi = ENVRegionTitleUI.Instance;
        if (titleUi != null && !string.IsNullOrWhiteSpace(triggerMessage)) titleUi.Show(triggerMessage, 3.5f);
        if (onFairyAppeared != null) onFairyAppeared.Invoke();
    }
    private void OnDisable()
    {
        if (video != null) { video.Stop(); Destroy(video); }
        if (playing) ENVCinematicScreen.End();
        playing = false;
    }
}
