using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Font piksel/retro 5 x 7 yang digambar lewat kode (tidak butuh file font).
/// Dipakai untuk judul, angka besar, dan label tombol. Teks otomatis dikecilkan agar muat
/// di dalam kotaknya dan selalu diletakkan tepat di tengah (horizontal dan vertikal).
/// Huruf kecil otomatis dijadikan huruf besar.
/// </summary>
public static class PixelFont
{
    const int GW = 5, GH = 7, Advance = 6;      // lebar glyph, tinggi glyph, jarak antar huruf (termasuk 1 kolom kosong)
    static readonly Dictionary<char, string[]> G = new Dictionary<char, string[]>();

    static void Add(char c, params string[] rows)
    {
        G[c] = rows;
    }

    static PixelFont()
    {
        Add('A', ".###.", "#...#", "#...#", "#####", "#...#", "#...#", "#...#");
        Add('B', "####.", "#...#", "#...#", "####.", "#...#", "#...#", "####.");
        Add('C', ".####", "#....", "#....", "#....", "#....", "#....", ".####");
        Add('D', "####.", "#...#", "#...#", "#...#", "#...#", "#...#", "####.");
        Add('E', "#####", "#....", "#....", "###..", "#....", "#....", "#####");
        Add('F', "#####", "#....", "#....", "###..", "#....", "#....", "#....");
        Add('G', ".####", "#....", "#....", "#.###", "#...#", "#...#", ".####");
        Add('H', "#...#", "#...#", "#...#", "#####", "#...#", "#...#", "#...#");
        Add('I', "#####", "..#..", "..#..", "..#..", "..#..", "..#..", "#####");
        Add('J', "..###", "...#.", "...#.", "...#.", "...#.", "#..#.", ".##..");
        Add('K', "#...#", "#..#.", "#.#..", "##...", "#.#..", "#..#.", "#...#");
        Add('L', "#....", "#....", "#....", "#....", "#....", "#....", "#####");
        Add('M', "#...#", "##.##", "#.#.#", "#.#.#", "#...#", "#...#", "#...#");
        Add('N', "#...#", "##..#", "#.#.#", "#..##", "#...#", "#...#", "#...#");
        Add('O', ".###.", "#...#", "#...#", "#...#", "#...#", "#...#", ".###.");
        Add('P', "####.", "#...#", "#...#", "####.", "#....", "#....", "#....");
        Add('Q', ".###.", "#...#", "#...#", "#...#", "#.#.#", "#..#.", ".##.#");
        Add('R', "####.", "#...#", "#...#", "####.", "#.#..", "#..#.", "#...#");
        Add('S', ".####", "#....", "#....", ".###.", "....#", "....#", "####.");
        Add('T', "#####", "..#..", "..#..", "..#..", "..#..", "..#..", "..#..");
        Add('U', "#...#", "#...#", "#...#", "#...#", "#...#", "#...#", ".###.");
        Add('V', "#...#", "#...#", "#...#", "#...#", "#...#", ".#.#.", "..#..");
        Add('W', "#...#", "#...#", "#...#", "#.#.#", "#.#.#", "##.##", "#...#");
        Add('X', "#...#", "#...#", ".#.#.", "..#..", ".#.#.", "#...#", "#...#");
        Add('Y', "#...#", "#...#", ".#.#.", "..#..", "..#..", "..#..", "..#..");
        Add('Z', "#####", "....#", "...#.", "..#..", ".#...", "#....", "#####");
        Add('0', ".###.", "#...#", "#..##", "#.#.#", "##..#", "#...#", ".###.");
        Add('1', "..#..", ".##..", "..#..", "..#..", "..#..", "..#..", ".###.");
        Add('2', ".###.", "#...#", "....#", "...#.", "..#..", ".#...", "#####");
        Add('3', "####.", "....#", "....#", ".###.", "....#", "....#", "####.");
        Add('4', "...#.", "..##.", ".#.#.", "#..#.", "#####", "...#.", "...#.");
        Add('5', "#####", "#....", "####.", "....#", "....#", "#...#", ".###.");
        Add('6', ".###.", "#....", "#....", "####.", "#...#", "#...#", ".###.");
        Add('7', "#####", "....#", "...#.", "..#..", ".#...", ".#...", ".#...");
        Add('8', ".###.", "#...#", "#...#", ".###.", "#...#", "#...#", ".###.");
        Add('9', ".###.", "#...#", "#...#", ".####", "....#", "....#", ".###.");
        Add(' ', ".....", ".....", ".....", ".....", ".....", ".....", ".....");
        Add('!', "..#..", "..#..", "..#..", "..#..", "..#..", ".....", "..#..");
        Add('?', ".###.", "#...#", "....#", "...#.", "..#..", ".....", "..#..");
        Add('.', ".....", ".....", ".....", ".....", ".....", ".....", "..#..");
        Add(':', ".....", "..#..", ".....", ".....", ".....", "..#..", ".....");
        Add('-', ".....", ".....", ".....", ".###.", ".....", ".....", ".....");
        Add(',', ".....", ".....", ".....", ".....", ".....", "..#..", ".#...");
        Add('/', "....#", "....#", "...#.", "..#..", ".#...", "#....", "#....");
        Add('%', "##..#", "##..#", "...#.", "..#..", ".#...", "#..##", "#..##");
        Add('+', ".....", "..#..", "..#..", "#####", "..#..", "..#..", ".....");
        Add('&', ".##..", "#..#.", "#.#..", ".#...", "#.#.#", "#..#.", ".##.#");
    }

