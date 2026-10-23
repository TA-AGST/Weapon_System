using UnityEngine;

// Slides a dummy left and right (like a target on a rail). It stops when the dummy is dead.
public class MovingTarget : MonoBehaviour
{
    [SerializeField] private Health health;
    [SerializeField] private float halfDistance = 6f;
    [SerializeField] private float speed = 2.5f;

    private Vector3 start;
    private float time;

    private void Awake()
    {
        start = transform.position;
    }

    private void Update()
    {
        if (health != null && health.IsDead) return;

        time += Time.deltaTime * speed;
        transform.position = start + Vector3.right * (Mathf.PingPong(time, halfDistance * 2f) - halfDistance);
    }
}
