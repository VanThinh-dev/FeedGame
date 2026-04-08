using UnityEngine;
using UnityEngine.UI;
using TMPro;

// ─────────────────────────────────────────────────────────────────────────────
// VocabCanvasController.cs — v2  (FIXED)
//
// FIX so với v1:
//   • OnEnable() đồng bộ cả panel cục bộ lẫn VocabManager (trước chỉ
//     gọi ShowTabChuaHoc() cục bộ mà quên notify VocabManager).
//   • VocabCanvasController là nguồn sự thật DUY NHẤT cho việc
//     bật/tắt daHocPanel / chuaHocPanel → VocabManager không cần ref panels.
//   • Guard null cho tất cả SerializeField trước khi dùng.
//   • Tab indicator update được tách riêng để dễ mở rộng sau.
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
    [SerializeField] private GameObject daHocPanel;     // Panel chứa danh sách bài đã học
    [SerializeField] private GameObject chuaHocPanel;   // Panel chứa danh sách bài chưa học

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
        // ✅ FIX: mặc định mở tab Chưa học khi canvas được bật,
        //    gọi SwitchToTabChuaHoc() thay vì ShowTabChuaHoc() để đảm bảo
        //    cả panels lẫn indicators đều được cập nhật đúng.
        SwitchToTabChuaHoc();
    }

    // ═════════════════════════════════════════════════════════════════════════
    // TAB HANDLERS — user tap
    // ═════════════════════════════════════════════════════════════════════════

    private void OnTabDaHoc()   => SwitchToTabDaHoc();
    private void OnTabChuaHoc() => SwitchToTabChuaHoc();

    // ═════════════════════════════════════════════════════════════════════════
    // TAB SWITCHING — nguồn sự thật duy nhất cho panels
    // ═════════════════════════════════════════════════════════════════════════

    private void SwitchToTabDaHoc()
    {
        SetPanels(daHocActive: true);
        UpdateTabIndicators(daHocActive: true);
        // Không cần notify VocabManager vì Manager đã bỏ panel management
    }

    private void SwitchToTabChuaHoc()
    {
        SetPanels(daHocActive: false);
        UpdateTabIndicators(daHocActive: false);
    }

    private void SetPanels(bool daHocActive)
    {
        if (daHocPanel   != null) daHocPanel.SetActive(daHocActive);
        if (chuaHocPanel != null) chuaHocPanel.SetActive(!daHocActive);
    }

    private void UpdateTabIndicators(bool daHocActive)
    {
        if (tabDaHocIndicator   != null)
            tabDaHocIndicator.color   = daHocActive  ? tabActiveColor : tabInactiveColor;
        if (tabChuaHocIndicator != null)
            tabChuaHocIndicator.color = !daHocActive ? tabActiveColor : tabInactiveColor;
    }

    // ═════════════════════════════════════════════════════════════════════════
    // CLOSE / ADD
    // ═════════════════════════════════════════════════════════════════════════

    private void OnClose()
    {
        if (VocabManager.Instance != null)
            VocabManager.Instance.CloseVocabCanvas();
        else
            gameObject.SetActive(false); // Fallback nếu VocabManager chưa có
    }

    private void OnAddLesson()
    {
        if (addLessonPanel != null)
            addLessonPanel.SetActive(true);
        else
            Debug.LogWarning("[VocabCanvasController] addLessonPanel chưa gán!");
    }

    // ═════════════════════════════════════════════════════════════════════════
    // PUBLIC API — cho phép code ngoài chuyển tab nếu cần
    // ═════════════════════════════════════════════════════════════════════════

    public void ShowDaHocTab()   => SwitchToTabDaHoc();
    public void ShowChuaHocTab() => SwitchToTabChuaHoc();
}