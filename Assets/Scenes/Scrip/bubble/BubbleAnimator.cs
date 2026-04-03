using System.Collections;
using UnityEngine;

public class BubbleAnimator : MonoBehaviour
{
    [Header("Spawn In")]
    [SerializeField] private float spawnDuration = 0.5f;   // Thời gian xuất hiện

    [Header("Idle Float")]
    [SerializeField] private float floatAmplitude = 0.12f; // Biên độ lắc lư
    [SerializeField] private float floatSpeed     = 1.2f;  // Tốc độ lắc lư

    // --- [COMMENTED OUT] Bounce In cũ với elastic overshoot ---
    // LÝ DO: Thay bằng ease mượt hơn, bỏ cảm giác cứng nhắc
    // [SerializeField] private float bounceInDuration = 0.45f;
    // [SerializeField] private float overshoot        = 1.25f;

    // --- [COMMENTED OUT] Pop Burst màu sắc ---
    // LÝ DO: Dùng lại sprite vỡ của bubble thay vì tạo mảnh vỡ bằng code
    // [SerializeField] private int   burstCount    = 8;
    // [SerializeField] private float burstDistance = 1.0f;
    // [SerializeField] private float burstDuration = 0.35f;

    private Vector3 originPos;
    private bool isFloating = false;
    private Coroutine floatRoutine;

    private void Awake()
    {
        // Bắt đầu trong suốt + nhỏ
        transform.localScale = Vector3.zero;
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null) sr.color = new Color(1f, 1f, 1f, 0f);
    }

    // ── XUẤT HIỆN MỀM MẠI: fade in + scale up mượt ───────────────────────
    public void PlayBounceIn(float delay = 0f)
    {
        originPos = transform.position;
        StartCoroutine(SmoothSpawnIn(delay));
    }

    private IEnumerator SmoothSpawnIn(float delay)
    {
        if (delay > 0f) yield return new WaitForSeconds(delay);

        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        
        float elapsed = 0f;
        while (elapsed < spawnDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / spawnDuration;

            // EaseOutBack: scale vượt nhẹ qua 1 rồi settle — mượt hơn elastic
            float scale = EaseOutBack(t);
            transform.localScale = Vector3.one * scale;

            // Fade in alpha cùng lúc
            float alpha = Mathf.Clamp01(t * 2f); // Hiện nhanh ở nửa đầu
            if (sr != null)
                sr.color = new Color(1f, 1f, 1f, alpha);

            yield return null;
        }

        transform.localScale = Vector3.one;
        if (sr != null) sr.color = Color.white;

        // Sau khi vào xong → lắc lư nhẹ
        floatRoutine = StartCoroutine(FloatRoutine());
    }

    // Lắc lư lên xuống nhẹ nhàng
    private IEnumerator FloatRoutine()
    {
        isFloating = true;
        float offset = Random.Range(0f, Mathf.PI * 2f);

        while (isFloating)
        {
            float y = Mathf.Sin(Time.time * floatSpeed + offset) * floatAmplitude;
            transform.position = originPos + new Vector3(0f, y, 0f);
            yield return null;
        }
    }

    // Dừng lắc khi bubble bị pop (gọi từ BubbleController nếu cần)
    public void StopFloat()
    {
        isFloating = false;
        if (floatRoutine != null) StopCoroutine(floatRoutine);
    }

    // ── EASING: EaseOutBack — mượt, vượt nhẹ rồi settle, không cứng ──────
    // So sánh: Elastic = nảy nhiều lần → cứng | Back = vượt 1 lần → mượt
    private float EaseOutBack(float t)
    {
        float c1 = 1.70158f;
        float c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
    }
}