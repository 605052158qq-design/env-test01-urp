#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class ENVAudioPrefabSetup
{
    private const string Base = "Assets/ENVAudioPlaceholders";
    private const string Prefabs = Base + "/Prefabs";
    private static readonly string[] Files = { "A_Music.wav", "B_Footsteps.wav", "C_Water.wav", "D_Waterfall.wav", "E_Campfire.wav", "F_Gem.wav" };

    [MenuItem("Tools/ENV Audio/01 - Build Audio Bank and Emitter Prefabs")]
    public static void Build()
    {
        if (!AssetDatabase.IsValidFolder(Prefabs)) AssetDatabase.CreateFolder(Base,"Prefabs");
        GameObject root = new GameObject("PF_ENV_AudioBank");
        var bank = root.AddComponent<ENVAudioBank>();
        bank.A_backgroundMusic = Load(0); bank.B_footstep = Load(1);
        bank.C_water = Load(2); bank.D_waterfall = Load(3);
        bank.E_campfire = Load(4); bank.F_gemTouch = Load(5);
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
