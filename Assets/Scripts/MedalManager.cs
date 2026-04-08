using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Firebase.Database;
using Firebase.Extensions;

// =============================================================================
// MedalManager.cs — v4
//
// FIX so với v3:
//   • AwardMedal() KHÔNG tự cộng ud.bronzeMedals/silverMedals/goldMedals nữa.
//     Việc cộng data là trách nhiệm của caller (RewardManager).
//     AwardMedal() chỉ đọc giá trị hiện tại trong UserData rồi lưu Firebase.
//
//   • Lý do: RewardManager đã cộng user.bronzeMedals trước khi gọi AwardMedal()
//     → nếu AwardMedal() cộng thêm lần nữa thì mỗi medal bị nhân đôi.
//
//   • Đổi tên thành SaveMedalToFirebase() cho rõ nghĩa, giữ AwardMedal() làm
//     wrapper public để không phải sửa các caller khác.
// =============================================================================

public class MedalManager : MonoBehaviour
{
    public static MedalManager Instance { get; private set; }

    [Header("Canvas")]
    [SerializeField] private GameObject medalCanvas;

    [Header("Panels")]
    [SerializeField] private GameObject bronzePanel;
    [SerializeField] private GameObject silverPanel;
    [SerializeField] private GameObject goldPanel;

    [Header("Grid Containers")]
    [SerializeField] private Transform bronzeContainer;
    [SerializeField] private Transform silverContainer;
    [SerializeField] private Transform goldContainer;

    [Header("Sprites")]
    [SerializeField] private Sprite bronzeMedalSprite;
    [SerializeField] private Sprite silverMedalSprite;
    [SerializeField] private Sprite goldMedalSprite;

    [Header("Tab Buttons")]
    [SerializeField] private Button tabBronze;
    [SerializeField] private Button tabSilver;
    [SerializeField] private Button tabGold;
    [SerializeField] private Button closeButton;

    [Header("Tab Colors")]
    [SerializeField] private Color bronzeActiveColor = new Color(0.80f, 0.50f, 0.20f, 1f);
    [SerializeField] private Color silverActiveColor = new Color(0.75f, 0.75f, 0.80f, 1f);
    [SerializeField] private Color goldActiveColor   = new Color(1.00f, 0.84f, 0.10f, 1f);
    [SerializeField] private Color tabInactiveColor  = new Color(0.22f, 0.22f, 0.28f, 0.85f);

    [Header("Icon Size")]
    [SerializeField] private float iconSize = 80f;

