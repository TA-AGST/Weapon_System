using UnityEngine;
using UnityEngine.UI;

// What you see when you look through a scope: a round lens, a reticle with hold-over marks,
// a range readout and the breath bar.
public class ScopeOverlay : MonoBehaviour
{
    [SerializeField] private WeaponController weapon;
    [SerializeField] private GameObject root;
    [SerializeField] private RawImage lens;
    [SerializeField] private RectTransform[] marks;           // one per distance below
    [SerializeField] private Text[] markLabels;
    private static readonly float[] markDistances = { 200f, 400f, 600f, 800f, 1000f };   // metres, shown as hundreds
    [SerializeField] private Text rangeText;
    [SerializeField] private Text zoomText;
    [SerializeField] private RectTransform breathFill;
    [SerializeField] private float screenHeight = 1080f;     // the canvas height the HUD is built for

    private void Awake()
    {
        lens.texture = MakeLensTexture(1024);
        root.SetActive(false);
    }

    // Everything that is painted on the glass, drawn once into a texture: the dark edge of the tube,
    // a thin metal ring, a soft vignette, a faint glare and a duplex reticle (thick posts outside, thin lines in the middle).
    private static Texture2D MakeLensTexture(int size)
    {
        var pixels = new Color32[size * size];
        float radius = size * 0.5f;
        float centre = (size - 1) * 0.5f;

        const float postHalf = 3.5f;       // thick post half width (pixels of the texture)
        const float lineHalf = 0.8f;       // thin line half width
        float postStart = radius * 0.16f;  // the thin part in the middle ends here
        float postEnd = radius * 0.94f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float px = x - centre;
                float py = y - centre;
                float r = Mathf.Sqrt(px * px + py * py) / radius;

                // base: black alpha of the tube, vignette towards the edge
                float alpha;
                Color ink = Color.black;
                if (r >= 0.96f) alpha = 1f;
                else if (r >= 0.94f) { alpha = 1f; ink = new Color(0.2f, 0.2f, 0.22f); }       // metal ring
                else
                {
                    float v = Mathf.Clamp01((r - 0.55f) / 0.39f);
                    alpha = 0.65f * v * v;
                }

                // glare spot, upper left
                float glare = Mathf.Clamp01(1f - Vector2.Distance(new Vector2(px, py) / radius, new Vector2(-0.42f, 0.42f)) / 0.22f) * 0.09f;
                if (glare > 0f && r < 0.94f)
                {
                    float outA = glare + alpha * (1f - glare);
                    ink = Color.Lerp(Color.black, Color.white, glare / outA);
                    alpha = outA;
                }

                // reticle (black lines drawn over the glass)
                if (r < 0.94f)
                {
                    float line = Mathf.Max(Coverage(lineHalf, Mathf.Abs(px)), Coverage(lineHalf, Mathf.Abs(py)));

                    bool bottomPost = py < -postStart && py > -postEnd;
                    bool sidePost = Mathf.Abs(px) > postStart && Mathf.Abs(px) < postEnd;
                    float post = Mathf.Max(bottomPost ? Coverage(postHalf, Mathf.Abs(px)) : 0f, sidePost ? Coverage(postHalf, Mathf.Abs(py)) : 0f);

                    float cover = Mathf.Max(line, post) * 0.95f;
                    if (cover > 0f)
                    {
                        float outA = cover + alpha * (1f - cover);
                        ink = Color.Lerp(ink, Color.black, cover / outA);
                        alpha = outA;
                    }
                }

                var c = ink;
                c.a = alpha;
                pixels[y * size + x] = c;
            }
        }

        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;
        texture.SetPixels32(pixels);
        texture.Apply();
        return texture;
    }

    // How much of a pixel is covered by a line of the given half width (gives smooth edges).
    private static float Coverage(float halfWidth, float distance)
    {
        return Mathf.Clamp01(halfWidth - distance + 0.5f);
    }

    private void Update()
    {
        bool show = weapon.ScopedView;
        if (root.activeSelf != show) root.SetActive(show);
        if (!show) return;

        UpdateMarks();
        UpdateReadouts();
    }

    // The marks sit below the centre by the bullet drop at each distance, so you put the mark on the target.
    private void UpdateMarks()
    {
        float pixelsPerTan = (screenHeight * 0.5f) / Mathf.Tan(weapon.Fov * 0.5f * Mathf.Deg2Rad);
        float speed = weapon.Current.data.muzzleVelocity;

        for (int i = 0; i < marks.Length; i++)
        {
            float angle = Ballistics.HoldoverAngle(speed, weapon.ZeroDistance, markDistances[i]);
            float y = -Mathf.Tan(angle) * pixelsPerTan;

            bool visible = Mathf.Abs(y) < screenHeight * 0.42f && Mathf.Abs(y) > 4f;
            marks[i].gameObject.SetActive(visible);
            marks[i].anchoredPosition = new Vector2(0f, y);
            markLabels[i].text = (markDistances[i] / 100f).ToString("0");
        }
    }

    private void UpdateReadouts()
    {
        Transform cam = weapon.Look.Cam.transform;
        if (Physics.Raycast(cam.position, cam.forward, out RaycastHit hit, 1500f, ~0, QueryTriggerInteraction.Ignore))
            rangeText.text = hit.distance.ToString("0") + " m";
        else
            rangeText.text = "-- m";

        zoomText.text = weapon.Zoom.ToString("0") + "x   zero " + weapon.ZeroDistance.ToString("0") + " m";
        breathFill.localScale = new Vector3(weapon.Breath01, 1f, 1f);
    }
}
