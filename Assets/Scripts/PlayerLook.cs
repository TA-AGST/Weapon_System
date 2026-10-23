using UnityEngine;
using UnityEngine.InputSystem;

// Mouse look, recoil, scope sway and the first / third person camera (V).
public class PlayerLook : MonoBehaviour
{
    [SerializeField] private Transform cameraPivot;
    [SerializeField] private Camera playerCamera;
    [SerializeField] private GameObject bodyModel;            // the capsule, only visible in third person
    [SerializeField] private float baseSensitivity = 0.12f;
    [SerializeField] private float minPitch = -80f;
    [SerializeField] private float maxPitch = 80f;
    [SerializeField] private Vector3 thirdPersonOffset = new Vector3(0.8f, 0.25f, -2.8f);
    [SerializeField] private float cameraSmooth = 12f;

    private float yaw;
    private float pitch;
    private Vector2 recoil;           // x = sideways, y = upwards (degrees)
    private Vector2 sway;             // scope sway (degrees)
    private float recoilRecovery = 8f;

    public Camera Cam => playerCamera;
    public bool ThirdPerson { get; private set; }
    public float SensitivityScale { get; set; } = 1f;     // smaller when zoomed in

    private void Awake()
    {
        yaw = transform.eulerAngles.y;
        SetCursorLocked(true);
        ApplyBodyVisibility();
    }

    public void AddRecoil(float up, float side, float recovery)
    {
        recoil += new Vector2(side, up);
        recoilRecovery = recovery;
    }

    public void SetSway(Vector2 degrees)
    {
        sway = degrees;
    }

    public void SetThirdPerson(bool value)
    {
        ThirdPerson = value;
        ApplyBodyVisibility();
    }

    private void Update()
    {
        Keyboard kb = Keyboard.current;
        Mouse mouse = Mouse.current;
        if (kb == null || mouse == null) return;

        if (kb.escapeKey.wasPressedThisFrame) SetCursorLocked(false);
        else if (mouse.leftButton.wasPressedThisFrame && Cursor.lockState != CursorLockMode.Locked) SetCursorLocked(true);

        if (Cursor.lockState == CursorLockMode.Locked)
        {
            Vector2 delta = mouse.delta.ReadValue() * baseSensitivity * SensitivityScale;
            yaw += delta.x;
            pitch = Mathf.Clamp(pitch - delta.y, minPitch, maxPitch);
        }

        // the kick slowly goes back down
        recoil = Vector2.MoveTowards(recoil, Vector2.zero, recoilRecovery * Time.deltaTime);

        transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        cameraPivot.localRotation = Quaternion.Euler(pitch - recoil.y + sway.y, recoil.x + sway.x, 0f);
    }

    private void LateUpdate()
    {
        Vector3 target = ThirdPerson ? thirdPersonOffset : Vector3.zero;
        Transform cam = playerCamera.transform;

        // in third person pull the camera in when a wall is behind us
        if (ThirdPerson)
        {
            Vector3 wanted = cameraPivot.TransformPoint(target);
            Vector3 direction = wanted - cameraPivot.position;
            if (Physics.SphereCast(cameraPivot.position, 0.2f, direction.normalized, out RaycastHit hit, direction.magnitude, ~0, QueryTriggerInteraction.Ignore)
                && !hit.collider.transform.IsChildOf(transform))
            {
                target = cameraPivot.InverseTransformPoint(cameraPivot.position + direction.normalized * Mathf.Max(0.4f, hit.distance - 0.1f));
            }
        }

        cam.localPosition = Vector3.Lerp(cam.localPosition, target, cameraSmooth * Time.deltaTime);
    }

    public void ToggleCameraMode()
    {
        SetThirdPerson(!ThirdPerson);
    }

    public void SetFov(float fov)
    {
        playerCamera.fieldOfView = fov;
    }

    private void ApplyBodyVisibility()
    {
        bodyModel.SetActive(ThirdPerson);
    }

    private static void SetCursorLocked(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }
}
