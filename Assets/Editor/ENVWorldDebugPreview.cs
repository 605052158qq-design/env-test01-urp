#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Scene-view-only diagnostic overlay. It never writes to a renderer, material,
/// collider, prefab or scene. Close the window to stop drawing the overlay.
/// </summary>
public sealed class ENVWorldDebugPreview : EditorWindow
{
    private enum PreviewMode { RecoveryGroups, Colliders }
    private static readonly Color[] RecoveryColors =
    {
        new Color(0.3f, 0.55f, 1f), new Color(1f, 0.22f, 0.22f), new Color(1f, 0.9f, 0.15f),
        new Color(0.15f, 0.95f, 1f), new Color(1f, 0.5f, 0.1f), new Color(0.25f, 1f, 0.35f),
        new Color(0.82f, 0.35f, 1f)
    };

    private sealed class RecoveryEntry
    {
        public Renderer renderer;
        public Mesh mesh;
        public int group;
    }

    private readonly List<RecoveryEntry> recoveryEntries = new List<RecoveryEntry>();
    private readonly List<Collider> colliders = new List<Collider>();
    private readonly int[] recoveryCounts = new int[8];
    private readonly bool[] showRecovery = { true, true, true, true, true, true, true, false };
    private Transform recoveryRoot;
    private Transform colliderScope;
    private PreviewMode previewMode = PreviewMode.RecoveryGroups;
    private bool includeInactive = true;
    private bool showTriggers = false;
    private bool showMeshColliders = true;
    private bool showPrimitiveColliders = true;
    private bool onlyCollisionScope;
    private Vector2 scroll;
    private static Material wireMaterial;

    [MenuItem("Tools/ENV Preview/World Debug Preview")]
    public static void Open()
    {
        GetWindow<ENVWorldDebugPreview>("ENV World Preview").Show();
    }

    private void OnEnable()
    {
        if (recoveryRoot == null) recoveryRoot = FindSceneTransform("all_mingming");
        if (colliderScope == null) colliderScope = FindSceneTransform("pengzhuang_map1");
        SceneView.duringSceneGui += DrawSceneOverlay;
        EditorApplication.hierarchyChanged += Refresh;
        Undo.undoRedoPerformed += Refresh;
        Refresh();
    }

    private void OnDisable()
    {
        SceneView.duringSceneGui -= DrawSceneOverlay;
        EditorApplication.hierarchyChanged -= Refresh;
        Undo.undoRedoPerformed -= Refresh;
        SceneView.RepaintAll();
    }

