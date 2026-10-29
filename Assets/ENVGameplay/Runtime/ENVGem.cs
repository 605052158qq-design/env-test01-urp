using UnityEngine;
using UnityEngine.Events;

/// <summary>L08/L09: put on actual gemstone root; visuals stay on the same object.</summary>
[DisallowMultipleComponent]
public sealed class ENVGem : MonoBehaviour
{
    public string gemstoneName = "宝石";
    [Min(0)] public int sequenceIndex;
    public Transform revealCenter;
    [Min(1f)] public float revealMaxRadius = 28f;
    [Min(0.1f)] public float revealSeconds = 2f;
    [Min(0.1f)] public float interactionRadius = 2.5f;
    public bool deactivateVisualOnActivate = false;
    public GameObject visualRoot;
    public UnityEvent onActivated;

    public bool Activated { get; private set; }
    public float RadiusNow { get; private set; }
    public Vector3 RevealPosition { get { return revealCenter != null ? revealCenter.position : transform.position; } }
    private float startTime;

    private void OnEnable() { EnsureRegistered(); }
    private void Start() { EnsureRegistered(); }
    private void OnDisable() { if (ENVWorldProgress.Instance != null) ENVWorldProgress.Instance.Unregister(this); }
    private void EnsureRegistered() { if (ENVWorldProgress.Instance != null) ENVWorldProgress.Instance.Register(this); }
    private void Update()
    {
        if (!Activated || RadiusNow >= revealMaxRadius) return;
        float t = Mathf.Clamp01((Time.time - startTime) / revealSeconds);
        RadiusNow = revealMaxRadius * Mathf.SmoothStep(0f, 1f, t);
    }
    public bool TryActivate()
    {
        if (Activated || (ENVWorldProgress.Instance != null && !ENVWorldProgress.Instance.CanActivate(this))) return false;
        Activated = true;
        startTime = Time.time;
        RadiusNow = 0f;
        if (onActivated != null) onActivated.Invoke();
        if (deactivateVisualOnActivate && visualRoot != null) visualRoot.SetActive(false);
        if (ENVWorldProgress.Instance != null) ENVWorldProgress.Instance.NotifyActivated(this);
        return true;
    }
    public void ResetGem()
    {
        Activated = false;
        RadiusNow = 0;
        if (visualRoot != null) visualRoot.SetActive(true);
    }
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, interactionRadius);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(RevealPosition, revealMaxRadius);
    }
}
