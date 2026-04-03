using UnityEngine;
using UnityEngine.UI;

// ─────────────────────────────────────────────────────────────────────────────
// DeskHitArea.cs
// Gắn vào: DeskHitArea GameObject trong BedroomCanvas
// Tap vào vùng bàn học → mở VocabCanvas
// ─────────────────────────────────────────────────────────────────────────────

public class DeskHitArea : MonoBehaviour
{
    [Header("Fallback — kéo VocabCanvas vào nếu VocabManager không tìm được")]
    [SerializeField] private GameObject vocabCanvasDirect;

    private void Start()
    {
        // Đảm bảo có Button để nhận input
        var btn = GetComponent<Button>();
        if (btn == null)
        {
            btn = gameObject.AddComponent<Button>();
            Debug.LogWarning("[DeskHitArea] Không có Button — đã tự thêm. " +
                             "Kiểm tra Image component để hit detection hoạt động.");
        }

        btn.onClick.AddListener(OnTap);

        // Đảm bảo có Image để raycast hit (dù trong suốt)
        var img = GetComponent<Image>();
        if (img == null)
        {
            img = gameObject.AddComponent<Image>();
            img.color = new Color(1, 1, 1, 0.01f);
        }
    }

    private void OnTap()
    {
        Debug.Log("[DeskHitArea] Tap bàn học!");

        // Cách 1: Qua VocabManager (singleton)
        if (VocabManager.Instance != null)
        {
            VocabManager.Instance.OpenVocabCanvas();
            return;
        }

        // Cách 2: Qua BedroomManager
        if (BedroomManager.Instance != null)
        {
            BedroomManager.Instance.OpenVocabCanvas();
            return;
        }

        // Cách 3: Direct reference (kéo vào Inspector)
        if (vocabCanvasDirect != null)
        {
            vocabCanvasDirect.SetActive(true);
            return;
        }

        // Không tìm được — thử tìm trong scene
        var vc = GameObject.Find("VocabCanvas");
        if (vc != null)
        {
            vc.SetActive(true);
            Debug.LogWarning("[DeskHitArea] Dùng fallback FindObject. " +
                             "Gán VocabManager hoặc vocabCanvasDirect vào Inspector.");
        }
        else
        {
            Debug.LogError("[DeskHitArea] ❌ Không mở được VocabCanvas — " +
                           "VocabManager.Instance null và không tìm thấy 'VocabCanvas' trong scene!");
        }
    }
}