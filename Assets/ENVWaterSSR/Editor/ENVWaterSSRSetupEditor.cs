#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public static class ENVWaterSSRSetupEditor
{
    [MenuItem("Tools/ENV SSR/01 - Register Selected Real Water Meshes")]
    private static void RegisterSelected()
    {
        int count = 0;
        foreach (GameObject go in Selection.gameObjects)
        {
            if (EditorUtility.IsPersistent(go)) continue;
            foreach (Renderer r in go.GetComponentsInChildren<Renderer>(true))
            {
                bool isWater = false;
                foreach (Material mat in r.sharedMaterials)
                    if (mat != null && mat.shader != null && mat.shader.name == "ENV/Water/StylizedWaterHLSL")
                    { isWater = true; break; }
                if (!isWater) continue;
                var src = r.GetComponent<ENVWaterSSRSource>();
                if (src == null) src = Undo.AddComponent<ENVWaterSSRSource>(r.gameObject);
                src.targetRenderer = r;
                EditorUtility.SetDirty(src);
                var old = r.GetComponent<ENVReflectionRTBinding>();
                if (old != null && old.enabled)
                    Debug.LogWarning($"[ENV SSR] {r.name}: existing ENVReflectionRTBinding is enabled; disable it to avoid conflicting texture binding.");
                count++;
            }
        }
        Debug.Log($"[ENV SSR] Registered {count} existing water Renderer(s), no scene objects duplicated.\n" +
                  "Next: open your active ENV_ForwardRenderer.asset, Add Renderer Feature > ENV Water SSR Feature.");
    }

    [MenuItem("Tools/ENV SSR/02 - Audit Water Sources (No Changes)")]
    private static void Audit()
    {
        var sources = Object.FindObjectsOfType<ENVWaterSSRSource>(true);
        foreach (var s in sources)
        {
            string name = s.targetRenderer != null ? s.targetRenderer.name : "MISSING Renderer";
            Debug.Log($"[ENV SSR] source {s.name} -> {name}", s);
        }
        Debug.Log($"[ENV SSR] Found {sources.Length} sources. Feature needs active camera Opaque/Depth textures. " +
                  "Reflection RT output global name: _ENV_WaterSSRTexture. Existing water shader receives it via MaterialPropertyBlock.");
    }

    [MenuItem("Tools/ENV SSR/03 - Suggest Water Shader Settings (No Changes)")]
    private static void Help()
    {
        Debug.Log("[ENV SSR] Keep ENV/Water/StylizedWaterHLSL. Recommended: Use Reflection RT=On (feature auto-controls it), " +
                  "Reflection Uses Alpha As Hit Mask=On (feature auto-controls it), Reflection Intensity=0.25-0.45, " +
                  "Fresnel Power=2-4. Register each visible river/sea water Renderer. Do not register waterfalls.");
    }
}
#endif
