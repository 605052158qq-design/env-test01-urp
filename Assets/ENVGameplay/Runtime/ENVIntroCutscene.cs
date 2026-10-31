using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>A deliberately simple placeholder transition until final cinematics are supplied.</summary>
[DisallowMultipleComponent]
public sealed class ENVIntroCutscene : MonoBehaviour
{
    [Min(0.1f)] public float fadeSeconds = 0.4f;
    [Min(0.1f)] public float captionSeconds = 1.6f;
    public UnityEvent onComplete;

    private CanvasGroup group;
    private Text caption;
    private string firstCaption = "潮声渐近……";
    private string secondCaption = "船正驶向微光码头";
    private bool playing;

    public void Configure(string first, string second)
    {
        firstCaption = first;
        secondCaption = second;
        EnsureUi();
    }

    private void Awake()
    {
        EnsureUi();
        group.alpha = 0f;
        gameObject.SetActive(false);
    }

    public void Play()
    {
        if (playing) return;
        gameObject.SetActive(true);
        StartCoroutine(Run());
    }

    private IEnumerator Run()
    {
        playing = true;
        yield return Fade(0f, 1f);
        caption.text = firstCaption;
        yield return new WaitForSeconds(captionSeconds);
        caption.text = secondCaption;
        yield return new WaitForSeconds(captionSeconds);
        yield return Fade(1f, 0f);
        playing = false;
        gameObject.SetActive(false);
        if (onComplete != null) onComplete.Invoke();
    }

    private IEnumerator Fade(float from, float to)
    {
        float elapsed = 0f;
        while (elapsed < fadeSeconds)
        {
            elapsed += Time.unscaledDeltaTime;
            group.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(elapsed / fadeSeconds));
            yield return null;
        }
        group.alpha = to;
    }

    private void EnsureUi()
    {
        Canvas canvas = GetComponent<Canvas>();
        if (canvas == null) canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;
        if (GetComponent<CanvasScaler>() == null) gameObject.AddComponent<CanvasScaler>();
        if (GetComponent<GraphicRaycaster>() == null) gameObject.AddComponent<GraphicRaycaster>();
        group = GetComponent<CanvasGroup>();
        if (group == null) group = gameObject.AddComponent<CanvasGroup>();
        Image shade = GetComponent<Image>();
        if (shade == null) shade = gameObject.AddComponent<Image>();
        shade.color = Color.black;
        shade.raycastTarget = false;
        RectTransform rootRect = shade.rectTransform;
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.offsetMin = Vector2.zero;
        rootRect.offsetMax = Vector2.zero;
        if (caption != null) return;

        GameObject textObject = new GameObject("Caption", typeof(RectTransform), typeof(Text));
        textObject.transform.SetParent(transform, false);
        caption = textObject.GetComponent<Text>();
        caption.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        caption.alignment = TextAnchor.MiddleCenter;
        caption.fontSize = 42;
        caption.color = Color.white;
        RectTransform captionRect = caption.rectTransform;
        captionRect.anchorMin = new Vector2(0.15f, 0.36f);
        captionRect.anchorMax = new Vector2(0.85f, 0.64f);
        captionRect.offsetMin = Vector2.zero;
        captionRect.offsetMax = Vector2.zero;
    }
}
