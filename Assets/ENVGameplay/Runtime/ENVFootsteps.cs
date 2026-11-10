using UnityEngine;

/// <summary>L15: on existing CharacterController root. Footsteps only if grounded and moving.</summary>
[RequireComponent(typeof(CharacterController))]
[DisallowMultipleComponent]
public sealed class ENVFootsteps : MonoBehaviour
{
    public AudioClip[] grassSteps;
    public AudioClip[] snowSteps;
    public LayerMask snowGroundLayers;
    [Min(0.1f)] public float walkStepInterval = 0.48f;
    [Min(0.1f)] public float runStepInterval = 0.3f;
    [Min(0.1f)] public float runSpeedThreshold = 4.5f;
    [Min(0f)] public float minimumMovementSpeed = 0.3f;
    [Range(0f, 1f)] public float volume = 0.55f;
    private CharacterController controller;
    private AudioSource audioSource;
    private float countdown;
    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.spatialBlend = 0f; audioSource.playOnAwake = false;

        // The customer supplied one walking clip for V1. Use it for both
        // surfaces until distinct snow steps are provided, while retaining the
        // Inspector arrays for a future per-surface replacement.
        ENVAudioBank bank = FindObjectOfType<ENVAudioBank>(true);
        if (bank != null && bank.B_footstep != null)
        {
            if (grassSteps == null || grassSteps.Length == 0) grassSteps = new[] { bank.B_footstep };
            if (snowSteps == null || snowSteps.Length == 0) snowSteps = new[] { bank.B_footstep };
        }
    }
    private void Update()
    {
        if (!controller.enabled || !controller.isGrounded) { StopFootsteps(); return; }
        Vector3 v = controller.velocity; v.y = 0f;
        float speed = v.magnitude;
        if (speed < minimumMovementSpeed) { StopFootsteps(); return; }
        countdown -= Time.deltaTime;
        if (countdown > 0f) return;
        countdown = speed > runSpeedThreshold ? runStepInterval : walkStepInterval;
        bool snow = Physics.Raycast(transform.position + Vector3.up * 0.3f, Vector3.down,
            out RaycastHit hit, 2f, snowGroundLayers, QueryTriggerInteraction.Ignore);
        AudioClip[] clips = snow ? snowSteps : grassSteps;
        if (clips == null || clips.Length == 0) return;
        var c = clips[Random.Range(0, clips.Length)];
        if (c == null) return;

        // One dedicated source prevents several footstep one-shots from piling up
        // when the player changes direction or stops between two step intervals.
        // PlayOneShot intentionally cannot be cancelled, which was the cause of
        // footsteps continuing after movement had already stopped.
        audioSource.clip = c;
        audioSource.volume = volume;
        audioSource.Play();
    }

    private void StopFootsteps()
    {
        countdown = 0f;
        if (audioSource != null && audioSource.isPlaying) audioSource.Stop();
    }

    private void OnDisable() { StopFootsteps(); }
}
