// ============================================================
//  MedalUIBuilder.cs — Assets/Editor/MedalUIBuilder.cs
//
//  Tools > Build Medal Canvas (Simple)
//
//  Layout:
//  MedalCanvas
//  └── MainPanel
//      ├── TopBar
//      │   ├── Title
//      │   └── TabRow: Tab_Bronze | Tab_Silver | Tab_Gold
//      ├── CloseButton
//      ├── BronzePanel  (ScrollView → Content GridLayout)
//      ├── SilverPanel  (ScrollView → Content GridLayout)
//      └── GoldPanel    (ScrollView → Content GridLayout)
// ============================================================
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public static class MedalUIBuilder
{
    static readonly Color PANEL_BG   = new Color(0.12f, 0.13f, 0.18f, 0.97f);
    static readonly Color OVERLAY_BG = new Color(0f, 0f, 0f, 0.55f);
    static readonly Color TOPBAR_BG  = new Color(0.10f, 0.11f, 0.16f, 1f);
    static readonly Color CLOSE_RED  = new Color(0.90f, 0.30f, 0.30f, 1f);
    static readonly Color BRONZE_COL = new Color(0.80f, 0.50f, 0.20f, 1f);
    static readonly Color SILVER_COL = new Color(0.75f, 0.75f, 0.80f, 1f);
    static readonly Color GOLD_COL   = new Color(1.00f, 0.84f, 0.10f, 1f);
    static readonly Color TAB_OFF    = new Color(0.22f, 0.22f, 0.28f, 0.85f);
    static readonly Color TEXT_WHITE = new Color(1f, 1f, 1f, 0.95f);

    const float REF_W    = 720f;
    const float REF_H    = 1280f;
    const float TOPBAR_H = 120f;
    const float TAB_H    = 50f;
    const float CLOSE_SZ = 44f;

    [MenuItem("Tools/Build Medal Canvas (Simple)")]
    public static void Build()
    {
        Undo.SetCurrentGroupName("Build Medal Canvas Simple");
        int grp = Undo.GetCurrentGroup();

        var old = GameObject.Find("MedalCanvas");
        if (old != null) Undo.DestroyObjectImmediate(old);

        // ── Canvas ───────────────────────────────────────────────────────────
        var canvasGO        = new GameObject("MedalCanvas");
        Undo.RegisterCreatedObjectUndo(canvasGO, "MedalCanvas");
        var canvas          = canvasGO.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 20;
        var scaler                 = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(REF_W, REF_H);
        scaler.matchWidthOrHeight  = 0.5f;
        canvasGO.AddComponent<GraphicRaycaster>();

        // ── Overlay ──────────────────────────────────────────────────────────
        var ov = MakeRT("Overlay", canvasGO.transform);
        Stretch(ov);
        ov.gameObject.AddComponent<Image>().color = OVERLAY_BG;

        // ── MainPanel ─────────────────────────────────────────────────────────
        var panel = MakeRT("MainPanel", canvasGO.transform);
        panel.anchorMin = new Vector2(0.05f, 0.05f);
        panel.anchorMax = new Vector2(0.95f, 0.95f);
        panel.offsetMin = panel.offsetMax = Vector2.zero;
        var panelImg    = panel.gameObject.AddComponent<Image>();
        panelImg.color  = PANEL_BG;
        panelImg.sprite = GetSprite();
        panelImg.type   = Image.Type.Sliced;

        // ── TopBar ────────────────────────────────────────────────────────────
        var topBar = MakeRT("TopBar", panel);
        topBar.anchorMin        = new Vector2(0f, 1f);
        topBar.anchorMax        = new Vector2(1f, 1f);
        topBar.pivot            = new Vector2(0.5f, 1f);
        topBar.sizeDelta        = new Vector2(0f, TOPBAR_H);
        topBar.anchoredPosition = Vector2.zero;
        topBar.gameObject.AddComponent<Image>().color = TOPBAR_BG;

        // Title
        var titleRT = MakeRT("Title", topBar);
        titleRT.anchorMin        = new Vector2(0.5f, 1f);
        titleRT.anchorMax        = new Vector2(0.5f, 1f);
        titleRT.pivot            = new Vector2(0.5f, 1f);
        titleRT.anchoredPosition = new Vector2(0f, -10f);
        titleRT.sizeDelta        = new Vector2(400f, 40f);
        var titleTxt       = titleRT.gameObject.AddComponent<TextMeshProUGUI>();
        titleTxt.text      = "Huy Chuong";
        titleTxt.fontSize  = 26;
        titleTxt.fontStyle = FontStyles.Bold;
        titleTxt.color     = TEXT_WHITE;
        titleTxt.alignment = TextAlignmentOptions.Center;

        // TabRow
        var tabRow = MakeRT("TabRow", topBar);
        tabRow.anchorMin        = new Vector2(0f, 0f);
        tabRow.anchorMax        = new Vector2(1f, 0f);
        tabRow.pivot            = new Vector2(0.5f, 0f);
        tabRow.sizeDelta        = new Vector2(0f, TAB_H);
        tabRow.anchoredPosition = new Vector2(0f, 6f);
        var hlg                    = tabRow.gameObject.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing                = 6f;
        hlg.padding                = new RectOffset(10, 10, 0, 0);
        hlg.childForceExpandWidth  = true;
        hlg.childForceExpandHeight = true;
        hlg.childAlignment         = TextAnchor.MiddleCenter;

        Button tabBronzeBtn, tabSilverBtn, tabGoldBtn;
        BuildTab(tabRow, "Tab_Bronze", "Dong",  BRONZE_COL, out tabBronzeBtn);
        BuildTab(tabRow, "Tab_Silver", "Bac",   SILVER_COL, out tabSilverBtn);
        BuildTab(tabRow, "Tab_Gold",   "Vang",  GOLD_COL,   out tabGoldBtn);

        // ── CloseButton ───────────────────────────────────────────────────────
        var closeRT = MakeRT("CloseButton", panel);
        closeRT.anchorMin        = new Vector2(1f, 1f);
        closeRT.anchorMax        = new Vector2(1f, 1f);
        closeRT.pivot            = new Vector2(1f, 1f);
        closeRT.anchoredPosition = new Vector2(-8f, -8f);
        closeRT.sizeDelta        = new Vector2(CLOSE_SZ, CLOSE_SZ);
        var closeImg    = closeRT.gameObject.AddComponent<Image>();
        closeImg.color  = CLOSE_RED;
        closeImg.sprite = GetSprite();
        closeImg.type   = Image.Type.Sliced;
        var closeBtn    = closeRT.gameObject.AddComponent<Button>();
        var xRT         = MakeRT("X", closeRT);
        Stretch(xRT);
        var xTxt        = xRT.gameObject.AddComponent<TextMeshProUGUI>();
        xTxt.text       = "X";
        xTxt.fontSize   = 20;
        xTxt.fontStyle  = FontStyles.Bold;
        xTxt.color      = Color.white;
        xTxt.alignment  = TextAlignmentOptions.Center;

        // ── 3 Scroll Panels ───────────────────────────────────────────────────
        Transform bronzeContent, silverContent, goldContent;
        var bronzePanel = BuildScrollPanel(panel, "BronzePanel", TOPBAR_H, out bronzeContent);
        var silverPanel = BuildScrollPanel(panel, "SilverPanel", TOPBAR_H, out silverContent);
        var goldPanel   = BuildScrollPanel(panel, "GoldPanel",   TOPBAR_H, out goldContent);

        // Mặc định hiện Bronze, ẩn 2 cái kia
        silverPanel.SetActive(false);
        goldPanel.SetActive(false);

        // ── Gán MedalManager ──────────────────────────────────────────────────
        var mmGO = GameObject.Find("MedalManager");
        if (mmGO == null) { mmGO = new GameObject("MedalManager"); Undo.RegisterCreatedObjectUndo(mmGO, "MedalManager"); }
        var mm = mmGO.GetComponent<MedalManager>() ?? Undo.AddComponent<MedalManager>(mmGO);
        var so = new SerializedObject(mm);
        so.FindProperty("medalCanvas")    .objectReferenceValue = canvasGO;
        so.FindProperty("bronzePanel")    .objectReferenceValue = bronzePanel;
        so.FindProperty("silverPanel")    .objectReferenceValue = silverPanel;
        so.FindProperty("goldPanel")      .objectReferenceValue = goldPanel;
        so.FindProperty("bronzeContainer").objectReferenceValue = bronzeContent;
        so.FindProperty("silverContainer").objectReferenceValue = silverContent;
        so.FindProperty("goldContainer")  .objectReferenceValue = goldContent;
        so.FindProperty("tabBronze")      .objectReferenceValue = tabBronzeBtn;
        so.FindProperty("tabSilver")      .objectReferenceValue = tabSilverBtn;
        so.FindProperty("tabGold")        .objectReferenceValue = tabGoldBtn;
        so.FindProperty("closeButton")    .objectReferenceValue = closeBtn;
        so.ApplyModifiedProperties();

        canvasGO.SetActive(false);
        Undo.CollapseUndoOperations(grp);

        EditorUtility.DisplayDialog("Medal Canvas",
            "Da tao MedalCanvas voi tabbar!\n\n" +
            "Viec con lai:\n" +
            "1. Keo bronzeMedalSprite / silverMedalSprite / goldMedalSprite vao MedalManager", "OK");
    }

    // ── Build ScrollPanel ─────────────────────────────────────────────────────
    static GameObject BuildScrollPanel(RectTransform parent, string name, float topOffset, out Transform content)
    {
        var panelRT     = MakeRT(name, parent);
        panelRT.anchorMin = Vector2.zero;
        panelRT.anchorMax = Vector2.one;
        panelRT.offsetMin = new Vector2(0f, 0f);
        panelRT.offsetMax = new Vector2(0f, -topOffset);

        var scrollGO = new GameObject("ScrollView");
        Undo.RegisterCreatedObjectUndo(scrollGO, "ScrollView");
        scrollGO.transform.SetParent(panelRT, false);
        var scrollRT    = scrollGO.AddComponent<RectTransform>();
        scrollRT.anchorMin = Vector2.zero; scrollRT.anchorMax = Vector2.one;
        scrollRT.offsetMin = scrollRT.offsetMax = Vector2.zero;
        var scroll      = scrollGO.AddComponent<ScrollRect>();
        scroll.horizontal = false;

        var vpGO = new GameObject("Viewport");
        Undo.RegisterCreatedObjectUndo(vpGO, "Viewport");
        vpGO.transform.SetParent(scrollGO.transform, false);
        var vpRT = vpGO.AddComponent<RectTransform>();
        vpRT.anchorMin = Vector2.zero; vpRT.anchorMax = Vector2.one;
        vpRT.offsetMin = vpRT.offsetMax = Vector2.zero;
        vpGO.AddComponent<RectMask2D>();
        scroll.viewport = vpRT;

        var contentGO = new GameObject("Content");
        Undo.RegisterCreatedObjectUndo(contentGO, "Content");
        contentGO.transform.SetParent(vpGO.transform, false);
        var contentRT = contentGO.AddComponent<RectTransform>();
        contentRT.anchorMin = new Vector2(0f, 1f);
        contentRT.anchorMax = new Vector2(1f, 1f);
        contentRT.pivot     = new Vector2(0.5f, 1f);
        contentRT.sizeDelta = Vector2.zero;
        scroll.content      = contentRT;

        var glg = contentGO.AddComponent<GridLayoutGroup>();
        glg.cellSize        = new Vector2(80f, 80f);
        glg.spacing         = new Vector2(16f, 16f);
        glg.padding         = new RectOffset(20, 20, 20, 20);
        glg.constraint      = GridLayoutGroup.Constraint.FixedColumnCount;
        glg.constraintCount = 5;
        glg.childAlignment  = TextAnchor.UpperLeft;

        var csf = contentGO.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        content = contentRT;
        return panelRT.gameObject;
    }

    // ── Build Tab Button ──────────────────────────────────────────────────────
    static void BuildTab(RectTransform parent, string name, string label, Color color, out Button btn)
    {
        var go  = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(go, name);
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
        var img    = go.AddComponent<Image>();
        img.color  = color;
        img.sprite = GetSprite();
        img.type   = Image.Type.Sliced;
        btn        = go.AddComponent<Button>();

        var lblRT = MakeRT("Label", go.GetComponent<RectTransform>());
        Stretch(lblRT);
        var txt        = lblRT.gameObject.AddComponent<TextMeshProUGUI>();
        txt.text       = label;
        txt.fontSize   = 18;
        txt.fontStyle  = FontStyles.Bold;
        txt.color      = Color.white;
        txt.alignment  = TextAlignmentOptions.Center;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────
    static RectTransform MakeRT(string n, Transform p)
    {
        var go = new GameObject(n);
        Undo.RegisterCreatedObjectUndo(go, n);
        go.transform.SetParent(p, false);
        return go.AddComponent<RectTransform>();
    }

    static RectTransform MakeRT(string n, RectTransform p) => MakeRT(n, p.transform);

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    static Sprite GetSprite() =>
        AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
}
#endif  