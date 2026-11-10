using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Video;
using UnityEngine.UI;

/// <summary>L10/L13: Directly assign supplied intro / ending VideoClip. Call Play from Button or UnityEvent.</summary>
[RequireComponent(typeof(VideoPlayer))]
[DisallowMultipleComponent]
public sealed class ENVVideoSequence : MonoBehaviour
{
    public VideoClip suppliedVideo;
    public Camera targetCamera;
    public bool playOnStart = false;
    [Header("Skip prompt")]
    public bool allowSpaceToSkip = true;
    public string skipPrompt = "按 空格 跳过";
    public UnityEvent onComplete;
    private VideoPlayer player;
    private bool busy;
    private Text skipHint;
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
        CreateSkipHint();
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
        SetSkipHintVisible(true);
    }

    private void Update()
    {
        if (!busy || !allowSpaceToSkip) return;
#if ENABLE_INPUT_SYSTEM
        if (UnityEngine.InputSystem.Keyboard.current != null &&
            UnityEngine.InputSystem.Keyboard.current.spaceKey.wasPressedThisFrame) Skip();
#else
        if (Input.GetKeyDown(KeyCode.Space)) Skip();
#endif
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
        SetSkipHintVisible(false);
        if (onComplete != null) onComplete.Invoke();
    }
    private void OnDisable()
    {
        if (busy) { busy = false; ENVCinematicScreen.End(); if (player != null) player.Stop(); }
        SetSkipHintVisible(false);
    }
    private void OnDestroy()
    {
        if (player == null) return;
        player.loopPointReached -= OnVideoFinished;
        player.errorReceived -= OnVideoError;
    }

    private void CreateSkipHint()
    {
        GameObject canvasObject = new GameObject("Video Skip Hint", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 900;
        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        GameObject textObject = new GameObject("Label", typeof(RectTransform), typeof(Text), typeof(Outline), typeof(Shadow));
        textObject.transform.SetParent(canvasObject.transform, false);
        skipHint = textObject.GetComponent<Text>();
        skipHint.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        skipHint.fontSize = 27;
        skipHint.alignment = TextAnchor.MiddleCenter;
        skipHint.color = new Color(1f, 1f, 1f, 0.95f);
        skipHint.raycastTarget = false;
        RectTransform rect = skipHint.rectTransform;
        rect.anchorMin = new Vector2(0.5f, 0.08f);
        rect.anchorMax = new Vector2(0.5f, 0.08f);
        rect.sizeDelta = new Vector2(360f, 48f);
        Outline outline = textObject.GetComponent<Outline>();
        outline.effectColor = new Color(0f, 0f, 0f, 0.82f);
        outline.effectDistance = new Vector2(2f, -2f);
        Shadow shadow = textObject.GetComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.45f);
        shadow.effectDistance = new Vector2(5f, -5f);
        canvasObject.SetActive(false);
    }

    private void SetSkipHintVisible(bool visible)
    {
        if (skipHint == null) return;
        skipHint.text = skipPrompt;
        if (skipHint.transform.parent != null) skipHint.transform.parent.gameObject.SetActive(visible);
    }
}
