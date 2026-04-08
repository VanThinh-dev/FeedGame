using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// =============================================================================
// RewardManager.cs — v7
//
// FIX so với v6:
//   • HandleGameCompleted bị gọi 2+ lần trong 1 ván (GameManager bắn event
//     mỗi lần SetNextTarget → OnSessionComplete → OnGameCompleted).
//     Thêm _isProcessing guard để chỉ xử lý lần đầu tiên, bỏ qua các lần sau.
//   • Unsubscribe trước khi Subscribe để tránh event handler tích lũy.
//   • _isProcessing được reset trong OnConfirmClicked() để ván tiếp theo OK.
// =============================================================================

public class RewardManager : MonoBehaviour
{
    public static RewardManager Instance { get; private set; }

    // ── Canvas / Panel ────────────────────────────────────────────────────────
    [Header("Canvas & Panel")]
    [SerializeField] private GameObject rewardCanvas;
    [SerializeField] private GameObject rewardPanel;

    // ── Card Coin ─────────────────────────────────────────────────────────────
    [Header("Card Coin - Phan Thuong Gameplay")]
    [SerializeField] private Image    coinIcon;
    [SerializeField] private TMP_Text coinAmountText;

    // ── Card XP ───────────────────────────────────────────────────────────────
    [Header("Card XP")]
    [SerializeField] private Image    xpIcon;
    [SerializeField] private TMP_Text xpAmountText;

    // ── Card Medal ────────────────────────────────────────────────────────────
    [Header("Card Medal - Gameplay")]
    [SerializeField] private GameObject medalCard;
    [SerializeField] private Image      medalIcon;
    [SerializeField] private TMP_Text   medalTypeText;
    [SerializeField] public  Sprite     bronzeMedalSprite;

    // ── Level-Up Section ─────────────────────────────────────────────────────
    [Header("Level-Up Section")]
    [SerializeField] private GameObject levelUpSection;
    [SerializeField] private TMP_Text   levelUpTitleText;
    [SerializeField] private TMP_Text   levelUpCoinText;
    [SerializeField] private TMP_Text   levelUpMedalText;
    [SerializeField] private Image      levelUpCoinIcon;
    [SerializeField] private Image      levelUpMedalIcon;
    [SerializeField] public  Sprite     levelUpBronzeSprite;

    // ── Button ────────────────────────────────────────────────────────────────
    [Header("Button")]
    [SerializeField] private Button confirmButton;

    // ── Reward Config ─────────────────────────────────────────────────────────
    [Header("=== REWARD CONFIG ===")]

    [Header("Gameplay Rewards")]
    [Tooltip("Số coin thưởng sau mỗi ván chơi")]
    [SerializeField] private int gameplayCoins = 50;

    [Tooltip("Số XP thưởng sau mỗi ván chơi")]
    [SerializeField] private int gameplayXp = 120;

    [Tooltip("Có trao huy chương đồng sau mỗi ván chơi không?")]
    [SerializeField] private bool awardBronzeMedalOnGameplay = true;

    [Tooltip("Số huy chương đồng trao sau mỗi ván chơi (nếu bật)")]
    [SerializeField] private int gameplayBronzeCount = 1;

    [Header("Level-Up Rewards")]
    [Tooltip("Coin thưởng mỗi lần lên level — phải khớp với UserData.LEVELUP_COIN_REWARD")]
    [SerializeField] private int levelUpCoinBonus = 10;

    [Tooltip("Số huy chương đồng trao mỗi lần lên level")]
    [SerializeField] private int levelUpBronzePerLevel = 1;

    [Header("Animation")]
    [SerializeField] private float animDuration = 0.4f;

    // ── Internal ─────────────────────────────────────────────────────────────
    private GameResult _lastResult;

    /// <summary>
    /// Guard chống HandleGameCompleted chạy nhiều lần trong 1 ván.
    /// Set true khi bắt đầu xử lý, reset false khi người dùng bấm Xác nhận.
    /// </summary>
    private bool _isProcessing = false;

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

        // Unsubscribe trước để tránh đăng ký trùng
        GameManager.Instance.OnGameCompleted -= HandleGameCompleted;
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
        // ── GUARD: chỉ xử lý 1 lần duy nhất mỗi ván ─────────────────────────
        if (_isProcessing)
        {
            Debug.LogWarning("[RewardManager] HandleGameCompleted goi lan 2+ — BO QUA.");
            return;
        }
        _isProcessing = true;

        _lastResult = result;

        UserData user = AuthManager.Instance?.CurrentUserData;
        if (user == null)
        {
            Debug.LogWarning("[RewardManager] CurrentUser null — bo qua tinh thuong.");
            ShowRewardCanvas();
            return;
        }

        // ── 1. Cộng phần thưởng gameplay vào UserData ─────────────────────────
        user.coins += gameplayCoins;
        int levelsGained = user.AddXp(gameplayXp);
        // AddXp tự cộng LEVELUP_COIN_REWARD vào user.coins mỗi lần lên level

        // ── 2. Trao huy chương gameplay ───────────────────────────────────────
        if (awardBronzeMedalOnGameplay && gameplayBronzeCount > 0)
        {
            user.bronzeMedals += gameplayBronzeCount;
            AwardMedals(MedalType.Bronze, gameplayBronzeCount, "gameplay");
        }

