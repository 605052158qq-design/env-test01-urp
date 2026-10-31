using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

/// <summary>Plays the supplied menu MP4 behind the separately composited menu art.</summary>
[RequireComponent(typeof(VideoPlayer))]
[DisallowMultipleComponent]
public sealed class ENVMenuVideoBackground : MonoBehaviour
{
    public VideoClip suppliedVideo;
    public bool playOnAwake = true;
    public bool muteAudio = true;

    private VideoPlayer player;
    private RawImage image;
    private RenderTexture renderTexture;

    private void Awake()
    {
        image = GetComponent<RawImage>();
        if (image == null) image = gameObject.AddComponent<RawImage>();
        image.raycastTarget = false;
        RectTransform rect = image.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.SetAsFirstSibling();

        player = GetComponent<VideoPlayer>();
        player.playOnAwake = false;
        player.isLooping = true;
        player.renderMode = VideoRenderMode.RenderTexture;
        player.aspectRatio = VideoAspectRatio.FitOutside;
        player.audioOutputMode = muteAudio ? VideoAudioOutputMode.None : VideoAudioOutputMode.Direct;
        player.clip = suppliedVideo;
        renderTexture = new RenderTexture(1920, 1080, 0, RenderTextureFormat.ARGB32);
        renderTexture.name = "ENV Menu Background RT";
        player.targetTexture = renderTexture;
        image.texture = renderTexture;
    }

    private void OnEnable()
    {
        if (playOnAwake && player != null && suppliedVideo != null) player.Play();
    }

    private void OnDisable()
    {
        if (player != null) player.Stop();
    }

    private void OnDestroy()
    {
        if (renderTexture == null) return;
        renderTexture.Release();
        Destroy(renderTexture);
    }
}
