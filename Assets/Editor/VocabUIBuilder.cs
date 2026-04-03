#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEditor;

// ─────────────────────────────────────────────────────────────────────────────
// VocabUIBuilder.cs  — v2
// Tools > Build Vocab Canvas
//
// FIX so với v1:
//   • VocabManager gắn thẳng lên VocabCanvas (không cần GameManager)
//   • Tạo WordRowPrefab (1 hàng Anh | Việt | X) và gán vào AddLessonPanel
//   • Wire đầy đủ tất cả serialized fields
// ─────────────────────────────────────────────────────────────────────────────

public class VocabUIBuilder : Editor
{
    private static readonly Color COL_BG       = new Color(0.97f, 0.95f, 0.90f, 1f);
    private static readonly Color COL_TOPBAR   = new Color(0.27f, 0.55f, 0.89f, 1f);
    private static readonly Color COL_ACTIVE   = new Color(0.18f, 0.42f, 0.75f, 1f);
    private static readonly Color COL_INACTIVE = new Color(0.60f, 0.75f, 0.95f, 1f);
    private static readonly Color COL_GREEN    = new Color(0.28f, 0.73f, 0.47f, 1f);
    private static readonly Color COL_RED      = new Color(0.90f, 0.35f, 0.35f, 1f);
    private static readonly Color COL_BLUE     = new Color(0.30f, 0.65f, 0.95f, 1f);
    private static readonly Color COL_WHITE    = Color.white;
    private const float W = 720f, H = 1280f;

