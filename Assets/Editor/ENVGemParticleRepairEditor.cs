#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>Manual repair only. No auto-run or asset/meta GUID manipulation.</summary>
public static class ENVGemParticleRepairEditor
{
    private const string GeneratedVfxFolder = "Assets/ENVArtResources/Generated/Prefabs";

    [MenuItem("Tools/ENV Repair/01 - Audit Gem Order and Trigger Zones (Read Only)")]
    public static void AuditGemOrder()
    {
        var gems = UnityEngine.Object.FindObjectsOfType<ENVGem>(true);
        Array.Sort(gems, (a, b) => a.sequenceIndex.CompareTo(b.sequenceIndex));
        var sb = new StringBuilder("[ENV Gem Audit] Gems found in open scenes: " + gems.Length + "\n");
        foreach (var gem in gems)
        {
            var zones = gem.GetComponentsInChildren<ENVGemTriggerZone>(true);
            var bridge = gem.GetComponent<ENVGemDissolveBridge>();
            var supported = false;
            var renderers = bridge != null && bridge.gemRenderers != null && bridge.gemRenderers.Length > 0
                ? bridge.gemRenderers : gem.GetComponentsInChildren<Renderer>(true);
            foreach (var renderer in renderers)
                if (renderer != null)
                    foreach (var mat in renderer.sharedMaterials)
                        if (mat != null && mat.HasProperty("_Dissolve")) supported = true;
            sb.Append("index=").Append(gem.sequenceIndex).Append(" ").Append(gem.gemstoneName)
              .Append(" object=").Append(gem.name)
              .Append(" zones=").Append(zones.Length)
              .Append(" _DissolveShader=").Append(supported ? "YES" : "NO")
              .Append(" bridge=").Append(bridge != null ? "YES" : "NO")
              .Append(" hideAfter=").Append(bridge != null && bridge.hideMeshAfterDissolve)
              .AppendLine();
        }
        var world = ENVWorldProgress.Instance;
        sb.Append("World next=").Append(world != null && world.NextGem != null ? world.NextGem.gemstoneName : "none")
          .Append("; registered=").Append(world != null ? world.RegisteredGemCount : 0).AppendLine();
        Debug.Log(sb.ToString());
    }

    [MenuItem("Tools/ENV Repair/02 - Repair Particle Velocity In Open Scenes")]
    public static void RepairSceneParticles()
    {
        int scanned = 0, modified = 0;
        foreach (var ps in Resources.FindObjectsOfTypeAll<ParticleSystem>())
        {
            if (ps == null || !IsSceneObject(ps.gameObject)) continue;
            scanned++;
            if (RepairParticle(ps)) modified++;
        }
        Debug.Log("[ENV VFX] Examined " + scanned + " ParticleSystems in loaded scenes; repaired " + modified +
                  ". Save the scene (Ctrl+S). To fix prefab source assets as well, run menu 03.");
    }

