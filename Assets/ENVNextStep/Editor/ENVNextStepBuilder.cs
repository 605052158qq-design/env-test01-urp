#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Video;

/// <summary>
/// Explicit editor actions only: never modifies your existing map on import/recompile.
/// Builds reusable prefabs in Generated/ and offers manual scene wiring commands.
/// </summary>
public static class ENVNextStepBuilder
{
    private const string Root = "Assets/ENVNextStep/Generated";
    private const string ShaderName = "ENV/Water/StylizedWaterHLSL";

    [MenuItem("Tools/ENV Next Step/00 - Generate Reusable Prefabs")]
    public static void GeneratePrefabs()
    {
        EnsureFolder(Root);
        EnsureFolder(Root + "/Prefabs");
        EnsureFolder(Root + "/Profiles");
        MakeAirWallPrefab();
        MakeSnowPrefab();
        MakeEndingPrefab();
        MakePostVolumes();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[ENV Next Step] Generated air wall, snow zone, ending and five local post-FX prefabs under " + Root);
    }

    [MenuItem("Tools/ENV Next Step/01 - Rebuild Selected Air Wall Colliders")]
    public static void RebuildSelectedWalls()
    {
        var path = Selection.activeGameObject != null ? Selection.activeGameObject.GetComponent<ENVAirWallPath>() : null;
        if (path == null) { EditorUtility.DisplayDialog("ENV Air Wall", "Select a scene object with ENVAirWallPath.", "OK"); return; }
        Rebuild(path);
        EditorSceneManager.MarkSceneDirty(path.gameObject.scene);
    }

    [MenuItem("Tools/ENV Next Step/02 - Add Point to Selected Air Wall")]
    public static void AddPoint()
    {
        var path = Selection.activeGameObject != null ? Selection.activeGameObject.GetComponent<ENVAirWallPath>() : null;
        if (path == null) { EditorUtility.DisplayDialog("ENV Air Wall", "Select the air-wall root in the scene.", "OK"); return; }
        var p = new GameObject("WallPoint_" + path.points.Count.ToString("00"));
        Undo.RegisterCreatedObjectUndo(p, "Add air wall control point");
        p.transform.SetParent(path.transform, false);
        p.transform.position = path.points.Count > 0 && path.points[path.points.Count-1] != null
            ? path.points[path.points.Count-1].position + Vector3.right * 3f : path.transform.position;
        Undo.RecordObject(path, "Append air wall point");
        path.points.Add(p.transform);
        EditorUtility.SetDirty(path);
        Selection.activeGameObject = p;
    }

    [MenuItem("Tools/ENV Next Step/03 - Set Selected Fountain Material Polar UV")]
    public static void FountainPolar()
    {
        int count = ConfigureSelectedWater(true);
        Debug.Log("[ENV Water] Set Polar mode on " + count + " selected water materials (other materials untouched).");
    }
    [MenuItem("Tools/ENV Next Step/04 - Set Selected River Material World UV")]
    public static void RiverWorld()
    {
        int count = ConfigureSelectedWater(false);
        Debug.Log("[ENV Water] Set World mode on " + count + " selected water materials (other materials untouched).");
    }
    private static int ConfigureSelectedWater(bool polar)
    {
        var materials = new HashSet<Material>();
        foreach (UnityEngine.Object obj in Selection.objects)
        {
            if (obj is Material material) materials.Add(material);
            if (obj is GameObject go)
                foreach (Renderer r in go.GetComponentsInChildren<Renderer>(true))
                    foreach (Material item in r.sharedMaterials) if (item != null) materials.Add(item);
        }
        int count = 0;
        foreach (Material material in materials)
        {
            if (material == null || material.shader == null || material.shader.name != ShaderName) continue;
            Undo.RecordObject(material, "Configure water polar sampling");
            material.SetFloat("_UsePolarUV", polar ? 1f : 0f);
            EditorUtility.SetDirty(material);
            count++;
        }
        return count;
    }