    /// <summary>Lebar teks dalam "unit piksel" (satu unit = satu kotak font).</summary>
    public static float Units(string text)
    {
        return text.Length == 0 ? 0f : text.Length * Advance - 1;
    }

    /// <summary>
    /// Menggambar teks piksel tepat di tengah kotak r. px = ukuran satu kotak dalam piksel layar;
    /// otomatis dikecilkan jika teks tidak muat. Bayangan tipis menjaga keterbacaan di atas latar apa pun.
    /// </summary>
    public static void Draw(Rect r, string text, float px, Color color, Texture2D white, bool shadow = true)
    {
        if (string.IsNullOrEmpty(text)) return;
        text = text.ToUpperInvariant();

        float units = Units(text);
        px = Mathf.Min(px, r.width / units, r.height / GH);
        if (px >= 2f) px = Mathf.Floor(px);              // kotak utuh agar tajam
        if (px < 0.5f) return;

        float x0 = Mathf.Round(r.x + (r.width - units * px) * 0.5f);
        float y0 = Mathf.Round(r.y + (r.height - GH * px) * 0.5f);

        Color prev = GUI.color;
        if (shadow)
        {
            float off = Mathf.Max(1f, Mathf.Round(px / 3f));
            Paint(text, x0 + off, y0 + off, px, new Color(0f, 0f, 0f, 0.5f * color.a * prev.a), white);
        }
        Paint(text, x0, y0, px, new Color(color.r, color.g, color.b, color.a * prev.a), white);
        GUI.color = prev;
    }

    static void Paint(string text, float x0, float y0, float px, Color col, Texture2D white)
    {
        GUI.color = col;
        for (int i = 0; i < text.Length; i++)
        {
            string[] g;
            if (!G.TryGetValue(text[i], out g)) continue;
            float gx = x0 + i * Advance * px;
            for (int row = 0; row < GH; row++)
            {
                string line = g[row];
                int c = 0;
                while (c < GW)
                {
                    if (line[c] != '#') { c++; continue; }
                    int start = c;
                    while (c < GW && line[c] == '#') c++;          // gabungkan kotak yang berurutan jadi satu persegi panjang
                    GUI.DrawTexture(new Rect(gx + start * px, y0 + row * px, (c - start) * px, px), white);
                }
            }
        }
    }
}
