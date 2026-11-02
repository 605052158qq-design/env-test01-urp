#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class ENVCampfireMainTuner
{
    [MenuItem("Tools/ENV Art/Fire - Tune ps_fire_main on Selected Scene Instance")]
    private static void Tune()
    {
        GameObject root = Selection.activeGameObject;
        if (root == null || EditorUtility.IsPersistent(root))
        {
            Debug.LogWarning("[ENV Fire] Select the existing pfb_eff_fire scene instance, not the original asset.");
            return;
        }
        ParticleSystem mainFire = null;
        foreach (var p in root.GetComponentsInChildren<ParticleSystem>(true))
        {
            if (p.name == "ps_fire_main") { mainFire = p; break; }
        }
        if (mainFire == null) { Debug.LogWarning("[ENV Fire] ps_fire_main not found under selection."); return; }
        Undo.RecordObject(mainFire, "ENV tune existing main campfire flame");
        var main = mainFire.main;
        main.startLifetime = 1.0f;
        main.startSpeed = 1.45f;
        main.startSize = new ParticleSystem.MinMaxCurve(0.75f, 1.15f);
        var size = mainFire.sizeOverLifetime;
        size.enabled = true;
        size.separateAxes = false;
        var curve = new AnimationCurve(
            new Keyframe(0, 1.0f), new Keyframe(0.24f, 0.9f),
            new Keyframe(0.58f, 0.52f), new Keyframe(1, 0.0f));
        size.size = new ParticleSystem.MinMaxCurve(1.0f, curve);
        var emission = mainFire.emission;
        emission.enabled = true;
        emission.rateOverTime = 14f;
        EditorUtility.SetDirty(mainFire);
        Debug.Log("[ENV Fire] Tuned ONLY ps_fire_main on this existing scene instance. " +
                  "Other flame/spark systems and original prefab asset were not touched.", mainFire);
    }
}
#endif
