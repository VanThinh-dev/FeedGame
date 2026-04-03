using System.Collections;
using UnityEngine;
using TMPro;

public class BubbleController : MonoBehaviour
{
    [Header("Sprites")]
    [SerializeField] private Sprite normalSprite;
    [SerializeField] private Sprite poppedSprite;   // Sprite vỡ — dùng lại như cũ

    [Header("Components")]
    private SpriteRenderer spriteRenderer;
    private Collider2D bubbleCollider;

    [Header("Effects")]
    [SerializeField] private ParticleSystem popEffect;
    [SerializeField] private AudioClip popSound;

    [Header("Word Display")]
    [SerializeField] private TextMeshPro textMesh;

    [Header("Hidden Fish")]
    [SerializeField] private SpriteRenderer fishSprite;

    private string word;
    private bool isPopped = false;

  private void Awake()
{
    spriteRenderer = GetComponent<SpriteRenderer>();
    bubbleCollider = GetComponent<Collider2D>();
    
    // Load từ Resources — không bao giờ bị strip khi build Android
    normalSprite = Resources.Load<Sprite>("Sprites/bubble_normal");
    poppedSprite = Resources.Load<Sprite>("Sprites/bubble_crack");
    
    if (normalSprite == null)
        Debug.LogError("[Bubble] KHÔNG TÌM THẤY bubble_normal! Kiểm tra tên file trong Resources/Sprites/");
    else
        spriteRenderer.sprite = normalSprite;
}

   private void Start()
{
    // Xóa dòng set sprite ở đây
    if (fishSprite != null)
    {
        fishSprite.enabled = false;
        fishSprite.gameObject.SetActive(false);
    }
}

    public void Init(string wordData)
    {
        word = wordData;

        if (textMesh != null)
        {
            textMesh.text = word;
            textMesh.transform.localPosition = Vector3.zero;
            textMesh.alignment = TextAlignmentOptions.Center;
            textMesh.transform.localScale = Vector3.one;
        }
        else
        {
            Debug.LogError("TextMeshPro chua duoc gan!");
        }
    }

    public string GetWord() => word;

    private void OnMouseDown()
    {
        if (isPopped) return;
        if (GameManager.Instance != null)
            GameManager.Instance.OnBubbleClicked(this);
        else
            Debug.LogError("GameManager NULL");
    }

    public void Pop()
    {
        if (isPopped) return;
        isPopped = true;

        bubbleCollider.enabled = false;

        // Spawn cá ngay lập tức trước khi chạy animation vỡ
        SpawnFish();

        // Âm thanh pop
        if (popSound != null)
            AudioSource.PlayClipAtPoint(popSound, Camera.main.transform.position);

        // Particle nếu có
        if (popEffect != null)
        {
            ParticleSystem fx = Instantiate(popEffect, transform.position, Quaternion.identity);
            fx.Play();
            Destroy(fx.gameObject, 1f);
        }

        BubbleManager.Instance.OnBubblePopped(this);

        // Chạy animation: hiện sprite vỡ → scale lên nhẹ → fade out mượt
        StartCoroutine(PopAnimation());
    }

    // ── ANIMATION VỠ: sprite cũ → phình nhẹ → mờ dần ────────────────────
    private IEnumerator PopAnimation()
    {
        // Bước 1: Đổi sang sprite vỡ ngay lập tức
        spriteRenderer.sprite = poppedSprite;

        // Ẩn text khi vỡ
        if (textMesh != null)
            textMesh.gameObject.SetActive(false);

        // Bước 2: Scale phình nhẹ lên (tạo cảm giác "nổ tung")
        float punchDuration = 0.12f;
        float elapsed = 0f;
        while (elapsed < punchDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / punchDuration;
            // Scale từ 1 → 1.3 rồi về 1.1 (overshoot nhỏ)
            float scale = Mathf.Lerp(1f, 1.3f, Mathf.Sin(t * Mathf.PI));
            transform.localScale = Vector3.one * scale;
            yield return null;
        }

        // Bước 3: Fade out mượt (sprite + text cùng mờ dần)
        float fadeDuration = 0.2f;
        elapsed = 0f;
        Color spriteColor = spriteRenderer.color;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, elapsed / fadeDuration);
            spriteRenderer.color = new Color(spriteColor.r, spriteColor.g, spriteColor.b, alpha);
            // Scale nhỏ dần cùng lúc fade — mượt hơn so với biến mất đột ngột
            transform.localScale = Vector3.one * Mathf.Lerp(1.1f, 0.8f, elapsed / fadeDuration);
            yield return null;
        }

        Destroy(gameObject);
    }

    private void SpawnFish()
    {
        if (fishSprite == null) return;

        fishSprite.gameObject.SetActive(true);
        fishSprite.enabled = true;
        fishSprite.transform.parent = null;
        fishSprite.transform.position = transform.position;

        FishItem fish = fishSprite.GetComponent<FishItem>();
        if (fish == null)
            fish = fishSprite.gameObject.AddComponent<FishItem>();

        Rigidbody2D rb = fishSprite.GetComponent<Rigidbody2D>();
        if (rb == null)
            rb = fishSprite.gameObject.AddComponent<Rigidbody2D>();

        Collider2D col = fishSprite.GetComponent<Collider2D>();
        if (col == null)
        {
            BoxCollider2D box = fishSprite.gameObject.AddComponent<BoxCollider2D>();
            box.isTrigger = false;
            box.size = new Vector2(0.5f, 0.5f);
        }

        fish.StartFalling();
    }
}