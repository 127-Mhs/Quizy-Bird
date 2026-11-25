using UnityEngine;

/// <summary>
/// Gerbang Kuis: dua tiang kayu dengan tirai cahaya setinggi layar dan lencana "?" di tengah.
/// Bergerak ke kiri seperti pipa. Saat burung melewatinya, permainan berhenti dan muncul kuis.
/// Gerbang ini bukan rintangan (collider-nya trigger).
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class QuizGate : MonoBehaviour
{
    static Sprite curtain, badge;

    Rigidbody2D rb;
    SpriteRenderer curtainSr;
    Transform badgeTr;
    float badgeY, t;
    bool used;

    public static void LoadSprites()
    {
        curtain = SpriteFactory.GateCurtain();
        badge = SpriteFactory.GateBadge();
    }

    public static QuizGate Create(float x, float badgeY)
    {
        var go = new GameObject("QuizGate");
        go.transform.position = new Vector3(x, 0f, 0f);
        var g = go.AddComponent<QuizGate>();
        g.Init(badgeY);
        return g;
    }

    void Init(float by)
    {
        badgeY = by;
        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;

        float bottom = GameManager.GroundTop - 0.5f, top = 7f;

        // tirai cahaya setinggi layar (diregangkan vertikal)
        var c = new GameObject("Curtain");
        c.transform.SetParent(transform, false);
        c.transform.localPosition = new Vector3(0f, (bottom + top) * 0.5f, 0f);
        c.transform.localScale = new Vector3(1f, (top - bottom) / curtain.bounds.size.y, 1f);
        curtainSr = c.AddComponent<SpriteRenderer>();
        curtainSr.sprite = curtain;
        curtainSr.sortingOrder = 3;

        // lencana tanda tanya
        var b = new GameObject("Badge");
        b.transform.SetParent(transform, false);
        b.transform.localPosition = new Vector3(0f, badgeY, 0f);
        var bsr = b.AddComponent<SpriteRenderer>();
        bsr.sprite = badge;
        bsr.sortingOrder = 4;
        badgeTr = b.transform;

        var col = gameObject.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(0.5f, top - bottom);
        col.offset = new Vector2(0f, (bottom + top) * 0.5f);
    }

    void Update()
    {
        t += Time.deltaTime;
        curtainSr.color = new Color(1f, 1f, 1f, 0.8f + 0.2f * Mathf.Sin(t * 5f));          // tirai berkilau
        badgeTr.localPosition = new Vector3(0f, badgeY + 0.12f * Mathf.Sin(t * 3f), 0f);   // lencana melayang
    }

    void FixedUpdate()
    {
        var gm = GameManager.Instance;
        if (gm == null) return;
        rb.MovePosition(rb.position + Vector2.left * gm.WorldSpeed * Time.fixedDeltaTime);
        if (rb.position.x < -(gm.HalfWidth + 3f)) Destroy(gameObject);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (used) return;
        var bird = other.GetComponent<BirdController>();
        if (bird == null || !bird.Alive) return;
        used = true;
        GameManager.Instance.OnGatePassed();
        Destroy(gameObject);
    }
}
