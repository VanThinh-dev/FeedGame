// ============================================================
//  AuthUIBuilder.cs  —  Assets/Editor/AuthUIBuilder.cs
//
//  FIX so với version trước:
//   1. NullReferenceException ở MiniStatCard.Find("V")
//      → Dùng biến trực tiếp thay vì Find()
//   2. Menu mới: "Rebuild Auth UI (giữ canvas cũ)"
//      → Chỉ xóa các Panel bên trong AuthCanvas, giữ nguyên canvas
//      → Nếu AuthCanvas chưa có thì tạo mới
//   3. Menu cũ: "Build Auth UI (tạo mới hoàn toàn)" vẫn còn
// ============================================================
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;

public static class AuthUIBuilder
{
    // ── Glassmorphism Theme ──────────────────────────────────────────────────
    static readonly Color GLASS_BG      = new Color(1f,   1f,   1f,   0.18f);
    static readonly Color GLASS_BORDER  = new Color(1f,   1f,   1f,   0.40f);
    static readonly Color GLASS_INPUT   = new Color(1f,   1f,   1f,   0.22f);
    static readonly Color GLASS_INPUT_B = new Color(1f,   1f,   1f,   0.55f);
    static readonly Color OVERLAY       = new Color(0.08f,0.05f,0.12f,0.35f);
    static readonly Color ACCENT        = new Color(1.00f,0.55f,0.30f,1f);
    static readonly Color ACCENT2       = new Color(0.95f,0.40f,0.55f,1f);
    static readonly Color TEXT_WHITE    = new Color(1f,   1f,   1f,   0.95f);
    static readonly Color TEXT_HINT     = new Color(1f,   1f,   1f,   0.55f);
    static readonly Color ERROR_COLOR   = new Color(1.00f,0.35f,0.35f,1f);

    // ── Layout ───────────────────────────────────────────────────────────────
    const float REF_W   = 720f;
    const float REF_H   = 1280f;
    const float CARD_W  = 600f;
    const float PADDING = 36f;
    const float GAP     = 18f;
    const float INPUT_H = 62f;
    const float BTN_H   = 58f;
    const float FONT_TITLE = 38f;
    const float FONT_INPUT = 18f;
    const float FONT_BTN   = 18f;
    const float FONT_ERR   = 13f;

    // ════════════════════════════════════════════════════════════════════════
    // MENU: Tạo mới hoàn toàn
    // ════════════════════════════════════════════════════════════════════════
    [MenuItem("Tools/Build Auth UI (tạo mới hoàn toàn)")]
    public static void BuildAll()
    {
        Undo.SetCurrentGroupName("Build Auth UI");
        int grp = Undo.GetCurrentGroup();

        Kill("BackgroundCanvas");
        Kill("AuthCanvas");

        EnsureEventSystem();
        EnsureFirebaseRoot();
        MakeBackgroundCanvas();

        var authGO = MakeAuthCanvas();
        BuildPanels(authGO);

        Undo.CollapseUndoOperations(grp);
        EditorUtility.DisplayDialog("Auth UI Builder",
            "✅ Tạo mới hoàn toàn!\n\nNhớ kéo ảnh nền vào BackgroundCanvas → BG → Source Image", "OK");
    }

    // ════════════════════════════════════════════════════════════════════════
    // MENU: Rebuild giao diện, giữ canvas cũ
    // ════════════════════════════════════════════════════════════════════════
    [MenuItem("Tools/Rebuild Auth UI (giữ canvas, chỉ rebuild UI)")]
    public static void RebuildOnly()
    {
        Undo.SetCurrentGroupName("Rebuild Auth UI");
        int grp = Undo.GetCurrentGroup();

        // Tìm AuthCanvas cũ — nếu không có thì tạo mới
        var authGO = GameObject.Find("AuthCanvas");
        if (authGO == null)
        {
            Debug.Log("[AuthUIBuilder] Không tìm thấy AuthCanvas cũ → tạo mới.");
            EnsureEventSystem();
            EnsureFirebaseRoot();
            MakeBackgroundCanvas();
            authGO = MakeAuthCanvas();
        }
        else
        {
            Debug.Log("[AuthUIBuilder] Tìm thấy AuthCanvas cũ → chỉ rebuild panels bên trong.");

            // Xóa tất cả children cũ (các panels)
            // Giữ nguyên Canvas + CanvasScaler + GraphicRaycaster + AuthUIManager
            var children = new System.Collections.Generic.List<GameObject>();
            for (int i = 0; i < authGO.transform.childCount; i++)
                children.Add(authGO.transform.GetChild(i).gameObject);
            foreach (var c in children)
                Undo.DestroyObjectImmediate(c);

            // Xóa AuthUIManager cũ rồi tạo lại (để wire lại refs)
            var oldMgr = authGO.GetComponent<AuthUIManager>();
            if (oldMgr != null) Undo.DestroyObjectImmediate(oldMgr);
        }

        BuildPanels(authGO);

        Undo.CollapseUndoOperations(grp);
        EditorUtility.DisplayDialog("Auth UI Builder",
            "✅ Rebuild xong! Canvas cũ được giữ nguyên, chỉ panels bên trong được rebuild.", "OK");
    }

