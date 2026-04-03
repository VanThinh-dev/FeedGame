using System.Collections.Generic;
using UnityEngine;

public class BubbleManager : MonoBehaviour
{
    public static BubbleManager Instance;

    [Header("Prefab")]
    [SerializeField] private GameObject bubblePrefab;

    [Header("Grid Settings")]
    [SerializeField] private int rows    = 3;
    [SerializeField] private int columns = 3;
    [SerializeField] private float spacing   = 1.5f;
    [SerializeField] private float topOffset = 2f;

    private List<BubbleController> bubbles = new List<BubbleController>();

    private void Awake() { Instance = this; }

    // --- [COMMENTED OUT] Start() tự SpawnGrid ---
    // LÝ DO: GameManager gọi SpawnGrid() sau khi Firebase load + SetNextTarget xong
    //        Nếu Start() tự spawn, currentTarget còn null → không có đáp án đúng
    // private void Start() { SpawnGrid(); }

    // ── SPAWN LƯỚI BUBBLE ─────────────────────────────────────────────────
    public void SpawnGrid()
    {
        ClearAllBubbles(); // Dọn sạch bubble cũ trước khi spawn mới

        Camera cam = Camera.main;
        Vector3 topCenter = cam.ViewportToWorldPoint(
            new Vector3(0.5f, 1f, Mathf.Abs(cam.transform.position.z))
        );
        topCenter.z = 0;
        Vector3 startPos = topCenter + Vector3.down * topOffset;

        int total = rows * columns;

        // Build danh sách từ: 1 đáp án đúng + (total-1) từ khác, không trùng
        List<string> wordPool = BuildWordPool(total);
        ShuffleList(wordPool); // Xáo trộn vị trí để đáp án không luôn ở slot cố định

        int index = 0;
        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < columns; col++)
            {
                Vector3 pos = startPos + new Vector3(
                    (col - (columns - 1) / 2f) * spacing,
                    -row * spacing, 0
                );

                GameObject obj = Instantiate(bubblePrefab, pos, Quaternion.identity);
                BubbleController bubble = obj.GetComponent<BubbleController>();

                // Tự AddComponent BubbleAnimator nếu prefab chưa có
                // Không cần gắn tay trong Inspector prefab
                BubbleAnimator anim = obj.GetComponent<BubbleAnimator>();
                if (anim == null)
                    anim = obj.AddComponent<BubbleAnimator>();

                bubble.Init(wordPool[index]);

                // Hiệu ứng bounce in lần lượt
                anim.PlayBounceIn(delay: index * 0.1f);
                bubbles.Add(bubble);
                index++;
            }
        }

        Debug.Log($"[BubbleManager] Spawned {total} bubbles. Answer: '{GameManager.Instance.GetCurrentTargetEnglish()}' is guaranteed in grid.");
    }

    // ── XÂY DỰNG WORD POOL ────────────────────────────────────────────────
    // Đảm bảo: đúng 1 bubble là đáp án đúng, các bubble còn lại KHÔNG trùng nhau
    private List<string> BuildWordPool(int total)
    {
        string targetWord    = GameManager.Instance.GetCurrentTargetEnglish();
        List<string> allWords = GameManager.Instance.GetAllEnglishWords();

        // Tách các từ khác (loại bỏ targetWord)
        List<string> otherWords = new List<string>();
        foreach (string w in allWords)
            if (w != targetWord) otherWords.Add(w);

        ShuffleList(otherWords); // Xáo trộn để lấy random

        // Bắt đầu bằng đúng 1 đáp án đúng
        List<string> result = new List<string> { targetWord };

        // Fill các slot còn lại bằng từ khác, không trùng
        for (int i = 0; i < total - 1; i++)
        {
            if (otherWords.Count == 0)
            {
                // Trường hợp vocab quá ít (ít hơn số bubble): báo lỗi, dừng fill
                Debug.LogWarning($"[BubbleManager] Không đủ từ vựng cho {total} bubble. Cần ít nhất {total} từ trong Firebase.");
                break;
            }

            // Lấy từ ở đầu danh sách (đã shuffle), sau đó xóa để không bị lặp lại
            result.Add(otherWords[0]);
            otherWords.RemoveAt(0);
        }

        return result;
    }

    // ── XỬ LÝ KHI BUBBLE NỔ ──────────────────────────────────────────────
    public void OnBubblePopped(BubbleController bubble)
    {
        bubbles.Remove(bubble);

        // Hết bubble → GameManager.OnFishEaten() đã set target mới → spawn lưới mới
        // if (bubbles.Count == 0)
        //     Invoke(nameof(SpawnGrid), 0.5f);
    }

    // ── XÓA TOÀN BỘ BUBBLE ────────────────────────────────────────────────
    // Dùng khi Reset hoặc ShowCompletionMessage
    public void ClearAllBubbles()
    {
        foreach (var b in bubbles)
            if (b != null) Destroy(b.gameObject);
        bubbles.Clear();
    }

    private void ShuffleList<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0));
    }
}