using System.Collections;
using UnityEngine;

// Brings a dummy back to life a few seconds after it was eliminated.
[RequireComponent(typeof(Health))]
public class Respawner : MonoBehaviour
{
    [SerializeField] private float delay = 10f;

    private Health health;

    private void Awake()
    {
        health = GetComponent<Health>();
    }

    private void OnEnable()
    {
        health.Died += OnDied;
    }

    private void OnDisable()
    {
        health.Died -= OnDied;
    }

    private void OnDied()
    {
        StartCoroutine(Return());
    }

    private IEnumerator Return()
    {
        yield return new WaitForSeconds(delay);
        health.ResetHealth();
    }
}
