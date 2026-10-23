using UnityEngine;
using UnityEngine.InputSystem;

// Moves the gun on screen so it feels alive: aiming, walking bob, sway when turning,
// sprint pose, reload dip, weapon switch and a kick back when firing. No animation clips, all done in code.
public class ViewModelMotion : MonoBehaviour
{
    [SerializeField] private WeaponController weapon;
    [SerializeField] private PlayerMover mover;
    [SerializeField] private PlayerLook look;
    [SerializeField] private Transform gunRig;
    [SerializeField] private Vector3 aimOffset = new Vector3(-0.22f, 0.07f, 0f);   // moves the gun to the middle of the screen
    [SerializeField] private float smooth = 18f;

    private float bobTime;
    private float sprintBlend;
    private float kick;
    private Vector2 mouseSway;
    private bool gunHidden;

    private void OnEnable()
    {
        weapon.Fired += OnFired;
    }

    private void OnDisable()
    {
        weapon.Fired -= OnFired;
    }

    private void OnFired()
    {
        kick = 1f;
    }

    private void LateUpdate()
    {
        float dt = Time.deltaTime;
        kick = Mathf.MoveTowards(kick, 0f, dt * 9f);

        Vector3 position = Vector3.zero;
        Vector3 rotation = Vector3.zero;

        if (!look.ThirdPerson) AddFirstPersonMotion(ref position, ref rotation, dt);

        // reload: the gun dips and tilts
        if (weapon.IsReloading)
        {
            float dip = Mathf.Sin(weapon.ReloadProgress * Mathf.PI);
            position.y -= 0.12f * dip;
            rotation.x += 35f * dip;
            rotation.z += 20f * dip;
        }

        // switching: the new gun comes up from below
        float lower = 1f - weapon.SwitchProgress;
        position.y -= 0.35f * lower * lower;
        rotation.x += 40f * lower;

        // knife: a slash from the right to the left with a little thrust
        float swing = weapon.MeleeProgress;
        if (swing >= 0f)
        {
            float arc = Mathf.Sin(swing * Mathf.PI);
            position += new Vector3(Mathf.Lerp(0.12f, -0.2f, swing), -0.05f * arc, 0.18f * arc);
            rotation += new Vector3(-20f * arc, Mathf.Lerp(40f, -50f, swing), Mathf.Lerp(-40f, 40f, swing));
        }

        // firing kick
        position.z -= 0.05f * kick;
        rotation.x -= 4f * kick;

        gunRig.localPosition = Vector3.Lerp(gunRig.localPosition, position, smooth * dt);
        gunRig.localRotation = Quaternion.Slerp(gunRig.localRotation, Quaternion.Euler(rotation), smooth * dt);

        // looking through a scope: the gun itself would only block the view
        bool hidden = weapon.ScopedView;
        if (hidden != gunHidden)
        {
            gunHidden = hidden;
            foreach (Renderer r in gunRig.GetComponentsInChildren<Renderer>(true)) r.enabled = !hidden;
        }
    }

    private void AddFirstPersonMotion(ref Vector3 position, ref Vector3 rotation, float dt)
    {
        float ads = weapon.AdsAmount;
        position += Vector3.Lerp(Vector3.zero, aimOffset, ads);

        // walking bob (smaller when aiming)
        float bobSpeed = mover.IsMoving ? (mover.IsSprinting ? 1.6f : 1f) : 0f;
        bobTime += dt * bobSpeed * 9f;
        float bobAmount = 1f - ads * 0.85f;
        position.x += Mathf.Sin(bobTime) * 0.012f * bobAmount;
        position.y += Mathf.Abs(Mathf.Cos(bobTime)) * 0.01f * bobAmount;

        // the gun lags a little behind when you turn
        Mouse mouse = Mouse.current;
        Vector2 delta = mouse != null ? mouse.delta.ReadValue() : Vector2.zero;
        mouseSway = Vector2.Lerp(mouseSway, -delta * 0.05f, 10f * dt);
        rotation.y += Mathf.Clamp(mouseSway.x, -4f, 4f) * (1f - ads * 0.7f);
        rotation.x += Mathf.Clamp(-mouseSway.y, -4f, 4f) * (1f - ads * 0.7f);

        // sprint pose
        sprintBlend = Mathf.MoveTowards(sprintBlend, mover.IsSprinting ? 1f : 0f, dt * 5f);
        position += new Vector3(0.05f, -0.07f, 0f) * sprintBlend;
        rotation += new Vector3(15f, -35f, 0f) * sprintBlend;
    }
}
