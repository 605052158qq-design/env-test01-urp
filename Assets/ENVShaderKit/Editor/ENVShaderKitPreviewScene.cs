#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class ENVShaderKitPreviewScene
{
    private const string MaterialsPath = "Assets/ENVShaderKit/Materials/";
    private const string ScenePath = "Assets/ENVShaderKit/ENV_ShaderKit_Showroom.unity";
    private static Material Mat(string name) => AssetDatabase.LoadAssetAtPath<Material>(MaterialsPath + name + ".mat");

    [MenuItem("Tools/ENV Shader Kit/01 - Create Isolated Shader Preview Scene")]
    public static void Create()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        // Work on an additive scene only; original map scene is not modified.
        try
        {
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Preview floor - not game art";
            ground.transform.localScale = new Vector3(3.2f, 1f, 2.1f);
            ground.transform.position = new Vector3(0,-0.65f,0);
            Move(ground, scene);
            var floorMat = Mat("M_Water_Ocean");
            // Ocean material on a distinct flat area; no changes to original map.
            PlacePrimitive(scene, PrimitiveType.Cube, "Preview water - river", new Vector3(-6, -.30f, 0), new Vector3(4.5f, .08f, 4), Mat("M_Water_River"));
            PlacePrimitive(scene, PrimitiveType.Cube, "Preview water - ocean", new Vector3(0, -.30f, 0), new Vector3(4.5f, .08f, 4), floorMat);
            PlacePrimitive(scene, PrimitiveType.Cube, "Preview water - fountain pool", new Vector3(6, -.30f, 0), new Vector3(4.5f, .08f, 4), Mat("M_Water_FountainPool"));

            var gem = PlacePrimitive(scene, PrimitiveType.Sphere, "Gem material (swap this material to real gem mesh)", new Vector3(-6,1,4), new Vector3(1.3f,1.6f,1.3f), Mat("M_Gem_01_Blue"));
            var gem2 = PlacePrimitive(scene, PrimitiveType.Sphere, "Gem dissolve preview - animated", new Vector3(-3,1,4), new Vector3(1.3f,1.6f,1.3f), Mat("M_GemDissolve_Blue"));
            var preview=gem2.AddComponent<ENVShaderKitPreviewOnly>();
            preview.target=gem2.GetComponent<Renderer>();
            preview.mode=ENVShaderKitPreviewOnly.Mode.Dissolve;

            PlacePrimitive(scene, PrimitiveType.Sphere, "Spirit emissive preview", new Vector3(0,1,4), Vector3.one*1.3f, Mat("M_SpiritGlow"));
            PlacePrimitive(scene, PrimitiveType.Cube, "Lamp warm preview", new Vector3(3,1,4), Vector3.one*1.2f, Mat("M_Lamp_Warm"));
            PlacePrimitive(scene, PrimitiveType.Cube, "Window warm preview", new Vector3(6,1,4), Vector3.one*1.2f, Mat("M_Window_Warm"));

            // Unity's default Plane lies in the XZ plane; this material uses its planar UVs.
            var ring=PlacePrimitive(scene,PrimitiveType.Plane,"Magic ring preview - animated",new Vector3(-5,.04f,-4),new Vector3(.65f,1,.65f),Mat("M_MagicRing_White"));
            var ringPreview=ring.AddComponent<ENVShaderKitPreviewOnly>();
            ringPreview.target=ring.GetComponent<Renderer>();
            ringPreview.mode=ENVShaderKitPreviewOnly.Mode.Ring;

            // Vertical mesh must have UV.y pointing from top to bottom for desired movement.
            var waterfall=PlacePrimitive(scene,PrimitiveType.Quad,"Waterfall sheet preview",new Vector3(3,1.5f,-4),new Vector3(3,3,1),Mat("M_Water_WaterfallSheet"));
            waterfall.transform.rotation=Quaternion.Euler(0,180,0);
            var camGo = new GameObject("ENV Shader Kit Preview Camera");
            Move(camGo,scene);
            var cam=camGo.AddComponent<Camera>();
            cam.clearFlags=CameraClearFlags.SolidColor;
            cam.backgroundColor=new Color(.12f,.19f,.27f);
            cam.fieldOfView=58f;
            camGo.transform.position=new Vector3(1,11,-16);
            camGo.transform.LookAt(new Vector3(0,0.7f,0));
            var sunGo=new GameObject("Preview directional light");
            Move(sunGo,scene);
            var sun=sunGo.AddComponent<Light>();
            sun.type=LightType.Directional;
            sun.intensity=1.25f;
            sunGo.transform.rotation=Quaternion.Euler(45,-30,0);
            EditorSceneManager.SaveScene(scene,ScenePath);
            Debug.Log("[ENV Shader Kit] Independent showroom saved: "+ScenePath+". Open this scene separately and press Play to see animated previews. Original map is unchanged.");
        }
        finally
        {
            // Close only the isolated scene we created; never close or save the current map.
            if (scene.IsValid() && scene.isLoaded) EditorSceneManager.CloseScene(scene,true);
        }
    }

    [MenuItem("Tools/ENV Shader Kit/02 - Audit Shader Materials (No Changes)")]
    public static void Audit()
    {
        string[] materials=AssetDatabase.FindAssets("t:Material", new[] {"Assets/ENVShaderKit/Materials"});
        int missing=0,checkedCount=0;
        foreach (string g in materials)
        {
            var path=AssetDatabase.GUIDToAssetPath(g);
            var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null) continue;
            ++checkedCount;
            if (mat.shader == null || !mat.shader.isSupported || mat.shader.name == "Hidden/InternalErrorShader")
            {
                ++missing;
                Debug.LogError("[ENV Shader Kit] Shader missing / unsupported: "+path);
            }
        }
        Debug.Log($"[ENV Shader Kit] Materials checked={checkedCount}, missing/unsupported Shader={missing}. Unity Scene/Game view still needs visual testing.");
    }

    private static void Move(GameObject go,Scene scene) => SceneManager.MoveGameObjectToScene(go,scene);
    private static GameObject PlacePrimitive(Scene scene,PrimitiveType primitive,string name,Vector3 pos,Vector3 scale,Material material)
    {
        var go=GameObject.CreatePrimitive(primitive);
        go.name=name;
        Move(go,scene);
        go.transform.position=pos;
        go.transform.localScale=scale;
        var r=go.GetComponent<Renderer>();
        if(r!=null && material!=null) r.sharedMaterial=material;
        var c=go.GetComponent<Collider>();
        if(c!=null) Object.DestroyImmediate(c);
        return go;
    }
}
#endif
