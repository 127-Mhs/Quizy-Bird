using UnityEngine;

public enum EnemyKind { Enggang, Kawung, Bondol }

/// <summary>
/// Musuh berupa burung bernuansa Nusantara:
///  - Enggang : burung rangkong bersayap motif ceplok, terbang bergelombang di antara dua pipa.
///  - Kawung  : burung bulat bermotif batik kawung, naik-turun lebar di antara dua pipa.
///  - Bondol  : elang bersayap batik; muncul tanda peringatan di tepi kanan layar sesuai tinggi
///              burung pemain, lalu melesat cepat ke kiri.
/// Menyentuh musuh sama seperti menabrak pipa (kuis penyelamat / perisai).
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class Enemy : MonoBehaviour
{
    const float WarnTime = 1.3f;       // lama tanda peringatan Bondol
    const float WarnLock = 0.4f;       // pada waktu ini ketinggian dikunci (tidak lagi mengikuti burung)
    const float DiveSpeed = 9f;

    static Sprite[] enggangFrames, kawungFrames;
    static Sprite bondolSprite, warnSprite;

    EnemyKind kind;
    Rigidbody2D rb;
    SpriteRenderer sr;
    Transform visual;
    Collider2D col;
    float x, baseY, amp, period, t, animT;
    bool warning;

    /// <summary>Dipanggil sekali oleh GameManager untuk membuat sprite musuh.</summary>
    public static void LoadSprites()
    {
        enggangFrames = SpriteFactory.EnggangFrames();
        kawungFrames = SpriteFactory.KawungFrames();
        bondolSprite = SpriteFactory.Bondol();
        warnSprite = SpriteFactory.Warning();
    }

    public static Enemy Create(EnemyKind kind, float x, float baseY, float amp)
    {
        var go = new GameObject("Enemy_" + kind);
        go.layer = GameManager.ObstacleLayer;
        go.transform.position = new Vector3(x, baseY, 0f);
        var e = go.AddComponent<Enemy>();
        e.Init(kind, x, baseY, amp);
        return e;
    }

    void Init(EnemyKind k, float px, float py, float a)
    {
        kind = k;
        x = px;
        baseY = py;
        amp = a;

        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;

        var v = new GameObject("Visual");
        v.transform.SetParent(transform, false);
        visual = v.transform;
        sr = v.AddComponent<SpriteRenderer>();
        sr.sortingOrder = 8;

        switch (kind)
        {
            case EnemyKind.Enggang:
            {
                sr.sprite = enggangFrames[0];
                var cc = gameObject.AddComponent<CircleCollider2D>();
                cc.radius = 0.3f;
                cc.offset = new Vector2(-0.08f, 0f);
                col = cc;
                period = 2.2f;
                break;
            }
            case EnemyKind.Kawung:
            {
                sr.sprite = kawungFrames[0];
                var cc = gameObject.AddComponent<CircleCollider2D>();
                cc.radius = 0.4f;
                col = cc;
                period = 2.8f;
                break;
            }
            case EnemyKind.Bondol:
            {
                warning = true;
                sr.sprite = warnSprite;
                visual.localScale = new Vector3(1.5f, 1.5f, 1f);
                var bc = gameObject.AddComponent<BoxCollider2D>();
                bc.size = new Vector2(1.0f, 0.3f);
                bc.enabled = false;                 // baru aktif setelah Bondol melesat
                col = bc;
                break;
            }
        }
    }

    void FixedUpdate()
    {
        var gm = GameManager.Instance;
        if (gm == null || gm.State != GameState.Playing) return;   // musuh diam saat kuis / game over

        float dt = Time.fixedDeltaTime * gm.GameSpeedMul;           // slow motion ikut memperlambat musuh
        t += dt;
        float y = baseY;

        switch (kind)
        {
            case EnemyKind.Enggang:
            case EnemyKind.Kawung:
                x -= gm.WorldSpeed * Time.fixedDeltaTime;           // WorldSpeed sudah memuat slow motion
                y = baseY + Mathf.Sin(t * Mathf.PI * 2f / period) * amp;
                break;

            case EnemyKind.Bondol:
                if (warning)
                {
                    x = gm.HalfWidth - 0.7f;
                    if (t < WarnTime - WarnLock)
                    {
                        float target = Mathf.Clamp(gm.BirdY, GameManager.GroundTop + 0.7f, gm.TopY - 0.7f);
                        baseY = Mathf.Lerp(baseY, target, 6f * dt);
                    }
                    y = baseY;
                    if (t >= WarnTime)
                    {
                        warning = false;
                        x = gm.HalfWidth + 0.8f;
                        sr.sprite = bondolSprite;
                        sr.color = Color.white;
                        visual.localScale = Vector3.one;
                        col.enabled = true;
                    }
                }
                else
                {
                    x -= DiveSpeed * dt;
                    y = baseY;
                }
                break;
        }

        rb.MovePosition(new Vector2(x, y));
        if (x < -(gm.HalfWidth + 3f)) Destroy(gameObject);
    }

    void Update()
    {
        var gm = GameManager.Instance;
        if (gm == null || gm.State != GameState.Playing) return;

        animT += Time.deltaTime;
        switch (kind)
        {
            case EnemyKind.Enggang:
                sr.sprite = enggangFrames[(int)(animT * 9f) % enggangFrames.Length];
                break;
            case EnemyKind.Kawung:
                sr.sprite = kawungFrames[(int)(animT * 8f) % kawungFrames.Length];
                break;
            case EnemyKind.Bondol:
                if (warning) sr.color = new Color(1f, 1f, 1f, 0.35f + 0.65f * Mathf.PingPong(animT * 6f, 1f));
                break;
        }
    }
}