    // ═════════════════════════════════════════════════════════════════════════
    [MenuItem("Tools/Build Vocab Canvas")]
    public static void Build()
    {
        // ── Xoá cũ nếu có ────────────────────────────────────────────────────
        var old = GameObject.Find("VocabCanvas");
        if (old != null)
        {
            if (!EditorUtility.DisplayDialog("Tồn tại rồi",
                "VocabCanvas đã có, xoá và tạo lại?", "Tạo lại", "Huỷ")) return;
            DestroyImmediate(old);
        }

        // ── 1. Root Canvas ────────────────────────────────────────────────────
        var root   = MakeCanvas("VocabCanvas", 20);
        root.SetActive(false);

        var bg     = MakeImage(root.transform, "Background", new Vector2(W, H), COL_BG);
        Stretch(bg.GetComponent<RectTransform>());

        // ── 2. CloseButton ────────────────────────────────────────────────────
        var closeBtn = MakeButton(bg.transform, "CloseButton", new Vector2(72, 72), "✕", 26, COL_RED);
        var closeBtnRT = closeBtn.GetComponent<RectTransform>();
        closeBtnRT.anchorMin = closeBtnRT.anchorMax = closeBtnRT.pivot = new Vector2(1, 1);
        closeBtnRT.anchoredPosition = new Vector2(-16, -16);

        // ── 3. TopBar ─────────────────────────────────────────────────────────
        var topBar   = MakeImage(bg.transform, "TopBar", new Vector2(W, 110), COL_TOPBAR);
        var topBarRT = topBar.GetComponent<RectTransform>();
        topBarRT.anchorMin = new Vector2(0, 1); topBarRT.anchorMax = new Vector2(1, 1);
        topBarRT.pivot = new Vector2(0.5f, 1);
        topBarRT.sizeDelta = new Vector2(0, 110);
        topBarRT.anchoredPosition = Vector2.zero;

        MakeTMP(topBar.transform, "Title_Text", "📚 Học Từ Vựng", 24, Color.white, FontStyles.Bold,
            new Vector2(0.05f, 0.5f), new Vector2(0.55f, 1f));

        var tabDaHoc   = MakeTabBtn(topBar.transform, "Tab_DaHoc",   "Đã học",   COL_INACTIVE, left: true);
        var tabChuaHoc = MakeTabBtn(topBar.transform, "Tab_ChuaHoc", "Chưa học", COL_ACTIVE,   left: false);

        // ── 4. ContentPanel ───────────────────────────────────────────────────
        var content   = MakeImage(bg.transform, "ContentPanel", Vector2.zero, new Color(0,0,0,0));
        var contentRT = content.GetComponent<RectTransform>();
        contentRT.anchorMin = Vector2.zero; contentRT.anchorMax = Vector2.one;
        contentRT.offsetMin = Vector2.zero; contentRT.offsetMax = new Vector2(0, -110);

        // ── 5. Scroll panels ──────────────────────────────────────────────────
        var daHocPanel   = MakeScrollView(content.transform, "DaHocPanel");
        var chuaHocPanel = MakeScrollView(content.transform, "ChuaHocPanel");
        daHocPanel.SetActive(false);
        chuaHocPanel.SetActive(true);

        // ── 6. AddButton (+) ──────────────────────────────────────────────────
        var addBtn   = MakeButton(chuaHocPanel.transform, "AddButton", new Vector2(72, 72), "+", 36, COL_GREEN);
        var addBtnRT = addBtn.GetComponent<RectTransform>();
        addBtnRT.anchorMin = addBtnRT.anchorMax = new Vector2(1, 0);
        addBtnRT.pivot = new Vector2(1, 0);
        addBtnRT.anchoredPosition = new Vector2(-16, 16);

        // ── 7. AddLessonPanel ─────────────────────────────────────────────────
        var addPanel = BuildAddLessonPanel(bg.transform);
        addPanel.SetActive(false);

        // ── 8. WordRowPrefab ──────────────────────────────────────────────────
        var wordRowPrefabPath = BuildWordRowPrefab();

        // ── 9. LessonCardPrefab ───────────────────────────────────────────────
        var cardPrefabPath = BuildLessonCardPrefab();

        // ── 10. Gắn VocabManager lên VocabCanvas ─────────────────────────────
        var vm   = root.AddComponent<VocabManager>();
        var vmSO = new SerializedObject(vm);

        vmSO.FindProperty("vocabCanvas").objectReferenceValue   = root;
        vmSO.FindProperty("daHocPanel").objectReferenceValue    = daHocPanel;
        vmSO.FindProperty("chuaHocPanel").objectReferenceValue  = chuaHocPanel;

        var daHocContent   = daHocPanel.transform.Find("Viewport/LessonListContent");
        var chuaHocContent = chuaHocPanel.transform.Find("Viewport/LessonListContent");
        if (daHocContent   != null) vmSO.FindProperty("daHocContent").objectReferenceValue   = daHocContent;
        if (chuaHocContent != null) vmSO.FindProperty("chuaHocContent").objectReferenceValue = chuaHocContent;

        var cardPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(cardPrefabPath);
        if (cardPrefab != null) vmSO.FindProperty("lessonCardPrefab").objectReferenceValue = cardPrefab;
        vmSO.ApplyModifiedProperties();

        // ── 11. Gắn VocabCanvasController ────────────────────────────────────
        var ctrl   = root.AddComponent<VocabCanvasController>();
        var ctrlSO = new SerializedObject(ctrl);
        ctrlSO.FindProperty("closeButton").objectReferenceValue      = closeBtn.GetComponent<Button>();
        ctrlSO.FindProperty("tabDaHocButton").objectReferenceValue   = tabDaHoc.GetComponent<Button>();
        ctrlSO.FindProperty("tabChuaHocButton").objectReferenceValue = tabChuaHoc.GetComponent<Button>();
        ctrlSO.FindProperty("addButton").objectReferenceValue        = addBtn.GetComponent<Button>();
        ctrlSO.FindProperty("daHocPanel").objectReferenceValue       = daHocPanel;
        ctrlSO.FindProperty("chuaHocPanel").objectReferenceValue     = chuaHocPanel;
        ctrlSO.FindProperty("addLessonPanel").objectReferenceValue   = addPanel;
        ctrlSO.ApplyModifiedProperties();

        // ── 12. Wire AddLessonPanel's wordRowPrefab ───────────────────────────
        var addPanelComp = addPanel.GetComponent<AddLessonPanel>();
        if (addPanelComp != null)
        {
            var wordRowPrefabGO = AssetDatabase.LoadAssetAtPath<GameObject>(wordRowPrefabPath);
            var apSO = new SerializedObject(addPanelComp);
            if (wordRowPrefabGO != null)
                apSO.FindProperty("wordRowPrefab").objectReferenceValue = wordRowPrefabGO;
            apSO.ApplyModifiedProperties();
        }

        // ── 13. Gắn VocabCanvas vào BedroomManager ───────────────────────────
        var bedroom = Object.FindFirstObjectByType<BedroomManager>();
        if (bedroom != null)
        {
            var bSO = new SerializedObject(bedroom);
            bSO.FindProperty("vocabCanvas").objectReferenceValue = root;
            bSO.ApplyModifiedProperties();
            Debug.Log("[VocabUIBuilder] ✅ Đã gán VocabCanvas vào BedroomManager.");
        }

        // ── 14. Gắn DeskHitArea script ────────────────────────────────────────
        var deskHitArea = GameObject.Find("DeskHitArea");
        if (deskHitArea != null)
        {
            if (deskHitArea.GetComponent<DeskHitArea>() == null)
                deskHitArea.AddComponent<DeskHitArea>();
            Debug.Log("[VocabUIBuilder] ✅ DeskHitArea script gắn xong.");
        }
        else
        {
            Debug.LogWarning("[VocabUIBuilder] Không tìm thấy DeskHitArea trong scene!");
        }

        Selection.activeGameObject = root;
        EditorUtility.SetDirty(root);
        Debug.Log("[VocabUIBuilder] ✅ Build xong! VocabManager nằm trên VocabCanvas.");
    }

