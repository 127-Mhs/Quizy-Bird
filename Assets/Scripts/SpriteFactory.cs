using System;
using UnityEngine;

/// <summary>
/// Membuat seluruh sprite (burung, pipa, tanah, bukit, awan) secara prosedural
/// sehingga game tidak membutuhkan file gambar eksternal.
/// </summary>
public static class SpriteFactory
{
    public const float PPU = 32f;

    static Color C(int r, int g, int b, int a = 255) { return new Color32((byte)r, (byte)g, (byte)b, (byte)a); }
    static readonly Color Clear = new Color(0, 0, 0, 0);
    static readonly Color Outline = new Color32(84, 56, 71, 255);

    // nilai <= 1 berarti titik berada di dalam elips
    static float E(float x, float y, float cx, float cy, float rx, float ry)
    {
        float dx = (x - cx) / rx, dy = (y - cy) / ry;
        return dx * dx + dy * dy;
    }

    static Sprite Make(int w, int h, Func<int, int, Color> px)
    {
        return MakeF(w, h, px, FilterMode.Point);
    }

    static Sprite MakeF(int w, int h, Func<int, int, Color> px, FilterMode filter)
    {
        var cols = new Color32[w * h];
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                cols[y * w + x] = px(x, y);
        return FromPixels(w, h, cols, PPU, filter);
    }

