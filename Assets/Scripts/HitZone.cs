using UnityEngine;

// Put this on each hitbox of a dummy (head, torso, legs). The multiplier decides how much damage that body part takes.
public class HitZone : MonoBehaviour
{
    [SerializeField] private Health owner;
    [SerializeField] private float multiplier = 1f;
    [SerializeField] private bool isHead;

    public bool IsHead => isHead;
    public bool OwnerIsDead => owner.IsDead;

    public void Hit(float damage)
    {
        owner.TakeDamage(damage * multiplier);
    }
}
