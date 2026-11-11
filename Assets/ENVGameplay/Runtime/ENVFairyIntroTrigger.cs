using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Ancient-tree encounter trigger. The former fairy-video/follow sequence was
/// deliberately replaced by the supplied NPC dialogue presentation.
/// </summary>
[RequireComponent(typeof(BoxCollider))]
[DisallowMultipleComponent]
public sealed class ENVFairyIntroTrigger : MonoBehaviour
{
    [Header("Trigger")]
    public Transform player;

    [Header("NPC dialogue art")]
    public Sprite dialogueBackground;
    public Sprite dialogueFrame;
    public Sprite travelerPortrait;
    public Sprite fairyPortrait;

    [Header("Fairy follow after dialogue")]
    [Tooltip("The real in-scene fairy visual, not the dialogue UI portrait.")]
    public ENVFairyFollower fairyFollower;
    public Vector3 fairyFollowOffset = new Vector3(0f, 1.15f, -1.6f);

    [Header("Event")]
    public UnityEvent onDialogueFinished;

    private readonly List<Behaviour> pausedBehaviours = new List<Behaviour>();
    private BoxCollider zone;
    private bool fired;
    private float timeScaleBeforeDialogue = 1f;
    public bool FairyAcquired { get; private set; }

    private void Awake()
    {
        zone = GetComponent<BoxCollider>();
        zone.isTrigger = true;
    }

    private void Start()
    {
        if (player == null)
        {
            CharacterController controller = FindObjectOfType<CharacterController>();
            if (controller != null) player = controller.transform;
        }
    }

    private void Update()
    {
        if (!fired && player != null && zone.bounds.Contains(player.position + Vector3.up * 0.5f))
            Activate();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (player != null && (other.transform == player || other.transform.IsChildOf(player)))
            Activate();
    }

    /// <summary>Allows the existing debug button to validate the encounter branch.</summary>
    public void DebugActivate() { Activate(); }

    private void Activate()
    {
        if (fired) return;
        fired = true;
        PauseGameplay();
        ENVNpcDialogueUI.Show(this);
    }

    internal void FinishDialogue()
    {
        FairyAcquired = StartFairyFollow();
        ResumeGameplay();
        if (onDialogueFinished != null) onDialogueFinished.Invoke();
    }

    private bool StartFairyFollow()
    {
        if (fairyFollower == null) fairyFollower = FindObjectOfType<ENVFairyFollower>(true);
        if (fairyFollower == null)
        {
            foreach (GameObject candidate in Resources.FindObjectsOfTypeAll<GameObject>())
            {
                if (candidate.name != "ENV_FairyPlaceholder_ReplaceWithArt" ||
                    !candidate.scene.IsValid() || !candidate.scene.isLoaded) continue;
                fairyFollower = candidate.GetComponent<ENVFairyFollower>();
                if (fairyFollower == null) fairyFollower = candidate.AddComponent<ENVFairyFollower>();
                break;
            }
        }

        if (fairyFollower == null)
        {
            Debug.LogWarning("[ENV NPC Dialogue] No in-scene fairy visual was found for the post-dialogue follow.", this);
            return false;
        }

        fairyFollower.player = player;
        fairyFollower.relativeOffset = fairyFollowOffset;
        fairyFollower.BeginFollow();
        return true;
    }

    private void PauseGameplay()
    {
        timeScaleBeforeDialogue = Time.timeScale;
        Time.timeScale = 0f;
        pausedBehaviours.Clear();
        foreach (Behaviour behaviour in FindObjectsOfType<Behaviour>(true))
        {
            if (!behaviour.enabled) continue;
            if (behaviour is ENVPrototypeThirdPersonController ||
                behaviour is ENVPrototypeOrbitCamera ||
                behaviour is ENVPlayerInteraction ||
                behaviour is ENVGemInteractor)
            {
                pausedBehaviours.Add(behaviour);
                behaviour.enabled = false;
            }
        }
    }

    private void ResumeGameplay()
    {
        Time.timeScale = timeScaleBeforeDialogue;
        foreach (Behaviour behaviour in pausedBehaviours)
            if (behaviour != null) behaviour.enabled = true;
        pausedBehaviours.Clear();
    }

    private void OnDisable()
    {
        if (fired && ENVNpcDialogueUI.IsShowingFor(this))
            ENVNpcDialogueUI.CloseWithoutCompleting();
        ResumeGameplay();
    }
}
