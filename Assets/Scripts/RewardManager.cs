using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// =============================================================================
// RewardManager.cs — v5
//
// Fix so với v4:
//   • ShowRewardCanvas() được gọi TRƯỚC ShowLevelUpSection()
//     → Khi canvas cha đang SetActive(false), gọi SetActive(true) trên con
//       không có hiệu lực trong hierarchy — canvas phải active trước.
//   • Bỏ emoji 🎉 trong title text (font LiberationSans không hỗ trợ Unicode emoji
//     → gây warning spam và ký tự bị thay thế).
//   • Thêm debug log để dễ trace nếu vẫn còn lỗi.
// =============================================================================

public class RewardManager : MonoBehaviour
{
    public static RewardManager Instance { get; private set; }

    // ── Canvas / Panel ────────────────────────────────────────────────────────
    [Header("Canvas & Panel")]
    [SerializeField] private GameObject rewardCanvas;   // RewardCanvas gốc
    [SerializeField] private GameObject rewardPanel;    // Panel con (scale animation)

    // ── Card Coin (phần thưởng gameplay) ─────────────────────────────────────
    [Header("Card Coin - Phan Thuong Gameplay")]
    [SerializeField] private Image    coinIcon;
    [SerializeField] private TMP_Text coinAmountText;   // "+50"

    // ── Card XP ───────────────────────────────────────────────────────────────
    [Header("Card XP")]
    [SerializeField] private Image    xpIcon;
    [SerializeField] private TMP_Text xpAmountText;     // "+120 XP"

    // ── Card Medal (gameplay — luôn trao Bronze) ──────────────────────────────
    [Header("Card Medal - Gameplay")]
    [SerializeField] private GameObject medalCard;
    [SerializeField] private Image      medalIcon;
    [SerializeField] private TMP_Text   medalTypeText;
    [SerializeField] public  Sprite     bronzeMedalSprite;

    // ── Level-Up Section (hiện trên cùng RewardCanvas) ───────────────────────
    [Header("Level-Up Section (SetActive false mac dinh)")]
    [SerializeField] private GameObject levelUpSection;     // Container ẩn/hiện
    [SerializeField] private TMP_Text   levelUpTitleText;   // "Chuc mung! Len Level X!"
    [SerializeField] private TMP_Text   levelUpCoinText;    // "+10 Xu"
    [SerializeField] private TMP_Text   levelUpMedalText;   // "+1 Huy Chuong Dong"
    [SerializeField] private Image      levelUpCoinIcon;    // Sprite coin — kéo vào
    [SerializeField] private Image      levelUpMedalIcon;   // Sprite medal — kéo vào
    [SerializeField] public  Sprite     levelUpBronzeSprite;// Sprite huy chương đồng lên level

    // ── Nút xác nhận ──────────────────────────────────────────────────────────
    [Header("Button")]
    [SerializeField] private Button confirmButton;

    // ── Công thức tính thưởng gameplay ────────────────────────────────────────
    [Header("Cong Thuc Thuong Gameplay")]
    [SerializeField] private int fixedCoins = 50;
    [SerializeField] private int fixedXp    = 120;
    // ── Animation ─────────────────────────────────────────────────────────────
    [Header("Animation")]
    [SerializeField] private float animDuration = 0.4f;

    // ── Internal ─────────────────────────────────────────────────────────────
    private GameResult _lastResult;

