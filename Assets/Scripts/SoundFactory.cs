using System;
using UnityEngine;

/// <summary>Efek suara 8-bit yang dibuat prosedural (pengganti file hasil sfxr).</summary>
public static class SoundFactory
{
    const int Rate = 44100;

    static AudioClip Tone(string name, float duration, Func<float, float> freqAt, bool noise = false, float volume = 0.35f)
    {
        int n = (int)(duration * Rate);
        var data = new float[n];
        float phase = 0f;
        var rnd = new System.Random(7);
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)n;
            float env = 1f - t;
            float s;
            if (noise)
            {
                s = (float)(rnd.NextDouble() * 2.0 - 1.0);
            }
            else
            {
                phase += 2f * Mathf.PI * freqAt(t) / Rate;
                s = Mathf.Sign(Mathf.Sin(phase)) * 0.5f + Mathf.Sin(phase) * 0.5f;
            }
            data[i] = s * env * volume;
        }
        var clip = AudioClip.Create(name, n, 1, Rate, false);
        clip.SetData(data, 0);
        return clip;
    }

    public static AudioClip Flap() { return Tone("flap", 0.10f, t => Mathf.Lerp(420f, 820f, t)); }
    public static AudioClip Score() { return Tone("score", 0.18f, t => t < 0.5f ? 988f : 1319f); }
    public static AudioClip Hit() { return Tone("hit", 0.16f, t => 0f, true, 0.45f); }
    public static AudioClip PowerUp() { return Tone("powerup", 0.40f, t => t < 0.33f ? 660f : (t < 0.66f ? 880f : 1175f)); }
    public static AudioClip Die() { return Tone("die", 0.45f, t => Mathf.Lerp(520f, 110f, t)); }
}
