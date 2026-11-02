#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>Only edits explicitly selected water material assets. Does not touch scenes or meta files.</summary>
public static class ENVWaterCausticSetupV5
{
    private const string ShaderName = "ENV/Water/StylizedWaterHLSL";
    private const string TexturePath = "Assets/ENVWaterHLSL/Textures/T_WaterCaustics_Seamless.png";

    private static List<Material> SelectedMaterialAssets()
    {
        var found = new List<Material>();
        foreach (Object obj in Selection.objects)
        {
            var material = obj as Material;
            if (material != null && AssetDatabase.Contains(material) && !found.Contains(material))
                found.Add(material);
        }
        return found;
    }

    [MenuItem("Tools/ENV Water/V5 - Set Up Caustic Texture On Selected HLSL Materials")]
    private static void SetupOnly()
    {
        Apply(convertShader: false);
    }

    [MenuItem("Tools/ENV Water/V5 - Convert Selected Water Materials To HLSL + Caustics")]
    private static void ConvertAndSetup()
    {
        if (!EditorUtility.DisplayDialog("Convert Selected Water Materials?",
            "Only SELECTED material assets will be changed. Original property values will be preserved by name where possible. Existing Shader Graph files and meta files are untouched. Make a project backup first.",
            "Convert", "Cancel")) return;
        Apply(convertShader: true);
    }

    private static void Apply(bool convertShader)
    {
        var target = Shader.Find(ShaderName);
        var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(TexturePath);
        var materials = SelectedMaterialAssets();
        if (target == null || texture == null || materials.Count == 0)
        {
            Debug.LogError("[ENV Water V5] Missing target HLSL shader, caustic texture, or selected material asset. Shader=" + target + " texture=" + texture + " selected=" + materials.Count);
            return;
        }

        var path = AssetDatabase.GetAssetPath(texture);
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null && (importer.wrapMode != TextureWrapMode.Repeat || importer.textureType != TextureImporterType.Default))
        {
            importer.wrapMode = TextureWrapMode.Repeat;
            importer.textureType = TextureImporterType.Default;
            importer.sRGBTexture = true;
            importer.SaveAndReimport();
        }

        int changed = 0;
        foreach (var m in materials)
        {
            if (!convertShader && m.shader != target)
            {
                Debug.LogWarning("[ENV Water V5] Skipped non-HLSL material: " + m.name);
                continue;
            }
            // Keep all existing property values so ShaderGraph -> HLSL keeps artist tweaks.
            var floats = new Dictionary<string,float>();
            var colors = new Dictionary<string,Color>();
            var textures = new Dictionary<string,Texture>();
            string[] floatNames = {
                "_WaterDepth","_WaterSpeed","_NormalStrength","_SurfaceScale","_Shineness",
                "_RefractionStrength","_FoamAmount","_FoamCutoff","_WaveAmplitude",
                "_WaveFrequency","_WaveSpeed","_InteractionRippleCutoff","_RTCamera"
            };
            foreach (string name in floatNames)
                if (m.HasProperty(name)) floats[name] = m.GetFloat(name);
            string[] colorNames = {"_ShallowWater","_DeepWater","_FoamColor"};
            foreach (string name in colorNames)
                if (m.HasProperty(name)) colors[name] = m.GetColor(name);
            if (m.HasProperty("_RenderTexture")) textures["_RenderTexture"] = m.GetTexture("_RenderTexture");
            Vector4 interactor = m.HasProperty("_InteractorPosition") ? m.GetVector("_InteractorPosition") : Vector4.zero;

            Undo.RecordObject(m, "ENV Water V5 Caustics Setup");
            if (convertShader) m.shader = target;
            foreach (var pair in floats) if (m.HasProperty(pair.Key)) m.SetFloat(pair.Key, pair.Value);
            foreach (var pair in colors) if (m.HasProperty(pair.Key)) m.SetColor(pair.Key, pair.Value);
            foreach (var pair in textures) if (m.HasProperty(pair.Key)) m.SetTexture(pair.Key, pair.Value);
            if (m.HasProperty("_InteractorPosition")) m.SetVector("_InteractorPosition", interactor);
            m.SetTexture("_RippleNoiseTex", texture);
            m.SetColor("_RippleColor", new Color(0.4f, 0.9f, 1.4f, 1f));
            m.SetFloat("_RippleIntensity", 1.35f);
            m.SetFloat("_RippleTiling", 0.22f);
            m.SetVector("_RippleSpeed", new Vector4(0.035f, 0.025f, 0, 0));
            m.SetFloat("_RippleThreshold", 0.4f);
            EditorUtility.SetDirty(m);
            changed++;
        }
        AssetDatabase.SaveAssets();
        Debug.Log("[ENV Water V5] Updated " + changed + " selected water material assets. No scene/prefab or gameplay scripts modified.");
    }
}
#endif
