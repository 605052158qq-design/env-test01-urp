#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Converts the art team's approved INT_* labels into scene components without
/// changing model geometry, meshes, materials, or the imported FBX.
/// </summary>
public static class ENVNamedInteractionSetup
{
    private const string Menu = "Tools/ENV Test01/11 - Attach Named INT Components";
    // Bump when the required named-object setup changes so the current editor
    // session re-audits existing scenes after scripts reload.
    private const string AutoSetupSessionKey = "ENV.NamedInteractionSetup.AutoApplied.v4";
    private static readonly Regex NumericSuffix = new Regex(@"_(\d+)$", RegexOptions.Compiled);

    [InitializeOnLoadMethod]
    private static void ScheduleRequestedAutoSetup()
    {
        EditorApplication.delayCall += ApplyMissingComponentsForCurrentNamedMap;
    }

    // This is intentionally a one-shot convenience for the currently supplied art map.
    // It does nothing when every supported INT_* object already has its expected components.
    private static void ApplyMissingComponentsForCurrentNamedMap()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || SessionState.GetBool(AutoSetupSessionKey, false)) return;
        Transform root = ResolveMapRoot();
        if (root == null || !HasMissingSeatComponents(root)) return;
        SessionState.SetBool(AutoSetupSessionKey, true);
        AttachMissingSeats(root);
    }

    [MenuItem(Menu)]
    public static void AttachNamedComponents()
    {
        Transform root = ResolveMapRoot();
        if (root == null)
        {
            EditorUtility.DisplayDialog("Named map missing", "Could not find the scene object 'all_mingming'. Place the approved named FBX in the open scene first.", "OK");
            return;
        }

        Undo.IncrementCurrentGroup();
        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Attach named INT components");

        int seats = 0;
        int gems = 0;
        int boats = 0;
        var unmatched = new List<string>();
        foreach (Transform target in root.GetComponentsInChildren<Transform>(true).OrderBy(t => t.name, StringComparer.Ordinal))
        {
            string name = target.name;
            if (!name.StartsWith("INT_", StringComparison.Ordinal)) continue;

            if (name.StartsWith("INT_yizi_", StringComparison.Ordinal) ||
                name.StartsWith("INT_muzhuang_", StringComparison.Ordinal))
            {
                ConfigureSeat(target.gameObject, name);
                seats++;
            }
            else if (name.StartsWith("INT_baoshi_", StringComparison.Ordinal))
            {
                ConfigureGem(target.gameObject, name);
                gems++;
            }
            else if (name.StartsWith("INT_chuan_", StringComparison.Ordinal))
            {
                ConfigureBoat(target.gameObject, name);
                boats++;
            }
            else
            {
                unmatched.Add(name);
            }
        }

        Scene scene = SceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Undo.CollapseUndoOperations(undoGroup);

        Debug.Log($"[ENV INT] Attached: seats={seats}, gems={gems}, boats={boats}. " +
                  "SeatPoint / ExitPoint use safe defaults and require scene placement review. " +
                  "Boat arrival remains disabled until its three scene markers are assigned.", root);
        if (unmatched.Count > 0)
            Debug.LogWarning("[ENV INT] INT_* objects with no configured gameplay role: " + string.Join(", ", unmatched), root);
    }

    [MenuItem("Tools/ENV Test01/12 - Audit Named INT Components")]
    public static void AuditNamedComponents()
    {
        Transform root = ResolveMapRoot();
        if (root == null) return;
        int expected = 0;
        int identities = 0;
        int errors = 0;
        foreach (Transform target in root.GetComponentsInChildren<Transform>(true))
        {
            if (!target.name.StartsWith("INT_", StringComparison.Ordinal)) continue;
            expected++;
            var identity = target.GetComponent<ENVInteractionIdentity>();
            if (identity == null || identity.interactionId != target.name)
            {
                errors++;
                Debug.LogWarning("[ENV INT] Missing or invalid identity: " + target.name, target);
                continue;
            }
            identities++;
            if ((identity.kind == ENVInteractionKind.Seat && target.GetComponent<ENVSeat>() == null) ||
                (identity.kind == ENVInteractionKind.Gem && target.GetComponent<ENVGem>() == null) ||
                (identity.kind == ENVInteractionKind.Boat && target.GetComponent<ENVBoatArrival>() == null))
            {
                errors++;
                Debug.LogWarning("[ENV INT] Interaction component missing: " + target.name, target);
            }
        }
        Debug.Log($"[ENV INT] Audit: INT targets={expected}, identities={identities}, errors={errors}.", root);
    }

    private static void ConfigureSeat(GameObject target, string id)
    {
        ConfigureIdentity(target, id, ENVInteractionKind.Seat);
        ENVSeat seat = AddIfMissing<ENVSeat>(target);
        Transform sit = MarkerChild(target.transform, "SeatPoint", new Vector3(0f, 0.45f, 0f));
        Transform exit = MarkerChild(target.transform, "ExitPoint", new Vector3(0f, 0f, -1.2f));
        Undo.RecordObject(seat, "Configure named seat");
        seat.sitPoint = sit;
        seat.exitPoint = exit;
        EditorUtility.SetDirty(seat);
    }

    private static void ConfigureGem(GameObject target, string id)
    {
        ConfigureIdentity(target, id, ENVInteractionKind.Gem);
        ENVGem gem = AddIfMissing<ENVGem>(target);
        Undo.RecordObject(gem, "Configure named gem");
        gem.gemstoneName = id;
        gem.sequenceIndex = Math.Max(0, ReadSuffix(id) - 1);
        EditorUtility.SetDirty(gem);

        ENVSpinOrFloat motion = AddIfMissing<ENVSpinOrFloat>(target);
        Undo.RecordObject(motion, "Configure gem rotation");
        motion.motion = ENVSpinOrFloat.Motion.Rotate;
        motion.visualPart = target.transform;
        EditorUtility.SetDirty(motion);
    }

    private static void ConfigureBoat(GameObject target, string id)
    {
        ConfigureIdentity(target, id, ENVInteractionKind.Boat);
        ENVBoatArrival boat = target.GetComponent<ENVBoatArrival>();
        if (boat == null)
        {
            boat = Undo.AddComponent<ENVBoatArrival>(target);
            // Markers are deliberately not guessed. A missing setup must be harmless.
            boat.playOnStart = false;
        }
        EditorUtility.SetDirty(boat);
    }

    private static void ConfigureIdentity(GameObject target, string id, ENVInteractionKind kind)
    {
        ENVInteractionIdentity identity = AddIfMissing<ENVInteractionIdentity>(target);
        Undo.RecordObject(identity, "Configure interaction identity");
        identity.interactionId = id;
        identity.kind = kind;
        EditorUtility.SetDirty(identity);
    }

    private static Transform ResolveMapRoot()
    {
        // The scene's named interactables are children of MapRoot.  The imported
        // all_mingming object is a sibling, so starting there silently finds none.
        GameObject mapRoot = GameObject.Find("MapRoot");
        if (mapRoot != null) return mapRoot.transform;
        GameObject named = GameObject.Find("all_mingming");
        if (named != null) return named.transform;
        Transform selected = Selection.activeTransform;
        if (selected != null && selected.gameObject.scene.IsValid())
        {
            Transform current = selected;
            while (current.parent != null) current = current.parent;
            return current;
        }
        return null;
    }

    private static bool HasMissingSupportedComponents(Transform root)
    {
        foreach (Transform target in root.GetComponentsInChildren<Transform>(true))
        {
            string name = target.name;
            if (name.StartsWith("INT_yizi_", StringComparison.Ordinal) || name.StartsWith("INT_muzhuang_", StringComparison.Ordinal))
            {
                ENVSeat seat = target.GetComponent<ENVSeat>();
                if (target.GetComponent<ENVInteractionIdentity>() == null ||
                    seat == null ||
                    seat.sitPoint == null ||
                    seat.exitPoint == null) return true;
            }
            else if (name.StartsWith("INT_baoshi_", StringComparison.Ordinal))
            {
                if (target.GetComponent<ENVInteractionIdentity>() == null || target.GetComponent<ENVGem>() == null || target.GetComponent<ENVSpinOrFloat>() == null) return true;
            }
            else if (name.StartsWith("INT_chuan_", StringComparison.Ordinal))
            {
                if (target.GetComponent<ENVInteractionIdentity>() == null || target.GetComponent<ENVBoatArrival>() == null) return true;
            }
        }
        return false;
    }

    private static bool HasMissingSeatComponents(Transform root)
    {
        foreach (Transform target in root.GetComponentsInChildren<Transform>(true))
        {
            string name = target.name;
            if (!name.StartsWith("INT_yizi_", StringComparison.Ordinal) &&
                !name.StartsWith("INT_muzhuang_", StringComparison.Ordinal)) continue;

            ENVSeat seat = target.GetComponent<ENVSeat>();
            if (target.GetComponent<ENVInteractionIdentity>() == null ||
                seat == null || seat.sitPoint == null || seat.exitPoint == null) return true;
        }
        return false;
    }

    private static void AttachMissingSeats(Transform root)
    {
        int configured = 0;
        foreach (Transform target in root.GetComponentsInChildren<Transform>(true))
        {
            string name = target.name;
            if (!name.StartsWith("INT_yizi_", StringComparison.Ordinal) &&
                !name.StartsWith("INT_muzhuang_", StringComparison.Ordinal)) continue;

            ENVSeat seat = target.GetComponent<ENVSeat>();
            if (target.GetComponent<ENVInteractionIdentity>() != null &&
                seat != null && seat.sitPoint != null && seat.exitPoint != null) continue;

            ConfigureSeat(target.gameObject, name);
            configured++;
        }

        if (configured == 0) return;
        Scene scene = SceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log($"[ENV INT] Auto-configured {configured} named chair/stump seat(s).", root);
    }

    private static Transform MarkerChild(Transform parent, string name, Vector3 localPosition)
    {
        Transform marker = parent.Find(name);
        if (marker != null) return marker;
        var child = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(child, "Create interaction marker");
        child.transform.SetParent(parent, false);
        child.transform.localPosition = localPosition;
        return child.transform;
    }

    private static int ReadSuffix(string value)
    {
        Match match = NumericSuffix.Match(value);
        return match.Success && int.TryParse(match.Groups[1].Value, out int result) ? result : 1;
    }

    private static T AddIfMissing<T>(GameObject target) where T : Component
    {
        T component = target.GetComponent<T>();
        return component != null ? component : Undo.AddComponent<T>(target);
    }
}
#endif
