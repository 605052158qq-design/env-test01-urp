#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>Incremental setup only. Does NOT import/replace/map art objects or overwrite scenes.</summary>
public static class ENVParallelBatchSetup
{
    private const string Menu = "Tools/ENV Test01/Parallel Batch/";
    private const string SysName = "ENV_GameplaySystems";

    [MenuItem(Menu + "00 - Install Core Logic + Region UI + Objective UI")]
    public static void InstallCore()
    {
        GameObject systems = GameObject.Find(SysName);
        if (systems == null) { systems = new GameObject(SysName); Undo.RegisterCreatedObjectUndo(systems, "ENV core systems"); }
        AddIfMissing<ENVWorldProgress>(systems);
        AddIfMissing<ENVRegionTitleUI>(systems);
        AddIfMissing<ENVObjectivePointer>(systems);
        CharacterController cc = Object.FindObjectOfType<CharacterController>();
        if (cc != null)
        {
            AddIfMissing<ENVGemInteractor>(cc.gameObject);
            Debug.Log("ENV: existing player was found and E interaction was attached", cc);
        }
        else Debug.LogWarning("ENV: no CharacterController found. Manually add ENVGemInteractor to your existing Player.");
        SaveScene();
        Debug.Log("ENV core installed. Next: use menu 01 to attach URP screen-color effect.");
    }

