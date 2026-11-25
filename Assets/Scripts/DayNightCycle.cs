using UnityEngine;

/// <summary>
/// Langit gradasi, matahari, bulan, dan bintang. Suasana berubah mengikuti score:
/// siang -> senja -> malam -> fajar -> siang lagi (satu siklus = 50 score).
/// </summary>
public class DayNightCycle : MonoBehaviour
{
    // (score, kekuatan senja, kekuatan malam)
    static readonly float[,] Keys =
    {
        {  0f, 0f, 0f }, {  8f, 0f, 0f }, { 14f, 1f, 0f }, { 20f, 1f, 0f },
        { 26f, 1f, 1f }, { 34f, 1f, 1f }, { 40f, 1f, 0f }, { 44f, 1f, 0f }, { 50f, 0f, 0f }
    };
    const float CycleLength = 50f;

    SpriteRenderer duskSky, nightSky, stars, sun, moon;
    ScrollingLayer[] layers;
    float dusk01, night01;

    /// <summary>Seberapa gelap malam saat ini (0 = siang, 1 = malam penuh); dipakai untuk memadukan musik.</summary>
    public float Night01 { get { return night01; } }

    public static DayNightCycle Create(ScrollingLayer[] layers)
    {
        var go = new GameObject("DayNightCycle");
        var d = go.AddComponent<DayNightCycle>();
        d.layers = layers;

        var tr = go.transform;
        Sky(tr, "SkyDay", SpriteFactory.SkyGradient(new Color32(58, 144, 228, 255), new Color32(128, 204, 246, 255), new Color32(255, 238, 196, 255)), -30);
        d.duskSky = Sky(tr, "SkyDusk", SpriteFactory.SkyGradient(new Color32(64, 52, 132, 255), new Color32(226, 108, 120, 255), new Color32(255, 186, 100, 255)), -29);
        d.nightSky = Sky(tr, "SkyNight", SpriteFactory.SkyGradient(new Color32(6, 10, 38, 255), new Color32(24, 34, 84, 255), new Color32(58, 70, 124, 255)), -28);

        var st = new GameObject("Stars");
        st.transform.SetParent(tr, false);
        st.transform.position = new Vector3(0f, 2.4f, 0f);
        d.stars = st.AddComponent<SpriteRenderer>();
        d.stars.sprite = SpriteFactory.Stars();
        d.stars.sortingOrder = -27;

        d.sun = Body(tr, "Sun", SpriteFactory.Sun(), -26);
        d.moon = Body(tr, "Moon", SpriteFactory.Moon(), -26);

        d.Apply(0f, 0f);
        return d;
    }

    // satu lapisan langit penuh layar: 60 x 10 unit (y = -4 .. 6)
    static SpriteRenderer Sky(Transform parent, string name, Sprite s, int order)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = new Vector3(0f, 1f, 0f);
        go.transform.localScale = new Vector3(30f, 10f / 128f, 1f);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = s;
        sr.sortingOrder = order;
        return sr;
    }

    static SpriteRenderer Body(Transform parent, string name, Sprite s, int order)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = s;
        sr.sortingOrder = order;
        return sr;
    }

    static void Sample(float score, out float dusk, out float night)
    {
        float p = score % CycleLength;
        int last = Keys.GetLength(0) - 1;
        dusk = Keys[last, 1];
        night = Keys[last, 2];
        for (int i = 0; i < last; i++)
        {
            if (p >= Keys[i, 0] && p <= Keys[i + 1, 0])
            {
                float u = Mathf.InverseLerp(Keys[i, 0], Keys[i + 1, 0], p);
                u = u * u * (3f - 2f * u);
                dusk = Mathf.Lerp(Keys[i, 1], Keys[i + 1, 1], u);
                night = Mathf.Lerp(Keys[i, 2], Keys[i + 1, 2], u);
                return;
            }
        }
    }

    void Update()
    {
        var gm = GameManager.Instance;
        if (gm == null) return;

        float td, tn;
        Sample(gm.Score, out td, out tn);
        float step = 0.35f * Time.deltaTime;          // perubahan suasana dibuat halus
        dusk01 = Mathf.MoveTowards(dusk01, td, step);
        night01 = Mathf.MoveTowards(night01, tn, step);
        Apply(dusk01, night01);
    }

    void Apply(float dusk, float night)
    {
        float hx = GameManager.Instance != null ? GameManager.Instance.HalfWidth : 8.9f;

        duskSky.color = new Color(1f, 1f, 1f, dusk);
        nightSky.color = new Color(1f, 1f, 1f, night);
        stars.color = new Color(1f, 1f, 1f, night * (0.8f + 0.2f * Mathf.Sin(Time.time * 2.1f)));

        // matahari turun saat senja lalu tenggelam di balik bukit; bulan naik saat malam
        float sunY = Mathf.Lerp(3.5f, -1.4f, dusk);
        sunY = Mathf.Lerp(sunY, -6.5f, night);
        sun.transform.position = new Vector3(hx * 0.45f, sunY, 0f);
        sun.color = Color.Lerp(new Color(1f, 0.97f, 0.75f), new Color(1f, 0.52f, 0.24f), dusk);
        moon.transform.position = new Vector3(-hx * 0.4f, Mathf.Lerp(-6f, 3.6f, night), 0f);

        // warna seluruh lapisan latar mengikuti waktu
        Color tint = Color.Lerp(Color.white, new Color(1f, 0.74f, 0.64f), dusk);
        tint = Color.Lerp(tint, new Color(0.34f, 0.4f, 0.64f), night);
        for (int i = 0; i < layers.Length; i++) if (layers[i] != null) layers[i].SetTint(tint);
    }
}
