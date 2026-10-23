using System;
using UnityEngine;
using UnityEngine.InputSystem;
using Random = UnityEngine.Random;

[Serializable]
public class WeaponSlot
{
    public WeaponData data;
    public GameObject model;
    public Transform muzzle;

    [NonSerialized] public int magazine;
    [NonSerialized] public int reserve;
    [NonSerialized] public bool auto;
    [NonSerialized] public int zeroIndex;
    [NonSerialized] public int zoomIndex;
}

// Everything about shooting: pick a gun (1-5), fire, reload (R), aim (RMB), scope zoom, zeroing and breath hold.
public class WeaponController : MonoBehaviour
{
    [SerializeField] private WeaponSlot[] slots;
    [SerializeField] private PlayerLook look;
    [SerializeField] private PlayerMover mover;
    [SerializeField] private Bullet bulletPrefab;

    [Header("Where the gun sits")]
    [SerializeField] private Transform gunRig;
    [SerializeField] private Transform firstPersonSocket;
    [SerializeField] private Transform thirdPersonSocket;

    [Header("Settings")]
    [SerializeField] private float normalFov = 70f;
    [SerializeField] private float switchTime = 0.45f;
    [SerializeField] private float[] zeroDistances = { 50f, 100f, 200f, 300f };
    [SerializeField] private float breathSeconds = 5f;

    private static readonly RaycastHit[] aimHits = new RaycastHit[8];

    private int currentIndex;
    private float fireTimer;
    private float switchTimer;
    private bool reloading;
    private float reloadTimer;
    private float reloadTotal;
    private float adsAmount;
    private float spread;
    private float bloom;
    private float breath = 1f;
    private bool exhausted;
    private bool holdingBreath;
    private bool restoreThirdPerson;
    private float autoReloadDelay = -1f;
    private float meleeTimer = -1f;
    private bool meleePending;

    private const float MeleeHitTime = 0.14f;
    private const float MeleeDuration = 0.4f;

    public event Action Fired;

    public WeaponSlot Current => slots[currentIndex];
    public int CurrentIndex => currentIndex;
    public int SlotCount => slots.Length;
    public float AdsAmount => Mathf.SmoothStep(0f, 1f, adsAmount);
    public float Spread => spread;
    public bool IsReloading => reloading;
    public float ReloadProgress => reloading ? 1f - reloadTimer / reloadTotal : 0f;
    public float SwitchProgress => 1f - Mathf.Clamp01(switchTimer / switchTime);
    public float ZeroDistance => zeroDistances[Current.zeroIndex];
    public float Zoom => Current.data.IsScope ? Current.data.zoomLevels[Current.zoomIndex] : 1f;
    public float Fov { get; private set; }
    public bool Aiming { get; private set; }
    public bool ScopedView => Current.data.IsScope && AdsAmount > 0.9f && !look.ThirdPerson;
    public float Breath01 => breath;
    public bool HoldingBreath => holdingBreath;
    public string FireModeLabel => Current.data.canAutoFire ? (Current.auto ? "AUTO" : "SEMI") : "SEMI";
    public PlayerLook Look => look;
    public float MeleeProgress => meleeTimer < 0f ? -1f : meleeTimer / MeleeDuration;   // -1 when not swinging

    private void Awake()
    {
        foreach (WeaponSlot s in slots) ResetSlot(s);
        Fov = normalFov;
        Equip(Mathf.Min(1, slots.Length - 1));     // slot 1 is the knife, we start with the pistol
    }

    private void ResetSlot(WeaponSlot s)
    {
        s.magazine = s.data.magazineSize;
        s.reserve = s.data.reserveAmmo;
        s.auto = s.data.autoByDefault;
        s.zeroIndex = Mathf.Clamp(s.data.defaultZeroIndex, 0, zeroDistances.Length - 1);
        s.zoomIndex = 0;
    }

    public void RefillAll()
    {
        foreach (WeaponSlot s in slots)
        {
            int zero = s.zeroIndex;
            ResetSlot(s);
            s.zeroIndex = zero;
        }
        reloading = false;
    }

