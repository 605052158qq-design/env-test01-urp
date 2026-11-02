using UnityEngine;

/// <summary>Add to a dedicated CHILD of the real gem. This object's BoxCollider defines the E-key interaction volume.</summary>
[RequireComponent(typeof(BoxCollider))]
[DisallowMultipleComponent]
public sealed class ENVGemTriggerZone : MonoBehaviour
{
    public ENVGem gem;
    public ENVGem Gem { get { return gem != null ? gem : GetComponentInParent<ENVGem>(); } }
    private void Reset()
    {
        BoxCollider box = GetComponent<BoxCollider>();
        box.isTrigger = true;
        if (gem == null) gem = GetComponentInParent<ENVGem>();
    }
    private void OnValidate()
    {
        BoxCollider box = GetComponent<BoxCollider>();
        if (box != null) box.isTrigger = true;
    }
}
