#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

// Non-destructive upgrade: updates only NEW V3 material properties, never model/prefab references.
public static class ENVVFXV3PatchTools
{
    const string MatRoot = "Assets/ENVShaderKit/Materials/";
    const string TexRoot = "Assets/ENVShaderKit/V3Textures/";
    const string SnowPrefab = "Assets/ENVArtResources/Generated/Prefabs/VFX_Snowfall.prefab";

    static Texture2D Texture(string name) => AssetDatabase.LoadAssetAtPath<Texture2D>(TexRoot + name + ".png");
    static Material Mat(string name) => AssetDatabase.LoadAssetAtPath<Material>(MatRoot + name + ".mat");

    static void AssignNewTexture(Material m, string property, string name)
    {
        if (m != null && m.HasProperty(property) && m.GetTexture(property) == null)
        {
            var tex = Texture(name);
            if (tex != null) m.SetTexture(property, tex);
        }
    }
    static void NewFloat(Material m, string property, float value)
    {
        if (m != null && m.HasProperty(property)) m.SetFloat(property, value);
    }

    [MenuItem("Tools/ENV Shader Kit/V3/01 - Apply New V3 Water and Gem Parameters (Keep References)")]
    public static void PatchMaterials()
    {
        string[] target = {
            "M_Water_FountainPool", "M_Water_River", "M_Water_Ocean",
            "M_Water_WaterfallSheet", "M_Water_FountainStream",
            "M_Gem_01_Blue", "M_Gem_02_Red", "M_Gem_03_Yellow",
            "M_Gem_04_Cyan", "M_Gem_05_Orange", "M_Gem_06_Green", "M_Gem_07_Purple"
        };
        int updated = 0;
        foreach (string name in target)
        {
            var mat = Mat(name);
            if (mat == null) continue;
            Undo.RecordObject(mat, "ENV VFX V3 add new properties");
            if (name.Contains("Waterfall") || name.Contains("FountainStream"))
            {
                AssignNewTexture(mat, "_NoiseMap", "T_V3_WarpNoise");
                AssignNewTexture(mat, "_FoamMap", "T_V3_FoamWisps");
                AssignNewTexture(mat, "_MaskMap", "T_V3_SoftMask");
                NewFloat(mat, "_NoiseScale", 3.4f);
                NewFloat(mat, "_NoiseStrength", 0.09f);
                NewFloat(mat, "_NoiseSpeed", 0.8f);
                NewFloat(mat, "_FoamTexStrength", 0.35f);
            }
            else if (name.StartsWith("M_Water_", StringComparison.Ordinal))
            {
                AssignNewTexture(mat, "_FlowMap", "T_V3_RiverFlowRG");
                AssignNewTexture(mat, "_NormalMap", "T_V3_WaveNormalRGB");
                AssignNewTexture(mat, "_FoamMap", "T_V3_FoamWisps");
                AssignNewTexture(mat, "_NoiseMap", "T_V3_WarpNoise");
                AssignNewTexture(mat, "_MaskMap", "T_V3_SoftMask");
                NewFloat(mat, "_FlowSpeedRate", 0.33f);
                NewFloat(mat, "_NoiseDistortion", 0.014f);
                if (name.Contains("FountainPool"))
                {
                    NewFloat(mat, "_PolarBlend", 1f);
                    NewFloat(mat, "_RadialRingStrength", 0.85f);
                    NewFloat(mat, "_RadialRingDensity", 16f);
                    NewFloat(mat, "_RadialRingSpeed", 0.65f);
                    NewFloat(mat, "_FoamTextureStrength", 0.18f);
                    NewFloat(mat, "_FlowStrength", 0f);
                    NewFloat(mat, "_NormalStrength", 0.15f);
                }
                else
                {
                    NewFloat(mat, "_PolarBlend", 0f);
                    NewFloat(mat, "_FlowStrength", 0.11f);
                    NewFloat(mat, "_NormalStrength", 0.32f);
                    NewFloat(mat, "_FoamTextureStrength", 0.24f);
                }
                // Depth foam stays OFF; turn on explicitly after enabling Depth Texture in URP.
                NewFloat(mat, "_DepthFoamEnabled", 0f);
            }
            else if (name.StartsWith("M_Gem_", StringComparison.Ordinal))
            {
                AssignNewTexture(mat, "_DissolveNoiseTex", "T_V3_WarpNoise");
                NewFloat(mat, "_Dissolve", 0f);
                NewFloat(mat, "_DissolveNoiseScale", 7f);
                NewFloat(mat, "_DissolveEdgeWidth", 0.09f);
            }
            EditorUtility.SetDirty(mat);
            updated++;
        }
        AssetDatabase.SaveAssets();
        Debug.Log($"[ENV VFX V3] Patched {updated} EXISTING material assets in-place. No scene, prefab, shader GUID, or meta changed.");
    }

