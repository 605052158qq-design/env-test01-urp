using UnityEditor;
using UnityEngine;

/// <summary>Creates the drop-in waterfall asset with a fixed vertical mesh and downward UV flow.</summary>
[InitializeOnLoad]
internal static class ENVVerticalWaterfallPrefabBuilder
{
    private const string MeshPath = "Assets/ENVShaderKit/Prefabs/Mesh_Waterfall_Vertical.asset";
    private const string MaterialPath = "Assets/ENVShaderKit/Materials/M_Water_WaterfallVertical.mat";
    private const string PrefabPath = "Assets/ENVShaderKit/Prefabs/PF_Waterfall_Vertical.prefab";
    private const string SourceMaterialPath = "Assets/ENVShaderKit/Materials/M_Water_WaterfallSheet.mat";
    private const string ShaderPath = "Assets/ENVShaderKit/Shaders/ENV_FallingWaterDownward.shader";

    static ENVVerticalWaterfallPrefabBuilder()
    {
        EditorApplication.delayCall += CreateAssets;
    }

    private static void CreateAssets()
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null)
            return;

        var shader = AssetDatabase.LoadAssetAtPath<Shader>(ShaderPath);
        var sourceMaterial = AssetDatabase.LoadAssetAtPath<Material>(SourceMaterialPath);
        if (shader == null || sourceMaterial == null)
        {
            Debug.LogWarning("ENV Shader Kit: vertical waterfall assets will be created after their shader and source material import.");
            return;
        }

        var folder = "Assets/ENVShaderKit/Prefabs";
        if (!AssetDatabase.IsValidFolder(folder))
            AssetDatabase.CreateFolder("Assets/ENVShaderKit", "Prefabs");

        var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(MeshPath);
        if (mesh == null)
        {
            mesh = new Mesh { name = "Mesh_Waterfall_Vertical" };
            // Pivot is at the centre of the top edge: position the prefab at the waterfall lip.
            mesh.vertices = new[]
            {
                new Vector3(-1.5f,  0f, 0f), new Vector3( 1.5f,  0f, 0f),
                new Vector3(-1.5f, -4f, 0f), new Vector3( 1.5f, -4f, 0f)
            };
            // V increases downward, matching the shader's unambiguous fall direction.
            mesh.uv = new[]
            {
                new Vector2(0f, 0f), new Vector2(1f, 0f),
                new Vector2(0f, 1f), new Vector2(1f, 1f)
            };
            mesh.triangles = new[] { 0, 2, 1, 1, 2, 3 };
            mesh.normals = new[] { Vector3.forward, Vector3.forward, Vector3.forward, Vector3.forward };
            mesh.RecalculateBounds();
            AssetDatabase.CreateAsset(mesh, MeshPath);
        }

        var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        if (material == null)
        {
            material = Object.Instantiate(sourceMaterial);
            material.name = "M_Water_WaterfallVertical";
            material.shader = shader;
            material.SetFloat("_StreakDensity", 12f);
            AssetDatabase.CreateAsset(material, MaterialPath);
        }

        var waterfall = new GameObject("PF_Waterfall_Vertical");
        waterfall.AddComponent<MeshFilter>().sharedMesh = mesh;
        waterfall.AddComponent<MeshRenderer>().sharedMaterial = material;
        PrefabUtility.SaveAsPrefabAsset(waterfall, PrefabPath);
        Object.DestroyImmediate(waterfall);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("ENV Shader Kit: created PF_Waterfall_Vertical. Place its pivot at the waterfall lip; only rotate it around Y.");
    }
}
