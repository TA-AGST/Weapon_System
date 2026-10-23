using UnityEngine;

public enum OpticType { IronSights, RedDot, FixedScope, VariableScope }

// All the numbers of one gun live here (one asset per gun), so the code stays the same for every weapon.
[CreateAssetMenu(menuName = "Weapons/Weapon Data")]
public class WeaponData : ScriptableObject
{
    public string displayName = "Pistol";

    [Header("Fire")]
    public float damage = 28f;
    public float fireRate = 400f;           // rounds per minute
    public bool canAutoFire = false;        // full-auto allowed (B toggles auto / semi)
    public bool autoByDefault = false;
    public int pellets = 1;                 // shotgun fires many
    public float pelletSpread = 0f;         // extra spread for pellets (degrees)
    public float muzzleVelocity = 380f;     // metres per second
    public float maxRange = 200f;
    public float impactForce = 6f;          // push on crates

    [Header("Melee (knife)")]
    public bool isMelee = false;            // no bullets: a short swing, no ammo, no aiming
    public float meleeRange = 1.8f;         // metres from the camera

    [Header("Damage over distance")]
    public float falloffStart = 15f;        // full damage up to here (metres)
    public float falloffEnd = 60f;          // damage is at its minimum from here
    public float minDamageFactor = 0.5f;

    [Header("Ammo")]
    public int magazineSize = 15;
    public int reserveAmmo = 60;
    public float reloadTime = 1.6f;
    public float emptyReloadTime = 2.1f;

    [Header("Spread (degrees)")]
    public float hipSpread = 1.6f;
    public float adsSpread = 0.4f;
    public float movePenalty = 1.5f;
    public float jumpPenalty = 3f;
    public float crouchMultiplier = 0.7f;
    public float bloomPerShot = 0.5f;
    public float maxBloom = 3f;
    public float bloomRecovery = 4f;

    [Header("Recoil (degrees per shot)")]
    public float recoilUp = 1.4f;
    public float recoilSide = 0.3f;
    public float recoilRecovery = 8f;

    [Header("Sights")]
    public OpticType optic = OpticType.IronSights;
    public float adsFovScale = 0.85f;       // iron sights and red dot: fraction of the normal FOV
    public float[] zoomLevels = { 1f };     // scopes: magnification steps (scroll to change)
    public float adsTime = 0.15f;
    public float adsMoveSpeed = 0.5f;       // walking speed while aiming
    public int defaultZeroIndex = 1;        // index in the zero list (50, 100, 200, 300)

    public bool IsScope => optic == OpticType.FixedScope || optic == OpticType.VariableScope;

    // 1 at close range, smaller further away.
    public float DamageFactor(float distance)
    {
        if (distance <= falloffStart) return 1f;
        float t = Mathf.InverseLerp(falloffStart, falloffEnd, distance);
        return Mathf.Lerp(1f, minDamageFactor, t);
    }
}
