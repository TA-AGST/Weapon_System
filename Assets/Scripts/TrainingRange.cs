using UnityEngine;
using UnityEngine.InputSystem;

// T resets the whole range: score, bullet holes, all dummies and the ammo.
public class TrainingRange : MonoBehaviour
{
    [SerializeField] private WeaponController weapon;

    private void Awake()
    {
        ScoreBoard.Reset();
    }

    private void Update()
    {
        Keyboard kb = Keyboard.current;
        if (kb != null && kb.tKey.wasPressedThisFrame) ResetRange();
    }

    private void ResetRange()
    {
        ScoreBoard.Reset();
        ImpactEffects.ClearHoles();

        foreach (RangeTarget board in FindObjectsByType<RangeTarget>(FindObjectsSortMode.None)) board.ResetBoard();
        foreach (Health h in FindObjectsByType<Health>(FindObjectsSortMode.None)) h.ResetHealth();
        weapon.RefillAll();
    }
}
