using UnityEngine;
using UnityEngine.UI;

// Ammo, weapon name, fire mode, zero distance, the crosshair that grows with the spread, hit markers and the score.
public class WeaponHUD : MonoBehaviour
{
    [SerializeField] private WeaponController weapon;

    [Header("Weapon info")]
    [SerializeField] private Text nameText;
    [SerializeField] private Text ammoText;
    [SerializeField] private Text modeText;
    [SerializeField] private GameObject reloadRoot;
    [SerializeField] private RectTransform reloadFill;

    [Header("Crosshair")]
    [SerializeField] private GameObject crosshairRoot;
    [SerializeField] private RectTransform crossUp;
    [SerializeField] private RectTransform crossDown;
    [SerializeField] private RectTransform crossLeft;
    [SerializeField] private RectTransform crossRight;
    [SerializeField] private float screenHeight = 1080f;

    [Header("Hit marker and score")]
    [SerializeField] private Graphic hitMarker;
    [SerializeField] private Text scoreText;

    private float markerTime;
    private Color markerColor = Color.white;

    private void OnEnable()
    {
        ScoreBoard.HitRegistered += OnHit;
    }

    private void OnDisable()
    {
        ScoreBoard.HitRegistered -= OnHit;
    }

    private void OnHit(bool head)
    {
        markerTime = 0.18f;
        markerColor = head ? new Color(1f, 0.25f, 0.2f) : Color.white;
    }

    private void Update()
    {
        UpdateWeaponInfo();
        UpdateCrosshair();
        UpdateHitMarker();
        UpdateScore();
    }

    private void UpdateWeaponInfo()
    {
        WeaponSlot slot = weapon.Current;
        nameText.text = (weapon.CurrentIndex + 1) + "  " + slot.data.displayName.ToUpper();
        if (slot.data.isMelee)
        {
            ammoText.text = "-";
            modeText.text = "MELEE   |   LMB to slash";
            reloadRoot.SetActive(false);
            return;
        }
        ammoText.text = slot.magazine + " / " + slot.reserve;

        string optic = slot.data.optic == OpticType.IronSights ? "IRON SIGHTS"
                     : slot.data.optic == OpticType.RedDot ? "RED DOT"
                     : weapon.Zoom.ToString("0") + "x SCOPE";
        modeText.text = weapon.FireModeLabel + "   |   " + optic + "   |   ZERO " + weapon.ZeroDistance.ToString("0") + " m";

        reloadRoot.SetActive(weapon.IsReloading);
        if (weapon.IsReloading) reloadFill.localScale = new Vector3(weapon.ReloadProgress, 1f, 1f);
    }

    // The four lines move apart as the spread grows, so you can see how accurate the next shot is.
    private void UpdateCrosshair()
    {
        crosshairRoot.SetActive(!weapon.ScopedView && !weapon.Current.data.isMelee);

        float pixelsPerTan = (screenHeight * 0.5f) / Mathf.Tan(weapon.Fov * 0.5f * Mathf.Deg2Rad);
        float gap = Mathf.Tan(weapon.Spread * Mathf.Deg2Rad) * pixelsPerTan + 6f;

        crossUp.anchoredPosition = new Vector2(0f, gap);
        crossDown.anchoredPosition = new Vector2(0f, -gap);
        crossLeft.anchoredPosition = new Vector2(-gap, 0f);
        crossRight.anchoredPosition = new Vector2(gap, 0f);
    }

    private void UpdateHitMarker()
    {
        markerTime -= Time.deltaTime;
        Color c = markerColor;
        c.a = Mathf.Clamp01(markerTime / 0.18f);
        hitMarker.color = c;
    }

    private void UpdateScore()
    {
        scoreText.text = "SCORE  " + ScoreBoard.Points + "     last  +" + ScoreBoard.LastPoints
            + "\nShots " + ScoreBoard.Shots + "    Hits " + ScoreBoard.Hits + "    Accuracy " + (ScoreBoard.Accuracy * 100f).ToString("0") + "%"
            + "\nHeadshots " + ScoreBoard.Headshots;
    }
}
