#if UNITY_EDITOR
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Applies high-quality minimap import and mask settings without
/// rebuilding the artist-authored border sprite.</summary>
public static class ENVMinimapQualitySetup
{
    private const string PrefabPath = "Assets/UI/Prefabs/PF_ENV_Minimap.prefab";
    private const string MapPath = "Assets/UI/小地图.png";
    private const string BorderSourcePath = "Assets/UI/ea754cb95f752f95cc67947bf4527ef6.png";
    private const string GeneratedFolder = "Assets/UI/Generated";
    private const string MaskPath = GeneratedFolder + "/ENV_MinimapCircleV2.asset";
    private const string BorderPath = GeneratedFolder + "/ENV_MinimapBorderCropped.asset";
    private const int MapDiameter = 290;
    private const int FrameDiameter = 300;

    [MenuItem("Tools/ENV Test01/24 - Apply High-Quality Minimap Mask")]
    public static void ApplyForOpenScene()
    {
        ConfigureMapImporter();
        Sprite maskSprite = GetOrCreateMaskSprite();
        Sprite borderSprite = GetOrCreateBorderSprite();
        UpdatePrefab(maskSprite, borderSprite);
        UpdateOpenScene(maskSprite, borderSprite);
        AssetDatabase.SaveAssets();
        Debug.Log("[ENV Minimap] Applied 1024px antialiased mask and 300/290 frame layout.");
    }

    private static void ConfigureMapImporter()
    {
        TextureImporter importer = AssetImporter.GetAtPath(MapPath) as TextureImporter;
        if (importer == null)
        {
            Debug.LogError("[ENV Minimap] Map texture not found: " + MapPath);
            return;
        }
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.alphaIsTransparency = true;
        importer.filterMode = FilterMode.Bilinear;
        importer.mipmapEnabled = false;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.maxTextureSize = 4096;
        importer.SaveAndReimport();
    }

