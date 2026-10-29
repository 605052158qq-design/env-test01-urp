using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// L05: Attach this directly to the actual chair/stump GameObject (or its one final prefab).
/// No duplicate interaction prefab or invisible stand-in is needed.
/// </summary>
[DisallowMultipleComponent]
public sealed class ENVSeat : MonoBehaviour
{
    public Transform sitPoint;
    public Transform exitPoint;
    [Min(0.1f)] public float interactionRadius = 2f;

    private static readonly List<ENVSeat> Active = new List<ENVSeat>();

    private void OnEnable() { if (!Active.Contains(this)) Active.Add(this); }
    private void OnDisable() { Active.Remove(this); }

    public static ENVSeat FindNearest(Vector3 worldPosition)
    {
        ENVSeat best = null;
        float nearestSqr = float.MaxValue;
        for (int i = 0; i < Active.Count; i++)
        {
            ENVSeat s = Active[i];
            if (s == null || !s.isActiveAndEnabled) continue;
            float sqr = (s.transform.position - worldPosition).sqrMagnitude;
            if (sqr > s.interactionRadius * s.interactionRadius || sqr >= nearestSqr) continue;
            best = s;
            nearestSqr = sqr;
        }
        return best;
    }

    public Transform SitTarget { get { return sitPoint != null ? sitPoint : transform; } }
    public Transform ExitTarget { get { return exitPoint != null ? exitPoint : transform; } }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.2f, 1f, 0.5f, 0.7f);
        Gizmos.DrawWireSphere(transform.position, interactionRadius);
        Transform p = SitTarget;
        Gizmos.color = Color.cyan;
        Gizmos.DrawSphere(p.position, 0.12f);
        Gizmos.DrawRay(p.position, p.forward * 0.6f);
        if (exitPoint != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(exitPoint.position, 0.1f);
        }
    }
}
