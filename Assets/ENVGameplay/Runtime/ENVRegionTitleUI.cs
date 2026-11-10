using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>L07 region title UI. Assign a customer-supplied Text if desired; otherwise creates one UI Text at runtime.</summary>
[DisallowMultipleComponent]
public sealed class ENVRegionTitleUI : MonoBehaviour
{
    public static ENVRegionTitleUI Instance { get; private set; }
    public Text titleText;
    public CanvasGroup group;
    [Min(0.1f)] public float defaultShowSeconds = 2.2f;
    [Min(0f)] public float fadeSeconds = 0.5f;
    private Coroutine routine;

    private void Awake()
    {
        if (Instance != null && Instance != this) { enabled = false; return; }
        Instance = this;
        if (titleText == null) CreateDefaultUI();
        else if (group == null) group = titleText.GetComponent<CanvasGroup>() ?? titleText.gameObject.AddComponent<CanvasGroup>();
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
        GameObject textGO = new GameObject("Area Name", typeof(RectTransform), typeof(CanvasGroup), typeof(Text), typeof(Outline), typeof(Shadow));
        textGO.transform.SetParent(canvasGO.transform, false);
        group = textGO.GetComponent<CanvasGroup>();
        titleText = textGO.GetComponent<Text>();
        titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        titleText.fontSize = 68;
        titleText.fontStyle = FontStyle.Bold;
        titleText.alignment = TextAnchor.MiddleCenter;
        titleText.color = new Color(1f, 0.97f, 0.87f);
        titleText.raycastTarget = false;
        titleText.horizontalOverflow = HorizontalWrapMode.Overflow;
        var rect = titleText.rectTransform;
        rect.anchorMin = new Vector2(0.15f, 0.65f);
        rect.anchorMax = new Vector2(0.85f, 0.81f);
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }
    public void Show(string title, float seconds = -1f)
    {
        if (titleText == null || group == null) return;
        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(ShowRoutine(title, seconds < 0f ? defaultShowSeconds : seconds));
    }
    private IEnumerator ShowRoutine(string title, float time)
    {
        titleText.text = title;
        titleText.rectTransform.localScale = Vector3.one * 0.86f;
        float enter = 0f;
        const float enterSeconds = 0.22f;
        while (enter < enterSeconds)
        {
            enter += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(enter / enterSeconds);
            group.alpha = t;
            titleText.rectTransform.localScale = Vector3.one * Mathf.Lerp(0.86f, 1f, 1f - Mathf.Pow(1f - t, 3f));
            yield return null;
        }
        group.alpha = 1f;
        titleText.rectTransform.localScale = Vector3.one;
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
        Outline outline = titleText.GetComponent<Outline>() ?? titleText.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(0.12f, 0.07f, 0.12f, 0.92f);
        outline.effectDistance = new Vector2(2.5f, -2.5f);
        Shadow shadow = titleText.GetComponent<Shadow>() ?? titleText.gameObject.AddComponent<Shadow>();
        shadow.effectColor = new Color(0.2f, 0.08f, 0.02f, 0.68f);
        shadow.effectDistance = new Vector2(7f, -7f);
    }
}
