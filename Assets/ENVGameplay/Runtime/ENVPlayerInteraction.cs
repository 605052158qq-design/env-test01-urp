using System.Collections;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// L05 + L06. Add to the EXISTING player root that already has CharacterController.
/// Existing model, Animator and third-person camera are reused.
/// The optional clips can be replaced in Inspector when final art is ready.
/// </summary>
[DisallowMultipleComponent]
public sealed class ENVPlayerInteraction : MonoBehaviour
{
    [Header("The Animator already used by your one character model")]
    public Animator characterAnimator;

    [Header("L05 / Drag in final AnimationClips, no Animator Controller edits")]
    public AnimationClip sitDownClip;
    public AnimationClip seatedIdleClip;
    public AnimationClip standUpClip;

    [Header("L06 / Optional existing character clips")]
    public AnimationClip boatRideClip;
    public AnimationClip disembarkClip;

    public enum ActorMode { Free, Sitting, StandingUp, Boat }
    public ActorMode CurrentMode { get; private set; }

    private CharacterController characterController;
    private Behaviour movementScript;
    private bool previousCcEnabled;
    private bool previousMotorEnabled;
    private bool previousApplyRootMotion;
    private ENVSeat seatedAt;
    private readonly ENVDirectClipPlayer clipPlayer = new ENVDirectClipPlayer();
    private Coroutine standRoutine;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        if (characterAnimator == null) characterAnimator = GetComponentInChildren<Animator>(true);
        movementScript = FindMotor();
        CurrentMode = ActorMode.Free;
    }

    private void Update()
    {
        clipPlayer.Tick(Time.deltaTime);
        if (!RPressed()) return;
        if (CurrentMode == ActorMode.Free)
        {
            ENVSeat seat = ENVSeat.FindNearest(transform.position);
            if (seat != null) Sit(seat);
        }
        else if (CurrentMode == ActorMode.Sitting)
        {
            if (standRoutine == null) standRoutine = StartCoroutine(StandUp());
        }
    }

    private void LateUpdate()
    {
        if (CurrentMode != ActorMode.Sitting || seatedAt == null || seatedIdleClip == null) return;
        // When the sit-down one-shot reaches its end, switch to the supplied seated idle.
        if (clipPlayer.IsAtEnd && !idleStarted)
        {
            idleStarted = true;
            PlayClip(seatedIdleClip, true, true);
        }
    }

    private bool idleStarted;

    public void Sit(ENVSeat seat)
    {
        if (CurrentMode != ActorMode.Free || seat == null) return;
        seatedAt = seat;
        idleStarted = false;
        LockMovement();
        CurrentMode = ActorMode.Sitting;
        transform.SetPositionAndRotation(seat.SitTarget.position, seat.SitTarget.rotation);
        PlayClip(sitDownClip != null ? sitDownClip : seatedIdleClip, sitDownClip == null && seatedIdleClip != null, true);
    }

    /// <summary>V1 GUI-only shortcut; it bypasses proximity but calls normal sit/stand logic.</summary>
    public void DebugToggleSeat()
    {
        if (CurrentMode == ActorMode.Sitting)
        {
            if (standRoutine == null) standRoutine = StartCoroutine(StandUp());
            return;
        }
        if (CurrentMode != ActorMode.Free) return;

        ENVSeat seat = FindObjectOfType<ENVSeat>(true);
        if (seat != null) Sit(seat);
    }

    private IEnumerator StandUp()
    {
        CurrentMode = ActorMode.StandingUp;
        PlayClip(standUpClip, false, true);
        if (standUpClip != null) yield return new WaitForSeconds(Mathf.Max(0.05f, standUpClip.length));
        if (seatedAt != null)
            transform.SetPositionAndRotation(seatedAt.ExitTarget.position, seatedAt.ExitTarget.rotation);
        clipPlayer.Stop();
        seatedAt = null;
        idleStarted = false;
        UnlockMovement();
        CurrentMode = ActorMode.Free;
        standRoutine = null;
    }

    /// <summary>Called by the boat script; use the same root and same Animator.</summary>
    public bool BeginBoatRide()
    {
        if (CurrentMode != ActorMode.Free) return false;
        LockMovement();
        CurrentMode = ActorMode.Boat;
        PlayClip(boatRideClip, true, true);
        return true;
    }

    public void StartDisembarking()
    {
        if (CurrentMode == ActorMode.Boat) PlayClip(disembarkClip, false, true);
    }

    public void EndBoatRide()
    {
        if (CurrentMode != ActorMode.Boat) return;
        clipPlayer.Stop();
        UnlockMovement();
        CurrentMode = ActorMode.Free;
    }

    private void PlayClip(AnimationClip clip, bool loop, bool hold)
    {
        if (characterAnimator == null) return;
        clipPlayer.Play(characterAnimator, clip, loop, hold);
    }

    private void LockMovement()
    {
        if (characterController == null) characterController = GetComponent<CharacterController>();
        if (movementScript == null) movementScript = FindMotor();
        if (characterController != null)
        {
            previousCcEnabled = characterController.enabled;
            characterController.enabled = false;
        }
        if (movementScript != null)
        {
            previousMotorEnabled = movementScript.enabled;
            movementScript.enabled = false;
        }
        if (characterAnimator != null)
        {
            previousApplyRootMotion = characterAnimator.applyRootMotion;
            characterAnimator.applyRootMotion = false;
        }
    }

    private void UnlockMovement()
    {
        if (characterController != null) characterController.enabled = previousCcEnabled;
        if (movementScript != null) movementScript.enabled = previousMotorEnabled;
        if (characterAnimator != null) characterAnimator.applyRootMotion = previousApplyRootMotion;
    }

    private Behaviour FindMotor()
    {
        foreach (MonoBehaviour component in GetComponents<MonoBehaviour>())
        {
            if (component == null) continue;
            var type = component.GetType();
            string name = type.Name;
            if (name == "ENVPrototypeThirdPersonController" ||
                (name == "ThirdPersonController" && type.Namespace == "StarterAssets"))
                return component;
        }
        return null;
    }

    private static bool RPressed()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.R);
#endif
    }

    private void OnDisable()
    {
        if (standRoutine != null) { StopCoroutine(standRoutine); standRoutine = null; }
        clipPlayer.Stop();
        if (CurrentMode != ActorMode.Free) UnlockMovement();
        CurrentMode = ActorMode.Free;
    }
}