    static Sprite FromPixels(int w, int h, Color32[] cols, float ppu, FilterMode filter)
    {
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.filterMode = filter;
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.SetPixels32(cols);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), ppu);
    }

    // lingkaran terisi pada buffer piksel (dipakai untuk menggambar pohon dan asap)
    static void Stamp(Color32[] cols, int w, int h, float cx, float cy, float r, Color32 c)
    {
        int x0 = Mathf.Max(0, Mathf.FloorToInt(cx - r)), x1 = Mathf.Min(w - 1, Mathf.CeilToInt(cx + r));
        int y0 = Mathf.Max(0, Mathf.FloorToInt(cy - r)), y1 = Mathf.Min(h - 1, Mathf.CeilToInt(cy + r));
        for (int y = y0; y <= y1; y++)
            for (int x = x0; x <= x1; x++)
            {
                float dx = x + 0.5f - cx, dy = y + 0.5f - cy;
                if (dx * dx + dy * dy <= r * r) cols[y * w + x] = c;
            }
    }

    // elips terisi pada buffer piksel
    static void FillE(Color32[] cols, int w, int h, float cx, float cy, float rx, float ry, Color32 c)
    {
        int x0 = Mathf.Max(0, Mathf.FloorToInt(cx - rx)), x1 = Mathf.Min(w - 1, Mathf.CeilToInt(cx + rx));
        int y0 = Mathf.Max(0, Mathf.FloorToInt(cy - ry)), y1 = Mathf.Min(h - 1, Mathf.CeilToInt(cy + ry));
        for (int y = y0; y <= y1; y++)
            for (int x = x0; x <= x1; x++)
                if (E(x + 0.5f, y + 0.5f, cx, cy, rx, ry) <= 1f) cols[y * w + x] = c;
    }

    // garis tebal yang jari-jarinya mengecil dari r0 ke r1 (dipakai untuk sayap, bulu, batang)
    static void Taper(Color32[] cols, int w, int h, float x0, float y0, float r0, float x1, float y1, float r1, Color32 c)
    {
        float len = Mathf.Max(1f, Mathf.Sqrt((x1 - x0) * (x1 - x0) + (y1 - y0) * (y1 - y0)));
        int steps = Mathf.CeilToInt(len * 2f);
        for (int i = 0; i <= steps; i++)
        {
            float u = i / (float)steps;
            Stamp(cols, w, h, Mathf.Lerp(x0, x1, u), Mathf.Lerp(y0, y1, u), Mathf.Lerp(r0, r1, u), c);
        }
    }

    // garis tepi 1 piksel di sekeliling bagian yang terisi
    static void AddOutline(Color32[] cols, int w, int h, Color32 oc)
    {
        var src = (Color32[])cols.Clone();
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                if (src[y * w + x].a != 0) continue;
                bool near = (x > 0 && src[y * w + x - 1].a > 0) || (x < w - 1 && src[y * w + x + 1].a > 0) ||
                            (y > 0 && src[(y - 1) * w + x].a > 0) || (y < h - 1 && src[(y + 1) * w + x].a > 0);
                if (near) cols[y * w + x] = oc;
            }
    }

    // cermin horizontal (semua burung digambar menghadap kanan lalu dibalik bila perlu)
    static void FlipX(Color32[] cols, int w, int h)
    {
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w / 2; x++)
            {
                int a = y * w + x, b = y * w + (w - 1 - x);
                Color32 t = cols[a]; cols[a] = cols[b]; cols[b] = t;
            }
    }


    public static Sprite Pixel()
    {
        var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
    }

    public static Texture2D WhiteTexture()
    {
        var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();
        return tex;
    }

    // ---------------- BURUNG CENDERAWASIH (52 x 30 px, 4 posisi sayap) ----------------
    // Kepala kuning, dada hijau zamrud, badan cokelat kemerahan, bulu samping keemasan,
    // dan dua kawat ekor yang melengkung. Menghadap ke kanan.
    public static Sprite[] BirdFrames()
    {
        Sprite up = Cendrawasih(13f, 27f), upMid = Cendrawasih(11f, 21f), mid = Cendrawasih(10f, 15.5f), down = Cendrawasih(13f, 5f);
        return new[] { up, upMid, mid, down, mid, upMid };   // satu siklus kepakan
    }

    static Sprite Cendrawasih(float tx, float ty)
    {
        const int W = 52, H = 30;
        var cols = new Color32[W * H];
        Color32 maroon = C(146, 68, 40), maroonDark = C(104, 46, 32), belly = C(226, 164, 98);
        Color32 yellow = C(252, 214, 58), emerald = C(20, 156, 88);
        Color32 gold = C(255, 172, 40), goldLight = C(255, 218, 100);
        Color32 beak = C(176, 206, 220), wire = C(48, 34, 38), wingC = C(96, 48, 32), dark = C(34, 24, 26);

        // dua kawat ekor yang melengkung
        float bend = Mathf.Sin(Mathf.PI * 0.8f);
        for (int i = 0; i <= 40; i++)
        {
            float u = i / 40f, x = 12f - 9.5f * u, d = 5f * Mathf.Sin(u * Mathf.PI * 0.8f);
            Stamp(cols, W, H, x, 12f - d, 0.7f, wire);
            Stamp(cols, W, H, x, 14f + d, 0.7f, wire);
        }
        Stamp(cols, W, H, 2.8f, 12f - 5f * bend, 1.4f, wire);
        Stamp(cols, W, H, 2.8f, 14f + 5f * bend, 1.4f, wire);

        // bulu samping keemasan dan pangkal ekor
        Taper(cols, W, H, 17f, 12f, 2.6f, 4f, 3f, 0.6f, gold);
        Taper(cols, W, H, 17f, 12f, 2.2f, 2.5f, 8f, 0.6f, goldLight);
        Taper(cols, W, H, 16f, 10f, 2.0f, 7f, 2.2f, 0.5f, goldLight);
        FillE(cols, W, H, 11f, 13f, 6f, 2.4f, maroonDark);

        // badan, dada zamrud, kepala kuning, paruh, mata
        FillE(cols, W, H, 25f, 14f, 13f, 7.5f, maroon);
        FillE(cols, W, H, 26f, 11f, 10.5f, 4.2f, belly);
        FillE(cols, W, H, 35f, 12.6f, 4.2f, 3.6f, emerald);
        FillE(cols, W, H, 36.5f, 17f, 6f, 5.4f, yellow);
        for (int x = 41; x <= 48; x++)
        {
            float half = 2.1f - (x - 41) * 0.25f;
            for (int y = 0; y < H; y++)
                if (Mathf.Abs(y + 0.5f - 16.4f) <= half) cols[y * W + x] = beak;
        }
        Stamp(cols, W, H, 39f, 18.4f, 1.5f, dark);
        Stamp(cols, W, H, 39.5f, 19f, 0.6f, Color.white);

        // sayap (ujung bergerak sesuai frame) dengan ujung bulu keemasan
        Taper(cols, W, H, 26f, 15.5f, 4.4f, tx, ty, 1.2f, wingC);
        Taper(cols, W, H, Mathf.Lerp(26f, tx, 0.55f), Mathf.Lerp(15.5f, ty, 0.55f), 2f, tx, ty, 0.8f, gold);

        AddOutline(cols, W, H, Outline);
        return FromPixels(W, H, cols, PPU, FilterMode.Point);
    }

    // ---------------- PIPA: batang kayu berlumut ----------------
    static readonly Color PipeBase = new Color32(150, 102, 58, 255);
    static readonly Color PipeLight = new Color32(198, 148, 88, 255);
    static readonly Color PipeDark = new Color32(104, 68, 40, 255);
    static readonly Color Moss = new Color32(92, 164, 62, 255);
    static readonly Color MossDark = new Color32(58, 120, 44, 255);
    static readonly Color GrassLight = new Color32(136, 204, 74, 255);
    static readonly Color GrassBase = new Color32(88, 160, 60, 255);

    static Color PipeShade(int x, int width)
    {
        if (x < 2 || x >= width - 2) return Outline;
        int i = x - 2, inner = width - 4;
        if (i < 4) return PipeLight;
        if (i < 8) return PipeBase;
        if (i > inner - 8) return PipeDark;
        if (i % 9 == 4) return PipeDark;          // serat kayu
        return PipeBase;
    }

    /// <summary>Badan batang 48 x 4 px (warna hanya bergantung pada x, jadi aman diregangkan vertikal).</summary>
    public static Sprite PipeBody()
    {
        return Make(48, 4, (x, y) => PipeShade(x, 48));
    }

    /// <summary>Ujung batang berlumut 52 x 24 px (0,75 unit tinggi), simetris atas-bawah.</summary>
    public static Sprite PipeCap()
    {
        return Make(52, 24, (x, y) =>
        {
            if (y < 2 || y >= 22 || x < 2 || x >= 50) return Outline;
            if (y < 7 || y >= 17) return (y == 6 || y == 17) ? MossDark : Moss;
            return PipeShade(x, 52);
        });
    }

    // ---------------- TANAH HUTAN (64 x 32 px = 2 x 1 unit) ----------------
    public static Sprite Ground()
    {
        return Make(64, 32, (x, y) =>
        {
            if (y == 31) return Outline;
            if (y >= 29) return GrassLight;
            if (y >= 27) return GrassBase;
            if (y == 26) return Outline;
            bool stripe = (((x + (31 - y)) / 8) % 2) == 0;
            return stripe ? C(122, 84, 54) : C(138, 98, 62);
        });
    }

    // ---------------- AWAN (256 x 64 px, 2 awan per tile) ----------------
    public static Sprite Clouds()
    {
        return Make(256, 64, (x, y) =>
        {
            float fx = x + 0.5f, fy = y + 0.5f;
            foreach (float cx in new[] { 64f, 184f })
            {
                if (E(fx, fy, cx, 24f, 30f, 12f) <= 1f ||
                    E(fx, fy, cx - 18f, 22f, 18f, 11f) <= 1f ||
                    E(fx, fy, cx + 20f, 23f, 20f, 11f) <= 1f ||
                    E(fx, fy, cx + 2f, 32f, 16f, 11f) <= 1f)
                    return C(255, 255, 255, 235);
            }
            return Clear;
        });
    }

    // =====================================================================
    //  LATAR BELAKANG: langit, matahari/bulan/bintang, pegunungan bersalju, hutan tropis, kampung Honai
    // =====================================================================

    /// <summary>Gradasi langit vertikal (2 x 128 px, bilinear). Bawah = cakrawala, atas = langit.</summary>
    public static Sprite SkyGradient(Color top, Color mid, Color bottom)
    {
        const int H = 128;
        var cols = new Color32[2 * H];
        for (int y = 0; y < H; y++)
        {
            float t = y / (float)(H - 1);
            Color c = t < 0.45f ? Color.Lerp(bottom, mid, t / 0.45f) : Color.Lerp(mid, top, (t - 0.45f) / 0.55f);
            cols[y * 2] = c;
            cols[y * 2 + 1] = c;
        }
        return FromPixels(2, H, cols, 1f, FilterMode.Bilinear);
    }

    public static Sprite Sun()
    {
        const int S = 128;
        return MakeF(S, S, (x, y) =>
        {
            float dx = x + 0.5f - S * 0.5f, dy = y + 0.5f - S * 0.5f;
            float r = Mathf.Sqrt(dx * dx + dy * dy);
            if (r <= 22f) return C(255, 246, 190);
            if (r <= 25f) return C(255, 226, 120);
            float g = Mathf.Clamp01(1f - (r - 25f) / 39f);
            return new Color(1f, 0.9f, 0.5f, g * g * 0.55f);
        }, FilterMode.Bilinear);
    }

    public static Sprite Moon()
    {
        const int S = 96;
        return MakeF(S, S, (x, y) =>
        {
            float fx = x + 0.5f, fy = y + 0.5f;
            float dx = fx - 48f, dy = fy - 48f;
            float r = Mathf.Sqrt(dx * dx + dy * dy);
            if (r <= 18f)
            {
                if (E(fx, fy, 41f, 54f, 4f, 4f) <= 1f || E(fx, fy, 55f, 42f, 5f, 5f) <= 1f || E(fx, fy, 50f, 58f, 2.5f, 2.5f) <= 1f)
                    return C(205, 208, 200);
                return C(244, 244, 232);
            }
            float g = Mathf.Clamp01(1f - (r - 18f) / 30f);
            return new Color(0.75f, 0.82f, 1f, g * g * 0.45f);
        }, FilterMode.Bilinear);
    }

    /// <summary>Taburan bintang (400 x 160 px, 16 px per unit = 25 x 10 unit).</summary>
    public static Sprite Stars()
    {
        const int W = 400, H = 160;
        var cols = new Color32[W * H];
        var rnd = new System.Random(11);
        for (int i = 0; i < 170; i++)
        {
            int x = rnd.Next(2, W - 3), y = rnd.Next(2, H - 3);
            int b = 170 + rnd.Next(0, 86);
            Color32 c = new Color32((byte)b, (byte)b, (byte)255, 255);
            cols[y * W + x] = c;
            if (rnd.Next(0, 4) == 0)
            {
                cols[y * W + x + 1] = c;
                cols[(y + 1) * W + x] = c;
                cols[(y + 1) * W + x + 1] = c;
            }
        }
        return FromPixels(W, H, cols, 16f, FilterMode.Point);
    }

    // tinggi punggung gunung (px) di kolom x; fungsi periodik sehingga tile menyambung mulus
    static float MountainH(int x, int w)
    {
        float t = x / (float)w * Mathf.PI * 2f;
        float h = 24f + 8f * Mathf.Sin(t * 2f + 0.5f) + 5f * Mathf.Sin(t * 5f + 2f) + 3f * Mathf.Sin(t * 11f);
        h += Mathf.Max(0f, 1f - Mathf.Abs(x - w * 0.5f) / (w * 0.20f)) * 70f;     // puncak tinggi bersalju
        h += Mathf.Max(0f, 1f - Mathf.Abs(x - w * 0.16f) / (w * 0.12f)) * 34f;
        h += Mathf.Max(0f, 1f - Mathf.Abs(x - w * 0.84f) / (w * 0.11f)) * 28f;
        return h;
    }

    /// <summary>Pegunungan jauh dengan puncak bersalju (512 x 128 px = 16 x 4 unit, bilinear).</summary>
    public static Sprite Mountains()
    {
        const int W = 512, H = 128;
        var cols = new Color32[W * H];
        Color dark = C(70, 92, 150);
        for (int x = 0; x < W; x++)
        {
            float h = MountainH(x, W);
            float slope = MountainH((x + 2) % W, W) - MountainH((x - 2 + W) % W, W);
            Color lit = slope > 0.5f ? C(128, 154, 206) : (slope < -0.5f ? C(84, 108, 166) : C(104, 130, 186));
            int top = Mathf.Min(H - 1, Mathf.FloorToInt(h));
            float snowLine = 72f + 4f * Mathf.Sin(x * 0.35f);
            for (int y = 0; y <= top; y++)
            {
                float k = Mathf.Clamp01(0.35f + 0.65f * y / Mathf.Max(1f, h));
                Color c = Color.Lerp(dark, lit, k);
                if (y >= snowLine) c = slope < -0.5f ? C(206, 222, 242) : C(250, 252, 255);   // salju
                else if (y > h - 2.5f) c = C(170, 188, 232);
                cols[y * W + x] = c;
            }
        }
        return FromPixels(W, H, cols, PPU, FilterMode.Bilinear);
    }

    /// <summary>Siluet hutan jauh yang kebiruan (256 x 96 px = 8 x 3 unit, tile mulus).</summary>
    public static Sprite FarForest()
    {
        const int W = 256, H = 96;
        var cols = new Color32[W * H];
        var rnd = new System.Random(5);
        Color32 deep = C(46, 104, 92), mid = C(62, 128, 104), hi = C(86, 152, 118);
        for (int x = 0; x < W; x++)
        {
            float t = x / (float)W * Mathf.PI * 2f;
            int top = Mathf.RoundToInt(40f + 4f * Mathf.Sin(t * 3f + 1f) + 3f * Mathf.Sin(t * 7f));
            for (int y = 0; y <= top; y++) cols[y * W + x] = deep;
        }
        for (int i = 0; i < 26; i++)
        {
            float cx = rnd.Next(0, W);
            float r = 7f + rnd.Next(0, 8);
            float cy = 40f + rnd.Next(0, 18);
            for (int k = -1; k <= 1; k++)     // diulang kiri/kanan supaya tile menyambung
            {
                Stamp(cols, W, H, cx + k * W, cy, r, mid);
                Stamp(cols, W, H, cx + k * W - r * 0.25f, cy + r * 0.3f, r * 0.6f, hi);
            }
        }
        return FromPixels(W, H, cols, PPU, FilterMode.Point);
    }

    /// <summary>Hutan tropis dekat: pohon besar berakar, tajuk berlapis, sulur, dan semak (256 x 128 px = 8 x 4 unit).</summary>
    public static Sprite MidForest()
    {
        const int W = 256, H = 128;
        var cols = new Color32[W * H];
        var rnd = new System.Random(9);
        Color32 trunk = C(92, 62, 40), trunkLight = C(124, 88, 56);
        Color32 dark = C(34, 94, 54), mid = C(50, 130, 66), light = C(88, 172, 86), vine = C(42, 112, 58);
        float[] tx = { 38f, 118f, 198f };
        float[] ty = { 82f, 94f, 76f };
        float[] tr = { 17f, 20f, 16f };

        for (int i = 0; i < tx.Length; i++)
        {
            float lean = (i - 1) * 2f;
            Taper(cols, W, H, tx[i], 0f, 4.6f, tx[i] + lean, ty[i], 3.2f, trunk);
            Taper(cols, W, H, tx[i] - 1.6f, 0f, 1.6f, tx[i] - 1.2f + lean, ty[i], 1.1f, trunkLight);
            Taper(cols, W, H, tx[i], 3f, 4f, tx[i] - 9f, 0f, 1.5f, trunk);      // akar papan
            Taper(cols, W, H, tx[i], 3f, 4f, tx[i] + 9f, 0f, 1.5f, trunk);

            for (int k = 0; k < 7; k++)
            {
                float a = k / 7f * Mathf.PI * 2f;
                Stamp(cols, W, H, tx[i] + Mathf.Cos(a) * tr[i] * 0.8f, ty[i] + Mathf.Sin(a) * tr[i] * 0.55f, tr[i] * 0.7f, dark);
            }
            for (int k = 0; k < 7; k++)
            {
                float a = k / 7f * Mathf.PI * 2f + 0.4f;
                Stamp(cols, W, H, tx[i] - 3f + Mathf.Cos(a) * tr[i] * 0.6f, ty[i] + 3f + Mathf.Sin(a) * tr[i] * 0.45f, tr[i] * 0.5f, mid);
            }
            for (int k = 0; k < 5; k++)
            {
                float a = k / 5f * Mathf.PI * 2f + 0.8f;
                Stamp(cols, W, H, tx[i] - 6f + Mathf.Cos(a) * tr[i] * 0.4f, ty[i] + 6f + Mathf.Sin(a) * tr[i] * 0.3f, tr[i] * 0.32f, light);
            }
            for (int j = 0; j < 6; j++)      // sulur yang menjuntai
            {
                float vx = tx[i] + ((float)rnd.NextDouble() - 0.5f) * tr[i] * 1.6f;
                float vy = ty[i] - tr[i] * 0.55f;
                float len = 10f + (float)rnd.NextDouble() * 16f;
                Taper(cols, W, H, vx, vy, 0.8f, vx, vy - len, 0.5f, vine);
            }
        }

        // semak dan rerumputan di dasar
        for (int n = 0; n < 24; n++)
        {
            float cx = rnd.Next(0, W);
            float r = 6f + rnd.Next(0, 7);
            float cy = 3f + rnd.Next(0, 8);
            for (int k = -1; k <= 1; k++)
            {
                Stamp(cols, W, H, cx + k * W, cy, r, dark);
                Stamp(cols, W, H, cx + k * W - r * 0.2f, cy + r * 0.3f, r * 0.55f, mid);
            }
        }
        return FromPixels(W, H, cols, PPU, FilterMode.Point);
    }

    /// <summary>Kampung Honai (512 x 96 px = 16 x 3 unit): dua rumah adat Papua dan pagar kayu, hanya dekorasi latar.</summary>
    public static Sprite HonaiVillage()
    {
        const int W = 512, H = 96;
        var cols = new Color32[W * H];
        DrawFence(cols, W, H, 232, 322);
        DrawHonai(cols, W, H, 150f, 1f);
        DrawHonai(cols, W, H, 392f, 0.78f);
        AddOutline(cols, W, H, C(60, 38, 28));
        return FromPixels(W, H, cols, PPU, FilterMode.Point);
    }

    // Honai: dinding kayu bulat, atap jerami berbentuk kubah, tiang puncak, dan pintu kecil
    static void DrawHonai(Color32[] cols, int w, int h, float cx, float sc)
    {
        Color32 wood = C(158, 110, 64), woodDark = C(112, 74, 44), woodLight = C(184, 134, 80);
        Color32 thatchA = C(218, 182, 92), thatchB = C(192, 154, 68), thatchC = C(164, 126, 52);
        Color32 doorC = C(44, 28, 22);

        int icx = Mathf.RoundToInt(cx);
        float wallHalf = 26f * sc, wallH = 24f * sc, roofHalf = 35f * sc, roofH = 42f * sc;
        int baseY = 2;
        int wallTop = baseY + Mathf.RoundToInt(wallH);

        // dinding papan kayu
        for (int x = Mathf.RoundToInt(cx - wallHalf); x <= Mathf.RoundToInt(cx + wallHalf); x++)
        {
            if (x < 0 || x >= w) continue;
            float rel = (x - cx) / wallHalf;
            for (int y = baseY; y <= wallTop; y++)
            {
                Color32 c = wood;
                if ((x - icx) % 4 == 0) c = woodDark;
                else if (rel < -0.55f) c = woodLight;
                else if (rel > 0.5f) c = woodDark;
                cols[y * w + x] = c;
            }
        }

        // pintu
        int dx = Mathf.Max(2, Mathf.RoundToInt(5f * sc)), dh = Mathf.RoundToInt(17f * sc);
        for (int x = icx - dx; x <= icx + dx; x++)
            for (int y = baseY; y <= baseY + dh; y++)
            {
                float rx = (x - cx) / dx;
                if (y > baseY + dh - 3 && Mathf.Abs(rx) > 0.7f) continue;     // ujung atas melengkung
                if (x >= 0 && x < w) cols[y * w + x] = doorC;
            }

        // atap jerami berbentuk kubah
        int roofY0 = wallTop - 2;
        int roofTop = roofY0 + Mathf.RoundToInt(roofH);
        for (int y = roofY0; y <= roofTop; y++)
        {
            if (y < 0 || y >= h) continue;
            float u = (y - roofY0) / roofH;
            float hw = roofHalf * Mathf.Pow(Mathf.Max(0f, 1f - u), 0.8f);
            int xa = Mathf.RoundToInt(cx - hw), xb = Mathf.RoundToInt(cx + hw);
            bool band = ((y - roofY0) / 3) % 2 == 0;
            for (int x = xa; x <= xb; x++)
            {
                if (x < 0 || x >= w) continue;
                Color32 c = band ? thatchA : thatchB;
                if (x > cx + hw * 0.35f) c = band ? thatchB : thatchC;          // sisi kanan lebih gelap
                if (((x * 7 + y * 3) % 11) < 2) c = thatchC;                    // serat jerami
                cols[y * w + x] = c;
            }
        }
        for (int x = Mathf.RoundToInt(cx - roofHalf); x <= Mathf.RoundToInt(cx + roofHalf); x++)
            if (x >= 0 && x < w && roofY0 - 1 >= 0) cols[(roofY0 - 1) * w + x] = thatchC;

        // tiang kayu di puncak atap
        for (int y = roofTop; y <= roofTop + Mathf.RoundToInt(7f * sc); y++)
            for (int x = icx - 1; x <= icx + 1; x++)
                if (x >= 0 && x < w && y >= 0 && y < h) cols[y * w + x] = (x == icx - 1) ? woodLight : wood;
    }

    static void DrawFence(Color32[] cols, int w, int h, int x0, int x1)
    {
        Color32 wood = C(150, 104, 60), dark = C(108, 72, 42);
        for (int x = x0; x <= x1; x++)
        {
            bool post = ((x - x0) % 12) < 3;
            for (int y = 2; y <= 16; y++)
            {
                if (post) cols[y * w + x] = wood;
                else if ((y >= 6 && y <= 7) || (y >= 12 && y <= 13)) cols[y * w + x] = dark;
            }
        }
    }

    // =====================================================================
    //  MUSUH (burung bernuansa Nusantara), PERISAI, GERBANG KUIS, DAN IKON
    // =====================================================================

    public static Sprite[] EnggangFrames()
    {
        return new[] { Enggang(11f, 26f), Enggang(9f, 16f), Enggang(11f, 5f) };
    }

    /// <summary>Burung Enggang (rangkong): paruh besar, kalung merah, sayap berhias motif ceplok batik. Menghadap kiri.</summary>
    static Sprite Enggang(float tx, float ty)
    {
        const int W = 48, H = 30;
        var cols = new Color32[W * H];
        Color32 black = C(40, 36, 48), white = C(246, 242, 230), cream = C(250, 236, 200);
        Color32 orange = C(244, 150, 38), red = C(206, 54, 40), gold = C(246, 198, 66), dark = C(30, 26, 36);

        // ekor putih berpita hitam
        Taper(cols, W, H, 14f, 14f, 3.2f, 3.5f, 12f, 2.4f, white);
        Taper(cols, W, H, 7.5f, 12.9f, 2.7f, 4.5f, 12.4f, 2.5f, black);
        // badan, kepala, kalung
        FillE(cols, W, H, 22f, 14f, 11f, 7f, black);
        FillE(cols, W, H, 23f, 11.5f, 8f, 3.6f, white);
        FillE(cols, W, H, 34f, 18f, 5.5f, 5f, cream);
        FillE(cols, W, H, 30.5f, 15.5f, 2f, 3.6f, red);
        // paruh besar dan tanduk merah
        for (int x = 38; x <= 45; x++)
        {
            float half = 3.6f - (x - 38) * 0.3f;
            for (int y = 0; y < H; y++)
                if (Mathf.Abs(y + 0.5f - 17.5f) <= half) cols[y * W + x] = orange;
        }
        FillE(cols, W, H, 41.5f, 21.4f, 5f, 2.6f, red);
        Stamp(cols, W, H, 36.6f, 19.4f, 1.4f, dark);
        Stamp(cols, W, H, 37.1f, 19.9f, 0.5f, white);
        // sayap hitam dengan motif ceplok emas
        float bx = 22f, by = 15f;
        Taper(cols, W, H, bx, by, 4.4f, tx, ty, 1.2f, black);
        for (int k = 1; k <= 3; k++)
        {
            float u = 0.25f + k * 0.18f;
            float px = Mathf.Lerp(bx, tx, u), py = Mathf.Lerp(by, ty, u);
            Stamp(cols, W, H, px, py, 0.9f, gold);
            Stamp(cols, W, H, px + 1.5f, py, 0.55f, gold);
            Stamp(cols, W, H, px - 1.5f, py, 0.55f, gold);
            Stamp(cols, W, H, px, py + 1.5f, 0.55f, gold);
            Stamp(cols, W, H, px, py - 1.5f, 0.55f, gold);
        }
        Taper(cols, W, H, Mathf.Lerp(bx, tx, 0.7f), Mathf.Lerp(by, ty, 0.7f), 1.8f, tx, ty, 0.8f, white);

        FlipX(cols, W, H);
        AddOutline(cols, W, H, Outline);
        return FromPixels(W, H, cols, PPU, FilterMode.Point);
    }

    public static Sprite[] KawungFrames()
    {
        return new[] { Kawung(true), Kawung(false) };
    }

    /// <summary>Burung bulat bermotif kawung (batik): perut krem berhias cincin cokelat soga, jambul emas. Menghadap kiri.</summary>
    static Sprite Kawung(bool wingUp)
    {
        const int W = 36, H = 34;
        var cols = new Color32[W * H];
        Color32 indigo = C(44, 66, 128), indigoDark = C(30, 46, 96), cream = C(244, 230, 196), soga = C(150, 92, 50);
        Color32 gold = C(246, 198, 66), orange = C(246, 150, 40), dark = C(30, 26, 36);

        Taper(cols, W, H, 9f, 14f, 3f, 2.5f, 12f, 1.5f, indigoDark);       // ekor pendek
        FillE(cols, W, H, 18f, 15f, 12f, 11f, indigo);                     // badan bulat
        FillE(cols, W, H, 18f, 11f, 8.5f, 6.6f, cream);                    // perut krem

        // motif kawung: empat cincin yang saling bertumpuk
        float[] rx = { 14.6f, 21.4f, 18f, 18f };
        float[] ry = { 11f, 11f, 14.2f, 7.8f };
        for (int i = 0; i < 4; i++) Stamp(cols, W, H, rx[i], ry[i], 3.1f, soga);
        for (int i = 0; i < 4; i++) Stamp(cols, W, H, rx[i], ry[i], 1.9f, cream);

        // jambul emas
        Taper(cols, W, H, 17f, 25f, 1.2f, 14f, 31f, 0.5f, gold);
        Taper(cols, W, H, 18f, 25.5f, 1.2f, 18f, 32f, 0.5f, gold);
        Taper(cols, W, H, 19f, 25f, 1.2f, 22f, 31f, 0.5f, gold);

        // mata besar yang ramah dan paruh kecil
        Stamp(cols, W, H, 25f, 19f, 2.6f, Color.white);
        Stamp(cols, W, H, 25.8f, 19f, 1.4f, dark);
        for (int x = 29; x <= 33; x++)
        {
            float half = 2.2f - (x - 29) * 0.4f;
            for (int y = 0; y < H; y++)
                if (Mathf.Abs(y + 0.5f - 16.5f) <= half) cols[y * W + x] = orange;
        }

        // sayap kecil
        if (wingUp) Taper(cols, W, H, 15f, 17f, 3.4f, 9f, 28f, 1f, soga);
        else Taper(cols, W, H, 15f, 15f, 3.4f, 8f, 5f, 1f, soga);

        FlipX(cols, W, H);
        AddOutline(cols, W, H, Outline);
        return FromPixels(W, H, cols, PPU, FilterMode.Point);
    }

    /// <summary>Elang Bondol bersayap batik yang melesat lurus (46 x 20 px). Menghadap kiri.</summary>
    public static Sprite Bondol()
    {
        const int W = 46, H = 20;
        var cols = new Color32[W * H];
        Color32 chestnut = C(168, 74, 42), wingC = C(112, 50, 32), white = C(250, 246, 236);
        Color32 gold = C(246, 198, 66), beak = C(250, 204, 54), dark = C(30, 26, 36);

        FillE(cols, W, H, 8f, 10f, 5f, 2.2f, chestnut);                    // ekor
        FillE(cols, W, H, 22f, 10f, 12f, 4.2f, chestnut);                  // badan
        FillE(cols, W, H, 35f, 11f, 5.2f, 4.4f, white);                    // kepala putih
        for (int x = 39; x <= 43; x++)                                     // paruh melengkung
        {
            float half = 1.7f - (x - 39) * 0.3f;
            float cy = 10.5f - (x - 39) * 0.25f;
            for (int y = 0; y < H; y++)
                if (Mathf.Abs(y + 0.5f - cy) <= half) cols[y * W + x] = beak;
        }
        Stamp(cols, W, H, 36.8f, 12.2f, 1.1f, dark);

        // sayap menyapu ke belakang (atas dan bawah) dengan titik-titik emas
        Taper(cols, W, H, 27f, 12f, 3.2f, 9f, 17.5f, 1f, wingC);
        Taper(cols, W, H, 27f, 8f, 3.2f, 9f, 2.5f, 1f, wingC);
        for (int k = 1; k <= 3; k++)
        {
            float u = 0.2f + k * 0.2f;
            Stamp(cols, W, H, Mathf.Lerp(27f, 9f, u), Mathf.Lerp(12f, 17.5f, u), 0.8f, gold);
            Stamp(cols, W, H, Mathf.Lerp(27f, 9f, u), Mathf.Lerp(8f, 2.5f, u), 0.8f, gold);
        }
        Taper(cols, W, H, Mathf.Lerp(27f, 9f, 0.7f), Mathf.Lerp(12f, 17.5f, 0.7f), 1.5f, 9f, 17.5f, 0.8f, white);
        Taper(cols, W, H, Mathf.Lerp(27f, 9f, 0.7f), Mathf.Lerp(8f, 2.5f, 0.7f), 1.5f, 9f, 2.5f, 0.8f, white);

        FlipX(cols, W, H);
        AddOutline(cols, W, H, Outline);
        return FromPixels(W, H, cols, PPU, FilterMode.Point);
    }

    /// <summary>Gelembung perisai di sekeliling burung (56 x 56 px).</summary>
    public static Sprite Shield()
    {
        const int S = 56;
        return MakeF(S, S, (x, y) =>
        {
            float dx = x + 0.5f - S * 0.5f, dy = y + 0.5f - S * 0.5f;
            float r = Mathf.Sqrt(dx * dx + dy * dy);
            if (r > 27.5f) return Clear;
            if (r > 25f) return new Color(0.5f, 0.95f, 1f, 0.9f);
            if (E(dx, dy, -10f, 11f, 5f, 2.8f) <= 1f) return new Color(1f, 1f, 1f, 0.7f);
            return new Color(0.45f, 0.9f, 1f, 0.10f + 0.18f * (r / 25f));
        }, FilterMode.Bilinear);
    }

    /// <summary>Gerbang kuis: dua tiang kayu dengan tirai cahaya di tengah (56 x 4 px, diregangkan vertikal).</summary>
    public static Sprite GateCurtain()
    {
        return Make(56, 4, (x, y) =>
        {
            int xm = x < 28 ? x : 55 - x;          // cermin kiri-kanan
            if (xm < 2) return Outline;
            if (xm < 4) return C(198, 148, 88);
            if (xm < 7) return C(150, 102, 58);
            if (xm == 7) return C(246, 198, 66);   // garis emas
            float k = 1f - Mathf.Abs(x - 27.5f) / 20f;
            return new Color(0.55f, 0.95f, 1f, 0.18f + 0.32f * Mathf.Clamp01(k));
        });
    }

    static readonly string[] QMark = { ".###.", "#...#", "....#", "..##.", "..#..", ".....", "..#.." };

    /// <summary>Lencana emas bertanda tanya di tengah gerbang kuis (36 x 36 px).</summary>
    public static Sprite GateBadge()
    {
        return Make(36, 36, (x, y) =>
        {
            float dx = x + 0.5f - 18f, dy = y + 0.5f - 18f;
            float r = Mathf.Sqrt(dx * dx + dy * dy);
            if (r > 17f) return Clear;
            if (r > 15f) return Outline;
            int cx = (x - 10) / 3, cy = 6 - (y - 7) / 3;       // baris 0 = bagian atas tanda tanya
            if (x >= 10 && x < 25 && y >= 7 && y < 28 && cy >= 0 && cy < 7 && cx >= 0 && cx < 5 && QMark[cy][cx] == '#')
                return C(110, 60, 20);
            return r < 8f ? C(255, 226, 110) : C(255, 196, 50);
        });
    }

    // ---------------- IKON UNTUK HUD (dipakai sebagai tekstur GUI) ----------------
    static readonly string[] HeartPat = { ".##...##.", "####.####", "#########", "#########", ".#######.", "..#####..", "...###...", "....#...." };

    static bool HeartBit(int cx, int cy)
    {
        if (cx < 0 || cx >= 9 || cy < 0 || cy >= 8) return false;
        return HeartPat[cy][cx] == '#';
    }

    static bool IsHeartPx(int x, int y)
    {
        if (x < 1 || y < 1) return false;
        return HeartBit((x - 1) / 2, 7 - (y - 1) / 2);
    }

    /// <summary>Ikon hati (nyawa), 20 x 18 px.</summary>
    public static Texture2D Heart()
    {
        return Make(20, 18, (x, y) =>
        {
            if (IsHeartPx(x, y))
            {
                int cx = (x - 1) / 2, cy = 7 - (y - 1) / 2;
                return (cy == 1 && (cx == 1 || cx == 2)) ? C(255, 150, 160) : C(230, 50, 64);
            }
            if (IsHeartPx(x - 1, y) || IsHeartPx(x + 1, y) || IsHeartPx(x, y - 1) || IsHeartPx(x, y + 1)) return Outline;
            return Clear;
        }).texture;
    }

    static bool InShield(float fx, float fy, float shrink)
    {
        float hw = (fy >= 10f ? 8f : 8f * Mathf.Clamp01((fy + 1f) / 11f)) - shrink;
        return fy > 0.5f + shrink * 0.8f && fy < 19.5f - shrink && Mathf.Abs(fx) <= hw;
    }

    /// <summary>Ikon perisai, 18 x 20 px.</summary>
    public static Texture2D ShieldIcon()
    {
        return Make(18, 20, (x, y) =>
        {
            float fx = x + 0.5f - 9f, fy = y + 0.5f;
            if (!InShield(fx, fy, 0f)) return Clear;
            if (!InShield(fx, fy, 1.4f)) return C(40, 70, 130);
            if (fx < -3f && fx > -5f && fy > 6f && fy < 16f) return C(210, 240, 255);
            return fx < 0f ? C(90, 170, 240) : C(56, 128, 214);
        }).texture;
    }

    /// <summary>Ikon jam pasir (slow motion), 16 x 20 px.</summary>
    public static Texture2D HourglassIcon()
    {
        return Make(16, 20, (x, y) =>
        {
            float fx = x + 0.5f - 8f, fy = y + 0.5f;
            float ax = Mathf.Abs(fx);
            if (y < 2 || y >= 18) return ax <= 7f ? C(120, 78, 44) : Clear;        // tutup atas dan bawah
            float hw = 1.2f + 5.8f * Mathf.Abs(fy - 10f) / 8f;
            if (ax > hw) return Clear;
            if (ax > hw - 1.2f) return C(70, 60, 50);
            if (fy < 7f || fy > 14.5f) return C(244, 196, 64);                     // pasir
            return new Color(0.75f, 0.9f, 0.97f, 0.85f);
        }).texture;
    }

    /// <summary>Segitiga peringatan merah dengan tanda seru (24 x 24 px).</summary>
    public static Sprite Warning()
    {
        return Make(24, 24, (x, y) =>
        {
            float fx = x + 0.5f, fy = y + 0.5f;
            float halfW = (22f - fy) / 20f * 11.5f;
            if (fy < 2f || fy > 22f || Mathf.Abs(fx - 12f) > halfW) return Clear;
            bool edge = fy < 3.2f || Mathf.Abs(fx - 12f) > halfW - 1.6f;
            if (edge) return Outline;
            if (Mathf.Abs(fx - 12f) <= 1.3f && ((fy >= 9f && fy <= 16f) || (fy >= 5f && fy <= 7f))) return Color.white;
            return C(245, 60, 40);
        });
    }
}
