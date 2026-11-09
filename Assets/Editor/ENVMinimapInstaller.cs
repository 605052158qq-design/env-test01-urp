#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Installs the runtime minimap without hand-editing scene YAML.</summary>
[InitializeOnLoad]
public static class ENVMinimapInstaller
{
    private const string MapTexturePath = "Assets/UI/小地图.png";

    // The project is normally opened directly to ENV_test01_Main.  Queue the
    // first install after scripts reload so dropping these two files into an
    // already-open editor still produces a persisted scene component.
    static ENVMinimapInstaller()
    {
        EditorApplication.delayCall += InstallForOpenPrototypeScene;
    }

    private static void InstallForOpenPrototypeScene()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        var activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (!activeScene.IsValid() || activeScene.name != "ENV_test01_Main") return;
        Install();
    }

    [MenuItem("Tools/ENV Test01/19 - Install Circular Minimap")]
    public static void Install()
    {
        ENVMinimap minimap = Object.FindObjectOfType<ENVMinimap>(true);
        if (minimap == null)
        {
            GameObject root = new GameObject("ENV Minimap");
            Undo.RegisterCreatedObjectUndo(root, "Install circular minimap");
            minimap = Undo.AddComponent<ENVMinimap>(root);
        }

        Undo.RecordObject(minimap, "Configure circular minimap");
        minimap.mapTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(MapTexturePath);
        CharacterController controller = Object.FindObjectOfType<CharacterController>(true);
        if (controller != null) minimap.player = controller.transform;
        GameObject mapRoot = GameObject.Find("MapRoot");
        if (mapRoot != null) minimap.mapRoot = mapRoot.transform;
        GameObject boatDock = GameObject.Find("BoatDockTarget");
        if (boatDock != null) minimap.dockAnchor = boatDock.transform;
        minimap.screenOffset = new Vector2(32f, -30f);
        minimap.dockMapPosition = new Vector2(0.62f, 0.145f);
        minimap.RefreshWorldBounds();
        EditorUtility.SetDirty(minimap);
        EditorSceneManager.MarkSceneDirty(minimap.gameObject.scene);
        EditorSceneManager.SaveScene(minimap.gameObject.scene);
        Debug.Log("[ENV Minimap] Installed circular masked minimap using " + MapTexturePath + ".", minimap);
    }
}
#endif
