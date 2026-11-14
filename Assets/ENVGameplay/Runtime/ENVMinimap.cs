using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// A self-contained minimap for the prototype scene.  It crops the supplied
/// square map illustration through a real Unity UI Mask, then converts world
/// X/Z positions into map coordinates for the player and next gem objective.
/// </summary>
[DisallowMultipleComponent]
public sealed class ENVMinimap : MonoBehaviour
{
    [Header("Map asset")]
    [Tooltip("The square map illustration shown under the circular UI mask.")]
    public Texture2D mapTexture;
    [Tooltip("Persistent sprite variant of the map artwork, used by the editable prefab.")]
    public Sprite mapArtworkSprite;
    [Tooltip("Optional supplied player arrow. Falls back to the temporary generated arrow when empty.")]
    public Sprite playerArrowSprite;
    [Tooltip("Optional supplied lower information panel artwork.")]
    public Sprite statusBackgroundSprite;
    [Tooltip("Persistent circular mask/frame sprite used by the editable prefab.")]
    public Sprite circleMaskSprite;

    [Header("Scene references (optional; resolved automatically when empty)")]
    public Transform player;
    public Camera targetCamera;
    public Transform mapRoot;
    [Tooltip("World-space dock marker that calibrates the artwork to the real scene.")]
    public Transform dockAnchor;
    [Tooltip("The final snow-mountain gem. Together with the dock it fixes map orientation.")]
    public Transform snowAnchor;

    [Header("Layout")]
    [Range(140f, 420f)] public float mapDiameter = 290f;
    [Range(0.02f, 0.20f)] public float edgePadding = 0.075f;
    public Vector2 screenOffset = new Vector2(32f, -30f);
    [Range(1f, 6f)]
    [Tooltip("Scale of the map artwork inside the circular window.")]
    public float mapArtworkScale = 3.7688f;
    [Tooltip("The illustrated dock's location in the source map, measured from bottom-left.")]
    public Vector2 dockMapPosition = new Vector2(0.62f, 0.145f);
    [Tooltip("The illustrated snow-mountain location, measured from bottom-left.")]
    public Vector2 snowMapPosition = new Vector2(0.52f, 0.83f);

    [Header("Prefab UI references")]
    [SerializeField] private RectTransform mapRect;
    [SerializeField] private RectTransform mapContent;
    [SerializeField] private RectTransform playerMarker;
    [SerializeField] private RectTransform objectiveMarker;
    [SerializeField] private Text objectiveTitleLabel;
    [SerializeField] private Text statusLabel;
    [SerializeField] private Canvas uiCanvas;
    private ENVStartScreen startScreen;
    private ENVBoatArrival boatArrival;
    private Bounds worldBounds;
    private bool hasWorldBounds;
    private Sprite circleSprite;
    private Sprite arrowSprite;
    private Sprite objectiveSprite;

    private void Awake()
    {
        ResolveReferences();
        BuildUi();
        RefreshWorldBounds();
    }

    private void OnEnable()
    {
        if (mapRect == null) BuildUi();
    }

    private void LateUpdate()
    {
        if (player == null) ResolveReferences();
        if (!hasWorldBounds) RefreshWorldBounds();
        if (mapRect == null || !hasWorldBounds) return;

        // BeginGame hides menuRoot before the pre-roll/video/boat sequence
        // begins, so menu visibility alone is not enough.  The minimap waits
        // for the boat's arrival-complete signal before appearing.
        if (startScreen == null) startScreen = FindObjectOfType<ENVStartScreen>(true);
        if (boatArrival == null) boatArrival = FindObjectOfType<ENVBoatArrival>(true);
        bool menuIsOpen = startScreen != null && startScreen.menuRoot != null && startScreen.menuRoot.activeInHierarchy;
        // The former condition only looked at BoatArrival.HasArrived.  Continue
        // Game legitimately bypasses that cinematic, leaving the map hidden for
        // the entire play session.  The menu is the authority on when controls
        // have actually been released to the player.
        bool arrivalFinished = boatArrival == null || !boatArrival.CanPlay || boatArrival.HasArrived;
        bool gameplayReady = startScreen != null ? startScreen.GameplayReady : arrivalFinished;
        bool shouldShow = !menuIsOpen && gameplayReady;
        if (uiCanvas != null) uiCanvas.enabled = shouldShow;
        if (!shouldShow) return;

        UpdatePlayerMarker();
        UpdateObjectiveMarker();
        UpdateStatus();
    }

