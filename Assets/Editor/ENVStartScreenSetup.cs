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
    private const string AudioPath = "Assets/ENVAudioPlaceholders/Clips/UI/";

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

        // Earlier setup used this flattened screenshot as the whole menu. Keep it
        // in the scene for rollback, but never allow it to cover the supplied MP4.
        Transform legacyBackground = menu.transform.Find("MenuBackground");
        if (legacyBackground != null) legacyBackground.gameObject.SetActive(false);

        ENVMenuVideoBackground background = GetOrAdd<ENVMenuVideoBackground>(menu);
        background.suppliedVideo = AssetDatabase.LoadAssetAtPath<VideoClip>(UiPath + "开始页面_4k_02.mp4");
        background.playOnAwake = true;
        background.muteAudio = true;
        Image title = FullScreenImage(menu.transform, "Title", LoadSprite("主界面_标题ui.png"));
        Image startNormal = FullScreenImage(menu.transform, "StartNormal", LoadSprite("主界面_开始游戏ui.png"));
        Image resumeNormal = FullScreenImage(menu.transform, "ContinueNormal", LoadSprite("主界面_继续游戏ui.png"));
        Image exitNormal = FullScreenImage(menu.transform, "ExitNormal", LoadSprite("主界面_退出游戏ui.png"));
        title.raycastTarget = startNormal.raycastTarget = resumeNormal.raycastTarget = exitNormal.raycastTarget = false;
        Image start = FullScreenImage(menu.transform, "StartHighlight", LoadSprite("主界面_开始游戏（选中）ui.png"));
        Image resume = FullScreenImage(menu.transform, "ContinueHighlight", LoadSprite("主界面_继续游戏（选中）ui.png"));
        Image exit = FullScreenImage(menu.transform, "ExitHighlight", LoadSprite("主界面_退出游戏（选中）ui.png"));
        start.raycastTarget = resume.raycastTarget = exit.raycastTarget = false;
        start.gameObject.SetActive(false);
        resume.gameObject.SetActive(false);
        exit.gameObject.SetActive(false);

        // Remove the retired caption-card fallback if an older scene still has it.
        Transform legacyCutscene = root.transform.Find("DockingCutscene");
        if (legacyCutscene != null)
            Undo.DestroyObjectImmediate(legacyCutscene.gameObject);

        ENVVideoSequence introSequence = Object.FindObjectOfType<ENVVideoSequence>(true);
        if (introSequence == null)
        {
            GameObject introObject = new GameObject("IntroSequence");
            introObject.AddComponent<VideoPlayer>();
            introSequence = introObject.AddComponent<ENVVideoSequence>();
        }
        introSequence.suppliedVideo = AssetDatabase.LoadAssetAtPath<VideoClip>(UiPath + "开始剧情动画.mp4");
        introSequence.targetCamera = Camera.main;
        introSequence.playOnStart = false;

        screen.menuRoot = menu;
        screen.introSequence = introSequence;
        // Explicit UnityEngine qualification also keeps this static editor utility
        // independent from any project type named Object.
        screen.boatArrival = UnityEngine.Object.FindObjectOfType<ENVBoatArrival>(true);
        if (screen.boatArrival != null)
        {
            screen.boatArrival.enabled = true;
            screen.boatArrival.playOnStart = false;
            EditorUtility.SetDirty(screen.boatArrival);
        }
        screen.startHighlight = start;
        screen.continueHighlight = resume;
        screen.exitHighlight = exit;
        screen.hoverAudio = AssetDatabase.LoadAssetAtPath<AudioClip>(AudioPath + "选定_ui(鼠标放置在ui上的声音).wav");
        screen.clickAudio = AssetDatabase.LoadAssetAtPath<AudioClip>(AudioPath + "选定_ui点击ui的声音.wav");
        screen.gameplayInput = FindGameplayInput();

        EditorUtility.SetDirty(root);
        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        Debug.Log("[ENV L10] Menu now uses 开始页面_4k_02.mp4 as its looping background. Start plays the placeholder docking handoff, then L06 docks the real boat.", root);
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
        var camera = Object.FindObjectOfType<ENVPrototypeOrbitCamera>();
        if (player == null)
            return camera == null ? new Behaviour[0] : new Behaviour[] { camera };
        var interaction = player.GetComponent<ENVPlayerInteraction>();
        if (interaction == null)
            return camera == null ? new Behaviour[] { player } : new Behaviour[] { player, camera };
        return camera == null
            ? new Behaviour[] { player, interaction }
            : new Behaviour[] { player, interaction, camera };
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