    [MenuItem("Tools/ENV Repair/03 - Repair Generated VFX Prefab Sources")]
    public static void RepairGeneratedVfxPrefabs()
    {
        if (!AssetDatabase.IsValidFolder(GeneratedVfxFolder))
        {
            Debug.LogWarning("[ENV VFX] Missing generated VFX prefab folder: " + GeneratedVfxFolder);
            return;
        }
        int scanned = 0, modified = 0;
        string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { GeneratedVfxFolder });
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (!path.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase)) continue;
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                int local = 0;
                foreach (var ps in root.GetComponentsInChildren<ParticleSystem>(true))
                    if (RepairParticle(ps)) local++;
                if (local > 0)
                {
                    PrefabUtility.SaveAsPrefabAsset(root, path); // preserves existing prefab/meta GUID
                    modified += local;
                }
                scanned++;
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
        AssetDatabase.SaveAssets();
        Debug.Log("[ENV VFX] Examined " + scanned + " generated VFX prefab assets; repaired " + modified +
                  ". Existing prefab GUIDs unchanged.");
    }

    [MenuItem("Tools/ENV Repair/04 - Enable End-of-Dissolve Mesh Hide (Scene Gems)")]
    public static void EnableHideAfterDissolve()
    {
        int modified = 0;
        foreach (var gem in UnityEngine.Object.FindObjectsOfType<ENVGem>(true))
        {
            var bridge = gem.GetComponent<ENVGemDissolveBridge>();
            if (bridge == null || bridge.hideMeshAfterDissolve) continue;
            Undo.RecordObject(bridge, "Enable end-of-dissolve hide");
            bridge.hideMeshAfterDissolve = true;
            EditorUtility.SetDirty(bridge);
            PrefabUtility.RecordPrefabInstancePropertyModifications(bridge);
            modified++;
        }
        Debug.Log("[ENV Gem] Enabled end-of-dissolve mesh hiding on " + modified +
                  " scene gems. This is a fallback only: smooth dissolution still needs a _Dissolve-capable shader. Save scene.");
    }

    private static bool RepairParticle(ParticleSystem ps)
    {
        var velocity = ps.velocityOverLifetime;
        if (!velocity.enabled) return false;
        var x = velocity.x;
        var y = velocity.y;
        var z = velocity.z;
        if (x.mode == y.mode && y.mode == z.mode) return false;
        Undo.RecordObject(ps, "Unify velocity curve modes");
        bool hasCurve = HasCurve(x.mode) || HasCurve(y.mode) || HasCurve(z.mode);
        velocity.enabled = false;
        if (hasCurve)
        {
            velocity.x = ToTwoCurves(x);
            velocity.y = ToTwoCurves(y);
            velocity.z = ToTwoCurves(z);
        }
        else
        {
            velocity.x = ToTwoConstants(x);
            velocity.y = ToTwoConstants(y);
            velocity.z = ToTwoConstants(z);
        }
        velocity.enabled = true;
        EditorUtility.SetDirty(ps);
        PrefabUtility.RecordPrefabInstancePropertyModifications(ps);
        Debug.Log("[ENV VFX] Repaired velocity X/Y/Z mode: " + ps.name, ps);
        return true;
    }

    private static bool HasCurve(ParticleSystemCurveMode mode)
    {
        return mode == ParticleSystemCurveMode.Curve || mode == ParticleSystemCurveMode.TwoCurves;
    }

    private static ParticleSystem.MinMaxCurve ToTwoConstants(ParticleSystem.MinMaxCurve source)
    {
        if (source.mode == ParticleSystemCurveMode.TwoConstants)
            return new ParticleSystem.MinMaxCurve(source.constantMin, source.constantMax);
        float val = source.constant;
        return new ParticleSystem.MinMaxCurve(val, val);
    }

    private static ParticleSystem.MinMaxCurve ToTwoCurves(ParticleSystem.MinMaxCurve source)
    {
        if (source.mode == ParticleSystemCurveMode.TwoCurves)
            return new ParticleSystem.MinMaxCurve(source.curveMultiplier, source.curveMin ?? Flat(0f), source.curveMax ?? Flat(0f));
        if (source.mode == ParticleSystemCurveMode.Curve)
        {
            var curve = source.curve ?? Flat(0f);
            return new ParticleSystem.MinMaxCurve(source.curveMultiplier, curve, curve);
        }
        float min = source.mode == ParticleSystemCurveMode.TwoConstants ? source.constantMin : source.constant;
        float max = source.mode == ParticleSystemCurveMode.TwoConstants ? source.constantMax : source.constant;
        return new ParticleSystem.MinMaxCurve(1f, Flat(min), Flat(max));
    }

    private static AnimationCurve Flat(float val)
    {
        return new AnimationCurve(new Keyframe(0f, val), new Keyframe(1f, val));
    }

    private static bool IsSceneObject(GameObject go)
    {
        return go != null && go.scene.IsValid() && go.scene.isLoaded &&
               !EditorUtility.IsPersistent(go) && (go.hideFlags & HideFlags.HideAndDontSave) == 0;
    }
}
#endif