    private void OnGUI()
    {
        scroll = EditorGUILayout.BeginScrollView(scroll);
        EditorGUILayout.HelpBox("这是只读 Scene 预览，不会改灰度分组、材质、Collider 或场景。关闭窗口即可关闭预览。", MessageType.Info);
        previewMode = (PreviewMode)GUILayout.Toolbar((int)previewMode, new[] { "灰度分组预览", "Collider 预览" });

        EditorGUILayout.Space(4);
        if (previewMode == PreviewMode.RecoveryGroups)
        {
            EditorGUILayout.LabelField("灰度恢复分组（灰1 - 灰7）", EditorStyles.boldLabel);
            recoveryRoot = (Transform)EditorGUILayout.ObjectField("地图美术根节点", recoveryRoot, typeof(Transform), true);
            for (int group = 1; group <= 7; group++)
            {
                EditorGUILayout.BeginHorizontal();
                Color before = GUI.color;
                GUI.color = RecoveryColors[group - 1];
                showRecovery[group - 1] = EditorGUILayout.ToggleLeft("灰" + group + "  ·  " + recoveryCounts[group] + " 个 Renderer", showRecovery[group - 1], GUILayout.Width(185));
                GUI.color = before;
                if (GUILayout.Button("只看", GUILayout.Width(42))) ShowOnlyGroup(group);
                if (GUILayout.Button("选中", GUILayout.Width(42))) SelectGroup(group);
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("显示全部灰度组")) { for (int i = 0; i < 7; i++) showRecovery[i] = true; RepaintScene(); }
            if (GUILayout.Button("隐藏全部灰度组")) { for (int i = 0; i < 7; i++) showRecovery[i] = false; RepaintScene(); }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(7);
            EditorGUILayout.LabelField("给当前选中物体设置灰度组（包含全部子物体）", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("会替换该物体原有的灰1–灰7归属，但会保留其它 Rendering Layer 位。可用 Ctrl+Z 撤销。", MessageType.None);
            EditorGUILayout.BeginHorizontal();
            for (int group = 1; group <= 4; group++)
                if (GUILayout.Button("设为 灰" + group)) AssignSelectionGroup(group);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.BeginHorizontal();
            for (int group = 5; group <= 7; group++)
                if (GUILayout.Button("设为 灰" + group)) AssignSelectionGroup(group);
            if (GUILayout.Button("清除灰度组")) AssignSelectionGroup(0);
            EditorGUILayout.EndHorizontal();
        }

        if (previewMode == PreviewMode.Colliders)
        {
            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Collider 预览", EditorStyles.boldLabel);
            colliderScope = (Transform)EditorGUILayout.ObjectField("碰撞根节点（可选）", colliderScope, typeof(Transform), true);
            onlyCollisionScope = EditorGUILayout.ToggleLeft("只显示这个根节点下面的碰撞", onlyCollisionScope);
            includeInactive = EditorGUILayout.ToggleLeft("包含未激活物体", includeInactive);
            showMeshColliders = EditorGUILayout.ToggleLeft("MeshCollider（绿色线框）", showMeshColliders);
            showPrimitiveColliders = EditorGUILayout.ToggleLeft("Box / Sphere / Capsule / Terrain（青色线框）", showPrimitiveColliders);
            showTriggers = EditorGUILayout.ToggleLeft("显示 Trigger（橙色线框）", showTriggers);
            EditorGUILayout.LabelField("当前范围内 Collider：" + colliders.Count);
            if (colliders.Count > 500)
                EditorGUILayout.HelpBox("数量很高，通常表示全部美术网格都被加了 MeshCollider。白线会非常密集；这不是灰度分组。可关闭 MeshCollider，或指定真正的碰撞根节点后只看该根节点。", MessageType.Warning);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("只预览 pengzhuang_map1")) { colliderScope = FindSceneTransform("pengzhuang_map1"); onlyCollisionScope = colliderScope != null; Refresh(); }
            if (GUILayout.Button("预览所有场景碰撞")) { onlyCollisionScope = false; Refresh(); }
            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.Space(8);
        if (GUILayout.Button("刷新扫描 / 重绘 Scene", GUILayout.Height(28))) Refresh();
        EditorGUILayout.EndScrollView();
    }

    private void ShowOnlyGroup(int group)
    {
        for (int i = 0; i < 7; i++) showRecovery[i] = i == group - 1;
        RepaintScene();
    }

    private void SelectGroup(int group)
    {
        var selected = new List<Object>();
        foreach (RecoveryEntry entry in recoveryEntries)
            if (entry.group == group && entry.renderer != null) selected.Add(entry.renderer.gameObject);
        Selection.objects = selected.ToArray();
        if (selected.Count > 0 && SceneView.lastActiveSceneView != null) SceneView.lastActiveSceneView.FrameSelected();
    }

    private void AssignSelectionGroup(int group)
    {
        if (Selection.gameObjects == null || Selection.gameObjects.Length == 0)
        {
            EditorUtility.DisplayDialog("未选择物体", "先在 Hierarchy 或 Scene 中选中一个物体。会同时处理它的所有子物体。", "OK");
            return;
        }
        var unique = new HashSet<Renderer>();
        foreach (GameObject selected in Selection.gameObjects)
        {
            if (selected == null || !selected.scene.IsValid() || EditorUtility.IsPersistent(selected)) continue;
            foreach (Renderer renderer in selected.GetComponentsInChildren<Renderer>(true)) unique.Add(renderer);
        }
        if (unique.Count == 0)
        {
            EditorUtility.DisplayDialog("没有 Renderer", "选中的物体及其子物体中没有可设置的 Renderer。", "OK");
            return;
        }
        const uint RecoveryBits = 0xFEu; // bits 1..7 used by ENVMaterialColorRecovery.
        int changed = 0;
        foreach (Renderer renderer in unique)
        {
            uint replacement = group == 0 ? 0u : 1u << group;
            uint newMask = (renderer.renderingLayerMask & ~RecoveryBits) | replacement;
            if (renderer.renderingLayerMask == newMask) continue;
            Undo.RecordObject(renderer, group == 0 ? "Clear recovery group" : "Assign recovery group " + group);
            renderer.renderingLayerMask = newMask;
            EditorUtility.SetDirty(renderer);
            changed++;
        }
        Debug.Log("[ENV Preview] " + (group == 0 ? "Cleared recovery group on " : "Assigned Gray " + group + " to ") + changed + " Renderer(s).", Selection.activeGameObject);
        Refresh();
    }

    private void Refresh()
    {
        recoveryEntries.Clear();
        colliders.Clear();
        for (int i = 0; i < recoveryCounts.Length; i++) recoveryCounts[i] = 0;

        if (recoveryRoot != null)
        {
            foreach (Renderer renderer in recoveryRoot.GetComponentsInChildren<Renderer>(true))
            {
                int group = GetRecoveryGroup(renderer.renderingLayerMask);
                if (group == 0) continue;
                recoveryCounts[group]++;
                Mesh mesh = GetRendererMesh(renderer);
                if (mesh != null) recoveryEntries.Add(new RecoveryEntry { renderer = renderer, mesh = mesh, group = group });
            }
        }

        if (onlyCollisionScope && colliderScope != null)
            colliders.AddRange(colliderScope.GetComponentsInChildren<Collider>(true));
        else
            foreach (Collider collider in Resources.FindObjectsOfTypeAll<Collider>())
                if (collider != null && collider.gameObject.scene.IsValid() && !EditorUtility.IsPersistent(collider)) colliders.Add(collider);
        Repaint();
        RepaintScene();
    }

    private void DrawSceneOverlay(SceneView sceneView)
    {
        if (Event.current.type != EventType.Repaint) return;
        CompareFunction previousZTest = Handles.zTest;
        Matrix4x4 previousMatrix = Handles.matrix;
        Color previousColor = Handles.color;
        Handles.zTest = CompareFunction.Always;
        try
        {
            if (previewMode == PreviewMode.RecoveryGroups) DrawRecoveryOverlay();
            else DrawColliderOverlay();
        }
        finally
        {
            GL.wireframe = false;
            Handles.zTest = previousZTest;
            Handles.matrix = previousMatrix;
            Handles.color = previousColor;
        }
    }

    private void DrawRecoveryOverlay()
    {
        foreach (RecoveryEntry entry in recoveryEntries)
        {
            if (entry.renderer == null || entry.mesh == null || !entry.renderer.gameObject.activeInHierarchy) continue;
            if (!showRecovery[entry.group - 1]) continue;
            Handles.color = RecoveryColors[entry.group - 1];
            DrawWireMesh(entry.mesh, entry.renderer.localToWorldMatrix, Handles.color);
        }
    }

    private void DrawColliderOverlay()
    {
        foreach (Collider collider in colliders)
        {
            if (collider == null || (!includeInactive && !collider.gameObject.activeInHierarchy)) continue;
            if (collider.isTrigger && !showTriggers) continue;
            bool mesh = collider is MeshCollider;
            if (mesh && !showMeshColliders) continue;
            if (!mesh && !showPrimitiveColliders) continue;
            // White is deliberately unlike all seven recovery colours.
            Handles.color = collider.isTrigger ? new Color(1f, 0.5f, 0.05f) : mesh ? Color.white : new Color(0.1f, 0.9f, 1f);
            DrawCollider(collider);
        }
    }

    private static void DrawCollider(Collider collider)
    {
        MeshCollider meshCollider = collider as MeshCollider;
        if (meshCollider != null && meshCollider.sharedMesh != null)
        {
            DrawWireMesh(meshCollider.sharedMesh, meshCollider.transform.localToWorldMatrix, Handles.color);
            return;
        }
        BoxCollider box = collider as BoxCollider;
        if (box != null)
        {
            Handles.matrix = box.transform.localToWorldMatrix;
            Handles.DrawWireCube(box.center, box.size);
            return;
        }
        // Bounds are exact enough for the remaining collider types and avoid creating any temporary meshes.
        Handles.matrix = Matrix4x4.identity;
        Handles.DrawWireCube(collider.bounds.center, collider.bounds.size);
    }

    private static Mesh GetRendererMesh(Renderer renderer)
    {
        MeshFilter filter = renderer.GetComponent<MeshFilter>();
        if (filter != null) return filter.sharedMesh;
        SkinnedMeshRenderer skinned = renderer as SkinnedMeshRenderer;
        return skinned != null ? skinned.sharedMesh : null;
    }

    // Unity 2022's Handles API does not expose DrawWireMesh.  This is an
    // editor-only immediate draw material; it is never assigned to a Renderer.
    private static void DrawWireMesh(Mesh mesh, Matrix4x4 matrix, Color color)
    {
        if (mesh == null) return;
        if (wireMaterial == null)
        {
            Shader shader = Shader.Find("Hidden/Internal-Colored");
            if (shader == null) return;
            wireMaterial = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
            wireMaterial.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            wireMaterial.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
            wireMaterial.SetInt("_Cull", (int)CullMode.Off);
            wireMaterial.SetInt("_ZWrite", 0);
            wireMaterial.SetInt("_ZTest", (int)CompareFunction.Always);
        }
        wireMaterial.SetColor("_Color", color);
        if (!wireMaterial.SetPass(0)) return;
        GL.wireframe = true;
        for (int subMesh = 0; subMesh < mesh.subMeshCount; subMesh++) Graphics.DrawMeshNow(mesh, matrix, subMesh);
        GL.wireframe = false;
    }

    private static int GetRecoveryGroup(uint mask)
    {
        for (int group = 1; group <= 7; group++) if ((mask & (1u << group)) != 0) return group;
        return 0;
    }

    private static Transform FindSceneTransform(string name)
    {
        foreach (Transform item in Resources.FindObjectsOfTypeAll<Transform>())
            if (item.name == name && item.gameObject.scene.IsValid() && !EditorUtility.IsPersistent(item)) return item;
        return null;
    }

    private static void RepaintScene() { SceneView.RepaintAll(); }
}
#endif
