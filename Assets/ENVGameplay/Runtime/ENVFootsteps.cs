using UnityEngine;

/// <summary>L15: on existing CharacterController root. Footsteps only if grounded and moving.</summary>
[RequireComponent(typeof(CharacterController), typeof(AudioSource))]
[DisallowMultipleComponent]
public sealed class ENVFootsteps : MonoBehaviour
{
    public AudioClip[] grassSteps;
    public AudioClip[] snowSteps;
    public LayerMask snowGroundLayers;
    [Min(0.1f)] public float walkStepInterval = 0.48f;
    [Min(0.1f)] public float runStepInterval = 0.3f;
    [Min(0.1f)] public float runSpeedThreshold = 4.5f;
    [Range(0f, 1f)] public float volume = 0.55f;
    private CharacterController controller;
    private AudioSource audioSource;
    private float countdown;
    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        audioSource = GetComponent<AudioSource>();
        audioSource.spatialBlend = 0f; audioSource.playOnAwake = false;
    }
    private void Update()
    {
        if (!controller.enabled || !controller.isGrounded) { countdown = 0f; return; }
        Vector3 v = controller.velocity; v.y = 0f;
        float speed = v.magnitude;
        if (speed < 0.3f) { countdown = 0f; return; }
        countdown -= Time.deltaTime;
        if (countdown > 0f) return;
        countdown = speed > runSpeedThreshold ? runStepInterval : walkStepInterval;
        bool snow = Physics.Raycast(transform.position + Vector3.up * 0.3f, Vector3.down,
            out RaycastHit hit, 2f, snowGroundLayers, QueryTriggerInteraction.Ignore);
        AudioClip[] clips = snow ? snowSteps : grassSteps;
        if (clips == null || clips.Length == 0) return;
        var c = clips[Random.Range(0, clips.Length)];
        if (c != null) audioSource.PlayOneShot(c, volume);
    }
}
