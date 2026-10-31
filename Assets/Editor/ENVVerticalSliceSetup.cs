#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Installs one representative L07/L08 chain on current, real scene content.</summary>
public static class ENVVerticalSliceSetup
{
    private const string Menu = "Tools/ENV Test01/16 - Install V1 Region + Gem Vertical Slice";

    [MenuItem(Menu)]
    public static void Install()
    {
        CharacterController playerController = FindPlayerController();
        if (playerController == null)
        {
            Debug.LogError("[ENV Slice] Existing Player with CharacterController is required.");
            return;
        }
        ENVGem gem = FindFirstGem();
        if (gem == null)
        {
            Debug.LogError("[ENV Slice] INT_baoshi_01 was not found under the named art map.");
            return;
        }

        GameObject systems = GameObject.Find("ENV_GameplaySystems");
        if (systems == null) systems = new GameObject("ENV_GameplaySystems");
        ENVWorldProgress progress = GetOrAdd<ENVWorldProgress>(systems);
        ENVRegionTitleUI regionUi = GetOrAdd<ENVRegionTitleUI>(systems);
        GetOrAdd<ENVObjectivePointer>(systems);
        GetOrAdd<ENVGemInteractor>(playerController.gameObject).showDebugHint = true;

        gem.gemstoneName = "蓝宝石";
        gem.sequenceIndex = 0;
        gem.interactionRadius = 2.5f;
        gem.revealSeconds = 1.2f;

        GameObject zone = GameObject.Find("V1_Zone_Wharf");
        if (zone == null) zone = new GameObject("V1_Zone_Wharf");
        zone.transform.position = playerController.transform.position;
        BoxCollider box = GetOrAdd<BoxCollider>(zone);
        box.isTrigger = true;
        box.size = new Vector3(12f, 6f, 12f);
        ENVRegionTrigger trigger = GetOrAdd<ENVRegionTrigger>(zone);
        trigger.regionName = "微光码头";
        trigger.restorationGem = gem;
        trigger.restoredRegionName = "微光码头 · 水路已恢复";
        trigger.player = playerController.transform;
        trigger.once = true;
        trigger.showSeconds = 3.5f;

        ENVVerticalSliceRunner runner = GetOrAdd<ENVVerticalSliceRunner>(systems);
        runner.player = playerController.transform;
        runner.sampleGem = gem;
        runner.titleUi = regionUi;
        runner.regionTitle = trigger.regionName;

        EditorUtility.SetDirty(progress);
        EditorUtility.SetDirty(regionUi);
        EditorUtility.SetDirty(gem);
        EditorUtility.SetDirty(trigger);
        EditorUtility.SetDirty(runner);
        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        Debug.Log("[ENV Slice] Installed. Play: finish docking, then G moves to the real blue gem and E triggers the normal gem interaction.", systems);
    }

    private static T GetOrAdd<T>(GameObject target) where T : Component
    {
        T component = target.GetComponent<T>();
        return component != null ? component : Undo.AddComponent<T>(target);
    }

    private static CharacterController FindPlayerController()
    {
        ENVPrototypeThirdPersonController prototype = Object.FindObjectOfType<ENVPrototypeThirdPersonController>(true);
        if (prototype != null) return prototype.GetComponent<CharacterController>();
        return Object.FindObjectOfType<CharacterController>(true);
    }

    private static ENVGem FindFirstGem()
    {
        foreach (ENVGem gem in Resources.FindObjectsOfTypeAll<ENVGem>())
            if (gem.gameObject.scene.IsValid() && gem.sequenceIndex == 0) return gem;
        return null;
    }
}
#endif
