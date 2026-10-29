using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

// Minimal temporary CharacterController. Keep this separate from the final character's visuals.
[RequireComponent(typeof(CharacterController))]
public sealed class ENVPrototypeThirdPersonController : MonoBehaviour
{
    public Transform viewCamera;
    public float walkSpeed = 3.5f;
    public float sprintSpeed = 6f;
    public float turnSpeed = 12f;
    public float jumpHeight = 1.3f;
    public float gravity = -20f;

    private CharacterController controller;
    private float verticalVelocity;

    private void Awake() { controller = GetComponent<CharacterController>(); }

    private void Update()
    {
        Vector2 input = ReadMove();
        Vector3 cameraForward = viewCamera != null ? viewCamera.forward : Vector3.forward;
        Vector3 cameraRight = viewCamera != null ? viewCamera.right : Vector3.right;
        cameraForward.y = cameraRight.y = 0f;
        cameraForward.Normalize();
        cameraRight.Normalize();
        Vector3 movement = Vector3.ClampMagnitude(cameraForward * input.y + cameraRight * input.x, 1f);

        if (movement.sqrMagnitude > 0.001f)
        {
            Quaternion facing = Quaternion.LookRotation(movement, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, facing, 1f - Mathf.Exp(-turnSpeed * Time.deltaTime));
        }

        if (controller.isGrounded && verticalVelocity < 0f) verticalVelocity = -2f;
        if (controller.isGrounded && JumpPressed())
            verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
        verticalVelocity += gravity * Time.deltaTime;
        float speed = SprintHeld() ? sprintSpeed : walkSpeed;
        controller.Move((movement * speed + Vector3.up * verticalVelocity) * Time.deltaTime);
    }

    private static Vector2 ReadMove()
    {
#if ENABLE_INPUT_SYSTEM
        var k = Keyboard.current;
        if (k == null) return Vector2.zero;
        float x = (k.dKey.isPressed || k.rightArrowKey.isPressed ? 1f : 0f) -
                  (k.aKey.isPressed || k.leftArrowKey.isPressed ? 1f : 0f);
        float y = (k.wKey.isPressed || k.upArrowKey.isPressed ? 1f : 0f) -
                  (k.sKey.isPressed || k.downArrowKey.isPressed ? 1f : 0f);
        return Vector2.ClampMagnitude(new Vector2(x, y), 1f);
#else
        float x = (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow) ? 1f : 0f) -
                  (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow) ? 1f : 0f);
        float y = (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow) ? 1f : 0f) -
                  (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow) ? 1f : 0f);
        return Vector2.ClampMagnitude(new Vector2(x, y), 1f);
#endif
    }

    private static bool JumpPressed()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.Space);
#endif
    }

    private static bool SprintHeld()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && (Keyboard.current.leftShiftKey.isPressed || Keyboard.current.rightShiftKey.isPressed);
#else
        return Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
#endif
    }
}