    // =========================================================================
    // LIFECYCLE
    // =========================================================================

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        if (medalCanvas != null) medalCanvas.SetActive(false);
    }

    private void Start()
    {
        if (tabBronze   != null) tabBronze  .onClick.AddListener(() => SwitchTab(MedalType.Bronze));
        if (tabSilver   != null) tabSilver  .onClick.AddListener(() => SwitchTab(MedalType.Silver));
        if (tabGold     != null) tabGold    .onClick.AddListener(() => SwitchTab(MedalType.Gold));
        if (closeButton != null) closeButton.onClick.AddListener(CloseMedalCanvas);
    }

    // =========================================================================
    // MỞ / ĐÓNG
    // =========================================================================

    public void OpenMedalCanvas()
    {
        if (medalCanvas == null) { Debug.LogError("[MedalManager] Chua gan medalCanvas!"); return; }
        medalCanvas.SetActive(true);
        SwitchTab(MedalType.Bronze);
    }

    public void CloseMedalCanvas()
    {
        if (medalCanvas != null) medalCanvas.SetActive(false);
    }

    // =========================================================================
    // CHUYỂN TAB
    // =========================================================================

    private void SwitchTab(MedalType tab)
    {
        if (bronzePanel != null) bronzePanel.SetActive(false);
        if (silverPanel != null) silverPanel.SetActive(false);
        if (goldPanel   != null) goldPanel  .SetActive(false);

        SetTabColor(tabBronze, tab == MedalType.Bronze, bronzeActiveColor);
        SetTabColor(tabSilver, tab == MedalType.Silver, silverActiveColor);
        SetTabColor(tabGold,   tab == MedalType.Gold,   goldActiveColor);

        var ud = AuthManager.Instance?.CurrentUserData;

        switch (tab)
        {
            case MedalType.Bronze:
                if (bronzePanel != null) bronzePanel.SetActive(true);
                SpawnIcons(bronzeContainer, bronzeMedalSprite, ud?.bronzeMedals ?? 0);
                break;
            case MedalType.Silver:
                if (silverPanel != null) silverPanel.SetActive(true);
                SpawnIcons(silverContainer, silverMedalSprite, ud?.silverMedals ?? 0);
                break;
            case MedalType.Gold:
                if (goldPanel != null) goldPanel.SetActive(true);
                SpawnIcons(goldContainer, goldMedalSprite, ud?.goldMedals ?? 0);
                break;
        }
    }

    private void SetTabColor(Button btn, bool active, Color activeColor)
    {
        if (btn == null) return;
        var img = btn.GetComponent<Image>();
        if (img != null) img.color = active ? activeColor : tabInactiveColor;
    }

    // =========================================================================
    // SPAWN ẢNH MEDAL
    // =========================================================================

    private void SpawnIcons(Transform container, Sprite sprite, int count)
    {
        if (container == null) return;

        foreach (Transform child in container)
            Destroy(child.gameObject);

        for (int i = 0; i < count; i++)
        {
            var go            = new GameObject("MedalIcon");
            go.transform.SetParent(container, false);
            var rt            = go.AddComponent<RectTransform>();
            rt.sizeDelta      = new Vector2(iconSize, iconSize);
            var img           = go.AddComponent<Image>();
            img.sprite        = sprite;
            img.preserveAspect = true;
        }
    }

    // =========================================================================
    // TRAO HUY CHƯƠNG
    //
    // QUAN TRỌNG: Hàm này KHÔNG cộng vào UserData.
    // Caller (RewardManager) phải tự cộng user.bronzeMedals/silverMedals/goldMedals
    // TRƯỚC khi gọi AwardMedal(). Hàm này chỉ đọc giá trị hiện tại rồi lưu Firebase.
    //
    // Ví dụ đúng:
    //   user.bronzeMedals += 1;
    //   MedalManager.Instance.AwardMedal(MedalType.Bronze);
    // =========================================================================

    public void AwardBronzeMedal() => AwardMedal(MedalType.Bronze);

    public void AwardMedal(MedalType type)
    {
        var ud = AuthManager.Instance?.CurrentUserData;
        if (ud == null)
        {
            Debug.LogWarning("[MedalManager] AwardMedal: CurrentUserData null.");
            return;
        }

        string uid = AuthManager.Instance.CurrentUser?.UserId;
        if (string.IsNullOrEmpty(uid))
        {
            Debug.LogWarning("[MedalManager] AwardMedal: uid null.");
            return;
        }

        // ── Chỉ đọc giá trị, KHÔNG cộng thêm ────────────────────────────────
        string field;
        int    currentCount;

        switch (type)
        {
            case MedalType.Gold:
                field        = "medals/gold";
                currentCount = ud.goldMedals;
                break;
            case MedalType.Silver:
                field        = "medals/silver";
                currentCount = ud.silverMedals;
                break;
            default: // Bronze
                field        = "medals/bronze";
                currentCount = ud.bronzeMedals;
                break;
        }

        // ── Lưu lên Firebase ─────────────────────────────────────────────────
        FirebaseDatabase.DefaultInstance
            .GetReference($"users/{uid}/{field}")
            .SetValueAsync(currentCount)
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsCompletedSuccessfully)
                    Debug.Log($"[MedalManager] Luu Firebase OK: {field} = {currentCount}");
                else
                    Debug.LogError($"[MedalManager] Luu Firebase loi: {field} | {task.Exception}");
            });
    }
}