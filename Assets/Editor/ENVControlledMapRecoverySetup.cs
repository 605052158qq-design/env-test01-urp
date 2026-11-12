#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Safely adopts the artist's regrouped map FBX without touching gameplay
/// objects already added as siblings under MapRoot.  It only swaps the old
/// visual root and assigns the existing recovery rendering-layer bits.
/// </summary>
public sealed class ENVControlledMapRecoverySetup : EditorWindow
{
    private const string NewMapAssetPath = "Assets/ENV_test2_1112.fbx";
    private const string MapRootName = "MapRoot";
    private const string LegacyVisualName = "all_mingming";
    private const string LegacyBackupName = "__Legacy_all_mingming";
    private Transform mapRoot;
    private Transform legacyVisual;
    private GameObject controlledMapAsset;
    private Transform controlledVisual;
    private static readonly string[] ControlledBranchNames =
    {
        "码头", "广场", "花园-黄宝石控制", "花园-绿宝石控制", "森林1", "森林2", "雪山"
    };
    // Bit order was explicitly confirmed by the project owner. Do not infer
    // order from the hierarchy or from words such as forest/snow.
    private static readonly int[] ControlledBranchGroups = { 1, 2, 3, 4, 5, 6, 7 };
    private readonly Transform[] controlledBranches = new Transform[7];
    private Transform resolvedControlRoot;
    private Vector2 scroll;

    [MenuItem("Tools/ENV Scene Content/04 - Controlled Map Colour Recovery")]
    public static void Open() { GetWindow<ENVControlledMapRecoverySetup>("Controlled Map Recovery").Show(); }

    private void OnEnable()
    {
        mapRoot = FindSceneTransform(MapRootName);
        legacyVisual = FindSceneTransform(LegacyVisualName);
        controlledMapAsset = AssetDatabase.LoadAssetAtPath<GameObject>(NewMapAssetPath);
        controlledVisual = FindInstalledControlledVisual();
        RefreshBranches();
    }

    private void OnGUI()
    {
        scroll = EditorGUILayout.BeginScrollView(scroll);
        EditorGUILayout.HelpBox(
            "安全接入方式：只替换 MapRoot 下面的旧视觉地图；MapRoot 同级的宝石、交互、相机等已修改对象完全保留。旧地图只会停用并改名为备份，不删除，可一键恢复。",
            MessageType.Info);
        mapRoot = (Transform)EditorGUILayout.ObjectField("MapRoot", mapRoot, typeof(Transform), true);
        legacyVisual = (Transform)EditorGUILayout.ObjectField("旧视觉地图", legacyVisual, typeof(Transform), true);
        controlledMapAsset = (GameObject)EditorGUILayout.ObjectField("美术受控 FBX", controlledMapAsset, typeof(GameObject), false);
        controlledVisual = (Transform)EditorGUILayout.ObjectField("场景中的受控地图", controlledVisual, typeof(Transform), true);

        EditorGUILayout.Space(5);
        if (controlledVisual == null)
        {
            EditorGUILayout.HelpBox("新 FBX 尚未进入场景。先用下面的安全替换按钮：会保留旧 all_mingming 为禁用备份。", MessageType.Warning);
            using (new EditorGUI.DisabledScope(mapRoot == null || legacyVisual == null || controlledMapAsset == null))
                if (GUILayout.Button("接入新受控地图，并备份旧 all_mingming", GUILayout.Height(32))) InstallControlledMap();
        }
        else
        {
            EditorGUILayout.HelpBox("已按确认的固定规则绑定：码头→灰1，广场→灰2，花园-黄→灰3，花园-绿→灰4，森林1→灰5，森林2→灰6，雪山→灰7。", MessageType.Info);
            if (GUILayout.Button("重新定位并检查七个美术分组")) RefreshBranches();
            DrawFixedBindings();
            using (new EditorGUI.DisabledScope(!AllControlledBranchesFound()))
                if (GUILayout.Button("应用七个固定恢复分组（可 Ctrl+Z）", GUILayout.Height(32))) ApplyGroups();
            if (legacyVisual != null && resolvedControlRoot != null && legacyVisual != resolvedControlRoot && legacyVisual.name == LegacyVisualName)
            {
                EditorGUILayout.Space(5);
                EditorGUILayout.HelpBox("分组写入后，旧 all_mingming 仍会继续渲染。确认新地图材质和位置正确后，使用下面按钮让新地图接管显示；旧地图会保留为禁用备份。", MessageType.Warning);
                if (GUILayout.Button("让当前受控地图接管 all_mingming（保留旧地图备份）", GUILayout.Height(30))) ActivateExistingControlledMap();
            }
        }

        EditorGUILayout.Space(8);
        if (GUILayout.Button("恢复旧 all_mingming，停用新地图")) RestoreLegacyMap();
        EditorGUILayout.EndScrollView();
    }

