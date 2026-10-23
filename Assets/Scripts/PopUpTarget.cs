using UnityEngine;

// A dummy that flips up for a few seconds, then lies down again. It can only be hit while it is up.
public class PopUpTarget : MonoBehaviour
{
    [SerializeField] private Health health;
    [SerializeField] private float upTime = 3f;
    [SerializeField] private float downTime = 2f;
    [SerializeField] private float flipTime = 0.25f;

    private Collider[] colliders;
    private float angle = -90f;      // -90 = lying down, 0 = standing
    private float timer;
    private bool isUp;

    private void Awake()
    {
        colliders = GetComponentsInChildren<Collider>();
        timer = Random.Range(0f, downTime);     // so several targets do not all pop up together
    }

    private void Update()
    {
        if (health != null && health.IsDead) return;

        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            isUp = !isUp;
            timer = isUp ? upTime : downTime;
        }

        angle = Mathf.MoveTowards(angle, isUp ? 0f : -90f, Time.deltaTime * 90f / flipTime);
        transform.localRotation = Quaternion.Euler(angle, 0f, 0f);

        bool hittable = angle > -45f;
        foreach (Collider c in colliders) c.enabled = hittable;
    }
}