    private void Update()
    {
        Keyboard kb = Keyboard.current;
        Mouse mouse = Mouse.current;
        if (kb == null || mouse == null) return;

        float dt = Time.deltaTime;
        bool locked = Cursor.lockState == CursorLockMode.Locked;

        fireTimer -= dt;
        switchTimer -= dt;
        bloom = fireTimer <= 0f ? Mathf.MoveTowards(bloom, 0f, Current.data.bloomRecovery * dt) : bloom;

        if (locked) ReadKeys(kb, mouse);

        UpdateReload(dt);
        UpdateMelee(dt);
        UpdateAim(mouse, locked, dt);
        UpdateCameraMode(kb, locked);
        UpdateBreathAndSway(kb, dt);
        UpdateSpread(dt);
        UpdateFov();

        if (locked) TryFire(mouse);

        if (autoReloadDelay > 0f)
        {
            autoReloadDelay -= dt;
            if (autoReloadDelay <= 0f) StartReload();
        }
    }

    // ---------------- input ----------------

    private void ReadKeys(Keyboard kb, Mouse mouse)
    {
        for (int i = 0; i < slots.Length; i++)
        {
            Key key = (Key)((int)Key.Digit1 + i);
            if (kb[key].wasPressedThisFrame && i != currentIndex) Equip(i);
        }

        if (kb.rKey.wasPressedThisFrame) StartReload();

        if (kb.bKey.wasPressedThisFrame && Current.data.canAutoFire) Current.auto = !Current.auto;

        if (kb.leftBracketKey.wasPressedThisFrame) Current.zeroIndex = Mathf.Max(0, Current.zeroIndex - 1);
        if (kb.rightBracketKey.wasPressedThisFrame) Current.zeroIndex = Mathf.Min(zeroDistances.Length - 1, Current.zeroIndex + 1);

        // scroll: zoom of a variable scope while aiming, otherwise it changes the gun
        float scroll = mouse.scroll.ReadValue().y;
        if (Mathf.Abs(scroll) > 0.1f)
        {
            int step = scroll > 0f ? 1 : -1;
            if (Aiming)
            {
                if (Current.data.optic == OpticType.VariableScope)
                    Current.zoomIndex = Mathf.Clamp(Current.zoomIndex + step, 0, Current.data.zoomLevels.Length - 1);
            }
            else if (switchTimer < switchTime * 0.6f)
            {
                Equip((currentIndex - step + slots.Length) % slots.Length);   // scroll up = previous gun
            }
        }
    }

    private void Equip(int index)
    {
        currentIndex = index;
        for (int i = 0; i < slots.Length; i++) slots[i].model.SetActive(i == index);

        reloading = false;
        autoReloadDelay = -1f;
        switchTimer = switchTime;
        bloom = 0f;
        meleeTimer = -1f;
        meleePending = false;
    }

    // ---------------- aiming and camera ----------------

    private void UpdateAim(Mouse mouse, bool locked, float dt)
    {
        bool wantsAim = locked && mouse.rightButton.isPressed && !reloading && switchTimer <= 0f && !mover.IsSprinting && !Current.data.isMelee;
        Aiming = wantsAim;

        adsAmount = Mathf.MoveTowards(adsAmount, wantsAim ? 1f : 0f, dt / Current.data.adsTime);

        mover.CanSprint = !wantsAim && !reloading;
        mover.SpeedMultiplier = Mathf.Lerp(1f, Current.data.adsMoveSpeed, AdsAmount);
    }

    // A scope only works in first person: switch there while aiming and go back afterwards.
    private void UpdateCameraMode(Keyboard kb, bool locked)
    {
        bool scopeAim = Aiming && Current.data.IsScope;

        if (scopeAim && look.ThirdPerson)
        {
            restoreThirdPerson = true;
            look.SetThirdPerson(false);
            AttachRig();
        }
        else if (!Aiming && restoreThirdPerson)
        {
            restoreThirdPerson = false;
            look.SetThirdPerson(true);
            AttachRig();
        }

        if (locked && kb.vKey.wasPressedThisFrame && !scopeAim)
        {
            look.ToggleCameraMode();
            AttachRig();
        }
    }

    private void AttachRig()
    {
        gunRig.SetParent(look.ThirdPerson ? thirdPersonSocket : firstPersonSocket, false);
        gunRig.localPosition = Vector3.zero;
        gunRig.localRotation = Quaternion.identity;
    }