    // ═════════════════════════════════════════════════════════════════════════
    // BUILD ADD LESSON PANEL
    // ═════════════════════════════════════════════════════════════════════════

    private static GameObject BuildAddLessonPanel(Transform parent)
    {
        // Dim overlay
        var overlay   = MakeImage(parent, "AddLessonPanel", new Vector2(W, H), new Color(0, 0, 0, 0.55f));
        Stretch(overlay.GetComponent<RectTransform>());

        // White popup
        var box   = MakeImage(overlay.transform, "PopupBox", new Vector2(680, 860), COL_WHITE);
        var boxRT = box.GetComponent<RectTransform>();
        boxRT.anchorMin = boxRT.anchorMax = boxRT.pivot = new Vector2(0.5f, 0.5f);
        boxRT.anchoredPosition = Vector2.zero;

        // Title
        MakeTMP(box.transform, "Title", "✏ Tạo bài mới", 24, new Color(0.2f, 0.2f, 0.2f),
            FontStyles.Bold, new Vector2(0.05f, 0.88f), new Vector2(0.85f, 0.97f));

        // CloseButton X
        var xBtn   = MakeButton(box.transform, "CloseButton", new Vector2(52, 52), "✕", 22, COL_RED);
        var xBtnRT = xBtn.GetComponent<RectTransform>();
        xBtnRT.anchorMin = xBtnRT.anchorMax = xBtnRT.pivot = new Vector2(1, 1);
        xBtnRT.anchoredPosition = new Vector2(-12, -12);

        // Lesson name input
        var nameField = MakeInputField(box.transform, "InputField_LessonName",
            "Tên bài học...", new Vector2(0.05f, 0.79f), new Vector2(0.95f, 0.88f));

        // Header row label
        MakeTMP(box.transform, "HeaderEng",  "Tiếng Anh",  14, new Color(0.4f, 0.4f, 0.4f),
            FontStyles.Bold, new Vector2(0.05f, 0.73f), new Vector2(0.50f, 0.79f));
        MakeTMP(box.transform, "HeaderViet", "Tiếng Việt", 14, new Color(0.4f, 0.4f, 0.4f),
            FontStyles.Bold, new Vector2(0.52f, 0.73f), new Vector2(0.95f, 0.79f));

        // ScrollView chứa danh sách từ
        var sv   = new GameObject("WordScrollView", typeof(RectTransform));
        sv.transform.SetParent(box.transform, false);
        var svRT = sv.GetComponent<RectTransform>();
        svRT.anchorMin = new Vector2(0.02f, 0.13f);
        svRT.anchorMax = new Vector2(0.98f, 0.73f);
        svRT.offsetMin = svRT.offsetMax = Vector2.zero;

        var svScroll = sv.AddComponent<ScrollRect>();
        var svImg    = sv.AddComponent<Image>();
        svImg.color  = new Color(0.93f, 0.93f, 0.93f, 1f);

        var viewport = new GameObject("Viewport", typeof(RectTransform));
        viewport.transform.SetParent(sv.transform, false);
        Stretch(viewport.GetComponent<RectTransform>());
        viewport.AddComponent<RectMask2D>();
        var vpImg   = viewport.AddComponent<Image>();
        vpImg.color = new Color(1, 1, 1, 0.01f);

        var listContent = new GameObject("WordListContent", typeof(RectTransform));
        listContent.transform.SetParent(viewport.transform, false);
        var lcRT    = listContent.GetComponent<RectTransform>();
        lcRT.anchorMin = new Vector2(0, 1); lcRT.anchorMax = new Vector2(1, 1);
        lcRT.pivot  = new Vector2(0.5f, 1);
        lcRT.anchoredPosition = Vector2.zero; lcRT.sizeDelta = Vector2.zero;

        var vlg     = listContent.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 6;
        vlg.padding = new RectOffset(6, 6, 6, 6);
        vlg.childForceExpandWidth = true;
        vlg.childControlHeight    = false;
        listContent.AddComponent<ContentSizeFitter>().verticalFit =
            ContentSizeFitter.FitMode.PreferredSize;

        svScroll.viewport   = viewport.GetComponent<RectTransform>();
        svScroll.content    = lcRT;
        svScroll.horizontal = false;
        svScroll.scrollSensitivity = 30;

        // "＋ Thêm từ" button
        var addRowBtn = MakeButton(box.transform, "AddRowButton", new Vector2(200, 52),
            "+ Thêm từ", 20, COL_BLUE);
        var addRowRT  = addRowBtn.GetComponent<RectTransform>();
        addRowRT.anchorMin = addRowRT.anchorMax = addRowRT.pivot = new Vector2(0.5f, 0);
        addRowRT.anchoredPosition = new Vector2(-90, 76);

        // "💾 Lưu bài" button
        var saveBtn   = MakeButton(box.transform, "SaveButton", new Vector2(200, 52),
            "Lưu bài", 20, COL_GREEN);
        var saveBtnRT = saveBtn.GetComponent<RectTransform>();
        saveBtnRT.anchorMin = saveBtnRT.anchorMax = saveBtnRT.pivot = new Vector2(0.5f, 0);
        saveBtnRT.anchoredPosition = new Vector2(90, 76);

        // Status text
        var statusGO = new GameObject("StatusText", typeof(RectTransform));
        statusGO.transform.SetParent(box.transform, false);
        var statusRT = statusGO.GetComponent<RectTransform>();
        statusRT.anchorMin = new Vector2(0.05f, 0.01f);
        statusRT.anchorMax = new Vector2(0.95f, 0.10f);
        statusRT.offsetMin = statusRT.offsetMax = Vector2.zero;
        var statusTMP = statusGO.AddComponent<TextMeshProUGUI>();
        statusTMP.fontSize  = 17;
        statusTMP.alignment = TextAlignmentOptions.Center;

        // Gắn AddLessonPanel script
        var addComp = overlay.AddComponent<AddLessonPanel>();
        var apSO    = new SerializedObject(addComp);
        apSO.FindProperty("lessonNameInput").objectReferenceValue =
            nameField.GetComponent<TMP_InputField>();
        apSO.FindProperty("addRowButton").objectReferenceValue =
            addRowBtn.GetComponent<Button>();
        apSO.FindProperty("saveButton").objectReferenceValue =
            saveBtn.GetComponent<Button>();
        apSO.FindProperty("closeButton").objectReferenceValue =
            xBtn.GetComponent<Button>();
        apSO.FindProperty("wordListContent").objectReferenceValue =
            listContent.GetComponent<Transform>();
        apSO.FindProperty("statusText").objectReferenceValue = statusTMP;
        apSO.ApplyModifiedProperties();

        // Close button trực tiếp
        xBtn.GetComponent<Button>().onClick.AddListener(() => overlay.SetActive(false));

        return overlay;
    }

