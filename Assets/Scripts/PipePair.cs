using UnityEngine;

/// <summary>Sepasang pipa (atas dan bawah) dengan celah, bergerak ke kiri.</summary>
[RequireComponent(typeof(Rigidbody2D))]
public class PipePair : MonoBehaviour
{
    const float CapHeight = 0.75f;
    Rigidbody2D rb;

    public void Build(Sprite body, Sprite cap, float gapCenter, float gap)
    {
        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;

        float gapBottom = gapCenter - gap * 0.5f;
        float gapTop = gapCenter + gap * 0.5f;

        // pipa bawah: kepala menempel di tepi bawah celah, badan turun sampai ke tanah
        Part(body, cap, gapBottom - CapHeight * 0.5f, GameManager.GroundTop - 0.5f, gapBottom - CapHeight);
        // pipa atas: kepala menempel di tepi atas celah, badan naik sampai di luar layar
        Part(body, cap, gapTop + CapHeight * 0.5f, gapTop + CapHeight, 7.5f);

        // zona penambah score di tengah celah
        var z = new GameObject("ScoreZone");
        z.transform.SetParent(transform, false);
        z.transform.localPosition = new Vector3(0f, gapCenter, 0f);
        var bc = z.AddComponent<BoxCollider2D>();
        bc.isTrigger = true;
        bc.size = new Vector2(0.3f, gap);
        z.AddComponent<ScoreZone>();
    }

    void Part(Sprite body, Sprite cap, float capY, float bodyY0, float bodyY1)
    {
        float len = Mathf.Max(0.01f, bodyY1 - bodyY0);

        var b = new GameObject("Body");
        b.layer = GameManager.ObstacleLayer;
        b.transform.SetParent(transform, false);
        b.transform.localPosition = new Vector3(0f, (bodyY0 + bodyY1) * 0.5f, 0f);
        b.transform.localScale = new Vector3(1f, len / body.bounds.size.y, 1f);
        var bsr = b.AddComponent<SpriteRenderer>();
        bsr.sprite = body;
        bsr.sortingOrder = 1;
        var bcol = b.AddComponent<BoxCollider2D>();
        bcol.size = body.bounds.size;

        var c = new GameObject("Cap");
        c.layer = GameManager.ObstacleLayer;
        c.transform.SetParent(transform, false);
        c.transform.localPosition = new Vector3(0f, capY, 0f);
        var csr = c.AddComponent<SpriteRenderer>();
        csr.sprite = cap;
        csr.sortingOrder = 2;
        var ccol = c.AddComponent<BoxCollider2D>();
        ccol.size = cap.bounds.size;
    }

    void FixedUpdate()
    {
        var gm = GameManager.Instance;
        if (gm == null) return;
        rb.MovePosition(rb.position + Vector2.left * gm.WorldSpeed * Time.fixedDeltaTime);
        if (rb.position.x < -(gm.HalfWidth + 3f)) Destroy(gameObject);
    }
}

/// <summary>Trigger di celah pipa: +1 score ketika burung melewatinya.</summary>
public class ScoreZone : MonoBehaviour
{
    bool used;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (used) return;
        var bird = other.GetComponent<BirdController>();
        if (bird == null || !bird.Alive) return;
        used = true;
        GameManager.Instance.AddScore();
    }
}
