using System.Collections.Generic;
using UnityEngine;

// Spawns sparks / blood and bullet holes where a bullet hits. Old bullet holes are removed so there are never too many.
public class ImpactEffects : MonoBehaviour
{
    [SerializeField] private GameObject sparksPrefab;
    [SerializeField] private GameObject bloodPrefab;
    [SerializeField] private GameObject holePrefab;
    [SerializeField] private GameObject boardHolePrefab;     // light rim with a dark middle, so it shows on every ring colour
    [SerializeField] private int maxHoles = 120;

    private static ImpactEffects instance;
    private readonly Queue<GameObject> holes = new Queue<GameObject>();

    private void Awake()
    {
        instance = this;
    }

    public static void Spawn(RaycastHit hit, bool flesh)
    {
        if (instance == null) return;

        Quaternion facing = Quaternion.FromToRotation(Vector3.up, hit.normal);
        GameObject fx = flesh ? instance.bloodPrefab : instance.sparksPrefab;
        if (fx != null) Instantiate(fx, hit.point, facing);

        if (flesh) return;

        RangeTarget board = hit.collider.GetComponentInParent<RangeTarget>();
        if (board != null && instance.boardHolePrefab != null)
        {
            GameObject mark = instance.AddHole(instance.boardHolePrefab, board.FacePoint(hit.point) + hit.normal * 0.002f, facing);
            board.AddHole(mark);
        }
        else if (instance.holePrefab != null)
        {
            instance.AddHole(instance.holePrefab, hit.point + hit.normal * 0.003f, facing);   // a hair above the surface so it does not flicker
        }
    }

    public static void ClearHoles()
    {
        if (instance == null) return;
        foreach (GameObject hole in instance.holes) if (hole != null) Destroy(hole);
        instance.holes.Clear();
    }

    private GameObject AddHole(GameObject prefab, Vector3 position, Quaternion facing)
    {
        GameObject hole = Instantiate(prefab, position, facing);
        holes.Enqueue(hole);

        while (holes.Count > maxHoles)
        {
            GameObject oldest = holes.Dequeue();
            if (oldest != null) Destroy(oldest);
        }
        return hole;
    }
}
