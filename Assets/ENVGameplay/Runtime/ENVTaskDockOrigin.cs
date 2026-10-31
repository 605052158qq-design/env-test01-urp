using UnityEngine;

/// <summary>
/// Stores an artist-authored task object's original transform before it is moved
/// into the temporary dock test layout. It has no runtime behaviour.
/// </summary>
[DisallowMultipleComponent]
public sealed class ENVTaskDockOrigin : MonoBehaviour
{
    public Transform originalParent;
    public Vector3 originalLocalPosition;
    public Quaternion originalLocalRotation;
    public Vector3 originalLocalScale;
    public bool captured;

    public void CaptureIfNeeded()
    {
        if (captured) return;
        originalParent = transform.parent;
        originalLocalPosition = transform.localPosition;
        originalLocalRotation = transform.localRotation;
        originalLocalScale = transform.localScale;
        captured = true;
    }

    public void Restore()
    {
        if (!captured) return;
        transform.SetParent(originalParent, false);
        transform.localPosition = originalLocalPosition;
        transform.localRotation = originalLocalRotation;
        transform.localScale = originalLocalScale;
    }
}
