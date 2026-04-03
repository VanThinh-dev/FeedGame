using System;
using UnityEngine;

// =============================================================================
// UserData.cs — v5
// XP reset về 0 khi lên level (không cộng dồn).
// xp = kinh nghiệm trong level hiện tại (0 → 99).
// level tăng khi xp đạt XP_PER_LEVEL, rồi xp reset về phần dư.
// =============================================================================

[Serializable]
public class UserData
{
    public string uid;
    public string email;
    public string displayName;
    public int    score;
    public int    level;
    public int    wordsLearned;
    public int    coins;
    public int    xp;           // XP TRONG LEVEL HIỆN TẠI (0 → XP_PER_LEVEL-1)
    public long   createdAt;

    // ── Huy chương ───────────────────────────────────────────────────────────
    public int bronzeMedals;
    public int silverMedals;
    public int goldMedals;

    [NonSerialized] public string[] completedLessons;

    // ── Hằng số ──────────────────────────────────────────────────────────────
    public const int XP_PER_LEVEL        = 100;
    public const int LEVELUP_COIN_REWARD = 10;   // Thưởng coin khi lên level
    // Huy chương đồng trao qua RewardManager / MedalManager — không tính ở đây

    // =========================================================================
    // Constructors
    // =========================================================================

    public UserData() { }

    public UserData(string uid, string email, string displayName)
    {
        this.uid          = uid;
        this.email        = email;
        this.displayName  = displayName;
        this.score        = 0;
        this.level        = 1;
        this.wordsLearned = 0;
        this.coins        = 0;
        this.xp           = 0;
        this.bronzeMedals = 0;
        this.silverMedals = 0;
        this.goldMedals   = 0;
        this.createdAt    = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    }

    // =========================================================================
    // XP / Level helpers
    // =========================================================================

    /// <summary>XP hiện tại trong level (0 → XP_PER_LEVEL-1).</summary>
    public int XpInCurrentLevel => xp; // xp đã là phần trong level hiện tại

    /// <summary>Tỉ lệ fill thanh XP (0.0 → 1.0).</summary>
    public float XpFillAmount => Mathf.Clamp01((float)xp / XP_PER_LEVEL);

    /// <summary>
    /// Cộng XP, xử lý lên level.
    /// XP reset về 0 (+ phần dư) mỗi khi lên level.
    /// Trả về số level đã tăng (0 = không lên level).
    /// </summary>
    public int AddXp(int amount)
    {
        if (amount <= 0) return 0;

        int levelsGained = 0;
        xp += amount;

        // Lên nhiều level nếu XP đủ (ví dụ: nhận 250 XP một lúc)
        while (xp >= XP_PER_LEVEL)
        {
            xp -= XP_PER_LEVEL; // reset về phần dư
            level++;
            levelsGained++;

            // Thưởng coin khi lên level — cộng trực tiếp vào userData
            coins += LEVELUP_COIN_REWARD;
        }

        return levelsGained; // caller dùng để trigger popup lên level
    }

    // =========================================================================
    // Utility
    // =========================================================================

    public DateTime CreatedAtDateTime =>
        DateTimeOffset.FromUnixTimeSeconds(createdAt).LocalDateTime;

    public override string ToString() =>
        $"[UserData] {displayName} | Lv.{level} | XP:{xp}/{XP_PER_LEVEL} | Coins:{coins} | Bronze:{bronzeMedals} Silver:{silverMedals} Gold:{goldMedals}";
}