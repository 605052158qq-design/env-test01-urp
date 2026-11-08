using UnityEngine;

/// <summary>
/// Drives the supplied Generic FBX clips directly on the visible model root.
/// The model and motion FBXs share Root/Bip001/... curves but have different
/// imported Avatar roots, so direct clip sampling is both simpler and reliable.
/// </summary>
[DisallowMultipleComponent]
public sealed class ENVPlayerVisualAnimator : MonoBehaviour
{
    [Header("Kept only for other optional systems that expect an Animator")]
    public Animator animator;
    public CharacterController characterController;
    [Header("Original FBX clips; remain independently previewable in Inspector")]
    public Transform animationRoot;
    public AnimationClip idleClip;
    public AnimationClip walkClip;
    public AnimationClip touchClip;
    [Min(0f)] public float walkThreshold = 0.08f;
    [Header("Visual grounding")]
    [Tooltip("Aligns the imported model's lowest renderer point to the CharacterController base. Keeps collision and movement dimensions unchanged.")]
    public bool alignVisualToControllerBase = true;
    [Tooltip("Small visual-only height adjustment after automatic grounding, in metres.")]
    public float visualGroundClearance = 0f;

    private AnimationClip activeClip;
    private float clipStartedAt;
    private bool touchPlaying;

    private void Awake()
    {
        if (animator == null) animator = GetComponentInChildren<Animator>(true);
        if (characterController == null) characterController = GetComponent<CharacterController>();
        if (animationRoot == null && animator != null) animationRoot = animator.transform;
        if (animator != null) animator.enabled = false;
        AlignVisualToControllerBase();
        SwitchTo(idleClip);
    }

    private void Update()
    {
        if (animationRoot == null) return;
        if (touchPlaying)
        {
            if (touchClip != null && Time.time - clipStartedAt < touchClip.length) return;
            touchPlaying = false;
        }
        Vector3 velocity = characterController != null ? characterController.velocity : Vector3.zero;
        velocity.y = 0f;
        SwitchTo(velocity.magnitude > walkThreshold ? walkClip : idleClip);
    }

    private void LateUpdate()
    {
        if (animationRoot == null || activeClip == null || activeClip.length <= 0f) return;

        // Do not let animation root motion move the CharacterController's visual
        // offset; only the skeleton below it is animated.
        Vector3 localPosition = animationRoot.localPosition;
        Quaternion localRotation = animationRoot.localRotation;
        Vector3 localScale = animationRoot.localScale;
        float elapsed = Mathf.Max(0f, Time.time - clipStartedAt);
        float sampleTime = touchPlaying ? Mathf.Min(elapsed, activeClip.length) : elapsed % activeClip.length;
        activeClip.SampleAnimation(animationRoot.gameObject, sampleTime);
        animationRoot.localPosition = localPosition;
        animationRoot.localRotation = localRotation;
        animationRoot.localScale = localScale;
    }

    public void PlayTouch()
    {
        if (touchClip == null) return;
        touchPlaying = true;
        SwitchTo(touchClip, true);
    }

    private void AlignVisualToControllerBase()
    {
        if (!alignVisualToControllerBase || animationRoot == null || characterController == null) return;

        Renderer[] renderers = animationRoot.GetComponentsInChildren<Renderer>(true);
        bool foundRenderer = false;
        float visualBottom = 0f;
        foreach (Renderer renderer in renderers)
        {
            if (renderer == null || renderer is ParticleSystemRenderer) continue;
            float bottom = renderer.bounds.min.y;
            if (!foundRenderer || bottom < visualBottom)
            {
                visualBottom = bottom;
                foundRenderer = true;
            }
        }
        if (!foundRenderer) return;

        float controllerBottom = transform.position.y + characterController.center.y - characterController.height * 0.5f;
        float offset = controllerBottom + visualGroundClearance - visualBottom;
        if (Mathf.Abs(offset) > 0.0001f)
            animationRoot.position += Vector3.up * offset;
    }

    private void SwitchTo(AnimationClip next, bool restart = false)
    {
        if (next == null) return;
        if (!restart && activeClip == next) return;
        activeClip = next;
        clipStartedAt = Time.time;
    }
}
