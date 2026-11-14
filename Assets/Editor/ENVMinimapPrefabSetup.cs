#if UNITY_EDITOR
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Builds an artist-editable minimap prefab with the real UI hierarchy
/// exposed in the Prefab editor, rather than only creating it at runtime.</summary>
public static class ENVMinimapPrefabSetup
{
    public const string PrefabPath = "Assets/UI/Prefabs/PF_ENV_Minimap.prefab";
    private const string GeneratedFolder = "Assets/UI/Generated";
    private const string CircleAssetPath = GeneratedFolder + "/ENV_MinimapCircleV2.asset";
    private const string MapPath = "Assets/UI/小地图.png";
    private const string ArrowPath = "Assets/UI/小地图的箭头.png";
    private const string StatusPath = "Assets/UI/小地图下方文字背景.png";

    [MenuItem("Tools/ENV Test01/21 - Build Editable Minimap Prefab")]
    public static void BuildPrefabMenu()
    {
        BuildPrefab();
    }

    [MenuItem("Tools/ENV Test01/22 - Replace Scene Minimap With Editable Prefab")]
    public static void ReplaceSceneMinimapMenu()
    {
        ReplaceSceneMinimap();
    }

    public static GameObject BuildPrefab()
    {
        EnsureFolder("Assets/UI/Prefabs");
        EnsureFolder(GeneratedFolder);
        EnsureSprite(MapPath);
        EnsureSprite(ArrowPath);
        EnsureSprite(StatusPath);

        GameObject root = new GameObject("PF_ENV_Minimap", typeof(RectTransform));
        ENVMinimap minimap = root.AddComponent<ENVMinimap>();
        minimap.mapTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(MapPath);
        minimap.mapArtworkSprite = AssetDatabase.LoadAssetAtPath<Sprite>(MapPath);
        minimap.playerArrowSprite = AssetDatabase.LoadAssetAtPath<Sprite>(ArrowPath);
        minimap.statusBackgroundSprite = AssetDatabase.LoadAssetAtPath<Sprite>(StatusPath);
        minimap.circleMaskSprite = GetOrCreateCircleSprite();
        minimap.mapDiameter = 290f;
        minimap.mapArtworkScale = 3.7688f;
        minimap.screenOffset = new Vector2(32f, -30f);
        minimap.dockMapPosition = new Vector2(0.62f, 0.145f);
        minimap.snowMapPosition = new Vector2(0.52f, 0.83f);
        minimap.BuildUiTemplate();

        // The arrow is deliberately outline-free. Artists can now select this
        // child in the prefab and adjust its source image/size/position directly.
        Transform player = root.transform.Find("ENV Minimap UI/Minimap Root/Circular Mask/Player");
        if (player != null)
        {
            Outline outline = player.GetComponent<Outline>();
            if (outline != null) Object.DestroyImmediate(outline);
        }

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Object.DestroyImmediate(root);
        AssetDatabase.SaveAssets();
        Debug.Log("[ENV Minimap] Built editable prefab: " + PrefabPath, prefab);
        return prefab;
    }

    public static void ReplaceSceneMinimap()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("[ENV Minimap] Stop Play Mode before replacing the scene minimap.");
            return;
        }

        GameObject prefab = BuildPrefab();
        Scene scene = SceneManager.GetActiveScene();
        ENVMinimap old = Object.FindObjectOfType<ENVMinimap>(true);
        ENVMinimap next;
        if (old == null)
        {
            next = ((GameObject)PrefabUtility.InstantiatePrefab(prefab, scene)).GetComponent<ENVMinimap>();
        }
        else
        {
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            next = instance.GetComponent<ENVMinimap>();
            CopyConfiguration(old, next);
            Object.DestroyImmediate(old.gameObject);
        }

        next.gameObject.name = "ENV Minimap";
        EditorUtility.SetDirty(next);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Selection.activeGameObject = next.gameObject;
        Debug.Log("[ENV Minimap] Scene now uses the editable prefab instance. Expand ENV Minimap in the Hierarchy to edit Player, Circular Border, Map Artwork and Status Background.", next);
    }

    private static void CopyConfiguration(ENVMinimap source, ENVMinimap target)
    {
        target.player = source.player;
        target.targetCamera = source.targetCamera;
        target.mapRoot = source.mapRoot;
        target.dockAnchor = source.dockAnchor;
        target.snowAnchor = source.snowAnchor;
        target.mapDiameter = source.mapDiameter;
        target.edgePadding = source.edgePadding;
        target.screenOffset = source.screenOffset;
        target.mapArtworkScale = source.mapArtworkScale;
        target.dockMapPosition = source.dockMapPosition;
        target.snowMapPosition = source.snowMapPosition;
        target.RefreshWorldBounds();
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = Path.GetDirectoryName(path).Replace('\\', '/');
        string name = Path.GetFileName(path);
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, name);
    }

    private static void EnsureSprite(string path)
    {
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null || importer.textureType == TextureImporterType.Sprite) return;
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.alphaIsTransparency = true;
        importer.SaveAndReimport();
    }

    private static Sprite GetOrCreateCircleSprite()
    {
        Sprite existing = AssetDatabase.LoadAllAssetsAtPath(CircleAssetPath)
            .OfType<Sprite>().FirstOrDefault();
        if (existing != null) return existing;

        const int size = 1024;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.name = "ENV Minimap Circle Texture";
        texture.filterMode = FilterMode.Bilinear;
        Color[] pixels = new Color[size * size];
        Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
        float radius = size * 0.5f;
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float alpha = Mathf.Clamp01(radius - Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), center) + 0.5f);
            pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
        }
        texture.SetPixels(pixels);
        texture.Apply();
        AssetDatabase.CreateAsset(texture, CircleAssetPath);
        Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
        sprite.name = "ENV Minimap Circle";
        AssetDatabase.AddObjectToAsset(sprite, texture);
        AssetDatabase.ImportAsset(CircleAssetPath);
        return AssetDatabase.LoadAllAssetsAtPath(CircleAssetPath).OfType<Sprite>().FirstOrDefault();
    }
}
#endif
