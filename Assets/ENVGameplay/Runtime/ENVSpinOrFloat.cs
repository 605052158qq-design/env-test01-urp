using UnityEngine;

/// <summary>A04/A08: a single script for the existing gem mesh or actual windmill blades.</summary>
[DisallowMultipleComponent]
public sealed class ENVSpinOrFloat : MonoBehaviour
{
    public enum Motion { Rotate, Float }
    public Motion motion = Motion.Rotate;
    public Transform visualPart;
    public Vector3 degreesPerSecond = new Vector3(0f, 60f, 0f);
    public Vector3 floatAxis = Vector3.up;
    [Min(0f)] public float floatAmplitude = 0.2f;
    [Min(0f)] public float floatFrequency = 0.8f;
    private Vector3 startingLocalPosition;
    private void Start()
    {
        if (visualPart == null) visualPart = transform;
        startingLocalPosition = visualPart.localPosition;
    }
    private void Update()
    {
        if (visualPart == null) return;
        if (motion == Motion.Rotate) visualPart.Rotate(degreesPerSecond * Time.deltaTime, Space.Self);
        else visualPart.localPosition = startingLocalPosition + floatAxis.normalized * (Mathf.Sin(Time.time * floatFrequency * Mathf.PI * 2f) * floatAmplitude);
    }
}