    [MenuItem(Menu + "01 - Install Color Recovery on ENV Forward Renderer")]
    public static void InstallColorFeature()
    {
        const string preferredPath = "Assets/Settings/ENV_ForwardRenderer.asset";
        ScriptableRendererData data = AssetDatabase.LoadAssetAtPath<ScriptableRendererData>(preferredPath);
        if (data == null)
        {
            string[] guids = AssetDatabase.FindAssets("t:ScriptableRendererData");
            if (guids.Length == 1) data = AssetDatabase.LoadAssetAtPath<ScriptableRendererData>(AssetDatabase.GUIDToAssetPath(guids[0]));
        }
        if (data == null)
        {
            EditorUtility.DisplayDialog("Renderer missing", "Cannot uniquely find the Universal Renderer asset. Open ENV_ForwardRenderer.asset in Inspector and click Add Renderer Feature > ENV Color Recovery Feature manually.", "OK");
            return;
        }
        foreach (var f in data.rendererFeatures)
            if (f is ENVColorRecoveryFeature) { Debug.Log("ENV color recovery already installed on " + data.name); return; }
        var shader = Shader.Find("Hidden/ENV/ColorRecovery");
        if (shader == null) { Debug.LogError("ColorRecovery shader not found. Check Assets/ENVGameplay/Shaders."); return; }
        var feature = ScriptableObject.CreateInstance<ENVColorRecoveryFeature>();
        feature.name = "ENV Color Recovery (A01-A03)";
        feature.settings.shader = shader;
        AssetDatabase.AddObjectToAsset(feature, data);
        data.rendererFeatures.Add(feature);
        // URP 14 stores renderer feature fileIDs in a second serialized list.
        SerializedObject serialized = new SerializedObject(data);
        SerializedProperty map = serialized.FindProperty("m_RendererFeatureMap");
        if (map != null)
        {
            map.arraySize = data.rendererFeatures.Count;
            for (int i = 0; i < data.rendererFeatures.Count; i++)
            {
                var f = data.rendererFeatures[i];
                if (f != null && AssetDatabase.TryGetGUIDAndLocalFileIdentifier(f, out string _, out long localID))
                    map.GetArrayElementAtIndex(i).longValue = localID;
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
        feature.Create();
        EditorUtility.SetDirty(feature); EditorUtility.SetDirty(data);
        AssetDatabase.SaveAssets(); AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(data));
        Debug.Log("ENV ColorRecoveryFeature installed on " + data.name + ". No map materials changed. Press Play to see grayscale.");
    }

    [MenuItem(Menu + "02 - Selected REAL Gem: Add E Interaction + Spin")]
    public static void SetupGem()
    {
        var obj = Selected(); if (obj == null) return;
        AddIfMissing<ENVGem>(obj);
        var motion = AddIfMissing<ENVSpinOrFloat>(obj);
        motion.motion = ENVSpinOrFloat.Motion.Rotate;
        motion.visualPart = obj.transform;
        EditorUtility.SetDirty(obj);
        SaveScene();
        Debug.Log("Gem configured on actual object. Set sequenceIndex/revealMaxRadius in Inspector.", obj);
    }

    [MenuItem(Menu + "03 - Selected REAL Model: Add Rotate / Float")]
    public static void SetupRotation()
    {
        var obj = Selected(); if (obj == null) return;
        var s = AddIfMissing<ENVSpinOrFloat>(obj);
        s.visualPart = obj.transform;
        SaveScene();
    }

    [MenuItem(Menu + "04 - Selected REAL Lamp/Window: Add Emission")]
    public static void SetupEmission()
    {
        var obj = Selected(); if (obj == null) return;
        var renderer = obj.GetComponent<Renderer>();
        if (renderer == null) { Debug.LogWarning("Select the WINDOW/LAMP mesh with a Renderer, not the whole house root.", obj); return; }
        var s = AddIfMissing<ENVEmissiveLight>(obj);
        s.targets = new[] { renderer };
        SaveScene();
        Debug.Log("Emission attached ONLY to the selected mesh renderer.", obj);
    }

    [MenuItem(Menu + "05 - Selected REAL Water: Convert CURRENT Material Shader")]
    public static void ConvertWater()
    {
        var obj = Selected(); if (obj == null) return;
        Renderer r = obj.GetComponent<Renderer>();
        Shader shader = Shader.Find("ENV/StylizedFlowWaterURP");
        if (r == null || shader == null) { Debug.LogWarning("Select a water MeshRenderer with a material; ensure water shader exists."); return; }
        if (!EditorUtility.DisplayDialog("Update existing water material?",
            "This switches the selected Renderer material asset(s) to the ENV URP water shader. ALL objects sharing the same material will change. No material or model copies are created. Continue?", "Yes", "Cancel")) return;
        foreach (var mat in r.sharedMaterials)
        {
            if (mat == null || mat.shader == shader) continue;
            var texture = mat.HasProperty("_BaseMap") ? mat.GetTexture("_BaseMap") : (mat.HasProperty("_MainTex") ? mat.GetTexture("_MainTex") : null);
            var color = mat.HasProperty("_BaseColor") ? mat.GetColor("_BaseColor") : (mat.HasProperty("_Color") ? mat.GetColor("_Color") : Color.white);
            Undo.RecordObject(mat, "Change Water Shader");
            mat.shader = shader;
            if (texture != null) mat.SetTexture("_BaseMap", texture);
            mat.SetColor("_BaseColor", color);
            EditorUtility.SetDirty(mat);
        }
        var effect = AddIfMissing<ENVWaterStyle>(obj);
        effect.targetRenderer = r;
        effect.ApplyStyle();
        AssetDatabase.SaveAssets(); SaveScene();
        Debug.Log("Water converted on the existing Renderer/material, tweak Flow preset & shader controls.", obj);
    }

    [MenuItem(Menu + "06 - Selected REAL Model: Add Particle Effect")]
    public static void SetupParticles()
    {
        var obj = Selected(); if (obj == null) return;
        AddIfMissing<ParticleSystem>(obj);
        var preset = AddIfMissing<ENVCartoonParticles>(obj);
        preset.urpParticleMaterial = GetOrCreateParticleMaterial();
        preset.ApplyPreset();
        SaveScene();
        Debug.Log("ParticleSystem created on selected real object. Set Look: Gem / Fire / Snow / Fairy.", obj);
    }

    [MenuItem(Menu + "07 - Selected REAL Object: Add 3D Sound Source")]
    public static void SetupAudio()
    {
        var obj = Selected(); if (obj == null) return;
        AddIfMissing<AudioSource>(obj);
        AddIfMissing<ENVSceneAudio>(obj);
        SaveScene();
        Debug.Log("Assign customerAudio in ENVSceneAudio, 3D attenuation already configured.", obj);
    }

    [MenuItem(Menu + "08 - Selected Map: Add Region Trigger Marker")]
    public static void SetupRegion()
    {
        var obj = Selected(); if (obj == null) return;
        var zone = new GameObject("ZoneMarker_ChangeName");
        Undo.RegisterCreatedObjectUndo(zone, "Add region trigger marker");
        zone.transform.SetParent(obj.transform, false);
        var collider = zone.AddComponent<BoxCollider>(); collider.isTrigger = true;
        collider.size = new Vector3(15f, 4f, 15f);
        zone.AddComponent<ENVRegionTrigger>();
        Selection.activeGameObject = zone;
        SaveScene();
        Debug.Log("Set Region Name, then resize the BoxCollider in scene. This is only an invisible trigger, not a duplicate model.", zone);
    }

    [MenuItem(Menu + "09 - Selected Fairy: Add Actual Fairy Follow")]
    public static void SetupFairy()
    {
        var obj = Selected(); if (obj == null) return;
        AddIfMissing<ENVFairyFollower>(obj);
        SaveScene();
    }

    [MenuItem(Menu + "10 - Audit This Scene (No Changes)")]
    public static void AuditScene()
    {
        var world = Object.FindObjectOfType<ENVWorldProgress>();
        var player = Object.FindObjectOfType<ENVGemInteractor>();
        var regions = Object.FindObjectsOfType<ENVRegionTrigger>();
        var gems = Object.FindObjectsOfType<ENVGem>();
        var audio = Object.FindObjectsOfType<ENVSceneAudio>();
        int noClips = 0;
        foreach (var a in audio) if (a.customerAudio == null) noClips++;
        Debug.Log($"[ENV AUDIT] Manager={(world != null)} PlayerE={(player != null)} Gems={gems.Length} RegionZones={regions.Length} AudioSources={audio.Length} AudioMissingClip={noClips}");
        if (world == null || player == null) Debug.LogWarning("ENV: Core logic missing, run menu 00.");
        if (gems.Length == 0) Debug.LogWarning("ENV: Add ENVGem components to the actual gems in your scene.");
        if (regions.Length == 0) Debug.LogWarning("ENV: No location trigger markers in scene.");
        foreach (var gem in gems)
        {
            if (gem.visualRoot == gem.gameObject)
                Debug.LogWarning("ENV: gem.visualRoot must be a child model, not the root with interaction!", gem);
        }
    }

    private static Material GetOrCreateParticleMaterial()
    {
        const string dir = "Assets/ENVGameplay/Materials";
        if (!AssetDatabase.IsValidFolder(dir)) AssetDatabase.CreateFolder("Assets/ENVGameplay", "Materials");
        string path = dir + "/ENV_CartoonParticles.mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat != null) return mat;
        Shader shader = Shader.Find("ENV/CartoonParticleURP");
        if (shader == null) { Debug.LogWarning("Particle shader missing"); return null; }
        mat = new Material(shader) { name = "ENV_CartoonParticles" };
        AssetDatabase.CreateAsset(mat, path);
        return mat;
    }
    private static GameObject Selected()
    {
        var obj = Selection.activeGameObject;
        if (obj != null && obj.scene.IsValid()) return obj;
        EditorUtility.DisplayDialog("Select a scene object", "Select the existing real model in Hierarchy (not an FBX asset in Project window).", "OK");
        return null;
    }
    private static T AddIfMissing<T>(GameObject o) where T : Component
    {
        T c = o.GetComponent<T>();
        if (c == null) c = Undo.AddComponent<T>(o);
        return c;
    }
    private static void SaveScene()
    {
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().IsValid())
            EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
    }
}
#endif
