#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Editor-only asset preparation. No gameplay logic, no changes to ENV_test01_Main.
public static class ENVArtResourcesGenerator
{
    private const string Root = "Assets/ENVArtResources";
    private const string Out = Root + "/Generated";
    private const string Mats = Out + "/Materials";
    private const string FX = Out + "/Prefabs";
    private const string Scenes = Out + "/Scenes";
    private const string WaterfallVfxVersionLabel = "ENV_StylizedWaterfall_V2";
    private const string WaterfallVfxSessionKey = "ENVArtResources.StylizedWaterfall.V2.Upgraded";

    // Apply the authored V2 breakup once to the generated prefab after this script
    // recompiles. The persistent asset label prevents it from touching later manual
    // edits or repeatedly rewriting the prefab on every domain reload.
    [InitializeOnLoadMethod]
    private static void UpgradeWaterfallAfterReload()
    {
        EditorApplication.delayCall += () =>
        {
            if (SessionState.GetBool(WaterfallVfxSessionKey, false)) return;
            SessionState.SetBool(WaterfallVfxSessionKey, true);
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(FX + "/VFX_WaterfallBase.prefab");
            if (existing == null || Array.IndexOf(AssetDatabase.GetLabels(existing), WaterfallVfxVersionLabel) >= 0) return;
            UpgradeStylizedWaterfallInternal();
        };
    }