    private void UpdateFov()
    {
        float scopedFov;
        if (Current.data.IsScope)
        {
            // magnification zoom: the visible angle shrinks by the zoom factor
            float half = Mathf.Atan(Mathf.Tan(normalFov * 0.5f * Mathf.Deg2Rad) / Zoom);
            scopedFov = half * 2f * Mathf.Rad2Deg;
        }
        else
        {
            scopedFov = normalFov * Current.data.adsFovScale;
        }

        Fov = Mathf.Lerp(normalFov, scopedFov, AdsAmount);
        look.SetFov(Fov);

        // keep the mouse feeling the same on screen when zoomed in
        look.SensitivityScale = Mathf.Tan(Fov * 0.5f * Mathf.Deg2Rad) / Mathf.Tan(normalFov * 0.5f * Mathf.Deg2Rad);
    }

    // ---------------- breath and sway ----------------

    private void UpdateBreathAndSway(Keyboard kb, float dt)
    {
        bool wantsHold = kb.leftShiftKey.isPressed && ScopedView && !exhausted;
        holdingBreath = wantsHold && breath > 0f;

        if (holdingBreath)
        {
            breath -= dt / breathSeconds;
            if (breath <= 0f)
            {
                breath = 0f;
                exhausted = true;      // out of breath: sway comes back until we recover
            }
        }
        else
        {
            breath = Mathf.MoveTowards(breath, 1f, dt / (breathSeconds * 0.6f));
            if (exhausted && breath > 0.4f) exhausted = false;
        }

        // gentle sway, bigger when zoomed in, aiming, moving or out of breath
        float amplitude = Mathf.Lerp(0.02f, 0.1f, AdsAmount) * Mathf.Sqrt(Zoom);
        if (mover.IsMoving) amplitude *= 2.5f;
        if (mover.IsCrouching) amplitude *= 0.6f;
        if (holdingBreath) amplitude *= 0.08f;

        float t = Time.time;
        Vector2 noise = new Vector2(Mathf.PerlinNoise(t * 0.7f, 0.1f) - 0.5f, Mathf.PerlinNoise(0.3f, t * 0.8f) - 0.5f) * 2f;
        look.SetSway(noise * amplitude);
    }

    private void UpdateSpread(float dt)
    {
        WeaponData d = Current.data;

        float baseSpread = Mathf.Lerp(d.hipSpread, d.adsSpread, AdsAmount);
        float movement = 0f;
        if (!mover.IsGrounded) movement = d.jumpPenalty;
        else if (mover.IsMoving) movement = d.movePenalty * (mover.IsSprinting ? 1.5f : 1f);

        float stance = mover.IsCrouching ? d.crouchMultiplier : 1f;
        float target = (baseSpread + movement + bloom) * stance;
        spread = Mathf.Lerp(spread, target, 12f * dt);
    }

    // ---------------- firing ----------------

    private void TryFire(Mouse mouse)
    {
        WeaponSlot s = Current;
        bool pressed = mouse.leftButton.wasPressedThisFrame;
        bool wants = s.auto ? mouse.leftButton.isPressed : pressed;
        if (!wants) return;

        if (reloading || switchTimer > 0f || mover.IsSprinting) return;

        if (s.data.isMelee)
        {
            if (pressed && fireTimer <= 0f) StartSwing(s);
            return;
        }

        if (s.magazine <= 0)
        {
            if (pressed) StartReload();
            return;
        }

        if (fireTimer > 0f) return;
        Shoot(s);
    }