    private void DrawFixedBindings()
    {
        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("美术受控分组 → 恢复层（固定）", EditorStyles.boldLabel);
        for (int i = 0; i < ControlledBranchNames.Length; i++)
        {
            EditorGUILayout.BeginHorizontal();
            GUI.enabled = false;
            EditorGUILayout.ObjectField(ControlledBranchNames[i], controlledBranches[i], typeof(Transform), true);
            GUI.enabled = true;
            EditorGUILayout.LabelField("灰" + ControlledBranchGroups[i], GUILayout.Width(38));
            EditorGUILayout.LabelField(controlledBranches[i] == null ? "未找到" : "已找到", GUILayout.Width(45));
            EditorGUILayout.EndHorizontal();
        }
        if (resolvedControlRoot != null) EditorGUILayout.LabelField("实际扫描根节点：" + resolvedControlRoot.name);
        if (!AllControlledBranchesFound()) EditorGUILayout.HelpBox("缺少的分组会保持未修改；为避免接错，必须七项全部找到才允许应用。", MessageType.Warning);
    }

    private void InstallControlledMap()
    {
        if (!EditorUtility.DisplayDialog("确认安全替换视觉地图",
            "会执行：\n1. 将旧 all_mingming 停用并改名为备份；\n2. 将 ENV_test2_1112 放在同一位置、同一缩放，作为新的 all_mingming；\n3. 保留 MapRoot 其余所有子物体。\n\n不会删除任何物体，可随时恢复。", "接入", "取消")) return;

        string assetPath = AssetDatabase.GetAssetPath(controlledMapAsset);
        if (string.IsNullOrEmpty(assetPath)) { EditorUtility.DisplayDialog("无效 FBX", "请选择 Project 内的 ENV_test2_1112.fbx。", "OK"); return; }
        AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
        GameObject instance = PrefabUtility.InstantiatePrefab(controlledMapAsset, SceneManager.GetActiveScene()) as GameObject;
        if (instance == null) { EditorUtility.DisplayDialog("接入失败", "无法实例化受控 FBX。", "OK"); return; }
        Undo.RegisterCreatedObjectUndo(instance, "Install controlled map");
        Undo.RecordObject(legacyVisual.gameObject, "Back up old map visual");
        Undo.SetTransformParent(instance.transform, mapRoot, "Parent controlled map under MapRoot");
        instance.transform.localPosition = legacyVisual.localPosition;
        instance.transform.localRotation = legacyVisual.localRotation;
        instance.transform.localScale = legacyVisual.localScale;
        legacyVisual.name = LegacyBackupName;
        legacyVisual.gameObject.SetActive(false);
        instance.name = LegacyVisualName;
        controlledVisual = instance.transform;
        EditorSceneManager.MarkSceneDirty(instance.scene);
        RefreshBranches();
        Debug.Log("[ENV Controlled Map] Installed new art root and kept the old art root disabled as " + LegacyBackupName + ".", instance);
    }

    private void RestoreLegacyMap()
    {
        Transform backup = FindSceneTransform(LegacyBackupName);
        if (backup == null) { EditorUtility.DisplayDialog("没有备份", "未找到 " + LegacyBackupName + "。", "OK"); return; }
        if (controlledVisual != null && controlledVisual != backup)
        {
            Undo.RecordObject(controlledVisual.gameObject, "Disable controlled map");
            controlledVisual.gameObject.SetActive(false);
        }
        Undo.RecordObject(backup.gameObject, "Restore old map visual");
        backup.name = LegacyVisualName;
        backup.gameObject.SetActive(true);
        legacyVisual = backup;
        controlledVisual = null;
        EditorSceneManager.MarkSceneDirty(backup.gameObject.scene);
    }