    // ═════════════════════════════════════════════════════════════════════════
    // BUILD WORD ROW PREFAB
    // ═════════════════════════════════════════════════════════════════════════

    private static string BuildWordRowPrefab()
    {
        const string path = "Assets/Prefabs/WordRowPrefab.prefab";
        if (!System.IO.Directory.Exists("Assets/Prefabs"))
            System.IO.Directory.CreateDirectory("Assets/Prefabs");

        // Row container
        var row   = new GameObject("WordRowPrefab", typeof(RectTransform));
        var rowRT = row.GetComponent<RectTransform>();
        rowRT.sizeDelta = new Vector2(650, 56);

        var rowHLG = row.AddComponent<HorizontalLayoutGroup>();
        rowHLG.spacing = 6;
        rowHLG.padding = new RectOffset(4, 4, 2, 2);
        rowHLG.childForceExpandWidth  = false;
        rowHLG.childForceExpandHeight = true;
        rowHLG.childControlWidth      = false;
        rowHLG.childControlHeight     = true;

        // English InputField
        var engGO   = BuildRowInputField(row.transform, "Field_English", "apple...", 284);
        // Vietnamese InputField
        var vietGO  = BuildRowInputField(row.transform, "Field_Vietnamese", "quả táo...", 284);

        // Delete button X
        var delBtn  = MakeButton(row.transform, "DeleteButton", new Vector2(48, 48), "✕", 18, COL_RED);
        var delLE   = delBtn.AddComponent<LayoutElement>();
        delLE.preferredWidth  = 48;
        delLE.preferredHeight = 48;
        delLE.flexibleWidth   = 0;

        PrefabUtility.SaveAsPrefabAsset(row, path);
        DestroyImmediate(row);
        Debug.Log($"[VocabUIBuilder] ✅ WordRowPrefab → {path}");
        return path;
    }

