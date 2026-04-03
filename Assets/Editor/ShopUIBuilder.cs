#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEditor;
using System.IO;

// ═══════════════════════════════════════════════════════════════
// ShopUIBuilder — Editor Script
// Menu: Tools > Build Shop Canvas
// Tạo: ShopCanvas, ShopItemCardPrefab, ExchangeCard Prefabs
// ═══════════════════════════════════════════════════════════════
public class ShopUIBuilder : Editor
{
    private const string PREFAB_PATH = "Assets/Prefabs/Shop";

    // ── Màu chủ đạo (phù hợp trẻ em — tươi sáng) ────────────────
    private static readonly Color CLR_BG        = new Color(0.97f, 0.94f, 0.88f); // kem nhạt
    private static readonly Color CLR_TOPBAR    = new Color(0.35f, 0.70f, 1.00f); // xanh dương
    private static readonly Color CLR_TAB_ACT   = new Color(1.00f, 1.00f, 1.00f);
    private static readonly Color CLR_TAB_INACT = new Color(0.70f, 0.88f, 1.00f);
    private static readonly Color CLR_CARD_BG   = new Color(1.00f, 1.00f, 1.00f);
    private static readonly Color CLR_BUY       = new Color(0.25f, 0.78f, 0.40f); // xanh lá
    private static readonly Color CLR_COIN      = new Color(1.00f, 0.78f, 0.10f); // vàng coin
    private static readonly Color CLR_EXCH_BG   = new Color(0.95f, 0.95f, 1.00f);
    private static readonly Color CLR_TEXT_DARK  = new Color(0.15f, 0.15f, 0.20f);

    [MenuItem("Tools/Build Shop Canvas")]
    public static void BuildShopCanvas()
    {
        if (!Directory.Exists(PREFAB_PATH)) Directory.CreateDirectory(PREFAB_PATH);

        // ── Tạo prefab card trước ─────────────────────────────────
        GameObject cardPrefab     = BuildShopItemCardPrefab();
        GameObject exchBSPrefab   = BuildExchangeCardPrefab("ExchangeCard_Bronze2Silver",
                                        "10 Đồng → 1 Bạc",
                                        new Color(0.75f, 0.45f, 0.20f));
        GameObject exchSGPrefab   = BuildExchangeCardPrefab("ExchangeCard_Silver2Gold",
                                        "20 Bạc → 1 Vàng",
                                        new Color(0.80f, 0.70f, 0.10f));

        // ── Lưu prefab ────────────────────────────────────────────
        SavePrefab(cardPrefab,   $"{PREFAB_PATH}/ShopItemCardPrefab.prefab");
        SavePrefab(exchBSPrefab, $"{PREFAB_PATH}/ExchangeCard_Bronze2Silver.prefab");
        SavePrefab(exchSGPrefab, $"{PREFAB_PATH}/ExchangeCard_Silver2Gold.prefab");

        // ── Tìm/tạo Canvas gốc ────────────────────────────────────
        var bedroomCanvas = FindOrCreateCanvas("BedroomCanvas");

        Transform existing = bedroomCanvas.transform.Find("ShopCanvas");
        if (existing) DestroyImmediate(existing.gameObject);

        // ══════════════════════════════════════════════════════════
        // SHOPCANVAS — overlay toàn màn hình
        // ══════════════════════════════════════════════════════════
        GameObject shopCanvas = CreateUI("ShopCanvas", bedroomCanvas.transform);
        Canvas sc = shopCanvas.AddComponent<Canvas>();
        sc.overrideSorting = true;
        sc.sortingOrder    = 20;
        shopCanvas.AddComponent<GraphicRaycaster>();

        Image scBg = shopCanvas.AddComponent<Image>();
        scBg.color = new Color(0f, 0f, 0f, 0.55f); // dim nền
        StretchFull(shopCanvas);
        shopCanvas.SetActive(false);

        // ── CloseButton (X) góc trên phải ────────────────────────
        GameObject closeBtn = MakeRoundButton("CloseButton", shopCanvas.transform,
                                new Vector2(-20f, -20f), 48f, new Color(0.85f, 0.30f, 0.20f));
        SetAnchor(closeBtn, new Vector2(1,1), new Vector2(1,1), new Vector2(1,1));
        AddTMPLabel(closeBtn, "✕", 22f);

        // ── TopBar ────────────────────────────────────────────────
        GameObject topBar = CreateUI("TopBar", shopCanvas.transform);
        AddImage(topBar, CLR_TOPBAR);
        RectTransform tbRT = topBar.GetComponent<RectTransform>();
        SetAnchor(topBar, new Vector2(0,1), new Vector2(1,1), new Vector2(0.5f,1));
        tbRT.anchoredPosition = Vector2.zero;
        tbRT.sizeDelta        = new Vector2(0, 90f);

        // Title
        GameObject titleGO = CreateUI("Title", topBar.transform);
        TMP_Text title = titleGO.AddComponent<TextMeshProUGUI>();
        title.text      = "Cửa Hàng";
        title.fontSize  = 26f;
        title.fontStyle = FontStyles.Bold;
        title.color     = Color.white;
        title.alignment = TextAlignmentOptions.Center;
        SetAnchor(titleGO, new Vector2(0.3f, 0.5f), new Vector2(0.7f, 1f), new Vector2(0.5f, 1f));
        titleGO.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;

        // Tab Shop
        GameObject tabShop = CreateUI("Tab_Shop", topBar.transform);
        AddImage(tabShop, CLR_TAB_ACT);
        Button tabShopBtn = tabShop.AddComponent<Button>();
        SetAnchor(tabShop, new Vector2(0.02f, 0f), new Vector2(0.48f, 0.55f), new Vector2(0.5f, 0f));
        tabShop.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 8f);
        AddTMPLabel(tabShop, "Mua sắm", 18f, CLR_TEXT_DARK);

