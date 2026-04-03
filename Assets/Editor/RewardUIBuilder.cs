// ============================================================
//  RewardUIBuilder.cs — Assets/Editor/RewardUIBuilder.cs
//
//  Tools > Build Reward Canvas
//
//  Layout:
//  RewardCanvas (Canvas overlay, sortOrder=30)
//  ├── BackgroundOverlay  ← mờ toàn màn hình (KHÔNG chứa RewardPanel)
//  └── RewardPanel        ← container scale animation (sibling với overlay)
//      ├── CardRow        ← HorizontalLayoutGroup chứa 2 card
//      │   ├── CoinCard   (trong suốt, icon + text — KHÔNG bị ảnh hưởng alpha overlay)
//      │   └── XpCard     (trong suốt, icon + text)
//      └── ConfirmButton  ← xanh, bo góc mềm
// ============================================================
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;

public static class RewardUIBuilder
{
    // ── Theme ────────────────────────────────────────────────────────────────
    static readonly Color CARD_BG       = new Color(1f,   1f,   1f,   0.15f);
    static readonly Color CARD_BORDER   = new Color(1f,   1f,   1f,   0.35f);
    static readonly Color OVERLAY_BG    = new Color(0f,   0f,   0f,   0.25f);
    static readonly Color BTN_GREEN     = new Color(0.20f,0.72f,0.45f,1f);
    static readonly Color BTN_GREEN_HL  = new Color(0.25f,0.85f,0.55f,1f);
    static readonly Color BTN_GREEN_PR  = new Color(0.15f,0.58f,0.35f,1f);
    static readonly Color TEXT_WHITE    = new Color(1f,   1f,   1f,   0.95f);
    static readonly Color TEXT_HINT     = new Color(1f,   1f,   1f,   0.70f);
    static readonly Color AMOUNT_YELLOW = new Color(1.00f,0.92f,0.40f,1f);
    static readonly Color AMOUNT_BLUE   = new Color(0.50f,0.85f,1.00f,1f);

    // ── Layout ───────────────────────────────────────────────────────────────
    const float REF_W   = 720f;
    const float REF_H   = 1280f;
    const float CARD_W  = 240f;
    const float CARD_H  = 280f;
    const float GAP     = 24f;
    const float BTN_H   = 64f;
    const float BTN_W   = 400f;
    const float PANEL_W = CARD_W * 2 + GAP + 32f;
    const float PANEL_H = CARD_H + BTN_H + GAP * 3;

