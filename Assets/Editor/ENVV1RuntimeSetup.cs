#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

/// <summary>
/// One safe V1 migration pass: remove only Codex-created physical test objects,
/// install the click-driven debug panel, and attach the supplied player art.
/// The editor refresh is intentionally idempotent so an asset refresh can safely
/// re-apply only missing V1 wiring.
/// </summary>
public static class ENVV1RuntimeSetup
{
    private const string SceneName = "ENV_test01_Main.unity";
    private const string PlayerModelPath = "Assets/Player/fbx/juese_skin.fbx";
    private const string IdlePath = "Assets/Player/fbx/ani_idle.fbx";
    private const string WalkPath = "Assets/Player/fbx/ani_walk.fbx";
    private const string TouchPath = "Assets/Player/fbx/ani_touch.fbx";
    private const string ControllerPath = "Assets/Player/ENV_PlayerPrototype.controller";
    private const string ForwardRendererPath = "Assets/Settings/ENV_ForwardRenderer.asset";

    [InitializeOnLoadMethod]
    private static void ScheduleRequestedMigration()
    {
        EditorApplication.delayCall += () =>
        {
            if (EditorApplication.isCompiling || EditorApplication.isPlayingOrWillChangePlaymode) return;
            Scene scene = SceneManager.GetActiveScene();
            if (string.IsNullOrEmpty(scene.path) || !scene.path.EndsWith(SceneName, StringComparison.Ordinal)) return;
            InstallOrUpdate();
        };
    }

