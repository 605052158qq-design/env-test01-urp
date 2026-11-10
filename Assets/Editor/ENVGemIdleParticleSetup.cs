#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>Converts the retained star child in VFX_GemActivation into a quiet, always-on gem idle effect.</summary>
public static class ENVGemIdleParticleSetup
{
    private const string PrefabPath = "Assets/ENVArtResources/Generated/Prefabs/VFX_GemActivation.prefab";
    private const string StarsName = "One shot stars";

    [MenuItem("Tools/ENV VFX/01 - Convert Gem Stars To Floating Idle")]
    public static void ConvertOneShotStarsToFloating()
    {
        GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            Transform stars = FindChild(root.transform, StarsName);
            if (stars == null)
            {
                Debug.LogError("[ENV VFX] Missing child '" + StarsName + "' in " + PrefabPath);
                return;
            }
            ParticleSystem system = stars.GetComponent<ParticleSystem>();
            if (system == null)
            {
                Debug.LogError("[ENV VFX] '" + StarsName + "' has no ParticleSystem.", stars);
                return;
            }

            stars.name = "Idle floating stars";
            ParticleSystem.MainModule main = system.main;
            main.loop = true;
            main.duration = 5f;
            main.prewarm = true;
            main.playOnAwake = true;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.startLifetime = new ParticleSystem.MinMaxCurve(2.1f, 3.4f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.025f, 0.09f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.065f, 0.15f);
            main.startColor = new ParticleSystem.MinMaxGradient(
                new Color(0.14f, 0.62f, 1f, 0.65f), new Color(0.78f, 0.98f, 1f, 1f));
            main.maxParticles = 42;
            main.gravityModifier = 0f;

            ParticleSystem.EmissionModule emission = system.emission;
            emission.enabled = true;
            emission.rateOverTime = 9f;
            emission.rateOverDistance = 0f;
            emission.SetBursts(new ParticleSystem.Burst[0]);

            ParticleSystem.ShapeModule shape = system.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.58f;
            shape.radiusThickness = 0.88f;

            ParticleSystem.VelocityOverLifetimeModule velocity = system.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.Local;
            // Unity requires each XYZ group in Velocity over Lifetime to use the
            // same curve mode.  Explicit constants prevent invalid inherited
            // curve modes from the former one-shot star effect.
            velocity.x = new ParticleSystem.MinMaxCurve(0f);
            velocity.y = new ParticleSystem.MinMaxCurve(0.09f);
            velocity.z = new ParticleSystem.MinMaxCurve(0f);
            velocity.orbitalX = new ParticleSystem.MinMaxCurve(0f);
            velocity.orbitalY = new ParticleSystem.MinMaxCurve(0.48f);
            velocity.orbitalZ = new ParticleSystem.MinMaxCurve(0f);
            velocity.radial = new ParticleSystem.MinMaxCurve(0f);

            ParticleSystem.ColorOverLifetimeModule color = system.colorOverLifetime;
            color.enabled = true;
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(new Color(0.12f, 0.55f, 1f), 0f),
                    new GradientColorKey(new Color(0.76f, 0.98f, 1f), 0.35f),
                    new GradientColorKey(new Color(0.25f, 0.72f, 1f), 1f)
                },
                new[]
                {
                    new GradientAlphaKey(0f, 0f),
                    new GradientAlphaKey(0.92f, 0.18f),
                    new GradientAlphaKey(0.5f, 0.72f),
                    new GradientAlphaKey(0f, 1f)
                });
            color.color = new ParticleSystem.MinMaxGradient(gradient);

            ParticleSystem.SizeOverLifetimeModule size = system.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
                new Keyframe(0f, 0.35f), new Keyframe(0.18f, 1f),
                new Keyframe(0.72f, 0.8f), new Keyframe(1f, 0.1f)));

            ParticleSystemRenderer renderer = system.GetComponent<ParticleSystemRenderer>();
            if (renderer != null) renderer.renderMode = ParticleSystemRenderMode.Billboard;

            system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            AssetDatabase.SaveAssets();
            Debug.Log("[ENV VFX] Converted 'One shot stars' to the looping idle gem particle in " + PrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    [MenuItem("Tools/ENV VFX/02 - Add Gem Dissolve Scatter + Fade")]
    public static void AddDissolveScatterAndFade()
    {
        GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            ParticleSystem[] allSystems = root.GetComponentsInChildren<ParticleSystem>(true);
            var controller = root.GetComponent<ENVGemActivationVfxController>();
            if (controller == null) controller = root.AddComponent<ENVGemActivationVfxController>();

            Transform scatterTransform = FindChild(root.transform, "Dissolve scatter");
            ParticleSystem scatter;
            if (scatterTransform == null)
            {
                GameObject child = new GameObject("Dissolve scatter", typeof(ParticleSystem));
                child.transform.SetParent(root.transform, false);
                child.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f); // cone's local forward points upward
                scatter = child.GetComponent<ParticleSystem>();
                ParticleSystemRenderer renderer = child.GetComponent<ParticleSystemRenderer>();
                ParticleSystemRenderer sourceRenderer = allSystems.Length > 0 ? allSystems[0].GetComponent<ParticleSystemRenderer>() : null;
                if (renderer != null && sourceRenderer != null) renderer.sharedMaterial = sourceRenderer.sharedMaterial;
            }
            else scatter = scatterTransform.GetComponent<ParticleSystem>();

            if (scatter == null)
            {
                Debug.LogError("[ENV VFX] Dissolve scatter is missing its ParticleSystem.", root);
                return;
            }
            ConfigureDissolveScatter(scatter);
            controller.idleParticles = root.GetComponentsInChildren<ParticleSystem>(true);
            controller.dissolveScatter = scatter;
            controller.fadeSeconds = 1.25f;
            controller.hideAfterSeconds = 1.55f;

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            AssetDatabase.SaveAssets();
            Debug.Log("[ENV VFX] Added fixed-direction dissolve scatter and fade/hide controller to " + PrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static void ConfigureDissolveScatter(ParticleSystem system)
    {
        ParticleSystem.MainModule main = system.main;
        main.loop = false;
        main.duration = 1.5f;
        main.prewarm = false;
        main.playOnAwake = false;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.85f, 1.35f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.7f, 1.1f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.055f, 0.14f);
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.16f, 0.64f, 1f, 0.9f), new Color(0.86f, 1f, 1f, 1f));
        main.maxParticles = 44;
        main.gravityModifier = 0f;

        ParticleSystem.EmissionModule emission = system.emission;
        emission.enabled = true;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 34) });

        ParticleSystem.ShapeModule shape = system.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.radius = 0.16f;
        shape.angle = 14f;

        ParticleSystem.VelocityOverLifetimeModule velocity = system.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.Local;
        velocity.x = new ParticleSystem.MinMaxCurve(0f);
        velocity.y = new ParticleSystem.MinMaxCurve(0.18f);
        velocity.z = new ParticleSystem.MinMaxCurve(0f);
        velocity.orbitalX = new ParticleSystem.MinMaxCurve(0f);
        velocity.orbitalY = new ParticleSystem.MinMaxCurve(0f);
        velocity.orbitalZ = new ParticleSystem.MinMaxCurve(0f);
        velocity.radial = new ParticleSystem.MinMaxCurve(0f);

        ParticleSystem.ColorOverLifetimeModule color = system.colorOverLifetime;
        color.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[] { new GradientColorKey(new Color(0.35f, 0.8f, 1f), 0f), new GradientColorKey(Color.white, 0.35f), new GradientColorKey(new Color(0.1f, 0.5f, 1f), 1f) },
            new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.1f), new GradientAlphaKey(0.55f, 0.7f), new GradientAlphaKey(0f, 1f) });
        color.color = new ParticleSystem.MinMaxGradient(gradient);

        ParticleSystem.SizeOverLifetimeModule size = system.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, 0.6f), new Keyframe(0.16f, 1f), new Keyframe(1f, 0.12f)));
    }

    private static Transform FindChild(Transform parent, string childName)
    {
        if (parent.name == childName) return parent;
        for (int i = 0; i < parent.childCount; i++)
        {
            Transform found = FindChild(parent.GetChild(i), childName);
            if (found != null) return found;
        }
        return null;
    }
}
#endif
