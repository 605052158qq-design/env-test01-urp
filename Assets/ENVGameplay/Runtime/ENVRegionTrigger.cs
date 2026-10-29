using UnityEngine;

/// <summary>L07: attach to empty region marker under the REAL map. Set BoxCollider IsTrigger, center/size.</summary>
[RequireComponent(typeof(BoxCollider))]
[DisallowMultipleComponent]
public sealed class ENVRegionTrigger : MonoBehaviour
{
    public string regionName = "微光码头";
    public Transform player;
    public bool once = true;
    public float showSeconds = 2.2f;
    private bool entered;
    private bool wasInside;
    private BoxCollider box;
    private void Awake() { box = GetComponent<BoxCollider>(); box.isTrigger = true; }
    private void Start()
    {
        if (player == null)
        {
            var cc = FindObjectOfType<CharacterController>();
            if (cc != null) player = cc.transform;
        }
    }
    private void Update()
    {
        if (player == null || box == null) return;
        // Explicit bounds test also works when no Rigidbody is present on this marker.
        bool inside = box.bounds.Contains(player.position + Vector3.up * 0.5f);
        if (inside && !wasInside) Fire();
        wasInside = inside;
    }
    private void OnTriggerEnter(Collider other)
    {
        if (player != null && (other.transform == player || other.transform.IsChildOf(player))) Fire();
    }
    private void Fire()
    {
        if (once && entered) return;
        entered = true;
        if (ENVRegionTitleUI.Instance != null) ENVRegionTitleUI.Instance.Show(regionName, showSeconds);
    }
}
