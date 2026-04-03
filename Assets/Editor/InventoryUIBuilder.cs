#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEditor;
using System.IO;

// ═══════════════════════════════════════════════════════════════
// InventoryUIBuilder — Editor Script
// Menu: Tools > Build Inventory Canvas
// ═══════════════════════════════════════════════════════════════
public class InventoryUIBuilder : Editor
{
    private const string PREFAB_PATH = "Assets/Prefabs/Inventory";

    // ── Bảng màu ─────────────────────────────────────────────────
    private static readonly Color CLR_BG        = new Color(0.92f, 0.96f, 1.00f);
    private static readonly Color CLR_HEADER    = new Color(0.30f, 0.65f, 0.95f);
    private static readonly Color CLR_CARD_BG   = new Color(1.00f, 1.00f, 1.00f);
    private static readonly Color CLR_DETAIL_BG = new Color(0.95f, 0.98f, 1.00f);
    private static readonly Color CLR_TEXT_DARK  = new Color(0.15f, 0.15f, 0.20f);
    private static readonly Color CLR_ACCENT     = new Color(0.30f, 0.70f, 0.95f);

    [MenuItem("Tools/Build Inventory Canvas")]
    public static void BuildInventoryCanvas()
    {
        if (!Directory.Exists(PREFAB_PATH)) Directory.CreateDirectory(PREFAB_PATH);

        // Build card prefab trước
        GameObject cardPrefab = BuildInventoryCardPrefab();
        SavePrefab(cardPrefab, $"{PREFAB_PATH}/InventoryCardPrefab.prefab");

        Canvas bedroomCanvas = FindOrCreateCanvas("BedroomCanvas");

        Transform existing = bedroomCanvas.transform.Find("InventoryCanvas");
        if (existing) DestroyImmediate(existing.gameObject);

        // ══════════════════════════════════════════════════════════
        // INVENTORYCANVAS — overlay mờ toàn màn hình
        // ══════════════════════════════════════════════════════════
        GameObject invCanvas = CreateUI("InventoryCanvas", bedroomCanvas.transform);
        Canvas ic = invCanvas.AddComponent<Canvas>();
        ic.overrideSorting = true;
        ic.sortingOrder    = 20;
        invCanvas.AddComponent<GraphicRaycaster>();
        Image icBg = invCanvas.AddComponent<Image>();
        icBg.color = new Color(0f, 0f, 0f, 0.50f);
        StretchFull(invCanvas);
        invCanvas.SetActive(false);

        // ── Header ────────────────────────────────────────────────
        GameObject header = CreateUI("Header", invCanvas.transform);
        AddImage(header, CLR_HEADER);
        RectTransform headerRT = header.GetComponent<RectTransform>();
        headerRT.anchorMin = new Vector2(0f, 1f);
        headerRT.anchorMax = new Vector2(1f, 1f);
        headerRT.pivot     = new Vector2(0.5f, 1f);
        headerRT.offsetMin = headerRT.offsetMax = Vector2.zero;
        headerRT.sizeDelta = new Vector2(0f, 80f);

        // Title
        GameObject titleGO = CreateUI("Title", header.transform);
        TMP_Text title = titleGO.AddComponent<TextMeshProUGUI>();
        title.text      = "Túi Đồ";
        title.fontSize  = 28f;
        title.fontStyle = FontStyles.Bold;
        title.color     = Color.white;
        title.alignment = TextAlignmentOptions.Center;
        StretchFull(titleGO);

        // Close button
        GameObject closeBtn = MakeRoundButton("CloseButton", header.transform,
                                  Vector2.zero, 48f, new Color(0.85f, 0.30f, 0.20f));
        RectTransform closeBtnRT = closeBtn.GetComponent<RectTransform>();
        closeBtnRT.anchorMin = new Vector2(1f, 0f);
        closeBtnRT.anchorMax = new Vector2(1f, 1f);
        closeBtnRT.pivot     = new Vector2(1f, 0.5f);
        closeBtnRT.sizeDelta = new Vector2(48f, 0f);
        closeBtnRT.anchoredPosition = new Vector2(-12f, 0f);
        AddTMPLabel(closeBtn, "✕", 22f);

        // ── Background box (dưới header, toàn bộ phần còn lại) ───
        GameObject bgBox = CreateUI("BackgroundBox", invCanvas.transform);
        AddImage(bgBox, CLR_BG);
        RectTransform bgRT = bgBox.GetComponent<RectTransform>();
        bgRT.anchorMin = Vector2.zero;
        bgRT.anchorMax = Vector2.one;
        bgRT.offsetMin = Vector2.zero;
        bgRT.offsetMax = new Vector2(0f, -80f); // nhường chỗ cho header

        // ══════════════════════════════════════════════════════════
        // ITEM LIST PANEL — chiếm 60% bên trái của bgBox
        // ══════════════════════════════════════════════════════════
        GameObject listPanel = CreateUI("ItemListPanel", bgBox.transform);
        AddImage(listPanel, new Color(0.97f, 0.98f, 1f));
        RectTransform listRT = listPanel.GetComponent<RectTransform>();
        listRT.anchorMin = new Vector2(0f, 0f);
        listRT.anchorMax = new Vector2(0.60f, 1f); // 60% trái
        listRT.offsetMin = listRT.offsetMax = Vector2.zero;

        // ScrollRect bên trong listPanel
        GameObject scrollView = CreateUI("ScrollView", listPanel.transform);
        ScrollRect sr = scrollView.AddComponent<ScrollRect>();
        sr.horizontal = false;
        AddImage(scrollView, Color.clear);
        StretchFull(scrollView);

        // Viewport — QUAN TRỌNG: Image phải enabled để Mask hoạt động
        GameObject viewport = CreateUI("Viewport", scrollView.transform);
        Mask vpMask = viewport.AddComponent<Mask>();
        vpMask.showMaskGraphic = false;
        Image vpImg = viewport.AddComponent<Image>();
        vpImg.color   = new Color(1f, 1f, 1f, 0.01f); // gần transparent, KHÔNG dùng Color.clear
        vpImg.enabled = true;                           // phải enabled thì Mask mới chạy
        StretchFull(viewport);
        sr.viewport = viewport.GetComponent<RectTransform>();

        // Grid content bên trong viewport
        GameObject content   = CreateUI("Content", viewport.transform);
        GridLayoutGroup grid = content.AddComponent<GridLayoutGroup>();
        grid.cellSize        = new Vector2(110f, 130f);
        grid.spacing         = new Vector2(10f, 10f);
        grid.padding         = new RectOffset(10, 10, 10, 10);
        grid.constraint      = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 3;

        ContentSizeFitter csf = content.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        RectTransform contentRT = content.GetComponent<RectTransform>();
        // anchor top-stretch để content grow xuống
        contentRT.anchorMin        = new Vector2(0f, 1f);
        contentRT.anchorMax        = new Vector2(1f, 1f);
        contentRT.pivot            = new Vector2(0.5f, 1f);
        contentRT.anchoredPosition = Vector2.zero;
        contentRT.sizeDelta        = Vector2.zero;
        sr.content = contentRT;

        // ══════════════════════════════════════════════════════════
        // DETAIL PANEL — overlay độc lập, 40% bên PHẢI của bgBox
        // KHÔNG nằm trong bất kỳ Layout Group nào
        // ══════════════════════════════════════════════════════════
        GameObject detailPanel = CreateUI("DetailPanel", bgBox.transform);
        AddImage(detailPanel, CLR_DETAIL_BG);

        // Anchor cố định 60%–100% ngang, toàn bộ dọc
        RectTransform dpRT = detailPanel.GetComponent<RectTransform>();
        dpRT.anchorMin        = new Vector2(0.60f, 0f);
        dpRT.anchorMax        = new Vector2(1.00f, 1f);
        dpRT.pivot            = new Vector2(0f, 0.5f); // pivot trái → slide từ phải vào trái
        dpRT.offsetMin        = dpRT.offsetMax = Vector2.zero;

        // VerticalLayoutGroup bên trong detail panel
        VerticalLayoutGroup detailVLG = detailPanel.AddComponent<VerticalLayoutGroup>();
        detailVLG.padding  = new RectOffset(16, 16, 16, 16);
        detailVLG.spacing  = 12f;
        detailVLG.childControlWidth      = true;
        detailVLG.childControlHeight     = false;
        detailVLG.childForceExpandWidth  = true;
        detailVLG.childForceExpandHeight = false;

        detailPanel.SetActive(false);

        // Detail Image
        GameObject detImgGO = CreateUI("DetailImage", detailPanel.transform);
        Image detImg = detImgGO.AddComponent<Image>();
        detImg.color = new Color(0.88f, 0.88f, 0.88f);
        detImg.preserveAspect = true;
        LayoutElement diLE = detImgGO.AddComponent<LayoutElement>();
        diLE.preferredHeight = 160f;

        // Detail Name
        GameObject detNameGO = CreateUI("DetailName", detailPanel.transform);
        TMP_Text detName = detNameGO.AddComponent<TextMeshProUGUI>();
        detName.text      = "Tên Item";
        detName.fontSize  = 20f;
        detName.fontStyle = FontStyles.Bold;
        detName.color     = CLR_TEXT_DARK;
        detName.alignment = TextAlignmentOptions.Center;
        LayoutElement nameLE = detNameGO.AddComponent<LayoutElement>();
        nameLE.preferredHeight = 30f;

        // Detail Description
        GameObject detDescGO = CreateUI("DetailDesc", detailPanel.transform);
        TMP_Text detDesc = detDescGO.AddComponent<TextMeshProUGUI>();
        detDesc.text      = "Mô tả item...";
        detDesc.fontSize  = 14f;
        detDesc.color     = new Color(0.40f, 0.40f, 0.45f);
        detDesc.alignment = TextAlignmentOptions.Center;
        detDesc.overflowMode         = TextOverflowModes.Overflow;
        detDesc.enableWordWrapping   = true;
        LayoutElement descLE = detDescGO.AddComponent<LayoutElement>();
        descLE.preferredHeight = 60f;

        // Owner Count
        GameObject ownerGO = CreateUI("OwnerCount", detailPanel.transform);
        TMP_Text ownerT = ownerGO.AddComponent<TextMeshProUGUI>();
        ownerT.text      = "Số người sở hữu: 0";
        ownerT.fontSize  = 13f;
        ownerT.color     = new Color(0.55f, 0.35f, 0.80f);
        ownerT.alignment = TextAlignmentOptions.Center;
        LayoutElement ownerLE = ownerGO.AddComponent<LayoutElement>();
        ownerLE.preferredHeight = 22f;

        // Quantity Owned
        GameObject qtyGO = CreateUI("QuantityOwned", detailPanel.transform);
        TMP_Text qtyT = qtyGO.AddComponent<TextMeshProUGUI>();
        qtyT.text      = "Bạn có: 0";
        qtyT.fontSize  = 16f;
        qtyT.fontStyle = FontStyles.Bold;
        qtyT.color     = CLR_ACCENT;
        qtyT.alignment = TextAlignmentOptions.Center;
        LayoutElement qtyLE = qtyGO.AddComponent<LayoutElement>();
        qtyLE.preferredHeight = 26f;

        // ══════════════════════════════════════════════════════════
        // Gán InventoryManager
        // ══════════════════════════════════════════════════════════
        InventoryManager im = invCanvas.AddComponent<InventoryManager>();
        SerializedObject so = new SerializedObject(im);

        AssignRef(so, "inventoryCanvas",     invCanvas);
        AssignRef(so, "closeBtn",            closeBtn.GetComponent<Button>());
        AssignRef(so, "itemListGrid",        content.transform);
        AssignRef(so, "inventoryCardPrefab", AssetDatabase.LoadAssetAtPath<GameObject>(
                                                 $"{PREFAB_PATH}/InventoryCardPrefab.prefab"));
        AssignRef(so, "detailPanel",         detailPanel);
        AssignRef(so, "detailImage",         detImg);
        AssignRef(so, "detailNameText",      detName);
        AssignRef(so, "detailDescText",      detDesc);
        AssignRef(so, "ownerCountText",      ownerT);
        AssignRef(so, "quantityOwnedText",   qtyT);

        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(invCanvas);
        Selection.activeGameObject = invCanvas;
        AssetDatabase.SaveAssets();
        Debug.Log("[InventoryUIBuilder] ✅ InventoryCanvas đã tạo xong!");
    }

