using System.Collections;
using UnityEngine;
using TMPro;

// Gắn vào cùng GameObject với TextMeshPro targetText
// GameManager gọi PlayNewTarget() mỗi khi đổi từ mới
public class TargetTextAnimator : MonoBehaviour
{
    [SerializeField] private float punchScale    = 1.4f;   // Phình to tối đa
    [SerializeField] private float punchDuration = 0.4f;   // Thời gian hiệu ứng

    private TextMeshPro tmp;
    private Vector3 originalScale;

    private void Awake()
    {
        tmp = GetComponent<TextMeshPro>();
        originalScale = transform.localScale;
    }

    // ── GỌI TỪ GameManager KHI ĐỔI TARGET MỚI ───────────────────────────
    public void PlayNewTarget()
    {
        StopAllCoroutines();
        StartCoroutine(PunchRoutine());
    }

    // Phình to rồi nảy về, kèm đổi màu cầu vồng
    private IEnumerator PunchRoutine()
    {
        float elapsed = 0f;

        // Màu cầu vồng random mỗi lần đổi từ — vui tươi cho trẻ em
        Color targetColor = Color.HSVToRGB(Random.Range(0f, 1f), 0.8f, 1f);

        while (elapsed < punchDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / punchDuration;

            // Scale: phình to rồi nảy về đúng size
            float scale = Mathf.Lerp(punchScale, 1f, EaseOutBounce(t));
            transform.localScale = originalScale * scale;

            // Màu: từ targetColor về trắng
            if (tmp != null)
                tmp.color = Color.Lerp(targetColor, Color.white, t);

            yield return null;
        }

        transform.localScale = originalScale;
        if (tmp != null) tmp.color = Color.white;
    }

    // Easing bounce — nảy lại như bóng cao su
    private float EaseOutBounce(float t)
    {
        if (t < 1f / 2.75f)       return 7.5625f * t * t;
        else if (t < 2f / 2.75f) { t -= 1.5f   / 2.75f; return 7.5625f * t * t + 0.75f; }
        else if (t < 2.5f / 2.75f){ t -= 2.25f  / 2.75f; return 7.5625f * t * t + 0.9375f; }
        else                      { t -= 2.625f / 2.75f; return 7.5625f * t * t + 0.984375f; }
    }
}