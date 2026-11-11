#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Copies the artist-approved blue gem VFX hierarchy to the other real gems without altering its parameters.</summary>
public static class ENVGemVfxReplicationSetup
{
    private const string VfxName = "VFX_GemActivation";
    private const string ScenePath = "Assets/Scenes/ENV_test01_Main.unity";

    [MenuItem("Tools/ENV VFX/03 - Copy Validated Gem VFX To All Gems")]
    public static void CopyValidatedVfxToAllGems()
    {
        if (SceneManager.GetActiveScene().path != ScenePath)
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        ENVGem[] gems = Resources.FindObjectsOfTypeAll<ENVGem>()
            .Where(g => g != null && g.gameObject.scene.IsValid() && g.gameObject.scene.isLoaded)
            .OrderBy(g => g.sequenceIndex)
            .ToArray();
        if (gems.Length == 0)
        {
            Debug.LogError("[ENV VFX] No scene ENVGem components were found.");
            return;
        }

        Transform template = FindVfxBelow(gems[0].transform);
        if (template == null)
        {
            template = Resources.FindObjectsOfTypeAll<Transform>()
                .FirstOrDefault(t => t != null && t.name == VfxName && t.gameObject.scene.IsValid() && t.gameObject.scene.isLoaded);
        }
        if (template == null)
        {
            Debug.LogError("[ENV VFX] Could not find the validated scene instance named '" + VfxName + "'.", gems[0]);
            return;
        }

        Undo.IncrementCurrentGroup();
        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Copy validated gem VFX to all gems");
        int copied = 0;
        int skipped = 0;
        foreach (ENVGem gem in gems)
        {
            if (gem == null || template.IsChildOf(gem.transform)) { skipped++; continue; }
            Transform existing = FindVfxBelow(gem.transform);
            if (existing != null)
            {
                Undo.DestroyObjectImmediate(existing.gameObject);
            }

            GameObject copy = UnityEngine.Object.Instantiate(template.gameObject, gem.transform);
            copy.name = VfxName;
            copy.transform.localPosition = template.localPosition;
            copy.transform.localRotation = template.localRotation;
            copy.transform.localScale = template.localScale;
            Undo.RegisterCreatedObjectUndo(copy, "Copy validated gem VFX");
            copied++;
        }

        Scene scene = SceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Undo.CollapseUndoOperations(undoGroup);
        Debug.Log("[ENV VFX] Copied the validated VFX hierarchy to " + copied + " gem(s); source skipped=" + skipped + ". " +
                  "All particle and controller values are identical copies. Adjust per-gem color only after visual review.", template);
    }

    private static Transform FindVfxBelow(Transform root)
    {
        if (root == null) return null;
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            if (child != root && child.name == VfxName) return child;
        return null;
    }
}
#endif