    // ══════════════════════════════════════════════════════════
    // Build InventoryCardPrefab
    // ══════════════════════════════════════════════════════════
    private static GameObject BuildInventoryCardPrefab()
    {
        GameObject card = CreateUI("InventoryCardPrefab", null);
        card.GetComponent<RectTransform>().sizeDelta = new Vector2(110f, 130f);

        // Nền card
        Image bg = card.AddComponent<Image>();
        bg.color  = CLR_CARD_BG;
        bg.sprite = MakeRoundedRectSprite();

        // Button toàn card
        Button btn = card.AddComponent<Button>();
        ColorBlock cb = btn.colors;
        cb.normalColor      = Color.white;
        cb.highlightedColor = new Color(0.90f, 0.95f, 1f);
        cb.pressedColor     = new Color(0.80f, 0.90f, 1f);
        btn.colors = cb;

        // Item Image
        GameObject imgGO = CreateUI("ItemImage", card.transform);
        Image itemImg = imgGO.AddComponent<Image>();
        itemImg.color = new Color(0.90f, 0.90f, 0.90f);
        itemImg.preserveAspect = true;
        RectTransform iRT = imgGO.GetComponent<RectTransform>();
        iRT.anchorMin = new Vector2(0.10f, 0.38f);
        iRT.anchorMax = new Vector2(0.90f, 0.93f);
        iRT.offsetMin = iRT.offsetMax = Vector2.zero;

        // Item Name
        GameObject nameGO = CreateUI("ItemName", card.transform);
        TMP_Text nameT = nameGO.AddComponent<TextMeshProUGUI>();
        nameT.text         = "Tên Item";
        nameT.fontSize     = 11f;
        nameT.fontStyle    = FontStyles.Bold;
        nameT.color        = new Color(0.15f, 0.15f, 0.20f);
        nameT.alignment    = TextAlignmentOptions.Center;
        nameT.overflowMode = TextOverflowModes.Ellipsis;
        RectTransform nRT = nameGO.GetComponent<RectTransform>();
        nRT.anchorMin = new Vector2(0.05f, 0.20f);
        nRT.anchorMax = new Vector2(0.95f, 0.38f);
        nRT.offsetMin = nRT.offsetMax = Vector2.zero;

        // Quantity badge góc trên phải
        GameObject qBadge = CreateUI("QuantityBadge", card.transform);
        Image qbImg = qBadge.AddComponent<Image>();
        qbImg.color  = new Color(0.25f, 0.65f, 1f);
        qbImg.sprite = MakeCircleSprite();
        RectTransform qbRT = qBadge.GetComponent<RectTransform>();
        qbRT.anchorMin = new Vector2(0.65f, 0.76f);
        qbRT.anchorMax = new Vector2(0.96f, 0.98f);
        qbRT.offsetMin = qbRT.offsetMax = Vector2.zero;

        GameObject qTxtGO = CreateUI("QuantityText", qBadge.transform);
        TMP_Text qTxt = qTxtGO.AddComponent<TextMeshProUGUI>();
        qTxt.text      = "x1";
        qTxt.fontSize  = 10f;
        qTxt.fontStyle = FontStyles.Bold;
        qTxt.color     = Color.white;
        qTxt.alignment = TextAlignmentOptions.Center;
        StretchFull(qTxtGO);

        // Dải màu dưới card
        GameObject stripe = CreateUI("BottomStripe", card.transform);
        AddImage(stripe, new Color(0.30f, 0.65f, 0.95f, 0.18f));
        RectTransform stRT = stripe.GetComponent<RectTransform>();
        stRT.anchorMin = new Vector2(0f, 0f);
        stRT.anchorMax = new Vector2(1f, 0.20f);
        stRT.offsetMin = stRT.offsetMax = Vector2.zero;

        // Gán InventoryCard script
        InventoryCard icComp = card.AddComponent<InventoryCard>();
        SerializedObject so  = new SerializedObject(icComp);
        AssignRef(so, "itemImage",    itemImg);
        AssignRef(so, "itemNameText", nameT);
        AssignRef(so, "quantityText", qTxt);
        AssignRef(so, "selectButton", btn);
        so.ApplyModifiedProperties();

        return card;
    }

