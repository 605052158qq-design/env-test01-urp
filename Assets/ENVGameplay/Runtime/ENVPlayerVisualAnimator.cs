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
    [Tooltip("Supplied jump clip. It is sampled once while the controller is airborne.")]
    public AnimationClip jumpClip;
    public AnimationClip touchClip;
    [Min(0f)] public float walkThreshold = 0.08f;

    private AnimationClip activeClip;
    private float clipStartedAt;
    private bool touchPlaying;
    private bool jumpPlaying;
    private bool interactionPlaying;
    private bool interactionLoops;
    private bool interactionHoldsLastFrame;

    public bool InteractionClipAtEnd
    {
        get
        {
            return interactionPlaying && !interactionLoops && activeClip != null &&
                   Time.time - clipStartedAt >= activeClip.length;
        }
    }

    private void Awake()
    {
        if (animator == null) animator = GetComponentInChildren<Animator>(true);
        if (characterController == null) characterController = GetComponent<CharacterController>();
        if (animationRoot == null && animator != null) animationRoot = animator.transform;
        if (animator != null) animator.enabled = false;
        SwitchTo(idleClip);
    }

    private void Update()
    {
        if (animationRoot == null) return;
        if (interactionPlaying)
        {
            if (interactionLoops || interactionHoldsLastFrame ||
                (activeClip != null && Time.time - clipStartedAt < activeClip.length)) return;
            interactionPlaying = false;
        }
        if (touchPlaying)
        {
            if (touchClip != null && Time.time - clipStartedAt < touchClip.length) return;
            touchPlaying = false;
        }
        if (jumpPlaying)
        {
            if (jumpClip != null && Time.time - clipStartedAt < jumpClip.length) return;
            jumpPlaying = false;
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
        bool oneShot = touchPlaying || jumpPlaying || (interactionPlaying && !interactionLoops);
        float sampleTime = oneShot ? Mathf.Min(elapsed, activeClip.length) : elapsed % activeClip.length;
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

    public void PlayJump()
    {
        if (jumpClip == null) return;
        touchPlaying = false;
        jumpPlaying = true;
        SwitchTo(jumpClip, true);
    }

    /// <summary>
    /// Uses the exact same Generic-FBX sampling path as idle/walk.  This is
    /// important for interaction clips because their bone bindings target the
    /// model hierarchy rather than an Animator Controller state.
    /// </summary>
    public void PlayInteractionClip(AnimationClip clip, bool loop, bool holdLastFrame)
    {
        if (clip == null) return;
        touchPlaying = false;
        jumpPlaying = false;
        interactionPlaying = true;
        interactionLoops = loop;
        interactionHoldsLastFrame = holdLastFrame;
        SwitchTo(clip, true);
    }

    public void StopInteractionClip()
    {
        if (!interactionPlaying) return;
        interactionPlaying = false;
        interactionLoops = false;
        interactionHoldsLastFrame = false;
        SwitchTo(idleClip, true);
    }

    private void SwitchTo(AnimationClip next, bool restart = false)
    {
        if (next == null) return;
        if (!restart && activeClip == next) return;
        activeClip = next;
        clipStartedAt = Time.time;
    }
}
