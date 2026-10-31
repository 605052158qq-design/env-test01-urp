#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

// Only affects NEW assets inside V3Textures. Never changes any pre-existing textures or metas.
public class ENVVFXV3TextureImporter : AssetPostprocessor
{
    void OnPreprocessTexture()
    {
        const string prefix = "Assets/ENVShaderKit/V3Textures/";
        if (!assetPath.StartsWith(prefix, System.StringComparison.Ordinal)) return;
        TextureImporter importer = (TextureImporter)assetImporter;
        importer.textureType = TextureImporterType.Default;
        importer.sRGBTexture = false; // RG flow, RGB packed normal, R masks are all DATA, not color.
        importer.mipmapEnabled = true;
        importer.alphaSource = TextureImporterAlphaSource.None;
        importer.filterMode = FilterMode.Bilinear;
        importer.wrapMode = assetPath.EndsWith("T_V3_RadialRing.png") || assetPath.EndsWith("T_V3_SnowDot.png")
            ? TextureWrapMode.Clamp : TextureWrapMode.Repeat;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
    }
}
#endif
