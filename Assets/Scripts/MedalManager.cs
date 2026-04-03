using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Firebase.Database;

// =============================================================================
// MedalManager.cs — v3
// Tabbar: Dong / Bac / Vang — mỗi tab hiện đúng số ảnh
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

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        if (medalCanvas != null) medalCanvas.SetActive(false);
    }

    private void Start()
    {
        if (tabBronze != null) tabBronze.onClick.AddListener(() => SwitchTab(MedalType.Bronze));
        if (tabSilver != null) tabSilver.onClick.AddListener(() => SwitchTab(MedalType.Silver));
        if (tabGold   != null) tabGold  .onClick.AddListener(() => SwitchTab(MedalType.Gold));
        if (closeButton != null) closeButton.onClick.AddListener(CloseMedalCanvas);
    }

    // =========================================================================
    // MỞ / ĐÓNG
    // =========================================================================

    public void OpenMedalCanvas()
    {
        if (medalCanvas == null) { Debug.LogError("[MedalManager] Chua gan medalCanvas!"); return; }
        medalCanvas.SetActive(true);
        SwitchTab(MedalType.Bronze); // mặc định mở tab Đồng
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
        // Ẩn tất cả panel
        if (bronzePanel != null) bronzePanel.SetActive(false);
        if (silverPanel != null) silverPanel.SetActive(false);
        if (goldPanel   != null) goldPanel  .SetActive(false);

        // Cập nhật màu tab
        SetTabColor(tabBronze, tab == MedalType.Bronze, bronzeActiveColor);
        SetTabColor(tabSilver, tab == MedalType.Silver, silverActiveColor);
        SetTabColor(tabGold,   tab == MedalType.Gold,   goldActiveColor);

        var ud = AuthManager.Instance?.CurrentUserData;

        // Hiện panel đúng và fill ảnh
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
    // SPAWN ẢNH
    // =========================================================================

    private void SpawnIcons(Transform container, Sprite sprite, int count)
    {
        if (container == null) return;

        foreach (Transform child in container)
            Destroy(child.gameObject);

        for (int i = 0; i < count; i++)
        {
            var go = new GameObject("MedalIcon");
            go.transform.SetParent(container, false);
            var rt       = go.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(iconSize, iconSize);
            var img           = go.AddComponent<Image>();
            img.sprite        = sprite;
            img.preserveAspect = true;
        }
    }

    // =========================================================================
    // TRAO HUY CHƯƠNG
    // =========================================================================

    public void AwardBronzeMedal() => AwardMedal(MedalType.Bronze);

    public void AwardMedal(MedalType type)
{
    var ud = AuthManager.Instance?.CurrentUserData;
    if (ud == null) return;

    string uid = AuthManager.Instance.CurrentUser?.UserId;
    if (string.IsNullOrEmpty(uid)) return;

    string field;

    switch (type)
    {
        case MedalType.Gold:
            ud.goldMedals++;
            field = "medals/gold";
            break;

        case MedalType.Silver:
            ud.silverMedals++;
            field = "medals/silver";
            break;

        default:
            ud.bronzeMedals++;
            field = "medals/bronze";
            break;
    }

    int newCount = type switch
    {
        MedalType.Gold => ud.goldMedals,
        MedalType.Silver => ud.silverMedals,
        _ => ud.bronzeMedals
    };

    FirebaseDatabase.DefaultInstance
        .GetReference($"users/{uid}/{field}")
        .SetValueAsync(newCount);
 }
}