    // ════════════════════════════════════════════════════════════════════════
    // BUILD PANELS (dùng chung cho cả 2 menu)
    // ════════════════════════════════════════════════════════════════════════
    static void BuildPanels(GameObject authGO)
    {
        var uiMgr = authGO.AddComponent<AuthUIManager>();

        var loginP = BuildLogin(authGO, uiMgr);
        var regP   = BuildRegister(authGO, uiMgr);
        var loadP  = BuildLoading(authGO);
        var mainP  = BuildMainMenu(authGO, uiMgr);

        var so = new SerializedObject(uiMgr);
        so.FindProperty("loginPanel").objectReferenceValue    = loginP;
        so.FindProperty("registerPanel").objectReferenceValue = regP;
        so.FindProperty("loadingPanel").objectReferenceValue  = loadP;
        so.FindProperty("mainMenuPanel").objectReferenceValue = mainP;
        so.ApplyModifiedProperties();

        regP.SetActive(false);
        loadP.SetActive(false);
        mainP.SetActive(false);
    }

    // ════════════════════════════════════════════════════════════════════════
    // SCENE SETUP
    // ════════════════════════════════════════════════════════════════════════
    static void Kill(string n) { var g = GameObject.Find(n); if (g) Undo.DestroyObjectImmediate(g); }

    static void EnsureEventSystem()
    {
        if (Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>()) return;
        var es = new GameObject("EventSystem");
        Undo.RegisterCreatedObjectUndo(es, "ES");
        es.AddComponent<UnityEngine.EventSystems.EventSystem>();
        es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
    }

    static void EnsureFirebaseRoot()
    {
        var root = GameObject.Find("FirebaseRoot");
        if (root == null)
        {
            root = new GameObject("FirebaseRoot");
            Undo.RegisterCreatedObjectUndo(root, "FR");
        }
        if (!root.GetComponent<FirebaseManager>()) Undo.AddComponent<FirebaseManager>(root);
        if (!root.GetComponent<AuthManager>())     Undo.AddComponent<AuthManager>(root);
    }

    static void MakeBackgroundCanvas()
    {
        // Nếu đã có rồi thì không tạo lại
        if (GameObject.Find("BackgroundCanvas") != null) return;

        var go = new GameObject("BackgroundCanvas");
        Undo.RegisterCreatedObjectUndo(go, "BG");
        var c = go.AddComponent<Canvas>();
        c.renderMode   = RenderMode.ScreenSpaceOverlay;
        c.sortingOrder = 0;
        Scaler(go);

        var bg  = UI("BG", go.transform);
        Stretch(bg.GetComponent<RectTransform>());
        var img = bg.AddComponent<Image>();
        img.color          = Color.white;
        img.preserveAspect = false;
        img.raycastTarget  = false;

        var ov  = UI("Overlay", go.transform);
        Stretch(ov.GetComponent<RectTransform>());
        var ovi = ov.AddComponent<Image>();
        ovi.color         = OVERLAY;
        ovi.raycastTarget = false;
    }

    static GameObject MakeAuthCanvas()
    {
        var go = new GameObject("AuthCanvas");
        Undo.RegisterCreatedObjectUndo(go, "AC");
        var c = go.AddComponent<Canvas>();
        c.renderMode   = RenderMode.ScreenSpaceOverlay;
        c.sortingOrder = 10;
        Scaler(go);
        go.AddComponent<GraphicRaycaster>();
        return go;
    }

