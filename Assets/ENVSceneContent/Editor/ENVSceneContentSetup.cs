#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Editor-only setup for EXISTING scenery. Creates only gameplay markers/pivots as prefabs.
/// Does not touch existing .meta, materials, URP settings, FBXs or scene objects automatically.
/// </summary>
public sealed class ENVSceneContentSetup : EditorWindow
{
    private const string PrefabFolder = "Assets/ENVSceneContent/GeneratedPrefabs";
    private static readonly string[] Colors = { "蓝", "红", "黄", "青", "橙", "绿", "紫" };
    private static readonly string[] RegionNames = { "微光码头", "心愿广场", "星星花园", "萤火森林", "浮光雪岭" };
    private static readonly string[] RegionKeys = { "Dock", "Square", "Garden", "Forest", "Snow" };
    private readonly GameObject[] realGemObjects = new GameObject[7];
    private GameObject realPlayer;
    private Texture2D gemNoise;
    private bool setupDissolve = true;

    [MenuItem("Tools/ENV Scene Content/01 - Seven Real Gems (Window)")]
    public static void Open() { GetWindow<ENVSceneContentSetup>("ENV Real Gem Setup").Show(); }

    private void OnEnable()
    {
        // Name-based suggestions are NOT treated as proven color associations.
        for (int i = 0; i < 7; i++)
        {
            string name = "INT_baoshi_" + (i + 1).ToString("00");
            var candidate = FindSceneByName(name);
            realGemObjects[i] = candidate;
        }
        gemNoise = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/ENVShaderKit/V3Textures/T_V3_WarpNoise.png");
        var cc = FindObjectOfType<CharacterController>(true);
        if (cc != null) realPlayer = cc.gameObject;
    }

    private static GameObject FindSceneByName(string name)
    {
        foreach (GameObject go in Resources.FindObjectsOfTypeAll<GameObject>())
            if (go.name == name && go.scene.IsValid() && go.scene.isLoaded && !EditorUtility.IsPersistent(go))
                return go;
        return null;
    }

    private void OnGUI()
    {
        EditorGUILayout.HelpBox("Place the 7 REAL gems by COLOR ORDER. Names INT_baoshi_01..07 are only auto-suggestions: verify positions against the customer's map before applying. No duplicate models are created.", MessageType.Warning);
        realPlayer = (GameObject)EditorGUILayout.ObjectField("Existing Player", realPlayer, typeof(GameObject), true);
        gemNoise = (Texture2D)EditorGUILayout.ObjectField("Optional V3 Dissolve Noise", gemNoise, typeof(Texture2D), false);
        setupDissolve = EditorGUILayout.Toggle("Add Dissolve Bridge", setupDissolve);
        EditorGUILayout.Space();
        for (int i = 0; i < 7; i++)
            realGemObjects[i] = (GameObject)EditorGUILayout.ObjectField((i+1) + " - " + Colors[i] + " Gem", realGemObjects[i], typeof(GameObject), true);
        EditorGUILayout.Space();
        if (GUILayout.Button("Apply to 7 REAL Gems (Undo Supported)", GUILayout.Height(33))) ApplySeven();
        EditorGUILayout.Space();
        if (GUILayout.Button("Generate 5 Regions + Tree + Windmill Prefabs", GUILayout.Height(28))) GeneratePrefabs();
        if (GUILayout.Button("Ensure One Region Title UI In Scene", GUILayout.Height(25))) EnsureTitleUI();
    }

    private void ApplySeven()
    {
        var uniq = new HashSet<GameObject>();
        for (int i = 0; i < 7; i++)
        {
            if (realGemObjects[i] == null || !realGemObjects[i].scene.IsValid())
            { EditorUtility.DisplayDialog("Missing gem", "Assign real " + Colors[i] + " gemstone in the currently open scene.", "OK"); return; }
            if (!uniq.Add(realGemObjects[i]))
            { EditorUtility.DisplayDialog("Duplicate", "Every color must use a different real gemstone object.", "OK"); return; }
        }
        if (!EditorUtility.DisplayDialog("Confirm COLOR-to-OBJECT mapping",
            "You checked every gemstone against the customer's location map?\n" +
            "This operation adds/updates ENVGem on the existing objects only.", "Yes, Apply", "Cancel")) return;

        ENVWorldProgress world = FindObjectOfType<ENVWorldProgress>(true);
        if (world == null)
        {
            var sys = new GameObject("ENV_WorldProgress");
            Undo.RegisterCreatedObjectUndo(sys, "Create World Progress");
            world = Undo.AddComponent<ENVWorldProgress>(sys);
        }
        Undo.RecordObject(world, "Set gem sequence");
        world.requireSequence = true;
        EditorUtility.SetDirty(world);
        for (int i = 0; i < 7; i++)
        {
            var go = realGemObjects[i];
            ENVGem gem = go.GetComponent<ENVGem>();
            if (gem == null) gem = Undo.AddComponent<ENVGem>(go);
            Undo.RecordObject(gem, "Bind Gem " + Colors[i]);
            gem.gemstoneName = Colors[i] + "色宝石";
            gem.sequenceIndex = i;
            gem.deactivateVisualOnActivate = false; // otherwise dissolve is invisible
            EditorUtility.SetDirty(gem);
            if (setupDissolve)
            {
                var bridge = go.GetComponent<ENVGemDissolveBridge>();
                if (bridge == null) bridge = Undo.AddComponent<ENVGemDissolveBridge>(go);
                Undo.RecordObject(bridge, "Bind dissolve");
                bridge.gem = gem;
                if (bridge.dissolveNoise == null) bridge.dissolveNoise = gemNoise;
                EditorUtility.SetDirty(bridge);
            }
        }
        if (realPlayer != null && realPlayer.GetComponent<ENVGemInteractor>() == null)
            Undo.AddComponent<ENVGemInteractor>(realPlayer);
        MarkScene();
        Debug.Log("[ENV Setup] 7 existing gemstone objects linked in BLUE/RED/YELLOW/CYAN/ORANGE/GREEN/PURPLE order. Confirm rendering layer hui1-hui7 separately.");
        EditorUtility.DisplayDialog("Configured", "7 gems configured. Existing art/materials untouched. Next: map 7 render-layer groups (hui1..hui7) and test E.", "OK");
    }

