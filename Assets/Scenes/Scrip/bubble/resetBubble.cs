using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Gắn script này vào Button GameObject trên Canvas
// Script tự tạo Canvas + Button nếu chưa có (không cần setup tay)
public class ResetButton : MonoBehaviour
{
//     // Tự tạo Canvas + Button khi vào scene nếu chưa có
//     [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
//     private static void AutoCreate()
//     {
//         // Kiểm tra đã có ResetButton trong scene chưa
//         if (FindAnyObjectByType<ResetButton>() != null) return;

//         // Tạo Canvas
//         GameObject canvasObj = new GameObject("ResetCanvas");
//         Canvas canvas = canvasObj.AddComponent<Canvas>();
//         canvas.renderMode = RenderMode.ScreenSpaceOverlay;
//         canvas.sortingOrder = 99; // Luôn hiển thị trên cùng
//         canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
//         canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();

//         // Tạo Button
//         GameObject btnObj = new GameObject("ResetButton");
//         btnObj.transform.SetParent(canvasObj.transform, false);

//         // Hình nền button
//         Image img = btnObj.AddComponent<Image>();
//         img.color = new Color(0.2f, 0.6f, 1f, 0.9f); // Xanh dương

//         Button btn = btnObj.AddComponent<Button>();

//         // Đặt vị trí góc trên phải
//         RectTransform rect = btnObj.GetComponent<RectTransform>();
//         rect.anchorMin = new Vector2(1, 1);
//         rect.anchorMax = new Vector2(1, 1);
//         rect.pivot     = new Vector2(1, 1);
//         rect.anchoredPosition = new Vector2(-20, -20); // Cách góc 20px
//         rect.sizeDelta = new Vector2(130, 50);

//         // Text trên button
//         GameObject textObj = new GameObject("Text");
//         textObj.transform.SetParent(btnObj.transform, false);
//         TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
//         tmp.text      = "🔄 Reset";
//         tmp.fontSize  = 22;
//         tmp.alignment = TextAlignmentOptions.Center;
//         tmp.color     = Color.white;
//         RectTransform textRect = textObj.GetComponent<RectTransform>();
//         textRect.anchorMin = Vector2.zero;
//         textRect.anchorMax = Vector2.one;
//         textRect.offsetMin = Vector2.zero;
//         textRect.offsetMax = Vector2.zero;

//         // Gắn ResetButton script để xử lý click
//         ResetButton rb = btnObj.AddComponent<ResetButton>();
//         rb.button = btn;
//         btn.onClick.AddListener(rb.OnResetClicked);

//         Debug.Log("[ResetButton] Auto-created Reset button on Canvas.");
//     }

//     // Dùng trong trường hợp tự tạo qua AutoCreate
    [HideInInspector] public Button button;

    private void Awake()
    {
        // PATCH: Nếu không muốn dùng ResetButton thì disable luôn
        // if (!enabled) return;

        // Button b = GetComponent<Button>();
        // if (b != null && b != button)
        //     b.onClick.AddListener(OnResetClicked);
        return;
    }

    // private void OnDestroy()
    // {
    //     // PATCH: remove listener tránh bị gọi lại
    //     Button b = GetComponent<Button>();
    //     if (b != null)
    //         b.onClick.RemoveListener(OnResetClicked);
    // }

    private void OnResetClicked()
    {
        // if (GameManager.Instance != null)
        //     GameManager.Instance.ResetGame();
        // else
        //     Debug.LogError("[ResetButton] GameManager not found!");
        return;
    }
}