    [MenuItem("Tools/ENV Art/01 - Generate VFX Prefabs and Materials")]
    public static void GenerateAll()
    {
        EnsureFolder("Assets", "ENVArtResources");
        EnsureFolder(Root, "Generated");
        EnsureFolder(Out, "Materials");
        EnsureFolder(Out, "Prefabs");
        EnsureFolder(Out, "Scenes");
        var alpha = Shader.Find("ENVArt/FX/ParticleAlpha");
        var add = Shader.Find("ENVArt/FX/ParticleAdditive");
        var water = Shader.Find("ENVArt/Water/FlowStylized");
        if (alpha == null || add == null || water == null)
        {
            Debug.LogError("[ENV Art] Custom shaders unavailable. Check Console for shader import errors.");
            return;
        }
        Material soft = ParticleMat("M_FX_SoftGlow", add, "T_FX_SoftGlow");
        Material star = ParticleMat("M_FX_Star", add, "T_FX_Star");
        Material ring = ParticleMat("M_FX_Ring", add, "T_FX_Ring");
        Material flame = ParticleMat("M_FX_Flame", alpha, "T_FX_Flame");
        Material smoke = ParticleMat("M_FX_Smoke", alpha, "T_FX_Smoke");
        Material drop = ParticleMat("M_FX_Droplet", alpha, "T_FX_Droplet");
        Material snow = ParticleMat("M_FX_Snowflake", alpha, "T_FX_Snowflake");
        Material streak = ParticleMat("M_FX_Streak", alpha, "T_FX_Streak");
        Material waterRing = ParticleMat("M_FX_WaterRing", alpha, "T_FX_Ring");
        MakeWaters(water);
        MakeGemMaterials();
        MakeLamp();
        GemIdle(soft, star);
        GemActivation(ring, star, soft);
        Spirit(soft, star);
        Campfire(flame, star, smoke);
        Snow(snow, soft);
        Fountain(drop, waterRing, soft);
        Waterfall(streak, soft, waterRing);
        RiverAmbient(soft);
        LampSparkles(soft, star);
        ColorRestoreRing(ring, star);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[ENV Art] Generated 10 reusable particle prefabs, 3 water materials, 7 gem materials, 1 warm lamp material. " +
                  "Open 'Assets/ENVArtResources/Generated/Prefabs'. No map/gameplay files modified.");
        EditorUtility.DisplayDialog("ENV Art Resources", "Visual resources generated in:\n" + Out + "\n\nThe map and gameplay scripts were not modified.", "OK");
    }

    // Kept separate so the existing map's waterfall prefab can be upgraded without
    // regenerating every unrelated VFX resource.
    [MenuItem("Tools/ENV Art/01b - Upgrade Stylized Waterfall VFX")]
    public static void UpgradeStylizedWaterfall()
    {
        UpgradeStylizedWaterfallInternal();
    }

    private static void UpgradeStylizedWaterfallInternal()
    {
        var alpha = Shader.Find("ENVArt/FX/ParticleAlpha");
        var add = Shader.Find("ENVArt/FX/ParticleAdditive");
        if (alpha == null || add == null)
        {
            Debug.LogError("[ENV Art] Particle shaders unavailable. Check Console for shader import errors.");
            return;
        }
        Material streak = ParticleMat("M_FX_Streak", alpha, "T_FX_Streak");
        Material soft = ParticleMat("M_FX_SoftGlow", add, "T_FX_SoftGlow");
        Material ring = ParticleMat("M_FX_WaterRing", alpha, "T_FX_Ring");
        Waterfall(streak, soft, ring);
        var upgraded = AssetDatabase.LoadAssetAtPath<GameObject>(FX + "/VFX_WaterfallBase.prefab");
        if (upgraded != null)
        {
            var labels = new System.Collections.Generic.List<string>(AssetDatabase.GetLabels(upgraded));
            if (!labels.Contains(WaterfallVfxVersionLabel))
            {
                labels.Add(WaterfallVfxVersionLabel);
                AssetDatabase.SetLabels(upgraded, labels.ToArray());
            }
        }
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[ENV Art] Upgraded VFX_WaterfallBase: lip droplets, splash flecks, dense impact foam and mist.");
    }

    [MenuItem("Tools/ENV Art/02 - Create Isolated VFX Preview Scene")]
    public static void CreateShowroom()
    {
        GenerateAllIfMissing();
        EnsureFolder(Out, "Scenes");
        Scene s = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        s.name = "ENV_VFX_Showroom";
        var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Preview floor (not part of the game map)";
        ground.transform.localScale = new Vector3(3, 1, 2.2f);
        SceneManager.MoveGameObjectToScene(ground, s);
        Material groundMat = GetMat("M_ShowroomGround", Shader.Find("Universal Render Pipeline/Lit"));
        if (groundMat != null)
        {
            groundMat.SetColor("_BaseColor", new Color(.12f, .17f, .23f));
            ground.GetComponent<Renderer>().sharedMaterial = groundMat;
        }
        string[] names = {"VFX_GemIdle", "VFX_GemActivation", "VFX_SpiritGlow", "VFX_Campfire", "VFX_Snowfall", "VFX_Fountain", "VFX_WaterfallBase", "VFX_RiverAmbient", "VFX_LampSparkles", "VFX_ColorRestoreRing"};
        for (int i = 0; i < names.Length; ++i)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(FX + "/" + names[i] + ".prefab");
            if (prefab == null) continue;
            var instance = PrefabUtility.InstantiatePrefab(prefab, s) as GameObject;
            if (instance == null) continue;
            instance.transform.position = new Vector3(-9 + (i % 5) * 4.5f, 0.5f, -2.7f + (i / 5) * 5.0f);
            if (names[i] == "VFX_Snowfall") instance.transform.position += Vector3.up * 4f;
            // Objects here are only labels/preview markers; no duplicates in the actual map.
            var label = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            label.name = "Preview marker - " + names[i];
            label.transform.SetParent(instance.transform, false);
            label.transform.localScale = new Vector3(.42f, .04f, .42f);
            label.transform.localPosition = new Vector3(0, -.45f, 0);
        }
        var cameraGo = new GameObject("Preview Camera");
        SceneManager.MoveGameObjectToScene(cameraGo, s);
        var cam = cameraGo.AddComponent<Camera>();
        cameraGo.tag = "Untagged"; // Don't take over the player's main camera.
        cameraGo.transform.position = new Vector3(0, 15, -16);
        cameraGo.transform.LookAt(new Vector3(0, 0, 1));
        cam.fieldOfView = 55;
        cam.enabled = true; // saved scene is intended to be opened independently
        var lightGo = new GameObject("Preview directional light");
        SceneManager.MoveGameObjectToScene(lightGo, s);
        var light = lightGo.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.15f;
        lightGo.transform.rotation = Quaternion.Euler(45, -30, 0);
        EditorSceneManager.SaveScene(s, Scenes + "/ENV_VFX_Showroom.unity");
        EditorSceneManager.CloseScene(s, true);
        Debug.Log("[ENV Art] Preview scene saved: " + Scenes + "/ENV_VFX_Showroom.unity. Open independently to inspect.");
    }

    private static void GenerateAllIfMissing()
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(FX + "/VFX_GemIdle.prefab") == null) GenerateAll();
    }
    private static void EnsureFolder(string parent, string name)
    {
        if (!AssetDatabase.IsValidFolder(parent + "/" + name)) AssetDatabase.CreateFolder(parent, name);
    }
    private static Material GetMat(string name, Shader shader)
    {
        if (shader == null) return null;
        string path = Mats + "/" + name + ".mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            mat = new Material(shader) { name = name };
            AssetDatabase.CreateAsset(mat, path);
        }
        else mat.shader = shader;
        EditorUtility.SetDirty(mat);
        return mat;
    }
    private static Material ParticleMat(string name, Shader shader, string spriteName)
    {
        var mat = GetMat(name, shader);
        var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(Root + "/Textures/" + spriteName + ".png");
        if (tex == null) Debug.LogWarning("[ENV Art] Missing particle sprite: " + spriteName);
        mat.SetTexture("_MainTex", tex);
        mat.SetColor("_TintColor", Color.white);
        return mat;
    }
    private static void MakeWaters(Shader shader)
    {
        Water("M_Water_River", shader, new Color(.13f, .80f, .93f, .96f), new Vector2(.035f, .014f), .015f, .15f);
        Water("M_Water_Fountain", shader, new Color(.17f, .79f, .94f, .97f), new Vector2(.012f, .028f), .055f, .22f);
        Water("M_Water_Waterfall", shader, new Color(.35f, .89f, 1f, .96f), new Vector2(0f, -.65f), .012f, .18f);
    }
    private static void Water(string name, Shader shader, Color col, Vector2 flow, float ripple, float foam)
    {
        var m = GetMat(name, shader);
        m.SetColor("_BaseColor", col);
        m.SetVector("_FlowSpeed", new Vector4(flow.x, flow.y, 0, 0));
        m.SetFloat("_RippleStrength", ripple);
        m.SetFloat("_RippleFrequency", 12f);
        m.SetFloat("_RippleSpeed", 2f);
        m.SetFloat("_FoamStrength", foam);
        m.SetFloat("_Alpha", .92f);
    }
    private static void MakeGemMaterials()
    {
        Color[] colors = {
            new Color(.12f,.48f,1f), new Color(1f,.18f,.23f), new Color(1f,.85f,.12f),
            new Color(.12f,.94f,.94f), new Color(1f,.49f,.10f), new Color(.24f,.92f,.31f), new Color(.62f,.23f,1f)
        };
        string[] names = {"Blue", "Red", "Yellow", "Cyan", "Orange", "Green", "Purple"};
        Shader lit = Shader.Find("Universal Render Pipeline/Lit");
        if (lit == null) {Debug.LogError("[ENV Art] URP/Lit not found. URP is required.");return;}
        for (int i=0;i<names.Length;++i)
        {
            Material m=GetMat("M_Gem_"+ (i+1).ToString("00") + "_"+names[i],lit);
            m.SetColor("_BaseColor", colors[i]);
            m.SetColor("_EmissionColor", colors[i]*.85f);
            m.EnableKeyword("_EMISSION");
            m.SetFloat("_Smoothness", .62f);
            m.SetFloat("_Metallic", .05f);
        }
    }
    private static void MakeLamp()
    {
        var lit=Shader.Find("Universal Render Pipeline/Lit");
        if(lit==null)return;
        var m=GetMat("M_Lamp_Warm_Emissive",lit);
        m.SetColor("_BaseColor",new Color(1f,.8f,.38f));
        m.SetColor("_EmissionColor",new Color(3.3f,1.5f,.36f));
        m.EnableKeyword("_EMISSION");
    }
    private static GameObject NewRoot(string name) => new GameObject(name);
    private static GameObject Child(GameObject parent,string name)
    {
        var obj = new GameObject(name);
        obj.transform.SetParent(parent.transform, false);
        return obj;
    }
    private static ParticleSystem System(GameObject root,string name,Material material,float lifeMin,float lifeMax,float sizeMin,float sizeMax,float speedMin,float speedMax,float count,bool loop=true)
    {
        var o=Child(root,name);
        var ps=o.AddComponent<ParticleSystem>();
        var main=ps.main;
        main.loop=loop;
        main.playOnAwake=true;
        main.duration=loop?5f:1.6f;
        main.startLifetime=new ParticleSystem.MinMaxCurve(lifeMin,lifeMax);
        main.startSize=new ParticleSystem.MinMaxCurve(sizeMin,sizeMax);
        main.startSpeed=new ParticleSystem.MinMaxCurve(speedMin,speedMax);
        main.startRotation=new ParticleSystem.MinMaxCurve(0f,6.28318f);
        main.simulationSpace=ParticleSystemSimulationSpace.Local;
        main.maxParticles=loop?220:150;
        var emission=ps.emission;
        emission.enabled=true;
        emission.rateOverTime=loop?count:0;
        if(!loop) emission.SetBursts(new [] {new ParticleSystem.Burst(0f,(short)count)});
        var shape=ps.shape;
        shape.enabled=true;
        shape.shapeType=ParticleSystemShapeType.Sphere;
        shape.radius=.25f;
        var renderer=o.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial=material;
        renderer.renderMode=ParticleSystemRenderMode.Billboard;
        renderer.alignment=ParticleSystemRenderSpace.View;
        return ps;
    }
    private static void Tint(ParticleSystem ps, Color from, Color to)
    {
        var main=ps.main;
        main.startColor=from;
        var c=ps.colorOverLifetime;
        c.enabled=true;
        var g=new Gradient();
        g.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(Color.white,1)},
                  new[]{new GradientAlphaKey(.0f,0f),new GradientAlphaKey(.95f,.15f),new GradientAlphaKey(.75f,.55f),new GradientAlphaKey(0,1)});
        c.color=g;
        // Per particle variants can be changed directly in ParticleSystem; no runtime driver.
        if(to != from) main.startColor=new ParticleSystem.MinMaxGradient(from,to);
    }
    private static void SavePrefab(GameObject root)
    {
        string path=FX+"/"+root.name+".prefab";
        PrefabUtility.SaveAsPrefabAsset(root,path);
        UnityEngine.Object.DestroyImmediate(root);
    }
    private static void GemIdle(Material soft,Material star)
    {
        var root=NewRoot("VFX_GemIdle");
        var glow=System(root,"Core soft glow",soft,.5f,1.25f,.38f,.85f,0,.04f,8);
        Tint(glow,new Color(.25f,.75f,1f,.55f),new Color(.7f,.95f,1f,.8f));
        var spark=System(root,"Crystal stars",star,.6f,1.5f,.04f,.15f,.06f,.25f,13);
        Tint(spark,new Color(.65f,.9f,1f,.9f),Color.white);
        SavePrefab(root);
    }
    private static void GemActivation(Material ring,Material star,Material soft)
    {
        var root=NewRoot("VFX_GemActivation");
        var burst=System(root,"One shot stars",star,.45f,.95f,.09f,.23f,1.1f,2.8f,40,false);
        Tint(burst,new Color(.5f,.9f,1f,1f),Color.white);
        var halo=System(root,"One shot halo",soft,.16f,.45f,.55f,1.8f,.12f,.3f,10,false);
        Tint(halo,new Color(.3f,.8f,1f,.8f),Color.white);
        var circle=System(root,"One shot expanding ring",ring,.7f,.85f,.9f,1.4f,0,0,2,false);
        var size=circle.sizeOverLifetime; size.enabled=true; size.size=new ParticleSystem.MinMaxCurve(1f,new AnimationCurve(new Keyframe(0,.2f),new Keyframe(1,2.5f)));
        var rr=circle.GetComponent<ParticleSystemRenderer>(); rr.renderMode=ParticleSystemRenderMode.HorizontalBillboard;
        Tint(circle,new Color(.7f,.95f,1f,.85f),Color.white);
        SavePrefab(root);
    }
    private static void Spirit(Material soft,Material star)
    {
        var root=NewRoot("VFX_SpiritGlow");
        var glow=System(root,"Fairy soft light",soft,.7f,1.5f,.25f,.55f,.02f,.08f,18);
        Tint(glow,new Color(.30f,.95f,.86f,.55f),new Color(.65f,1f,.95f,.85f));
        var flies=System(root,"Fairy orbit sparks",star,1f,2.4f,.03f,.1f,.08f,.3f,12);
        Tint(flies,new Color(.37f,1f,.9f,1),Color.white);
        var shape=flies.shape;shape.radius=.45f;
        SavePrefab(root);
    }
    private static void Campfire(Material flame,Material sparks,Material smoke)
    {
        var root=NewRoot("VFX_Campfire");
        var f=System(root,"Warm cartoon flames",flame,.4f,.8f,.2f,.55f,.3f,.9f,32);
        Tint(f,new Color(1f,.55f,.1f,.95f),new Color(1f,.9f,.4f,.95f));
        var cone=f.shape; cone.shapeType=ParticleSystemShapeType.Cone;cone.radius=.28f;cone.angle=8;
        var e=System(root,"Small rising embers",sparks,.6f,1.8f,.025f,.075f,.5f,1.7f,12);
        Tint(e,new Color(1f,.65f,.12f,.85f),new Color(1f,.25f,.03f,.85f));
        var s=System(root,"Light smoke",smoke,1.4f,3.2f,.30f,.75f,.24f,.5f,3);
        Tint(s,new Color(.32f,.35f,.39f,.27f),new Color(.48f,.51f,.55f,.20f));
        var smokeShape=s.shape;smokeShape.radius=.16f;
        SavePrefab(root);
    }
    private static void Snow(Material snow,Material dots)
    {
        var root=NewRoot("VFX_Snowfall");
        var flake=System(root,"Six arm snowflakes",snow,5f,8f,.08f,.2f,.2f,.45f,28);
        var sh=flake.shape;sh.shapeType=ParticleSystemShapeType.Box;sh.scale=new Vector3(14f,1f,14f);
        flake.transform.localPosition=new Vector3(0,6f,0);
        var main=flake.main;main.gravityModifier=.03f;main.maxParticles=300;
        var fall=flake.velocityOverLifetime;fall.enabled=true;fall.y=new ParticleSystem.MinMaxCurve(-.9f);fall.x=new ParticleSystem.MinMaxCurve(.07f);
        Tint(flake,new Color(.93f,.97f,1f,.92f),Color.white);
        var fine=System(root,"Small white snow",dots,4f,7f,.025f,.075f,.2f,.45f,45);
        var s2=fine.shape;s2.shapeType=ParticleSystemShapeType.Box;s2.scale=new Vector3(14f,1f,14f);
        fine.transform.localPosition=new Vector3(0,6f,0);
        var m2=fine.main;m2.gravityModifier=.03f;m2.maxParticles=450;
        var fineFall=fine.velocityOverLifetime;fineFall.enabled=true;fineFall.y=new ParticleSystem.MinMaxCurve(-.75f);fineFall.x=new ParticleSystem.MinMaxCurve(.05f);
        Tint(fine,new Color(.85f,.94f,1f,.75f),Color.white);
        SavePrefab(root);
    }
    private static void Fountain(Material drops,Material ring,Material soft)
    {
        var root=NewRoot("VFX_Fountain");
        var spray=System(root,"Water droplets",drops,1.0f,2.0f,.04f,.12f,2f,4f,65);
        var main=spray.main;main.gravityModifier=.7f;main.maxParticles=260;
        var shape=spray.shape;shape.shapeType=ParticleSystemShapeType.Cone;shape.radius=.12f;shape.angle=22f;
        Tint(spray,new Color(.35f,.9f,1f,.9f),new Color(.9f,1f,1f,.85f));
        var foam=System(root,"Fountain surface ripple",ring,.7f,1.1f,.6f,1.2f,0,0,3);
        var rr=foam.GetComponent<ParticleSystemRenderer>();rr.renderMode=ParticleSystemRenderMode.HorizontalBillboard;
        Tint(foam,new Color(.75f,.99f,1f,.45f),Color.white);
        var mist=System(root,"White mist",soft,.3f,.9f,.12f,.3f,.08f,.25f,10);
        Tint(mist,new Color(.72f,.96f,1f,.24f),Color.white);
        SavePrefab(root);
    }
    private static void Waterfall(Material streak,Material soft,Material ring)
    {
        var root=NewRoot("VFX_WaterfallBase");
        // The sheet shader carries the continuous water volume. Particles only sell
        // breakup at the lip and impact, avoiding the old "rain under a blue Quad" look.
        var lip=System(root,"Lip breakup droplets",streak,.35f,.7f,.025f,.085f,.9f,1.8f,46);
        lip.transform.localPosition=new Vector3(0,.35f,0);
        var lipShape=lip.shape;lipShape.shapeType=ParticleSystemShapeType.Box;lipShape.scale=new Vector3(2.15f,.06f,.32f);
        var lipVelocity=lip.velocityOverLifetime;lipVelocity.enabled=true;lipVelocity.y=new ParticleSystem.MinMaxCurve(-2.7f,-1.25f);lipVelocity.x=new ParticleSystem.MinMaxCurve(-.22f,.22f);
        var lipMain=lip.main;lipMain.maxParticles=150;
        Tint(lip,new Color(.74f,.98f,1f,.82f),Color.white);

        var splashes=System(root,"Impact foam flecks",streak,.28f,.62f,.045f,.16f,.65f,1.65f,88);
        var shape=splashes.shape;shape.shapeType=ParticleSystemShapeType.Box;shape.scale=new Vector3(2.7f,.06f,.55f);
        var splashVelocity=splashes.velocityOverLifetime;splashVelocity.enabled=true;splashVelocity.y=new ParticleSystem.MinMaxCurve(.16f,.85f);splashVelocity.x=new ParticleSystem.MinMaxCurve(-.55f,.55f);
        var splashMain=splashes.main;splashMain.gravityModifier=.62f;splashMain.maxParticles=240;
        Tint(splashes,new Color(.82f,1f,1f,.9f),Color.white);

        var mist=System(root,"Foamy impact mist",soft,.6f,1.35f,.32f,.9f,.03f,.2f,24);
        var ms=mist.shape;ms.shapeType=ParticleSystemShapeType.Box;ms.scale=new Vector3(2.85f,.08f,.7f);
        mist.transform.localPosition=new Vector3(0,.08f,0);
        var mistVelocity=mist.velocityOverLifetime;mistVelocity.enabled=true;mistVelocity.y=new ParticleSystem.MinMaxCurve(.12f,.38f);mistVelocity.x=new ParticleSystem.MinMaxCurve(-.1f,.1f);
        var mistMain=mist.main;mistMain.maxParticles=100;
        Tint(mist,new Color(.76f,.98f,1f,.24f),new Color(.92f,1f,1f,.32f));

        var rip=System(root,"Impact foam rings",ring,.52f,.9f,.72f,1.22f,0,0,8);
        rip.GetComponent<ParticleSystemRenderer>().renderMode=ParticleSystemRenderMode.HorizontalBillboard;
        var rippleSize=rip.sizeOverLifetime;rippleSize.enabled=true;rippleSize.size=new ParticleSystem.MinMaxCurve(1f,new AnimationCurve(new Keyframe(0,.32f),new Keyframe(1,1.85f)));
        Tint(rip,new Color(.77f,1f,1f,.62f),Color.white);
        SavePrefab(root);
    }
    private static void RiverAmbient(Material soft)
    {
        var root=NewRoot("VFX_RiverAmbient");
        var foam=System(root,"Ambient water glints",soft,1f,2.5f,.06f,.16f,.01f,.08f,12);
        var sh=foam.shape;sh.shapeType=ParticleSystemShapeType.Box;sh.scale=new Vector3(5,.1f,1.2f);
        Tint(foam,new Color(.8f,1f,1f,.28f),Color.white);
        SavePrefab(root);
    }
    private static void LampSparkles(Material soft,Material stars)
    {
        var root=NewRoot("VFX_LampSparkles");
        var glow=System(root,"Warm light dust",soft,1.2f,2.4f,.09f,.23f,.02f,.08f,9);
        Tint(glow,new Color(1f,.75f,.27f,.38f),new Color(1f,.92f,.6f,.25f));
        var flecks=System(root,"Warm stars",stars,.5f,1.3f,.015f,.055f,.02f,.09f,2);
        Tint(flecks,new Color(1f,.9f,.5f,.65f),Color.white);
        SavePrefab(root);
    }
    private static void ColorRestoreRing(Material ring,Material star)
    {
        var root=NewRoot("VFX_ColorRestoreRing");
        var p=System(root,"White expanding ground circle",ring,1.1f,1.1f,1f,1f,0,0,1,false);
        p.GetComponent<ParticleSystemRenderer>().renderMode=ParticleSystemRenderMode.HorizontalBillboard;
        var size=p.sizeOverLifetime;
        size.enabled=true;
        size.size=new ParticleSystem.MinMaxCurve(1f,new AnimationCurve(new Keyframe(0,.15f),new Keyframe(1,6f)));
        Tint(p,new Color(1f,1f,1f,.8f),Color.white);
        var dust=System(root,"Restoration white sparks",star,.6f,1.2f,.04f,.14f,.4f,1.8f,30,false);
        Tint(dust,new Color(.9f,1f,1f,.8f),Color.white);
        SavePrefab(root);
    }
}
#endif
