using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>L07 region title UI. Assign a customer-supplied Text if desired; otherwise creates one UI Text at runtime.</summary>
[DisallowMultipleComponent]
public sealed class ENVRegionTitleUI : MonoBehaviour
{
    public static ENVRegionTitleUI Instance { get; private set; }
    public Text titleText;
    [Tooltip("Runtime image used for the supplied hand-drawn regional title cards.")]
    public Image titleImage;
    public CanvasGroup group;
    [Header("Supplied title artwork")]
    public Sprite dockTitle;
    public Sprite squareTitle;
    public Sprite gardenTitle;
    public Sprite forestTitle;
    public Sprite snowTitle;
    [Min(0.1f)] public float defaultShowSeconds = 2.2f;
    [Min(0f)] public float fadeSeconds = 0.5f;
    private Coroutine routine;

    private void Awake()
    {
        if (Instance != null && Instance != this) { enabled = false; return; }
        Instance = this;
        if (titleText == null && titleImage == null) CreateDefaultUI();
        else if (group == null)
        {
            GameObject target = titleText != null ? titleText.gameObject : titleImage.gameObject;
            group = target.GetComponent<CanvasGroup>() ?? target.AddComponent<CanvasGroup>();
        }
        ApplyTitleStyle();
        if (group != null) group.alpha = 0f;
    }
    private void OnDestroy() { if (Instance == this) Instance = null; }
    private void CreateDefaultUI()
    {
        GameObject canvasGO = new GameObject("ENV Region Title Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGO.transform.SetParent(transform, false);
        var canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 200;
        var scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        GameObject container = new GameObject("Area Title Container", typeof(RectTransform), typeof(CanvasGroup));
        container.transform.SetParent(canvasGO.transform, false);
        group = container.GetComponent<CanvasGroup>();
        RectTransform containerRect = container.GetComponent<RectTransform>();
        containerRect.anchorMin = new Vector2(0.15f, 0.65f);
        containerRect.anchorMax = new Vector2(0.85f, 0.81f);
        containerRect.offsetMin = containerRect.offsetMax = Vector2.zero;

        GameObject imageGO = new GameObject("Area Name Artwork", typeof(RectTransform), typeof(Image));
        imageGO.transform.SetParent(container.transform, false);
        titleImage = imageGO.GetComponent<Image>();
        titleImage.preserveAspect = true;
        titleImage.raycastTarget = false;
        Stretch(titleImage.rectTransform);

        GameObject textGO = new GameObject("Area Name", typeof(RectTransform), typeof(Text));
        textGO.transform.SetParent(container.transform, false);
        titleText = textGO.GetComponent<Text>();
        titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        titleText.fontSize = 68;
        titleText.fontStyle = FontStyle.Bold;
        titleText.alignment = TextAnchor.MiddleCenter;
        titleText.color = new Color(1f, 0.97f, 0.87f);
        titleText.raycastTarget = false;
        titleText.horizontalOverflow = HorizontalWrapMode.Overflow;
        Stretch(titleText.rectTransform);
    }
    public void Show(string title, float seconds = -1f)
    {
        if (group == null) return;
        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(ShowRoutine(title, seconds < 0f ? defaultShowSeconds : seconds));
    }
    private IEnumerator ShowRoutine(string title, float time)
    {
        Sprite suppliedArtwork = FindArtwork(title);
        bool hasArtwork = suppliedArtwork != null && titleImage != null;
        if (titleImage != null)
        {
            titleImage.sprite = suppliedArtwork;
            titleImage.enabled = hasArtwork;
        }
        if (titleText != null)
        {
            titleText.enabled = !hasArtwork;
            titleText.text = title;
            titleText.rectTransform.localScale = Vector3.one * 0.86f;
        }
        float enter = 0f;
        const float enterSeconds = 0.22f;
        while (enter < enterSeconds)
        {
            enter += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(enter / enterSeconds);
            group.alpha = t;
            if (titleText != null) titleText.rectTransform.localScale = Vector3.one * Mathf.Lerp(0.86f, 1f, 1f - Mathf.Pow(1f - t, 3f));
            if (titleImage != null) titleImage.rectTransform.localScale = Vector3.one * Mathf.Lerp(0.92f, 1f, 1f - Mathf.Pow(1f - t, 3f));
            yield return null;
        }
        group.alpha = 1f;
        if (titleText != null) titleText.rectTransform.localScale = Vector3.one;
        if (titleImage != null) titleImage.rectTransform.localScale = Vector3.one;
        yield return new WaitForSecondsRealtime(Mathf.Max(0f, time));
        float elapsed = 0f;
        float dur = Mathf.Max(0.01f, fadeSeconds);
        while (elapsed < dur)
        {
            elapsed += Time.unscaledDeltaTime;
            group.alpha = 1f - Mathf.Clamp01(elapsed / dur);
            yield return null;
        }
        group.alpha = 0f;
        routine = null;
    }

    private void ApplyTitleStyle()
    {
        if (titleText == null) return;
        titleText.fontStyle = FontStyle.Bold;
        titleText.color = new Color(1f, 0.97f, 0.87f);
        titleText.fontSize = Mathf.Max(titleText.fontSize, 62);
    }

    private Sprite FindArtwork(string title)
    {
        if (string.IsNullOrEmpty(title)) return null;
        if (title.Contains("码头")) return dockTitle;
        if (title.Contains("广场")) return squareTitle;
        if (title.Contains("花园")) return gardenTitle;
        if (title.Contains("森林")) return forestTitle;
        if (title.Contains("雪岭")) return snowTitle;
        return null;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }
}
