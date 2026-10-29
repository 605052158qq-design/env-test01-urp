using UnityEngine;

/// <summary>A05/A11/A12/A13: uses ONE particle system attached to the actual object. Choose a preset once.</summary>
[RequireComponent(typeof(ParticleSystem))]
[DisallowMultipleComponent]
public sealed class ENVCartoonParticles : MonoBehaviour
{
    public enum Look { GemSparkles, Campfire, Snow, FairyGlitter }
    public Look look = Look.GemSparkles;
    public Material urpParticleMaterial;
    public bool configureOnAwake = true;
    [ContextMenu("Apply Particle Preset")]
    public void ApplyPreset()
    {
        var ps = GetComponent<ParticleSystem>();
        if (ps == null) return;
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = ps.main;
        main.loop = true;
        main.playOnAwake = true;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        var emission = ps.emission;
        var shape = ps.shape;
        shape.enabled = true;
        var color = ps.colorOverLifetime;
        color.enabled = true;
        var rotation = ps.rotationOverLifetime;
        rotation.enabled = false;
        Color a, b;
        switch (look)
        {
            case Look.Campfire:
                main.startLifetime = new ParticleSystem.MinMaxCurve(0.3f, 0.8f);
                main.startSpeed = new ParticleSystem.MinMaxCurve(0.25f, 1.0f);
                main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.24f);
                main.startColor = new Color(1f, 0.7f, 0.14f);
                main.gravityModifier = 0f;
                emission.rateOverTime = 55f;
                shape.shapeType = ParticleSystemShapeType.Cone; shape.radius = 0.2f; shape.angle = 12f;
                a = new Color(1f, 0.9f, 0.35f, 0f); b = new Color(1f, 0.25f, 0.04f, 0f);
                break;
            case Look.Snow:
                main.startLifetime = new ParticleSystem.MinMaxCurve(4f, 8f);
                main.startSpeed = new ParticleSystem.MinMaxCurve(0.15f, 0.45f);
                main.startSize = new ParticleSystem.MinMaxCurve(0.035f, 0.12f);
                main.startColor = new Color(1f, 1f, 1f);
                main.gravityModifier = 0.15f;
                emission.rateOverTime = 140f;
                shape.shapeType = ParticleSystemShapeType.Box; shape.scale = new Vector3(35f, 1f, 35f);
                a = new Color(1f, 1f, 1f, 0f); b = new Color(1f, 1f, 1f, 0f);
                break;
            case Look.FairyGlitter:
                main.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 1.6f);
                main.startSpeed = new ParticleSystem.MinMaxCurve(0.05f, 0.25f);
                main.startSize = new ParticleSystem.MinMaxCurve(0.04f, 0.11f);
                main.startColor = new Color(0.5f, 0.95f, 1f);
                main.gravityModifier = 0f;
                emission.rateOverTime = 22f;
                shape.shapeType = ParticleSystemShapeType.Sphere; shape.radius = 0.45f;
                a = new Color(0.3f, 0.8f, 1f, 0f); b = new Color(0.8f, 1f, 1f, 0f);
                break;
            default:
                main.startLifetime = new ParticleSystem.MinMaxCurve(0.6f, 1.6f);
                main.startSpeed = new ParticleSystem.MinMaxCurve(0.08f, 0.35f);
                main.startSize = new ParticleSystem.MinMaxCurve(0.045f, 0.12f);
                main.startColor = new Color(0.4f, 0.8f, 1f);
                main.gravityModifier = 0f;
                emission.rateOverTime = 32f;
                shape.shapeType = ParticleSystemShapeType.Sphere; shape.radius = 0.75f;
                a = new Color(0.2f, 0.7f, 1f, 0f); b = new Color(0.85f, 1f, 1f, 0f);
                break;
        }
        Gradient gradient = new Gradient();
        gradient.SetKeys(new [] { new GradientColorKey(a, 0f), new GradientColorKey(b, 1f) },
                         new [] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.25f), new GradientAlphaKey(0f, 1f) });
        color.color = new ParticleSystem.MinMaxGradient(gradient);
        var rend = ps.GetComponent<ParticleSystemRenderer>();
        if (rend != null && urpParticleMaterial != null) rend.sharedMaterial = urpParticleMaterial;
        ps.Play();
    }
    private void Awake() { if (configureOnAwake) ApplyPreset(); }
}
