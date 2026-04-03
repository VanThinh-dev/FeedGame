// =============================================================================
// RewardWireBuilder.cs — Assets/Editor/RewardWireBuilder.cs
//
// Tools > Fix Reward Manager Wiring
//
// KHÔNG tạo gì mới. Chỉ tìm đúng các object trong hierarchy hiện có
// và wire vào RewardManager Inspector cho đúng.
//
// Chạy khi: các slot Coin Icon, XP Icon, Amount Text bị wire nhầm.
// =============================================================================
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public static class RewardWireBuilder
{
    [MenuItem("Tools/Fix Reward Manager Wiring")]
    public static void FixWiring()
    {
        // ── Tìm RewardManager ─────────────────────────────────────────────────
        var rm = GameObject.FindObjectOfType<RewardManager>();
        if (rm == null)
        {
            EditorUtility.DisplayDialog("Fix Wiring", "Không tìm thấy RewardManager trong scene!", "OK");
            return;
        }

        // ── Tìm RewardCanvas và RewardPanel ──────────────────────────────────
        var rewardCanvas = GameObject.Find("RewardCanvas");
        if (rewardCanvas == null) { Log("Không tìm thấy RewardCanvas!"); return; }

        var rewardPanel = rewardCanvas.transform.Find("RewardPanel");
        if (rewardPanel == null) { Log("Không tìm thấy RewardPanel!"); return; }

        // ── Tìm các card trong hierarchy ─────────────────────────────────────
        // CardRow > CoinCard > IconImage + AmountText
        var cardRow  = rewardPanel.Find("CardRow");
        if (cardRow == null) { Log("Không tìm thấy CardRow!"); return; }

        var coinCard = cardRow.Find("CoinCard");
        var xpCard   = cardRow.Find("XpCard");
        if (coinCard == null) { Log("Không tìm thấy CoinCard!"); return; }
        if (xpCard   == null) { Log("Không tìm thấy XpCard!");   return; }

        // Lấy đúng component từ đúng card
        var coinIconImg  = coinCard.Find("IconImage")?.GetComponent<Image>();
        var coinAmtText  = coinCard.Find("AmountText")?.GetComponent<TMP_Text>();
        var xpIconImg    = xpCard.Find("IconImage")?.GetComponent<Image>();
        var xpAmtText    = xpCard.Find("AmountText")?.GetComponent<TMP_Text>();

        // ── MedalCard ─────────────────────────────────────────────────────────
        var medalCardGO  = rewardPanel.Find("MedalCard")?.gameObject;
        var medalIconImg = rewardPanel.Find("MedalCard/IconImage")?.GetComponent<Image>();
        var medalTypeTxt = rewardPanel.Find("MedalCard/AmountText")?.GetComponent<TMP_Text>();

        // ── ConfirmButton ─────────────────────────────────────────────────────
        var confirmBtn   = rewardPanel.Find("ConfirmButton")?.GetComponent<Button>();

        // ── LevelUpSection ────────────────────────────────────────────────────
        var levelUpSec   = rewardPanel.Find("LevelUpSection")?.gameObject;
        var luTitleTxt   = rewardPanel.Find("LevelUpSection/TitleText")?.GetComponent<TMP_Text>();
        // RewardRow > CoinRewardItem / MedalRewardItem
        var luRow        = rewardPanel.Find("LevelUpSection/RewardRow");
        var luCoinIcon   = luRow?.Find("CoinRewardItem/Icon")?.GetComponent<Image>();
        var luCoinTxt    = luRow?.Find("CoinRewardItem/AmountText")?.GetComponent<TMP_Text>();
        var luMedalIcon  = luRow?.Find("MedalRewardItem/Icon")?.GetComponent<Image>();
        var luMedalTxt   = luRow?.Find("MedalRewardItem/AmountText")?.GetComponent<TMP_Text>();

        // ── Wire vào RewardManager ────────────────────────────────────────────
        var so = new SerializedObject(rm);

        // Canvas & Panel
        so.FindProperty("rewardCanvas").objectReferenceValue = rewardCanvas;
        so.FindProperty("rewardPanel").objectReferenceValue  = rewardPanel.gameObject;

        // Gameplay cards
        SetProp(so, "coinIcon",        coinIconImg);
        SetProp(so, "coinAmountText",  coinAmtText);
        SetProp(so, "xpIcon",          xpIconImg);
        SetProp(so, "xpAmountText",    xpAmtText);

        // Medal card
        SetProp(so, "medalCard",       medalCardGO);
        SetProp(so, "medalIcon",       medalIconImg);
        SetProp(so, "medalTypeText",   medalTypeTxt);

        // Confirm button
        SetProp(so, "confirmButton",   confirmBtn);

        // Level-up section
        SetProp(so, "levelUpSection",  levelUpSec);
        SetProp(so, "levelUpTitleText",luTitleTxt);
        SetProp(so, "levelUpCoinText", luCoinTxt);
        SetProp(so, "levelUpMedalText",luMedalTxt);
        SetProp(so, "levelUpCoinIcon", luCoinIcon);
        SetProp(so, "levelUpMedalIcon",luMedalIcon);

        so.ApplyModifiedProperties();

        // ── Log kết quả ───────────────────────────────────────────────────────
        string report =
            $"coinIcon:      {(coinIconImg  != null ? "✅" : "❌ không tìm thấy CoinCard/IconImage")}\n" +
            $"coinAmountText:{(coinAmtText  != null ? "✅" : "❌ không tìm thấy CoinCard/AmountText")}\n" +
            $"xpIcon:        {(xpIconImg    != null ? "✅" : "❌ không tìm thấy XpCard/IconImage")}\n" +
            $"xpAmountText:  {(xpAmtText    != null ? "✅" : "❌ không tìm thấy XpCard/AmountText")}\n" +
            $"medalCard:     {(medalCardGO  != null ? "✅" : "❌ không tìm thấy MedalCard")}\n" +
            $"confirmButton: {(confirmBtn   != null ? "✅" : "❌ không tìm thấy ConfirmButton")}\n" +
            $"levelUpSection:{(levelUpSec   != null ? "✅" : "⚠️  chưa có — chạy 'Add Level-Up Section' trước")}";

        Debug.Log("[RewardWireBuilder] Kết quả wire:\n" + report);

        EditorUtility.DisplayDialog("Fix Reward Manager Wiring",
            "Wire xong!\n\n" + report + "\n\n" +
            "Còn lại: kéo Sprite vào Bronze Medal Sprite và Level Up Bronze Sprite trong Inspector.",
            "OK");
    }

    static void SetProp(SerializedObject so, string propName, Object value)
    {
        var prop = so.FindProperty(propName);
        if (prop != null)
            prop.objectReferenceValue = value;
        else
            Debug.LogWarning($"[RewardWireBuilder] Property '{propName}' không tìm thấy trong RewardManager. " +
                             "Kiểm tra tên field [SerializeField] trong RewardManager.cs");
    }

    static void Log(string msg) => Debug.LogError("[RewardWireBuilder] " + msg);
}
#endif