    [MenuItem("Tools/ENV Next Step/05 - Ensure Existing Ending Event Is Connected")]
    public static void EnsureEnding()
    {
        var all = UnityEngine.Object.FindObjectsOfType<ENVEndSequenceOnCompletion>(true);
        ENVEndSequenceOnCompletion ending = all.Length > 0 ? all[0] : null;
        if (ending == null)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Root + "/Prefabs/PF_ENV_EndSequence.prefab");
            if (prefab == null) { GeneratePrefabs(); prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Root + "/Prefabs/PF_ENV_EndSequence.prefab"); }
            if (prefab != null)
            {
                GameObject go = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
                if (go != null) { Undo.RegisterCreatedObjectUndo(go,"Create ending manager"); ending = go.GetComponent<ENVEndSequenceOnCompletion>(); }
            }
        }
        if (ending == null) return;
        Undo.RecordObject(ending,"Connect ending sequence");
        if (ending.world == null) ending.world = UnityEngine.Object.FindObjectOfType<ENVWorldProgress>(true);
        if (ending.endingVideo == null) ending.endingVideo = ending.GetComponent<ENVVideoSequence>();
        EditorUtility.SetDirty(ending);
        EditorSceneManager.MarkSceneDirty(ending.gameObject.scene);
        Selection.activeGameObject = ending.gameObject;
        Debug.Log("[ENV Ending] Connected existing ending event. Drag customer END VideoClip into ENVVideoSequence.suppliedVideo. The last gem triggers via ENVWorldProgress.onAllGemsActivated.");
    }

    private static void EnsureFolder(string path)
    {
        string[] parts = path.Split('/');
        string parent = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string child = parent + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(child)) AssetDatabase.CreateFolder(parent,parts[i]);
            parent = child;
        }
    }
    private static bool Exists(string assetPath) { return AssetDatabase.LoadMainAssetAtPath(assetPath) != null; }

    private static void SavePrefabOnce(GameObject go, string name)
    {
        string path = Root + "/Prefabs/" + name + ".prefab";
        // Never overwrite a prefab that the user may have adjusted.
        if (!Exists(path)) PrefabUtility.SaveAsPrefabAsset(go,path);
        UnityEngine.Object.DestroyImmediate(go);
    }
    private static void MakeAirWallPrefab()
    {
        GameObject go = new GameObject("PF_ENVAirWallPath");
        var walls = go.AddComponent<ENVAirWallPath>();
        for (int i=0; i<2; i++)
        {
            GameObject point = new GameObject("WallPoint_" + i.ToString("00"));
            point.transform.SetParent(go.transform,false);
            point.transform.localPosition = new Vector3(i * 4f,0,0);
            walls.points.Add(point.transform);
        }
        SavePrefabOnce(go,"PF_ENVAirWallPath");
    }
    private static void MakeSnowPrefab()
    {
        GameObject root = new GameObject("PF_ENVSnowArea");
        var box = root.AddComponent<BoxCollider>();
        box.isTrigger = true;
        box.size = new Vector3(22f,12f,22f);
        var area = root.AddComponent<ENVSnowArea>();
        GameObject fx = new GameObject("SnowParticles_Replaceable");
        fx.transform.SetParent(root.transform,false);
        fx.transform.localPosition = new Vector3(0,6,0);
        var particles = fx.AddComponent<ParticleSystem>();
        var main = particles.main;
        main.playOnAwake = false;
        main.loop = true;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startLifetime = new ParticleSystem.MinMaxCurve(4f,7f);
        main.startSize = new ParticleSystem.MinMaxCurve(.04f,.13f);
        main.startSpeed = 0f;
        main.maxParticles = 500;
        var emission = particles.emission;
        emission.rateOverTime = 60f;
        var shape = particles.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(20f,.5f,20f);
        var velocity = particles.velocityOverLifetime;
        velocity.enabled = true;
        // All X/Y/Z use Constant mode, eliminating the mixed-curve error.
        velocity.x = new ParticleSystem.MinMaxCurve(0.1f);
        velocity.y = new ParticleSystem.MinMaxCurve(-1.4f);
        velocity.z = new ParticleSystem.MinMaxCurve(0.08f);
        var renderer = particles.GetComponent<ParticleSystemRenderer>();
        Shader s = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (s == null) s = Shader.Find("Particles/Standard Unlit");
        var snowMatPath = Root + "/M_ENV_SnowPreview.mat";
        Material m = AssetDatabase.LoadAssetAtPath<Material>(snowMatPath);
        if (m == null && s != null)
        {
            m = new Material(s); m.name="M_ENV_SnowPreview";
            AssetDatabase.CreateAsset(m,snowMatPath);
        }
        if (m != null) renderer.sharedMaterial = m;
        area.snowEffect = particles;
        SavePrefabOnce(root,"PF_ENVSnowArea");
    }
    private static void MakeEndingPrefab()
    {
        GameObject root = new GameObject("PF_ENV_EndSequence");
        root.AddComponent<VideoPlayer>();
        var video = root.AddComponent<ENVVideoSequence>();
        video.playOnStart = false;
        var ending = root.AddComponent<ENVEndSequenceOnCompletion>();
        ending.endingVideo = video;
        SavePrefabOnce(root,"PF_ENV_EndSequence");
    }
    private static VolumeProfile CreateProfile(string id, float exposure,float saturation,float contrast,float bloom,float temperature,float vignette)
    {
        string path = Root+"/Profiles/VP_" + id + ".asset";
        VolumeProfile profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
        if (profile != null) return profile;
        profile = ScriptableObject.CreateInstance<VolumeProfile>();
        profile.name = "VP_" + id;
        AssetDatabase.CreateAsset(profile,path);
        var color = profile.Add<ColorAdjustments>(true);
        color.postExposure.Override(exposure);
        color.saturation.Override(saturation);
        color.contrast.Override(contrast);
        var bl = profile.Add<Bloom>(true);
        bl.intensity.Override(bloom);
        bl.threshold.Override(1.15f);
        var wb = profile.Add<WhiteBalance>(true);
        wb.temperature.Override(temperature);
        var v = profile.Add<Vignette>(true);
        v.intensity.Override(vignette);
        v.smoothness.Override(.35f);
        EditorUtility.SetDirty(profile);
        return profile;
    }
    private static void MakePostVolumes()
    {
        string[] names = { "Dock", "Square", "Garden", "Forest", "Snow" };
        float[] exposure = { 0f,0.12f,.1f,-.08f,.05f };
        float[] sat = { 2,10,12,5,-12 };
        float[] contrast = { 2,5,3,6,8 };
        float[] bloom = { .18f,.25f,.2f,.27f,.15f };
        float[] temp = { 4,5,3,-7,-18 };
        float[] vig = { .08f,.06f,.07f,.12f,.08f };
        for (int i=0;i<names.Length;i++)
        {
            var profile = CreateProfile(names[i],exposure[i],sat[i],contrast[i],bloom[i],temp[i],vig[i]);
            GameObject go = new GameObject("PF_LocalPostFX_" + names[i]);
            BoxCollider box = go.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = new Vector3(25f,14f,25f);
            Volume volume = go.AddComponent<Volume>();
            volume.isGlobal = false;
            volume.priority = 10f;
            volume.blendDistance = 5f;
            volume.weight = 1f;
            volume.sharedProfile = profile;
            SavePrefabOnce(go,"PF_LocalPostFX_" + names[i]);
        }
    }

    public static void Rebuild(ENVAirWallPath path)
    {
        Transform generated = path.transform.Find(ENVAirWallPath.GeneratedRootName);
        if (generated != null) Undo.DestroyObjectImmediate(generated.gameObject);
        GameObject holder = new GameObject(ENVAirWallPath.GeneratedRootName);
        Undo.RegisterCreatedObjectUndo(holder,"Create air wall colliders");
        holder.transform.SetParent(path.transform,false);
        int count=0;
        if (path.points == null) return;
        int edgeCount=path.closedLoop ? path.points.Count : path.points.Count-1;
        for (int i=0; i<edgeCount;i++)
        {
            Transform a=path.points[i];
            Transform b=path.points[(i+1)%path.points.Count];
            if (a == null || b == null) continue;
            Vector3 direction=b.position-a.position;
            direction.y=0f;
            if (direction.sqrMagnitude < .01f) continue;
            GameObject edge=new GameObject("AirWall_" + count.ToString("00"));
            Undo.RegisterCreatedObjectUndo(edge,"Create air wall segment");
            edge.transform.SetParent(holder.transform,true);
            edge.transform.position = (a.position+b.position)*.5f+Vector3.up*(path.bottomOffset+path.height*.5f);
            edge.transform.rotation=Quaternion.LookRotation(direction.normalized,Vector3.up);
            BoxCollider collider=edge.AddComponent<BoxCollider>();
            collider.size=new Vector3(path.thickness,path.height,direction.magnitude + path.thickness);
            collider.isTrigger=false;
            count++;
        }
        Debug.Log("[ENV Air Wall] Generated " + count + " invisible solid BoxCollider segments. No map meshes were modified.",path);
    }
}
#endif