    [MenuItem("Tools/ENV Test01/20 - Apply V1 GUI Debug + Player Art")]
    public static void InstallOrUpdate()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || !scene.isLoaded) return;

        bool changed = RemoveOnlyTemporaryTestObjects();
        GameObject systems = GameObject.Find("ENV_GameplaySystems");
        if (systems != null && systems.GetComponent<ENVV1DebugGUI>() == null)
        {
            Undo.AddComponent<ENVV1DebugGUI>(systems);
            changed = true;
        }
        if (systems != null && systems.GetComponent<ENVMaterialColorRecovery>() == null)
        {
            Undo.AddComponent<ENVMaterialColorRecovery>(systems);
            changed = true;
        }

        GameObject player = FindPlayer();
        if (player == null)
        {
            Debug.LogWarning("[ENV V1] Player root not found. GUI debug setup completed; player art will retry after the player exists.");
            SaveIfChanged(scene, changed);
            return;
        }

        if (InstallSceneGameplay(player)) changed = true;
        if (InstallPlayerVisual(player)) changed = true;
        if (DisableColorRecoveryRendererFeature()) changed = true;
        SaveIfChanged(scene, changed);
    }

    private static bool RemoveOnlyTemporaryTestObjects()
    {
        bool changed = false;
        foreach (GameObject target in Resources.FindObjectsOfTypeAll<GameObject>()
                     .Where(x => x != null && x.scene.IsValid() && IsTemporaryTestObject(x.name))
                     .ToArray())
        {
            Undo.DestroyObjectImmediate(target);
            changed = true;
        }

        foreach (ENVTaskDockOrigin origin in Resources.FindObjectsOfTypeAll<ENVTaskDockOrigin>()
                     .Where(x => x != null && x.gameObject.scene.IsValid()).ToArray())
        {
            Undo.DestroyObjectImmediate(origin);
            changed = true;
        }
        foreach (ENVVerticalSliceRunner runner in Resources.FindObjectsOfTypeAll<ENVVerticalSliceRunner>()
                     .Where(x => x != null && x.gameObject.scene.IsValid()).ToArray())
        {
            Undo.DestroyObjectImmediate(runner);
            changed = true;
        }
        return changed;
    }

    private static bool IsTemporaryTestObject(string objectName)
    {
        return objectName == "V1_DockTaskHub" || objectName == "V1_Zone_Wharf" ||
               objectName.StartsWith("V1_TestRegion_", StringComparison.Ordinal);
    }

    private static bool InstallPlayerVisual(GameObject player)
    {
        EnsureModelRig();
        Avatar avatar = AssetDatabase.LoadAllAssetsAtPath(PlayerModelPath).OfType<Avatar>().FirstOrDefault();
        if (avatar == null)
        {
            Debug.LogError("[ENV V1] Player model has no usable Avatar after import: " + PlayerModelPath);
            return false;
        }

        EnsureClipRig(IdlePath, avatar, true);
        EnsureClipRig(WalkPath, avatar, true);
        EnsureClipRig(TouchPath, avatar, false);
        AnimationClip idle = LoadClip(IdlePath);
        AnimationClip walk = LoadClip(WalkPath);
        AnimationClip touch = LoadClip(TouchPath);
        if (idle == null || walk == null || touch == null)
        {
            Debug.LogError("[ENV V1] One or more supplied player animation FBXs have no imported AnimationClip.");
            return false;
        }

        AnimatorController controller = GetOrCreateController(idle, walk, touch);
        Transform visual = player.transform.Find("PlayerVisual");
        bool changed = false;
        if (visual == null)
        {
            GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerModelPath);
            if (model == null) return false;
            GameObject instance = PrefabUtility.InstantiatePrefab(model, player.scene) as GameObject;
            if (instance == null) return false;
            Undo.RegisterCreatedObjectUndo(instance, "Attach supplied player art");
            instance.name = "PlayerVisual";
            instance.transform.SetParent(player.transform, false);
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;
            instance.transform.localScale = Vector3.one;
            visual = instance.transform;
            Transform capsule = player.transform.Find("Visual_ReplaceLater");
            if (capsule != null) capsule.gameObject.SetActive(false);
            changed = true;
        }

        Animator animator = visual.GetComponent<Animator>();
        if (animator == null) animator = Undo.AddComponent<Animator>(visual.gameObject);
        Transform incorrectlyAddedAnimatorRoot = visual.Find("Root");
        Animator incorrectlyAddedAnimator = incorrectlyAddedAnimatorRoot != null
            ? incorrectlyAddedAnimatorRoot.GetComponent<Animator>() : null;
        if (incorrectlyAddedAnimator != null && incorrectlyAddedAnimator != animator)
        {
            Undo.DestroyObjectImmediate(incorrectlyAddedAnimator);
            changed = true;
        }
        if (animator.enabled || animator.runtimeAnimatorController != controller)
        {
            Undo.RecordObject(animator, "Configure direct Generic player clip sampler");
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            animator.enabled = false;
            EditorUtility.SetDirty(animator);
            changed = true;
        }

        ENVPlayerInteraction interaction = player.GetComponent<ENVPlayerInteraction>();
        if (interaction != null && interaction.characterAnimator != animator)
        {
            Undo.RecordObject(interaction, "Bind supplied player Animator");
            interaction.characterAnimator = animator;
            EditorUtility.SetDirty(interaction);
            changed = true;
        }

        ENVGemInteractor gemInteractor = player.GetComponent<ENVGemInteractor>();
        if (gemInteractor != null && gemInteractor.showDebugHint)
        {
            Undo.RecordObject(gemInteractor, "Replace physical gem debug hint with GUI");
            gemInteractor.showDebugHint = false;
            EditorUtility.SetDirty(gemInteractor);
            changed = true;
        }

        ENVPlayerVisualAnimator driver = player.GetComponent<ENVPlayerVisualAnimator>();
        if (driver == null)
        {
            driver = Undo.AddComponent<ENVPlayerVisualAnimator>(player);
            changed = true;
        }
        if (driver.animator != animator || driver.characterController == null ||
            driver.animationRoot != visual || driver.idleClip != idle ||
            driver.walkClip != walk || driver.touchClip != touch)
        {
            Undo.RecordObject(driver, "Bind direct Generic player animation clips");
            driver.animator = animator;
            driver.characterController = player.GetComponent<CharacterController>();
            driver.animationRoot = visual;
            driver.idleClip = idle;
            driver.walkClip = walk;
            driver.touchClip = touch;
            EditorUtility.SetDirty(driver);
            changed = true;
        }
        return changed;
    }

    private static bool InstallSceneGameplay(GameObject player)
    {
        bool changed = false;
        ENVGem blueGem = Resources.FindObjectsOfTypeAll<ENVGem>()
            .FirstOrDefault(gem => gem != null && gem.gameObject.scene.IsValid() && gem.sequenceIndex == 0);
        if (blueGem != null && blueGem.GetComponent<ENVSingleObjectColorRecovery>() == null)
        {
            ENVSingleObjectColorRecovery recovery = Undo.AddComponent<ENVSingleObjectColorRecovery>(blueGem.gameObject);
            recovery.sourceGem = blueGem;
            recovery.targets = blueGem.GetComponentsInChildren<Renderer>(true);
            EditorUtility.SetDirty(recovery);
            changed = true;
        }
        if (blueGem != null && blueGem.revealMaxRadius < 75f)
        {
            Undo.RecordObject(blueGem, "Set blue recovery range");
            blueGem.revealMaxRadius = 75f;
            EditorUtility.SetDirty(blueGem);
            changed = true;
        }

        if (Resources.FindObjectsOfTypeAll<ENVFairyFollower>()
            .All(follower => follower == null || !follower.gameObject.scene.IsValid()))
        {
            GameObject placeholder = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Undo.RegisterCreatedObjectUndo(placeholder, "Create temporary fairy sphere");
            placeholder.name = "ENV_FairyPlaceholder_ReplaceWithArt";
            placeholder.transform.position = player.transform.position + new Vector3(1.1f, 1.5f, -1.2f);
            placeholder.transform.localScale = Vector3.one * 0.28f;
            Collider collider = placeholder.GetComponent<Collider>();
            if (collider != null) Undo.DestroyObjectImmediate(collider);
            ENVFairyFollower fairy = Undo.AddComponent<ENVFairyFollower>(placeholder);
            fairy.player = player.transform;
            fairy.startFollowing = false;
            fairy.alwaysFaceCamera = false;
            EditorUtility.SetDirty(fairy);
            changed = true;
        }

        if (TryInstallWindmillRotation()) changed = true;
        return changed;
    }

    private static bool DisableColorRecoveryRendererFeature()
    {
        UniversalRendererData rendererData = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(ForwardRendererPath);
        if (rendererData == null)
        {
            Debug.LogWarning("[ENV V1] URP renderer data not found: " + ForwardRendererPath);
            return false;
        }
        bool changed = false;
        foreach (ENVColorRecoveryFeature feature in rendererData.rendererFeatures.OfType<ENVColorRecoveryFeature>())
        {
            if (!feature.isActive) continue;
            feature.SetActive(false);
            EditorUtility.SetDirty(feature);
            changed = true;
        }
        if (!changed) return false;
        EditorUtility.SetDirty(rendererData);
        AssetDatabase.SaveAssets();
        Debug.Log("[ENV V1] Disabled the old fullscreen color-recovery feature; embedded URP Lit now owns grey recovery.");
        return true;
    }

    private static bool TryInstallWindmillRotation()
    {
        string[] exactHints = { "windmill", "wind_mill", "windmillblade", "windmill_blade", "fengche", "feng_che", "风车", "风车叶" };
        Transform rotor = Resources.FindObjectsOfTypeAll<Transform>()
            .FirstOrDefault(transform => transform != null && transform.gameObject.scene.IsValid() &&
                exactHints.Any(hint => transform.name.IndexOf(hint, StringComparison.OrdinalIgnoreCase) >= 0));
        if (rotor == null || rotor.GetComponent<ENVSpinOrFloat>() != null) return false;
        ENVSpinOrFloat spin = Undo.AddComponent<ENVSpinOrFloat>(rotor.gameObject);
        spin.motion = ENVSpinOrFloat.Motion.Rotate;
        spin.visualPart = rotor;
        spin.degreesPerSecond = new Vector3(0f, 0f, 70f);
        EditorUtility.SetDirty(spin);
        Debug.Log("[ENV V1] Added windmill rotation to: " + rotor.name, rotor);
        return true;
    }

    private static void EnsureModelRig()
    {
        ModelImporter importer = AssetImporter.GetAtPath(PlayerModelPath) as ModelImporter;
        if (importer == null) return;
        if (importer.animationType == ModelImporterAnimationType.Generic &&
            importer.avatarSetup == ModelImporterAvatarSetup.CreateFromThisModel) return;
        importer.animationType = ModelImporterAnimationType.Generic;
        importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
        importer.SaveAndReimport();
    }

    private static void EnsureClipRig(string path, Avatar avatar, bool loop)
    {
        ModelImporter importer = AssetImporter.GetAtPath(path) as ModelImporter;
        if (importer == null) return;
        bool change = importer.animationType != ModelImporterAnimationType.Generic ||
                      importer.avatarSetup != ModelImporterAvatarSetup.CopyFromOther ||
                      importer.sourceAvatar != avatar;
        ModelImporterClipAnimation[] clips = importer.defaultClipAnimations;
        foreach (ModelImporterClipAnimation clip in clips)
        {
            if (clip.loopTime != loop) { clip.loopTime = loop; change = true; }
        }
        if (!change) return;
        importer.animationType = ModelImporterAnimationType.Generic;
        importer.avatarSetup = ModelImporterAvatarSetup.CopyFromOther;
        importer.sourceAvatar = avatar;
        importer.clipAnimations = clips;
        importer.SaveAndReimport();
    }

    private static AnimationClip LoadClip(string path)
    {
        return AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>()
            .FirstOrDefault(clip => clip != null && !clip.name.StartsWith("__preview__", StringComparison.Ordinal));
    }

    private static AnimatorController GetOrCreateController(AnimationClip idle, AnimationClip walk, AnimationClip touch)
    {
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if (controller != null) return controller;

        controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
        AnimatorStateMachine machine = controller.layers[0].stateMachine;
        controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
        controller.AddParameter("Touch", AnimatorControllerParameterType.Trigger);
        AnimatorState idleState = machine.AddState("Idle");
        AnimatorState walkState = machine.AddState("Walk");
        AnimatorState touchState = machine.AddState("Touch");
        idleState.motion = idle;
        walkState.motion = walk;
        touchState.motion = touch;
        machine.defaultState = idleState;

        AnimatorStateTransition toWalk = idleState.AddTransition(walkState);
        toWalk.hasExitTime = false;
        toWalk.duration = 0.1f;
        toWalk.AddCondition(AnimatorConditionMode.Greater, 0.08f, "Speed");
        AnimatorStateTransition toIdle = walkState.AddTransition(idleState);
        toIdle.hasExitTime = false;
        toIdle.duration = 0.1f;
        toIdle.AddCondition(AnimatorConditionMode.Less, 0.08f, "Speed");
        AnimatorStateTransition toTouch = machine.AddAnyStateTransition(touchState);
        toTouch.hasExitTime = false;
        toTouch.duration = 0.05f;
        toTouch.AddCondition(AnimatorConditionMode.If, 0f, "Touch");
        AnimatorStateTransition leaveTouch = touchState.AddTransition(idleState);
        leaveTouch.hasExitTime = true;
        leaveTouch.exitTime = 0.95f;
        leaveTouch.duration = 0.08f;
        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();
        return controller;
    }

    private static GameObject FindPlayer()
    {
        ENVPrototypeThirdPersonController controller = UnityEngine.Object.FindObjectOfType<ENVPrototypeThirdPersonController>(true);
        if (controller != null) return controller.gameObject;
        CharacterController character = UnityEngine.Object.FindObjectOfType<CharacterController>(true);
        return character != null ? character.gameObject : null;
    }

    private static void SaveIfChanged(Scene scene, bool changed)
    {
        if (!changed) return;
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[ENV V1] Removed temporary V1 trigger objects and installed GUI debug + supplied player art.");
    }
}
#endif