    private static GameObject BuildRowInputField(Transform parent, string name,
        string placeholder, float width)
    {
        var go   = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt   = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(width, 52);

        var le   = go.AddComponent<LayoutElement>();
        le.preferredWidth  = width;
        le.preferredHeight = 52;
        le.flexibleWidth   = 0;

        var img  = go.AddComponent<Image>();
        img.color = COL_WHITE;

        var field = go.AddComponent<TMP_InputField>();

        var ta    = new GameObject("Text Area", typeof(RectTransform));
        ta.transform.SetParent(go.transform, false);
        Stretch(ta.GetComponent<RectTransform>(), new Vector2(8, 4), new Vector2(-8, -4));
        ta.AddComponent<RectMask2D>();

        var ph    = new GameObject("Placeholder", typeof(RectTransform));
        ph.transform.SetParent(ta.transform, false);
        Stretch(ph.GetComponent<RectTransform>());
        var phTMP = ph.AddComponent<TextMeshProUGUI>();
        phTMP.text      = placeholder;
        phTMP.fontSize  = 15;
        phTMP.color     = new Color(0.65f, 0.65f, 0.65f);
        phTMP.fontStyle = FontStyles.Italic;

        var txt    = new GameObject("Text", typeof(RectTransform));
        txt.transform.SetParent(ta.transform, false);
        Stretch(txt.GetComponent<RectTransform>());
        var txtTMP = txt.AddComponent<TextMeshProUGUI>();
        txtTMP.fontSize = 15;
        txtTMP.color    = Color.black;

        field.textViewport  = ta.GetComponent<RectTransform>();
        field.textComponent = txtTMP;
        field.placeholder   = phTMP;

        return go;
    }

    // ═════════════════════════════════════════════════════════════════════════
    // BUILD LESSON CARD PREFAB
    // ═════════════════════════════════════════════════════════════════════════

    private static string BuildLessonCardPrefab()
    {
        const string path = "Assets/Prefabs/LessonCardPrefab.prefab";
        if (!System.IO.Directory.Exists("Assets/Prefabs"))
            System.IO.Directory.CreateDirectory("Assets/Prefabs");

        if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null)
        {
            Debug.Log("[VocabUIBuilder] LessonCardPrefab đã tồn tại, giữ nguyên.");
            return path;
        }

        var card   = new GameObject("LessonCardPrefab", typeof(RectTransform));
        var cardRT = card.GetComponent<RectTransform>();
        cardRT.sizeDelta = new Vector2(320, 160);

        var cardImg = card.AddComponent<Image>();
        cardImg.color = COL_WHITE;
        card.AddComponent<Button>();

        var iconGO  = MakeImage(card.transform, "BookIcon", new Vector2(56, 56),
            new Color(0.4f, 0.65f, 1f));
        var iconRT  = iconGO.GetComponent<RectTransform>();
        iconRT.anchorMin = new Vector2(0, 0.5f); iconRT.anchorMax = new Vector2(0, 0.5f);
        iconRT.pivot = new Vector2(0, 0.5f); iconRT.anchoredPosition = new Vector2(16, 0);

