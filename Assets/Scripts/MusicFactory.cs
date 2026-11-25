using UnityEngine;

/// <summary>
/// Musik latar 8-bit bertangga nada gamelan, dibuat sepenuhnya lewat kode (tanpa file audio).
///  - Day()   : siang, ceria, laras slendro, 132 BPM, 8 birama (sekitar 15 detik).
///  - Night() : malam, tenang, laras pelog, 84 BPM, 8 birama (sekitar 23 detik).
/// Semua nada ditulis melingkar (wrap-around) sehingga ekor nada menyambung ke awal dan pengulangan terdengar mulus.
/// </summary>
public static class MusicFactory
{
    const int Rate = 22050;
    const int R = -99;                       // jeda (tidak ada nada)

    // Laras gamelan dalam cent (pendekatan). Slendro: lima nada berjarak hampir sama. Pelog: lima nada pilihan.
    static readonly float[] Slendro = { 0f, 240f, 480f, 720f, 960f };
    static readonly float[] Pelog = { 0f, 120f, 270f, 540f, 670f };

    // frekuensi nada ke-n pada laras (n boleh melewati satu oktaf atau negatif)
    static float Hz(float baseHz, float[] cents, int n)
    {
        int len = cents.Length;
        int oct = Mathf.FloorToInt(n / (float)len);
        int deg = n - oct * len;
        return baseHz * Mathf.Pow(2f, cents[deg] / 1200f + oct);
    }

    // ------------------------------------------------------------ SIANG
    public static AudioClip Day()
    {
        float step = 60f / 132f / 2f;        // satu langkah = not 1/8
        int[] lead =
        {
            4, R, 6, 7, 6, R, 4, 2,
            3, R, 4, 6, 4, R, 3, 1,
            2, R, 4, 6, 7, R, 6, 4,
            3, 4, 3, 1, 2, R, R, R,
            4, R, 6, 7, 9, R, 7, 6,
            6, R, 7, 6, 4, R, 3, 4,
            2, 3, 4, 6, 4, 3, 2, 1,
            0, R, 2, 3, 2, R, R, R
        };
        int[] bass = { 0, 2, 3, 1, 2, 0, 3, 2, 0, 2, 3, 4, 2, 1, 0, 0 };   // dua nada per birama

        var buf = new float[Mathf.RoundToInt(lead.Length * step * Rate)];
        float baseHz = 330f;

        for (int i = 0; i < lead.Length; i++)
        {
            int start = Mathf.RoundToInt(i * step * Rate);
            if (lead[i] != R)
            {
                float f = Hz(baseHz, Slendro, lead[i]);
                Pulse(buf, start, f, step * 1.8f, 0.20f, 0.25f);          // melodi 8-bit (gelombang pulsa)
                Bell(buf, start, f * 2f, step * 3f, 0.10f);               // kilau logam ala saron
            }
            if (i % 2 == 0) Tick(buf, start, i % 8 == 0 ? 0.07f : 0.04f, i);   // detak ritme (kendang ringan)
        }
        for (int b = 0; b < bass.Length; b++)
            Tri(buf, Mathf.RoundToInt(b * 4 * step * Rate), Hz(baseHz * 0.5f, Slendro, bass[b]), step * 3.6f, 0.22f);

        Gong(buf, 0, 98f, 4f, 0.20f);
        Gong(buf, Mathf.RoundToInt(32 * step * Rate), 98f, 4f, 0.16f);
        return Finish("music_day", buf);
    }

    // ------------------------------------------------------------ MALAM
    public static AudioClip Night()
    {
        float step = 60f / 84f / 2f;
        int[] lead =
        {
            4, R, 3, R, 2, R, R, R,
            3, R, 2, R, 0, R, R, R,
            2, R, 3, R, 4, R, 6, R,
            4, R, 3, R, 2, R, R, R,
            6, R, 4, R, 3, R, R, R,
            4, R, 3, R, 2, R, 3, R,
            2, R, 0, R, 2, R, 3, R,
            2, R, R, R, 0, R, R, R
        };
        int[] bass = { 0, 3, 2, 0, 3, 4, 2, 0 };                 // satu nada per birama
        int[,] pad = { { 0, 3 }, { 2, 4 }, { 0, 3 }, { 1, 4 } };  // dua nada dengung per dua birama
        int[] sparkle = { 10, 27, 42, 58 };                       // kilau tinggi seperti bintang

        var buf = new float[Mathf.RoundToInt(lead.Length * step * Rate)];
        float baseHz = 262f;

        for (int i = 0; i < lead.Length; i++)
        {
            if (lead[i] == R) continue;
            int start = Mathf.RoundToInt(i * step * Rate);
            float f = Hz(baseHz, Pelog, lead[i]);
            Bell(buf, start, f, step * 5f, 0.17f);                // nada lembut berdenting panjang (gender)
            Tri(buf, start, f, step * 2.5f, 0.06f);
        }
        for (int b = 0; b < bass.Length; b++)
            Soft(buf, Mathf.RoundToInt(b * 8 * step * Rate), Hz(baseHz * 0.5f, Pelog, bass[b]), step * 7.5f, 0.18f, 0.03f);
        for (int p = 0; p < 4; p++)
        {
            int start = Mathf.RoundToInt(p * 16 * step * Rate);
            Soft(buf, start, Hz(baseHz, Pelog, pad[p, 0]), step * 15f, 0.07f, 0.9f);
            Soft(buf, start, Hz(baseHz, Pelog, pad[p, 1]), step * 15f, 0.05f, 1.2f);
        }
        for (int k = 0; k < sparkle.Length; k++)
            Bell(buf, Mathf.RoundToInt(sparkle[k] * step * Rate), Hz(baseHz, Pelog, 8 + (k % 2)), step * 6f, 0.07f);

        Gong(buf, 0, 87f, 6f, 0.22f);
        Gong(buf, Mathf.RoundToInt(32 * step * Rate), 87f, 6f, 0.18f);
        return Finish("music_night", buf);
    }

