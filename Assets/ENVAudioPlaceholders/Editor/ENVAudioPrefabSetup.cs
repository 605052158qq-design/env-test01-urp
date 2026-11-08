#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class ENVAudioPrefabSetup
{
    private const string Base = "Assets/ENVAudioPlaceholders";
    private const string Prefabs = Base + "/Prefabs";
    // Keep this source of truth aligned with PF_ENV_AudioBank.  The former A-F
    // files were temporary placeholders; these are the supplied production clips.
    private static readonly string[] Files =
    {
        "环境/选定_全程bgm.mp3",
        "交互/选定_走路.mp3",
        "环境/选定_环境海浪声.mp3",
        "环境/选定_瀑布声.wav",
        "环境/选定_篝火声.mp3",
        "交互/选定_触碰宝石恢复色彩.wav"
    };

    [MenuItem("Tools/ENV Audio/01 - Build Audio Bank and Emitter Prefabs")]
    public static void Build()
    {
        if (!AssetDatabase.IsValidFolder(Prefabs)) AssetDatabase.CreateFolder(Base,"Prefabs");
        GameObject root = new GameObject("PF_ENV_AudioBank");
        var bank = root.AddComponent<ENVAudioBank>();
        bank.A_backgroundMusic = Load(0); bank.B_footstep = Load(1);
        bank.C_water = Load(2); bank.D_waterfall = Load(3);
        bank.E_campfire = Load(4); bank.F_gemTouch = Load(5);
        bank.uiHover = AssetDatabase.LoadAssetAtPath<AudioClip>(Base + "/Clips/UI/选定_ui(鼠标放置在ui上的声音).wav");
        bank.uiClick = AssetDatabase.LoadAssetAtPath<AudioClip>(Base + "/Clips/UI/选定_ui点击ui的声音.wav");
        PrefabUtility.SaveAsPrefabAsset(root,Prefabs+"/PF_ENV_AudioBank.prefab");
        Object.DestroyImmediate(root);

        GameObject emitterRoot = new GameObject("PF_ENV_3D_AudioEmitter");
        emitterRoot.AddComponent<AudioSource>();
        var emitter = emitterRoot.AddComponent<ENVAudioEmitter>();
        emitter.slot = ENVAudioSlot.C_Water;
        emitter.is2D = false;
        PrefabUtility.SaveAsPrefabAsset(emitterRoot,Prefabs+"/PF_ENV_3D_AudioEmitter.prefab");
        Object.DestroyImmediate(emitterRoot);
        AssetDatabase.SaveAssets();
        Debug.Log("[ENV Audio] Generated reusable prefabs in " + Prefabs + ". Drop the bank into scene once. Add emitter prefab as child of your REAL river/fire/gem and choose the slot.");
    }
    private static AudioClip Load(int n)
    {
        string path = Base+"/Clips/"+Files[n];
        var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
        if (clip == null) Debug.LogWarning("[ENV Audio] Missing clip " + path);
        return clip;
    }
}
#endif