        MakeTMP(card.transform, "LessonName_Text", "Tên bài học", 19,
            new Color(0.15f, 0.15f, 0.15f), FontStyles.Bold,
            new Vector2(0.28f, 0.52f), new Vector2(0.95f, 0.92f));

        MakeTMP(card.transform, "WordCount_Text", "0 từ", 15,
            new Color(0.5f, 0.5f, 0.5f), FontStyles.Normal,
            new Vector2(0.28f, 0.08f), new Vector2(0.95f, 0.50f));

        var badge = MakeTMP(card.transform, "CompletedBadge", "✓ Đã học", 14,
            new Color(0.2f, 0.7f, 0.3f), FontStyles.Bold,
            new Vector2(0.6f, 0.6f), new Vector2(1f, 1f));
        badge.SetActive(false);

        var lc   = card.AddComponent<LessonCard>();
        var lcSO = new SerializedObject(lc);
        lcSO.FindProperty("cardBackground").objectReferenceValue  = cardImg;
        lcSO.FindProperty("bookIcon").objectReferenceValue        = iconGO.GetComponent<Image>();
        lcSO.FindProperty("lessonNameText").objectReferenceValue  =
            card.transform.Find("LessonName_Text").GetComponent<TextMeshProUGUI>();
        lcSO.FindProperty("wordCountText").objectReferenceValue   =
            card.transform.Find("WordCount_Text").GetComponent<TextMeshProUGUI>();
        lcSO.FindProperty("completedBadge").objectReferenceValue  =
            badge.GetComponent<TextMeshProUGUI>();
        lcSO.FindProperty("tapButton").objectReferenceValue       =
            card.GetComponent<Button>();
        lcSO.ApplyModifiedProperties();