    private static Sprite GetOrCreateMaskSprite()
    {
        Sprite existing = AssetDatabase.LoadAllAssetsAtPath(MaskPath).OfType<Sprite>().FirstOrDefault();
        if (existing != null) return existing;

        EnsureFolder(GeneratedFolder);
        const int size = 1024;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            name = "ENV Minimap Antialiased Circle Texture",
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };
        Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
        float radius = size * 0.5f;
        Color[] pixels = new Color[size * size];
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float distance = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), center);
            float alpha = Mathf.Clamp01(radius - distance + 0.5f);
            pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
        }
        texture.SetPixels(pixels);
        texture.Apply(false, false);
        AssetDatabase.CreateAsset(texture, MaskPath);
        Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
        sprite.name = "ENV Minimap Antialiased Circle";
        AssetDatabase.AddObjectToAsset(sprite, texture);
        AssetDatabase.ImportAsset(MaskPath);
        return AssetDatabase.LoadAllAssetsAtPath(MaskPath).OfType<Sprite>().FirstOrDefault();
    }

    private static Sprite GetOrCreateBorderSprite()
    {
        Sprite existing = AssetDatabase.LoadAllAssetsAtPath(BorderPath).OfType<Sprite>().FirstOrDefault();
        if (existing != null) return existing;

        Texture2D source = AssetDatabase.LoadAssetAtPath<Texture2D>(BorderSourcePath);
        if (source == null)
        {
            Debug.LogError("[ENV Minimap] Border source not found: " + BorderSourcePath);
            return null;
        }

        Texture2D readable = ReadTexture(source);
        Rect opaque = FindOpaqueBounds(readable, 0.02f);
        if (opaque.width < 1f || opaque.height < 1f)
        {
            Object.DestroyImmediate(readable);
            Debug.LogError("[ENV Minimap] Border source has no visible pixels: " + BorderSourcePath);
            return null;
        }

        const int padding = 16;
        int side = Mathf.CeilToInt(Mathf.Max(opaque.width, opaque.height)) + padding * 2;
        int x = Mathf.Clamp(Mathf.RoundToInt(opaque.center.x - side * 0.5f), 0, Mathf.Max(0, readable.width - side));
        int y = Mathf.Clamp(Mathf.RoundToInt(opaque.center.y - side * 0.5f), 0, Mathf.Max(0, readable.height - side));
        side = Mathf.Min(side, Mathf.Min(readable.width - x, readable.height - y));

        Texture2D cropped = new Texture2D(side, side, TextureFormat.RGBA32, false)
        {
            name = "ENV Minimap Border Cropped",
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };
        cropped.SetPixels(readable.GetPixels(x, y, side, side));
        cropped.Apply(false, false);
        Object.DestroyImmediate(readable);

        AssetDatabase.CreateAsset(cropped, BorderPath);
        Sprite sprite = Sprite.Create(cropped, new Rect(0f, 0f, side, side), new Vector2(0.5f, 0.5f), 100f);
        sprite.name = "ENV Minimap Border Cropped";
        AssetDatabase.AddObjectToAsset(sprite, cropped);
        AssetDatabase.ImportAsset(BorderPath);
        return AssetDatabase.LoadAllAssetsAtPath(BorderPath).OfType<Sprite>().FirstOrDefault();
    }

    private static void UpdatePrefab(Sprite maskSprite, Sprite borderSprite)
    {
        if (!File.Exists(PrefabPath))
        {
            Debug.LogError("[ENV Minimap] Prefab not found: " + PrefabPath);
            return;
        }
        GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            ENVMinimap minimap = root.GetComponent<ENVMinimap>();
            Transform mask = root.transform.Find("ENV Minimap UI/Minimap Root/Circular Mask");
            Transform border = root.transform.Find("ENV Minimap UI/Minimap Root/Circular Border");
            if (minimap == null || mask == null || border == null)
            {
                Debug.LogError("[ENV Minimap] Required prefab hierarchy is missing.");
                return;
            }
            minimap.circleMaskSprite = maskSprite;
            minimap.mapDiameter = MapDiameter;
            Image maskImage = mask.GetComponent<Image>();
            Image borderImage = border.GetComponent<Image>();
            maskImage.sprite = maskSprite;
            if (borderSprite != null) borderImage.sprite = borderSprite;
            ConfigureLayout(mask.GetComponent<RectTransform>(), new Vector2(5f, -5f), MapDiameter);
            ConfigureLayout(border.GetComponent<RectTransform>(), Vector2.zero, FrameDiameter);
            EditorUtility.SetDirty(minimap);
            EditorUtility.SetDirty(maskImage);
            EditorUtility.SetDirty(borderImage);
            EditorUtility.SetDirty(mask);
            EditorUtility.SetDirty(border);
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static void UpdateOpenScene(Sprite maskSprite, Sprite borderSprite)
    {
        ENVMinimap minimap = Object.FindObjectOfType<ENVMinimap>(true);
        if (minimap == null) return;
        Transform mask = minimap.transform.Find("ENV Minimap UI/Minimap Root/Circular Mask");
        Transform border = minimap.transform.Find("ENV Minimap UI/Minimap Root/Circular Border");
        if (mask == null || border == null) return;
        minimap.circleMaskSprite = maskSprite;
        minimap.mapDiameter = MapDiameter;
        Image maskImage = mask.GetComponent<Image>();
        Image borderImage = border.GetComponent<Image>();
        maskImage.sprite = maskSprite;
        if (borderSprite != null) borderImage.sprite = borderSprite;
        ConfigureLayout(mask.GetComponent<RectTransform>(), new Vector2(5f, -5f), MapDiameter);
        ConfigureLayout(border.GetComponent<RectTransform>(), Vector2.zero, FrameDiameter);
        EditorUtility.SetDirty(minimap);
        EditorUtility.SetDirty(maskImage);
        EditorUtility.SetDirty(borderImage);
        EditorSceneManager.MarkSceneDirty(minimap.gameObject.scene);
        EditorSceneManager.SaveScene(minimap.gameObject.scene);
    }

    private static void ConfigureLayout(RectTransform rect, Vector2 position, float size)
    {
        rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(size, size);
        rect.localScale = Vector3.one;
    }

    private static Texture2D ReadTexture(Texture source)
    {
        RenderTexture temporary = RenderTexture.GetTemporary(source.width, source.height, 0, RenderTextureFormat.ARGB32);
        RenderTexture previous = RenderTexture.active;
        Graphics.Blit(source, temporary);
        RenderTexture.active = temporary;
        Texture2D readable = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false);
        readable.ReadPixels(new Rect(0f, 0f, source.width, source.height), 0, 0);
        readable.Apply(false, false);
        RenderTexture.active = previous;
        RenderTexture.ReleaseTemporary(temporary);
        return readable;
    }

    private static Rect FindOpaqueBounds(Texture2D texture, float alphaThreshold)
    {
        Color[] pixels = texture.GetPixels();
        int minX = texture.width, minY = texture.height, maxX = -1, maxY = -1;
        for (int y = 0; y < texture.height; y++)
        for (int x = 0; x < texture.width; x++)
        {
            if (pixels[y * texture.width + x].a < alphaThreshold) continue;
            minX = Mathf.Min(minX, x);
            minY = Mathf.Min(minY, y);
            maxX = Mathf.Max(maxX, x);
            maxY = Mathf.Max(maxY, y);
        }
        return maxX < minX || maxY < minY ? Rect.zero : Rect.MinMaxRect(minX, minY, maxX + 1, maxY + 1);
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = Path.GetDirectoryName(path).Replace('\\', '/');
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
    }
}
#endif
