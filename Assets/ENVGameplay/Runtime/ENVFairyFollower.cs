using UnityEngine;

/// <summary>L12. Add to the REAL fairy Sprite/mesh. It follows the current player, no proxy actor.</summary>
[DisallowMultipleComponent]
public sealed class ENVFairyFollower : MonoBehaviour
{
    public Transform player;
    public Vector3 relativeOffset = new Vector3(1.1f, 1.5f, -1.2f);
    [Min(0.01f)] public float followSmoothTime = 0.3f;
    public bool startFollowing = false;
    public bool alwaysFaceCamera = false;
    public Camera facingCamera;
    private Vector3 velocity;
    private void Start()
    {
        if (player == null) { var cc = FindObjectOfType<CharacterController>(); if (cc != null) player = cc.transform; }
        if (facingCamera == null) facingCamera = Camera.main;
    }
    public void BeginFollow() { startFollowing = true; velocity = Vector3.zero; }
    public void StopFollow() { startFollowing = false; }
    private void LateUpdate()
    {
        if (!startFollowing || player == null) return;
        Vector3 target = player.TransformPoint(relativeOffset);
        transform.position = Vector3.SmoothDamp(transform.position, target, ref velocity, followSmoothTime);
        if (alwaysFaceCamera && facingCamera != null)
        {
            Vector3 dir = facingCamera.transform.position - transform.position;
            if (dir.sqrMagnitude > 0.001f) transform.rotation = Quaternion.LookRotation(dir.normalized, Vector3.up);
        }
    }
}