        PrefabUtility.SaveAsPrefabAsset(card, path);
        DestroyImmediate(card);
        Debug.Log($"[VocabUIBuilder] ✅ LessonCardPrefab → {path}");
        return path;
    }

    // ═════════════════════════════════════════════════════════════════════════
    // UI FACTORIES
    // ═════════════════════════════════════════════════════════════════════════

    private static GameObject MakeCanvas(string name, int order)
    {
        var go  = new GameObject(name);
        var c   = go.AddComponent<Canvas>();
        c.renderMode    = RenderMode.ScreenSpaceOverlay;
        c.sortingOrder  = order;
        var cs  = go.AddComponent<CanvasScaler>();
        cs.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        cs.referenceResolution = new Vector2(W, H);
        cs.matchWidthOrHeight  = 0.5f;
        go.AddComponent<GraphicRaycaster>();
        return go;
    }

    private static GameObject MakeScrollView(Transform parent, string name)
    {
        var sv   = new GameObject(name, typeof(RectTransform));
        sv.transform.SetParent(parent, false);
        Stretch(sv.GetComponent<RectTransform>());

        var sr   = sv.AddComponent<ScrollRect>();

        var vp   = new GameObject("Viewport", typeof(RectTransform));
        vp.transform.SetParent(sv.transform, false);
        Stretch(vp.GetComponent<RectTransform>());
        vp.AddComponent<RectMask2D>();
        var vpImg = vp.AddComponent<Image>();
        vpImg.color = new Color(1, 1, 1, 0.01f);

        var cnt  = new GameObject("LessonListContent", typeof(RectTransform));
        cnt.transform.SetParent(vp.transform, false);
        var cntRT = cnt.GetComponent<RectTransform>();
        cntRT.anchorMin = new Vector2(0, 1); cntRT.anchorMax = new Vector2(1, 1);
        cntRT.pivot = new Vector2(0.5f, 1);
        cntRT.anchoredPosition = Vector2.zero; cntRT.sizeDelta = Vector2.zero;

        var grid = cnt.AddComponent<GridLayoutGroup>();
        grid.cellSize        = new Vector2(316, 160);
        grid.spacing         = new Vector2(16, 16);
        grid.padding         = new RectOffset(16, 16, 16, 16);
        grid.constraint      = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 2;
        cnt.AddComponent<ContentSizeFitter>().verticalFit =
            ContentSizeFitter.FitMode.PreferredSize;

        sr.viewport   = vp.GetComponent<RectTransform>();
        sr.content    = cntRT;
        sr.horizontal = false;
        sr.scrollSensitivity = 30;

        return sv;
    }

    private static GameObject MakeTabBtn(Transform parent, string name,
        string label, Color color, bool left)
    {
        var btn   = MakeButton(parent, name, new Vector2(150, 46), label, 18, color);
        var btnRT = btn.GetComponent<RectTransform>();
        btnRT.anchorMin = new Vector2(left ? 0.30f : 0.52f, 0.1f);
        btnRT.anchorMax = new Vector2(left ? 0.50f : 0.72f, 0.9f);
        btnRT.offsetMin = btnRT.offsetMax = Vector2.zero;
        btnRT.sizeDelta = Vector2.zero;
        return btn;
    }

    private static GameObject MakeButton(Transform parent, string name,
        Vector2 size, string label, int fontSize, Color bgColor)
    {
        var go  = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt  = go.GetComponent<RectTransform>();
        rt.sizeDelta = size;

        var img = go.AddComponent<Image>();
        img.color = bgColor;
        go.AddComponent<Button>();

        var lbl = new GameObject("Label", typeof(RectTransform));
        lbl.transform.SetParent(go.transform, false);
        Stretch(lbl.GetComponent<RectTransform>());
        var tmp = lbl.AddComponent<TextMeshProUGUI>();
        tmp.text      = label;
        tmp.fontSize  = fontSize;
        tmp.color     = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;
        return go;
    }

    private static GameObject MakeImage(Transform parent, string name,
        Vector2 size, Color color)
    {
        var go  = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt  = go.GetComponent<RectTransform>();
        rt.sizeDelta = size;
        var img = go.AddComponent<Image>();
        img.color = color;
        return go;
    }

    private static GameObject MakeTMP(Transform parent, string name, string text,
        int size, Color color, FontStyles style,
        Vector2 anchorMin, Vector2 anchorMax)
    {
        var go  = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt  = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin; rt.anchorMax = anchorMax;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text      = text;
        tmp.fontSize  = size;
        tmp.color     = color;
        tmp.fontStyle = style;
        tmp.alignment = TextAlignmentOptions.MidlineLeft;
        return go;
    }

    private static GameObject MakeInputField(Transform parent, string name,
        string placeholder, Vector2 anchorMin, Vector2 anchorMax)
    {
        var go  = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt  = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin; rt.anchorMax = anchorMax;
        rt.offsetMin = rt.offsetMax = Vector2.zero;

        var img = go.AddComponent<Image>();
        img.color = new Color(0.92f, 0.92f, 0.92f);

        var field = go.AddComponent<TMP_InputField>();

        var ta    = new GameObject("Text Area", typeof(RectTransform));
        ta.transform.SetParent(go.transform, false);
        Stretch(ta.GetComponent<RectTransform>(), new Vector2(8, 4), new Vector2(-8, -4));
        ta.AddComponent<RectMask2D>();

        var ph    = new GameObject("Placeholder", typeof(RectTransform));
        ph.transform.SetParent(ta.transform, false);
        Stretch(ph.GetComponent<RectTransform>());
        var phTMP = ph.AddComponent<TextMeshProUGUI>();
        phTMP.text      = placeholder;
        phTMP.fontSize  = 16;
        phTMP.color     = new Color(0.6f, 0.6f, 0.6f);
        phTMP.fontStyle = FontStyles.Italic;

        var txt    = new GameObject("Text", typeof(RectTransform));
        txt.transform.SetParent(ta.transform, false);
        Stretch(txt.GetComponent<RectTransform>());
        var txtTMP = txt.AddComponent<TextMeshProUGUI>();
        txtTMP.fontSize = 16;
        txtTMP.color    = Color.black;

        field.textViewport  = ta.GetComponent<RectTransform>();
        field.textComponent = txtTMP;
        field.placeholder   = phTMP;
        return go;
    }

    private static void Stretch(RectTransform rt,
        Vector2 oMin = default, Vector2 oMax = default)
    {
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = oMin; rt.offsetMax = oMax;
    }
}
#endif
