using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

// Lightweight mouse-orbit camera for the prototype controller. Not a replacement for Cinemachine.
[RequireComponent(typeof(Camera))]
public sealed class ENVPrototypeOrbitCamera : MonoBehaviour
{
    public Transform target;
    public float distance = 4.5f;
    public float mouseSensitivity = 0.13f;
    public float minPitch = -30f;
    public float maxPitch = 70f;
    private float yaw;
    private float pitch = 14f;
    private bool mouseLocked;

    private void Start()
    {
        yaw = transform.eulerAngles.y;
        SetMouseLock(true);
    }

    private void OnDisable()
    {
        SetMouseLock(false);
    }

    private void LateUpdate()
    {
        if (target == null) return;
        if (EscapePressed()) SetMouseLock(false);
        if (!mouseLocked && LeftMousePressed()) SetMouseLock(true);
        if (mouseLocked)
        {
            Vector2 delta = ReadMouseDelta();
            yaw += delta.x * mouseSensitivity;
            pitch = Mathf.Clamp(pitch - delta.y * mouseSensitivity, minPitch, maxPitch);
        }
        Quaternion orbit = Quaternion.Euler(pitch, yaw, 0f);
        transform.position = target.position - orbit * Vector3.forward * distance;
        transform.rotation = orbit;
    }

    private void SetMouseLock(bool value)
    {
        mouseLocked = value;
        Cursor.lockState = value ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !value;
    }

    private static Vector2 ReadMouseDelta()
    {
#if ENABLE_INPUT_SYSTEM
        return Mouse.current != null ? Mouse.current.delta.ReadValue() : Vector2.zero;
#else
        // Legacy input axis is already sensitivity-scaled; adjust into similar range.
        return new Vector2(Input.GetAxis("Mouse X") * 8f, Input.GetAxis("Mouse Y") * 8f);
#endif
    }

    private static bool EscapePressed()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.Escape);
#endif
    }

    private static bool LeftMousePressed()
    {
#if ENABLE_INPUT_SYSTEM
        return Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
#else
        return Input.GetMouseButtonDown(0);
#endif
    }
}
