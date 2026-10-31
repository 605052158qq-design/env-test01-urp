using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>Attach to the existing player, E activates the closest gemstone.</summary>
[DisallowMultipleComponent]
public sealed class ENVGemInteractor : MonoBehaviour
{
    [Min(0.5f)] public float maxDistance = 3f;
    public bool showDebugHint = false;
    private CharacterController controller;
    private ENVPlayerVisualAnimator visualAnimator;
    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        visualAnimator = GetComponent<ENVPlayerVisualAnimator>();
    }
    private void Update()
    {
        if (controller != null && !controller.enabled) return; // sitting/boat
        if (!EPressed()) return;
        ENVGem closest = FindNearest();
        if (closest != null && closest.TryActivate())
        {
            // The supplied touch clip is a one-shot acknowledgement for the real
            // interaction path. Movement continues to drive Idle/Walk normally.
            if (visualAnimator == null) visualAnimator = GetComponent<ENVPlayerVisualAnimator>();
            if (visualAnimator != null) visualAnimator.PlayTouch();
        }
    }
    public ENVGem FindNearest()
    {
        var world = ENVWorldProgress.Instance;
        if (world == null) return null;
        ENVGem result = null;
        float nearest = maxDistance * maxDistance;
        foreach (var g in world.Gems)
        {
            if (g == null || !g.isActiveAndEnabled || g.Activated || !world.CanActivate(g)) continue;
            float d = (g.transform.position - transform.position).sqrMagnitude;
            float allowed = Mathf.Min(maxDistance, g.interactionRadius);
            if (d < nearest && d <= allowed * allowed) { result = g; nearest = d; }
        }
        return result;
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
        GUI.Label(new Rect(Screen.width * 0.5f - 75, Screen.height - 120, 200, 28), "按 E 触碰宝石");
    }
}
