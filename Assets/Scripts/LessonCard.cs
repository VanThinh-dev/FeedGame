using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// ─────────────────────────────────────────────────────────────────────────────
// LessonCard.cs
//
// Gắn vào LessonCardPrefab.
// Nhận LessonData từ VocabManager → hiển thị tên bài, số từ, trạng thái.
// Tap vào card → callback OnCardTapped(LessonData).
// ─────────────────────────────────────────────────────────────────────────────

public class LessonCard : MonoBehaviour
{
    // ── UI Slots — kéo từ Inspector ───────────────────────────────────────────
    [Header("Background & Icon")]
    [SerializeField] private Image     cardBackground;   // Image nền card
    [SerializeField] private Image     bookIcon;         // Icon sách

    [Header("Sprites — kéo ảnh vào đây")]
    [SerializeField] private Sprite    spriteNormal;     // Nền card chưa học
    [SerializeField] private Sprite    spriteCompleted;  // Nền card đã học (màu khác)
    [SerializeField] private Sprite    spriteBook;       // Icon sách mặc định
    [SerializeField] private Sprite    spriteBookDone;   // Icon sách khi đã học (tùy chọn)

    [Header("Text")]
    [SerializeField] private TextMeshProUGUI lessonNameText;   // Tên bài học
    [SerializeField] private TextMeshProUGUI wordCountText;    // "12 từ"
    [SerializeField] private TextMeshProUGUI completedBadge;   // "✓ Đã học" — ẩn nếu chưa học

    [Header("Button")]
    [SerializeField] private Button tapButton;

    // ── Runtime data ──────────────────────────────────────────────────────────
    private LessonData          lessonData;
    private Action<LessonData>  onTapCallback;

    // ═════════════════════════════════════════════════════════════════════════
    // SETUP
    // ═════════════════════════════════════════════════════════════════════════

    /// <summary>VocabManager gọi sau khi Instantiate prefab.</summary>
    public void Setup(LessonData data, Action<LessonData> onTap)
    {
        lessonData    = data;
        onTapCallback = onTap;

        // ── Tên bài ──
        if (lessonNameText != null)
            lessonNameText.text = data.name;

        // ── Số từ ──
        if (wordCountText != null)
            wordCountText.text = data.wordCount > 0
                ? $"{data.wordCount} từ"
                : "0 từ";

        // ── Badge đã học ──
        if (completedBadge != null)
            completedBadge.gameObject.SetActive(data.isCompleted);

        // ── Nền card ──
        if (cardBackground != null)
        {
            if (data.isCompleted && spriteCompleted != null)
                cardBackground.sprite = spriteCompleted;
            else if (spriteNormal != null)
                cardBackground.sprite = spriteNormal;
        }

        // ── Icon sách ──
        if (bookIcon != null)
        {
            if (data.isCompleted && spriteBookDone != null)
                bookIcon.sprite = spriteBookDone;
            else if (spriteBook != null)
                bookIcon.sprite = spriteBook;
        }

        // ── Button listener ──
        if (tapButton != null)
        {
            tapButton.onClick.RemoveAllListeners();
            tapButton.onClick.AddListener(OnTapped);
        }
        else
        {
            // Fallback: dùng Button trên chính GameObject này
            var btn = GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(OnTapped);
            }
        }
    }

    // ── Tap handler ──────────────────────────────────────────────────────────
    private void OnTapped()
    {
        Debug.Log($"[LessonCard] Tap: {lessonData?.name}");
        onTapCallback?.Invoke(lessonData);
    }

    // ── Public getter ─────────────────────────────────────────────────────────
    public LessonData GetLessonData() => lessonData;
}
