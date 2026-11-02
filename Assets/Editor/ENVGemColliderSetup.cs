#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class ENVGemColliderSetup
{
    [MenuItem("Tools/ENV Gems/01 - Add Collider Zones to Selected Real Gems")]
    private static void BuildSelected()
    {
        int changed = 0;
        foreach (GameObject selected in Selection.gameObjects)
        {
            ENVGem gem = selected.GetComponent<ENVGem>() ?? selected.GetComponentInParent<ENVGem>();
            if (gem == null) { Debug.LogWarning("Select a real object with ENVGem: " + selected.name); continue; }
            Transform child = gem.transform.Find("ENV_GemInteractionTrigger");
            if (child == null)
            {
                GameObject obj = new GameObject("ENV_GemInteractionTrigger");
                Undo.RegisterCreatedObjectUndo(obj,"Create gem trigger zone");
                obj.transform.SetParent(gem.transform,false);
                child = obj.transform;
            }
            BoxCollider collider = child.GetComponent<BoxCollider>();
            if (collider == null) collider = Undo.AddComponent<BoxCollider>(child.gameObject);
            Undo.RecordObject(collider,"Configure gem trigger");
            collider.isTrigger = true;
            // Intentionally extends UNDER elevated gemstones (e.g. red gem over the fountain).
            if (collider.size == Vector3.one)
            {
                collider.center = new Vector3(0f,-1.75f,0f);
                collider.size = new Vector3(4f,5.5f,4f);
            }
            ENVGemTriggerZone zone = child.GetComponent<ENVGemTriggerZone>();
            if (zone == null) zone = Undo.AddComponent<ENVGemTriggerZone>(child.gameObject);
            Undo.RecordObject(zone,"Bind trigger gem");
            zone.gem = gem;
            changed++;
        }
        Debug.Log("[ENV Gem Collider] Configured " + changed + " real gem collider zones. Resize each BoxCollider as needed.");
    }

    [MenuItem("Tools/ENV Gems/03 - Assign RED Recovery Layer to Selected Renderers")]
    private static void SetRedGroup()
    {
        int count = 0;
        foreach (GameObject selected in Selection.gameObjects)
        {
            foreach (Renderer r in selected.GetComponentsInChildren<Renderer>(true))
            {
                if (r == null) continue;
                Undo.RecordObject(r,"Assign RED gem recovery rendering layer");
                // Current ENVMaterialColorRecovery uses bits 1..7 for gems 0..6.
                // Clear only these seven bits; preserve bit0 and all unrelated bits.
                r.renderingLayerMask = (r.renderingLayerMask & ~0xFEu) | (1u << 2);
                EditorUtility.SetDirty(r);
                count++;
            }
        }
        Debug.Log("[ENV Gem Layer] Assigned RED recovery bit2 to " + count +
            " Renderers under selected objects. Does not change material or prefab reference.");
    }

    [MenuItem("Tools/ENV Gems/02 - Audit Red Reveal Setup (No Changes)")]
    private static void Audit()
    {
        var gems = Object.FindObjectsOfType<ENVGem>(true);
        foreach (var g in gems)
        {
            var zone = g.GetComponentInChildren<ENVGemTriggerZone>(true);
            Debug.Log("[ENV Audit] " + g.gemstoneName + " index=" + g.sequenceIndex +
                ", gemActive=" + g.isActiveAndEnabled + ", trigger=" +
                (zone != null ? zone.name : "MISSING") + ", revealMaxRadius=" + g.revealMaxRadius, g);
        }
        var materialRecovery = Object.FindObjectOfType<ENVMaterialColorRecovery>(true);
        Debug.Log("[ENV Audit] ENVMaterialColorRecovery=" + (materialRecovery == null ? "MISSING" : "present") +
            ". If E activates red but colors do not restore, verify the RED group's Rendering Layer Mask (hui2 / bit 2) and that the active shader implements _ENVMaterialRevealCircles.");
        int redLayers = 0;
        foreach (Renderer r in Object.FindObjectsOfType<Renderer>(true))
            if ((r.renderingLayerMask & (1u << 2)) != 0) redLayers++;
        Debug.Log("[ENV Audit] Renderers with red recovery bit 2 (mask 4): " + redLayers +
            ". Zero means your red recovery group is not assigned to any Renderer.");
    }
}
#endif
