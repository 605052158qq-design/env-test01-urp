#if UNITY_EDITOR
using System.IO;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// A reversible, editor-only staging layout. It deliberately moves the real
/// named task objects close to the dock for rapid V1 testing, while preserving
/// their original local transforms for one-click restoration.
/// </summary>
public static class ENVTaskDockStagingSetup
{
    private const string StageMenu = "Tools/ENV Test01/18 - Stage V1 Tasks At Dock For Testing _F9";
    private const string RestoreMenu = "Tools/ENV Test01/19 - Restore V1 Tasks From Dock Test Layout";
    private const string HubName = "V1_DockTaskHub";

    // The user requested this layout for the current V1 test scene. It executes
    // once after the next editor script reload, then the hub acts as the marker
    // that prevents any later automatic rearrangement.
    [InitializeOnLoadMethod]
    private static void StageRequestedV1SceneAfterReload()
    {
        EditorApplication.delayCall += () =>
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            Scene scene = SceneManager.GetActiveScene();
            if (scene.path == null || !scene.path.EndsWith("ENV_test01_Main.unity")) return;
            if (GameObject.Find(HubName) != null) return;
            // Never auto-save or rearrange a scene with user changes that have
            // not been saved. The explicit F9 menu command remains available.
            if (scene.isDirty)
            {
                Debug.LogWarning("[ENV Dock Test] Skipped automatic staging because the scene has unsaved changes. " +
                                 "Save first, then run Tools/ENV Test01/18 (F9).");
                return;
            }
            if (FindPlayer() == null || FindSceneTransform("all_mingming") == null) return;
            StageAtDock();
        };
    }

    [MenuItem(StageMenu)]
    public static void StageAtDock()
    {
        CharacterController player = FindPlayer();
        Transform map = FindSceneTransform("all_mingming");
        if (player == null || map == null)
        {
            Debug.LogError("[ENV Dock Test] The existing player and all_mingming map are required.");
            return;
        }

        GameObject hub = GameObject.Find(HubName);
        if (hub == null)
        {
            hub = new GameObject(HubName);
            Undo.RegisterCreatedObjectUndo(hub, "Create dock test hub");
        }

        Vector3 forward = Vector3.ProjectOnPlane(player.transform.forward, Vector3.up).normalized;
        if (forward.sqrMagnitude < 0.01f) forward = Vector3.forward;
        Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
        Vector3 anchor = FindDockAnchor(player.transform.position, forward);
        hub.transform.SetPositionAndRotation(anchor, Quaternion.LookRotation(forward, Vector3.up));

        var tasks = map.GetComponentsInChildren<ENVInteractionIdentity>(true)
            .Where(x => x != null && x.kind != ENVInteractionKind.Boat)
            .OrderBy(x => x.kind)
            .ThenBy(x => x.interactionId)
            .ToList();
        if (tasks.Count == 0)
        {
            Debug.LogError("[ENV Dock Test] No configured seat or gemstone task objects were found.", map);
            return;
        }

        CreateSceneBackup();

        Undo.IncrementCurrentGroup();
        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Stage V1 task objects at dock");
        int placed = 0;
        for (int i = 0; i < tasks.Count; i++)
        {
            ENVInteractionIdentity task = tasks[i];
            Transform target = task.transform;
            ENVTaskDockOrigin origin = target.GetComponent<ENVTaskDockOrigin>();
            if (origin == null) origin = Undo.AddComponent<ENVTaskDockOrigin>(target.gameObject);
            Undo.RecordObject(origin, "Remember original task position");
            origin.CaptureIfNeeded();

            // A compact 5-column grid gives each object room while keeping every
            // interaction within a few seconds' walk from the dock landing.
            int column = i % 5;
            int row = i / 5;
            Vector3 requested = anchor + right * ((column - 2) * 3.1f) + forward * (3.5f + row * 3.3f);
            Undo.RecordObject(target, "Stage task at dock");
            target.position = ProjectToWalkableGround(requested, player.transform.position.y);
            placed++;
        }

        ConfigureSixRegionTestLayout(hub.transform, player.transform, tasks);
        ConfigureCompletionFallback();
        TryStageTree(map, anchor + forward * 9.5f - right * 6f, player.transform);
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        Undo.CollapseUndoOperations(undoGroup);
        Debug.Log("[ENV Dock Test] Staged " + placed + " real seats/logs/gems beside the dock. " +
                  "Use R at a seat or log, and use E on the gems in numerical order. " +
                  "Run menu 19 to restore the authored layout.", hub);
    }

    [MenuItem(RestoreMenu)]
    public static void RestoreFromDock()
    {
        ENVTaskDockOrigin[] staged = Resources.FindObjectsOfTypeAll<ENVTaskDockOrigin>()
            .Where(x => x != null && x.gameObject.scene.IsValid()).ToArray();
        int restored = 0;
        Undo.IncrementCurrentGroup();
        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Restore V1 task object layout");
        foreach (ENVTaskDockOrigin origin in staged)
        {
            if (!origin.captured) continue;
            Undo.RecordObject(origin.transform, "Restore task position");
            origin.Restore();
            restored++;
        }
        GameObject hub = GameObject.Find(HubName);
        if (hub != null) Undo.DestroyObjectImmediate(hub);
        foreach (GameObject zone in Resources.FindObjectsOfTypeAll<GameObject>()
                     .Where(x => x != null && x.scene.IsValid() && x.name.StartsWith("V1_TestRegion_"))
                     .ToArray())
        {
            Undo.DestroyObjectImmediate(zone);
        }
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        Undo.CollapseUndoOperations(undoGroup);
        Debug.Log("[ENV Dock Test] Restored " + restored + " task objects to their authored positions.");
    }

    private static void TryStageTree(Transform map, Vector3 position, Transform player)
    {
        Transform tree = map.GetComponentsInChildren<Transform>(true)
            .FirstOrDefault(x => x.name == "INT_shu_01");
        if (tree == null)
        {
            Debug.LogWarning("[ENV Dock Test] INT_shu_01 is not in the current scene. " +
                             "The tree/fairy task will be included automatically when the named art is imported.", map);
            return;
        }

        ENVTaskDockOrigin origin = tree.GetComponent<ENVTaskDockOrigin>();
        if (origin == null) origin = Undo.AddComponent<ENVTaskDockOrigin>(tree.gameObject);
        Undo.RecordObject(origin, "Remember original tree position");
        origin.CaptureIfNeeded();
        Undo.RecordObject(tree, "Stage tree at dock");
        tree.position = ProjectToWalkableGround(position, player.position.y);

        BoxCollider triggerBox = tree.GetComponent<BoxCollider>();
        if (triggerBox == null) triggerBox = Undo.AddComponent<BoxCollider>(tree.gameObject);
        triggerBox.isTrigger = true;
        triggerBox.center = new Vector3(0f, 1.5f, 0f);
        triggerBox.size = new Vector3(3.5f, 3f, 3.5f);
        ENVFairyIntroTrigger trigger = tree.GetComponent<ENVFairyIntroTrigger>();
        if (trigger == null) trigger = Undo.AddComponent<ENVFairyIntroTrigger>(tree.gameObject);
        trigger.player = player;
        trigger.fairy = Object.FindObjectOfType<ENVFairyFollower>(true);
        trigger.titleUi = Object.FindObjectOfType<ENVRegionTitleUI>(true);
        EditorUtility.SetDirty(trigger);
    }

    private static void ConfigureCompletionFallback()
    {
        GameObject systems = GameObject.Find("ENV_GameplaySystems");
        if (systems == null) return;
        ENVEndSequenceOnCompletion ending = systems.GetComponent<ENVEndSequenceOnCompletion>();
        if (ending == null) ending = Undo.AddComponent<ENVEndSequenceOnCompletion>(systems);
        ending.world = systems.GetComponent<ENVWorldProgress>();
        ending.titleUi = systems.GetComponent<ENVRegionTitleUI>();
        EditorUtility.SetDirty(ending);
    }

    private static void ConfigureSixRegionTestLayout(Transform hub, Transform player, List<ENVInteractionIdentity> tasks)
    {
        ENVGem[] gems = tasks.Select(x => x.GetComponent<ENVGem>()).Where(x => x != null)
            .OrderBy(x => x.sequenceIndex).ToArray();
        if (gems.Length < 7)
        {
            Debug.LogWarning("[ENV Dock Test] The six-region test layout needs all seven named gems; found " + gems.Length + ".", hub);
            return;
        }

        string[] names = { "微光码头", "花园岛", "萤光森林", "营地", "雪山", "终章" };
        int[][] gemIndexes =
        {
            new[] { 0 }, new[] { 1 }, new[] { 2, 3 }, new[] { 4 }, new[] { 5 }, new[] { 6 }
        };
        for (int i = 0; i < names.Length; i++)
        {
            GameObject zone = GameObject.Find("V1_TestRegion_" + (i + 1));
            if (zone == null)
            {
                zone = new GameObject("V1_TestRegion_" + (i + 1));
                Undo.RegisterCreatedObjectUndo(zone, "Create V1 test region");
            }
            zone.transform.position = hub.position + hub.forward * (3f + i * 3.3f);
            BoxCollider box = zone.GetComponent<BoxCollider>();
            if (box == null) box = Undo.AddComponent<BoxCollider>(zone);
            box.isTrigger = true;
            box.size = new Vector3(14f, 5f, 3f);
            ENVRegionTrigger trigger = zone.GetComponent<ENVRegionTrigger>();
            if (trigger == null) trigger = Undo.AddComponent<ENVRegionTrigger>(zone);
            trigger.regionName = names[i];
            trigger.restoredRegionName = names[i] + " · 已恢复";
            trigger.restorationGem = gems[gemIndexes[i][0]];
            trigger.additionalRestorationGems = gemIndexes[i].Skip(1).Select(index => gems[index]).ToArray();
            trigger.player = player;
            trigger.once = true;
            trigger.showSeconds = 2.4f;
            EditorUtility.SetDirty(trigger);
        }
    }

    private static CharacterController FindPlayer()
    {
        ENVPrototypeThirdPersonController controller = Object.FindObjectOfType<ENVPrototypeThirdPersonController>(true);
        return controller != null ? controller.GetComponent<CharacterController>() : Object.FindObjectOfType<CharacterController>(true);
    }

    // A staging run changes authored scene transforms. Keep a human-readable,
    // pre-staging scene copy in addition to Unity's normal Undo and Git history.
    private static void CreateSceneBackup()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (string.IsNullOrEmpty(scene.path)) return;

        // The explicit stage command saves anyway; save first so the backup is
        // the exact pre-staging state rather than an older on-disk revision.
        if (scene.isDirty) EditorSceneManager.SaveScene(scene);

        string projectRoot = Directory.GetParent(Application.dataPath).FullName;
        string backupDirectory = Path.Combine(projectRoot, "Backups");
        string backupPath = Path.Combine(
            backupDirectory,
            Path.GetFileNameWithoutExtension(scene.path) + ".before-dock-task-staging.unity.bak");
        if (File.Exists(backupPath)) return;

        Directory.CreateDirectory(backupDirectory);
        File.Copy(scene.path, backupPath, false);
        Debug.Log("[ENV Dock Test] Created pre-staging scene backup: " + backupPath);
    }

    private static Transform FindSceneTransform(string objectName)
    {
        return Resources.FindObjectsOfTypeAll<Transform>()
            .FirstOrDefault(x => x.gameObject.scene.IsValid() && x.name == objectName);
    }

    private static Vector3 FindDockAnchor(Vector3 fallback, Vector3 forward)
    {
        Transform landing = FindSceneTransform("PlayerLandingTarget");
        return landing != null ? landing.position : fallback + forward * 3f;
    }

    private static Vector3 ProjectToWalkableGround(Vector3 requested, float fallbackY)
    {
        Physics.SyncTransforms();
        Ray ray = new Ray(requested + Vector3.up * 30f, Vector3.down);
        return Physics.Raycast(ray, out RaycastHit hit, 60f, ~0, QueryTriggerInteraction.Ignore)
            ? hit.point
            : new Vector3(requested.x, fallbackY, requested.z);
    }
}
#endif