    [MenuItem("Tools/ENV Scene Content/02 - Generate Marker and Windmill Prefabs")]
    public static void GeneratePrefabs()
    {
        EnsureFolder("Assets/ENVSceneContent"); EnsureFolder(PrefabFolder);
        for (int i = 0; i < RegionNames.Length; i++)
        {
            var go = new GameObject("PF_Region_" + (i+1).ToString("00") + "_" + RegionKeys[i]);
            var box = go.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.center = new Vector3(0, 1.4f, 0);
            box.size = new Vector3(9, 3, 9); // resize in scene per actual walkable geometry
            var region = go.AddComponent<ENVRegionTrigger>();
            region.regionName = RegionNames[i];
            region.restoredRegionName = RegionNames[i] + " · 已恢复";
            region.once = true;
            region.showSeconds = 2.2f;
            Save(go);
        }
        {
            var go = new GameObject("PF_FairyTree_ProximityTrigger");
            var box = go.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.center = new Vector3(0, 1.2f, 0);
            box.size = new Vector3(4, 2.5f, 4);
            go.AddComponent<ENVFairyIntroTrigger>();
            Save(go);
        }
        {
            var go = new GameObject("PF_Windmill_RotorPivot");
            var rotate = go.AddComponent<ENVSpinOrFloat>();
            rotate.motion = ENVSpinOrFloat.Motion.Rotate;
            rotate.visualPart = go.transform;
            rotate.degreesPerSecond = new Vector3(0, 0, 45); // axis depends on MODEL LOCAL SPACE
            Save(go);
        }
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("Created", "7 Prefabs saved in " + PrefabFolder + ".\nDrop markers in scene and move them to REAL locations.\nWindmill pivot is a logic-only prefab: move ORIGINAL blades underneath or add ENVSpinOrFloat directly to original rotor.", "OK");
    }

    private static void Save(GameObject go)
    {
        string path = PrefabFolder + "/" + go.name + ".prefab";
        if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null)
        {
            // Never silently overwrite existing arranged prefab resources.
            Debug.Log("[ENV] Existing prefab preserved: " + path);
            DestroyImmediate(go);
            return;
        }
        PrefabUtility.SaveAsPrefabAsset(go, path);
        DestroyImmediate(go);
    }

    [MenuItem("Tools/ENV Scene Content/03 - Ensure One Region Title UI")]
    public static void EnsureTitleUI()
    {
        ENVRegionTitleUI region = FindObjectOfType<ENVRegionTitleUI>(true);
        if (region == null)
        {
            GameObject go = new GameObject("ENV_RegionTitleUI");
            Undo.RegisterCreatedObjectUndo(go, "Create Region UI");
            region = Undo.AddComponent<ENVRegionTitleUI>(go);
        }
        if (region.titleText != null && region.group != null)
        {
            Selection.activeObject = region.gameObject;
            Debug.Log("[ENV] Reusing the existing title UI, no changes.");
            return;
        }
        GameObject canvasObj = new GameObject("Region UI Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Undo.RegisterCreatedObjectUndo(canvasObj, "Region UI Canvas");
        canvasObj.transform.SetParent(region.transform, false);
        Canvas canvas = canvasObj.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 200;
        var scaler = canvasObj.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        GameObject textObj = new GameObject("Region Text", typeof(RectTransform), typeof(CanvasGroup), typeof(Text));
        Undo.RegisterCreatedObjectUndo(textObj, "Region Text");
        textObj.transform.SetParent(canvasObj.transform, false);
        Text label = textObj.GetComponent<Text>();
        label.font = Resources.GetBuiltinResource<Font>("Arial.ttf"); // Unity 2022 uGUI builtin
        label.text = "";
        label.fontSize = 46;
        label.alignment = TextAnchor.MiddleCenter;
        label.color = Color.white;
        label.raycastTarget = false;
        var rect = label.rectTransform;
        rect.anchorMin = new Vector2(0.12f, 0.70f);
        rect.anchorMax = new Vector2(0.88f, 0.82f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        Undo.RecordObject(region, "Assign title UI");
        region.titleText = label;
        region.group = textObj.GetComponent<CanvasGroup>();
        region.group.alpha = 0;
        EditorUtility.SetDirty(region);
        Selection.activeGameObject = region.gameObject;
        MarkScene();
    }

    private static void EnsureFolder(string folder)
    {
        if (AssetDatabase.IsValidFolder(folder)) return;
        string parent = Path.GetDirectoryName(folder).Replace('\\', '/');
        string leaf = Path.GetFileName(folder);
        if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, leaf);
    }

    private static void MarkScene()
    {
        if (SceneManager.GetActiveScene().IsValid()) EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
    }
}
#endif
