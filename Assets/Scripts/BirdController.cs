using UnityEngine;

/// <summary>
/// Burung Cenderawasih: kepakan sayap (flap), gravitasi lewat Rigidbody2D, animasi sayap, rotasi,
/// gelembung perisai, dan kebal sementara (tembus rintangan dan berkedip).
/// </summary>
public class BirdController : MonoBehaviour
{
    const float FlapVelocity = 7.2f;
    const float GravityScale = 2.4f;
    const float MaxFall = -11f;

    Rigidbody2D rb;
    SpriteRenderer sr, shieldSr;
    Sprite[] frames;
    Vector3 home;
    float animT, hoverT, rotZ, invulUntil;
    bool playing, frozen, shieldOn, shieldBlink, ignoring;

    public bool Alive { get; private set; }
    public bool Invulnerable { get { return Time.time < invulUntil; } }

    public static BirdController Create(Sprite[] frames, Vector3 position)
    {
        var go = new GameObject("Bird");
        go.layer = GameManager.BirdLayer;
        go.transform.position = position;
        var b = go.AddComponent<BirdController>();
        b.frames = frames;
        b.home = position;

        b.sr = go.AddComponent<SpriteRenderer>();
        b.sr.sprite = frames[0];
        b.sr.sortingOrder = 10;

        var col = go.AddComponent<CircleCollider2D>();
        col.radius = 0.3f;
        col.offset = new Vector2(0.05f, 0f);

        // gelembung perisai (tampil hanya saat power-up perisai aktif)
        var sh = new GameObject("Shield");
        sh.transform.SetParent(go.transform, false);
        sh.transform.localPosition = new Vector3(0.05f, 0f, 0f);
        b.shieldSr = sh.AddComponent<SpriteRenderer>();
        b.shieldSr.sprite = SpriteFactory.Shield();
        b.shieldSr.sortingOrder = 11;
        b.shieldSr.enabled = false;

        b.rb = go.AddComponent<Rigidbody2D>();
        b.rb.gravityScale = GravityScale;
        b.rb.freezeRotation = true;
        b.rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        b.Hover();
        return b;
    }

    float GetVY()
    {
#if UNITY_6000_0_OR_NEWER
        return rb.linearVelocity.y;
#else
        return rb.velocity.y;
#endif
    }

    void SetVY(float vy)
    {
#if UNITY_6000_0_OR_NEWER
        rb.linearVelocity = new Vector2(0f, vy);
#else
        rb.velocity = new Vector2(0f, vy);
#endif
    }

    /// <summary>Kebal sementara: burung berkedip dan menembus pipa/musuh (tanah tetap padat).</summary>
    public void GrantInvulnerable(float seconds)
    {
        invulUntil = Mathf.Max(invulUntil, Time.time + seconds);
    }

    public void SetShield(bool on, bool blink)
    {
        shieldOn = on;
        shieldBlink = blink;
    }

    /// <summary>Mode melayang di tempat (menu, layar "siap", dan setelah kuis penyelamat).</summary>
    public void Hover()
    {
        invulUntil = 0f;
        sr.color = Color.white;
        Alive = true;
        frozen = false;
        playing = false;
        hoverT = 0f;
        rotZ = 0f;
        rb.bodyType = RigidbodyType2D.Kinematic;
        SetVY(0f);
        rb.angularVelocity = 0f;
        transform.position = home;
        transform.rotation = Quaternion.identity;
    }

    /// <summary>Mulai bermain: gravitasi aktif.</summary>
    public void Begin()
    {
        playing = true;
        rb.bodyType = RigidbodyType2D.Dynamic;
        SetVY(0f);
    }

    public void Flap()
    {
        if (!Alive || !playing) return;
        SetVY(FlapVelocity);
    }

    /// <summary>Membekukan burung di tempat (saat kuis ditampilkan).</summary>
    public void Freeze()
    {
        Alive = false;
        frozen = true;
        rb.bodyType = RigidbodyType2D.Kinematic;
        SetVY(0f);
        rb.angularVelocity = 0f;
    }

    /// <summary>Melepas pembekuan setelah gerbang kuis: burung lanjut terbang dari posisinya.</summary>
    public void Unfreeze()
    {
        Alive = true;
        frozen = false;
        playing = true;
        rb.bodyType = RigidbodyType2D.Dynamic;
        SetVY(0f);
    }

    /// <summary>Burung mati dan jatuh ke tanah.</summary>
    public void Fall()
    {
        invulUntil = 0f;
        sr.color = Color.white;
        Alive = false;
        frozen = false;
        rb.bodyType = RigidbodyType2D.Dynamic;
        SetVY(0f);
    }

    void Update()
    {
        // kebal sementara: tabrakan dengan rintangan dimatikan lewat matriks layer
        bool inv = Invulnerable;
        if (inv != ignoring)
        {
            ignoring = inv;
            Physics2D.IgnoreLayerCollision(GameManager.BirdLayer, GameManager.ObstacleLayer, inv);
        }
        if (shieldSr != null) shieldSr.enabled = shieldOn && (!shieldBlink || ((int)(Time.time * 8f) % 2 == 0));

        if (frozen) return;

        if (Alive)
        {
            animT += Time.deltaTime * 14f;
            sr.sprite = frames[(int)animT % frames.Length];
            sr.color = inv ? new Color(1f, 1f, 1f, ((int)(Time.time * 14f) % 2 == 0) ? 0.35f : 1f) : Color.white;
        }

        if (!playing && Alive)
        {
            hoverT += Time.deltaTime;
            var p = transform.position;
            p.y = home.y + Mathf.Sin(hoverT * 5f) * 0.15f;
            transform.position = p;
            return;
        }

        float target = Alive ? Mathf.Clamp(GetVY() * 5f, -90f, 30f) : -90f;
        rotZ = Mathf.Lerp(rotZ, target, 12f * Time.deltaTime);
        transform.rotation = Quaternion.Euler(0f, 0f, rotZ);
    }

    void FixedUpdate()
    {
        if (rb.bodyType == RigidbodyType2D.Dynamic && GetVY() < MaxFall) SetVY(MaxFall);
    }

    void OnCollisionEnter2D(Collision2D c)
    {
        Hit();
    }

    // tetap menyentuh sesuatu setelah kebal berakhir (mis. tanah) juga dihitung sebagai tabrakan
    void OnCollisionStay2D(Collision2D c)
    {
        Hit();
    }

    void Hit()
    {
        if (!Alive) return;
        GameManager.Instance.OnBirdHit();
    }
}