    // =========================================================================
    // LIFECYCLE
    // =========================================================================

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        if (rewardCanvas != null) rewardCanvas.SetActive(false);
    }

    private void Start()
    {
        confirmButton?.onClick.AddListener(OnConfirmClicked);
        StartCoroutine(SubscribeToGameManager());
    }

    private IEnumerator SubscribeToGameManager()
    {
        while (GameManager.Instance == null)
            yield return null;
        GameManager.Instance.OnGameCompleted += HandleGameCompleted;
        Debug.Log("[RewardManager] Da dang ky OnGameCompleted.");
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnGameCompleted -= HandleGameCompleted;
    }

    // =========================================================================
    // NHẬN KẾT QUẢ TỪ GAMEMANAGER
    // =========================================================================

    private void HandleGameCompleted(GameResult result)
    {
        _lastResult = result;

        // ── 1. Tính phần thưởng gameplay ──────────────────────────────────────
        int coinsEarned = fixedCoins;
        int xpEarned    = fixedXp;

        // ── 2. Cập nhật UserData (AddXp trả về số level tăng) ─────────────────
        UserData user = AuthManager.Instance?.CurrentUserData;
        if (user == null)
        {
            Debug.LogWarning("[RewardManager] CurrentUser null — bo qua tinh thuong.");
            ShowRewardCanvas();
            return;
        }

        user.coins += coinsEarned;
        int levelsGained = user.AddXp(xpEarned);
        // AddXp đã cộng LEVELUP_COIN_REWARD vào user.coins nếu lên level

        // ── 3. Cập nhật UI gameplay cards ─────────────────────────────────────
        UpdateGameplayCards(coinsEarned, xpEarned);
        ShowGameplayMedalCard();

        // ── 4. Lưu Firebase ───────────────────────────────────────────────────
        SaveAllToFirebase(user, result.correct);

        // ── 5. Refresh AvatarWidget ───────────────────────────────────────────
        RefreshAvatarWidget(user);

        // ── 6. Bật canvas TRƯỚC — quan trọng! ────────────────────────────────
        // Canvas phải active trước khi gọi SetActive(true) trên LevelUpSection,
        // vì khi GameObject cha đang inactive, SetActive(true) trên con không
        // có hiệu lực trong hierarchy (activeInHierarchy vẫn = false).
        ShowRewardCanvas();

        // ── 7. Sau khi canvas đã active, mới toggle LevelUpSection ───────────
        if (levelsGained > 0)
        {
            user.bronzeMedals += levelsGained;
            AwardLevelUpMedal(levelsGained);
            ShowLevelUpSection(user.level, levelsGained);
        }
        else
        {
            if (levelUpSection != null) levelUpSection.SetActive(false);
        }

        Debug.Log($"[RewardManager] Gameplay: +{coinsEarned} coins, +{xpEarned} XP | Level up: {levelsGained} lan");
    }

    // =========================================================================
    // CẬP NHẬT UI — GAMEPLAY CARDS
    // =========================================================================

    private void UpdateGameplayCards(int coins, int xp)
    {
        if (coinAmountText != null) coinAmountText.text = $"+{coins}";
        if (xpAmountText   != null) xpAmountText.text   = $"+{xp} XP";
    }

    private void ShowGameplayMedalCard()
    {
        if (medalCard     != null) medalCard.SetActive(true);
        if (medalIcon     != null && bronzeMedalSprite != null)
            medalIcon.sprite = bronzeMedalSprite;
        if (medalTypeText != null)
            medalTypeText.text = "Huy Chương Đồng";
    }

    // =========================================================================
    // LEVEL-UP SECTION
    // =========================================================================

    private void ShowLevelUpSection(int newLevel, int levelsGained)
{
    if (levelUpSection == null)
    {
        Debug.LogError("[RewardManager] levelUpSection CHUA DUOC GAN trong Inspector!");
        return;
    }

    levelUpSection.SetActive(true);
    
    // ── FIX: Đưa lên top của render order để không bị che ─────────────────
    levelUpSection.transform.SetAsLastSibling();

    Debug.Log($"[RewardManager] LevelUpSection SetActive(true) | activeInHierarchy={levelUpSection.activeInHierarchy}");

    if (levelUpTitleText != null)
        levelUpTitleText.text = levelsGained > 1
            ? $"Tuyet voi! Len Level {newLevel}! (+{levelsGained})"
            : $"Chuc mung! Len Level {newLevel}!";

    int totalCoinBonus = UserData.LEVELUP_COIN_REWARD * levelsGained;
    if (levelUpCoinText  != null) levelUpCoinText.text  = $"+{totalCoinBonus} Xu";
    if (levelUpMedalText != null) levelUpMedalText.text = $"+{levelsGained} Huy Chương Đồng";

    if (levelUpMedalIcon != null && levelUpBronzeSprite != null)
        levelUpMedalIcon.sprite = levelUpBronzeSprite;
}

    // =========================================================================
    // TRAO HUY CHƯƠNG KHI LÊN LEVEL
    // =========================================================================

    private void AwardLevelUpMedal(int count)
    {
        if (MedalManager.Instance == null)
        {
            Debug.LogWarning("[RewardManager] MedalManager.Instance null — bo qua trao medal len level.");
            return;
        }
        for (int i = 0; i < count; i++)
            MedalManager.Instance.AwardMedal(MedalType.Bronze);
        Debug.Log($"[RewardManager] Trao {count} Huy Chương Đồng (len level).");
    }

    // =========================================================================
    // LƯU FIREBASE
    // =========================================================================

    private void SaveAllToFirebase(UserData user, int wordsCorrect)
    {
        if (AuthManager.Instance == null || !AuthManager.Instance.IsLoggedIn)
        {
            Debug.LogWarning("[RewardManager] Chua dang nhap — bo qua luu Firebase.");
            return;
        }
        AuthManager.Instance.UpdateFullUserData(user, wordsCorrect);
    }

    // =========================================================================
    // REFRESH AVATAR WIDGET
    // =========================================================================

    private void RefreshAvatarWidget(UserData user)
    {
        var widget = FindObjectOfType<AvatarWidgetController>();
        widget?.RefreshWidget(user);
    }

    // =========================================================================
    // ANIMATION — scale panel từ 0 lên 1, sau đó mới apply LevelUpSection
    // =========================================================================

    private void ShowRewardCanvas()
    {
        if (rewardCanvas == null)
        {
            Debug.LogError("[RewardManager] rewardCanvas chua gan!");
            return;
        }

        // Đảm bảo LevelUpSection ẩn trước khi canvas hiện (reset trạng thái)
        if (levelUpSection != null) levelUpSection.SetActive(false);

        rewardCanvas.SetActive(true);

        if (rewardPanel != null)
            StartCoroutine(ScaleIn(rewardPanel.transform));
    }

    private IEnumerator ScaleIn(Transform target)
    {
        target.localScale = Vector3.zero;
        float elapsed = 0f;
        while (elapsed < animDuration)
        {
            elapsed += Time.deltaTime;
            float t     = Mathf.Clamp01(elapsed / animDuration);
            float eased = 1f - Mathf.Pow(1f - t, 3f); // ease-out cubic
            target.localScale = Vector3.one * eased;
            yield return null;
        }
        target.localScale = Vector3.one;
    }

    // =========================================================================
    // NÚT XÁC NHẬN → VỀ BEDROOM
    // =========================================================================

    private void OnConfirmClicked()
    {
        Debug.Log("[RewardManager] Xac nhan — ve Bedroom.");
        if (rewardCanvas != null) rewardCanvas.SetActive(false);
        GameManager.Instance?.ExitGameplay();
    }
}