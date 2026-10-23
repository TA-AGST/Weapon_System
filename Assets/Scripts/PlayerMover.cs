using UnityEngine;
using UnityEngine.InputSystem;

// WASD to walk, Shift to sprint, C to crouch, Space to jump.
[RequireComponent(typeof(CharacterController))]
public class PlayerMover : MonoBehaviour
{
    [SerializeField] private float walkSpeed = 4.5f;
    [SerializeField] private float sprintSpeed = 7.5f;
    [SerializeField] private float crouchSpeed = 2.4f;
    [SerializeField] private float jumpHeight = 1.0f;
    [SerializeField] private float gravity = -20f;
    [SerializeField] private float standHeight = 1.8f;
    [SerializeField] private float crouchHeight = 1.2f;
    [SerializeField] private Transform cameraPivot;
    [SerializeField] private float standEyeHeight = 1.6f;
    [SerializeField] private float crouchEyeHeight = 1.1f;

    private CharacterController controller;
    private float verticalVelocity;
    private float eyeHeight;
    private Vector3 horizontalVelocity;

    public bool IsGrounded => controller.isGrounded;
    public bool IsCrouching { get; private set; }
    public bool IsSprinting { get; private set; }
    public bool IsMoving => horizontalVelocity.magnitude > 0.5f;
    public bool CanSprint { get; set; } = true;       // the weapon turns this off while aiming or reloading
    public float SpeedMultiplier { get; set; } = 1f;  // the weapon slows us down while aiming

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        eyeHeight = standEyeHeight;
    }

    private void Update()
    {
        Keyboard kb = Keyboard.current;
        if (kb == null) return;

        bool locked = Cursor.lockState == CursorLockMode.Locked;

        Vector2 input = Vector2.zero;
        if (locked)
        {
            if (kb.wKey.isPressed) input.y += 1f;
            if (kb.sKey.isPressed) input.y -= 1f;
            if (kb.dKey.isPressed) input.x += 1f;
            if (kb.aKey.isPressed) input.x -= 1f;
            input = Vector2.ClampMagnitude(input, 1f);

            if (kb.cKey.wasPressedThisFrame) IsCrouching = !IsCrouching;
        }

        // sprint only when going forward and not crouching
        IsSprinting = locked && CanSprint && kb.leftShiftKey.isPressed && input.y > 0.5f && !IsCrouching;
        if (IsSprinting) IsCrouching = false;

        float speed = IsCrouching ? crouchSpeed : (IsSprinting ? sprintSpeed : walkSpeed);
        Vector3 wish = (transform.right * input.x + transform.forward * input.y) * speed * SpeedMultiplier;
        horizontalVelocity = Vector3.Lerp(horizontalVelocity, wish, 12f * Time.deltaTime);

        if (controller.isGrounded && verticalVelocity < 0f) verticalVelocity = -2f;
        if (locked && controller.isGrounded && !IsCrouching && kb.spaceKey.wasPressedThisFrame)
            verticalVelocity = Mathf.Sqrt(2f * -gravity * jumpHeight);
        verticalVelocity += gravity * Time.deltaTime;

        controller.Move((horizontalVelocity + Vector3.up * verticalVelocity) * Time.deltaTime);

        UpdateStance();
    }

    // Crouching makes the capsule shorter and lowers the eyes smoothly.
    private void UpdateStance()
    {
        float targetHeight = IsCrouching ? crouchHeight : standHeight;
        controller.height = Mathf.Lerp(controller.height, targetHeight, 10f * Time.deltaTime);
        controller.center = new Vector3(0f, controller.height * 0.5f, 0f);

        float targetEye = IsCrouching ? crouchEyeHeight : standEyeHeight;
        eyeHeight = Mathf.Lerp(eyeHeight, targetEye, 10f * Time.deltaTime);
        cameraPivot.localPosition = new Vector3(0f, eyeHeight, 0f);
    }
}