    [ContextMenu("Refresh Map Bounds")]
    public void RefreshWorldBounds()
    {
        Transform root = mapRoot;
        if (root == null)
        {
            GameObject mapRootObject = GameObject.Find("MapRoot");
            root = mapRootObject != null ? mapRootObject.transform : null;
        }

        Renderer[] renderers = root != null
            ? root.GetComponentsInChildren<Renderer>(true)
            : FindObjectsOfType<Renderer>();

        hasWorldBounds = false;
        foreach (Renderer renderer in renderers)
        {
            if (renderer == null || !renderer.enabled || renderer is ParticleSystemRenderer) continue;
            if (!hasWorldBounds)
            {
                worldBounds = renderer.bounds;
                hasWorldBounds = true;
            }
            else worldBounds.Encapsulate(renderer.bounds);
        }

        // An empty/prototype scene can still display the map and centered marker.
        if (!hasWorldBounds)
        {
            worldBounds = new Bounds(Vector3.zero, new Vector3(100f, 1f, 100f));
            hasWorldBounds = true;
        }
    }

    private void ResolveReferences()
    {
        if (player == null)
        {
            CharacterController controller = FindObjectOfType<CharacterController>();
            if (controller != null) player = controller.transform;
        }
        if (targetCamera == null) targetCamera = Camera.main;
        if (mapRoot == null)
        {
            GameObject root = GameObject.Find("MapRoot");
            if (root != null) mapRoot = root.transform;
        }
        if (dockAnchor == null)
        {
            GameObject dock = GameObject.Find("BoatDockTarget");
            if (dock != null) dockAnchor = dock.transform;
        }
        if (snowAnchor == null)
        {
            GameObject snowGem = GameObject.Find("INT_baoshi_07");
            if (snowGem != null) snowAnchor = snowGem.transform;
        }
    }

    /// <summary>Editor prefab builder entry point. Runtime calls this from Awake.</summary>
    public void BuildUiTemplate()
    {
        BuildUi();
    }

