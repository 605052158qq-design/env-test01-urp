using System.Collections.Generic;
using UnityEngine;

/// <summary>Editable physical boundary along the edge of the permitted green walk area.
/// Move control points in Scene View; run the editor Rebuild command to refresh box colliders.
/// Only one set of invisible colliders is generated; no replacement map meshes.
/// </summary>
[DisallowMultipleComponent]
public sealed class ENVAirWallPath : MonoBehaviour
{
    public List<Transform> points = new List<Transform>();
    [Min(0.05f)] public float thickness = 0.5f;
    [Min(0.2f)] public float height = 5f;
    public float bottomOffset = -0.2f;
    public bool closedLoop = false;
    public const string GeneratedRootName = "__AirWallColliders";

    private void OnDrawGizmosSelected()
    {
        if (points == null) return;
        Gizmos.color = new Color(1f, 0.5f, 0.1f, 1f);
        for (int i = 1; i < points.Count; i++)
            if (points[i] != null && points[i-1] != null)
                Gizmos.DrawLine(points[i-1].position, points[i].position);
        if (closedLoop && points.Count > 2 && points[0] != null && points[points.Count-1] != null)
            Gizmos.DrawLine(points[points.Count-1].position, points[0].position);
    }
}
