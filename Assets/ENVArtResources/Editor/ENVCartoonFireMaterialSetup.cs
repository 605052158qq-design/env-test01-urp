#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class ENVCartoonFireMaterialSetup
{
    private const string TexturePath = "Assets/ENVArtResources/Textures/T_FireCartoon_Main.png";
    private const string MaterialPath = "Assets/ENVArtResources/Generated/M_FireCartoon_V1.mat";

    [MenuItem("Tools/ENV Art/Fire > Use Cartoon Texture on Selected Fire Instance")]
    public static void ApplyToSceneFire()
    {
        GameObject root = Selection.activeGameObject;
        if (root == null || EditorUtility.IsPersistent(root))
        {
            Debug.LogWarning("[ENV Fire] Select the pfb_eff_fire INSTANCE in the scene first.");
            return;
        }
        ParticleSystem target = null;
        foreach (ParticleSystem ps in root.GetComponentsInChildren<ParticleSystem>(true))
            if (ps.name == "ps_fire_main") { target = ps; break; }
        if (target == null)
        {
            Debug.LogWarning("[ENV Fire] ps_fire_main not found under the selected object.");
            return;
        }
        Shader shader = Shader.Find("ENV/FX/CartoonFireUnlit");
        Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(TexturePath);
        if (shader == null || texture == null)
        {
            Debug.LogError("[ENV Fire] Missing shader or cartoon flame PNG. Confirm Assets/ENVArtResources was copied.");
            return;
        }
        System.IO.Directory.CreateDirectory("Assets/ENVArtResources/Generated");
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        if (mat == null)
        {
            mat = new Material(shader);
            mat.SetTexture("_BaseMap", texture);
            mat.SetColor("_BaseColor", Color.white);
            mat.SetFloat("_EmissionStrength", 1.0f);
            AssetDatabase.CreateAsset(mat, MaterialPath);
        }
        ParticleSystemRenderer renderer = target.GetComponent<ParticleSystemRenderer>();
        if (renderer == null)
        {
            Debug.LogError("[ENV Fire] Missing ParticleSystemRenderer on ps_fire_main.");
            return;
        }
        Undo.RecordObject(renderer, "Use cartoon particle material on scene instance");
        renderer.sharedMaterial = mat;
        // The PNG is one full sprite, not an atlas.
        Undo.RecordObject(target, "Disable flame texture sheet animation");
        var sheet = target.textureSheetAnimation;
        sheet.enabled = false;
        EditorUtility.SetDirty(renderer);
        EditorUtility.SetDirty(target);
        PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
        PrefabUtility.RecordPrefabInstancePropertyModifications(target);
        if (target.gameObject.scene.IsValid()) EditorSceneManager.MarkSceneDirty(target.gameObject.scene);
        Debug.Log("[ENV Fire] New cartoon material assigned ONLY to scene instance's ps_fire_main. " +
                  "Original prefab asset and other systems not modified. Check Bloom and smoke separately.");
    }
}
#endif
