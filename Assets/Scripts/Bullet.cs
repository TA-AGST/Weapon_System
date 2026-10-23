using UnityEngine;

// A bullet that really flies: it has a speed and gravity pulls it down, so far targets need a hold-over.
// Each step checks the path from the old position to the new one with a raycast, so it never skips through things.
public class Bullet : MonoBehaviour
{
    [SerializeField] private TrailRenderer trail;

    private static readonly RaycastHit[] hits = new RaycastHit[8];

    private WeaponData data;
    private Transform shooter;
    private Vector3 velocity;
    private float travelled;
    private float age;
    private bool finished;

    public void Fire(Vector3 direction, WeaponData weapon, Transform shooterRoot)
    {
        data = weapon;
        shooter = shooterRoot;
        velocity = direction * weapon.muzzleVelocity;
    }

    private void FixedUpdate()
    {
        if (finished) return;

        float dt = Time.fixedDeltaTime;
        age += dt;

        Vector3 from = transform.position;
        velocity.y -= Ballistics.Gravity * dt;
        Vector3 move = velocity * dt;
        float distance = move.magnitude;

        if (FindHit(from, move / distance, distance, out RaycastHit hit))
        {
            travelled += hit.distance;
            OnHit(hit, move / distance);
            return;
        }

        transform.position = from + move;
        travelled += distance;

        if (travelled > data.maxRange || age > 6f) Finish();
    }

    // Nearest thing on the path that is not the shooter.
    private bool FindHit(Vector3 origin, Vector3 direction, float distance, out RaycastHit nearest)
    {
        int count = Physics.RaycastNonAlloc(origin, direction, hits, distance, ~0, QueryTriggerInteraction.Ignore);

        nearest = default;
        float best = float.MaxValue;
        bool found = false;

        for (int i = 0; i < count; i++)
        {
            if (hits[i].collider.transform.IsChildOf(shooter)) continue;
            if (hits[i].distance < best)
            {
                best = hits[i].distance;
                nearest = hits[i];
                found = true;
            }
        }
        return found;
    }

    private void OnHit(RaycastHit hit, Vector3 direction)
    {
        ApplyHit(hit, data.damage * data.DamageFactor(travelled), direction, data.impactForce);

        transform.position = hit.point;
        Finish();
    }

    // Damage, score, push and impact effects for one hit. The knife uses this too.
    public static void ApplyHit(RaycastHit hit, float damage, Vector3 direction, float force)
    {
        bool flesh = false;

        HitZone zone = hit.collider.GetComponent<HitZone>();
        RangeTarget board = hit.collider.GetComponentInParent<RangeTarget>();

        if (zone != null)
        {
            flesh = true;
            if (!zone.OwnerIsDead)
            {
                zone.Hit(damage);
                ScoreBoard.AddBodyHit(zone.IsHead);
            }
        }
        else if (board != null)
        {
            int points = board.Score(hit.point);
            if (points > 0)
            {
                ScoreBoard.AddTargetHit(points);
                board.Shot();
            }
        }

        if (hit.rigidbody != null && !hit.rigidbody.isKinematic)
            hit.rigidbody.AddForceAtPosition(direction * force, hit.point, ForceMode.Impulse);

        ImpactEffects.Spawn(hit, flesh);
    }

    private void Finish()
    {
        finished = true;
        float wait = trail != null ? trail.time : 0f;
        Destroy(gameObject, wait);
    }
}
