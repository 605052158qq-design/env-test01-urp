using System.Collections;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// L06: Add to the REAL boat root; it moves that very same art object into the dock.
/// A passenger may be attached to a deck anchor only while the boat travels.
/// The passenger's world position and rotation are preserved; input/physics are
/// frozen through ENVPlayerInteraction so the CharacterController cannot fight
/// the moving hierarchy.
/// </summary>
[DisallowMultipleComponent]
public sealed class ENVBoatArrival : MonoBehaviour
{
    [Header("Use the existing boat and a scene target marker")]
    public Transform boatDockTarget;

    [Header("Optional passenger freeze during the crossing")]
    [Tooltip("The existing player root. It is never moved or rotated by this script.")]
    public Transform passenger;
    [Tooltip("A child of the real boat, normally BoatPassengerAnchor, where the player is parented during travel.")]
    public Transform passengerAnchor;
    [Tooltip("Return the player to its original scene container after the boat has stopped.")]
    public bool detachPassengerAtDock = true;

    [Header("Sequence")]
    public bool playOnStart = true;
    [Min(0.05f)] public float approachSeconds = 5f;
    [Min(0f)] public float pauseAtDockSeconds = 0.5f;
    public AnimationCurve motionCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [Header("Event")]
    public UnityEvent onArrivalComplete;

    private bool playing;
    private ENVPlayerInteraction passengerInteraction;
    private Transform passengerOriginalParent;
    private bool passengerRideStarted;
    public bool IsPlaying { get { return playing; } }
    /// <summary>True only after the arrival cinematic has fully finished.</summary>
    public bool HasArrived { get; private set; }
    public bool CanPlay
    {
        get
        {
            return boatDockTarget != null;
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
        if (!CanPlay)
        {
            Debug.LogWarning("[ENV L06] Assign BoatDockTarget on the real boat before playing.", this);
            return;
        }
        HasArrived = false;
        StartCoroutine(RunArrival());
    }

    private IEnumerator RunArrival()
    {
        playing = true;
        BeginPassengerRide();
        yield return MoveBoatToDock();
        if (pauseAtDockSeconds > 0f) yield return new WaitForSeconds(pauseAtDockSeconds);
        EndPassengerRide();
        playing = false;
        HasArrived = true;
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

    private void BeginPassengerRide()
    {
        // Scene edits can be unsaved while testing. Resolve the intended existing
        // objects as a safe fallback instead of requiring a scene reload.
        if (passenger == null)
        {
            ENVPlayerInteraction foundPassenger = FindObjectOfType<ENVPlayerInteraction>(true);
            if (foundPassenger != null) passenger = foundPassenger.transform;
        }
        if (passengerAnchor == null)
        {
            Transform[] deckChildren = GetComponentsInChildren<Transform>(true);
            foreach (Transform child in deckChildren)
            {
                if (child.name == "BoatPassengerAnchor")
                {
                    passengerAnchor = child;
                    break;
                }
            }
        }
        if (passenger == null) return;

        passengerInteraction = passenger.GetComponent<ENVPlayerInteraction>();
        if (passengerInteraction != null && !passengerInteraction.BeginBoatRide())
        {
            Debug.LogWarning("[ENV L06] Passenger is not free to board; boat will travel without freezing the player.", passenger);
            passengerInteraction = null;
            return;
        }

        passengerOriginalParent = passenger.parent;
        if (passengerAnchor == null)
        {
            Debug.LogWarning("[ENV L06] Assign BoatPassengerAnchor to keep the passenger stable during boat travel.", this);
        }
        else if (passenger.parent != passengerAnchor)
        {
            // worldPositionStays avoids a visible teleport or any rotation change.
            passenger.SetParent(passengerAnchor, true);
        }
        passengerRideStarted = true;
    }

    private void EndPassengerRide()
    {
        if (!passengerRideStarted) return;
        if (passenger != null && detachPassengerAtDock)
            passenger.SetParent(passengerOriginalParent, true);
        if (passengerInteraction != null) passengerInteraction.EndBoatRide();
        passengerInteraction = null;
        passengerOriginalParent = null;
        passengerRideStarted = false;
    }

    private void OnDisable()
    {
        EndPassengerRide();
        playing = false;
        HasArrived = false;
    }

    private void OnDrawGizmos()
    {
        if (boatDockTarget != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, boatDockTarget.position);
            Gizmos.DrawWireSphere(boatDockTarget.position, 0.35f);
        }
    }
}
