using System.Collections;
using UnityEngine;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>L13: routes last gemstone's completion to the SAME video object; no second fake scene.</summary>
[DisallowMultipleComponent]
public sealed class ENVEndSequenceOnCompletion : MonoBehaviour
{
    public ENVWorldProgress world;
    public ENVVideoSequence endingVideo;
    [Tooltip("Used until the supplied ending video is available.")]
    public ENVRegionTitleUI titleUi;
    public string completionMessage = "七色宝石已归位 · 微光小镇恢复色彩";
    [Header("Companion ending")]
    [Tooltip("Shown only when the player has completed the ancient-tree fairy dialogue.")]
    public Texture2D fairyCompanionEndingBackground;
    private void Start()
    {
        if (world == null) world = ENVWorldProgress.Instance;
        if (world != null) world.onAllGemsActivated.AddListener(OnDone);
    }
    private void OnDestroy() { if (world != null) world.onAllGemsActivated.RemoveListener(OnDone); }
    private void OnDone()
    {
        ENVFairyIntroTrigger fairyEncounter = FindObjectOfType<ENVFairyIntroTrigger>(true);
        if (fairyEncounter != null && fairyEncounter.FairyAcquired && fairyCompanionEndingBackground != null)
        {
            ENVFairyCompanionEndingUI.Show(fairyCompanionEndingBackground);
            return;
        }
        // No companion branch: after the same post-recovery delay, return
        // directly to the first menu rather than leaving a frozen completion card.
        ReturnToMainMenu();
    }

    /// <summary>Invoked by the V1 debug GUI when validating the end branch.</summary>
    public void DebugComplete() { OnDone(); }

    public static void ReturnToMainMenu()
    {
        ENVAudioBank bank = FindObjectOfType<ENVAudioBank>(true);
        if (bank != null) bank.StopGameplayAudio();
        ENVStartScreen menu = FindObjectOfType<ENVStartScreen>(true);
        if (menu != null) menu.ShowMenu();
    }
}

/// <summary>Interactive ending shown only after the player has gained the fairy companion.</summary>
public sealed class ENVFairyCompanionEndingUI : MonoBehaviour
{
    private static ENVFairyCompanionEndingUI active;
    private static readonly string[] Lines =
    {
        "颜色全都回来了！",
        "谢谢你…旅行家。",
        "那些离开这里的人，很快就会循着熟悉的色彩，陆陆续续回到故乡。",
        "广场会再次热闹起来，夜晚的窗边也会亮起温暖的灯。",
        "而我，也终于不用再独自躲在森林里，守着一个褪色的梦了。",
        "我们会再见的对吧？",
        "这里随时欢迎你。"
    };

    private Image fade;
    private Text message;
    private int lineIndex;
    private bool readyForInput;
    private bool closing;
    private float timeScaleBeforeEnding;

    public static void Show(Texture2D backgroundTexture)
    {
        if (active != null) return;
        GameObject root = new GameObject("ENV Fairy Companion Ending", typeof(RectTransform));
        active = root.AddComponent<ENVFairyCompanionEndingUI>();
        active.Build(backgroundTexture);
        active.StartCoroutine(active.Open());
    }

    private void Build(Texture2D backgroundTexture)
    {
        timeScaleBeforeEnding = Time.timeScale;
        Time.timeScale = 0f;

        Canvas canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1200;
        CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        gameObject.AddComponent<GraphicRaycaster>();

        RawImage background = CreateRawImage("Ending Background", transform, backgroundTexture, Color.white);
        Stretch(background.rectTransform, Vector2.zero, Vector2.one);

        message = CreateText("Ending Text", transform, 36, Color.white);
        message.alignment = TextAnchor.MiddleCenter;
        message.horizontalOverflow = HorizontalWrapMode.Wrap;
        message.verticalOverflow = VerticalWrapMode.Overflow;
        message.lineSpacing = 1.18f;
        SetAnchors(message.rectTransform, new Vector2(0.12f, 0.07f), new Vector2(0.88f, 0.24f));

        fade = CreateImage("Ending Fade", transform, Color.black);
        Stretch(fade.rectTransform, Vector2.zero, Vector2.one);
    }

    private IEnumerator Open()
    {
        yield return Fade(1f, 0f, 0.55f);
        message.text = Lines[lineIndex];
        readyForInput = true;
    }

    private void Update()
    {
        if (!readyForInput || !AdvancePressed()) return;
        if (lineIndex >= Lines.Length - 1)
        {
            if (!closing) StartCoroutine(CloseToMenu());
            return;
        }
        lineIndex++;
        message.text = Lines[lineIndex];
    }

    private IEnumerator CloseToMenu()
    {
        closing = true;
        readyForInput = false;
        yield return Fade(0f, 1f, 0.45f);
        // Restore the previous scale before the menu records the current one as
        // its own pause baseline.
        Time.timeScale = timeScaleBeforeEnding;
        ENVEndSequenceOnCompletion.ReturnToMainMenu();
        Destroy(gameObject);
    }

    private IEnumerator Fade(float from, float to, float seconds)
    {
        float elapsed = 0f;
        while (elapsed < seconds)
        {
            elapsed += Time.unscaledDeltaTime;
            fade.color = new Color(0f, 0f, 0f, Mathf.Lerp(from, to, elapsed / seconds));
            yield return null;
        }
        fade.color = new Color(0f, 0f, 0f, to);
    }

    private void OnDestroy()
    {
        if (active == this) active = null;
        Time.timeScale = timeScaleBeforeEnding;
    }

    private static RawImage CreateRawImage(string name, Transform parent, Texture texture, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(RawImage));
        go.transform.SetParent(parent, false);
        RawImage image = go.GetComponent<RawImage>();
        image.texture = texture;
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private static Image CreateImage(string name, Transform parent, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        Image image = go.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private static Text CreateText(string name, Transform parent, int fontSize, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);
        Text text = go.GetComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = fontSize;
        text.color = color;
        text.raycastTarget = false;
        return text;
    }

    private static void Stretch(RectTransform rect, Vector2 min, Vector2 max)
    {
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static void SetAnchors(RectTransform rect, Vector2 min, Vector2 max)
    {
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static bool AdvancePressed()
    {
#if ENABLE_INPUT_SYSTEM
        Mouse mouse = Mouse.current;
        return mouse != null && mouse.leftButton.wasPressedThisFrame;
#else
        return Input.GetMouseButtonDown(0);
#endif
    }
}
