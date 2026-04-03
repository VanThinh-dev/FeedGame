using System.Collections;
using UnityEngine;

// Gắn vào GameManager GameObject
// GameManager gọi Play() khi hoàn thành toàn bộ từ vựng
public class ConfettiEffect : MonoBehaviour
{
    [SerializeField] private int   pieceCount   = 60;   // Số mảnh confetti
    [SerializeField] private float fallDuration = 3f;   // Thời gian rơi
    [SerializeField] private float spawnWidth   = 10f;  // Chiều rộng khu vực spawn

    // Màu cầu vồng cho confetti
    private static readonly Color[] confettiColors = {
        new Color(1f,   0.3f, 0.3f),
        new Color(1f,   0.8f, 0.1f),
        new Color(0.3f, 0.9f, 0.3f),
        new Color(0.3f, 0.6f, 1f),
        new Color(0.9f, 0.4f, 1f),
        new Color(1f,   0.55f,0.1f),
        new Color(0.1f, 0.9f, 0.8f),
    };

    // ── GỌI TỪ GameManager KHI HOÀN THÀNH ───────────────────────────────
    public void Play()
    {
        StartCoroutine(SpawnConfetti());
    }

    private IEnumerator SpawnConfetti()
    {
        Camera cam = Camera.main;

        // Vị trí spawn: trên màn hình
        Vector3 topLeft  = cam.ViewportToWorldPoint(new Vector3(0f, 1.05f, Mathf.Abs(cam.transform.position.z)));
        Vector3 topRight = cam.ViewportToWorldPoint(new Vector3(1f, 1.05f, Mathf.Abs(cam.transform.position.z)));
        topLeft.z = topRight.z = 0f;

        for (int i = 0; i < pieceCount; i++)
        {
            // Spawn rải đều theo thời gian để không xuất hiện cùng lúc
            yield return new WaitForSeconds(fallDuration / pieceCount);
            SpawnOnePiece(topLeft, topRight);
        }
    }

    private void SpawnOnePiece(Vector3 topLeft, Vector3 topRight)
    {
        GameObject piece = new GameObject("Confetti");

        // Vị trí random trên đỉnh màn hình
        float t = Random.Range(0f, 1f);
        piece.transform.position = Vector3.Lerp(topLeft, topRight, t) + Vector3.up * Random.Range(0f, 1f);

        // Sprite hình vuông nhỏ hoặc hình chữ nhật (ngẫu nhiên)
        SpriteRenderer sr = piece.AddComponent<SpriteRenderer>();
        sr.sprite = CreateRectSprite(
            Random.Range(6, 14),
            Random.Range(6, 14)
        );
        sr.color        = confettiColors[Random.Range(0, confettiColors.Length)];
        sr.sortingOrder = 20;

        // Chạy animation rơi + xoay + mờ dần
        EffectRunner.Run(FallAndFade(piece));
    }

    private IEnumerator FallAndFade(GameObject piece)
    {
        if (piece == null) yield break;

        SpriteRenderer sr = piece.GetComponent<SpriteRenderer>();
        Vector3 startPos  = piece.transform.position;

        // Mỗi mảnh có tốc độ và hướng lắc riêng
        float fallSpeed   = Random.Range(2f, 5f);
        float swaySpeed   = Random.Range(1f, 3f);
        float swayAmount  = Random.Range(0.2f, 0.6f);
        float rotateSpeed = Random.Range(90f, 360f) * (Random.value > 0.5f ? 1f : -1f);

        float elapsed = 0f;
        Color baseColor = sr.color;

        while (elapsed < fallDuration)
        {
            if (piece == null) yield break;
            elapsed += Time.deltaTime;
            float progress = elapsed / fallDuration;

            // Rơi xuống + lắc ngang
            float x = startPos.x + Mathf.Sin(elapsed * swaySpeed) * swayAmount;
            float y = startPos.y - elapsed * fallSpeed;
            piece.transform.position = new Vector3(x, y, 0f);

            // Xoay
            piece.transform.Rotate(0f, 0f, rotateSpeed * Time.deltaTime);

            // Mờ dần ở 70% cuối
            float alpha = progress < 0.3f ? 1f : 1f - ((progress - 0.3f) / 0.7f);
            sr.color = new Color(baseColor.r, baseColor.g, baseColor.b, alpha);

            yield return null;
        }

        Destroy(piece);
    }

    // Tạo sprite hình chữ nhật bằng code — không cần asset
    private Sprite CreateRectSprite(int w, int h)
    {
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;

        Color[] pixels = new Color[w * h];
        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = Color.white;

        tex.SetPixels(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, w, h), Vector2.one * 0.5f, Mathf.Max(w, h));
    }
}