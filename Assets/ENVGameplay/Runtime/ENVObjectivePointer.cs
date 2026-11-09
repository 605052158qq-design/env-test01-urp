using UnityEngine;
using UnityEngine.UI;

/// <summary>L14. Displays the next gemstone location on-screen.
/// The marker intentionally has no gemstone-name text; it only exposes the
/// interaction prompt while the player is inside that gem's trigger zone.</summary>
[DisallowMultipleComponent]
public sealed class ENVObjectivePointer : MonoBehaviour
{
    public Camera targetCamera;
    public Transform player;
    public RectTransform pointer;
    public Text distanceLabel;
    public Sprite customerPointerSprite;
    public Vector2 screenMargin = new Vector2(90f, 90f);
    private Canvas canvas;
    private RectTransform canvasRect;
    private ENVGemInteractor gemInteractor;
    private void Start()
    {
        if (targetCamera == null) targetCamera = Camera.main;
        if (player == null) { var cc = FindObjectOfType<CharacterController>(); if (cc != null) player = cc.transform; }
        if (player != null) gemInteractor = player.GetComponent<ENVGemInteractor>();
        if (pointer == null) MakeDefaultUI();
    }
    private void MakeDefaultUI()
    {
        GameObject root = new GameObject("ENV Objective UI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        root.transform.SetParent(transform, false);
        canvas = root.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 220;
        canvasRect = root.GetComponent<RectTransform>();
        root.GetComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
        GameObject item = new GameObject("Objective Arrow", typeof(RectTransform), typeof(CanvasRenderer));
        item.transform.SetParent(root.transform, false);
        pointer = item.GetComponent<RectTransform>(); pointer.sizeDelta = new Vector2(75f, 75f);
        if (customerPointerSprite != null)
        {
            var img = item.AddComponent<Image>();
            img.sprite = customerPointerSprite; img.preserveAspect = true; img.raycastTarget = false;
        }
        else
        {
            var t = item.AddComponent<Text>(); t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.text = "◆"; t.fontSize = 40; t.alignment = TextAnchor.MiddleCenter;
            t.color = new Color(1f, 0.9f, 0.4f); t.raycastTarget = false;
        }
        GameObject label = new GameObject("Objective Distance", typeof(RectTransform), typeof(Text));
        label.transform.SetParent(item.transform, false);
        distanceLabel = label.GetComponent<Text>(); distanceLabel.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        distanceLabel.text = "按 E 交互";
        distanceLabel.fontSize = 17; distanceLabel.alignment = TextAnchor.MiddleCenter; distanceLabel.color = Color.white;
        distanceLabel.raycastTarget = false;
        distanceLabel.rectTransform.sizeDelta = new Vector2(190f, 28f);
        distanceLabel.rectTransform.anchoredPosition = new Vector2(0, -46f);
    }
    private void LateUpdate()
    {
        if (targetCamera == null || pointer == null) return;
        var world = ENVWorldProgress.Instance;
        var next = world != null ? world.NextGem : null;
        pointer.gameObject.SetActive(next != null);
        if (next == null) return;
        Vector3 vp = targetCamera.WorldToViewportPoint(next.transform.position + Vector3.up * 1.5f);
        if (vp.z < 0f) { vp.x = 1f - vp.x; vp.y = 1f - vp.y; }
        float x = Mathf.Clamp(vp.x * Screen.width, screenMargin.x, Screen.width - screenMargin.x);
        float y = Mathf.Clamp(vp.y * Screen.height, screenMargin.y, Screen.height - screenMargin.y);
        pointer.position = new Vector3(x, y, 0f);
        if (distanceLabel != null)
        {
            if (gemInteractor == null && player != null) gemInteractor = player.GetComponent<ENVGemInteractor>();
            // FindNearest performs the same exact trigger-volume test used by
            // the E-key action.  The prompt therefore cannot appear merely
            // because a gem is visible or nearby through a wall.
            bool canInteract = gemInteractor != null && gemInteractor.FindNearest() == next;
            if (distanceLabel.gameObject.activeSelf != canInteract)
                distanceLabel.gameObject.SetActive(canInteract);
            if (canInteract) distanceLabel.text = "按 E 交互";
        }
    }
}
