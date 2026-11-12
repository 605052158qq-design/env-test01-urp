using UnityEngine;
using UnityEngine.UI;

/// <summary>Displays the supplied fairy-location icon until the player reaches
/// the ancient-tree encounter.  It is a visual guide only and never changes
/// the dialogue or objective sequence.</summary>
[DisallowMultipleComponent]
public sealed class ENVFairyLocatorUI : MonoBehaviour
{
    public ENVFairyIntroTrigger encounter;
    public Sprite icon;
    public Camera targetCamera;
    public Vector2 screenMargin = new Vector2(76f, 76f);
    public Vector3 targetOffset = new Vector3(0f, 1.8f, 0f);

    private RectTransform marker;
    private ENVStartScreen startScreen;

    private void Start()
    {
        if (encounter == null) encounter = GetComponent<ENVFairyIntroTrigger>();
        if (targetCamera == null) targetCamera = Camera.main;
        startScreen = FindObjectOfType<ENVStartScreen>(true);
        BuildUi();
    }

    private void LateUpdate()
    {
        if (marker == null) return;
        if (startScreen == null) startScreen = FindObjectOfType<ENVStartScreen>(true);
        // The fairy objective should never be present over the title/menu or
        // boat intro.  It appears only when the arrival sequence has released
        // normal gameplay controls.
        bool gameplayReady = startScreen == null || startScreen.GameplayReady;
        bool visible = gameplayReady && encounter != null && !encounter.EncounterStarted && icon != null && targetCamera != null;
        if (marker.gameObject.activeSelf != visible) marker.gameObject.SetActive(visible);
        if (!visible) return;

        Vector3 viewport = targetCamera.WorldToViewportPoint(encounter.transform.position + targetOffset);
        if (viewport.z < 0f) { viewport.x = 1f - viewport.x; viewport.y = 1f - viewport.y; }
        marker.position = new Vector3(
            Mathf.Clamp(viewport.x * Screen.width, screenMargin.x, Screen.width - screenMargin.x),
            Mathf.Clamp(viewport.y * Screen.height, screenMargin.y, Screen.height - screenMargin.y), 0f);
    }

    private void BuildUi()
    {
        GameObject canvasObject = new GameObject("ENV Fairy Locator UI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        canvasObject.transform.SetParent(transform, false);
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 215;
        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        GameObject iconObject = new GameObject("Fairy Location", typeof(RectTransform), typeof(Image));
        iconObject.transform.SetParent(canvasObject.transform, false);
        Image image = iconObject.GetComponent<Image>();
        image.sprite = icon;
        image.preserveAspect = true;
        image.raycastTarget = false;
        marker = image.rectTransform;
        marker.sizeDelta = new Vector2(92f, 92f);
    }
}
