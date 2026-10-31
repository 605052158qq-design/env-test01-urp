using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// L07: attach to an empty marker under the real map. The BoxCollider defines the
/// gameplay region; visuals remain on the artist's map. Optionally bind the
/// gemstone that restores this region so all six regions can use the same flow.
/// </summary>
[RequireComponent(typeof(BoxCollider))]
[DisallowMultipleComponent]
public sealed class ENVRegionTrigger : MonoBehaviour
{
    public string regionName = "微光码头";
    [Header("Optional restoration rule")]
    [Tooltip("When this real gemstone is activated, this region becomes restored.")]
    public ENVGem restorationGem;
    [Tooltip("Additional gemstones required by this region. Use this for the two-gem map2 recovery.")]
    public ENVGem[] additionalRestorationGems;
    [Tooltip("Shown on re-entry and immediately when the bound gemstone restores this region.")]
    public string restoredRegionName = "微光码头 · 已恢复";
    public UnityEvent onRestored;

    public Transform player;
    public bool once = true;
    public float showSeconds = 2.2f;
    public bool IsRestored { get; private set; }
    private bool entered;
    private bool wasInside;
    private BoxCollider box;
    private bool subscribed;

    private void Awake()
    {
        box = GetComponent<BoxCollider>();
        box.isTrigger = true;
    }

    private void Start()
    {
        if (player == null)
        {
            var cc = FindObjectOfType<CharacterController>();
            if (cc != null) player = cc.transform;
        }
        SubscribeToGems();
        EvaluateRestoration();
    }

    private void OnEnable()
    {
        SubscribeToGems();
    }

    private void OnDisable()
    {
        if (!subscribed) return;
        foreach (ENVGem gem in RestorationGems()) gem.onActivated.RemoveListener(OnRestorationGemActivated);
        subscribed = false;
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
        ShowTitle(IsRestored ? RestoredTitle : regionName);
    }

    private void SubscribeToGems()
    {
        if (subscribed) return;
        ENVGem[] gems = RestorationGems();
        if (gems.Length == 0) return;
        foreach (ENVGem gem in gems) gem.onActivated.AddListener(OnRestorationGemActivated);
        subscribed = true;
    }

    private void OnRestorationGemActivated()
    {
        EvaluateRestoration();
    }

    private void EvaluateRestoration()
    {
        ENVGem[] gems = RestorationGems();
        if (gems.Length == 0) return;
        foreach (ENVGem gem in gems)
            if (gem == null || !gem.Activated) return;
        Restore();
    }

    private ENVGem[] RestorationGems()
    {
        var gems = new System.Collections.Generic.List<ENVGem>();
        if (restorationGem != null) gems.Add(restorationGem);
        if (additionalRestorationGems != null)
            foreach (ENVGem gem in additionalRestorationGems)
                if (gem != null && !gems.Contains(gem)) gems.Add(gem);
        return gems.ToArray();
    }

    private void Restore()
    {
        if (IsRestored) return;
        IsRestored = true;
        // `once` means one title per region state. Permit the restored title on a later re-entry.
        entered = false;
        if (onRestored != null) onRestored.Invoke();
        ShowTitle(RestoredTitle);
    }

    private string RestoredTitle
    {
        get { return string.IsNullOrWhiteSpace(restoredRegionName) ? regionName + " · 已恢复" : restoredRegionName; }
    }

    private void ShowTitle(string title)
    {
        if (ENVRegionTitleUI.Instance != null) ENVRegionTitleUI.Instance.Show(title, showSeconds);
    }
}