    private void Shoot(WeaponSlot s)
    {
        WeaponData d = s.data;
        fireTimer = 60f / d.fireRate;
        s.magazine--;
        ScoreBoard.AddShots(d.pellets);

        Transform cam = look.Cam.transform;
        Vector3 aimPoint = AimPoint();
        float zeroAngle = Ballistics.ZeroAngle(d.muzzleVelocity, ZeroDistance) * Mathf.Rad2Deg;

        for (int i = 0; i < d.pellets; i++)
        {
            Vector3 direction = (aimPoint - s.muzzle.position).normalized;
            direction = Quaternion.AngleAxis(-zeroAngle, cam.right) * direction;   // tilt up so the bullet meets the crosshair at the zero distance
            direction = ApplySpread(direction, spread + (d.pellets > 1 ? d.pelletSpread : 0f));

            Bullet bullet = Instantiate(bulletPrefab, s.muzzle.position, Quaternion.LookRotation(direction));
            bullet.Fire(direction, d, transform);
        }

        // recoil is a bit smaller when aiming or crouching
        float control = (1f - 0.25f * AdsAmount) * (mover.IsCrouching ? 0.85f : 1f);
        look.AddRecoil(d.recoilUp * control * Random.Range(0.85f, 1.15f), Random.Range(-d.recoilSide, d.recoilSide) * control, d.recoilRecovery);
        bloom = Mathf.Min(bloom + d.bloomPerShot, d.maxBloom);

        MuzzleFlash flash = s.muzzle.GetComponent<MuzzleFlash>();
        if (flash != null) flash.Play();

        Fired?.Invoke();

        if (s.magazine == 0 && s.reserve > 0) autoReloadDelay = 0.35f;
    }

    // ---------------- knife ----------------

    private void StartSwing(WeaponSlot s)
    {
        fireTimer = 60f / s.data.fireRate;
        meleeTimer = 0f;
        meleePending = true;
        ScoreBoard.AddShots(1);
        Fired?.Invoke();
    }

    // The blade connects a moment after the swing starts, not on the click.
    private void UpdateMelee(float dt)
    {
        if (meleeTimer < 0f) return;

        meleeTimer += dt;
        if (meleePending && meleeTimer >= MeleeHitTime)
        {
            meleePending = false;
            MeleeHit(Current.data);
        }
        if (meleeTimer >= MeleeDuration) meleeTimer = -1f;
    }

    private void MeleeHit(WeaponData d)
    {
        Transform cam = look.Cam.transform;
        int count = Physics.SphereCastNonAlloc(cam.position, 0.2f, cam.forward, aimHits, d.meleeRange, ~0, QueryTriggerInteraction.Ignore);

        bool found = false;
        RaycastHit best = default;
        for (int i = 0; i < count; i++)
        {
            if (aimHits[i].collider.transform.IsChildOf(transform)) continue;
            if (aimHits[i].distance <= 0f) continue;          // started inside something
            if (!found || aimHits[i].distance < best.distance)
            {
                best = aimHits[i];
                found = true;
            }
        }
        if (found) Bullet.ApplyHit(best, d.damage, cam.forward, d.impactForce);
    }

    // The point the crosshair is on (up to the zero distance). Bullets start at the muzzle and fly towards it.
    private Vector3 AimPoint()
    {
        Transform cam = look.Cam.transform;
        float best = ZeroDistance;

        int count = Physics.RaycastNonAlloc(cam.position, cam.forward, aimHits, best, ~0, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < count; i++)
        {
            if (aimHits[i].collider.transform.IsChildOf(transform)) continue;
            if (aimHits[i].distance < best) best = aimHits[i].distance;
        }
        return cam.position + cam.forward * best;
    }

    private Vector3 ApplySpread(Vector3 direction, float degrees)
    {
        if (degrees <= 0f) return direction;

        Transform cam = look.Cam.transform;
        Vector2 offset = Random.insideUnitCircle * Mathf.Tan(degrees * Mathf.Deg2Rad);
        return (direction + cam.right * offset.x + cam.up * offset.y).normalized;
    }

    // ---------------- reload ----------------

    private void StartReload()
    {
        WeaponSlot s = Current;
        int capacity = s.data.magazineSize + (s.magazine > 0 ? 1 : 0);   // one stays in the chamber

        if (reloading || s.reserve <= 0 || s.magazine >= capacity) return;

        reloading = true;
        reloadTotal = s.magazine == 0 ? s.data.emptyReloadTime : s.data.reloadTime;
        reloadTimer = reloadTotal;
    }

    private void UpdateReload(float dt)
    {
        if (!reloading) return;

        reloadTimer -= dt;
        if (reloadTimer > 0f) return;

        WeaponSlot s = Current;
        int capacity = s.data.magazineSize + (s.magazine > 0 ? 1 : 0);
        int take = Mathf.Min(capacity - s.magazine, s.reserve);
        s.magazine += take;
        s.reserve -= take;
        reloading = false;
    }
}
