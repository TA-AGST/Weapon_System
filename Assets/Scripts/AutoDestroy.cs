using UnityEngine;

// Removes an effect after a few seconds.
public class AutoDestroy : MonoBehaviour
{
    [SerializeField] private float lifetime = 3f;

    private void Start()
    {
        Destroy(gameObject, lifetime);
    }
}