        // Tab Exchange
        GameObject tabEx = CreateUI("Tab_Exchange", topBar.transform);
        AddImage(tabEx, CLR_TAB_INACT);
        Button tabExBtn = tabEx.AddComponent<Button>();
        SetAnchor(tabEx, new Vector2(0.52f, 0f), new Vector2(0.98f, 0.55f), new Vector2(0.5f, 0f));
        tabEx.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 8f);
        AddTMPLabel(tabEx, "Đổi Huy Chương", 16f, CLR_TEXT_DARK);

        // ── CoinDisplay ──────────────────────────────────────────
        GameObject coinDisp = CreateUI("CoinDisplay", shopCanvas.transform);
        AddImage(coinDisp, new Color(1f, 0.95f, 0.75f));
        SetAnchor(coinDisp, new Vector2(0f,1f), new Vector2(0.4f,1f), new Vector2(0f,1f));
        RectTransform cdRT = coinDisp.GetComponent<RectTransform>();
        cdRT.anchoredPosition = new Vector2(16f, -100f);
        cdRT.sizeDelta        = new Vector2(0f, 40f);

        HorizontalLayoutGroup cdHLG = coinDisp.AddComponent<HorizontalLayoutGroup>();
        cdHLG.padding = new RectOffset(10, 10, 6, 6);
        cdHLG.spacing = 6f;
        cdHLG.childControlHeight = true;
        cdHLG.childControlWidth  = false;
        cdHLG.childForceExpandWidth = false;

        // Coin Icon (vòng tròn vàng)
        GameObject coinIcon = CreateUI("CoinIcon", coinDisp.transform);
        Image coinIconImg   = coinIcon.AddComponent<Image>();
        coinIconImg.color   = CLR_COIN;
        coinIconImg.sprite  = MakeCircleSprite();
        coinIcon.GetComponent<RectTransform>().sizeDelta = new Vector2(28f, 28f);

        // Coin Text
        GameObject coinTxtGO = CreateUI("CoinText", coinDisp.transform);
        TMP_Text coinTxt = coinTxtGO.AddComponent<TextMeshProUGUI>();
        coinTxt.text      = "0";
        coinTxt.fontSize  = 20f;
        coinTxt.fontStyle = FontStyles.Bold;
        coinTxt.color     = new Color(0.60f, 0.40f, 0.00f);
        coinTxt.alignment = TextAlignmentOptions.MidlineLeft;
        coinTxtGO.GetComponent<RectTransform>().sizeDelta = new Vector2(100f, 30f);

        // ══════════════════════════════════════════════════════════
        // SHOP PANEL — ScrollView + Grid
        // ══════════════════════════════════════════════════════════
        GameObject shopPanel = CreateUI("ShopPanel", shopCanvas.transform);
        SetAnchor(shopPanel, new Vector2(0,0), new Vector2(1,1), new Vector2(0.5f,0.5f));
        shopPanel.GetComponent<RectTransform>().offsetMin = new Vector2(0, 60f);
        shopPanel.GetComponent<RectTransform>().offsetMax = new Vector2(0, -150f);

        // ScrollView
        GameObject scrollView = CreateUI("ScrollView", shopPanel.transform);
        ScrollRect sr         = scrollView.AddComponent<ScrollRect>();
        sr.horizontal         = false;
        AddImage(scrollView, Color.clear);
        StretchFull(scrollView);
        scrollView.GetComponent<RectTransform>().offsetMin = new Vector2(10, 50f);
        scrollView.GetComponent<RectTransform>().offsetMax = new Vector2(-10, 0);

        GameObject viewport = CreateUI("Viewport", scrollView.transform);
        viewport.AddComponent<Mask>().showMaskGraphic = false;
        AddImage(viewport, Color.clear);
        StretchFull(viewport);
        sr.viewport = viewport.GetComponent<RectTransform>();

        // Grid Content
        GameObject content   = CreateUI("Content", viewport.transform);
        GridLayoutGroup grid = content.AddComponent<GridLayoutGroup>();
        grid.cellSize        = new Vector2(200f, 230f);
        grid.spacing         = new Vector2(16f, 16f);
        grid.padding         = new RectOffset(20, 20, 16, 16);
        grid.constraint      = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 3;

        ContentSizeFitter csf = content.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        RectTransform contentRT = content.GetComponent<RectTransform>();
        contentRT.anchorMin = new Vector2(0, 1);
        contentRT.anchorMax = new Vector2(1, 1);
        contentRT.pivot     = new Vector2(0.5f, 1f);
        contentRT.anchoredPosition = Vector2.zero;
        sr.content = contentRT;

        // Pagination bar (dưới shopPanel)
        GameObject pageBar = CreateUI("PageBar", shopPanel.transform);
        HorizontalLayoutGroup pHLG = pageBar.AddComponent<HorizontalLayoutGroup>();
        pHLG.spacing = 20f;
        pHLG.childAlignment = TextAnchor.MiddleCenter;
        pHLG.childControlWidth  = false;
        pHLG.childControlHeight = false;
        pHLG.childForceExpandWidth = false;
        SetAnchor(pageBar, new Vector2(0,0), new Vector2(1,0), new Vector2(0.5f,0));
        pageBar.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 48f);
        pageBar.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;

        GameObject prevBtn = MakeRoundButton("PrevButton", pageBar.transform, Vector2.zero, 40f,
                                CLR_TOPBAR);
        prevBtn.GetComponent<RectTransform>().sizeDelta = new Vector2(40f, 40f);
        AddTMPLabel(prevBtn, "◀", 18f);

        GameObject pageInd = CreateUI("PageIndicator", pageBar.transform);
        TMP_Text pageIndTxt = pageInd.AddComponent<TextMeshProUGUI>();
        pageIndTxt.text      = "1/1";
        pageIndTxt.fontSize  = 18f;
        pageIndTxt.color     = CLR_TEXT_DARK;
        pageIndTxt.alignment = TextAlignmentOptions.Center;
        pageInd.GetComponent<RectTransform>().sizeDelta = new Vector2(80f, 40f);

        GameObject nextBtn = MakeRoundButton("NextButton", pageBar.transform, Vector2.zero, 40f,
                                CLR_TOPBAR);
        nextBtn.GetComponent<RectTransform>().sizeDelta = new Vector2(40f, 40f);
        AddTMPLabel(nextBtn, "▶", 18f);

        // ══════════════════════════════════════════════════════════
        // EXCHANGE PANEL
        // ══════════════════════════════════════════════════════════
        GameObject exPanel = CreateUI("ExchangePanel", shopCanvas.transform);
        AddImage(exPanel, CLR_EXCH_BG);
        SetAnchor(exPanel, new Vector2(0,0), new Vector2(1,1), new Vector2(0.5f,0.5f));
        exPanel.GetComponent<RectTransform>().offsetMin = new Vector2(0, 60f);
        exPanel.GetComponent<RectTransform>().offsetMax = new Vector2(0, -150f);
        exPanel.SetActive(false);

        // Medal count row
        GameObject medalRow = CreateUI("MedalCounts", exPanel.transform);
        HorizontalLayoutGroup mHLG = medalRow.AddComponent<HorizontalLayoutGroup>();
        mHLG.spacing = 24f;
        mHLG.childAlignment = TextAnchor.MiddleCenter;
        mHLG.childControlWidth  = false;
        mHLG.childControlHeight = false;
        mHLG.childForceExpandWidth = false;
        SetAnchor(medalRow, new Vector2(0f,1f), new Vector2(1f,1f), new Vector2(0.5f,1f));
        medalRow.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -20f);
        medalRow.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 50f);

        BuildMedalCount("BronzeCount", "Đồng: 0", new Color(0.75f, 0.45f, 0.20f), medalRow.transform);
        BuildMedalCount("SilverCount", "Bạc: 0",  new Color(0.70f, 0.70f, 0.70f), medalRow.transform);
        BuildMedalCount("GoldCount",   "Vàng: 0", new Color(0.90f, 0.75f, 0.10f), medalRow.transform);

        // Exchange cards (2 cái nằm ngang)
        GameObject exchRow = CreateUI("ExchangeRow", exPanel.transform);
        HorizontalLayoutGroup eHLG = exchRow.AddComponent<HorizontalLayoutGroup>();
        eHLG.spacing    = 30f;
        eHLG.childAlignment = TextAnchor.MiddleCenter;
        eHLG.childControlWidth  = false;
        eHLG.childControlHeight = false;
        eHLG.childForceExpandWidth = false;
        StretchFull(exchRow);
        RectTransform exRT = exchRow.GetComponent<RectTransform>();
        exRT.offsetMin = new Vector2(0, 80f);
        exRT.offsetMax = new Vector2(0, 0);
        // Place exchange card prefabs reference nodes
        GameObject ecBS = CreateUI("ExchangeCard_Bronze2Silver", exchRow.transform);
        ecBS.GetComponent<RectTransform>().sizeDelta = new Vector2(260f, 320f);
        AddImage(ecBS, new Color(0.95f, 0.88f, 0.80f));
        // Nút mở popup
        Button btnBS = ecBS.AddComponent<Button>();
        BuildExchangeMiniCard(ecBS, "10 Đồng → 1 Bạc",
            new Color(0.75f, 0.45f, 0.20f), new Color(0.75f, 0.75f, 0.75f), "bronze2SilverBtn");

        GameObject ecSG = CreateUI("ExchangeCard_Silver2Gold", exchRow.transform);
        ecSG.GetComponent<RectTransform>().sizeDelta = new Vector2(260f, 320f);
        AddImage(ecSG, new Color(0.95f, 0.95f, 0.80f));
        Button btnSG = ecSG.AddComponent<Button>();
        BuildExchangeMiniCard(ecSG, "20 Bạc → 1 Vàng",
            new Color(0.75f, 0.75f, 0.75f), new Color(0.90f, 0.75f, 0.10f), "silver2GoldBtn");

        // ── Popups đổi huy chương (ẩn mặc định) ──────────────────
        BuildExchangePopup("ExchangePopup_BS", shopCanvas.transform,
            "10 Huy Chương Đồng", "→ 1 Huy Chương Bạc");
        BuildExchangePopup("ExchangePopup_SG", shopCanvas.transform,
            "20 Huy Chương Bạc", "→ 1 Huy Chương Vàng");

        // ── Gán ShopManager ───────────────────────────────────────
        ShopManager sm = shopCanvas.AddComponent<ShopManager>();
        SerializedObject so = new SerializedObject(sm);

        AssignRef(so, "shopCanvas",           shopCanvas);
        AssignRef(so, "shopPanel",            shopPanel);
        AssignRef(so, "exchangePanel",        exPanel);
        AssignRef(so, "tabShopBtn",           tabShopBtn);
        AssignRef(so, "tabExchangeBtn",       tabExBtn);
        AssignRef(so, "coinText",             coinTxtGO.GetComponent<TMP_Text>());
        AssignRef(so, "itemGrid",             content.GetComponent<Transform>());
        AssignRef(so, "shopItemCardPrefab",   AssetDatabase.LoadAssetAtPath<GameObject>(
                                                  $"{PREFAB_PATH}/ShopItemCardPrefab.prefab"));
        AssignRef(so, "prevPageBtn",          prevBtn.GetComponent<Button>());
        AssignRef(so, "nextPageBtn",          nextBtn.GetComponent<Button>());
        AssignRef(so, "pageIndicatorText",    pageIndTxt);
        AssignRef(so, "closeBtn",             closeBtn.GetComponent<Button>());
        AssignRef(so, "bronze2SilverBtn",     ecBS.GetComponent<Button>());
        AssignRef(so, "silver2GoldBtn",       ecSG.GetComponent<Button>());

        // Medal count texts
        AssignRef(so, "bronzeCountText",
            medalRow.transform.Find("BronzeCount/CountText")?.GetComponent<TMP_Text>());
        AssignRef(so, "silverCountText",
            medalRow.transform.Find("SilverCount/CountText")?.GetComponent<TMP_Text>());
        AssignRef(so, "goldCountText",
            medalRow.transform.Find("GoldCount/CountText")?.GetComponent<TMP_Text>());

        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(shopCanvas);
        Selection.activeGameObject = shopCanvas;
        AssetDatabase.SaveAssets();
        Debug.Log("[ShopUIBuilder] ✅ ShopCanvas đã tạo xong!");
    }

    // ══════════════════════════════════════════════════════════
    // Build ShopItemCardPrefab
    // ══════════════════════════════════════════════════════════
    private static GameObject BuildShopItemCardPrefab()
    {
        GameObject card = CreateUI("ShopItemCardPrefab", null);
        card.GetComponent<RectTransform>().sizeDelta = new Vector2(200f, 230f);

        // Nền card bo góc
        Image cardBg = card.AddComponent<Image>();
        cardBg.color  = CLR_CARD_BG;
        cardBg.sprite = MakeRoundedRectSprite();

        // Shadow nhẹ (Image phía sau)
        GameObject shadow = CreateUI("Shadow", card.transform);
        Image shadowImg = shadow.AddComponent<Image>();
        shadowImg.color = new Color(0f, 0f, 0f, 0.08f);
        RectTransform sRT = shadow.GetComponent<RectTransform>();
        sRT.anchorMin = Vector2.zero; sRT.anchorMax = Vector2.one;
        sRT.offsetMin = new Vector2(-2, -4); sRT.offsetMax = new Vector2(2, 0);

        // ItemImage (80% width, trên cùng)
        GameObject imgGO = CreateUI("ItemImage", card.transform);
        Image itemImg    = imgGO.AddComponent<Image>();
        itemImg.color    = new Color(0.90f, 0.90f, 0.90f);
        itemImg.preserveAspect = true;
        RectTransform iRT = imgGO.GetComponent<RectTransform>();
        iRT.anchorMin = new Vector2(0.10f, 0.40f);
        iRT.anchorMax = new Vector2(0.90f, 0.95f);
        iRT.offsetMin = iRT.offsetMax = Vector2.zero;

        // ItemName
        GameObject nameGO = CreateUI("ItemName", card.transform);
        TMP_Text nameT    = nameGO.AddComponent<TextMeshProUGUI>();
        nameT.text         = "Tên Sản Phẩm";
        nameT.fontSize     = 14f;
        nameT.fontStyle    = FontStyles.Bold;
        nameT.color        = CLR_TEXT_DARK;
        nameT.alignment    = TextAlignmentOptions.Center;
        nameT.overflowMode = TextOverflowModes.Ellipsis;
        RectTransform nRT  = nameGO.GetComponent<RectTransform>();
        nRT.anchorMin = new Vector2(0.05f, 0.28f);
        nRT.anchorMax = new Vector2(0.95f, 0.42f);
        nRT.offsetMin = nRT.offsetMax = Vector2.zero;

        // PriceText
        GameObject priceGO = CreateUI("PriceText", card.transform);
        TMP_Text priceT    = priceGO.AddComponent<TextMeshProUGUI>();
        priceT.text         = "99 xu";
        priceT.fontSize     = 13f;
        priceT.color        = new Color(0.65f, 0.40f, 0f);
        priceT.alignment    = TextAlignmentOptions.Center;
        RectTransform pRT   = priceGO.GetComponent<RectTransform>();
        pRT.anchorMin = new Vector2(0.05f, 0.18f);
        pRT.anchorMax = new Vector2(0.95f, 0.29f);
        pRT.offsetMin = pRT.offsetMax = Vector2.zero;

        // BuyButton
        GameObject btnGO = CreateUI("BuyButton", card.transform);
        Image btnImg     = btnGO.AddComponent<Image>();
        btnImg.color     = CLR_BUY;
        btnImg.sprite    = MakeRoundedRectSprite();
        Button buyBtn    = btnGO.AddComponent<Button>();
        RectTransform btnRT = btnGO.GetComponent<RectTransform>();
        btnRT.anchorMin = new Vector2(0.10f, 0.04f);
        btnRT.anchorMax = new Vector2(0.90f, 0.17f);
        btnRT.offsetMin = btnRT.offsetMax = Vector2.zero;
        AddTMPLabel(btnGO, "Mua", 14f);

        // Tooltip "Không đủ xu"
        GameObject tooltip = CreateUI("Tooltip", card.transform);
        AddImage(tooltip, new Color(0.90f, 0.20f, 0.20f, 0.92f));
        RectTransform ttRT = tooltip.GetComponent<RectTransform>();
        ttRT.anchorMin = new Vector2(0f, 0.17f);
        ttRT.anchorMax = new Vector2(1f, 0.30f);
        ttRT.offsetMin = ttRT.offsetMax = Vector2.zero;
        AddTMPLabel(tooltip, "Không đủ xu!", 12f, Color.white);
        tooltip.SetActive(false);

        // Gán ShopItemCard script
        ShopItemCard sic = card.AddComponent<ShopItemCard>();
        SerializedObject so = new SerializedObject(sic);
        AssignRef(so, "itemImage",      itemImg);
        AssignRef(so, "itemNameText",   nameT);
        AssignRef(so, "priceText",      priceT);
        AssignRef(so, "buyButton",      buyBtn);
        AssignRef(so, "buyButtonImage", btnImg);
        AssignRef(so, "buyButtonLabel", btnGO.GetComponentInChildren<TMP_Text>());
        AssignRef(so, "tooltipObj",     tooltip);
        so.ApplyModifiedProperties();

        return card;
    }

    // ── Build Exchange Card Prefab ────────────────────────────────
    private static GameObject BuildExchangeCardPrefab(string name, string label, Color medalColor)
    {
        GameObject card = CreateUI(name, null);
        card.GetComponent<RectTransform>().sizeDelta = new Vector2(260f, 320f);
        AddImage(card, new Color(0.95f, 0.93f, 0.88f));

        // Medal image placeholder
        GameObject medalGO = CreateUI("MedalImage", card.transform);
        Image medalImg     = medalGO.AddComponent<Image>();
        medalImg.color     = medalColor;
        medalImg.sprite    = MakeCircleSprite();
        RectTransform mRT  = medalGO.GetComponent<RectTransform>();
        mRT.anchorMin = new Vector2(0.20f, 0.50f);
        mRT.anchorMax = new Vector2(0.80f, 0.95f);
        mRT.offsetMin = mRT.offsetMax = Vector2.zero;

        // Text mô tả
        GameObject txtGO = CreateUI("ExchangeText", card.transform);
        TMP_Text txt     = txtGO.AddComponent<TextMeshProUGUI>();
        txt.text          = label;
        txt.fontSize      = 16f;
        txt.fontStyle     = FontStyles.Bold;
        txt.color         = CLR_TEXT_DARK;
        txt.alignment     = TextAlignmentOptions.Center;
        RectTransform tRT = txtGO.GetComponent<RectTransform>();
        tRT.anchorMin = new Vector2(0.05f, 0.30f);
        tRT.anchorMax = new Vector2(0.95f, 0.52f);
        tRT.offsetMin = tRT.offsetMax = Vector2.zero;

        // Exchange Button
        GameObject exBtn = CreateUI("ExchangeButton", card.transform);
        AddImage(exBtn, new Color(0.20f, 0.60f, 0.95f));
        exBtn.AddComponent<Button>();
        RectTransform ebRT = exBtn.GetComponent<RectTransform>();
        ebRT.anchorMin = new Vector2(0.10f, 0.10f);
        ebRT.anchorMax = new Vector2(0.90f, 0.28f);
        ebRT.offsetMin = ebRT.offsetMax = Vector2.zero;
        AddTMPLabel(exBtn, "Đổi ngay", 15f, Color.white);

        return card;
    }

    // ── Mini card trong ExchangePanel ────────────────────────────
    private static void BuildExchangeMiniCard(GameObject parent,
        string label, Color fromColor, Color toColor, string btnId)
    {
        // Icon "from" medal
        GameObject fromGO = CreateUI("FromMedal", parent.transform);
        Image fromImg     = fromGO.AddComponent<Image>();
        fromImg.color     = fromColor;
        fromImg.sprite    = MakeCircleSprite();
        RectTransform fRT = fromGO.GetComponent<RectTransform>();
        fRT.anchorMin = new Vector2(0.10f, 0.65f);
        fRT.anchorMax = new Vector2(0.45f, 0.95f);
        fRT.offsetMin = fRT.offsetMax = Vector2.zero;

        // Arrow
        GameObject arrGO = CreateUI("Arrow", parent.transform);
        TMP_Text arrT    = arrGO.AddComponent<TextMeshProUGUI>();
        arrT.text         = "→";
        arrT.fontSize     = 22f;
        arrT.color        = CLR_TEXT_DARK;
        arrT.alignment    = TextAlignmentOptions.Center;
        RectTransform aRT = arrGO.GetComponent<RectTransform>();
        aRT.anchorMin = new Vector2(0.40f, 0.72f);
        aRT.anchorMax = new Vector2(0.60f, 0.88f);
        aRT.offsetMin = aRT.offsetMax = Vector2.zero;

        // Icon "to" medal
        GameObject toGO = CreateUI("ToMedal", parent.transform);
        Image toImg     = toGO.AddComponent<Image>();
        toImg.color     = toColor;
        toImg.sprite    = MakeCircleSprite();
        RectTransform tRT2 = toGO.GetComponent<RectTransform>();
        tRT2.anchorMin = new Vector2(0.55f, 0.65f);
        tRT2.anchorMax = new Vector2(0.90f, 0.95f);
        tRT2.offsetMin = tRT2.offsetMax = Vector2.zero;

        // Label
        GameObject lGO = CreateUI("ExchangeLabel", parent.transform);
        TMP_Text lT    = lGO.AddComponent<TextMeshProUGUI>();
        lT.text         = label;
        lT.fontSize     = 14f;
        lT.fontStyle    = FontStyles.Bold;
        lT.color        = CLR_TEXT_DARK;
        lT.alignment    = TextAlignmentOptions.Center;
        RectTransform lRT2 = lGO.GetComponent<RectTransform>();
        lRT2.anchorMin = new Vector2(0.05f, 0.45f);
        lRT2.anchorMax = new Vector2(0.95f, 0.64f);
        lRT2.offsetMin = lRT2.offsetMax = Vector2.zero;
    }

    // ── Popup đổi huy chương ─────────────────────────────────────
    private static void BuildExchangePopup(string name, Transform parent, string needText, string getText)
    {
        GameObject popup = CreateUI(name, parent);
        AddImage(popup, new Color(0f, 0f, 0f, 0.6f));
        StretchFull(popup);
        popup.SetActive(false);

        GameObject box = CreateUI("Box", popup.transform);
        AddImage(box, new Color(0.98f, 0.96f, 0.90f));
        RectTransform bRT = box.GetComponent<RectTransform>();
        bRT.anchorMin = new Vector2(0.15f, 0.3f);
        bRT.anchorMax = new Vector2(0.85f, 0.7f);
        bRT.offsetMin = bRT.offsetMax = Vector2.zero;

        // Medal placeholder ảnh
        GameObject medImg = CreateUI("MedalImage", box.transform);
        AddImage(medImg, new Color(0.85f, 0.72f, 0.15f));
        RectTransform miRT = medImg.GetComponent<RectTransform>();
        miRT.anchorMin = new Vector2(0.30f, 0.60f);
        miRT.anchorMax = new Vector2(0.70f, 0.95f);
        miRT.offsetMin = miRT.offsetMax = Vector2.zero;

        // Need text
        GameObject needGO = CreateUI("NeedText", box.transform);
        TMP_Text needT    = needGO.AddComponent<TextMeshProUGUI>();
        needT.text         = needText;
        needT.fontSize     = 16f;
        needT.color        = CLR_TEXT_DARK;
        needT.alignment    = TextAlignmentOptions.Center;
        SetAnchor(needGO, new Vector2(0.05f, 0.40f), new Vector2(0.95f, 0.60f), new Vector2(0.5f, 0.5f));

        // Get text
        GameObject getGO = CreateUI("GetText", box.transform);
        TMP_Text getT    = getGO.AddComponent<TextMeshProUGUI>();
        getT.text         = getText;
        getT.fontSize     = 18f;
        getT.fontStyle    = FontStyles.Bold;
        getT.color        = new Color(0.10f, 0.55f, 0.20f);
        getT.alignment    = TextAlignmentOptions.Center;
        SetAnchor(getGO, new Vector2(0.05f, 0.24f), new Vector2(0.95f, 0.42f), new Vector2(0.5f, 0.5f));

        // Confirm button
        GameObject confBtn = CreateUI("ConfirmBtn", box.transform);
        AddImage(confBtn, CLR_BUY);
        confBtn.AddComponent<Button>();
        SetAnchor(confBtn, new Vector2(0.10f, 0.04f), new Vector2(0.90f, 0.22f), new Vector2(0.5f, 0f));
        AddTMPLabel(confBtn, "Xác nhận đổi", 15f, Color.white);

        // Close button X
        GameObject closeBtn = MakeRoundButton("CloseBtn", box.transform, Vector2.zero, 32f,
                                  new Color(0.85f, 0.30f, 0.20f));
        SetAnchor(closeBtn, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f));
        closeBtn.GetComponent<RectTransform>().anchoredPosition = new Vector2(-4f, -4f);
        AddTMPLabel(closeBtn, "✕", 14f);
    }

    // ── Medal count display ──────────────────────────────────────
    private static void BuildMedalCount(string id, string label, Color color, Transform parent)
    {
        GameObject go = CreateUI(id, parent);
        VerticalLayoutGroup vlg = go.AddComponent<VerticalLayoutGroup>();
        vlg.childAlignment = TextAnchor.MiddleCenter;
        vlg.spacing = 4f;
        vlg.childControlWidth = false;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth = false;
        go.GetComponent<RectTransform>().sizeDelta = new Vector2(100f, 50f);

        // Circle medal
        GameObject circle = CreateUI("Circle", go.transform);
        Image cImg        = circle.AddComponent<Image>();
        cImg.color        = color;
        cImg.sprite       = MakeCircleSprite();
        circle.GetComponent<RectTransform>().sizeDelta = new Vector2(30f, 30f);

        // Count text
        GameObject countGO = CreateUI("CountText", go.transform);
        TMP_Text countT    = countGO.AddComponent<TextMeshProUGUI>();
        countT.text         = "0";
        countT.fontSize     = 15f;
        countT.fontStyle    = FontStyles.Bold;
        countT.color        = color;
        countT.alignment    = TextAlignmentOptions.Center;
        countGO.GetComponent<RectTransform>().sizeDelta = new Vector2(80f, 22f);
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

    private static void SetAnchor(GameObject go, Vector2 min, Vector2 max, Vector2 pivot)
    {
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = min; rt.anchorMax = max; rt.pivot = pivot;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    private static Image AddImage(GameObject go, Color color)
    {
        var img = go.AddComponent<Image>();
        img.color = color;
        return img;
    }

    private static void AddTMPLabel(GameObject go, string text, float size,
        Color? color = null)
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
        var go   = CreateUI(name, parent);
        var img  = go.AddComponent<Image>();
        img.sprite = MakeCircleSprite();
        img.color  = color;
        go.AddComponent<Button>();
        var rt   = go.GetComponent<RectTransform>();
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
        else Debug.LogWarning($"[ShopBuilder] Field không tìm thấy: {field}");
    }

    private static void AssignRef(SerializedObject so, string field, Component comp)
        => AssignRef(so, field, comp as Object);

    private static Sprite MakeCircleSprite()
    {
        const int size = 128;
        var tex  = new Texture2D(size, size, TextureFormat.RGBA32, false);
        float r  = size * 0.5f;
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float dx    = x - r + 0.5f;
            float dy    = y - r + 0.5f;
            float alpha = Mathf.Clamp01(r - Mathf.Sqrt(dx * dx + dy * dy));
            tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
    }

    private static Sprite MakeRoundedRectSprite()
    {
        const int size = 128;
        const int r    = 20;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            // Áp dụng góc bo tròn
            int cx = Mathf.Clamp(x, r, size - r);
            int cy = Mathf.Clamp(y, r, size - r);
            float dx    = x - cx;
            float dy    = y - cy;
            float alpha = Mathf.Clamp01((float)r - Mathf.Sqrt(dx*dx + dy*dy));
            tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
    }
}
#endif