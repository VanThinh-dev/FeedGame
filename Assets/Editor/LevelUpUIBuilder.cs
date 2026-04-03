// =============================================================================
// LevelUpUIBuilder.cs — Assets/Editor/LevelUpUIBuilder.cs
//
// Tools > Add Level-Up Section to Reward Canvas
//
// KHÔNG tạo RewardCanvas mới. Chỉ thêm LevelUpSection vào RewardPanel đã có.
// Nếu LevelUpSection đã tồn tại → xóa rồi tạo lại (idempotent).
// =============================================================================

#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public static class LevelUpUIBuilder
{
    // ── Theme ────────────────────────────────────────────────────────────────
    static readonly Color SECTION_BG    = new Color(1.0f, 0.90f, 0.15f, 0.18f);
    static readonly Color TITLE_GOLD    = new Color(1.0f, 0.88f, 0.10f, 1.00f);
    static readonly Color REWARD_COIN   = new Color(1.0f, 0.92f, 0.30f, 1.00f);
    static readonly Color REWARD_MEDAL  = new Color(0.85f, 0.55f, 0.15f, 1.00f);
    static readonly Color DIVIDER_COLOR = new Color(1.0f, 1.00f, 1.00f, 0.20f);
    static readonly Color ITEM_BG       = new Color(1.0f, 1.00f, 1.00f, 0.10f);

    const float SECTION_W = 520f;
    const float SECTION_H = 175f;
    const float ICON_SIZE  = 48f;
    const float ITEM_W     = 180f;
    const float ITEM_H     = 80f;
    const float ITEM_GAP   = 24f;

    // =========================================================================
    [MenuItem("Tools/Add Level-Up Section to Reward Canvas")]
    public static void AddLevelUpSection()
    {
        Undo.SetCurrentGroupName("Add LevelUpSection");
        int grp = Undo.GetCurrentGroup();

        // ── 1. Tìm RewardPanel đã có trong scene ─────────────────────────────
        GameObject rewardPanelGO = FindRewardPanel();
        if (rewardPanelGO == null)
        {
            EditorUtility.DisplayDialog("Level-Up Builder",
                "Không tìm thấy 'RewardPanel' trong scene!\n" +
                "Hãy chạy 'Tools > Build Reward Canvas' trước.",
                "OK");
            return;
        }
        Debug.Log($"[LevelUpBuilder] Tìm thấy RewardPanel: {GetPath(rewardPanelGO)}");

        // ── 2. Xóa LevelUpSection cũ nếu đã có ───────────────────────────────
        var old = rewardPanelGO.transform.Find("LevelUpSection");
        if (old != null) Undo.DestroyObjectImmediate(old.gameObject);

        // ── 3. Tạo LevelUpSection ─────────────────────────────────────────────
        var sectionGO = new GameObject("LevelUpSection");
        Undo.RegisterCreatedObjectUndo(sectionGO, "LevelUpSection");
        sectionGO.transform.SetParent(rewardPanelGO.transform, false);
        sectionGO.transform.SetAsFirstSibling(); // Hiện trên cùng

        var sectionRT = sectionGO.AddComponent<RectTransform>();
        sectionRT.anchorMin        = new Vector2(0.5f, 1f);
        sectionRT.anchorMax        = new Vector2(0.5f, 1f);
        sectionRT.pivot            = new Vector2(0.5f, 1f);
        sectionRT.sizeDelta        = new Vector2(SECTION_W, SECTION_H);
        sectionRT.anchoredPosition = new Vector2(0f, 0f);

        var sectionBg   = sectionGO.AddComponent<Image>();
        sectionBg.color  = SECTION_BG;
        sectionBg.sprite = UISprite();
        sectionBg.type   = Image.Type.Sliced;

        // ── 4. TitleText ──────────────────────────────────────────────────────
        var titleGO = new GameObject("TitleText");
        titleGO.transform.SetParent(sectionGO.transform, false);
        var titleRT = titleGO.AddComponent<RectTransform>();
        titleRT.anchorMin        = new Vector2(0f, 1f);
        titleRT.anchorMax        = new Vector2(1f, 1f);
        titleRT.pivot            = new Vector2(0.5f, 1f);
        titleRT.sizeDelta        = new Vector2(0f, 44f);
        titleRT.anchoredPosition = new Vector2(0f, -10f);

        var titleTxt   = titleGO.AddComponent<TextMeshProUGUI>();
        titleTxt.text      = "🎉 Chúc mừng! Lên Level X!";
        titleTxt.fontSize  = 20;
        titleTxt.fontStyle = FontStyles.Bold;
        titleTxt.color     = TITLE_GOLD;
        titleTxt.alignment = TextAlignmentOptions.Center;

        // ── 5. RewardRow ──────────────────────────────────────────────────────
        var rowGO = new GameObject("RewardRow");
        rowGO.transform.SetParent(sectionGO.transform, false);
        var rowRT = rowGO.AddComponent<RectTransform>();
        rowRT.anchorMin        = new Vector2(0.5f, 0.5f);
        rowRT.anchorMax        = new Vector2(0.5f, 0.5f);
        rowRT.pivot            = new Vector2(0.5f, 0.5f);
        rowRT.sizeDelta        = new Vector2(ITEM_W * 2 + ITEM_GAP, ITEM_H);
        rowRT.anchoredPosition = new Vector2(0f, -14f);

        var hlg = rowGO.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing                = ITEM_GAP;
        hlg.childForceExpandWidth  = false;
        hlg.childForceExpandHeight = false;
        hlg.childAlignment         = TextAnchor.MiddleCenter;

        Image    coinIcon;  TMP_Text coinText;
        Image   medalIcon;  TMP_Text medalText;

        BuildRewardItem("CoinRewardItem",     rowGO.transform,
                        "+10 Xu",             REWARD_COIN,
                        out coinIcon,          out coinText);
        BuildRewardItem("MedalRewardItem",    rowGO.transform,
                        "+1 Huy Chương Đồng", REWARD_MEDAL,
                        out medalIcon,         out medalText);

        // ── 6. Divider ────────────────────────────────────────────────────────
        var divGO = new GameObject("Divider");
        divGO.transform.SetParent(sectionGO.transform, false);
        var divRT = divGO.AddComponent<RectTransform>();
        divRT.anchorMin        = new Vector2(0f, 0f);
        divRT.anchorMax        = new Vector2(1f, 0f);
        divRT.pivot            = new Vector2(0.5f, 0f);
        divRT.sizeDelta        = new Vector2(-32f, 2f);
        divRT.anchoredPosition = new Vector2(0f, 10f);
        divGO.AddComponent<Image>().color = DIVIDER_COLOR;

        // ── 7. Ẩn section mặc định ────────────────────────────────────────────
        sectionGO.SetActive(false);

        // ── 8. Wire vào RewardManager (không tạo mới) ────────────────────────
        WireToRewardManager(sectionGO, titleTxt, coinText, medalText, coinIcon, medalIcon);

        Undo.CollapseUndoOperations(grp);

        EditorUtility.DisplayDialog(
            "Level-Up Builder",
            "✅ Đã thêm LevelUpSection vào RewardPanel hiện có!\n\n" +
            "Còn lại:\n" +
            "• Kéo sprite coin vào RewardManager > Level Up Coin Icon\n" +
            "• Kéo sprite huy chương đồng vào Level Up Medal Icon & Level Up Bronze Sprite",
            "OK");
    }

    // =========================================================================
    // Tìm RewardPanel — ưu tiên con của RewardCanvas
    // =========================================================================
    static GameObject FindRewardPanel()
    {
        // Ưu tiên: RewardCanvas > RewardPanel
        var canvas = GameObject.Find("RewardCanvas");
        if (canvas != null)
        {
            var t = canvas.transform.Find("RewardPanel");
            if (t != null) return t.gameObject;
        }

        // Fallback: tìm tất cả RectTransform tên "RewardPanel"
        foreach (var rt in GameObject.FindObjectsOfType<RectTransform>())
            if (rt.name == "RewardPanel") return rt.gameObject;

        return null;
    }

    // =========================================================================
    // Wire vào RewardManager hiện có (không tạo mới)
    // =========================================================================
    static void WireToRewardManager(GameObject section, TMP_Text titleTxt,
                                    TMP_Text coinTxt, TMP_Text medalTxt,
                                    Image coinIcon, Image medalIcon)
    {
        var rm = GameObject.FindObjectOfType<RewardManager>();
        if (rm == null)
        {
            Debug.LogWarning("[LevelUpBuilder] Không tìm thấy RewardManager — tự wire trong Inspector.");
            return;
        }

        var so = new SerializedObject(rm);
        so.FindProperty("levelUpSection").objectReferenceValue    = section;
        so.FindProperty("levelUpTitleText").objectReferenceValue  = titleTxt;
        so.FindProperty("levelUpCoinText").objectReferenceValue   = coinTxt;
        so.FindProperty("levelUpMedalText").objectReferenceValue  = medalTxt;
        so.FindProperty("levelUpCoinIcon").objectReferenceValue   = coinIcon;
        so.FindProperty("levelUpMedalIcon").objectReferenceValue  = medalIcon;
        so.ApplyModifiedProperties();

        Debug.Log($"[LevelUpBuilder] Đã wire vào RewardManager '{rm.gameObject.name}'.");
    }

    // =========================================================================
    // Helpers
    // =========================================================================
    static void BuildRewardItem(string name, Transform parent,
                                string defaultText, Color textColor,
                                out Image iconImg, out TMP_Text amountTxt)
    {
        var itemGO = new GameObject(name);
        itemGO.transform.SetParent(parent, false);

        var le = itemGO.AddComponent<LayoutElement>();
        le.preferredWidth  = ITEM_W;
        le.preferredHeight = ITEM_H;

        var bg    = itemGO.AddComponent<Image>();
        bg.color  = ITEM_BG;
        bg.sprite = UISprite();
        bg.type   = Image.Type.Sliced;

        // Icon
        var iconGO = new GameObject("Icon");
        iconGO.transform.SetParent(itemGO.transform, false);
        var iconRT = iconGO.AddComponent<RectTransform>();
        iconRT.anchorMin        = iconRT.anchorMax = new Vector2(0.5f, 1f);
        iconRT.pivot            = new Vector2(0.5f, 1f);
        iconRT.sizeDelta        = new Vector2(ICON_SIZE, ICON_SIZE);
        iconRT.anchoredPosition = new Vector2(0f, -6f);
        iconImg                 = iconGO.AddComponent<Image>();
        iconImg.preserveAspect  = true;

        // Text
        var txtGO = new GameObject("AmountText");
        txtGO.transform.SetParent(itemGO.transform, false);
        var txtRT = txtGO.AddComponent<RectTransform>();
        txtRT.anchorMin        = new Vector2(0f, 0f);
        txtRT.anchorMax        = new Vector2(1f, 0f);
        txtRT.pivot            = new Vector2(0.5f, 0f);
        txtRT.sizeDelta        = new Vector2(0f, 26f);
        txtRT.anchoredPosition = new Vector2(0f, 5f);

        amountTxt           = txtGO.AddComponent<TextMeshProUGUI>();
        amountTxt.text      = defaultText;
        amountTxt.fontSize  = 15;
        amountTxt.fontStyle = FontStyles.Bold;
        amountTxt.color     = textColor;
        amountTxt.alignment = TextAlignmentOptions.Center;
    }

    static Sprite UISprite() =>
        AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");

    static string GetPath(GameObject go)
    {
        string path = go.name;
        var t = go.transform.parent;
        while (t != null) { path = t.name + "/" + path; t = t.parent; }
        return path;
    }
}
#endif