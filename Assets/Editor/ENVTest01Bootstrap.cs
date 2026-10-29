using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

// Unity 2022.3 + URP 14 repair / scene assembly.
// The scene is saved as a real .unity scene in Assets/Scenes after the initial import.
[InitializeOnLoad]
public static class ENVTest01Bootstrap
{
    private const string ScenePath = "Assets/Scenes/ENV_test01_Main.unity";
    private const string ModelPath = ENVTest01MaterialAssigner.SourcePath;
    private const string UrpPath = "Assets/Settings/ENV_URP.asset";
    private const string RendererPath = "Assets/Settings/ENV_ForwardRenderer.asset";
    private const string MaterialsFolder = "Assets/Map/Materials";
    private static int retries;
    private static bool attempted;

    static ENVTest01Bootstrap()
    {
        EditorApplication.delayCall += TryAutoSetup;
    }

    private static void TryAutoSetup()
    {
        if (attempted) return;
        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            if (++retries < 180) EditorApplication.delayCall += TryAutoSetup;
            return;
        }
        if (AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath) == null ||
            Shader.Find("Universal Render Pipeline/Lit") == null)
        {
            if (++retries < 180) EditorApplication.delayCall += TryAutoSetup;
            return;
        }
        attempted = true;
        // Do not overwrite user scene edits when an assembled scene is already saved.
        string diskScene = Path.Combine(Directory.GetCurrentDirectory(), ScenePath);
        bool mapAlreadySaved = File.Exists(diskScene) && File.ReadAllText(diskScene).Contains("m_Name: MapRoot");
        if (!mapAlreadySaved) BuildAndRepair();
        else EnsureURP();
    }

    [MenuItem("Tools/ENV Test01/01 - Build or Repair Scene + URP + Materials")]
    public static void BuildAndRepair()
    {
        try
        {
            EnsureURP();
            EnsureStandaloneMaterials();
            RemapFBXMaterials();
            BuildScene();
            AssetDatabase.SaveAssets();
            Debug.Log("[ENV Test01] Finished: URP + separate materials + scene saved to " + ScenePath);
        }
        catch (Exception e) { Debug.LogError("[ENV Test01] Build/repair failed: " + e); }
    }

    [MenuItem("Tools/ENV Test01/02 - Repair Material References Only")]
    public static void RepairMaterialsOnly()
    {
        EnsureURP();
        EnsureStandaloneMaterials();
        RemapFBXMaterials();
        BuildScene();
        AssetDatabase.SaveAssets();
    }

    private static void EnsureURP()
    {
        // Assets are already included on disk. Recover only if Unity cannot load them.
        UniversalRendererData renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RendererPath);
        if (renderer == null)
        {
            if (AssetDatabase.LoadMainAssetAtPath(RendererPath) != null)
                AssetDatabase.DeleteAsset(RendererPath);
            renderer = ScriptableObject.CreateInstance<UniversalRendererData>();
            AssetDatabase.CreateAsset(renderer, RendererPath);
        }
        renderer.renderingMode = RenderingMode.Forward;
        renderer.opaqueLayerMask = ~0;
        renderer.transparentLayerMask = ~0;
        EditorUtility.SetDirty(renderer);

        UniversalRenderPipelineAsset urp = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(UrpPath);
        if (urp == null)
        {
            if (AssetDatabase.LoadMainAssetAtPath(UrpPath) != null)
                AssetDatabase.DeleteAsset(UrpPath);
            urp = ScriptableObject.CreateInstance<UniversalRenderPipelineAsset>();
            AssetDatabase.CreateAsset(urp, UrpPath);
        }
        var serialized = new SerializedObject(urp);
        var list = serialized.FindProperty("m_RendererDataList");
        if (list == null) throw new Exception("URP renderer list is missing, verify URP package version 14.x.");
        list.arraySize = 1;
        list.GetArrayElementAtIndex(0).objectReferenceValue = renderer;
        var defaultRenderer = serialized.FindProperty("m_DefaultRendererIndex");
        if (defaultRenderer != null) defaultRenderer.intValue = 0;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        GraphicsSettings.renderPipelineAsset = urp;
        QualitySettings.renderPipeline = urp;
        EditorUtility.SetDirty(urp);
        AssetDatabase.SaveAssets();
    }

    private static void EnsureStandaloneMaterials()
    {
        Shader lit = Shader.Find("Universal Render Pipeline/Lit");
        if (lit == null) throw new Exception("URP Lit shader not installed");
        int count = 0;
        foreach (string id in AssetDatabase.FindAssets("t:Material", new[] { MaterialsFolder }))
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(id));
            if (mat == null) continue;
            // Only change the shader; keep original BaseColor and BaseMap authored in .mat.
            if (mat.shader != lit) mat.shader = lit;
            mat.SetFloat("_Metallic", 0f);
            mat.SetFloat("_Smoothness", 0f);
            EditorUtility.SetDirty(mat);
            count++;
        }
        Debug.Log("[ENV Test01] Independent external URP materials checked: " + count);
    }

    // Unity official remap API: imported FBX material sub-assets -> independent .mat assets.
    private static void RemapFBXMaterials()
    {
        var importer = AssetImporter.GetAtPath(ModelPath) as ModelImporter;
        if (importer == null) throw new Exception("FBX importer missing");
        var internalMaterials = AssetDatabase.LoadAllAssetsAtPath(ModelPath).OfType<Material>();
        int mapped = 0;
        foreach (Material embedded in internalMaterials)
        {
            Material external = ENVTest01MaterialAssigner.Resolve(embedded.name);
            if (external == null) continue;
            importer.AddRemap(new AssetImporter.SourceAssetIdentifier(embedded), external);
            mapped++;
        }
        if (mapped > 0)
        {
            AssetDatabase.WriteImportSettingsIfDirty(ModelPath);
            AssetDatabase.ImportAsset(ModelPath, ImportAssetOptions.ForceUpdate);
        }
        Debug.Log("[ENV Test01] FBX embedded material mappings applied: " + mapped);
    }

    // Creates an external URP material if the importer renamed an unexpected source slot.
    private static Material GetExternalMaterial(Material original, Shader lit)
    {
        if (original == null) return null;
        string currentPath = AssetDatabase.GetAssetPath(original);
        if (currentPath.StartsWith(MaterialsFolder + "/", StringComparison.OrdinalIgnoreCase))
            return original;
        Material mapped = ENVTest01MaterialAssigner.Resolve(original.name);
        if (mapped != null) return mapped;

        // Graceful fallback: preserve editable color, never use a magenta embedded material.
        string safeName = string.Concat(original.name.Select(ch => char.IsLetterOrDigit(ch) || ch == '_' || ch == '-' ? ch : '_'));
        if (safeName.Length > 65) safeName = safeName.Substring(0, 65);
        if (string.IsNullOrEmpty(safeName)) safeName = "Unknown";
        string path = MaterialsFolder + "/Fallback_" + safeName + ".mat";
        Material found = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (found != null) return found;
        var newMat = new Material(lit);
        newMat.name = original.name + "_Standalone";
        Color color = original.HasProperty("_BaseColor") ? original.GetColor("_BaseColor") :
                      original.HasProperty("_Color") ? original.GetColor("_Color") : Color.white;
        newMat.SetColor("_BaseColor", color);
        if (original.HasProperty("_MainTex")) newMat.SetTexture("_BaseMap", original.GetTexture("_MainTex"));
        AssetDatabase.CreateAsset(newMat, path);
        Debug.LogWarning("[ENV Test01] Unmapped FBX material: " + original.name + " -> " + path);
        return newMat;
    }

    private static int ReplaceRendererSlots(Transform root)
    {
        Shader lit = Shader.Find("Universal Render Pipeline/Lit");
        if (lit == null) throw new Exception("Missing URP Lit shader");
        int changed = 0;
        foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
        {
            var slots = renderer.sharedMaterials;
            bool dirty = false;
            for (int i=0; i<slots.Length; ++i)
            {
                var source = slots[i];
                var replacement = GetExternalMaterial(source, lit);
                if (replacement != source && replacement != null)
                {
                    slots[i] = replacement;
                    changed++;
                    dirty = true;
                }
            }
            if (dirty) renderer.sharedMaterials = slots;
        }
        return changed;
    }

    private static void BuildScene()
    {
        var modelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
        if (modelPrefab == null) throw new Exception("FBX not imported");
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        var mapRoot = scene.GetRootGameObjects().FirstOrDefault(go => go.name == "MapRoot");
        if (mapRoot == null)
        {
            mapRoot = new GameObject("MapRoot");
            SceneManager.MoveGameObjectToScene(mapRoot, scene);
        }
        if (mapRoot.transform.childCount == 0)
        {
            var map = PrefabUtility.InstantiatePrefab(modelPrefab, scene) as GameObject;
            if (map == null) throw new Exception("FBX Prefab instantiate failed");
            map.name = "ENV_test01_1021";
            map.transform.SetParent(mapRoot.transform, false);
            map.transform.localPosition = Vector3.zero;
            map.transform.localRotation = Quaternion.identity;
            map.transform.localScale = Vector3.one;
        }
        int remapped = ReplaceRendererSlots(mapRoot.transform);

        Bounds bounds = new Bounds(Vector3.zero, Vector3.one * 10f);
        var renderers = mapRoot.GetComponentsInChildren<Renderer>(true);
        bool first = true;
        foreach (Renderer r in renderers)
        {
            if (first) { bounds = r.bounds; first = false; }
            else bounds.Encapsulate(r.bounds);
        }
        var roots = scene.GetRootGameObjects();
        Camera camera = roots.SelectMany(go => go.GetComponentsInChildren<Camera>(true)).FirstOrDefault();
        if (camera == null)
        {
            var go = new GameObject("Main Camera");
            SceneManager.MoveGameObjectToScene(go, scene);
            go.tag = "MainCamera";
            camera = go.AddComponent<Camera>();
            go.AddComponent<AudioListener>();
            float distance = Mathf.Max(30f, bounds.size.magnitude * 0.85f);
            go.transform.position = bounds.center + new Vector3(.7f,.95f,-1.2f).normalized * distance;
            go.transform.LookAt(bounds.center);
            camera.farClipPlane = Mathf.Max(1000f,distance * 5f);
            camera.fieldOfView = 50f;
            camera.allowHDR = true;
        }
        Light sunlight = scene.GetRootGameObjects()
            .SelectMany(go => go.GetComponentsInChildren<Light>(true))
            .FirstOrDefault(l => l.type == LightType.Directional);
        if (sunlight == null)
        {
            var go = new GameObject("Directional Light");
            SceneManager.MoveGameObjectToScene(go, scene);
            sunlight = go.AddComponent<Light>();
            sunlight.type = LightType.Directional;
            go.transform.rotation = Quaternion.Euler(47f,-35f,0);
            sunlight.intensity = 1.15f;
            sunlight.shadows = LightShadows.Soft;
        }
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath,true) };
        if (SceneView.lastActiveSceneView != null) SceneView.lastActiveSceneView.Frame(bounds,false);
        Debug.Log("[ENV Test01] Scene saved. Renderers=" + renderers.Length + ", external slots remapped=" + remapped);
    }

    [MenuItem("Tools/ENV Test01/03 - Audit Pink & Embedded Materials")]
    public static void CheckMaterials()
    {
        var scene = SceneManager.GetActiveScene();
        var roots = scene.GetRootGameObjects();
        int total=0, embedded=0, wrongShader=0, empty=0;
        foreach (var r in roots.SelectMany(go => go.GetComponentsInChildren<Renderer>(true)))
        foreach (var mat in r.sharedMaterials)
        {
            total++;
            if (mat == null) {empty++;continue;}
            string path = AssetDatabase.GetAssetPath(mat);
            if (path == ModelPath) embedded++;
            if (mat.shader == null || mat.shader.name != "Universal Render Pipeline/Lit") wrongShader++;
        }
        Debug.LogFormat("[ENV Test01] Slots={0}; embedded FBX slots={1}; non-URP-Lit={2}; null={3}; active pipeline={4}",
            total, embedded, wrongShader,empty, GraphicsSettings.currentRenderPipeline != null ? GraphicsSettings.currentRenderPipeline.name : "None");
    }
}
