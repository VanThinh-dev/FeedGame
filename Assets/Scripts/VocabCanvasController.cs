using UnityEngine;
using UnityEngine.UI;
using TMPro;

// ─────────────────────────────────────────────────────────────────────────────
// VocabCanvasController.cs
//
// Gắn vào: VocabCanvas (root GameObject)
// Chịu trách nhiệm:
//   • Nút X đóng canvas
//   • Tab Đã học / Chưa học switching
//   • Nút "+" mở AddLessonPanel
// ─────────────────────────────────────────────────────────────────────────────

public class VocabCanvasController : MonoBehaviour
{
    // ── UI Refs ───────────────────────────────────────────────────────────────
    [Header("Buttons")]
    [SerializeField] private Button closeButton;        // Nút X góc phải trên
    [SerializeField] private Button tabDaHocButton;     // Tab "Đã học"
    [SerializeField] private Button tabChuaHocButton;   // Tab "Chưa học"
    [SerializeField] private Button addButton;          // Nút "+" mở AddLessonPanel

    [Header("Tab Panels")]
    [SerializeField] private GameObject daHocPanel;
    [SerializeField] private GameObject chuaHocPanel;

    [Header("Add Lesson Popup")]
    [SerializeField] private GameObject addLessonPanel;

    [Header("Tab Indicator (tùy chọn — underline hoặc đổi màu)")]
    [SerializeField] private Image tabDaHocIndicator;
    [SerializeField] private Image tabChuaHocIndicator;
    [SerializeField] private Color tabActiveColor   = new Color(0.2f, 0.6f, 1f);
    [SerializeField] private Color tabInactiveColor = new Color(0.7f, 0.7f, 0.7f);

    // ═════════════════════════════════════════════════════════════════════════
    // LIFECYCLE
    // ═════════════════════════════════════════════════════════════════════════

    private void Awake()
    {
        if (closeButton      != null) closeButton.onClick.AddListener(OnClose);
        if (tabDaHocButton   != null) tabDaHocButton.onClick.AddListener(OnTabDaHoc);
        if (tabChuaHocButton != null) tabChuaHocButton.onClick.AddListener(OnTabChuaHoc);
        if (addButton        != null) addButton.onClick.AddListener(OnAddLesson);
    }

    private void OnEnable()
    {
        // Mặc định mở tab Chưa học khi mở canvas
        ShowTabChuaHoc();
    }

    // ═════════════════════════════════════════════════════════════════════════
    // TAB HANDLERS
    // ═════════════════════════════════════════════════════════════════════════

    private void OnTabDaHoc()
    {
        ShowTabDaHoc();
        VocabManager.Instance?.ShowTabDaHoc();
    }

    private void OnTabChuaHoc()
    {
        ShowTabChuaHoc();
        VocabManager.Instance?.ShowTabChuaHoc();
    }

    private void ShowTabDaHoc()
    {
        if (daHocPanel   != null) daHocPanel.SetActive(true);
        if (chuaHocPanel != null) chuaHocPanel.SetActive(false);
        UpdateTabIndicators(true);
    }

    private void ShowTabChuaHoc()
    {
        if (daHocPanel   != null) daHocPanel.SetActive(false);
        if (chuaHocPanel != null) chuaHocPanel.SetActive(true);
        UpdateTabIndicators(false);
    }

    private void UpdateTabIndicators(bool daHocActive)
    {
        if (tabDaHocIndicator   != null) tabDaHocIndicator.color   = daHocActive  ? tabActiveColor : tabInactiveColor;
        if (tabChuaHocIndicator != null) tabChuaHocIndicator.color = !daHocActive ? tabActiveColor : tabInactiveColor;
    }

    // ═════════════════════════════════════════════════════════════════════════
    // CLOSE / ADD
    // ═════════════════════════════════════════════════════════════════════════

    private void OnClose()
    {
        VocabManager.Instance?.CloseVocabCanvas();
        // Fallback nếu VocabManager chưa có
        gameObject.SetActive(false);
    }

    private void OnAddLesson()
    {
        if (addLessonPanel != null)
            addLessonPanel.SetActive(true);
        else
            Debug.LogWarning("[VocabCanvasController] addLessonPanel chưa gán!");
    }
}
