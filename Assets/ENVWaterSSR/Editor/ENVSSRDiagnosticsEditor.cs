#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Diagnostic setup only; does not change water materials, shader settings or scene geometry.</summary>
public static class ENVSSRDiagnosticsEditor
{
    private const string ExpectedShader = "ENV/Water/StylizedWaterHLSL";

    private static bool HasExpectedWaterShader(Renderer r)
    {
        if (r == null) return false;
        foreach (Material m in r.sharedMaterials)
            if (m != null && m.shader != null && m.shader.name == ExpectedShader) return true;
        return false;
    }

    [MenuItem("Tools/ENV SSR/10 - Audit All Scene Water and Sources (No Changes)")]
    public static void Audit()
    {
        int eligible = 0, linked = 0, wrongShaderLikely = 0, invalidSource = 0;
        foreach (Renderer r in Object.FindObjectsOfType<Renderer>(true))
        {
            if (EditorUtility.IsPersistent(r)) continue;
            bool expected = HasExpectedWaterShader(r);
            ENVWaterSSRSource source = r.GetComponent<ENVWaterSSRSource>();
            if (expected)
            {
                eligible++;
                if (source != null && source.targetRenderer == r && source.isActiveAndEnabled && r.enabled && r.gameObject.activeInHierarchy)
                    linked++;
                else
                    Debug.LogWarning($"[ENV SSR] Water renderer not actively registered: {r.name} (path: {GetPath(r.transform)})", r);
            }
            else if (r.name.ToLowerInvariant().Contains("water") || r.name.ToLowerInvariant().Contains("river") || r.name.ToLowerInvariant().Contains("fountain"))
            {
                wrongShaderLikely++;
                string shaderName = r.sharedMaterial != null && r.sharedMaterial.shader != null ? r.sharedMaterial.shader.name : "NO SHADER";
                Debug.Log($"[ENV SSR] Candidate water but different shader: {GetPath(r.transform)} -> {shaderName}", r);
            }
            if (source != null && source.targetRenderer == null) invalidSource++;
        }
        Debug.Log($"[ENV SSR AUDIT] eligibleHLSLWater={eligible}, activeAndRegistered={linked}, " +
                  $"possibleOtherShaderWater={wrongShaderLikely}, brokenSource={invalidSource}. " +
                  "Frame Debugger: debugMode=1 should show MAGENTA where eligible water meshes are visible.");
    }

    [MenuItem("Tools/ENV SSR/11 - Register ALL Scene HLSL Water (Safe, No Materials Changed)")]
    public static void RegisterAll()
    {
        int added = 0, repaired = 0, valid = 0;
        foreach (Renderer r in Object.FindObjectsOfType<Renderer>(true))
        {
            if (EditorUtility.IsPersistent(r) || !HasExpectedWaterShader(r)) continue;
            ENVWaterSSRSource source = r.GetComponent<ENVWaterSSRSource>();
            if (source == null)
            {
                source = Undo.AddComponent<ENVWaterSSRSource>(r.gameObject);
                added++;
            }
            else if (source.targetRenderer != r) repaired++;
            else valid++;
            Undo.RecordObject(source, "Register existing water renderer for SSR");
            source.targetRenderer = r;
            source.enabled = true;
            EditorUtility.SetDirty(source);
            if (source.gameObject.scene.IsValid()) EditorSceneManager.MarkSceneDirty(source.gameObject.scene);
        }
        Debug.Log($"[ENV SSR] Registered water: added={added}, repaired={repaired}, alreadyValid={valid}. " +
                  "No mesh/material/Prefab asset was replaced. Save scene. Enable RendererFeature Debug Mode=1.");
    }

    private static string GetPath(Transform t)
    {
        string result = t.name;
        while (t.parent != null) { t = t.parent; result = t.name + "/" + result; }
        return result;
    }
}
#endif
