#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// One explicit V1 setup pass for the artist-provided scene hierarchy.
/// It adds colliders to the visible art; it does not create proxy floors or a proxy boat.
/// </summary>
public static class ENVV1WorldSetup
{
    [MenuItem("Tools/ENV Test01/17 - Configure V1 World Colliders Only _F8")]
    public static void ConfigureWorld()
    {
        Transform artRoot = FindSceneTransform("all_mingming");
        if (artRoot == null)
        {
            Debug.LogError("[ENV V1] Could not find the scene instance named all_mingming.");
            return;
        }

        int colliders = AddArtColliders(artRoot);
        Transform collisionRoot = FindSceneTransform("pengzhuang_map1");
        if (collisionRoot != null) colliders += AddArtColliders(collisionRoot);
        Physics.SyncTransforms();

        Save();
        Debug.Log("[ENV V1] Added/updated " + colliders + " MeshColliders on the real art. " +
                  "Boat animation is intentionally not configured here: use the artist-authored Animation clip.", artRoot);
    }

    [MenuItem("Tools/ENV Test01/18 - Validate V1 World Setup")]
    public static void ValidateWorld()
    {
        Transform artRoot = FindSceneTransform("all_mingming");
        ENVGem blueGem = Resources.FindObjectsOfTypeAll<ENVGem>()
            .FirstOrDefault(g => g.gameObject.scene.IsValid() && g.sequenceIndex == 0);
        int artMeshes = artRoot == null ? 0 : artRoot.GetComponentsInChildren<MeshFilter>(true).Count(f => f.sharedMesh != null);
        int artColliders = artRoot == null ? 0 : artRoot.GetComponentsInChildren<MeshCollider>(true).Count(c => c.sharedMesh != null && !c.isTrigger);
        bool blueHasCollider = blueGem != null && blueGem.GetComponentsInChildren<Collider>(true).Any(c => !c.isTrigger);
        Debug.Log("[ENV V1] Validate: art MeshColliders=" + artColliders + "/" + artMeshes +
                  ", blue gem collider=" + blueHasCollider +
                  ". Boat validation is handled by the artist-authored Animation clip.", artRoot);
    }

    [MenuItem("Tools/ENV Scene Content/05 - Add MeshColliders to Current all_mingming")]
    public static void AddCollidersToCurrentActiveMap()
    {
        Transform artRoot = FindSceneTransform("all_mingming");
        if (artRoot == null || !artRoot.gameObject.activeInHierarchy)
        {
            Debug.LogError("[ENV Map Collider] Could not find an active scene object named all_mingming.");
            return;
        }
        int colliders = AddArtColliders(artRoot);
        Physics.SyncTransforms();
        Save();
        Debug.Log("[ENV Map Collider] Added " + colliders +
                  " new MeshColliders below the CURRENT active all_mingming and refreshed all existing ones.", artRoot);
    }

    private static int AddArtColliders(Transform root)
    {
        int count = 0;
        foreach (MeshFilter filter in root.GetComponentsInChildren<MeshFilter>(true))
        {
            if (filter.sharedMesh == null) continue;
            MeshCollider collider = filter.GetComponent<MeshCollider>();
            if (collider == null)
            {
                collider = Undo.AddComponent<MeshCollider>(filter.gameObject);
                count++;
            }
            collider.sharedMesh = filter.sharedMesh;
            collider.convex = false;
            collider.isTrigger = false;
            EditorUtility.SetDirty(collider);
        }
        return count;
    }

    private static Transform FindSceneTransform(string name)
    {
        return Resources.FindObjectsOfTypeAll<Transform>()
            .FirstOrDefault(t => t.name == name && t.gameObject.scene.IsValid());
    }

    private static void Save()
    {
        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
    }
}
#endif