    // ------------------------------------------------------------ suara dasar (semua ditambahkan melingkar ke buf)
    static void Pulse(float[] buf, int start, float freq, float dur, float vol, float duty)
    {
        int n = (int)(dur * Rate);
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)Rate;
            float ph = (freq * t) % 1f;
            float env = Mathf.Min(1f, t / 0.006f) * Mathf.Exp(-5f * t / dur);
            buf[(start + i) % buf.Length] += (ph < duty ? 1f : -1f) * env * vol;
        }
    }

    static void Tri(float[] buf, int start, float freq, float dur, float vol)
    {
        int n = (int)(dur * Rate);
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)Rate;
            float ph = (freq * t) % 1f;
            float env = Mathf.Min(1f, t / 0.008f) * Mathf.Exp(-2.5f * t / dur);
            buf[(start + i) % buf.Length] += (4f * Mathf.Abs(ph - 0.5f) - 1f) * env * vol;
        }
    }

    // dentingan logam (saron / gender): nada dasar + dua sebagian nada yang tidak harmonis dan cepat hilang
    static void Bell(float[] buf, int start, float freq, float dur, float vol)
    {
        int n = (int)(dur * Rate);
        float w = 2f * Mathf.PI * freq;
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)Rate;
            float env = Mathf.Min(1f, t / 0.002f) * Mathf.Exp(-6f * t / dur);
            float s = Mathf.Sin(w * t)
                    + 0.45f * Mathf.Sin(w * 2.76f * t) * Mathf.Exp(-14f * t)
                    + 0.20f * Mathf.Sin(w * 5.4f * t) * Mathf.Exp(-25f * t);
            buf[(start + i) % buf.Length] += s * env * vol;
        }
    }

    // gelombang sinus lembut dengan masuk dan keluar perlahan (dengung / bas)
    static void Soft(float[] buf, int start, float freq, float dur, float vol, float attack)
    {
        int n = (int)(dur * Rate);
        float w = 2f * Mathf.PI * freq;
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)Rate;
            float env = Mathf.Min(1f, t / attack) * Mathf.Clamp01((dur - t) / (0.25f * dur));
            buf[(start + i) % buf.Length] += Mathf.Sin(w * t) * env * vol;
        }
    }

    // gong rendah dengan dua nada yang sedikit berbeda (efek bergetar) dan ekor panjang
    static void Gong(float[] buf, int start, float freq, float dur, float vol)
    {
        int n = (int)(dur * Rate);
        float w = 2f * Mathf.PI * freq;
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)Rate;
            float env = Mathf.Min(1f, t / 0.01f) * Mathf.Exp(-4f * t / dur);
            float s = Mathf.Sin(w * t)
                    + 0.5f * Mathf.Sin(w * 1.004f * t)
                    + 0.35f * Mathf.Sin(w * 2.38f * t) * Mathf.Exp(-2.2f * t)
                    + 0.20f * Mathf.Sin(w * 3.9f * t) * Mathf.Exp(-3.5f * t);
            buf[(start + i) % buf.Length] += s * env * vol;
        }
    }

    // detak derau pendek (ritme ringan)
    static void Tick(float[] buf, int start, float vol, int seed)
    {
        int n = (int)(0.04f * Rate);
        uint x = 12345u + (uint)seed * 2654435761u;
        float prev = 0f;
        for (int i = 0; i < n; i++)
        {
            x = x * 1664525u + 1013904223u;
            float nz = ((x >> 9) & 0x7FFFu) / 16384f - 1f;
            float hp = nz - prev;                    // penapis sederhana: buang nada rendah
            prev = nz;
            float t = i / (float)Rate;
            buf[(start + i) % buf.Length] += hp * Mathf.Exp(-90f * t) * vol;
        }
    }

    static AudioClip Finish(string name, float[] buf)
    {
        float peak = 0.0001f;
        for (int i = 0; i < buf.Length; i++) peak = Mathf.Max(peak, Mathf.Abs(buf[i]));
        float g = peak > 0.9f ? 0.9f / peak : 1f;
        for (int i = 0; i < buf.Length; i++) buf[i] *= g;
        var clip = AudioClip.Create(name, buf.Length, 1, Rate, false);
        clip.SetData(buf, 0);
        return clip;
    }
}
