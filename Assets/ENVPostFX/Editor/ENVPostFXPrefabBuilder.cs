#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// ENV post processing asset generator for Unity 2022.3 / URP 14.
/// No runtime component, renderer feature, material, or gameplay integration is required.
/// Auto-generates a prefab and 3 real, persistent VolumeProfile assets after import.
/// </summary>
public static class ENVPostFXPrefabBuilder
{
    private const string Root = "Assets/ENVPostFX";
    private const string Profiles = Root + "/Profiles";
    private const string Prefabs = Root + "/Prefabs";
    private const string PrefabPath = Prefabs + "/ENV_GlobalPostFX_Cartoon.prefab";
    private const string CartoonPath = Profiles + "/ENV_PP_Cartoon_Balanced.asset";
    private const string VividPath = Profiles + "/ENV_PP_Fantasy_Vivid.asset";
    private const string SoftPath = Profiles + "/ENV_PP_Mobile_Soft.asset";

    [InitializeOnLoadMethod]
    private static void ScheduleGenerateIfMissing()
    {
        EditorApplication.delayCall += () =>
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode)
                return;
            if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null)
                return;
            try { EnsureAssets(); }
            catch (Exception e) { Debug.LogError("[ENV PostFX] Auto generation failed: " + e); }
        };
    }

    [MenuItem("Tools/ENV PostFX/01 - Generate Missing Prefab and Profiles")]
    private static void GenerateMissing()
    {
        EnsureAssets();
        EditorUtility.DisplayDialog("ENV PostFX", "Resources ready. Drag ENV_GlobalPostFX_Cartoon.prefab into the scene.\n\nIf no visual change: enable Post Processing on your Base Camera, or use menu 03.", "OK");
    }

    private static void EnsureAssets()
    {
        EnsureFolder(Root, "Assets", "ENVPostFX");
        EnsureFolder(Profiles, Root, "Profiles");
        EnsureFolder(Prefabs, Root, "Prefabs");

        var cartoon = CreateProfileIfMissing(CartoonPath, "Cartoon balanced", 1.00f, 0.48f, 0.67f, 9f, 8f, 0.02f, 0.09f);
        CreateProfileIfMissing(VividPath, "Fantasy vivid", 0.92f, 0.76f, 0.72f, 18f, 11f, 0.06f, 0.12f);
        CreateProfileIfMissing(SoftPath, "Mobile soft", 1.18f, 0.26f, 0.60f, 4f, 5f, 0.00f, 0.05f);
        AssetDatabase.SaveAssets();

        // Do not replace an existing Prefab; do not erase user-authored profile tuning.
        if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) == null)
        {
            var go = new GameObject("ENV_GlobalPostFX_Cartoon");
            go.layer = 0; // Default: the common camera Volume Mask includes this layer.
            try
            {
                var volume = go.AddComponent<Volume>();
                volume.isGlobal = true;
                volume.priority = 20f;
                volume.weight = 1f;
                volume.sharedProfile = cartoon;
                PrefabUtility.SaveAsPrefabAsset(go, PrefabPath);
            }
            finally { UnityEngine.Object.DestroyImmediate(go); }
        }
        AssetDatabase.SaveAssets();
        Debug.Log("[ENV PostFX] Prefab: " + PrefabPath + "\nProfiles: " + Profiles);
    }

    private static void EnsureFolder(string path, string parent, string folderName)
    {
        if (!AssetDatabase.IsValidFolder(path))
            AssetDatabase.CreateFolder(parent, folderName);
    }

    private static T AddPersistent<T>(VolumeProfile profile) where T : VolumeComponent
    {
        T component = profile.Add<T>(true);
        component.name = typeof(T).Name;
        AssetDatabase.AddObjectToAsset(component, profile);
        EditorUtility.SetDirty(component);
        EditorUtility.SetDirty(profile);
        return component;
    }

    private static VolumeProfile CreateProfileIfMissing(string path, string friendlyName,
        float threshold, float intensity, float scatter, float saturation,
        float contrast, float exposure, float vignetteIntensity)
    {
        var old = AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
        if (old != null)
            return old;

        VolumeProfile profile = ScriptableObject.CreateInstance<VolumeProfile>();
        profile.name = friendlyName;
        AssetDatabase.CreateAsset(profile, path);

        // NOTE: VolumeProfile.Add alone does NOT persist child objects in an .asset.
        // Each override is added as a subasset so it survives Editor restarts/reimports.
        Bloom bloom = AddPersistent<Bloom>(profile);
        bloom.threshold.Override(threshold);
        bloom.intensity.Override(intensity);
        bloom.scatter.Override(scatter);
        bloom.highQualityFiltering.Override(false);
        bloom.maxIterations.Override(5);

        Tonemapping tonemap = AddPersistent<Tonemapping>(profile);
        tonemap.mode.Override(TonemappingMode.Neutral);

        ColorAdjustments colors = AddPersistent<ColorAdjustments>(profile);
        colors.saturation.Override(saturation);
        colors.contrast.Override(contrast);
        colors.postExposure.Override(exposure);

        Vignette vignette = AddPersistent<Vignette>(profile);
        vignette.intensity.Override(vignetteIntensity);
        vignette.smoothness.Override(0.34f);
        vignette.rounded.Override(false);

        EditorUtility.SetDirty(bloom);
        EditorUtility.SetDirty(tonemap);
        EditorUtility.SetDirty(colors);
        EditorUtility.SetDirty(vignette);
        EditorUtility.SetDirty(profile);
        AssetDatabase.SaveAssets();

        // Verify that actual persistent override subassets survived serialization.
        var loaded = AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
        if (loaded == null || loaded.components.Count < 4 ||
            !loaded.Has<Bloom>() || !loaded.Has<Tonemapping>() ||
            !loaded.Has<ColorAdjustments>() || !loaded.Has<Vignette>())
        {
            throw new InvalidOperationException("Volume overrides are missing or not persisted: " + path);
        }
        return loaded;
    }

    [MenuItem("Tools/ENV PostFX/02 - Put Prefab Into Current Scene (No Duplicates)")]
    private static void AddToScene()
    {
        EnsureAssets();
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (prefab == null) { Debug.LogError("[ENV PostFX] Prefab not generated."); return; }
        var existing = UnityEngine.Object.FindObjectsOfType<Volume>(true);
        foreach (var v in existing)
        {
            if (v.gameObject.scene == UnityEngine.SceneManagement.SceneManager.GetActiveScene() &&
                v.gameObject.name.StartsWith("ENV_GlobalPostFX_Cartoon", StringComparison.Ordinal))
            {
                Selection.activeGameObject = v.gameObject;
                Debug.Log("[ENV PostFX] Already in active scene; not duplicated.");
                return;
            }
        }
        var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        Undo.RegisterCreatedObjectUndo(instance, "Add ENV PostFX volume");
        Selection.activeGameObject = instance;
        EditorSceneManager.MarkSceneDirty(instance.scene);
        Debug.Log("[ENV PostFX] Added to current scene. Enable camera PP if you don't see a change.");
    }

    [MenuItem("Tools/ENV PostFX/03 - Enable Post Processing On Scene Base Cameras")]
    private static void EnableCameras()
    {
        int count = 0;
        foreach (var cam in UnityEngine.Object.FindObjectsOfType<Camera>(true))
        {
            if (!cam.gameObject.scene.IsValid() || !cam.gameObject.scene.isLoaded)
                continue;
            var data = cam.GetUniversalAdditionalCameraData();
            if (data.renderType != CameraRenderType.Base)
                continue;
            Undo.RecordObject(cam, "Enable HDR for ENV PostFX");
            Undo.RecordObject(data, "Enable URP postprocessing");
            cam.allowHDR = true;
            data.renderPostProcessing = true;
            LayerMask mask = data.volumeLayerMask;
            mask.value |= 1; // include Default layer where our prefab lives
            data.volumeLayerMask = mask;
            EditorUtility.SetDirty(cam);
            EditorUtility.SetDirty(data);
            EditorSceneManager.MarkSceneDirty(cam.gameObject.scene);
            count++;
        }
        Debug.Log("[ENV PostFX] Enabled post processing and HDR on " + count + " Base scene cameras.");
        if (count == 0)
            Debug.LogWarning("[ENV PostFX] No Base Camera found. Check your active game camera and enable Rendering > Post Processing manually.");
    }

    [MenuItem("Tools/ENV PostFX/04 - Diagnose Active Scene")]
    private static void Diagnose()
    {
        int volumeCount = 0, cameraCount = 0, enabledCount = 0;
        foreach (var v in UnityEngine.Object.FindObjectsOfType<Volume>(true))
        {
            if (!v.gameObject.scene.IsValid() || !v.gameObject.scene.isLoaded) continue;
            volumeCount++;
            Debug.Log("[ENV PostFX] Volume: " + v.name + "; global=" + v.isGlobal +
                      "; weight=" + v.weight + "; priority=" + v.priority +
                      "; profile=" + (v.sharedProfile ? v.sharedProfile.name : "NONE"));
        }
        foreach (var cam in UnityEngine.Object.FindObjectsOfType<Camera>(true))
        {
            if (!cam.gameObject.scene.IsValid() || !cam.gameObject.scene.isLoaded) continue;
            var data = cam.GetUniversalAdditionalCameraData();
            if (data.renderType != CameraRenderType.Base) continue;
            cameraCount++;
            if (data.renderPostProcessing) enabledCount++;
            Debug.Log("[ENV PostFX] Camera: " + cam.name +
                      "; postProcessing=" + data.renderPostProcessing +
                      "; HDR=" + cam.allowHDR + "; mask=" + data.volumeLayerMask.value);
        }
        Debug.Log("[ENV PostFX] " + volumeCount + " volumes, " + enabledCount + "/" + cameraCount + " Base cameras enabled.\n" +
                  "Check Game view and Scene view Post Processing toggle. Other Global Volumes may override this one.");
    }
}
#endif
