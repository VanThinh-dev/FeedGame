// =============================================================================
// AvatarWidgetBuilder.cs — Assets/Editor/AvatarWidgetBuilder.cs
//
// Tools > Build AvatarWidget (v3 — 6 phòng)
//
// Tạo / cập nhật toàn bộ AvatarWidget:
//   • XPRingBg + XPRing (Image Radial360)
//   • AvatarCircle + LevelBadge + XpProgressText
//   • DropdownPanel với 6 room buttons (2×3 grid) + separator + action buttons
//   • Lock overlays + lock level texts cho 5 phòng cần level
//   • Wire tất cả references vào AvatarWidgetController
//
// Chạy: Tools > Build AvatarWidget (v3 — 6 phòng)
// =============================================================================

#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public static class AvatarWidgetBuilder
{
    // ── Layout constants ──────────────────────────────────────────────────────
    const float WIDGET_SIZE      = 90f;
    const float RING_SIZE        = 90f;
    const float AVATAR_SIZE      = 72f;
    const float BADGE_SIZE       = 26f;

    const float DROPDOWN_W       = 260f;
    const float DROPDOWN_H       = 310f;  // tăng để chứa 2 hàng × 3 cột
    const float ROOM_BTN_SIZE    = 62f;
    const float ROOM_BTN_SPACING = 8f;

    const float ACTION_BTN_W     = 220f;
    const float ACTION_BTN_H     = 40f;

    // ── Colors ────────────────────────────────────────────────────────────────
    static readonly Color PANEL_BG      = new Color(0.08f, 0.08f, 0.15f, 0.95f);
    static readonly Color ROOM_BTN_BG   = new Color(0.20f, 0.45f, 0.80f, 1f);
    static readonly Color ROOM_LOCK_BG  = new Color(0.15f, 0.15f, 0.15f, 0.85f);
    static readonly Color ACTION_SHOP   = new Color(0.20f, 0.70f, 0.35f, 1f);
    static readonly Color ACTION_INV    = new Color(0.25f, 0.55f, 0.85f, 1f);
    static readonly Color ACTION_LOGOUT = new Color(0.75f, 0.22f, 0.22f, 1f);
    static readonly Color XP_GREEN      = new Color(0.20f, 0.85f, 0.40f, 1f);
    static readonly Color SEPARATOR_COL = new Color(1f,    1f,    1f,    0.12f);

    // =========================================================================
    [MenuItem("Tools/Build AvatarWidget (v3 — 6 phòng)")]
    public static void BuildAvatarWidget()
    {
        Undo.SetCurrentGroupName("Build AvatarWidget v3");
        int grp = Undo.GetCurrentGroup();

        // ── Tìm BedroomCanvas ─────────────────────────────────────────────────
        var bedroomCanvas = GameObject.Find("BedroomCanvas");
        if (bedroomCanvas == null)
        {
            EditorUtility.DisplayDialog("AvatarWidget Builder",
                "Không tìm thấy BedroomCanvas!\nChạy 'Tools > Build Bedroom Scene UI' trước.", "OK");
            return;
        }

        // ── Xoá AvatarWidget cũ nếu có ───────────────────────────────────────
        var oldWidget = bedroomCanvas.transform.Find("AvatarWidget");
        if (oldWidget != null) Undo.DestroyObjectImmediate(oldWidget.gameObject);

        // ── Root: AvatarWidget ────────────────────────────────────────────────
        var widgetGO = CreateGO("AvatarWidget", bedroomCanvas.transform);
        var widgetRT = widgetGO.AddComponent<RectTransform>();
        // Góc trên phải
        widgetRT.anchorMin        = new Vector2(1f, 1f);
        widgetRT.anchorMax        = new Vector2(1f, 1f);
        widgetRT.pivot            = new Vector2(1f, 1f);
        widgetRT.sizeDelta        = new Vector2(WIDGET_SIZE, WIDGET_SIZE);
        widgetRT.anchoredPosition = new Vector2(-10f, -10f);

        // ── XP Ring (background + filled) ────────────────────────────────────
        var ringBgGO = CreateGO("XPRingBg", widgetGO.transform);
        var ringBgRT = ringBgGO.AddComponent<RectTransform>();
        StretchFull(ringBgRT);
        var ringBgImg        = ringBgGO.AddComponent<Image>();
        ringBgImg.color      = new Color(0.2f, 0.2f, 0.2f, 0.6f);
        ringBgImg.type       = Image.Type.Filled;
        ringBgImg.fillMethod = Image.FillMethod.Radial360;
        ringBgImg.fillAmount = 1f;

        var ringGO = CreateGO("XPRing", widgetGO.transform);
        var ringRT = ringGO.AddComponent<RectTransform>();
        StretchFull(ringRT);
        var ringImg            = ringGO.AddComponent<Image>();
        ringImg.color          = XP_GREEN;
        ringImg.type           = Image.Type.Filled;
        ringImg.fillMethod     = Image.FillMethod.Radial360;
        ringImg.fillOrigin     = (int)Image.Origin360.Top;
        ringImg.fillClockwise  = true;
        ringImg.fillAmount     = 0.45f; // preview

        // ── AvatarCircle ──────────────────────────────────────────────────────
        var avatarGO = CreateGO("AvatarCircle", widgetGO.transform);
        var avatarRT = avatarGO.AddComponent<RectTransform>();
        avatarRT.anchorMin        = new Vector2(0.5f, 0.5f);
        avatarRT.anchorMax        = new Vector2(0.5f, 0.5f);
        avatarRT.pivot            = new Vector2(0.5f, 0.5f);
        avatarRT.sizeDelta        = new Vector2(AVATAR_SIZE, AVATAR_SIZE);
        avatarRT.anchoredPosition = Vector2.zero;
        avatarGO.AddComponent<Image>().color = new Color(0.6f, 0.8f, 1f, 1f);
        var avatarBtn = avatarGO.AddComponent<Button>();

        // ── LevelBadge ────────────────────────────────────────────────────────
        var badgeGO = CreateGO("LevelBadge", widgetGO.transform);
        var badgeRT = badgeGO.AddComponent<RectTransform>();
        badgeRT.anchorMin        = new Vector2(0f, 0f);
        badgeRT.anchorMax        = new Vector2(0f, 0f);
        badgeRT.pivot            = new Vector2(0.5f, 0.5f);
        badgeRT.sizeDelta        = new Vector2(BADGE_SIZE + 10f, BADGE_SIZE);
        badgeRT.anchoredPosition = new Vector2(12f, 12f);
        badgeGO.AddComponent<Image>().color = new Color(0.10f, 0.25f, 0.65f, 1f);

        var lvTextGO = CreateGO("LevelText", badgeGO.transform);
        var lvTextRT = lvTextGO.AddComponent<RectTransform>();
        StretchFull(lvTextRT);
        var lvTMP       = lvTextGO.AddComponent<TextMeshProUGUI>();
        lvTMP.text      = "Lv.1";
        lvTMP.fontSize  = 11;
        lvTMP.fontStyle = FontStyles.Bold;
        lvTMP.alignment = TextAlignmentOptions.Center;
        lvTMP.color     = Color.white;

        // ── XP Progress Text (ẩn mặc định, hiện khi hover nếu muốn) ──────────
        var xpTextGO = CreateGO("XpProgressText", widgetGO.transform);
        var xpTextRT = xpTextGO.AddComponent<RectTransform>();
        xpTextRT.anchorMin        = new Vector2(0.5f, 0f);
        xpTextRT.anchorMax        = new Vector2(0.5f, 0f);
        xpTextRT.pivot            = new Vector2(0.5f, 1f);
        xpTextRT.sizeDelta        = new Vector2(120f, 20f);
        xpTextRT.anchoredPosition = new Vector2(0f, -4f);
        var xpTMP       = xpTextGO.AddComponent<TextMeshProUGUI>();
        xpTMP.text      = "0 / 100 XP";
        xpTMP.fontSize  = 9;
        xpTMP.alignment = TextAlignmentOptions.Center;
        xpTMP.color     = new Color(0.85f, 0.85f, 0.85f, 1f);

        // ── DropdownPanel ─────────────────────────────────────────────────────
        var panelGO = CreateGO("DropdownPanel", bedroomCanvas.transform);
        var panelRT = panelGO.AddComponent<RectTransform>();
        panelRT.anchorMin        = new Vector2(1f, 1f);
        panelRT.anchorMax        = new Vector2(1f, 1f);
        panelRT.pivot            = new Vector2(1f, 1f);
        panelRT.sizeDelta        = new Vector2(DROPDOWN_W, DROPDOWN_H);
        panelRT.anchoredPosition = new Vector2(-10f, -(WIDGET_SIZE + 14f));
        var panelImg       = panelGO.AddComponent<Image>();
        panelImg.color     = PANEL_BG;
        panelImg.sprite    = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        panelImg.type      = Image.Type.Sliced;
        panelGO.SetActive(false);

        // ── RoomButtons container (GridLayout 2×3) ────────────────────────────
        var roomGridGO = CreateGO("RoomButtons", panelGO.transform);
        var roomGridRT = roomGridGO.AddComponent<RectTransform>();
        roomGridRT.anchorMin        = new Vector2(0f, 1f);
        roomGridRT.anchorMax        = new Vector2(1f, 1f);
        roomGridRT.pivot            = new Vector2(0.5f, 1f);
        roomGridRT.offsetMin        = new Vector2(8f,  0f);
        roomGridRT.offsetMax        = new Vector2(-8f, 0f);
        roomGridRT.sizeDelta        = new Vector2(0f, (ROOM_BTN_SIZE + ROOM_BTN_SPACING) * 2 + 16f);
        roomGridRT.anchoredPosition = new Vector2(0f, -8f);

        var grid                    = roomGridGO.AddComponent<GridLayoutGroup>();
        grid.cellSize               = new Vector2(ROOM_BTN_SIZE, ROOM_BTN_SIZE);
        grid.spacing                = new Vector2(ROOM_BTN_SPACING, ROOM_BTN_SPACING);
        grid.constraint             = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount        = 3;
        grid.childAlignment         = TextAnchor.UpperCenter;

        // Tạo 6 room buttons
        string[] roomNames   = { "Phong ngu", "Phong khach", "Phong bep",
                                  "Phong tam",  "Phong choi",   "San vuon" };
        int[]    roomLevels  = { 0, 5, 10, 15, 20, 25 };

        var roomButtons      = new Button[6];
        var lockOverlays     = new GameObject[6];
        var lockTexts        = new TMP_Text[6];

        for (int i = 0; i < 6; i++)
        {
            var (btn, lockOverlay, lockText) = CreateRoomButton(
                roomGridGO.transform, roomNames[i], roomLevels[i]);
            roomButtons[i]  = btn;
            lockOverlays[i] = lockOverlay;
            lockTexts[i]    = lockText;
        }

        // ── Separator ─────────────────────────────────────────────────────────
        var sepGO = CreateGO("Separator", panelGO.transform);
        var sepRT = sepGO.AddComponent<RectTransform>();
        sepRT.anchorMin        = new Vector2(0.05f, 1f);
        sepRT.anchorMax        = new Vector2(0.95f, 1f);
        sepRT.pivot            = new Vector2(0.5f, 1f);
        sepRT.sizeDelta        = new Vector2(0f, 1f);
        // Vị trí dưới room grid: 8 + gridH + 4
        float gridH = (ROOM_BTN_SIZE + ROOM_BTN_SPACING) * 2 + 16f;
        sepRT.anchoredPosition = new Vector2(0f, -(8f + gridH + 4f));
        sepGO.AddComponent<Image>().color = SEPARATOR_COL;

        // ── Action Buttons container ──────────────────────────────────────────
        var actionsGO = CreateGO("ActionButtons", panelGO.transform);
        var actionsRT = actionsGO.AddComponent<RectTransform>();
        actionsRT.anchorMin        = new Vector2(0f, 0f);
        actionsRT.anchorMax        = new Vector2(1f, 0f);
        actionsRT.pivot            = new Vector2(0.5f, 0f);
        actionsRT.offsetMin        = new Vector2(8f, 0f);
        actionsRT.offsetMax        = new Vector2(-8f, 0f);
        actionsRT.sizeDelta        = new Vector2(0f, (ACTION_BTN_H + 6f) * 3f + 8f);
        actionsRT.anchoredPosition = new Vector2(0f, 8f);
        var actVL                    = actionsGO.AddComponent<VerticalLayoutGroup>();
        actVL.spacing                = 6f;
        actVL.childForceExpandWidth  = true;
        actVL.childForceExpandHeight = false;
        actVL.childAlignment         = TextAnchor.LowerCenter;

        var shopBtn      = CreateActionButton(actionsGO.transform, "Cửa hàng",  ACTION_SHOP);
        var inventoryBtn = CreateActionButton(actionsGO.transform, "Túi đồ",    ACTION_INV);
        var logoutBtn    = CreateActionButton(actionsGO.transform, "Đăng xuất", ACTION_LOGOUT);

        // ── DisplayName text ──────────────────────────────────────────────────
        // (Không tạo ở đây — thường nằm ngoài widget. Để user tự wire.)

        // ── Wire AvatarWidgetController ───────────────────────────────────────
        var ctrl = widgetGO.AddComponent<AvatarWidgetController>();
        var so   = new SerializedObject(ctrl);

        so.FindProperty("xpRingImage").objectReferenceValue        = ringImg;
        so.FindProperty("avatarCircleImage").objectReferenceValue  = avatarGO.GetComponent<Image>();
        so.FindProperty("avatarCircleButton").objectReferenceValue = avatarBtn;
        so.FindProperty("levelBadgeText").objectReferenceValue     = lvTMP;
        so.FindProperty("xpProgressText").objectReferenceValue     = xpTMP;
        so.FindProperty("dropdownPanel").objectReferenceValue      = panelGO;

        // Room buttons
        so.FindProperty("bedroomBtn").objectReferenceValue    = roomButtons[0];
        so.FindProperty("livingRoomBtn").objectReferenceValue = roomButtons[1];
        so.FindProperty("kitchenBtn").objectReferenceValue    = roomButtons[2];
        so.FindProperty("bathroomBtn").objectReferenceValue   = roomButtons[3];
        so.FindProperty("playRoomBtn").objectReferenceValue   = roomButtons[4];
        so.FindProperty("gardenBtn").objectReferenceValue     = roomButtons[5];

        // Lock overlays (index 0 = bedroom = không cần lock overlay thực sự)
        so.FindProperty("bedroomLockOverlay").objectReferenceValue    = lockOverlays[0];
        so.FindProperty("livingRoomLockOverlay").objectReferenceValue = lockOverlays[1];
        so.FindProperty("kitchenLockOverlay").objectReferenceValue    = lockOverlays[2];
        so.FindProperty("bathroomLockOverlay").objectReferenceValue   = lockOverlays[3];
        so.FindProperty("playRoomLockOverlay").objectReferenceValue   = lockOverlays[4];
        so.FindProperty("gardenLockOverlay").objectReferenceValue     = lockOverlays[5];

        // Lock texts (index 1–5, index 0 không cần)
        so.FindProperty("livingRoomLockText").objectReferenceValue = lockTexts[1];
        so.FindProperty("kitchenLockText").objectReferenceValue    = lockTexts[2];
        so.FindProperty("bathroomLockText").objectReferenceValue   = lockTexts[3];
        so.FindProperty("playRoomLockText").objectReferenceValue   = lockTexts[4];
        so.FindProperty("gardenLockText").objectReferenceValue     = lockTexts[5];

        // Action buttons
        so.FindProperty("shopBtn").objectReferenceValue      = shopBtn;
        so.FindProperty("inventoryBtn").objectReferenceValue = inventoryBtn;
        so.FindProperty("logoutBtn").objectReferenceValue    = logoutBtn;

        so.ApplyModifiedProperties();

        Undo.CollapseUndoOperations(grp);

        EditorUtility.DisplayDialog(
            "AvatarWidget Builder v3",
            "AvatarWidget (6 phòng) đã được tạo!\n\n" +
            "Việc còn lại:\n" +
            "1. Kéo avatar sprite vào AvatarWidgetController > Avatar Sprite\n" +
            "2. Kéo shopCanvas + inventoryCanvas vào Canvas References\n" +
            "3. Kéo icon sprite cho từng room button (Room Button Icons)\n" +
            "4. Kéo displayNameText nếu có\n\n" +
            "Level lock đang TẮT. Tìm '[LEVEL-LOCK]' để bật lại.",
            "OK");
    }

    // =========================================================================
    // HELPERS
    // =========================================================================

    private static (Button btn, GameObject lockOverlay, TMP_Text lockText)
        CreateRoomButton(Transform parent, string label, int requiredLevel)
    {
        var btnRootGO = CreateGO(label.Replace(" ", ""), parent);
        var btnRootRT = btnRootGO.AddComponent<RectTransform>();
        // GridLayout sẽ kiểm soát kích thước

        // Background
        var bg      = btnRootGO.AddComponent<Image>();
        bg.color    = ROOM_BTN_BG;
        bg.sprite   = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        bg.type     = Image.Type.Sliced;

        var btn = btnRootGO.AddComponent<Button>();

        // Label text
        var lblGO = CreateGO("Label", btnRootGO.transform);
        var lblRT = lblGO.AddComponent<RectTransform>();
        lblRT.anchorMin        = new Vector2(0f, 0f);
        lblRT.anchorMax        = new Vector2(1f, 0.38f);
        lblRT.offsetMin        = Vector2.zero;
        lblRT.offsetMax        = Vector2.zero;
        var lblTMP       = lblGO.AddComponent<TextMeshProUGUI>();
        lblTMP.text      = label;
        lblTMP.fontSize  = 8;
        lblTMP.alignment = TextAlignmentOptions.Center;
        lblTMP.color     = Color.white;

        // Lock overlay
        var lockGO = CreateGO("LockOverlay", btnRootGO.transform);
        var lockRT = lockGO.AddComponent<RectTransform>();
        StretchFull(lockRT);
        var lockImg   = lockGO.AddComponent<Image>();
        lockImg.color = ROOM_LOCK_BG;
        lockGO.SetActive(false); // Mặc định ẩn (test mode)

        TMP_Text lockTmp = null;
        if (requiredLevel > 0)
        {
            var lockTxtGO = CreateGO("LockLevelText", lockGO.transform);
            var lockTxtRT = lockTxtGO.AddComponent<RectTransform>();
            StretchFull(lockTxtRT);
            lockTmp          = lockTxtGO.AddComponent<TextMeshProUGUI>();
            lockTmp.text     = $"Lv.{requiredLevel}";
            lockTmp.fontSize = 10;
            lockTmp.fontStyle = FontStyles.Bold;
            lockTmp.alignment = TextAlignmentOptions.Center;
            lockTmp.color     = new Color(1f, 0.85f, 0.1f, 1f);
        }

        return (btn, lockGO, lockTmp);
    }

    private static Button CreateActionButton(Transform parent, string label, Color bgColor)
    {
        var go = CreateGO(label.Replace(" ", "") + "Btn", parent);
        var rt = go.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(0f, ACTION_BTN_H);

        var img    = go.AddComponent<Image>();
        img.color  = bgColor;
        img.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        img.type   = Image.Type.Sliced;

        var btn = go.AddComponent<Button>();

        var txtGO = CreateGO("Text", go.transform);
        var txtRT = txtGO.AddComponent<RectTransform>();
        StretchFull(txtRT);
        var tmp       = txtGO.AddComponent<TextMeshProUGUI>();
        tmp.text      = label;
        tmp.fontSize  = 14;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color     = Color.white;

        return btn;
    }

    private static GameObject CreateGO(string name, Transform parent)
    {
        var go = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(go, name);
        go.transform.SetParent(parent, false);
        return go;
    }

    private static void StretchFull(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}
#endif