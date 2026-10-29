#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// L02/L03 additive setup: do not modify MapRoot, materials, URP assets or packages.
// Menu 04 uses Unity's official Starter Assets only if the dependencies are actually imported.
// Menu 04B is an optional standalone prototype if the official assets have not been imported.
public static class ENVThirdPersonSetup
{
    const string GameplayRoot = "__Gameplay__";
    const string OfficialCameraName = "PlayerFollowCamera";
    const string PrototypePlayerName = "ENV_PrototypePlayer";

    [MenuItem("Tools/ENV Test01/04 - Add Official Third Person (L02 + L03)")]
    public static void AddOfficialThirdPerson()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!CheckScene(scene)) return;

        GameObject playerPrefab = FindPrefab("PlayerArmature", "PlayerCapsule");
        GameObject cameraPrefab = FindPrefab(OfficialCameraName);
        Type brainType = FindType("Cinemachine.CinemachineBrain", "Unity.Cinemachine.CinemachineBrain");
        Type inputType = FindType("UnityEngine.InputSystem.PlayerInput");
        if (playerPrefab == null || cameraPrefab == null || brainType == null || inputType == null)
        {
            string details = (playerPrefab == null ? "- PlayerArmature / PlayerCapsule prefab\n" : "") +
                             (cameraPrefab == null ? "- PlayerFollowCamera prefab\n" : "") +
                             (brainType == null ? "- Cinemachine\n" : "") +
                             (inputType == null ? "- Input System\n" : "");
            int choice = EditorUtility.DisplayDialogComplex(
                "Starter Assets resources missing",
                "The official controller can't be created yet.\n\nMissing:\n" + details +
                "\nImport the Unity 2022.3-compatible Third Person Starter Assets " +
                "(including its Samples), Cinemachine and Input System.\n\n" +
                "You can use a temporary built-in controller while installing them.",
                "Create temporary controller", "Cancel", "Official package page");
            if (choice == 0) AddPrototypeThirdPerson();
            else if (choice == 2) Application.OpenURL("https://assetstore.unity.com/packages/3d/characters/first-person-third-person-character-controllers-196526");
            return;
        }

        GameObject gameplay = GetOrCreateRoot(scene);
        GameObject prototype = FindSceneObject(scene, PrototypePlayerName);
        Vector3 spawn = prototype != null ? prototype.transform.position : SuggestedSpawnPosition();
        GameObject player = FindSceneObject(scene, "PlayerArmature") ?? FindSceneObject(scene, "PlayerCapsule");
        if (player == null)
        {
            player = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab, scene);
            if (player == null) { Debug.LogError("[ENV L02/L03] Failed to instantiate official player prefab."); return; }
            Undo.RegisterCreatedObjectUndo(player, "Add Official Starter Assets Player");
            player.transform.SetParent(gameplay.transform, true);
            player.transform.position = spawn;
        }
        Transform followTarget = FindChild(player.transform, "PlayerCameraRoot") ??
                                 FindChild(player.transform, "CinemachineCameraTarget");
        if (followTarget == null)
        {
            EditorUtility.DisplayDialog("Official prefab mismatch", "The player prefab has no PlayerCameraRoot / CinemachineCameraTarget.\nChoose the Third Person prefab from Starter Assets.", "OK");
            return;
        }

        GameObject vcam = FindSceneObject(scene, OfficialCameraName);
        if (vcam == null)
        {
            vcam = (GameObject)PrefabUtility.InstantiatePrefab(cameraPrefab, scene);
            if (vcam == null) { Debug.LogError("[ENV L02/L03] Failed to instantiate official camera prefab."); return; }
            Undo.RegisterCreatedObjectUndo(vcam, "Add Official Follow Camera");
            vcam.transform.SetParent(gameplay.transform, true);
        }

        Component vcamComponent = vcam.GetComponentsInChildren<Component>(true).FirstOrDefault(c =>
            c != null && (c.GetType().Name == "CinemachineVirtualCamera" || c.GetType().Name == "CinemachineCamera"));
        if (vcamComponent == null || !SetFollow(vcamComponent, followTarget))
            Debug.LogWarning("[ENV L02/L03] Follow could not be assigned automatically. Set PlayerFollowCamera.Follow to " + followTarget.name + ".");

        Camera main = FindMainCamera(scene);
        if (main == null) main = CreateMainCamera(scene);
        main.gameObject.tag = "MainCamera";
        var oldOrbit = main.GetComponent<ENVPrototypeOrbitCamera>();
        if (oldOrbit != null) { Undo.RecordObject(oldOrbit, "Disable prototype camera"); oldOrbit.enabled = false; }
        if (main.GetComponent(brainType) == null) Undo.AddComponent(main.gameObject, brainType);
        if (prototype != null) { Undo.RecordObject(prototype, "Hide temporary player"); prototype.SetActive(false); }

        Selection.activeGameObject = player;
        SaveScene(scene);
        Debug.Log("[ENV L02/L03] Official Starter Assets installed; original MapRoot unchanged. Move player to the dock spawn point.");
    }

    [MenuItem("Tools/ENV Test01/04B - Create Temporary Third Person (no packages)")]
    public static void AddPrototypeThirdPerson()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!CheckScene(scene)) return;
        GameObject gameplay = GetOrCreateRoot(scene);
        GameObject player = FindSceneObject(scene, PrototypePlayerName);
        if (player == null)
        {
            player = new GameObject(PrototypePlayerName);
            SceneManager.MoveGameObjectToScene(player, scene);
            Undo.RegisterCreatedObjectUndo(player, "Create prototype third person player");
            player.transform.SetParent(gameplay.transform, true);
            player.transform.position = SuggestedSpawnPosition();
            var cc = Undo.AddComponent<CharacterController>(player);
            cc.radius = 0.33f; cc.height = 1.85f; cc.center = new Vector3(0f, 0.93f, 0f);
            Undo.AddComponent<ENVPrototypeThirdPersonController>(player);

            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            visual.name = "Visual_ReplaceLater";
            SceneManager.MoveGameObjectToScene(visual, scene);
            Undo.RegisterCreatedObjectUndo(visual, "Add temporary visual");
            visual.transform.SetParent(player.transform, false);
            visual.transform.localPosition = new Vector3(0f, 0.93f, 0f);
            visual.transform.localScale = new Vector3(0.65f, 0.92f, 0.65f);
            Collider visualCollider = visual.GetComponent<Collider>();
            if (visualCollider != null) Undo.DestroyObjectImmediate(visualCollider);
            var renderer = visual.GetComponent<MeshRenderer>();
            if (renderer != null) renderer.sharedMaterial = GetOrCreatePrototypeMaterial();

            var targetObject = new GameObject("PlayerCameraRoot");
            SceneManager.MoveGameObjectToScene(targetObject, scene);
            Undo.RegisterCreatedObjectUndo(targetObject, "Create camera pivot");
            targetObject.transform.SetParent(player.transform, false);
            targetObject.transform.localPosition = new Vector3(0f, 1.5f, 0f);
        }
        else if (!player.activeSelf) { Undo.RecordObject(player, "Enable temporary player"); player.SetActive(true); }

        Camera main = FindMainCamera(scene);
        if (main == null) main = CreateMainCamera(scene);
        main.gameObject.tag = "MainCamera";
        foreach (Component c in main.GetComponents<Component>())
        {
            if (c == null || c.GetType().Name != "CinemachineBrain") continue;
            Behaviour behaviour = c as Behaviour;
            if (behaviour != null) { Undo.RecordObject(behaviour, "Disable Cinemachine for prototype"); behaviour.enabled = false; }
        }
        var orbit = main.GetComponent<ENVPrototypeOrbitCamera>();
        if (orbit == null) orbit = Undo.AddComponent<ENVPrototypeOrbitCamera>(main.gameObject);
        orbit.enabled = true;
        orbit.target = FindChild(player.transform, "PlayerCameraRoot");
        var movement = player.GetComponent<ENVPrototypeThirdPersonController>();
        if (movement == null) movement = Undo.AddComponent<ENVPrototypeThirdPersonController>(player);
        movement.viewCamera = main.transform;
        Selection.activeGameObject = player;
        SaveScene(scene);
        Debug.Log("[ENV L02/L03] Temporary player created. WASD to move, Space to jump, Shift to sprint, mouse to orbit. ESC unlocks cursor. No official Starter Assets or Cinemachine required. Move to dock and add ground colliders.");
    }

    [MenuItem("Tools/ENV Test01/05 - Add Prototype MeshColliders To Map")]
    public static void AddPrototypeMapColliders()
    {
        Scene scene = SceneManager.GetActiveScene();
        GameObject map = FindSceneObject(scene, "MapRoot");
        if (map == null) { EditorUtility.DisplayDialog("MapRoot missing", "Open the map scene before generating temporary collisions.", "OK"); return; }
        var filters = map.GetComponentsInChildren<MeshFilter>(true);
        int candidates = filters.Count(f => f != null && f.sharedMesh != null &&
            f.GetComponent<MeshRenderer>() != null && f.GetComponent<Collider>() == null);
        if (candidates == 0) { Debug.Log("[ENV] No mesh collision candidates."); return; }
        if (!EditorUtility.DisplayDialog("Temporary mesh collisions", "Create up to " + candidates +
            " MeshColliders? This is for testing only and can be expensive on a large FBX. Prefer simplified colliders later.", "Create", "Cancel")) return;
        int added = 0;
        foreach (MeshFilter f in filters)
        {
            if (f == null || f.sharedMesh == null || f.GetComponent<MeshRenderer>() == null || f.GetComponent<Collider>() != null) continue;
            MeshCollider mc = Undo.AddComponent<MeshCollider>(f.gameObject);
            mc.sharedMesh = f.sharedMesh;
            added++;
        }
        SaveScene(scene);
        Debug.Log("[ENV] Created temporary MeshColliders: " + added);
    }

    private static bool CheckScene(Scene scene)
    {
        if (scene.IsValid() && scene.isLoaded) return true;
        EditorUtility.DisplayDialog("No active scene", "Open ENV_test01_Main.unity before running setup.", "OK");
        return false;
    }

    private static GameObject GetOrCreateRoot(Scene scene)
    {
        GameObject root = FindSceneObject(scene, GameplayRoot);
        if (root != null) return root;
        root = new GameObject(GameplayRoot);
        SceneManager.MoveGameObjectToScene(root, scene);
        Undo.RegisterCreatedObjectUndo(root, "Create Gameplay Root");
        return root;
    }

    private static GameObject FindPrefab(params string[] names)
    {
        // AssetDatabase searches both Assets and installed project packages (including package samples).
        foreach (string name in names)
        {
            var paths = AssetDatabase.FindAssets(name + " t:Prefab")
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(p => p.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase))
                .Where(p => string.Equals(Path.GetFileNameWithoutExtension(p), name, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(p => p.IndexOf("ThirdPerson", StringComparison.OrdinalIgnoreCase) >= 0)
                .ThenByDescending(p => p.IndexOf("StarterAssets", StringComparison.OrdinalIgnoreCase) >= 0);
            foreach (string path in paths)
            {
                GameObject obj = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (obj != null) return obj;
            }
        }
        return null;
    }

    private static Type FindType(params string[] names)
    {
        foreach (var a in AppDomain.CurrentDomain.GetAssemblies())
        foreach (string n in names)
        {
            Type t = a.GetType(n, false);
            if (t != null) return t;
        }
        return null;
    }

    private static bool SetFollow(Component cm, Transform follow)
    {
        var so = new SerializedObject(cm);
        var prop = so.FindProperty("m_Follow");
        if (prop != null && prop.propertyType == SerializedPropertyType.ObjectReference)
        {
            prop.objectReferenceValue = follow;
            so.ApplyModifiedProperties();
            return true;
        }
        PropertyInfo p = cm.GetType().GetProperty("Follow", BindingFlags.Instance | BindingFlags.Public);
        if (p == null || !p.CanWrite || p.PropertyType != typeof(Transform)) return false;
        Undo.RecordObject(cm, "Assign follow target");
        p.SetValue(cm, follow, null);
        EditorUtility.SetDirty(cm);
        return true;
    }

    private static GameObject FindSceneObject(Scene scene, string name)
    {
        if (!scene.IsValid() || !scene.isLoaded) return null;
        foreach (GameObject go in scene.GetRootGameObjects())
        {
            Transform t = FindChild(go.transform, name);
            if (t != null) return t.gameObject;
        }
        return null;
    }

    private static Transform FindChild(Transform root, string name)
    {
        if (root.name == name) return root;
        foreach (Transform child in root)
        {
            Transform result = FindChild(child, name);
            if (result != null) return result;
        }
        return null;
    }

    private static Camera FindMainCamera(Scene scene)
    {
        Camera fallback = null;
        foreach (GameObject root in scene.GetRootGameObjects())
        foreach (Camera c in root.GetComponentsInChildren<Camera>(true))
        {
            if (c.CompareTag("MainCamera")) return c;
            if (fallback == null) fallback = c;
        }
        return fallback;
    }

    private static Camera CreateMainCamera(Scene scene)
    {
        GameObject obj = new GameObject("Main Camera");
        SceneManager.MoveGameObjectToScene(obj, scene);
        Undo.RegisterCreatedObjectUndo(obj, "Add main camera");
        Camera c = Undo.AddComponent<Camera>(obj);
        Undo.AddComponent<AudioListener>(obj);
        return c;
    }

    private static Vector3 SuggestedSpawnPosition()
    {
        Vector3 pivot = SceneView.lastActiveSceneView != null ? SceneView.lastActiveSceneView.pivot : Vector3.zero;
        if (Physics.Raycast(pivot + Vector3.up * 300f, Vector3.down, out RaycastHit hit, 600f))
            return hit.point + Vector3.up * 0.12f;
        return pivot + Vector3.up * 2f;
    }

    private static Material GetOrCreatePrototypeMaterial()
    {
        const string folder = "Assets/ENVGameplay";
        const string matFolder = folder + "/Materials";
        const string path = matFolder + "/M_ENV_PrototypePlayer.mat";
        Material existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing != null) return existing;
        if (!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder("Assets", "ENVGameplay");
        if (!AssetDatabase.IsValidFolder(matFolder)) AssetDatabase.CreateFolder(folder, "Materials");
        Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        Material m = new Material(shader);
        m.color = new Color(0.18f, 0.65f, 0.84f, 1f);
        AssetDatabase.CreateAsset(m, path);
        AssetDatabase.SaveAssets();
        return m;
    }

    private static void SaveScene(Scene scene)
    {
        EditorSceneManager.MarkSceneDirty(scene);
        if (!string.IsNullOrEmpty(scene.path)) EditorSceneManager.SaveScene(scene);
    }
}
#endif
