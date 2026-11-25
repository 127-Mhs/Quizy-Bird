using UnityEngine;

/// <summary>
/// Memutar musik latar berulang. Versi siang (ceria) dan malam (tenang) saling memudar (crossfade)
/// mengikuti DayNightCycle. Volume mengecil saat kuis atau Game Over, dan senyap saat dibisukan (tombol M).
/// </summary>
public class MusicPlayer : MonoBehaviour
{
    const float MasterVolume = 0.32f;       // dibuat lebih pelan dari efek suara agar efek tetap terdengar jelas

    AudioSource dayAs, nightAs;
    DayNightCycle cycle;
    float master;

    public static MusicPlayer Create(DayNightCycle cycle)
    {
        var go = new GameObject("MusicPlayer");
        var m = go.AddComponent<MusicPlayer>();
        m.cycle = cycle;
        m.dayAs = NewSource(go, MusicFactory.Day());
        m.nightAs = NewSource(go, MusicFactory.Night());
        return m;
    }

    static AudioSource NewSource(GameObject go, AudioClip clip)
    {
        var a = go.AddComponent<AudioSource>();
        a.clip = clip;
        a.loop = true;
        a.playOnAwake = false;
        a.spatialBlend = 0f;
        a.volume = 0f;
        return a;
    }

    void Update()
    {
        var gm = GameManager.Instance;
        if (gm == null) return;

        float mix = Mathf.SmoothStep(0f, 1f, cycle != null ? cycle.Night01 : 0f);   // 0 = siang, 1 = malam

        float target = gm.Muted ? 0f : MasterVolume;
        if (gm.State == GameState.Quiz || gm.State == GameState.QuizFeedback) target *= 0.5f;
        else if (gm.State == GameState.GameOver) target *= 0.4f;
        master = Mathf.MoveTowards(master, target, 0.8f * Time.unscaledDeltaTime);

        Apply(dayAs, 1f - mix);
        Apply(nightAs, mix);
    }

    void Apply(AudioSource a, float share)
    {
        a.volume = share * master;
        if (share > 0.001f)
        {
            if (!a.isPlaying) a.Play();       // masuk dari awal loop agar dimulai di ketukan pertama
        }
        else if (a.isPlaying) a.Stop();
    }
}
