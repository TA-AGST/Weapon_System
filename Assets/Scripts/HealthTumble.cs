using System.Collections;
using UnityEngine;

// When the dummy dies it falls over, and it stands up again when the range is reset.
[RequireComponent(typeof(Health))]
public class HealthTumble : MonoBehaviour
{
    [SerializeField] private float fallTime = 0.4f;

    private Health health;
    private Quaternion startRotation;
    private Vector3 startPosition;

    private void Awake()
    {
        health = GetComponent<Health>();
        startRotation = transform.localRotation;
        startPosition = transform.localPosition;
    }

    private void OnEnable()
    {
        health.Died += OnDied;
        health.Revived += OnRevived;
    }

    private void OnDisable()
    {
        health.Died -= OnDied;
        health.Revived -= OnRevived;
    }

    private void OnDied()
    {
        StartCoroutine(FallOver());
    }

    private void OnRevived()
    {
        StopAllCoroutines();
        transform.localRotation = startRotation;
        transform.localPosition = startPosition;
    }

    private IEnumerator FallOver()
    {
        Quaternion from = transform.localRotation;
        Quaternion to = from * Quaternion.Euler(-90f, 0f, 0f);

        for (float t = 0f; t < 1f; t += Time.deltaTime / fallTime)
        {
            transform.localRotation = Quaternion.Slerp(from, to, t);
            yield return null;
        }
        transform.localRotation = to;
    }
}