    static void Scaler(GameObject go)
    {
        var cs = go.AddComponent<CanvasScaler>();
        cs.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        cs.referenceResolution = new Vector2(REF_W, REF_H);
        cs.matchWidthOrHeight  = 0.5f;
    }

    // ════════════════════════════════════════════════════════════════════════
    // LOGIN PANEL
    // ════════════════════════════════════════════════════════════════════════
    static GameObject BuildLogin(GameObject canvas, AuthUIManager uiMgr)
    {
        var panel = Panel("LoginPanel", canvas.transform);
        float h   = PADDING + 56+GAP + 44+12+GAP + INPUT_H+GAP + INPUT_H+GAP + 24+GAP + BTN_H+GAP + BTN_H + PADDING;
        var card  = GlassCard("Card", panel.transform, CARD_W, h);
        float y   = -PADDING;

        var catLbl = EmojiLabel("CatIcon", card.transform, "🐱", 36f);
        Pin(catLbl, (CARD_W-56f)/2f, y, 56f, 56f); y -= 56f+GAP;

        var title = Txt("Title", card.transform, "Xin Chào!", FONT_TITLE, TEXT_WHITE, true);
        title.GetComponent<TMP_Text>().alignment = TextAlignmentOptions.Center | TextAlignmentOptions.Midline;
        Pin(title, 0, y, CARD_W, 44f); y -= 44f+12f+GAP;

        var emailIn = GlassInput("EmailInput", card.transform, "Email");
        emailIn.contentType = TMP_InputField.ContentType.EmailAddress;
        Pin(emailIn.gameObject, PADDING, y, CARD_W-PADDING*2, INPUT_H); y -= INPUT_H+GAP;

        var passIn = GlassInput("PasswordInput", card.transform, "Mật khẩu");
        passIn.contentType = TMP_InputField.ContentType.Password;
        Pin(passIn.gameObject, PADDING, y, CARD_W-PADDING*2, INPUT_H); y -= INPUT_H+GAP;

        var errGO = Txt("ErrorText", card.transform, "", FONT_ERR, ERROR_COLOR);
        errGO.GetComponent<TMP_Text>().alignment = TextAlignmentOptions.Center | TextAlignmentOptions.Midline;
        Pin(errGO, PADDING, y, CARD_W-PADDING*2, 24f);
        errGO.SetActive(false); y -= 24f+GAP;

        var loginBtn = GlassButton("LoginButton", card.transform, "ĐĂNG NHẬP", ACCENT);
        Pin(loginBtn.gameObject, PADDING, y, CARD_W-PADDING*2, BTN_H); y -= BTN_H+GAP;

        var regBtn = GhostButton("GoToRegisterButton", card.transform, "Chưa có tài khoản? Đăng ký");
        Pin(regBtn.gameObject, PADDING, y, CARD_W-PADDING*2, BTN_H);

        var so = new SerializedObject(uiMgr);
        so.FindProperty("loginEmailInput").objectReferenceValue    = emailIn;
        so.FindProperty("loginPasswordInput").objectReferenceValue = passIn;
        so.FindProperty("loginButton").objectReferenceValue        = loginBtn;
        so.FindProperty("goToRegisterButton").objectReferenceValue = regBtn;
        so.FindProperty("loginErrorText").objectReferenceValue     = errGO.GetComponent<TMP_Text>();
        so.ApplyModifiedProperties();

        Click(loginBtn, uiMgr, "OnLoginButtonClicked");
        Click(regBtn,   uiMgr, "OnGoToRegisterClicked");
        return panel;
    }