    [MenuItem("Tools/ENV Shader Kit/V3/02 - Create Optional Uber VFX Material")]
    public static void CreateUberVfxMaterial()
    {
        const string path = "Assets/ENVShaderKit/Materials/M_VFX_UberV3.mat";
        if (AssetDatabase.LoadAssetAtPath<Material>(path) != null)
        {
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<Material>(path);
            Debug.Log("[ENV VFX V3] Existing M_VFX_UberV3 kept unchanged.");
            return;
        }
        var shader = Shader.Find("ENVShaderKit/FX/UberParticleV3");
        if (shader == null) { Debug.LogError("[ENV VFX V3] Missing UberParticleV3 shader."); return; }
        var mat = new Material(shader);
        mat.SetTexture("_BaseMap", Texture("T_V3_RadialRing"));
        mat.SetTexture("_MaskMap", Texture("T_V3_RadialRing"));
        mat.SetTexture("_NoiseMap", Texture("T_V3_WarpNoise"));
        mat.SetTexture("_FlowMap", Texture("T_V3_RiverFlowRG"));
        mat.SetColor("_Color", new Color(0.7f, 1.4f, 2.0f, 1f));
        mat.SetFloat("_PolarBlend", 0f); // Ring texture already contains circular UV shape.
        mat.SetFloat("_Distortion", 0.015f);
        mat.SetFloat("_Opacity", 0.75f);
        AssetDatabase.CreateAsset(mat, path);
        AssetDatabase.SaveAssets();
        Selection.activeObject = mat;
    }

    [MenuItem("Tools/ENV Shader Kit/V3/04 - Create Optional Particle Material Presets (Do Not Reassign)")]
    public static void CreateOptionalParticlePresets()
    {
        var shader = Shader.Find("ENVShaderKit/FX/UberParticleV3");
        if (shader == null) { Debug.LogError("[ENV VFX V3] Missing UberParticleV3 shader."); return; }
        // Reuse old V1 art textures; never replace or edit the existing ParticleSystem Prefabs.
        CreateParticleMaterial("M_VFX_GemIdle_V3", shader, "T_FX_Star", new Color(0.27f, 1.0f, 2.5f, 1f), 0.82f);
        CreateParticleMaterial("M_VFX_GemActivation_V3", shader, "T_FX_SoftGlow", new Color(0.9f, 1.6f, 3.0f, 1f), 0.7f);
        CreateParticleMaterial("M_VFX_Campfire_V3", shader, "T_FX_Flame", new Color(2.2f, 0.65f, 0.1f, 1f), 0.90f);
        CreateParticleMaterial("M_VFX_LampSparkles_V3", shader, "T_FX_Star", new Color(2.1f, 1.15f, 0.35f, 1f), 0.65f);
        AssetDatabase.SaveAssets();
        Debug.Log("[ENV VFX V3] Optional material presets created if absent; existing prefabs / positions untouched. Assign manually to each ParticleSystem Renderer.");
    }

    static void CreateParticleMaterial(string name, Shader shader, string v1Texture, Color color, float opacity)
    {
        string path = MatRoot + name + ".mat";
        if (AssetDatabase.LoadAssetAtPath<Material>(path) != null) return;
        var mat = new Material(shader);
        var tex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/ENVArtResources/Textures/" + v1Texture + ".png");
        if (tex == null) tex = Texture("T_V3_SnowDot");
        mat.SetTexture("_BaseMap", tex);
        mat.SetTexture("_MaskMap", tex);
        mat.SetColor("_Color", color);
        mat.SetFloat("_Opacity", opacity);
        mat.SetFloat("_Distortion", 0f);
        AssetDatabase.CreateAsset(mat, path);
    }

    [MenuItem("Tools/ENV Shader Kit/V3/03 - Restore Missing VFX_Snowfall Prefab (If Absent)")]
    public static void MakeSnowIfMissing()
    {
        var existing = AssetDatabase.LoadAssetAtPath<GameObject>(SnowPrefab);
        if (existing != null) { Selection.activeObject = existing; Debug.Log("[ENV VFX V3] Snowfall already exists; did not modify it."); return; }
        string folder = "Assets/ENVArtResources/Generated/Prefabs";
        EnsureFolder(folder);
        var go = new GameObject("VFX_Snowfall");
        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = ps.main;
        main.duration = 5f; main.loop = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(3.5f, 6.5f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.25f, 0.65f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.12f);
        main.startColor = new Color(0.95f, 0.99f, 1f, 0.9f);
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 1000;
        var emission = ps.emission; emission.rateOverTime = 45f;
        var shape = ps.shape; shape.shapeType = ParticleSystemShapeType.Box; shape.scale = new Vector3(20f, 0.5f, 20f);
        var vel = ps.velocityOverLifetime; vel.enabled = true;
        vel.space = ParticleSystemSimulationSpace.World;
        vel.y = new ParticleSystem.MinMaxCurve(-0.9f, -0.45f);
        vel.x = new ParticleSystem.MinMaxCurve(-0.2f, 0.2f);
        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        var shader = Shader.Find("ENVShaderKit/FX/UberParticleV3");
        if (shader != null)
        {
            string matPath = "Assets/ENVShaderKit/Materials/M_Snow_V3.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (mat == null)
            {
                mat = new Material(shader);
                mat.SetTexture("_BaseMap", Texture("T_V3_SnowDot"));
                mat.SetTexture("_MaskMap", Texture("T_V3_SnowDot"));
                mat.SetFloat("_Distortion", 0f);
                mat.SetFloat("_Opacity", 0.75f);
                AssetDatabase.CreateAsset(mat, matPath);
            }
            renderer.sharedMaterial = mat;
        }
        var saved = PrefabUtility.SaveAsPrefabAsset(go, SnowPrefab);
        UnityEngine.Object.DestroyImmediate(go);
        AssetDatabase.SaveAssets();
        Selection.activeObject = saved;
        Debug.Log("[ENV VFX V3] Created missing VFX_Snowfall only. Drag prefab to snow area and place emitter ABOVE ground.");
    }

    static void EnsureFolder(string path)
    {
        var pieces = path.Split('/');
        string cur = pieces[0];
        for (int i = 1; i < pieces.Length; i++)
        {
            string next = cur + "/" + pieces[i];
            if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(cur, pieces[i]);
            cur = next;
        }
    }
}
#endif
