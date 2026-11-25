using UnityEngine;

/// <summary>Lapisan latar yang bergerak tanpa putus (tanah, pohon, bukit, gunung, awan).</summary>
public class ScrollingLayer : MonoBehaviour
{
    const float Extent = 13f; // area yang ditutup: -13 .. +13 unit (cukup untuk layar ultrawide)

    float factor, tileWidth;
    Transform[] tiles;
    SpriteRenderer[] renderers;

    /// <param name="scale">Skala sprite (mis. 0.5 untuk awan jauh yang lebih kecil).</param>
    /// <param name="xShift">Geser posisi awal (nilai negatif, dalam unit) supaya pola tiap lapisan tidak sejajar.</param>
    public static ScrollingLayer Create(string name, Sprite sprite, float y, int order, float factor, float scale = 1f, float xShift = 0f)
    {
        var go = new GameObject(name);
        var l = go.AddComponent<ScrollingLayer>();
        l.factor = factor;
        l.tileWidth = sprite.bounds.size.x * scale;
        int count = Mathf.CeilToInt(Extent * 2f / l.tileWidth) + 2;
        l.tiles = new Transform[count];
        l.renderers = new SpriteRenderer[count];
        for (int i = 0; i < count; i++)
        {
            var t = new GameObject("Tile" + i);
            t.transform.SetParent(go.transform, false);
            t.transform.localPosition = new Vector3(-Extent + xShift + i * l.tileWidth, y, 0f);
            t.transform.localScale = new Vector3(scale, scale, 1f);
            var sr = t.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = order;
            l.tiles[i] = t.transform;
            l.renderers[i] = sr;
        }
        return l;
    }

    /// <summary>Mewarnai seluruh lapisan (dipakai untuk suasana siang / senja / malam).</summary>
    public void SetTint(Color c)
    {
        for (int i = 0; i < renderers.Length; i++) renderers[i].color = c;
    }

    void Update()
    {
        var gm = GameManager.Instance;
        if (gm == null) return;
        float dx = gm.WorldSpeed * factor * Time.deltaTime;
        float wrap = tileWidth * tiles.Length;
        foreach (var t in tiles)
        {
            var p = t.localPosition;
            p.x -= dx;
            if (p.x < -Extent - tileWidth * 0.5f) p.x += wrap;
            t.localPosition = p;
        }
    }
}
