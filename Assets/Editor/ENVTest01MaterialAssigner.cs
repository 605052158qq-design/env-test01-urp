using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// Forces FBX material slots to reference independent URP .mat assets.
// Names are taken directly from the supplied FBX; no material is shared across an embedded .fbx dependency.
public class ENVTest01MaterialAssigner : AssetPostprocessor
{
    public const string SourcePath = "Assets/Map/Models/ENV_test01_1021.FBX";

    public static readonly Dictionary<string, string> MaterialPaths =
        new Dictionary<string, string>(StringComparer.Ordinal)
    {
        { "songshu-green", "Assets/Map/Materials/MAT_001_songshu-green.mat" },
        { "shumu", "Assets/Map/Materials/MAT_002_shumu.mat" },
        { "green-tree", "Assets/Map/Materials/MAT_003_green-tree.mat" },
        { "09 - Default", "Assets/Map/Materials/MAT_004_09_-_Default.mat" },
        { "white-flower", "Assets/Map/Materials/MAT_005_white-flower.mat" },
        { "pinktree", "Assets/Map/Materials/MAT_006_pinktree.mat" },
        { "stone", "Assets/Map/Materials/MAT_007_stone.mat" },
        { "08 - Default", "Assets/Map/Materials/MAT_008_08_-_Default.mat" },
        { "15 - Default", "Assets/Map/Materials/MAT_009_15_-_Default.mat" },
        { "xueshan", "Assets/Map/Materials/MAT_010_xueshan.mat" },
        { "weilan", "Assets/Map/Materials/MAT_011_weilan.mat" },
        { "water", "Assets/Map/Materials/MAT_012_water.mat" },
        { "yizi", "Assets/Map/Materials/MAT_013_yizi.mat" },
        { "muxiang", "Assets/Map/Materials/MAT_014_muxiang.mat" },
        { "lambert1", "Assets/Map/Materials/MAT_015_lambert1.mat" },
        { "ludeng", "Assets/Map/Materials/MAT_016_ludeng.mat" },
        { "qiang", "Assets/Map/Materials/MAT_017_qiang.mat" },
        { "fangding", "Assets/Map/Materials/MAT_018_fangding.mat" },
        { "penquan2", "Assets/Map/Materials/MAT_019_penquan2.mat" },
        { "baoshi", "Assets/Map/Materials/MAT_020_baoshi.mat" },
        { "pinzi", "Assets/Map/Materials/MAT_021_pinzi.mat" },
        { "24 - Default", "Assets/Map/Materials/MAT_022_24_-_Default.mat" },
        { "fangding2", "Assets/Map/Materials/MAT_023_fangding2.mat" },
        { "None.001", "Assets/Map/Materials/MAT_024_None_001.mat" },
        { "Material #141", "Assets/Map/Materials/MAT_025_Material_141.mat" },
        { "I223-卡通农场资产\\I223-卡通农场资产_OBJ_NewAtlas", "Assets/Map/Materials/MAT_026_I223-_I223-__OBJ_NewAtlas.mat" },
        { "Material #26", "Assets/Map/Materials/MAT_027_Material_26.mat" },
        { "Material #27", "Assets/Map/Materials/MAT_028_Material_27.mat" },
        { "Material #28", "Assets/Map/Materials/MAT_029_Material_28.mat" },
        { "apple", "Assets/Map/Materials/MAT_030_apple.mat" },
        { "Material #25", "Assets/Map/Materials/MAT_031_Material_25.mat" },
        { "Material #143", "Assets/Map/Materials/MAT_032_Material_143.mat" },
        { "Material #142", "Assets/Map/Materials/MAT_033_Material_142.mat" },
        { "map2", "Assets/Map/Materials/MAT_034_map2.mat" },
        { "Material #167", "Assets/Map/Materials/MAT_035_Material_167.mat" },
        { "Material #13700", "Assets/Map/Materials/MAT_036_Material_13700.mat" },
        { "Material #13701", "Assets/Map/Materials/MAT_037_Material_13701.mat" },
        { "Material #13702", "Assets/Map/Materials/MAT_038_Material_13702.mat" },
        { "Material #13703", "Assets/Map/Materials/MAT_039_Material_13703.mat" },
        { "Material #13704", "Assets/Map/Materials/MAT_040_Material_13704.mat" },
        { "Material #13705", "Assets/Map/Materials/MAT_041_Material_13705.mat" },
        { "Material #13711", "Assets/Map/Materials/MAT_042_Material_13711.mat" },
        { "material_0___28648", "Assets/Map/Materials/MAT_043_material_0___28648.mat" },
        { "material_1___28648", "Assets/Map/Materials/MAT_044_material_1___28648.mat" },
        { "material_2___28648", "Assets/Map/Materials/MAT_045_material_2___28648.mat" },
        { "material_3___28648", "Assets/Map/Materials/MAT_046_material_3___28648.mat" },
        { "material_4___28648", "Assets/Map/Materials/MAT_047_material_4___28648.mat" },
        { "material_5___28648", "Assets/Map/Materials/MAT_048_material_5___28648.mat" },
        { "material_6___28648", "Assets/Map/Materials/MAT_049_material_6___28648.mat" },
        { "material_7___28648", "Assets/Map/Materials/MAT_050_material_7___28648.mat" },
        { "material_8___28648", "Assets/Map/Materials/MAT_051_material_8___28648.mat" },
        { "material_9___28648", "Assets/Map/Materials/MAT_052_material_9___28648.mat" },
        { "material_10___28648", "Assets/Map/Materials/MAT_053_material_10___28648.mat" },
        { "material_11___28648", "Assets/Map/Materials/MAT_054_material_11___28648.mat" },
        { "material_12___28648", "Assets/Map/Materials/MAT_055_material_12___28648.mat" },
        { "material_13___28648", "Assets/Map/Materials/MAT_056_material_13___28648.mat" },
        { "material_14___28648", "Assets/Map/Materials/MAT_057_material_14___28648.mat" },
        { "material_15___28648", "Assets/Map/Materials/MAT_058_material_15___28648.mat" },
        { "material_16___28648", "Assets/Map/Materials/MAT_059_material_16___28648.mat" },
        { "material_17___28648", "Assets/Map/Materials/MAT_060_material_17___28648.mat" },
        { "material_18___28648", "Assets/Map/Materials/MAT_061_material_18___28648.mat" },
        { "material_19___28648", "Assets/Map/Materials/MAT_062_material_19___28648.mat" },
        { "material_20___28648", "Assets/Map/Materials/MAT_063_material_20___28648.mat" },
        { "material_21___28648", "Assets/Map/Materials/MAT_064_material_21___28648.mat" },
        { "material_22___28648", "Assets/Map/Materials/MAT_065_material_22___28648.mat" },
        { "material_23___28648", "Assets/Map/Materials/MAT_066_material_23___28648.mat" },
        { "material_24___28648", "Assets/Map/Materials/MAT_067_material_24___28648.mat" },
        { "material_25___28648", "Assets/Map/Materials/MAT_068_material_25___28648.mat" },
        { "material_0___28652", "Assets/Map/Materials/MAT_069_material_0___28652.mat" },
        { "material_1___28652", "Assets/Map/Materials/MAT_070_material_1___28652.mat" },
        { "material_2___28652", "Assets/Map/Materials/MAT_071_material_2___28652.mat" },
        { "material_3___28652", "Assets/Map/Materials/MAT_072_material_3___28652.mat" },
        { "material_4___28652", "Assets/Map/Materials/MAT_073_material_4___28652.mat" },
        { "material_5___28652", "Assets/Map/Materials/MAT_074_material_5___28652.mat" },
        { "material_6___28652", "Assets/Map/Materials/MAT_075_material_6___28652.mat" },
        { "material_7___28652", "Assets/Map/Materials/MAT_076_material_7___28652.mat" },
        { "material_8___28652", "Assets/Map/Materials/MAT_077_material_8___28652.mat" },
        { "material_9___28652", "Assets/Map/Materials/MAT_078_material_9___28652.mat" },
        { "material_10___28652", "Assets/Map/Materials/MAT_079_material_10___28652.mat" },
        { "material_11___28652", "Assets/Map/Materials/MAT_080_material_11___28652.mat" },
        { "material_12___28652", "Assets/Map/Materials/MAT_081_material_12___28652.mat" },
        { "material_13___28652", "Assets/Map/Materials/MAT_082_material_13___28652.mat" },
        { "material_14___28652", "Assets/Map/Materials/MAT_083_material_14___28652.mat" },
        { "material_15___28652", "Assets/Map/Materials/MAT_084_material_15___28652.mat" },
        { "material_16___28652", "Assets/Map/Materials/MAT_085_material_16___28652.mat" },
        { "material_17___28652", "Assets/Map/Materials/MAT_086_material_17___28652.mat" },
        { "material_18___28652", "Assets/Map/Materials/MAT_087_material_18___28652.mat" },
        { "material_19___28652", "Assets/Map/Materials/MAT_088_material_19___28652.mat" },
        { "material_20___28652", "Assets/Map/Materials/MAT_089_material_20___28652.mat" },
        { "material_21___28652", "Assets/Map/Materials/MAT_090_material_21___28652.mat" },
        { "material_22___28652", "Assets/Map/Materials/MAT_091_material_22___28652.mat" },
        { "material_23___28652", "Assets/Map/Materials/MAT_092_material_23___28652.mat" },
        { "material_24___28652", "Assets/Map/Materials/MAT_093_material_24___28652.mat" },
        { "material_25___28652", "Assets/Map/Materials/MAT_094_material_25___28652.mat" },
        { "Material #13271", "Assets/Map/Materials/MAT_095_Material_13271.mat" },
        { "Material #12520", "Assets/Map/Materials/MAT_096_Material_12520.mat" },
        { "Material #12521", "Assets/Map/Materials/MAT_097_Material_12521.mat" },
        { "Material #12553", "Assets/Map/Materials/MAT_098_Material_12553.mat" },
        { "Material #12554", "Assets/Map/Materials/MAT_099_Material_12554.mat" },
        { "Material #12542", "Assets/Map/Materials/MAT_100_Material_12542.mat" },
        { "Material #12543", "Assets/Map/Materials/MAT_101_Material_12543.mat" },
        { "Material #12509", "Assets/Map/Materials/MAT_102_Material_12509.mat" },
        { "Material #12510", "Assets/Map/Materials/MAT_103_Material_12510.mat" },
        { "Material #12531", "Assets/Map/Materials/MAT_104_Material_12531.mat" },
        { "Material #12532", "Assets/Map/Materials/MAT_105_Material_12532.mat" },
        { "Material #13270", "Assets/Map/Materials/MAT_106_Material_13270.mat" },
        { "mogu", "Assets/Map/Materials/MAT_107_mogu.mat" },
        { "Material #13001", "Assets/Map/Materials/MAT_108_Material_13001.mat" },
        { "Material #12218", "Assets/Map/Materials/MAT_109_Material_12218.mat" },
        { "Tile1_1", "Assets/Map/Materials/MAT_110_Tile1_1.mat" },
        { "Material #13025", "Assets/Map/Materials/MAT_111_Material_13025.mat" },
        { "Material #12370", "Assets/Map/Materials/MAT_112_Material_12370.mat" },
        { "Material #12369", "Assets/Map/Materials/MAT_113_Material_12369.mat" },
        { "Material #13116", "Assets/Map/Materials/MAT_114_Material_13116.mat" },
        { "Material #12372", "Assets/Map/Materials/MAT_115_Material_12372.mat" },
        { "Material #12373", "Assets/Map/Materials/MAT_116_Material_12373.mat" },
        { "Tree2_1.001", "Assets/Map/Materials/MAT_117_Tree2_1_001.mat" },
        { "deng", "Assets/Map/Materials/MAT_118_deng.mat" },
        { "Material #13268", "Assets/Map/Materials/MAT_119_Material_13268.mat" },
        { "Material #13267", "Assets/Map/Materials/MAT_120_Material_13267.mat" },
        { "Material #13266", "Assets/Map/Materials/MAT_121_Material_13266.mat" },
        { "Material #13269", "Assets/Map/Materials/MAT_122_Material_13269.mat" },
        { "Material #13265", "Assets/Map/Materials/MAT_123_Material_13265.mat" },
        { "Material #13264", "Assets/Map/Materials/MAT_124_Material_13264.mat" },
        { "Material #13117", "Assets/Map/Materials/MAT_125_Material_13117.mat" },
        { "heliu-shitou", "Assets/Map/Materials/MAT_126_heliu-shitou.mat" },
        { "Material #13792", "Assets/Map/Materials/MAT_127_Material_13792.mat" },
    };

    private void OnPreprocessModel()
    {
        if (!string.Equals(assetPath, SourcePath, StringComparison.OrdinalIgnoreCase)) return;
        var importer = assetImporter as ModelImporter;
        if (importer == null) return;
        importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
        importer.importAnimation = false;
        importer.importCameras = false;
        importer.importLights = false;
    }

    public static Material Resolve(string rawName)
    {
        if (string.IsNullOrEmpty(rawName)) return null;
        string name = rawName.Trim();
        if (name.EndsWith(" (Instance)", StringComparison.Ordinal))
            name = name.Substring(0, name.Length - " (Instance)".Length);
        string assetPath;
        if (!MaterialPaths.TryGetValue(name, out assetPath)) return null;
        return AssetDatabase.LoadAssetAtPath<Material>(assetPath);
    }

    private Material OnAssignMaterialModel(Material material, Renderer renderer)
    {
        if (!string.Equals(assetPath, SourcePath, StringComparison.OrdinalIgnoreCase)) return null;
        return material != null ? Resolve(material.name) : null;
    }
}