    // ════════════════════════════════════════════════════════════════════════
    [MenuItem("Tools/Build Reward Canvas")]
    public static void BuildRewardCanvas()
    {
        Undo.SetCurrentGroupName("Build Reward Canvas");
        int grp = Undo.GetCurrentGroup();

        var old = GameObject.Find("RewardCanvas");
        if (old != null) Undo.DestroyObjectImmediate(old);

        // Canvas
        var canvasGO = new GameObject("RewardCanvas");
        Undo.RegisterCreatedObjectUndo(canvasGO, "RewardCanvas");

        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 30;

        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(REF_W, REF_H);
        scaler.matchWidthOrHeight = 0.5f;

        canvasGO.AddComponent<GraphicRaycaster>();

        // Overlay
        var overlay = MakeGO("BackgroundOverlay", canvasGO.transform);
        Stretch(overlay.GetComponent<RectTransform>());

        var ovImg = overlay.AddComponent<Image>();
        ovImg.color = OVERLAY_BG;
        ovImg.raycastTarget = true;

        // Panel
        var panel = MakeGO("RewardPanel", canvasGO.transform);
        var panelRT = panel.GetComponent<RectTransform>();
        Center(panelRT, PANEL_W, PANEL_H);

        // Row
        var cardRow = MakeGO("CardRow", panel.transform);
        var rowRT = cardRow.GetComponent<RectTransform>();

        rowRT.anchorMin = rowRT.anchorMax = rowRT.pivot = new Vector2(0.5f, 1f);
        rowRT.anchoredPosition = new Vector2(0f, 0f);
        rowRT.sizeDelta = new Vector2(CARD_W * 2 + GAP, CARD_H);

        var hlg = cardRow.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = GAP;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = false;
        hlg.childAlignment = TextAnchor.MiddleCenter;

        TMP_Text coinAmountTxt;
        Image coinIconImg;

        BuildCard(
            "CoinCard",
            cardRow.transform,
            "Xu",
            AMOUNT_YELLOW,
            "+0",
            out coinIconImg,
            out coinAmountTxt
        );

        TMP_Text xpAmountTxt;
        Image xpIconImg;

        BuildCard(
            "XpCard",
            cardRow.transform,
            "XP",
            AMOUNT_BLUE,
            "+0 XP",
            out xpIconImg,
            out xpAmountTxt
        );

        // Button
        var btnGO = MakeGO("ConfirmButton", panel.transform);
        var btnRT = btnGO.GetComponent<RectTransform>();

        btnRT.anchorMin = btnRT.anchorMax = btnRT.pivot = new Vector2(0.5f, 0f);
        btnRT.sizeDelta = new Vector2(BTN_W, BTN_H);

        var btnImg = btnGO.AddComponent<Image>();
        btnImg.color = BTN_GREEN;
        btnImg.sprite = Spr();
        btnImg.type = Image.Type.Sliced;

        var btn = btnGO.AddComponent<Button>();

        var btnLabel = MakeGO("Label", btnGO.transform);
        StretchRT(btnLabel.GetComponent<RectTransform>());

        var btnTxt = btnLabel.AddComponent<TextMeshProUGUI>();
        btnTxt.text = "Nhận Thưởng!";
        btnTxt.fontSize = 22;
        btnTxt.fontStyle = FontStyles.Bold;
        btnTxt.alignment = TextAlignmentOptions.Center;

        // RewardManager
        var rmGO = GameObject.Find("RewardManager");

        if (rmGO == null)
        {
            rmGO = new GameObject("RewardManager");
            Undo.RegisterCreatedObjectUndo(rmGO, "RewardManager");
        }

        var rm = rmGO.GetComponent<RewardManager>();

        if (rm == null)
            rm = Undo.AddComponent<RewardManager>(rmGO);

        var so = new SerializedObject(rm);

        so.FindProperty("rewardCanvas").objectReferenceValue = canvasGO;
        so.FindProperty("rewardPanel").objectReferenceValue = panel;
        so.FindProperty("coinIcon").objectReferenceValue = coinIconImg;
        so.FindProperty("coinAmountText").objectReferenceValue = coinAmountTxt;
        so.FindProperty("xpIcon").objectReferenceValue = xpIconImg;
        so.FindProperty("xpAmountText").objectReferenceValue = xpAmountTxt;
        so.FindProperty("confirmButton").objectReferenceValue = btn;

        so.ApplyModifiedProperties();

        canvasGO.SetActive(false);

        Undo.CollapseUndoOperations(grp);

        EditorUtility.DisplayDialog(
            "Reward UI Builder",
            "Reward Canvas đã được tạo thành công!",
            "OK"
        );
    }

    // ─────────────────────────────────────────────
    static void BuildCard(
        string name,
        Transform parent,
        string label,
        Color amountColor,
        string defaultAmount,
        out Image iconImg,
        out TMP_Text amountTxt
    )
    {
        var card = MakeGO(name, parent);

        var le = card.AddComponent<LayoutElement>();
        le.preferredWidth = CARD_W;
        le.preferredHeight = CARD_H;

        var bg = card.AddComponent<Image>();
        bg.color = CARD_BG;
        bg.sprite = Spr();
        bg.type = Image.Type.Sliced;

        var iconGO = MakeGO("IconImage", card.transform);
        var iconRT = iconGO.GetComponent<RectTransform>();

        iconRT.anchorMin = iconRT.anchorMax =
        iconRT.pivot = new Vector2(0.5f, 1f);

        iconRT.sizeDelta = new Vector2(120, 120);

        iconImg = iconGO.AddComponent<Image>();

        var labelGO = MakeGO("LabelText", card.transform);
        var labelTxt = labelGO.AddComponent<TextMeshProUGUI>();

        labelTxt.text = label;
        labelTxt.alignment = TextAlignmentOptions.Center;

        var amountGO = MakeGO("AmountText", card.transform);

        amountTxt = amountGO.AddComponent<TextMeshProUGUI>();
        amountTxt.text = defaultAmount;
        amountTxt.fontSize = 36;
        amountTxt.fontStyle = FontStyles.Bold;
        amountTxt.color = amountColor;
        amountTxt.alignment = TextAlignmentOptions.Center;
    }

    static GameObject MakeGO(string name, Transform parent)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
        return go;
    }

    static void Center(RectTransform rt, float w, float h)
    {
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(w, h);
    }

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    static void StretchRT(RectTransform rt) => Stretch(rt);

    static Sprite Spr()
    {
        return AssetDatabase.GetBuiltinExtraResource<Sprite>(
            "UI/Skin/UISprite.psd"
        );
    }
}
#endif