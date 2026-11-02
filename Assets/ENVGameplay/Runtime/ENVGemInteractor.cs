using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// E interaction is based on real Unity Trigger Colliders instead of the distance to
/// the gem root. The query runs only when E is pressed (or if the optional hint is on).
/// Existing ENVGem/ENVWorldProgress/animations are reused unchanged.
/// Replace the old .cs in-place; DO NOT replace its .meta file.
/// </summary>
[DisallowMultipleComponent]
public sealed class ENVGemInteractor : MonoBehaviour
{
    [Min(0.5f)] public float maxDistance = 3f; // kept for old serialized Inspector compatibility; not used for trigger checks
    public bool showDebugHint = false;
    public bool logEFailures = true;
    private CharacterController controller;
    private ENVPlayerVisualAnimator visualAnimator;
    private readonly Collider[] hits = new Collider[128];

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        visualAnimator = GetComponent<ENVPlayerVisualAnimator>();
    }

    private void Update()
    {
        if (controller != null && !controller.enabled) return;
        if (!EPressed()) return;
        ENVGem closest = FindNearest();
        if (closest == null)
        {
            if (logEFailures) LogWhyNoGem();
            return;
        }
        if (closest.TryActivate())
        {
            Debug.Log("[ENV Gem E] Activated " + closest.gemstoneName +
                " (index " + closest.sequenceIndex + "). Color reveal is handled separately by ENVMaterialColorRecovery or ENVColorRecoveryFeature.", closest);
            if (visualAnimator == null) visualAnimator = GetComponent<ENVPlayerVisualAnimator>();
            if (visualAnimator != null) visualAnimator.PlayTouch();
        }
        else if (logEFailures)
            Debug.LogWarning("[ENV Gem E] Gem is inside the Collider but activation was refused: " + closest.gemstoneName, closest);
    }

    public ENVGem FindNearest()
    {
        ENVWorldProgress world = ENVWorldProgress.Instance;
        if (world == null || !world.SetupReady) return null;
        int count = QueryPlayerOverlaps();
        ENVGem best = null;
        float nearest = float.PositiveInfinity;
        for (int i=0;i<count;i++)
        {
            Collider hit = hits[i];
            if (hit == null) continue;
            ENVGemTriggerZone zone = hit.GetComponent<ENVGemTriggerZone>();
            if (zone == null || !zone.isActiveAndEnabled || zone.Gem == null) continue;
            ENVGem gem = zone.Gem;
            if (!gem.isActiveAndEnabled || gem.Activated || !world.CanActivate(gem)) continue;
            // Used only for ranking multiple overlapping zones, not eligibility.
            float dist = (hit.ClosestPoint(transform.position) - transform.position).sqrMagnitude;
            if (dist < nearest) { nearest = dist; best = gem; }
        }
        return best;
    }

    private int QueryPlayerOverlaps()
    {
        // Physics gives exact BoxCollider/MeshCollider shape overlap, even when the
        // gem is above the player or the trigger is rotated.
        Physics.SyncTransforms();
        if (controller != null)
        {
            Vector3 up = transform.up;
            Vector3 center = transform.TransformPoint(controller.center);
            float scaleR = Mathf.Max(Mathf.Abs(transform.lossyScale.x), Mathf.Abs(transform.lossyScale.z));
            float radius = Mathf.Max(0.05f, controller.radius * scaleR);
            float height = Mathf.Max(radius*2,controller.height*Mathf.Abs(transform.lossyScale.y));
            float offset = height*0.5f - radius;
            return Physics.OverlapCapsuleNonAlloc(center-up*offset,center+up*offset,
                radius,hits,~0,QueryTriggerInteraction.Collide);
        }
        return Physics.OverlapSphereNonAlloc(transform.position,0.65f,hits,~0,QueryTriggerInteraction.Collide);
    }

    private void LogWhyNoGem()
    {
        var world = ENVWorldProgress.Instance;
        if (world == null) { Debug.LogWarning("[ENV Gem E] No ENVWorldProgress in scene.",this); return; }
        if (!world.SetupReady)
        {
            Debug.LogWarning("[ENV Gem E] Gem registration incomplete/invalid (" +
                world.RegisteredGemCount + "/" + world.expectedGemCount + ").",this);
            return;
        }
        ENVGem next = world.NextGem;
        int overlaps = QueryPlayerOverlaps();
        int zones = 0;
        for (int i=0;i<overlaps;i++) if (hits[i]!=null && hits[i].GetComponent<ENVGemTriggerZone>()!=null) zones++;
        Debug.LogWarning("[ENV Gem E] No eligible gem Trigger overlaps player. Next=" +
            (next != null ? next.gemstoneName : "none") + ", trigger zones touching player=" + zones +
            ". Adjust child BoxCollider (IsTrigger=true) in Scene view.",this);
    }

    private static bool EPressed()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.E);
#endif
    }
    private void OnGUI()
    {
        if (!showDebugHint || FindNearest() == null) return;
        GUI.Label(new Rect(Screen.width * .5f-75,Screen.height-120,220,28),"按 E 触碰宝石");
    }
}
