#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

/// <summary>Builds L10's menu only from the UI source files supplied in Assets/UI.</summary>
public static class ENVStartScreenSetup
{
    private const string Menu = "Tools/ENV Test01/13 - Install V1 Start Screen + Intro";
    private const string RootName = "ENV_StartScreen";
    private const string UiPath = "Assets/UI/";

    [MenuItem(Menu)]
    public static void Install()
    {
        GameObject root = GameObject.Find(RootName);
        if (root == null)
        {
            root = new GameObject(RootName);
            Undo.RegisterCreatedObjectUndo(root, "Create V1 start screen");
        }

        ENVStartScreen screen = GetOrAdd<ENVStartScreen>(root);
        GameObject menu = FindOrCreateChild(root.transform, "MenuCanvas");
        Canvas canvas = GetOrAdd<Canvas>(menu);
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        GetOrAdd<CanvasScaler>(menu).uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        menu.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1920, 1080);
        GetOrAdd<GraphicRaycaster>(menu);

        Image background = FullScreenImage(menu.transform, "MenuBackground", LoadSprite("主界面（整体）_UI.png"));
        background.raycastTarget = false;
        Image start = FullScreenImage(menu.transform, "StartHighlight", LoadSprite("主界面_开始游戏（选中）ui.png"));
        Image resume = FullScreenImage(menu.transform, "ContinueHighlight", LoadSprite("主界面_继续游戏（选中）ui.png"));
        Image exit = FullScreenImage(menu.transform, "ExitHighlight", LoadSprite("主界面_退出游戏（选中）ui.png"));
        start.raycastTarget = resume.raycastTarget = exit.raycastTarget = false;
        start.gameObject.SetActive(false);
        resume.gameObject.SetActive(false);
        exit.gameObject.SetActive(false);

        GameObject sequenceObject = FindOrCreateChild(root.transform, "IntroSequence");
        VideoPlayer player = GetOrAdd<VideoPlayer>(sequenceObject);
        ENVVideoSequence sequence = GetOrAdd<ENVVideoSequence>(sequenceObject);
        sequence.suppliedVideo = AssetDatabase.LoadAssetAtPath<VideoClip>(UiPath + "开始页面_4k效果预览.mp4");
        sequence.targetCamera = Camera.main;
        sequence.playOnStart = false;
        player.playOnAwake = false;

        screen.menuRoot = menu;
        screen.introSequence = sequence;
        screen.startHighlight = start;
        screen.continueHighlight = resume;
        screen.exitHighlight = exit;
        screen.gameplayInput = FindGameplayInput();

        EditorUtility.SetDirty(root);
        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        Debug.Log("[ENV L10] Start screen installed from Assets/UI. Enter, Space, or the supplied Start button starts the intro, then enables player input.", root);
    }

    [MenuItem("Tools/ENV Test01/14 - Validate Start Screen (Play + Screenshot)")]
    public static void ValidateStartScreen()
    {
        if (GameObject.Find(RootName) == null)
        {
            Debug.LogError("[ENV L10] Install the start screen before validating it.");
            return;
        }
        EditorApplication.isPlaying = true;
    }

    [MenuItem("Tools/ENV Test01/15 - Stop Play Mode")]
    public static void StopPlayMode()
    {
        EditorApplication.isPlaying = false;
    }

    private static Behaviour[] FindGameplayInput()
    {
        var player = Object.FindObjectOfType<ENVPrototypeThirdPersonController>();
        if (player == null) return new Behaviour[0];
        var interaction = player.GetComponent<ENVPlayerInteraction>();
        return interaction == null ? new Behaviour[] { player } : new Behaviour[] { player, interaction };
    }

    private static Image FullScreenImage(Transform parent, string name, Sprite sprite)
    {
        GameObject child = FindOrCreateChild(parent, name);
        Image image = GetOrAdd<Image>(child);
        image.sprite = sprite;
        image.preserveAspect = false;
        RectTransform rect = image.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        return image;
    }

    private static Sprite LoadSprite(string fileName)
    {
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(UiPath + fileName);
        if (sprite == null) Debug.LogError("[ENV L10] UI sprite import failed: " + fileName);
        return sprite;
    }

    private static GameObject FindOrCreateChild(Transform parent, string name)
    {
        Transform child = parent.Find(name);
        if (child != null) return child.gameObject;
        GameObject result = new GameObject(name, typeof(RectTransform));
        result.transform.SetParent(parent, false);
        return result;
    }

    private static T GetOrAdd<T>(GameObject gameObject) where T : Component
    {
        T component = gameObject.GetComponent<T>();
        return component != null ? component : Undo.AddComponent<T>(gameObject);
    }
}
#endif