    // ══════════════════════════════════════════════════════════
    // Utilities
    // ══════════════════════════════════════════════════════════
    private static GameObject CreateUI(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        if (parent != null) go.transform.SetParent(parent, false);
        return go;
    }

    private static void StretchFull(GameObject go)
    {
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    private static Image AddImage(GameObject go, Color color)
    {
        var img = go.AddComponent<Image>();
        img.color = color;
        return img;
    }

    private static void AddTMPLabel(GameObject go, string text, float size, Color? color = null)
    {
        var child = CreateUI("Label", go.transform);
        TMP_Text t = child.AddComponent<TextMeshProUGUI>();
        t.text      = text;
        t.fontSize  = size;
        t.color     = color ?? Color.white;
        t.alignment = TextAlignmentOptions.Center;
        t.fontStyle = FontStyles.Bold;
        StretchFull(child);
    }

    private static GameObject MakeRoundButton(string name, Transform parent,
        Vector2 pos, float size, Color color)
    {
        var go  = CreateUI(name, parent);
        var img = go.AddComponent<Image>();
        img.sprite = MakeCircleSprite();
        img.color  = color;
        go.AddComponent<Button>();
        var rt = go.GetComponent<RectTransform>();
        rt.sizeDelta        = new Vector2(size, size);
        rt.anchoredPosition = pos;
        return go;
    }

    private static Canvas FindOrCreateCanvas(string name)
    {
        var found = GameObject.Find(name);
        if (found != null) return found.GetComponent<Canvas>();
        var go = new GameObject(name);
        var c  = go.AddComponent<Canvas>();
        c.renderMode = RenderMode.ScreenSpaceOverlay;
        go.AddComponent<CanvasScaler>();
        go.AddComponent<GraphicRaycaster>();
        return c;
    }

    private static void SavePrefab(GameObject go, string path)
    {
        if (go == null) return;
        PrefabUtility.SaveAsPrefabAsset(go, path);
        DestroyImmediate(go);
    }

    private static void AssignRef(SerializedObject so, string field, Object obj)
    {
        var p = so.FindProperty(field);
        if (p != null) p.objectReferenceValue = obj;
        else Debug.LogWarning($"[InventoryBuilder] Field không tìm thấy: {field}");
    }

    private static void AssignRef(SerializedObject so, string field, Component c)
        => AssignRef(so, field, c as Object);

    private static Sprite MakeCircleSprite()
    {
        const int size = 128;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        float r = size * 0.5f;
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float dx = x - r + 0.5f;
            float dy = y - r + 0.5f;
            float a  = Mathf.Clamp01(r - Mathf.Sqrt(dx * dx + dy * dy));
            tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
    }

    private static Sprite MakeRoundedRectSprite()
    {
        const int size = 128, r = 20;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            int   cx = Mathf.Clamp(x, r, size - r);
            int   cy = Mathf.Clamp(y, r, size - r);
            float dx = x - cx;
            float dy = y - cy;
            float a  = Mathf.Clamp01((float)r - Mathf.Sqrt(dx * dx + dy * dy));
            tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
    }
}
#endif