    private void BuildUi()
    {
        if (mapRect != null) return;

        // Existing scene instances were authored at the old 224px size.
        // Promote them at runtime as well as through the installer so the
        // enlarged presentation is immediately visible after recompilation.
        mapDiameter = Mathf.Max(mapDiameter, 290f);

        // Fallback instances still receive a dense, feathered circle rather
        // than the old 128px hard-edge stencil shape.
        circleSprite = circleMaskSprite != null ? circleMaskSprite : CreateCircleSprite(1024, 1f);
        arrowSprite = playerArrowSprite != null ? playerArrowSprite : CreateArrowSprite(96);
        objectiveSprite = circleMaskSprite != null ? circleMaskSprite : CreateCircleSprite(64, 0.82f);

        GameObject canvasObject = new GameObject("ENV Minimap UI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        canvasObject.transform.SetParent(transform, false);
        uiCanvas = canvasObject.GetComponent<Canvas>();
        uiCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        uiCanvas.sortingOrder = 230;
        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        RectTransform rootRect = CreateRect("Minimap Root", canvasObject.transform);
        rootRect.anchorMin = rootRect.anchorMax = new Vector2(0f, 1f);
        rootRect.pivot = new Vector2(0f, 1f);
        // Older scenes stored a right-side negative X offset.  Keep the value
        // usable after moving this UI to the left side without requiring a
        // scene migration before the next play session.
        rootRect.anchoredPosition = new Vector2(Mathf.Abs(screenOffset.x), screenOffset.y);
        rootRect.sizeDelta = new Vector2(mapDiameter + 10f, mapDiameter + 64f);

        // Keep the frame deliberately neutral: the supplied map is the visual
        // treatment now, rather than the old dark/gold prototype frame.
        Image border = CreateImage("Circular Border", rootRect, circleSprite, new Color(1f, 1f, 1f, 0.92f));
        RectTransform borderRect = border.rectTransform;
        borderRect.anchorMin = borderRect.anchorMax = new Vector2(0f, 1f);
        borderRect.pivot = new Vector2(0f, 1f);
        borderRect.anchoredPosition = Vector2.zero;
        borderRect.sizeDelta = new Vector2(mapDiameter + 10f, mapDiameter + 10f);

        Image maskImage = CreateImage("Circular Mask", rootRect, circleSprite, Color.white);
        mapRect = maskImage.rectTransform;
        mapRect.anchorMin = mapRect.anchorMax = new Vector2(0f, 1f);
        mapRect.pivot = new Vector2(0f, 1f);
        mapRect.anchoredPosition = new Vector2(5f, -5f);
        mapRect.sizeDelta = new Vector2(mapDiameter, mapDiameter);
        Mask mask = maskImage.gameObject.AddComponent<Mask>();
        mask.showMaskGraphic = false;

        mapContent = CreateRect("Map Content", mapRect);
        mapContent.anchorMin = mapContent.anchorMax = new Vector2(0.5f, 0.5f);
        mapContent.pivot = new Vector2(0.5f, 0.5f);
        mapContent.sizeDelta = new Vector2(mapDiameter, mapDiameter);
        mapContent.localScale = Vector3.one * mapArtworkScale;

        Image mapImage = CreateImage("Map Artwork", mapContent, CreateMapSprite(), Color.white);
        Stretch(mapImage.rectTransform);
        mapImage.preserveAspect = true;

        // Objective icons live under Map Content, not under the player UI.
        // They therefore remain locked to their world/map coordinates while
        // the artwork pans beneath the centered player arrow.
        Image objective = CreateImage("Next Gem", mapContent, objectiveSprite, new Color(1f, 0.82f, 0.22f));
        objectiveMarker = objective.rectTransform;
        objectiveMarker.sizeDelta = new Vector2(19f, 19f);
        objectiveMarker.localScale = Vector3.one / mapArtworkScale;
        AddOutline(objective, new Color(0.2f, 0.08f, 0.02f, 0.9f), 2f);

        Image marker = CreateImage("Player", mapRect, arrowSprite, new Color(1f, 1f, 1f));
        playerMarker = marker.rectTransform;
        playerMarker.sizeDelta = new Vector2(25f, 31f);
        AddOutline(marker, new Color(0.03f, 0.16f, 0.23f, 0.95f), 2f);

        Image captionBackground = CreateImage("Status Background", rootRect, statusBackgroundSprite, new Color(1f, 1f, 1f, 0.88f));
        if (statusBackgroundSprite == null) captionBackground.color = new Color(0.015f, 0.05f, 0.08f, 0.48f);
        RectTransform captionRect = captionBackground.rectTransform;
        captionRect.anchorMin = captionRect.anchorMax = new Vector2(0f, 1f);
        captionRect.pivot = new Vector2(0f, 1f);
        captionRect.anchoredPosition = new Vector2(2f, -mapDiameter - 21f);
        captionRect.sizeDelta = new Vector2(mapDiameter + 6f, 60f);

        Text diamond = CreateText("Objective Diamond", captionRect, 22, TextAnchor.MiddleCenter, new Color(1f, 0.83f, 0.32f));
        RectTransform diamondRect = diamond.rectTransform;
        diamondRect.anchorMin = diamondRect.anchorMax = new Vector2(0f, 1f);
        diamondRect.pivot = new Vector2(0f, 1f);
        diamondRect.anchoredPosition = new Vector2(13f, -6f);
        diamondRect.sizeDelta = new Vector2(27f, 26f);
        diamond.text = "◆";
        AddShadow(diamond, new Color(0f, 0f, 0f, 0.8f), new Vector2(1f, -1f));

        objectiveTitleLabel = CreateText("Objective Title", captionRect, 20, TextAnchor.MiddleLeft, new Color(1f, 0.86f, 0.42f));
        RectTransform titleRect = objectiveTitleLabel.rectTransform;
        titleRect.anchorMin = titleRect.anchorMax = new Vector2(0f, 1f);
        titleRect.pivot = new Vector2(0f, 1f);
        titleRect.anchoredPosition = new Vector2(42f, -4f);
        titleRect.sizeDelta = new Vector2(mapDiameter - 34f, 29f);
        objectiveTitleLabel.text = "寻找散落的光之宝石";
        AddShadow(objectiveTitleLabel, new Color(0f, 0f, 0f, 0.8f), new Vector2(1f, -1f));

        statusLabel = CreateText("Status", captionRect, 17, TextAnchor.MiddleLeft, new Color(0.94f, 0.97f, 0.98f));
        RectTransform statusRect = statusLabel.rectTransform;
        statusRect.anchorMin = statusRect.anchorMax = new Vector2(0f, 1f);
        statusRect.pivot = new Vector2(0f, 1f);
        statusRect.anchoredPosition = new Vector2(42f, -31f);
        statusRect.sizeDelta = new Vector2(mapDiameter - 34f, 25f);
        statusLabel.text = "已找到 0 / 7 · 正在定位";
        AddShadow(statusLabel, new Color(0f, 0f, 0f, 0.75f), new Vector2(1f, -1f));
    }

    private void UpdatePlayerMarker()
    {
        if (playerMarker == null) return;
        bool visible = player != null;
        if (playerMarker.gameObject.activeSelf != visible) playerMarker.gameObject.SetActive(visible);
        if (!visible) return;
        UpdateMapArtwork(player.position);
        // This is a scrolling minimap: the player remains in the center while
        // the zoomed map moves in the opposite direction.
        playerMarker.anchoredPosition = Vector2.zero;
        playerMarker.localRotation = Quaternion.Euler(0f, 0f, -player.eulerAngles.y);
    }

    private void UpdateObjectiveMarker()
    {
        ENVWorldProgress world = ENVWorldProgress.Instance;
        ENVGem nextGem = world != null ? world.NextGem : null;
        bool visible = nextGem != null;
        if (objectiveMarker.gameObject.activeSelf != visible) objectiveMarker.gameObject.SetActive(visible);
        if (visible) objectiveMarker.anchoredPosition = WorldToMapLocalPosition(nextGem.transform.position);
    }

    private void UpdateStatus()
    {
        if (statusLabel == null) return;
        ENVWorldProgress world = ENVWorldProgress.Instance;
        int total = world != null ? world.expectedGemCount : 7;
        int found = 0;
        ENVGem nextGem = null;
        if (world != null)
        {
            foreach (ENVGem gem in world.Gems) if (gem != null && gem.Activated) found++;
            nextGem = world.NextGem;
        }

        string distance = string.Empty;
        if (player != null && nextGem != null)
            distance = " · 距离 " + Mathf.RoundToInt(Vector3.Distance(player.position, nextGem.transform.position)) + " 米";
        statusLabel.text = "已找到 " + found + " / " + total + distance;
    }

    private void UpdateMapArtwork(Vector3 playerPosition)
    {
        if (mapContent == null) return;
        mapContent.localScale = Vector3.one * mapArtworkScale;
        // mapContent is zoomed, so compensate from the unscaled coordinate.
        mapContent.anchoredPosition = -WorldToMapLocalPosition(playerPosition) * mapArtworkScale;
    }

    private Vector2 WorldToMapLocalPosition(Vector3 position)
    {
        float width = Mathf.Max(0.001f, worldBounds.size.x);
        float depth = Mathf.Max(0.001f, worldBounds.size.z);
        Vector2 mapUv;
        if (dockAnchor != null && snowAnchor != null)
        {
            // The illustration's compass does not match the scene's X/Z axes.
            // Two real-world anchors make the mapping unambiguous: the arrival
            // dock maps to the southern pier and the seventh gem maps to the
            // snow mountain at the top of the image.
            Vector2 worldDock = new Vector2(dockAnchor.position.x, dockAnchor.position.z);
            Vector2 worldSnow = new Vector2(snowAnchor.position.x, snowAnchor.position.z);
            Vector2 worldDirection = worldSnow - worldDock;
            Vector2 mapDirection = snowMapPosition - dockMapPosition;
            if (worldDirection.sqrMagnitude > 0.001f && mapDirection.sqrMagnitude > 0.000001f)
            {
                float rotationRadians = Mathf.Atan2(mapDirection.y, mapDirection.x) -
                                        Mathf.Atan2(worldDirection.y, worldDirection.x);
                float scale = mapDirection.magnitude / worldDirection.magnitude;
                Vector2 worldOffset = new Vector2(position.x, position.z) - worldDock;
                float cos = Mathf.Cos(rotationRadians);
                float sin = Mathf.Sin(rotationRadians);
                Vector2 rotatedOffset = new Vector2(
                    worldOffset.x * cos - worldOffset.y * sin,
                    worldOffset.x * sin + worldOffset.y * cos);
                mapUv = dockMapPosition + rotatedOffset * scale;
            }
            else mapUv = dockMapPosition;
        }
        else if (dockAnchor != null)
        {
            mapUv = dockMapPosition + new Vector2(
                (position.x - dockAnchor.position.x) / width,
                (position.z - dockAnchor.position.z) / depth);
        }
        else
        {
            mapUv = new Vector2(
                Mathf.InverseLerp(worldBounds.min.x, worldBounds.max.x, position.x),
                Mathf.InverseLerp(worldBounds.min.z, worldBounds.max.z, position.z));
        }
        // Do not clamp here: a scrolling map needs its content to continue
        // moving beneath the centered player arrow at the circle edge.
        return new Vector2((mapUv.x - 0.5f) * mapDiameter, (mapUv.y - 0.5f) * mapDiameter);
    }

    private Sprite CreateMapSprite()
    {
        if (mapArtworkSprite != null) return mapArtworkSprite;
        if (mapTexture == null) return circleSprite;
        return Sprite.Create(mapTexture, new Rect(0f, 0f, mapTexture.width, mapTexture.height), new Vector2(0.5f, 0.5f), 100f);
    }

    private static RectTransform CreateRect(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go.GetComponent<RectTransform>();
    }

    private static Image CreateImage(string name, Transform parent, Sprite sprite, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent, false);
        Image image = go.GetComponent<Image>();
        image.sprite = sprite;
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private static Text CreateText(string name, Transform parent, int fontSize, TextAnchor alignment, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        go.transform.SetParent(parent, false);
        Text text = go.GetComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = color;
        text.raycastTarget = false;
        return text;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static void AddOutline(Graphic graphic, Color color, float distance)
    {
        Outline outline = graphic.gameObject.AddComponent<Outline>();
        outline.effectColor = color;
        outline.effectDistance = new Vector2(distance, -distance);
        outline.useGraphicAlpha = false;
    }

    private static void AddShadow(Graphic graphic, Color color, Vector2 distance)
    {
        Shadow shadow = graphic.gameObject.AddComponent<Shadow>();
        shadow.effectColor = color;
        shadow.effectDistance = distance;
        shadow.useGraphicAlpha = false;
    }

    private static Sprite CreateCircleSprite(int size, float radiusScale)
    {
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.name = "Runtime Minimap Circle";
        texture.filterMode = FilterMode.Bilinear;
        float radius = size * 0.5f * radiusScale;
        Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
        Color[] pixels = new Color[size * size];
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            // One texel of coverage feathering prevents the circular UI edge
            // from stair-stepping when Unity scales it on different displays.
            float alpha = Mathf.Clamp01(radius - Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), center) + 0.5f);
            pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
        }
        texture.SetPixels(pixels);
        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
    }

    private static Sprite CreateArrowSprite(int size)
    {
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.name = "Runtime Minimap Player Arrow";
        texture.filterMode = FilterMode.Bilinear;
        Color[] pixels = new Color[size * size];
        Vector2 a = new Vector2(size * 0.5f, size * 0.96f);
        Vector2 b = new Vector2(size * 0.12f, size * 0.12f);
        Vector2 c = new Vector2(size * 0.88f, size * 0.12f);
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            Vector2 p = new Vector2(x + 0.5f, y + 0.5f);
            bool inside = SameSide(p, a, b, c) && SameSide(p, b, c, a) && SameSide(p, c, a, b);
            pixels[y * size + x] = inside ? Color.white : Color.clear;
        }
        texture.SetPixels(pixels);
        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
    }

    private static bool SameSide(Vector2 point, Vector2 lineA, Vector2 lineB, Vector2 opposite)
    {
        float sidePoint = Cross(lineB - lineA, point - lineA);
        float sideOpposite = Cross(lineB - lineA, opposite - lineA);
        return sidePoint * sideOpposite >= 0f;
    }

    private static float Cross(Vector2 a, Vector2 b) { return a.x * b.y - a.y * b.x; }
}
