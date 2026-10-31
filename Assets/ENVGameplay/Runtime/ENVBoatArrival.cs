using System.Collections;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// L06: Add to the REAL boat root; it moves that very same art object into the dock.
/// No prototype boat prefab or separate proxy renderer. Use existing player & camera.
/// </summary>
[DisallowMultipleComponent]
public sealed class ENVBoatArrival : MonoBehaviour
{
    [Header("Use the existing objects / only empty positional markers are created")]
    public ENVPlayerInteraction player;
    public Transform playerOnBoat;
    public Transform boatDockTarget;
    public Transform playerLandingTarget;

    [Header("Sequence")]
    public bool playOnStart = true;
    [Min(0.05f)] public float approachSeconds = 5f;
    [Min(0f)] public float pauseAtDockSeconds = 0.5f;
    [Min(0.05f)] public float walkOffSeconds = 1.2f;
    public AnimationCurve motionCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [Tooltip("V1 keeps the player on the dock. Enable only when final boarding animation/anchors are authored.")]
    public bool movePlayerWithBoat = false;
    [Tooltip("During arrival, show the real moving boat instead of leaving the camera at the dock.")]
    public bool focusCameraOnBoat = true;
    [Min(1f)] public float arrivalCameraDistance = 10f;
    [Header("Event")]
    public UnityEvent onArrivalComplete;

    private bool playing;
    private Transform originalPlayerParent;
    private ENVPrototypeOrbitCamera orbitCamera;
    private Transform originalCameraTarget;
    private float originalCameraDistance;
    public bool IsPlaying { get { return playing; } }
    public bool CanPlay
    {
        get
        {
            return boatDockTarget != null &&
                   (!movePlayerWithBoat || (player != null && playerOnBoat != null && playerLandingTarget != null));
        }
    }

    private void Start()
    {
        if (playOnStart) StartCoroutine(DelayedStart());
    }

    private IEnumerator DelayedStart()
    {
        yield return null; // Let the normal player controller initialize first.
        PlayArrival();
    }

    [ContextMenu("Play Boat Arrival")]
    public void PlayArrival()
    {
        if (playing) return;
        if (player == null) player = FindObjectOfType<ENVPlayerInteraction>();
        if (!CanPlay)
        {
            Debug.LogWarning("[ENV L06] Select a real boat and assign the existing player and all three markers.", this);
            return;
        }
        StartCoroutine(RunArrival());
    }

    private IEnumerator RunArrival()
    {
        playing = true;
        BeginArrivalCamera();
        if (!movePlayerWithBoat)
        {
            yield return MoveBoatToDock();
            EndArrivalCamera();
            playing = false;
            if (onArrivalComplete != null) onArrivalComplete.Invoke();
            yield break;
        }

        if (player == null || !player.BeginBoatRide())
        {
            EndArrivalCamera();
            playing = false;
            yield break;
        }
        Transform actor = player.transform;
        Transform oldParent = actor.parent;
        originalPlayerParent = oldParent;

        // CharacterController and movement script are disabled by BeginBoatRide().
        actor.SetParent(transform, true);
        actor.SetPositionAndRotation(playerOnBoat.position, playerOnBoat.rotation);

        yield return MoveBoatToDock();
        if (pauseAtDockSeconds > 0f) yield return new WaitForSeconds(pauseAtDockSeconds);

        actor.SetParent(oldParent, true);
        player.StartDisembarking();
        Vector3 from = actor.position;
        Quaternion rotationFrom = actor.rotation;
        Vector3 to = playerLandingTarget.position;
        Quaternion rotationTo = playerLandingTarget.rotation;
        float elapsed = 0f;
        while (elapsed < walkOffSeconds)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / walkOffSeconds);
            actor.SetPositionAndRotation(Vector3.Lerp(from, to, t), Quaternion.Slerp(rotationFrom, rotationTo, t));
            yield return null;
        }
        actor.SetPositionAndRotation(to, rotationTo);
        player.EndBoatRide();
        originalPlayerParent = null;
        EndArrivalCamera();
        playing = false;
        if (onArrivalComplete != null) onArrivalComplete.Invoke();
    }

    private IEnumerator MoveBoatToDock()
    {
        Vector3 startPosition = transform.position;
        // Dock markers provide the horizontal route only. A boat never rises or sinks during V1 arrival.
        Vector3 endPosition = new Vector3(boatDockTarget.position.x, startPosition.y, boatDockTarget.position.z);
        Quaternion startRotation = transform.rotation;
        float elapsed = 0f;
        while (elapsed < approachSeconds)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / approachSeconds);
            float eased = motionCurve == null ? t : motionCurve.Evaluate(t);
            transform.SetPositionAndRotation(Vector3.LerpUnclamped(startPosition, endPosition, eased), startRotation);
            yield return null;
        }
        transform.SetPositionAndRotation(endPosition, startRotation);
    }

    private void BeginArrivalCamera()
    {
        if (!focusCameraOnBoat) return;
        orbitCamera = FindObjectOfType<ENVPrototypeOrbitCamera>(true);
        if (orbitCamera == null) return;
        originalCameraTarget = orbitCamera.target;
        originalCameraDistance = orbitCamera.distance;
        orbitCamera.target = transform;
        orbitCamera.distance = arrivalCameraDistance;
        orbitCamera.enabled = true;
    }

    private void EndArrivalCamera()
    {
        if (orbitCamera == null) return;
        orbitCamera.target = originalCameraTarget;
        orbitCamera.distance = originalCameraDistance;
        orbitCamera = null;
    }

    private void OnDisable()
    {
        // If the boat is disabled mid-cutscene, do not leave movement permanently locked.
        if (!playing || player == null) return;
        if (movePlayerWithBoat)
        {
            player.transform.SetParent(originalPlayerParent, true);
            player.EndBoatRide();
        }
        originalPlayerParent = null;
        EndArrivalCamera();
        playing = false;
    }

    private void OnDrawGizmosSelected()
    {
        if (boatDockTarget != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, boatDockTarget.position);
            Gizmos.DrawWireSphere(boatDockTarget.position, 0.35f);
        }
        if (playerLandingTarget != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(playerLandingTarget.position, 0.3f);
        }
    }
}
