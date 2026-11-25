using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Pembantu tampilan antarmuka (IMGUI): font sistem, tekstur sudut membulat (9-slice),
/// serta gaya panel dan tombol dengan keadaan normal / hover / pressed.
/// Hanya memakai 2 jenis font: Title (judul) dan Body (teks isi).
/// </summary>
public static class UiKit
{
    public static Font TitleFont, BodyFont;
    static readonly Dictionary<string, Texture2D> cache = new Dictionary<string, Texture2D>();

    public static void Init()
    {
        if (BodyFont != null) return;
        TitleFont = Font.CreateDynamicFontFromOSFont(new[] { "Arial Black", "Impact", "Trebuchet MS", "Verdana", "Arial" }, 32);
        BodyFont = Font.CreateDynamicFontFromOSFont(new[] { "Trebuchet MS", "Segoe UI", "Verdana", "Arial" }, 24);
    }

    /// <summary>Tekstur persegi membulat kecil yang aman diregangkan lewat GUIStyle.border (9-slice).</summary>
    public static Texture2D Rounded(Color fill, Color border, int radius, int borderPx)
    {
        string key = fill + "|" + border + "|" + radius + "|" + borderPx;
        Texture2D t;
        if (cache.TryGetValue(key, out t) && t != null) return t;

        int size = radius * 2 + 4;
        t = new Texture2D(size, size, TextureFormat.RGBA32, false);
        t.filterMode = FilterMode.Bilinear;
        t.wrapMode = TextureWrapMode.Clamp;
        var px = new Color32[size * size];
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float fx = x + 0.5f, fy = y + 0.5f;
                float cx = Mathf.Clamp(fx, radius, size - radius), cy = Mathf.Clamp(fy, radius, size - radius);
                float d = Mathf.Sqrt((fx - cx) * (fx - cx) + (fy - cy) * (fy - cy));
                float edge = radius - d;                       // > 0 di dalam bentuk
                Color c = Color.Lerp(border, fill, Mathf.Clamp01(edge - borderPx + 0.5f));
                c.a = Mathf.Clamp01(edge + 0.5f) * fill.a;
                px[y * size + x] = c;
            }
        t.SetPixels32(px);
        t.Apply();
        cache[key] = t;
        return t;
    }

    public static GUIStyle Panel(Color fill, Color border, int radius = 14, int borderPx = 4)
    {
        var st = new GUIStyle();
        st.normal.background = Rounded(fill, border, radius, borderPx);
        st.border = new RectOffset(radius + 1, radius + 1, radius + 1, radius + 1);
        return st;
    }

    public static GUIStyle Button(Color fill, Color hover, Color pressed, Color border, Color text, Font font, int radius = 12)
    {
        var st = new GUIStyle();
        st.normal.background = Rounded(fill, border, radius, 3);
        st.hover.background = Rounded(hover, border, radius, 3);
        st.active.background = Rounded(pressed, border, radius, 3);
        st.focused.background = st.normal.background;
        st.border = new RectOffset(radius + 1, radius + 1, radius + 1, radius + 1);
        st.normal.textColor = text;
        st.hover.textColor = text;
        st.active.textColor = text;
        st.focused.textColor = text;
        st.alignment = TextAnchor.MiddleCenter;
        st.fontStyle = FontStyle.Bold;
        st.font = font;
        st.wordWrap = true;
        st.padding = new RectOffset(14, 14, 6, 6);
        return st;
    }
}