    // ════════════════════════════════════════════════════════════════════════
    // REGISTER PANEL
    // ════════════════════════════════════════════════════════════════════════
    static GameObject BuildRegister(GameObject canvas, AuthUIManager uiMgr)
    {
        var panel = Panel("RegisterPanel", canvas.transform);
        float h   = PADDING + 56+GAP + 44+12+GAP + (INPUT_H+GAP)*4 + 24+GAP + BTN_H+GAP + BTN_H + PADDING;
        var card  = GlassCard("Card", panel.transform, CARD_W, h);
        float y   = -PADDING;

        var catLbl = EmojiLabel("CatIcon", card.transform, "🐾", 36f);
        Pin(catLbl, (CARD_W-56f)/2f, y, 56f, 56f); y -= 56f+GAP;

        var title = Txt("Title", card.transform, "Tạo Tài Khoản", FONT_TITLE, TEXT_WHITE, true);
        title.GetComponent<TMP_Text>().alignment = TextAlignmentOptions.Center | TextAlignmentOptions.Midline;
        Pin(title, 0, y, CARD_W, 44f); y -= 44f+12f+GAP;

        var nameIn = GlassInput("NameInput", card.transform, "Tên của bạn");
        nameIn.contentType = TMP_InputField.ContentType.Standard;
        Pin(nameIn.gameObject, PADDING, y, CARD_W-PADDING*2, INPUT_H); y -= INPUT_H+GAP;

        var emailIn = GlassInput("EmailInput", card.transform, "Email");
        emailIn.contentType = TMP_InputField.ContentType.EmailAddress;
        Pin(emailIn.gameObject, PADDING, y, CARD_W-PADDING*2, INPUT_H); y -= INPUT_H+GAP;

        var passIn = GlassInput("PasswordInput", card.transform, "Mật khẩu");
        passIn.contentType = TMP_InputField.ContentType.Password;
        Pin(passIn.gameObject, PADDING, y, CARD_W-PADDING*2, INPUT_H); y -= INPUT_H+GAP;

        var confIn = GlassInput("ConfirmPasswordInput", card.transform, "Xác nhận mật khẩu");
        confIn.contentType = TMP_InputField.ContentType.Password;
        Pin(confIn.gameObject, PADDING, y, CARD_W-PADDING*2, INPUT_H); y -= INPUT_H+GAP;

        var errGO = Txt("ErrorText", card.transform, "", FONT_ERR, ERROR_COLOR);
        errGO.GetComponent<TMP_Text>().alignment = TextAlignmentOptions.Center | TextAlignmentOptions.Midline;
        Pin(errGO, PADDING, y, CARD_W-PADDING*2, 24f);
        errGO.SetActive(false); y -= 24f+GAP;

        var regBtn   = GlassButton("RegisterButton", card.transform, "ĐĂNG KÝ", ACCENT2);
        Pin(regBtn.gameObject, PADDING, y, CARD_W-PADDING*2, BTN_H); y -= BTN_H+GAP;

        var loginBtn = GhostButton("GoToLoginButton", card.transform, "Đã có tài khoản? Đăng nhập");
        Pin(loginBtn.gameObject, PADDING, y, CARD_W-PADDING*2, BTN_H);

        var so = new SerializedObject(uiMgr);
        so.FindProperty("registerNameInput").objectReferenceValue            = nameIn;
        so.FindProperty("registerEmailInput").objectReferenceValue           = emailIn;
        so.FindProperty("registerPasswordInput").objectReferenceValue        = passIn;
        so.FindProperty("registerConfirmPasswordInput").objectReferenceValue = confIn;
        so.FindProperty("registerButton").objectReferenceValue               = regBtn;
        so.FindProperty("goToLoginButton").objectReferenceValue              = loginBtn;
        so.FindProperty("registerErrorText").objectReferenceValue            = errGO.GetComponent<TMP_Text>();
        so.ApplyModifiedProperties();

        Click(regBtn,   uiMgr, "OnRegisterButtonClicked");
        Click(loginBtn, uiMgr, "OnGoToLoginClicked");
        return panel;
    }

    // ════════════════════════════════════════════════════════════════════════
    // LOADING PANEL
    // ════════════════════════════════════════════════════════════════════════
    static GameObject BuildLoading(GameObject canvas)
    {
        var panel = Panel("LoadingPanel", canvas.transform);
        panel.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.5f);
        var card = GlassCard("Card", panel.transform, 240f, 110f);

        var cat = EmojiLabel("Icon", card.transform, "🐱", 36f);
        Pin(cat, 0, -18f, 240f, 46f);

