#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>One-shot editor installer for the ancient-tree NPC dialogue assets.</summary>
public static class ENVNpcDialogueInstaller
{
    private const string Folder = "Assets/UI/npc瀵硅瘽/";

    [InitializeOnLoadMethod]
    private static void AutoInstallAfterScriptReload()
    {
        // This is intentionally narrow: it only touches a scene that already has
        // the ancient-tree trigger and only fills missing dialogue references.
        EditorApplication.delayCall += AutoInstallIfReady;
    }

    private static void AutoInstallIfReady()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        ENVFairyIntroTrigger trigger = Object.FindObjectOfType<ENVFairyIntroTrigger>(true);
        if (trigger == null) return;

        Sprite background = LoadSprite("鑳屾櫙.png");
        Sprite traveler = LoadSprite("瑙掕壊.png");
        Sprite fairy = LoadSprite("绮剧伒.png");
        Sprite frame = LoadSprite("微信图片_20261109220950_304_1081.png");
        if (background == null || traveler == null || fairy == null || frame == null) return;

        Undo.RecordObject(trigger, "Install ancient tree NPC dialogue");
        trigger.dialogueBackground = background;
        trigger.travelerPortrait = traveler;
        trigger.fairyPortrait = fairy;
        trigger.dialogueFrame = frame;
        EditorUtility.SetDirty(trigger);
        ConfigurePostDialogueFollower(trigger);
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        Debug.Log("[ENV NPC Dialogue] Bound the dialogue UI and configured the fairy to follow after dialogue completion.", trigger);
    }

    [MenuItem("Tools/ENV Scene Content/04 - Install Ancient Tree NPC Dialogue")]
    public static void Install()
    {
        ENVFairyIntroTrigger trigger = Object.FindObjectOfType<ENVFairyIntroTrigger>(true);
        if (trigger == null)
        {
            EditorUtility.DisplayDialog("Missing ancient-tree trigger", "Open the gameplay scene that contains ENVFairyIntroTrigger first.", "OK");
            return;
        }

        Sprite background = LoadSprite("鑳屾櫙.png");
        Sprite traveler = LoadSprite("瑙掕壊.png");
        Sprite fairy = LoadSprite("绮剧伒.png");
        Sprite frame = LoadSprite("微信图片_20261109220950_304_1081.png");
        if (background == null || traveler == null || fairy == null || frame == null)
        {
            EditorUtility.DisplayDialog("Dialogue art not imported", "The four PNG assets under Assets/UI/npc瀵硅瘽 could not be loaded as Sprites. Wait for import to finish and retry.", "OK");
            return;
        }

        Undo.RecordObject(trigger, "Install ancient tree NPC dialogue");
        trigger.dialogueBackground = background;
        trigger.travelerPortrait = traveler;
        trigger.fairyPortrait = fairy;
        trigger.dialogueFrame = frame;
        EditorUtility.SetDirty(trigger);

        ConfigurePostDialogueFollower(trigger);

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        Selection.activeGameObject = trigger.gameObject;
        Debug.Log("[ENV NPC Dialogue] Installed the dialogue UI and configured the fairy to follow after it ends.", trigger);
    }

    private static Sprite LoadSprite(string fileName)
    {
        return AssetDatabase.LoadAssetAtPath<Sprite>(Folder + fileName);
    }

    private static void ConfigurePostDialogueFollower(ENVFairyIntroTrigger trigger)
    {
        ENVFairyFollower follower = Object.FindObjectOfType<ENVFairyFollower>(true);
        if (follower == null)
        {
            foreach (GameObject candidate in Resources.FindObjectsOfTypeAll<GameObject>())
            {
                if (candidate.name != "ENV_FairyPlaceholder_ReplaceWithArt" ||
                    !candidate.scene.IsValid() || !candidate.scene.isLoaded) continue;
                follower = Undo.AddComponent<ENVFairyFollower>(candidate);
                break;
            }
        }
        if (follower == null) return;

        Undo.RecordObject(follower, "Configure fairy follow after dialogue");
        follower.player = trigger.player;
        follower.relativeOffset = trigger.fairyFollowOffset;
        follower.startFollowing = false;
        trigger.fairyFollower = follower;
        EditorUtility.SetDirty(follower);
        EditorUtility.SetDirty(trigger);
    }
}
#endif
