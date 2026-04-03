using UnityEngine;

public class FishItem : MonoBehaviour
{
    private Rigidbody2D rb;
    private bool landed = false;

    // CatController dùng hàm này để biết cá đã chạm đất chưa
    public bool HasLanded() => landed;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb != null) rb.gravityScale = 0; // chưa rơi khi mới tạo
    }

    public void StartFalling()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            // Rơi thẳng xuống, không xoay, không trượt ngang
            rb.gravityScale = 2f;
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.linearDamping = 0f;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation
                           | RigidbodyConstraints2D.FreezePositionX; // chỉ di chuyển trục Y
        }

        // Đảm bảo collider đúng size, không phải trigger để va chạm với Ground
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            col.isTrigger = false;
            if (col is BoxCollider2D box)
                box.size = new Vector2(0.5f, 0.5f);
        }

        // Ignore collision với tất cả bubble để cá không bị chặn khi rơi
        IgnoreAllBubbles();
    }

    private void IgnoreAllBubbles()
    {
        Collider2D fishCol = GetComponent<Collider2D>();
        if (fishCol == null) return;

        // Tìm tất cả bubble trong scene và bỏ qua collision với từng cái
        BubbleController[] allBubbles = FindObjectsByType<BubbleController>(FindObjectsSortMode.None);
        foreach (BubbleController bubble in allBubbles)
        {
            Collider2D bubbleCol = bubble.GetComponent<Collider2D>();
            if (bubbleCol != null)
                Physics2D.IgnoreCollision(fishCol, bubbleCol, true); // cá xuyên qua bong bóng
        }
    }

    // Va chạm vật lý với Ground → cá dừng lại
    private void OnCollisionEnter2D(Collision2D col)
    {
        if (landed) return;

        if (col.gameObject.CompareTag("Ground"))
        {
            landed = true;
            rb.linearVelocity = Vector2.zero;
            rb.gravityScale = 0;
            rb.constraints = RigidbodyConstraints2D.FreezeAll; // đứng yên hoàn toàn
        }
    }

    // CatController gọi hàm này khi mèo ăn cá
    public void Disappear()
    {
        Destroy(gameObject);
    }
}