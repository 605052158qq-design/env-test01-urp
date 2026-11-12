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
    private bool previousAnimatorEnabled;
    private bool previousVisualAnimatorEnabled;
    private ENVPlayerVisualAnimator visualAnimator;
    private ENVSeat seatedAt;
    private readonly ENVDirectClipPlayer clipPlayer = new ENVDirectClipPlayer();
    private Coroutine standRoutine;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        if (characterAnimator == null) characterAnimator = GetComponentInChildren<Animator>(true);
        movementScript = FindMotor();
        visualAnimator = GetComponent<ENVPlayerVisualAnimator>();
        CurrentMode = ActorMode.Free;
        if (GetComponent<ENVInteractionPromptUI>() == null)
            gameObject.AddComponent<ENVInteractionPromptUI>();
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
        bool sitDownEnded = visualAnimator != null ? visualAnimator.InteractionClipAtEnd : clipPlayer.IsAtEnd;
        if (sitDownEnded && !idleStarted)
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
        StopClipPlayback();
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
        StopClipPlayback();
        UnlockMovement();
        CurrentMode = ActorMode.Free;
    }

    private void PlayClip(AnimationClip clip, bool loop, bool hold)
    {
        if (visualAnimator != null)
        {
            visualAnimator.PlayInteractionClip(clip, loop, hold);
            return;
        }
        if (characterAnimator == null) return;
        clipPlayer.Play(characterAnimator, clip, loop, hold);
    }

    private void StopClipPlayback()
    {
        if (visualAnimator != null) visualAnimator.StopInteractionClip();
        clipPlayer.Stop();
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
            previousAnimatorEnabled = characterAnimator.enabled;
            // The normal visual animator samples Generic clips directly.  Only
            // the Animator-Playable fallback needs an enabled Animator.
            if (visualAnimator == null) characterAnimator.enabled = true;
            characterAnimator.applyRootMotion = false;
        }
        if (visualAnimator != null)
        {
            previousVisualAnimatorEnabled = visualAnimator.enabled;
            visualAnimator.enabled = true;
        }
    }

    private void UnlockMovement()
    {
        if (characterController != null) characterController.enabled = previousCcEnabled;
        if (movementScript != null) movementScript.enabled = previousMotorEnabled;
        if (characterAnimator != null)
        {
            characterAnimator.applyRootMotion = previousApplyRootMotion;
            characterAnimator.enabled = previousAnimatorEnabled;
        }
        if (visualAnimator != null) visualAnimator.enabled = previousVisualAnimatorEnabled;
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
        StopClipPlayback();
        if (CurrentMode != ActorMode.Free) UnlockMovement();
        CurrentMode = ActorMode.Free;
    }
}

/// <summary>One contextual uGUI prompt for the real seat and gem components near the player.</summary>
[DisallowMultipleComponent]
public sealed class ENVInteractionPromptUI : MonoBehaviour
{
    [Min(0.05f)] public float queryInterval = 0.12f;
    public string sitPrompt = "按 R 坐下";
    public string standPrompt = "按 R 起身";
    public string gemPrompt = "按 E 触碰宝石";

    private ENVPlayerInteraction playerInteraction;
    private ENVGemInteractor gemInteractor;
    private ENVStartScreen startScreen;
    private UnityEngine.UI.Text label;
    private float nextQuery;

    private void Awake()
    {
        playerInteraction = GetComponent<ENVPlayerInteraction>();
        gemInteractor = GetComponent<ENVGemInteractor>();
        CreateUi();
    }

    private void Update()
    {
        if (Time.unscaledTime < nextQuery) return;
        nextQuery = Time.unscaledTime + queryInterval;
        if (startScreen == null) startScreen = FindObjectOfType<ENVStartScreen>(true);
        if (startScreen != null && startScreen.IsMenuShowing) { SetVisible(false); return; }
        if (ENVCinematicScreen.ActiveSequences > 0) { SetVisible(false); return; }

        if (playerInteraction != null && playerInteraction.CurrentMode == ENVPlayerInteraction.ActorMode.Sitting)
        {
            SetText(standPrompt);
            return;
        }

        if (gemInteractor == null) gemInteractor = GetComponent<ENVGemInteractor>();
        if (gemInteractor != null && gemInteractor.FindNearest() != null)
        {
            SetText(gemPrompt);
            return;
        }

        SetText(ENVSeat.FindNearest(transform.position) != null ? sitPrompt : null);
    }

    private void CreateUi()
    {
        GameObject canvasObject = new GameObject("Interaction Prompt", typeof(RectTransform), typeof(Canvas), typeof(UnityEngine.UI.CanvasScaler), typeof(UnityEngine.UI.GraphicRaycaster));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 250;
        UnityEngine.UI.CanvasScaler scaler = canvasObject.GetComponent<UnityEngine.UI.CanvasScaler>();
        scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        // Supplied direction: retain the current font but keep this as a clean,
        // flat white prompt without the old black outline/drop-shadow treatment.
        GameObject textObject = new GameObject("Prompt", typeof(RectTransform), typeof(UnityEngine.UI.Text));
        textObject.transform.SetParent(canvasObject.transform, false);
        label = textObject.GetComponent<UnityEngine.UI.Text>();
        label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        label.fontSize = 30;
        label.fontStyle = FontStyle.Bold;
        label.alignment = TextAnchor.MiddleCenter;
        label.color = Color.white;
        label.raycastTarget = false;
        RectTransform rect = label.rectTransform;
        rect.anchorMin = new Vector2(0.5f, 0.16f);
        rect.anchorMax = new Vector2(0.5f, 0.16f);
        rect.sizeDelta = new Vector2(440f, 52f);
        SetVisible(false);
    }

    private void SetText(string value)
    {
        if (string.IsNullOrEmpty(value)) { SetVisible(false); return; }
        label.text = value;
        SetVisible(true);
    }

    private void SetVisible(bool visible)
    {
        if (label != null && label.gameObject.activeSelf != visible) label.gameObject.SetActive(visible);
    }
}
