using UnityEngine;

// A short flash at the end of the barrel: a glowing ball and a light that are on for a few frames.
public class MuzzleFlash : MonoBehaviour
{
    [SerializeField] private GameObject flashObject;
    [SerializeField] private float showTime = 0.04f;

    private float timer;

    private void Awake()
    {
        flashObject.SetActive(false);
    }

    public void Play()
    {
        timer = showTime;
        flashObject.transform.localScale = Vector3.one * Random.Range(0.8f, 1.3f);
        flashObject.SetActive(true);
    }

    private void Update()
    {
        if (timer <= 0f) return;

        timer -= Time.deltaTime;
        if (timer <= 0f) flashObject.SetActive(false);
    }
}