        var loadTxt = Txt("L", card.transform, "Đang tải...", 16f, TEXT_WHITE);
        loadTxt.GetComponent<TMP_Text>().alignment = TextAlignmentOptions.Center | TextAlignmentOptions.Midline;
        Pin(loadTxt, 0, -72f, 240f, 28f);
        return panel;
    }

    // ════════════════════════════════════════════════════════════════════════
    // MAIN MENU PANEL
    // FIX: không dùng Find("V") — lưu ref trực tiếp vào biến
    // ════════════════════════════════════════════════════════════════════════
    static GameObject BuildMainMenu(GameObject canvas, AuthUIManager uiMgr)
    {
        var panel = Panel("MainMenuPanel", canvas.transform);
        var card  = GlassCard("Card", panel.transform, CARD_W, 370f);
        float y   = -PADDING;

        // Avatar
        var av  = UI("Avatar", card.transform);
        Pin(av, (CARD_W-80f)/2f, y, 80f, 80f);
        var avi = av.AddComponent<Image>();
        avi.color  = ACCENT;
        avi.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
        var avl    = EmojiLabel("E", av.transform, "🐱", 38f);
        Pin(avl, 0, 0, 80f, 80f);
        y -= 80f+GAP;

        // Welcome
        var wGO = Txt("WelcomeText", card.transform, "Chào, Bạn!", 26f, TEXT_WHITE, true);
        wGO.GetComponent<TMP_Text>().alignment = TextAlignmentOptions.Center | TextAlignmentOptions.Midline;
        Pin(wGO, 0, y, CARD_W, 38f); y -= 38f+GAP;

        // Stats — FIX: lưu scoreTxt và wordsTxt trực tiếp
        float hw = (CARD_W - PADDING*2 - GAP) / 2f;
        var row  = UI("Stats", card.transform);
        Pin(row, PADDING, y, CARD_W-PADDING*2, 68f); y -= 68f+GAP+4f;

        TMP_Text scoreValTxt, wordsValTxt;
        var sc = MiniStatCard("ScoreCard",  row.transform, "🏆 Điểm",     "0", out scoreValTxt);
        AnchorStat(sc, 0, hw);
        var wc = MiniStatCard("WordsCard",  row.transform, "📚 Từ đã học", "0", out wordsValTxt);
        AnchorStat(wc, 1, hw);

        // Play
        var playBtn = GlassButton("PlayButton", card.transform, "🎮  CHƠI NGAY", ACCENT);
        Pin(playBtn.gameObject, PADDING, y, CARD_W-PADDING*2, BTN_H); y -= BTN_H+GAP;

        // Logout
        var outBtn = GhostButton("LogoutButton", card.transform, "Đăng Xuất");
        outBtn.GetComponentInChildren<TMP_Text>().color = ERROR_COLOR;
        Pin(outBtn.gameObject, PADDING, y, CARD_W-PADDING*2, 44f);

        // Wire — dùng biến trực tiếp, không Find()
        var so = new SerializedObject(uiMgr);
        so.FindProperty("welcomeText").objectReferenceValue  = wGO.GetComponent<TMP_Text>();
        so.FindProperty("scoreText").objectReferenceValue    = scoreValTxt;   // ← FIX
        so.FindProperty("wordsText").objectReferenceValue    = wordsValTxt;   // ← FIX
        so.FindProperty("logoutButton").objectReferenceValue = outBtn;
        so.FindProperty("playButton").objectReferenceValue   = playBtn;
        so.ApplyModifiedProperties();

        Click(outBtn, uiMgr, "OnLogoutButtonClicked");
        return panel;
    }

    // ════════════════════════════════════════════════════════════════════════
    // GLASS FACTORIES
    // ════════════════════════════════════════════════════════════════════════

    static GameObject GlassCard(string name, Transform parent, float w, float h)
    {
        // Shadow
        var glow = UI("Glow", parent);
        var grt  = glow.GetComponent<RectTransform>();
        grt.anchorMin = grt.anchorMax = grt.pivot = new Vector2(0.5f, 0.5f);
        grt.anchoredPosition = new Vector2(0f, -4f);
        grt.sizeDelta = new Vector2(w+24f, h+24f);
        var gi = glow.AddComponent<Image>();
        gi.color = new Color(0f,0f,0f,0.28f); gi.sprite = Spr(); gi.type = Image.Type.Sliced;
        gi.raycastTarget = false;

        // Card
        var go = UI(name, parent);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero; rt.sizeDelta = new Vector2(w, h);
        var img = go.AddComponent<Image>();
        img.color = GLASS_BG; img.sprite = Spr(); img.type = Image.Type.Sliced;

        // Border
        var bd  = UI("Border", go.transform);
        Stretch(bd.GetComponent<RectTransform>());
        var bdi = bd.AddComponent<Image>();
        bdi.color = GLASS_BORDER; bdi.sprite = Spr(); bdi.type = Image.Type.Sliced;
        bdi.fillCenter = false; bdi.raycastTarget = false;
        return go;
    }

    static TMP_InputField GlassInput(string name, Transform parent, string hint)
    {
        var go = UI(name, parent);
        var bg = go.AddComponent<Image>();
        bg.color = GLASS_INPUT; bg.sprite = Spr(); bg.type = Image.Type.Sliced;

        var bd  = UI("Border", go.transform);
        Stretch(bd.GetComponent<RectTransform>());
        var bdi = bd.AddComponent<Image>();
        bdi.color = GLASS_INPUT_B; bdi.sprite = Spr(); bdi.type = Image.Type.Sliced;
        bdi.fillCenter = false; bdi.raycastTarget = false;

        var field = go.AddComponent<TMP_InputField>();
        field.targetGraphic = bg;

        var ta   = UI("TA", go.transform);
        var tart = ta.GetComponent<RectTransform>();
        tart.anchorMin = Vector2.zero; tart.anchorMax = Vector2.one;
        tart.offsetMin = new Vector2(22f, 4f); tart.offsetMax = new Vector2(-22f, -4f);

        var ph  = UI("PH", ta.transform);
        Stretch(ph.GetComponent<RectTransform>());
        var pht = ph.AddComponent<TextMeshProUGUI>();
        pht.text = hint; pht.fontSize = FONT_INPUT; pht.color = TEXT_HINT;
        pht.fontStyle = FontStyles.Italic;
        pht.alignment = TextAlignmentOptions.Left | TextAlignmentOptions.Midline;
        pht.raycastTarget = false;

        var tx  = UI("TX", ta.transform);
        Stretch(tx.GetComponent<RectTransform>());
        var txt = tx.AddComponent<TextMeshProUGUI>();
        txt.fontSize = FONT_INPUT; txt.color = TEXT_WHITE;
        txt.alignment = TextAlignmentOptions.Left | TextAlignmentOptions.Midline;

        field.placeholder   = pht;
        field.textComponent = txt;
        field.textViewport  = tart;
        return field;
    }

    static Button GlassButton(string name, Transform parent, string label, Color accent)
    {
        var go  = UI(name, parent);
        var img = go.AddComponent<Image>();
        img.color = accent; img.sprite = Spr(); img.type = Image.Type.Sliced;

        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        var cb = btn.colors;
        cb.normalColor      = accent;
        cb.highlightedColor = new Color(Mathf.Min(accent.r*1.15f,1f), Mathf.Min(accent.g*1.15f,1f), Mathf.Min(accent.b*1.15f,1f), 1f);
        cb.pressedColor     = new Color(accent.r*0.80f, accent.g*0.80f, accent.b*0.80f, 1f);
        cb.disabledColor    = new Color(0.5f,0.5f,0.5f,0.5f);
        cb.fadeDuration     = 0.08f;
        btn.colors = cb;

        var tgo = UI("T", go.transform);
        Stretch(tgo.GetComponent<RectTransform>());
        var t = tgo.AddComponent<TextMeshProUGUI>();
        t.text = label; t.fontSize = FONT_BTN; t.fontStyle = FontStyles.Bold;
        t.color = Color.white;
        t.alignment = TextAlignmentOptions.Center | TextAlignmentOptions.Midline;
        t.raycastTarget = false;
        return btn;
    }

    static Button GhostButton(string name, Transform parent, string label)
    {
        var go  = UI(name, parent);
        var img = go.AddComponent<Image>();
        img.color = new Color(1f,1f,1f,0.08f); img.sprite = Spr(); img.type = Image.Type.Sliced;

        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        var cb = btn.colors;
        cb.normalColor      = new Color(1f,1f,1f,0.08f);
        cb.highlightedColor = new Color(1f,1f,1f,0.20f);
        cb.pressedColor     = new Color(1f,1f,1f,0.30f);
        cb.fadeDuration     = 0.08f; btn.colors = cb;

        var tgo = UI("T", go.transform);
        Stretch(tgo.GetComponent<RectTransform>());
        var t = tgo.AddComponent<TextMeshProUGUI>();
        t.text = label; t.fontSize = FONT_BTN-2f; t.color = TEXT_WHITE;
        t.alignment = TextAlignmentOptions.Center | TextAlignmentOptions.Midline;
        t.raycastTarget = false;
        return btn;
    }

    // FIX: trả về TMP_Text của value qua out parameter — không dùng Find()
    static GameObject MiniStatCard(string name, Transform parent, string label, string value, out TMP_Text valueTxt)
    {
        var go  = UI(name, parent);
        var img = go.AddComponent<Image>();
        img.color = new Color(1f,1f,1f,0.15f); img.sprite = Spr(); img.type = Image.Type.Sliced;

        var lgo = UI("L", go.transform);
        var lrt = lgo.GetComponent<RectTransform>();
        lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
        lrt.offsetMin = new Vector2(12f,36f); lrt.offsetMax = new Vector2(-8f,-6f);
        var lt = lgo.AddComponent<TextMeshProUGUI>();
        lt.text = label; lt.fontSize = 12f; lt.color = TEXT_HINT;
        lt.alignment = TextAlignmentOptions.Left | TextAlignmentOptions.Bottom;
        lt.raycastTarget = false;

        var vgo = UI("V", go.transform);
        var vrt = vgo.GetComponent<RectTransform>();
        vrt.anchorMin = Vector2.zero; vrt.anchorMax = Vector2.one;
        vrt.offsetMin = new Vector2(12f,4f); vrt.offsetMax = new Vector2(-8f,-30f);
        var vt = vgo.AddComponent<TextMeshProUGUI>();
        vt.text = value; vt.fontSize = 26f; vt.fontStyle = FontStyles.Bold;
        vt.color = TEXT_WHITE;
        vt.alignment = TextAlignmentOptions.Left | TextAlignmentOptions.Top;
        vt.raycastTarget = false;

        valueTxt = vt; // ← trả về trực tiếp, không cần Find()
        return go;
    }

    // ════════════════════════════════════════════════════════════════════════
    // HELPERS
    // ════════════════════════════════════════════════════════════════════════

    static GameObject EmojiLabel(string name, Transform parent, string emoji, float size)
    {
        var go = UI(name, parent);
        var t  = go.AddComponent<TextMeshProUGUI>();
        t.text      = emoji;
        t.fontSize  = size;
        t.alignment = TextAlignmentOptions.Center | TextAlignmentOptions.Midline;
        t.raycastTarget = false;
        return go;
    }

    static GameObject Panel(string name, Transform parent)
    {
        var go = UI(name, parent);
        Stretch(go.GetComponent<RectTransform>());
        return go;
    }

    static GameObject Txt(string name, Transform parent, string content, float size, Color color, bool bold = false)
    {
        var go = UI(name, parent);
        var t  = go.AddComponent<TextMeshProUGUI>();
        t.text = content; t.fontSize = size; t.color = color;
        t.fontStyle          = bold ? FontStyles.Bold : FontStyles.Normal;
        t.alignment          = TextAlignmentOptions.Left | TextAlignmentOptions.Midline;
        t.enableWordWrapping = true;
        return go;
    }

    static GameObject UI(string name, Transform parent)
    {
        var go = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(go, name);
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
        return go;
    }

    static void Pin(GameObject go, float x, float y, float w, float h)
    {
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = new Vector2(x, y);
        rt.sizeDelta = new Vector2(w, h);
    }

    static void Stretch(RectTransform rt, float i = 0)
    {
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(i,i); rt.offsetMax = new Vector2(-i,-i);
    }

    static void AnchorStat(GameObject go, float anchorX, float w)
    {
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(anchorX, 0.5f);
        rt.pivot     = new Vector2(anchorX, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(w, 68f);
    }

    static Sprite Spr() =>
        AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");

    static void Click(Button btn, AuthUIManager mgr, string method)
    {
        var a = System.Delegate.CreateDelegate(
            typeof(UnityAction), mgr,
            typeof(AuthUIManager).GetMethod(method)) as UnityAction;
        UnityEventTools.AddPersistentListener(btn.onClick, a);
    }
}
#endif