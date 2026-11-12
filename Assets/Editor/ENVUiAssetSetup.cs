#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Video;

/// <summary>
/// Binds the client's current Assets/UI art and new player FBX clips to the
/// existing runtime systems.  It performs all scene changes through Unity's
/// editor API, never by modifying scene YAML.
/// </summary>
public static class ENVUiAssetSetup
{
    private const string ScenePath = "Assets/Scenes/ENV_test01_Main.unity";
    private const string Ui = "Assets/UI/";

    [MenuItem("Tools/ENV Test01/20 - Apply Current UI + Player Animations")]
    public static void ApplyToMainScene()
    {
        EnsureSprite(Ui + "小地图的箭头.png");
        EnsureSprite(Ui + "小地图下方文字背景.png");
        foreach (string title in new[] { "微光码头.png", "心愿广场.png", "星星花园.png", "萤火森林.png", "浮光雪岭.png" })
            EnsureSprite(Ui + title);

        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        Apply();
        EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
        Debug.Log("[ENV UI] Bound supplied minimap, region-title, ending, jump and sit assets to ENV_test01_Main.");
    }

    public static void Apply()
    {
        ENVMinimap minimap = Object.FindObjectOfType<ENVMinimap>(true);
        if (minimap == null)
        {
            GameObject root = new GameObject("ENV Minimap");
            minimap = root.AddComponent<ENVMinimap>();
        }
        minimap.mapTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(Ui + "小地图.png");
        minimap.playerArrowSprite = LoadSprite("小地图的箭头.png");
        minimap.statusBackgroundSprite = LoadSprite("小地图下方文字背景.png");
        EditorUtility.SetDirty(minimap);

        ENVRegionTitleUI titleUi = Object.FindObjectOfType<ENVRegionTitleUI>(true);
        if (titleUi == null)
        {
            GameObject root = new GameObject("ENV Region Title UI");
            titleUi = root.AddComponent<ENVRegionTitleUI>();
        }
        titleUi.dockTitle = LoadSprite("微光码头.png");
        titleUi.squareTitle = LoadSprite("心愿广场.png");
        titleUi.gardenTitle = LoadSprite("星星花园.png");
        titleUi.forestTitle = LoadSprite("萤火森林.png");
        titleUi.snowTitle = LoadSprite("浮光雪岭.png");
        EditorUtility.SetDirty(titleUi);

        AnimationClip jump = LoadClip("Assets/Player/fbx/ani_jump.fbx");
        AnimationClip sit = LoadClip("Assets/Player/fbx/ani_sit.fbx");
        foreach (ENVPlayerVisualAnimator visual in Object.FindObjectsOfType<ENVPlayerVisualAnimator>(true))
        {
            visual.jumpClip = jump;
            EditorUtility.SetDirty(visual);
        }
        foreach (ENVPrototypeThirdPersonController controller in Object.FindObjectsOfType<ENVPrototypeThirdPersonController>(true))
        {
            controller.jumpWindupSeconds = 0.12f;
            EditorUtility.SetDirty(controller);
        }
        foreach (ENVPlayerInteraction interaction in Object.FindObjectsOfType<ENVPlayerInteraction>(true))
        {
            interaction.sitDownClip = sit;
            // The supplied sit file is a one-shot. ENVDirectClipPlayer holds its
            // final seated pose instead of looping it back to the standing start.
            interaction.seatedIdleClip = null;
            interaction.standUpClip = null;
            EditorUtility.SetDirty(interaction);
        }

        foreach (ENVFootsteps footsteps in Object.FindObjectsOfType<ENVFootsteps>(true))
        {
            footsteps.volume = 0.30f;
            EditorUtility.SetDirty(footsteps);
        }
        foreach (ENVAudioBank bank in Object.FindObjectsOfType<ENVAudioBank>(true))
        {
            bank.musicVolume = 0.26f;
            bank.waterVolume = 0.15f;
            bank.waterfallVolume = 0.12f;
            bank.campfireVolume = 0.12f;
            EditorUtility.SetDirty(bank);
        }
        foreach (ENVRegionTrigger region in Object.FindObjectsOfType<ENVRegionTrigger>(true))
        {
            region.spatializeAmbient = true;
            region.ambientMinDistance = 4f;
            region.ambientMaxDistance = 28f;
            region.ambientVolume = region.regionName != null && region.regionName.Contains("森林") ? 0.10f : 0.18f;
            EditorUtility.SetDirty(region);
        }

        Sprite fairyLocatorSprite = LoadSprite("精灵位置ui.png");
        foreach (ENVFairyIntroTrigger encounter in Object.FindObjectsOfType<ENVFairyIntroTrigger>(true))
        {
            ENVFairyLocatorUI locator = encounter.GetComponent<ENVFairyLocatorUI>();
            if (locator == null) locator = encounter.gameObject.AddComponent<ENVFairyLocatorUI>();
            locator.encounter = encounter;
            locator.icon = fairyLocatorSprite;
            EditorUtility.SetDirty(locator);
        }

        Texture2D endingTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(Ui + "微信图片_20261111100300_606_1081.png");
        foreach (ENVEndSequenceOnCompletion end in Object.FindObjectsOfType<ENVEndSequenceOnCompletion>(true))
        {
            end.fairyCompanionEndingBackground = endingTexture;
            EditorUtility.SetDirty(end);
        }

        foreach (ENVStartScreen startScreen in Object.FindObjectsOfType<ENVStartScreen>(true))
        {
            if (startScreen.menuRoot != null)
            {
                ENVMenuVideoBackground background = startScreen.menuRoot.GetComponent<ENVMenuVideoBackground>();
                if (background == null) background = startScreen.menuRoot.AddComponent<ENVMenuVideoBackground>();
                background.suppliedVideo = AssetDatabase.LoadAssetAtPath<VideoClip>(Ui + "开始页面_4k_02.mp4");
                background.playOnAwake = true;
                background.muteAudio = true;
                EditorUtility.SetDirty(background);
            }
            if (startScreen.introSequence != null)
            {
                startScreen.introSequence.suppliedVideo = AssetDatabase.LoadAssetAtPath<VideoClip>(Ui + "开始剧情动画.mp4");
                EditorUtility.SetDirty(startScreen.introSequence);
            }
            EditorUtility.SetDirty(startScreen);
        }
    }

    private static void EnsureSprite(string path)
    {
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null || importer.textureType == TextureImporterType.Sprite) return;
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.alphaIsTransparency = true;
        importer.SaveAndReimport();
    }

    private static Sprite LoadSprite(string filename)
    {
        return AssetDatabase.LoadAssetAtPath<Sprite>(Ui + filename);
    }

    private static AnimationClip LoadClip(string path)
    {
        return AssetDatabase.LoadAllAssetsAtPath(path)
            .OfType<AnimationClip>()
            .FirstOrDefault(clip => !clip.name.StartsWith("__preview__"));
    }
}
#endif