    private void ActivateExistingControlledMap()
    {
        if (!EditorUtility.DisplayDialog("确认切换视觉地图",
            "会停用旧 all_mingming 并保留为备份；当前 ENV_test2_1112 会移到 MapRoot 下、使用旧地图的位置与缩放，并改名为 all_mingming。\n\nMapRoot 其余子物体不变。",
            "接管显示", "取消")) return;
        Transform newVisual = resolvedControlRoot;
        Transform oldVisual = legacyVisual;
        Undo.RecordObject(oldVisual.gameObject, "Back up old map visual");
        Undo.RecordObject(newVisual.gameObject, "Activate controlled map visual");
        Undo.SetTransformParent(newVisual, mapRoot, "Parent controlled map under MapRoot");
        newVisual.localPosition = oldVisual.localPosition;
        newVisual.localRotation = oldVisual.localRotation;
        newVisual.localScale = oldVisual.localScale;
        oldVisual.name = LegacyBackupName;
        oldVisual.gameObject.SetActive(false);
        newVisual.name = LegacyVisualName;
        legacyVisual = oldVisual;
        controlledVisual = newVisual;
        resolvedControlRoot = newVisual;
        EditorSceneManager.MarkSceneDirty(newVisual.gameObject.scene);
        // This is the new active visual map; give its real mesh hierarchy the
        // same collision coverage that the old visual root had.
        ENVV1WorldSetup.AddCollidersToCurrentActiveMap();
        Debug.Log("[ENV Controlled Map] Current artist-controlled map now owns the all_mingming visual slot; old map remains disabled as backup.", newVisual);
    }

    private void RefreshBranches()
    {
        resolvedControlRoot = ResolveControlRoot();
        for (int i = 0; i < controlledBranches.Length; i++)
            controlledBranches[i] = resolvedControlRoot == null ? null : FindDescendantByName(resolvedControlRoot, ControlledBranchNames[i]);
    }

    private void ApplyGroups()
    {
        if (!AllControlledBranchesFound()) { RefreshBranches(); return; }
        int changed = 0;
        for (int i = 0; i < ControlledBranchNames.Length; i++)
        {
            Transform branch = controlledBranches[i];
            foreach (Renderer renderer in branch.GetComponentsInChildren<Renderer>(true))
            {
                const uint recoveryBits = 0xFEu;
                uint mask = (renderer.renderingLayerMask & ~recoveryBits) | (1u << ControlledBranchGroups[i]);
                if (mask == renderer.renderingLayerMask) continue;
                Undo.RecordObject(renderer, "Assign controlled map recovery group");
                renderer.renderingLayerMask = mask;
                EditorUtility.SetDirty(renderer);
                changed++;
            }
        }
        EditorSceneManager.MarkSceneDirty(resolvedControlRoot.gameObject.scene);
        Debug.Log("[ENV Controlled Map] Assigned recovery layers on " + changed + " renderers. Use Tools/ENV Preview/World Debug Preview to verify each group.", controlledVisual);
    }

    private bool AllControlledBranchesFound()
    {
        for (int i = 0; i < controlledBranches.Length; i++) if (controlledBranches[i] == null) return false;
        return true;
    }

    private static Transform FindSceneTransform(string name)
    {
        foreach (Transform item in Resources.FindObjectsOfTypeAll<Transform>())
            if (item.name == name && item.gameObject.scene.IsValid() && !EditorUtility.IsPersistent(item)) return item;
        return null;
    }

    private Transform ResolveControlRoot()
    {
        // The imported FBX may have an extra importer node (for example
        // "对象183"). Always prefer the artist-authored ENV_test2_1112 root.
        Transform byName = FindSceneTransform("ENV_test2_1112");
        if (byName != null) return byName;
        if (controlledVisual == null || !controlledVisual.gameObject.scene.IsValid()) return null;
        return controlledVisual.name == "ENV_test2_1112"
            ? controlledVisual
            : FindDescendantByName(controlledVisual, "ENV_test2_1112") ?? controlledVisual;
    }

    private static Transform FindDescendantByName(Transform root, string name)
    {
        if (root == null) return null;
        if (root.name == name) return root;
        foreach (Transform item in root.GetComponentsInChildren<Transform>(true))
            if (item.name == name) return item;
        return null;
    }

    private static Transform FindInstalledControlledVisual()
    {
        // The installed instance is deliberately renamed all_mingming so the
        // existing gameplay setup continues to find it.  Identify it by its
        // source FBX instead of by its scene name.
        foreach (Transform item in Resources.FindObjectsOfTypeAll<Transform>())
        {
            if (!item.gameObject.scene.IsValid() || EditorUtility.IsPersistent(item)) continue;
            string sourcePath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(item.gameObject);
            if (!string.Equals(sourcePath, NewMapAssetPath, StringComparison.OrdinalIgnoreCase)) continue;
            Transform parent = item.parent;
            string parentSourcePath = parent == null ? null : PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(parent.gameObject);
            if (!string.Equals(parentSourcePath, NewMapAssetPath, StringComparison.OrdinalIgnoreCase)) return item;
        }
        return FindSceneTransform("ENV_test2_1112");
    }
}
#endif
