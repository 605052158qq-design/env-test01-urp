#if UNITY_EDITOR
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Converts the supplied 1920x1080 title-card source images into tightly
/// cropped sprite assets, then assigns them to the live regional-title UI.
/// This keeps the artwork sharp and artist-editable without stretching it.
/// </summary>
public static class ENVRegionTitleArtworkSetup
{
    private const string UiFolder = "Assets/UI/";
    private const string OutputFolder = "Assets/UI/Generated/RegionTitles";

    [MenuItem("Tools/ENV Test01/23 - Crop and Bind Region Title Artwork")]
    public static void CropAndBindForOpenScene()
    {
        EnsureFolder(OutputFolder);
        ENVRegionTitleUI titleUi = Object.FindObjectOfType<ENVRegionTitleUI>(true);
        if (titleUi == null)
        {
            Debug.LogError("[ENV Region Title] ENVRegionTitleUI was not found in the open scene.");
            return;
        }

        titleUi.dockTitle = GetOrCreateCroppedSprite("微光码头.png");
        titleUi.squareTitle = GetOrCreateCroppedSprite("心愿广场.png");
        titleUi.gardenTitle = GetOrCreateCroppedSprite("星星花园.png");
        titleUi.forestTitle = GetOrCreateCroppedSprite("萤火森林.png");
        titleUi.snowTitle = GetOrCreateCroppedSprite("浮光雪岭.png");
        ResizeTitleContainer(titleUi);

        EditorUtility.SetDirty(titleUi);
        EditorSceneManager.MarkSceneDirty(titleUi.gameObject.scene);
        EditorSceneManager.SaveScene(titleUi.gameObject.scene);
        AssetDatabase.SaveAssets();
        Debug.Log("[ENV Region Title] Cropped title cards were bound to the open scene.", titleUi);
    }

    private static void ResizeTitleContainer(ENVRegionTitleUI titleUi)
    {
        if (titleUi.titleImage == null || titleUi.titleImage.transform.parent == null) return;
        RectTransform container = titleUi.titleImage.transform.parent as RectTransform;
        if (container == null) return;

        container.anchorMin = new Vector2(0.16f, 0.61f);
        container.anchorMax = new Vector2(0.84f, 0.91f);
        container.offsetMin = Vector2.zero;
        container.offsetMax = Vector2.zero;
        EditorUtility.SetDirty(container);
    }

    private static Sprite GetOrCreateCroppedSprite(string sourceFileName)
    {
        string outputPath = OutputFolder + "/" + Path.GetFileNameWithoutExtension(sourceFileName) + ".asset";
        Sprite existing = AssetDatabase.LoadAllAssetsAtPath(outputPath).OfType<Sprite>().FirstOrDefault();
        if (existing != null) return existing;

        Texture2D source = AssetDatabase.LoadAssetAtPath<Texture2D>(UiFolder + sourceFileName);
        if (source == null)
        {
            Debug.LogError("[ENV Region Title] Missing title artwork: " + UiFolder + sourceFileName);
            return null;
        }

        Texture2D readable = ReadTexture(source);
        Rect crop = FindOpaqueBounds(readable, 0.03f);
        if (crop.width < 1f || crop.height < 1f)
        {
            Object.DestroyImmediate(readable);
            Debug.LogError("[ENV Region Title] No visible pixels found in: " + sourceFileName);
            return null;
        }

        // Preserve a small transparent breathing room around the hand-drawn title.
        const int padding = 18;
        int x = Mathf.Max(0, Mathf.FloorToInt(crop.xMin) - padding);
        int y = Mathf.Max(0, Mathf.FloorToInt(crop.yMin) - padding);
        int xMax = Mathf.Min(readable.width, Mathf.CeilToInt(crop.xMax) + padding);
        int yMax = Mathf.Min(readable.height, Mathf.CeilToInt(crop.yMax) + padding);
        int width = xMax - x;
        int height = yMax - y;

        Texture2D cropped = new Texture2D(width, height, TextureFormat.RGBA32, false)
        {
            name = Path.GetFileNameWithoutExtension(sourceFileName) + " Cropped",
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };
        cropped.SetPixels(readable.GetPixels(x, y, width, height));
        cropped.Apply(false, false);
        Object.DestroyImmediate(readable);

        AssetDatabase.CreateAsset(cropped, outputPath);
        Sprite sprite = Sprite.Create(cropped, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f), 100f);
        sprite.name = Path.GetFileNameWithoutExtension(sourceFileName);
        AssetDatabase.AddObjectToAsset(sprite, cropped);
        AssetDatabase.ImportAsset(outputPath);
        return AssetDatabase.LoadAllAssetsAtPath(outputPath).OfType<Sprite>().FirstOrDefault();
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
        int minX = texture.width;
        int minY = texture.height;
        int maxX = -1;
        int maxY = -1;
        for (int y = 0; y < texture.height; y++)
        for (int x = 0; x < texture.width; x++)
        {
            if (pixels[y * texture.width + x].a < alphaThreshold) continue;
            minX = Mathf.Min(minX, x);
            minY = Mathf.Min(minY, y);
            maxX = Mathf.Max(maxX, x);
            maxY = Mathf.Max(maxY, y);
        }
        return maxX < minX || maxY < minY
            ? Rect.zero
            : Rect.MinMaxRect(minX, minY, maxX + 1, maxY + 1);
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
