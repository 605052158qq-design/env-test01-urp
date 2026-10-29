#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Adds L05 & L06 components to the objects already in the scene, never makes duplicate art.
/// Scene-only Transform markers are permitted for editing docking/seat positions.
/// </summary>
public static class ENVSeatBoatSetup
{
    [MenuItem("Tools/ENV Test01/06 - Install L05 L06 On Existing Player")]
    public static void InstallPlayer()
    {
        GameObject player = ResolvePlayer();
        if (player == null)
        {
            EditorUtility.DisplayDialog("Player missing", "First create an L02/L03 player with menu 04 or 04B, then select its root.", "OK");
            return;
        }
        ENVPlayerInteraction interactions = player.GetComponent<ENVPlayerInteraction>();
        if (interactions == null) interactions = Undo.AddComponent<ENVPlayerInteraction>(player);
        if (interactions.characterAnimator == null)
        {
            interactions.characterAnimator = player.GetComponentInChildren<Animator>(true);
            EditorUtility.SetDirty(interactions);
        }
        Selection.activeGameObject = player;
        Save();
        if (interactions.characterAnimator == null)
            Debug.LogWarning("[ENV L05/L06] Attached to existing player. No Animator found: moving/sitting/boat sequence work, but visual actions need your final character+Animator+clips.", player);
        else
            Debug.Log("[ENV L05/L06] Installed on the existing player; drag Sit/Stand/Ride clips into ENVPlayerInteraction whenever available.", player);
    }

    [MenuItem("Tools/ENV Test01/07 - Make Selected Chair or Log a Seat")]
    public static void MakeSeats()
    {
        GameObject[] selected = Selection.gameObjects.Where(x => x != null && !EditorUtility.IsPersistent(x) && x.scene.IsValid()).ToArray();
        if (selected.Length == 0)
        {
            EditorUtility.DisplayDialog("Select the chair", "In the Hierarchy select the actual chair/log scene object (not an FBX file in Project).", "OK");
            return;
        }
        int added = 0;
        foreach (GameObject go in selected)
        {
            if (go.name == "MapRoot" || go.name == "__Gameplay__") { Debug.LogWarning("[ENV L05] Select one individual chair/log, not the entire map root.", go); continue; }
            ENVSeat seat = go.GetComponent<ENVSeat>();
            if (seat == null) seat = Undo.AddComponent<ENVSeat>(go);
            Transform sit = MarkerChild(go.transform, "SeatPoint", new Vector3(0f, 0.45f, 0f), Quaternion.identity);
            Transform exit = MarkerChild(go.transform, "ExitPoint", new Vector3(0f, 0f, -1.2f), Quaternion.identity);
            Undo.RecordObject(seat, "Assign chair seat markers");
            seat.sitPoint = sit;
            seat.exitPoint = exit;
            EditorUtility.SetDirty(seat);
            added++;
        }
        Save();
        Debug.Log("[ENV L05] " + added + " real chair/log objects configured. In Scene view MOVE SeatPoint to the cushion and ExitPoint to safe walkable ground. The colored gizmos appear when selected.");
    }

    [MenuItem("Tools/ENV Test01/08 - Configure Selected Real Boat Arrival")]
    public static void MakeBoat()
    {
        GameObject boat = Selection.activeGameObject;
        if (boat == null || EditorUtility.IsPersistent(boat) || !boat.scene.IsValid())
        {
            EditorUtility.DisplayDialog("Select a scene boat", "Select the EXISTING boat art object's root in the Hierarchy, not the FBX asset file.", "OK");
            return;
        }
        if (boat.name == "MapRoot" || boat.name == "__Gameplay__")
        {
            EditorUtility.DisplayDialog("Wrong object", "Select the actual boat object, never the map root, or you will move the entire map.", "OK");
            return;
        }
        GameObject playerRoot = ResolvePlayer();
        if (playerRoot == null)
        {
            EditorUtility.DisplayDialog("Player missing", "Install L05/L06 on your existing L02/L03 player first using menu 06.", "OK");
            return;
        }
        ENVPlayerInteraction actor = playerRoot.GetComponent<ENVPlayerInteraction>();
        if (actor == null)
        {
            EditorUtility.DisplayDialog("Player component missing", "Run menu 06 before configuring the real boat.", "OK");
            return;
        }

        ENVBoatArrival arrival = boat.GetComponent<ENVBoatArrival>();
        if (arrival == null) arrival = Undo.AddComponent<ENVBoatArrival>(boat);
        Transform board = MarkerChild(boat.transform, "PlayerOnBoat", new Vector3(0f, 0.8f, 0f), Quaternion.identity);
        // The two dock markers are SCENE markers, NOT children of the moving boat.
        Transform dock = SceneMarker("BoatDockTarget", boat.transform.position + boat.transform.forward * 6f,
            boat.transform.rotation, boat.scene);
        Transform landing = SceneMarker("PlayerLandingTarget", dock.position + dock.right * 2f + dock.forward * 1.5f,
            dock.rotation, boat.scene);
        Undo.RecordObject(arrival, "Configure boat arrival");
        arrival.player = actor;
        arrival.playerOnBoat = board;
        arrival.boatDockTarget = dock;
        arrival.playerLandingTarget = landing;
        EditorUtility.SetDirty(arrival);
        Selection.activeGameObject = boat;
        Save();
        Debug.Log("[ENV L06] Boat script added to YOUR real boat. Reposition PlayerOnBoat (deck), BoatDockTarget (where boat ends), PlayerLandingTarget (safe shore) before pressing Play. No second boat model was created.", boat);
    }

