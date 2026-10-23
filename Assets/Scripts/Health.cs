using System;
using UnityEngine;

// Health of a target dummy.
public class Health : MonoBehaviour
{
    [SerializeField] private float maxHealth = 100f;

    public float Max => maxHealth;
    public float Current { get; private set; }
    public bool IsDead => Current <= 0f;

    public event Action Changed;
    public event Action Died;
    public event Action Revived;

    private void Awake()
    {
        Current = maxHealth;
    }

    public void TakeDamage(float amount)
    {
        if (IsDead || amount <= 0f) return;

        Current = Mathf.Max(0f, Current - amount);
        Changed?.Invoke();

        if (IsDead) Died?.Invoke();
    }

    public void ResetHealth()
    {
        Current = maxHealth;
        Changed?.Invoke();
        Revived?.Invoke();
    }
}
