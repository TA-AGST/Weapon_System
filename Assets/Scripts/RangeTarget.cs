using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// A round scoring board. The closer the hit is to the centre, the more points (10 in the middle, 1 at the edge).
// After a hit the board stays for a moment so the mark can be seen, then disappears for a few seconds and comes back clean.
public class RangeTarget : MonoBehaviour
{
    [SerializeField] private float boardRadius = 0.5f;   // radius of the board in its own (unscaled) units
    [SerializeField] private float faceOffset = 0.04f;   // how far the painted rings stick out in front of the collider
    [SerializeField] private float showMarkTime = 0.4f;
    [SerializeField] private float hiddenTime = 5f;

    private Renderer[] renderers;
    private Collider[] colliders;
    private readonly List<GameObject> holes = new List<GameObject>();
    private bool waiting;

    private void Awake()
    {
        renderers = GetComponentsInChildren<Renderer>();
        colliders = GetComponentsInChildren<Collider>();
    }

    // 'point' is in world space. The board faces along its local -Z axis.
    public int Score(Vector3 point)
    {
        Vector3 local = transform.InverseTransformPoint(point);
        float distance = new Vector2(local.x, local.y).magnitude;
        if (distance > boardRadius) return 0;

        return Mathf.Clamp(Mathf.CeilToInt(10f * (1f - distance / boardRadius)), 1, 10);
    }

    // The hit point moved onto the painted face, so a bullet hole is not hidden inside the rings.
    public Vector3 FacePoint(Vector3 point)
    {
        Vector3 local = transform.InverseTransformPoint(point);
        local.z = -faceOffset;
        return transform.TransformPoint(local);
    }

    public void AddHole(GameObject hole)
    {
        holes.Add(hole);
    }

    public void Shot()
    {
        if (!waiting) StartCoroutine(HideForAWhile());
    }

    private IEnumerator HideForAWhile()
    {
        waiting = true;
        yield return new WaitForSeconds(showMarkTime);

        SetVisible(false);
        yield return new WaitForSeconds(hiddenTime);

        foreach (GameObject hole in holes) if (hole != null) Destroy(hole);
        holes.Clear();
        SetVisible(true);
        waiting = false;
    }

    // Used by the T key: bring the board back at once.
    public void ResetBoard()
    {
        StopAllCoroutines();
        foreach (GameObject hole in holes) if (hole != null) Destroy(hole);
        holes.Clear();
        SetVisible(true);
        waiting = false;
    }

    private void SetVisible(bool visible)
    {
        foreach (Renderer r in renderers) r.enabled = visible;
        foreach (Collider c in colliders) c.enabled = visible;
        foreach (GameObject hole in holes) if (hole != null) hole.SetActive(visible);
    }
}