    [MenuItem("Tools/ENV Test01/09 - Validate L05 L06 Scene References")]
    public static void Validate()
    {
        ENVPlayerInteraction[] players = UnityEngine.Object.FindObjectsOfType<ENVPlayerInteraction>(true);
        ENVSeat[] seats = UnityEngine.Object.FindObjectsOfType<ENVSeat>(true);
        ENVBoatArrival[] boats = UnityEngine.Object.FindObjectsOfType<ENVBoatArrival>(true);
        int warnings = 0;
        foreach (ENVSeat s in seats)
        {
            if (s.sitPoint == null || s.exitPoint == null) { warnings++; Debug.LogWarning("[ENV L05] Seat missing anchor: " + s.name, s); }
        }
        foreach (ENVBoatArrival b in boats)
        {
            if (b.player == null || b.playerOnBoat == null || b.boatDockTarget == null || b.playerLandingTarget == null)
            { warnings++; Debug.LogWarning("[ENV L06] Boat setup incomplete: " + b.name, b); }
        }
        if (players.Length == 0) { warnings++; Debug.LogWarning("[ENV L05/L06] No player interaction installed. Run menu 06."); }
        if (players.Length > 1) { warnings++; Debug.LogWarning("[ENV L05/L06] More than one player interaction active. Verify only one playable character is enabled."); }
        Debug.Log("[ENV L05/L06] Scene check: player=" + players.Length + ", seat=" + seats.Length + ", boat=" + boats.Length + ", warnings=" + warnings + ". Check anchor positions manually before Play.");
    }

    private static GameObject ResolvePlayer()
    {
        GameObject selected = Selection.activeGameObject;
        if (selected != null && !EditorUtility.IsPersistent(selected))
        {
            CharacterController cc = selected.GetComponentInParent<CharacterController>();
            if (cc != null) return cc.gameObject;
        }
        ENVPlayerInteraction existing = UnityEngine.Object.FindObjectOfType<ENVPlayerInteraction>();
        if (existing != null) return existing.gameObject;
        CharacterController[] controllers = UnityEngine.Object.FindObjectsOfType<CharacterController>();
        foreach (CharacterController cc in controllers)
        {
            if (cc.GetComponent("ENVPrototypeThirdPersonController") != null) return cc.gameObject;
            if (cc.GetComponent("ThirdPersonController") != null) return cc.gameObject;
        }
        foreach (CharacterController cc in controllers)
        {
            if (cc.name == "ENV_PrototypePlayer" || cc.name == "PlayerArmature" || cc.name == "PlayerCapsule") return cc.gameObject;
        }
        return null;
    }

    private static Transform MarkerChild(Transform parent, string name, Vector3 localPosition, Quaternion localRotation)
    {
        Transform found = parent.Find(name);
        if (found != null) return found;
        GameObject marker = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(marker, "Create " + name + " marker");
        marker.transform.SetParent(parent, false);
        marker.transform.localPosition = localPosition;
        marker.transform.localRotation = localRotation;
        return marker.transform;
    }

    private static Transform SceneMarker(string name, Vector3 position, Quaternion rotation, Scene scene)
    {
        Transform found = scene.GetRootGameObjects().Select(x => x.transform).FirstOrDefault(x => x.name == name);
        if (found != null) return found;
        GameObject marker = new GameObject(name);
        SceneManager.MoveGameObjectToScene(marker, scene);
        Undo.RegisterCreatedObjectUndo(marker, "Create " + name + " marker");
        marker.transform.SetPositionAndRotation(position, rotation);
        return marker.transform;
    }

    private static void Save()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (scene.IsValid() && scene.isLoaded)
        {
            EditorSceneManager.MarkSceneDirty(scene);
            if (!string.IsNullOrEmpty(scene.path)) EditorSceneManager.SaveScene(scene);
        }
    }
}
#endif