        // ── 3. Trao huy chương lên level ──────────────────────────────────────
        int levelUpBronzeTotal = 0;
        if (levelsGained > 0)
        {
            levelUpBronzeTotal = levelUpBronzePerLevel * levelsGained;
            user.bronzeMedals += levelUpBronzeTotal;
            AwardMedals(MedalType.Bronze, levelUpBronzeTotal, "level-up");
        }

        // ── 4. Cập nhật UI cards ──────────────────────────────────────────────
        UpdateGameplayCards(gameplayCoins, gameplayXp);
        SetMedalCardVisible(awardBronzeMedalOnGameplay);

        // ── 5. Lưu Firebase ───────────────────────────────────────────────────
        SaveAllToFirebase(user, result.correct);

        // ── 6. Refresh AvatarWidget ───────────────────────────────────────────
        RefreshAvatarWidget(user);

        // ── 7. Bật canvas TRƯỚC ───────────────────────────────────────────────
        ShowRewardCanvas();

        // ── 8. Toggle LevelUpSection ──────────────────────────────────────────
        if (levelsGained > 0)
            ShowLevelUpSection(user.level, levelsGained, levelUpBronzeTotal);
        else
            HideLevelUpSection();

        Debug.Log($"[RewardManager] +{gameplayCoins} coins | +{gameplayXp} XP | " +
                  $"+{(awardBronzeMedalOnGameplay ? gameplayBronzeCount : 0)} medal(gameplay) | " +
                  $"Level up: {levelsGained} lan | +{levelUpBronzeTotal} medal(level-up)");
    }

    // =========================================================================
    // UI — GAMEPLAY CARDS
    // =========================================================================

    private void UpdateGameplayCards(int coins, int xp)
    {
        if (coinAmountText != null) coinAmountText.text = $"+{coins}";
        if (xpAmountText   != null) xpAmountText.text   = $"+{xp} XP";
    }

    private void SetMedalCardVisible(bool visible)
    {
        if (medalCard == null) return;
        medalCard.SetActive(visible);
        if (!visible) return;

        if (medalIcon != null && bronzeMedalSprite != null)
            medalIcon.sprite = bronzeMedalSprite;
        if (medalTypeText != null)
            medalTypeText.text = gameplayBronzeCount > 1
                ? $"x{gameplayBronzeCount} Huy Chương Đồng"
                : "Huy Chương Đồng";
    }

    // =========================================================================
    // UI — LEVEL-UP SECTION
    // =========================================================================

    private void ShowLevelUpSection(int newLevel, int levelsGained, int bronzeAwarded)
    {
        if (levelUpSection == null)
        {
            Debug.LogError("[RewardManager] levelUpSection CHUA DUOC GAN trong Inspector!");
            return;
        }

        levelUpSection.SetActive(true);
        levelUpSection.transform.SetAsLastSibling();

        Debug.Log($"[RewardManager] LevelUpSection active | activeInHierarchy={levelUpSection.activeInHierarchy}");

        if (levelUpTitleText != null)
            levelUpTitleText.text = levelsGained > 1
                ? $"Tuyet voi! Len Level {newLevel}! (+{levelsGained})"
                : $"Chuc mung! Len Level {newLevel}!";

        int totalCoinBonus = levelUpCoinBonus * levelsGained;
        if (levelUpCoinText  != null) levelUpCoinText.text  = $"+{totalCoinBonus} Xu";
        if (levelUpMedalText != null) levelUpMedalText.text = $"+{bronzeAwarded} Huy Chương Đồng";

        if (levelUpMedalIcon != null && levelUpBronzeSprite != null)
            levelUpMedalIcon.sprite = levelUpBronzeSprite;
    }

    private void HideLevelUpSection()
    {
        if (levelUpSection != null) levelUpSection.SetActive(false);
    }

    // =========================================================================
    // TRAO HUY CHƯƠNG
    // =========================================================================

    private void AwardMedals(MedalType type, int count, string source)
    {
        if (MedalManager.Instance == null)
        {
            Debug.LogWarning($"[RewardManager] MedalManager null — bo qua trao medal ({source}).");
            return;
        }
        for (int i = 0; i < count; i++)
            MedalManager.Instance.AwardMedal(type);
        Debug.Log($"[RewardManager] Trao {count}x {type} ({source}).");
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
    // SHOW CANVAS + ANIMATION
    // =========================================================================

    private void ShowRewardCanvas()
    {
        if (rewardCanvas == null)
        {
            Debug.LogError("[RewardManager] rewardCanvas chua gan!");
            return;
        }

        HideLevelUpSection();
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
            float eased = 1f - Mathf.Pow(1f - t, 3f);
            target.localScale = Vector3.one * eased;
            yield return null;
        }
        target.localScale = Vector3.one;
    }

    // =========================================================================
    // NÚT XÁC NHẬN
    // =========================================================================

    private void OnConfirmClicked()
    {
        Debug.Log("[RewardManager] Xac nhan — ve Bedroom.");

        // Reset guard để ván chơi tiếp theo được xử lý bình thường
        _isProcessing = false;

        if (rewardCanvas != null) rewardCanvas.SetActive(false);
        GameManager.Instance?.ExitGameplay();
    }
}