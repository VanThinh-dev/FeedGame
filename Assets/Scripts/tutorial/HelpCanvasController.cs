// Assets/Scripts/Tutorial/HelpCanvasController.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HelpCanvasController : MonoBehaviour
{
    [Header("=== Buttons ===")]
    [SerializeField] private Button tabFunctionBtn;
    [SerializeField] private Button tabHowToPlayBtn;
    [SerializeField] private Button closeButton;

    [Header("=== Panels ===")]
    [SerializeField] private GameObject functionPanel;
    [SerializeField] private GameObject howToPlayPanel;

    [Header("=== Text content ===")]
    [SerializeField] private TextMeshProUGUI functionText;
    [SerializeField] private TextMeshProUGUI howToPlayText;

    [Header("=== Tab màu ===")]
    [SerializeField] private Color activeColor   = new Color(0.2f, 0.6f, 1f);
    [SerializeField] private Color inactiveColor = new Color(0.3f, 0.3f, 0.3f);

    void Awake()
    {
        if (functionText)
            functionText.text =
                "<b>Bàn học</b>\nHọc từ vựng theo bài, tạo bộ từ mới\n1.Ấn vào bàn học để bắt đầu -> 2.chọn bài\nTạo bài mới\n1.Ấn vào chưa học -> 2.Ấn vào dấu cộng -> thêm từ tối thiểu 9 từ\n\n" +
                "<b>Huy chương</b>\nXem bộ sưu tập huy chương đã đạt\n 1.Ấn vào kệ huy chương\n" +
                "<b>Cửa hàng</b>\nMua vật phẩm, đổi huy chương\n1.Ấn vào ảnh avatar -> 2.Ấn vào cửa hàng 3.Ấn vào vật phẩm cần mua.\nĐổi Huy chương\n1.Ấn vào huy tab đổi huy chương\n\n " +
                "<b>Túi đồ</b>\nXem vật phẩm đã mua\n1.Ấn vào ảnh avatar -> 2.Ấn vào túi đồ\n\n" +
                "<b>Chuyển Phòng</b>\n1.Ấn vào ảnh avatar -> chọn phòng học hoặc phòng chơi";

        if (howToPlayText)
            howToPlayText.text =
                "1️  Chọn bài học từ bàn học\n\n" +
                "2️  Bong bóng có từ tiếng Anh bay lên\n\n" +
                "3️  Ấn vào bong bóng đúng với nghĩa tiếng Việt hiện ở dưới\n\n" +
                "4️  Đúng → ghi điểm bóng vỡ\n\n" +
                "5️  Hoàn thành bài → nhận huy chương và phần thưởng";

        SwitchTab(0);
    }

    void Start()
    {
        if (tabFunctionBtn)  tabFunctionBtn.onClick.AddListener(() => SwitchTab(0));
        if (tabHowToPlayBtn) tabHowToPlayBtn.onClick.AddListener(() => SwitchTab(1));
        if (closeButton)     closeButton.onClick.AddListener(() => gameObject.SetActive(false));
    }

    private void SwitchTab(int index)
    {
        if (functionPanel)  functionPanel.SetActive(index == 0);
        if (howToPlayPanel) howToPlayPanel.SetActive(index == 1);
        SetTabColor(tabFunctionBtn,  index == 0);
        SetTabColor(tabHowToPlayBtn, index == 1);
    }

    private void SetTabColor(Button btn, bool active)
    {
        if (!btn) return;
        var img = btn.GetComponent<Image>();
        if (img) img.color = active ? activeColor : inactiveColor;
    }
}