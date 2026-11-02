#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class ENVStylizedWaterMaterialConverter
{
    private static readonly string[] Floats = {
        "_WaterDepth", "_WaterSpeed", "_NormalStrength", "_SurfaceScale", "_Shineness",
        "_InteractionRippleCutoff", "_RefractionStrength", "_FoamAmount", "_FoamCutoff",
        "_WaveAmplitude", "_WaveFrequency", "_WaveSpeed", "_RTCamera"
    };
    private static readonly string[] Colors = { "_ShallowWater", "_DeepWater", "_FoamColor" };

    [MenuItem("Tools/ENV Water HLSL/Convert Selected Material (Keep Values)")]
    private static void Convert()
    {
        Shader target = Shader.Find("ENV/Water/StylizedWaterHLSL");
        if (target == null) { Debug.LogError("ENV StylizedWater HLSL shader is not imported."); return; }
        var mats = new HashSet<Material>();
        foreach (Object obj in Selection.objects)
        {
            if (obj is Material m) mats.Add(m);
            if (obj is GameObject go)
                foreach (Renderer r in go.GetComponentsInChildren<Renderer>(true))
                    foreach (Material sharedMat in r.sharedMaterials) if (sharedMat != null) mats.Add(sharedMat);
        }
        int count = 0;
        foreach (Material mat in mats)
        {
            if (mat.shader == target) continue;
            var numbers = new Dictionary<string,float>();
            var colors = new Dictionary<string,Color>();
            foreach (string p in Floats) if (mat.HasProperty(p)) numbers[p] = mat.GetFloat(p);
            foreach (string p in Colors) if (mat.HasProperty(p)) colors[p] = mat.GetColor(p);
            Vector4 interactor = mat.HasProperty("_InteractorPosition") ? mat.GetVector("_InteractorPosition") : Vector4.zero;
            Texture rt = mat.HasProperty("_RenderTexture") ? mat.GetTexture("_RenderTexture") : null;
            Vector2 texScale = mat.HasProperty("_RenderTexture") ? mat.GetTextureScale("_RenderTexture") : Vector2.one;
            Vector2 texOffset = mat.HasProperty("_RenderTexture") ? mat.GetTextureOffset("_RenderTexture") : Vector2.zero;
            Undo.RecordObject(mat, "Convert Stylized Water to HLSL");
            mat.shader = target;
            foreach (var pair in numbers) mat.SetFloat(pair.Key, pair.Value);
            foreach (var pair in colors) mat.SetColor(pair.Key, pair.Value);
            mat.SetVector("_InteractorPosition",interactor);
            if (rt != null) mat.SetTexture("_RenderTexture",rt);
            mat.SetTextureScale("_RenderTexture",texScale);
            mat.SetTextureOffset("_RenderTexture",texOffset);
            EditorUtility.SetDirty(mat);
            count++;
            Debug.Log("[ENV Water] Converted in-place: " + AssetDatabase.GetAssetPath(mat),mat);
        }
        if (count > 0) AssetDatabase.SaveAssets();
        Debug.Log("[ENV Water] Converted " + count + " selected materials. No .mat/.meta files were generated.");
    }
}
#endif
