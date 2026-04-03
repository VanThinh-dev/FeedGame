// // ============================================================
// //  AuthUIBuilder.cs  —  Assets/Editor/AuthUIBuilder.cs
// //  Tools → Build Auth UI
// //
// //  Hierarchy tạo ra:
// //    ├── EventSystem
// //    ├── FirebaseRoot   (FirebaseManager + AuthManager)
// //    ├── BackgroundCanvas
// //    └── AuthCanvas     (AuthUIManager + 4 panels)
// // ============================================================
// #if UNITY_EDITOR
// using UnityEditor;
// using UnityEditor.Events;
// using UnityEngine;
// using UnityEngine.Events;
// using UnityEngine.UI;
// using TMPro;

// public static class AuthUIBuilder
// {
//     // ── Theme ────────────────────────────────────────────────────────────────
//     static readonly Color BG_COLOR     = new Color(0.10f, 0.10f, 0.14f, 1f);
//     static readonly Color CARD_COLOR   = new Color(0.15f, 0.15f, 0.20f, 1f);
//     static readonly Color ACCENT       = new Color(0.42f, 0.27f, 0.90f, 1f);
//     static readonly Color INPUT_BG     = new Color(0.10f, 0.10f, 0.14f, 1f);
//     static readonly Color INPUT_BORDER = new Color(0.42f, 0.27f, 0.90f, 0.55f);
//     static readonly Color TEXT_WHITE   = new Color(0.95f, 0.95f, 0.97f, 1f);
//     static readonly Color TEXT_HINT    = new Color(0.50f, 0.50f, 0.56f, 1f);
//     static readonly Color ERROR_COLOR  = new Color(1.00f, 0.35f, 0.35f, 1f);

//     // ── Layout — portrait 9:16 ───────────────────────────────────────────────
//     const float REF_W   = 720f;
//     const float REF_H   = 1280f;
//     const float CARD_W  = 620f;   // 86% của REF_W
//     const float PADDING = 36f;
//     const float GAP     = 16f;
//     const float INPUT_H = 58f;
//     const float BTN_H   = 56f;

//     const float FONT_TITLE = 34f;
//     const float FONT_INPUT = 18f;
//     const float FONT_BTN   = 17f;
//     const float FONT_ERR   = 13f;

//     // ────────────────────────────────────────────────────────────────────────
//     [MenuItem("Tools/Build Auth UI")]
//     public static void BuildAll()
//     {
//         Undo.SetCurrentGroupName("Build Auth UI");
//         int grp = Undo.GetCurrentGroup();

//         // Xóa cũ
//         Kill("BackgroundCanvas");
//         Kill("AuthCanvas");

//         EnsureEventSystem();
//         EnsureFirebaseRoot();
//         MakeBackgroundCanvas();

//         var authGO  = MakeAuthCanvas();
//         var uiMgr   = authGO.AddComponent<AuthUIManager>();

//         var loginP  = BuildLogin(authGO, uiMgr);
//         var regP    = BuildRegister(authGO, uiMgr);
//         var loadP   = BuildLoading(authGO);
//         var mainP   = BuildMainMenu(authGO, uiMgr);

//         var so = new SerializedObject(uiMgr);
//         so.FindProperty("loginPanel").objectReferenceValue    = loginP;
//         so.FindProperty("registerPanel").objectReferenceValue = regP;
//         so.FindProperty("loadingPanel").objectReferenceValue  = loadP;
//         so.FindProperty("mainMenuPanel").objectReferenceValue = mainP;
//         so.ApplyModifiedProperties();

//         regP.SetActive(false);
//         loadP.SetActive(false);
//         mainP.SetActive(false);

//         Undo.CollapseUndoOperations(grp);
//         EditorUtility.DisplayDialog("Auth UI Builder",
//             "✅ Xong!\n\nHierarchy:\n• FirebaseRoot\n• BackgroundCanvas\n• AuthCanvas (4 panels)", "OK");
//     }

//     // ── Scene Setup ──────────────────────────────────────────────────────────

//     static void Kill(string name)
//     {
//         var go = GameObject.Find(name);
//         if (go) Undo.DestroyObjectImmediate(go);
//     }

//     static void EnsureEventSystem()
//     {
//         if (Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>()) return;
//         var es = new GameObject("EventSystem");
//         Undo.RegisterCreatedObjectUndo(es, "ES");
//         es.AddComponent<UnityEngine.EventSystems.EventSystem>();
//         es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
//     }

//     static void EnsureFirebaseRoot()
//     {
//         var root = GameObject.Find("FirebaseRoot");
//         if (root == null)
//         {
//             root = new GameObject("FirebaseRoot");
//             Undo.RegisterCreatedObjectUndo(root, "FR");
//         }
//         if (!root.GetComponent<FirebaseManager>()) Undo.AddComponent<FirebaseManager>(root);
//         if (!root.GetComponent<AuthManager>())     Undo.AddComponent<AuthManager>(root);
//     }

//     static void MakeBackgroundCanvas()
//     {
//         var go = new GameObject("BackgroundCanvas");
//         Undo.RegisterCreatedObjectUndo(go, "BG");
//         var c = go.AddComponent<Canvas>();
//         c.renderMode = RenderMode.ScreenSpaceOverlay; c.sortingOrder = -1;
//         Scaler(go); go.AddComponent<GraphicRaycaster>();

//         var bg = UI("BG", go.transform);
//         Stretch(bg.GetComponent<RectTransform>());
//         bg.AddComponent<Image>().color = BG_COLOR;
//     }

//     static GameObject MakeAuthCanvas()
//     {
//         var go = new GameObject("AuthCanvas");
//         Undo.RegisterCreatedObjectUndo(go, "AC");
//         var c = go.AddComponent<Canvas>();
//         c.renderMode = RenderMode.ScreenSpaceOverlay; c.sortingOrder = 10;
//         Scaler(go); go.AddComponent<GraphicRaycaster>();
//         return go;
//     }

//     static void Scaler(GameObject go)
//     {
//         var cs = go.AddComponent<CanvasScaler>();
//         cs.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
//         cs.referenceResolution = new Vector2(REF_W, REF_H);
//         cs.matchWidthOrHeight  = 0.5f;
//     }

//     // ── LOGIN PANEL ──────────────────────────────────────────────────────────

//     static GameObject BuildLogin(GameObject canvas, AuthUIManager uiMgr)
//     {
//         var panel = Panel("LoginPanel", canvas.transform);

//         float h = PADDING + 44+GAP+8 + INPUT_H+GAP + INPUT_H+GAP + 22+GAP + BTN_H+GAP + BTN_H + PADDING;
//         var card = Card("Card", panel.transform, CARD_W, h);
//         float y = -PADDING;

//         // Title
//         var title = Txt("Title", card.transform, "Login", FONT_TITLE, TEXT_WHITE, true);
//         title.GetComponent<TMP_Text>().alignment = TextAlignmentOptions.Center | TextAlignmentOptions.Midline;
//         Pin(title, 0, y, CARD_W, 44f); y -= 44f+GAP+8f;

//         // Inputs
//         var emailIn = Input("EmailInput", card.transform, "Username", TMP_InputField.ContentType.EmailAddress);
//         Pin(emailIn.gameObject, PADDING, y, CARD_W-PADDING*2, INPUT_H); y -= INPUT_H+GAP;

//         var passIn = Input("PasswordInput", card.transform, "Password", TMP_InputField.ContentType.Password);
//         Pin(passIn.gameObject, PADDING, y, CARD_W-PADDING*2, INPUT_H); y -= INPUT_H+GAP;

//         // Error
//         var errGO = Txt("ErrorText", card.transform, "", FONT_ERR, ERROR_COLOR);
//         errGO.GetComponent<TMP_Text>().alignment = TextAlignmentOptions.Center | TextAlignmentOptions.Midline;
//         Pin(errGO, PADDING, y, CARD_W-PADDING*2, 22f);
//         errGO.SetActive(false); y -= 22f+GAP;

//         // Buttons
//         var loginBtn = Btn("LoginButton", card.transform, "LOGIN", ACCENT, TEXT_WHITE, true);
//         Pin(loginBtn.gameObject, PADDING, y, CARD_W-PADDING*2, BTN_H); y -= BTN_H+GAP;

//         var regBtn = BtnOutline("GoToRegisterButton", card.transform, "Have account? Sign In");
//         Pin(regBtn.gameObject, PADDING, y, CARD_W-PADDING*2, BTN_H);

//         // Wire
//         var so = new SerializedObject(uiMgr);
//         so.FindProperty("loginEmailInput").objectReferenceValue    = emailIn;
//         so.FindProperty("loginPasswordInput").objectReferenceValue = passIn;
//         so.FindProperty("loginButton").objectReferenceValue        = loginBtn;
//         so.FindProperty("goToRegisterButton").objectReferenceValue = regBtn;
//         so.FindProperty("loginErrorText").objectReferenceValue     = errGO.GetComponent<TMP_Text>();
//         so.ApplyModifiedProperties();

//         Click(loginBtn, uiMgr, "OnLoginButtonClicked");
//         Click(regBtn,   uiMgr, "OnGoToRegisterClicked");
//         return panel;
//     }

//     // ── REGISTER PANEL ───────────────────────────────────────────────────────

//     static GameObject BuildRegister(GameObject canvas, AuthUIManager uiMgr)
//     {
//         var panel = Panel("RegisterPanel", canvas.transform);

//         float h = PADDING + 44+GAP+8 + (INPUT_H+GAP)*4 + 22+GAP + BTN_H+GAP + BTN_H + PADDING;
//         var card = Card("Card", panel.transform, CARD_W, h);
//         float y = -PADDING;

//         // Title
//         var title = Txt("Title", card.transform, "Register", FONT_TITLE, TEXT_WHITE, true);
//         title.GetComponent<TMP_Text>().alignment = TextAlignmentOptions.Center | TextAlignmentOptions.Midline;
//         Pin(title, 0, y, CARD_W, 44f); y -= 44f+GAP+8f;

//         // Inputs
//         var nameIn = Input("NameInput", card.transform, "Full Name", TMP_InputField.ContentType.Standard);
//         Pin(nameIn.gameObject, PADDING, y, CARD_W-PADDING*2, INPUT_H); y -= INPUT_H+GAP;

//         var emailIn = Input("EmailInput", card.transform, "E-mail", TMP_InputField.ContentType.EmailAddress);
//         Pin(emailIn.gameObject, PADDING, y, CARD_W-PADDING*2, INPUT_H); y -= INPUT_H+GAP;

//         var passIn = Input("PasswordInput", card.transform, "Password", TMP_InputField.ContentType.Password);
//         Pin(passIn.gameObject, PADDING, y, CARD_W-PADDING*2, INPUT_H); y -= INPUT_H+GAP;

//         var confIn = Input("ConfirmPasswordInput", card.transform, "Confirm Password", TMP_InputField.ContentType.Password);
//         Pin(confIn.gameObject, PADDING, y, CARD_W-PADDING*2, INPUT_H); y -= INPUT_H+GAP;

//         // Error
//         var errGO = Txt("ErrorText", card.transform, "", FONT_ERR, ERROR_COLOR);
//         errGO.GetComponent<TMP_Text>().alignment = TextAlignmentOptions.Center | TextAlignmentOptions.Midline;
//         Pin(errGO, PADDING, y, CARD_W-PADDING*2, 22f);
//         errGO.SetActive(false); y -= 22f+GAP;

//         // Buttons
//         var regBtn   = Btn("RegisterButton", card.transform, "Register", ACCENT, TEXT_WHITE, true);
//         Pin(regBtn.gameObject, PADDING, y, CARD_W-PADDING*2, BTN_H); y -= BTN_H+GAP;

//         var loginBtn = BtnOutline("GoToLoginButton", card.transform, "Have account? Sign In");
//         Pin(loginBtn.gameObject, PADDING, y, CARD_W-PADDING*2, BTN_H);

//         // Wire
//         var so = new SerializedObject(uiMgr);
//         so.FindProperty("registerNameInput").objectReferenceValue            = nameIn;
//         so.FindProperty("registerEmailInput").objectReferenceValue           = emailIn;
//         so.FindProperty("registerPasswordInput").objectReferenceValue        = passIn;
//         so.FindProperty("registerConfirmPasswordInput").objectReferenceValue = confIn;
//         so.FindProperty("registerButton").objectReferenceValue               = regBtn;
//         so.FindProperty("goToLoginButton").objectReferenceValue              = loginBtn;
//         so.FindProperty("registerErrorText").objectReferenceValue            = errGO.GetComponent<TMP_Text>();
//         so.ApplyModifiedProperties();

//         Click(regBtn,   uiMgr, "OnRegisterButtonClicked");
//         Click(loginBtn, uiMgr, "OnGoToLoginClicked");
//         return panel;
//     }

//     // ── LOADING PANEL ────────────────────────────────────────────────────────

//     static GameObject BuildLoading(GameObject canvas)
//     {
//         var panel = Panel("LoadingPanel", canvas.transform);
//         panel.AddComponent<Image>().color = new Color(0,0,0,0.7f);

//         var card = Card("Card", panel.transform, 260f, 110f);
//         Pin(Txt("S", card.transform, "◌", 38f, ACCENT), 0, -18f, 260f, 46f);
//         Pin(Txt("L", card.transform, "Đang xử lý...", 15f, TEXT_HINT), 0, -72f, 260f, 26f);
//         return panel;
//     }

//     // ── MAIN MENU PANEL ──────────────────────────────────────────────────────

//     static GameObject BuildMainMenu(GameObject canvas, AuthUIManager uiMgr)
//     {
//         var panel = Panel("MainMenuPanel", canvas.transform);
//         var card  = Card("Card", panel.transform, CARD_W, 380f);
//         float y   = -PADDING;

//         // Avatar
//         var av = UI("Avatar", card.transform);
//         Pin(av, (CARD_W-72f)/2f, y, 72f, 72f);
//         var avi = av.AddComponent<Image>();
//         avi.color = ACCENT;
//         avi.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
//         var avl = Txt("L", av.transform, "P", 28f, TEXT_WHITE, true);
//         avl.GetComponent<TMP_Text>().alignment = TextAlignmentOptions.Center | TextAlignmentOptions.Midline;
//         Pin(avl, 0, 0, 72f, 72f);
//         y -= 72f+GAP;

//         // Welcome
//         var wGO = Txt("WelcomeText", card.transform, "Chào, Player!", 28f, TEXT_WHITE, true);
//         wGO.GetComponent<TMP_Text>().alignment = TextAlignmentOptions.Center | TextAlignmentOptions.Midline;
//         Pin(wGO, 0, y, CARD_W, 38f); y -= 38f+4f+GAP+4f;

//         // Stats
//         float hw = (CARD_W - PADDING*2 - GAP) / 2f;
//         var row = UI("Stats", card.transform);
//         Pin(row, PADDING, y, CARD_W-PADDING*2, 68f); y -= 68f+GAP+8f;

//         var sc = StatCard("ScoreCard", row.transform, "🏆 Điểm", "0");
//         AnchorStat(sc, 0, hw);
//         var wc = StatCard("WordsCard", row.transform, "📚 Từ đã học", "0");
//         AnchorStat(wc, 1, hw);

//         // Play
//         var playBtn = Btn("PlayButton", card.transform, "🎮  CHƠI NGAY", ACCENT, TEXT_WHITE, true);
//         Pin(playBtn.gameObject, PADDING, y, CARD_W-PADDING*2, BTN_H); y -= BTN_H+GAP;

//         // Logout
//         var outBtn = BtnOutline("LogoutButton", card.transform, "Đăng Xuất");
//         outBtn.GetComponentInChildren<TMP_Text>().color = ERROR_COLOR;
//         Pin(outBtn.gameObject, PADDING, y, CARD_W-PADDING*2, 44f);

//         var so = new SerializedObject(uiMgr);
//         so.FindProperty("welcomeText").objectReferenceValue  = wGO.GetComponent<TMP_Text>();
//         so.FindProperty("scoreText").objectReferenceValue    = sc.transform.Find("V").GetComponent<TMP_Text>();
//         so.FindProperty("wordsText").objectReferenceValue    = wc.transform.Find("V").GetComponent<TMP_Text>();
//         so.FindProperty("logoutButton").objectReferenceValue = outBtn;
//         so.FindProperty("playButton").objectReferenceValue   = playBtn;
//         so.ApplyModifiedProperties();

//         Click(outBtn, uiMgr, "OnLogoutButtonClicked");
//         return panel;
//     }

//     // ════════════════════════════════════════════════════════════════════════
//     // FACTORIES
//     // ════════════════════════════════════════════════════════════════════════

//     static GameObject Panel(string name, Transform parent)
//     {
//         var go = UI(name, parent);
//         Stretch(go.GetComponent<RectTransform>());
//         return go;
//     }

//     static GameObject Card(string name, Transform parent, float w, float h)
//     {
//         var sh = UI("Sh", parent);
//         var srt = sh.GetComponent<RectTransform>();
//         srt.anchorMin = srt.anchorMax = srt.pivot = new Vector2(0.5f, 0.5f);
//         srt.anchoredPosition = new Vector2(0, -5f);
//         srt.sizeDelta = new Vector2(w+8f, h+8f);
//         var si = sh.AddComponent<Image>();
//         si.color = new Color(0,0,0,0.4f); si.sprite = Spr(); si.type = Image.Type.Sliced;

//         var go = UI(name, parent);
//         var rt = go.GetComponent<RectTransform>();
//         rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
//         rt.anchoredPosition = Vector2.zero; rt.sizeDelta = new Vector2(w, h);
//         var img = go.AddComponent<Image>();
//         img.color = CARD_COLOR; img.sprite = Spr(); img.type = Image.Type.Sliced;
//         return go;
//     }

//     static TMP_InputField Input(string name, Transform parent, string hint, TMP_InputField.ContentType type)
//     {
//         var go = UI(name, parent);
//         var bg = go.AddComponent<Image>();
//         bg.color = INPUT_BG; bg.sprite = Spr(); bg.type = Image.Type.Sliced;

//         // Accent border
//         var bd = UI("B", go.transform);
//         Stretch(bd.GetComponent<RectTransform>());
//         var bi = bd.AddComponent<Image>();
//         bi.color = INPUT_BORDER; bi.sprite = Spr(); bi.type = Image.Type.Sliced;

//         var field = go.AddComponent<TMP_InputField>();
//         field.targetGraphic = bg;

//         var ta = UI("TA", go.transform);
//         var tart = ta.GetComponent<RectTransform>();
//         tart.anchorMin = Vector2.zero; tart.anchorMax = Vector2.one;
//         tart.offsetMin = new Vector2(20,4); tart.offsetMax = new Vector2(-20,-4);

//         var ph = UI("PH", ta.transform);
//         Stretch(ph.GetComponent<RectTransform>());
//         var pht = ph.AddComponent<TextMeshProUGUI>();
//         pht.text = hint; pht.fontSize = FONT_INPUT; pht.color = TEXT_HINT;
//         pht.alignment = TextAlignmentOptions.Left | TextAlignmentOptions.Midline;

//         var tx = UI("TX", ta.transform);
//         Stretch(tx.GetComponent<RectTransform>());
//         var txt = tx.AddComponent<TextMeshProUGUI>();
//         txt.fontSize = FONT_INPUT; txt.color = TEXT_WHITE;
//         txt.alignment = TextAlignmentOptions.Left | TextAlignmentOptions.Midline;

//         field.placeholder = pht; field.textComponent = txt;
//         field.contentType = type; field.textViewport = tart;
//         return field;
//     }

//     static Button Btn(string name, Transform parent, string label, Color bg, Color fg, bool bold = false)
//     {
//         var go  = UI(name, parent);
//         var img = go.AddComponent<Image>();
//         img.color = bg; img.sprite = Spr(); img.type = Image.Type.Sliced;

//         var btn = go.AddComponent<Button>();
//         btn.targetGraphic = img;
//         var cb = btn.colors;
//         cb.normalColor      = bg;
//         cb.highlightedColor = new Color(bg.r*1.15f, bg.g*1.15f, bg.b*1.15f, 1f);
//         cb.pressedColor     = new Color(bg.r*0.80f, bg.g*0.80f, bg.b*0.80f, 1f);
//         cb.disabledColor    = new Color(0.35f,0.35f,0.35f,0.7f);
//         cb.fadeDuration     = 0.08f;
//         btn.colors = cb;

//         var tgo = UI("T", go.transform);
//         Stretch(tgo.GetComponent<RectTransform>());
//         var t = tgo.AddComponent<TextMeshProUGUI>();
//         t.text = label; t.fontSize = FONT_BTN; t.color = fg;
//         t.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
//         t.alignment = TextAlignmentOptions.Center | TextAlignmentOptions.Midline;
//         return btn;
//     }

//     static Button BtnOutline(string name, Transform parent, string label)
//     {
//         var go  = UI(name, parent);
//         var img = go.AddComponent<Image>();
//         img.color  = new Color(0.42f,0.27f,0.90f,0.15f);
//         img.sprite = Spr(); img.type = Image.Type.Sliced;

//         var btn = go.AddComponent<Button>();
//         btn.targetGraphic = img;
//         var cb = btn.colors;
//         cb.normalColor      = new Color(0.42f,0.27f,0.90f,0.15f);
//         cb.highlightedColor = new Color(0.42f,0.27f,0.90f,0.35f);
//         cb.pressedColor     = new Color(0.42f,0.27f,0.90f,0.50f);
//         cb.fadeDuration     = 0.08f; btn.colors = cb;

//         var tgo = UI("T", go.transform);
//         Stretch(tgo.GetComponent<RectTransform>());
//         var t = tgo.AddComponent<TextMeshProUGUI>();
//         t.text = label; t.fontSize = FONT_BTN; t.color = ACCENT;
//         t.alignment = TextAlignmentOptions.Center | TextAlignmentOptions.Midline;
//         return btn;
//     }

//     static GameObject Txt(string name, Transform parent, string content, float size, Color color, bool bold = false)
//     {
//         var go = UI(name, parent);
//         var t  = go.AddComponent<TextMeshProUGUI>();
//         t.text = content; t.fontSize = size; t.color = color;
//         t.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
//         t.alignment = TextAlignmentOptions.Left | TextAlignmentOptions.Midline;
//         t.enableWordWrapping = true;
//         return go;
//     }

//     static GameObject StatCard(string name, Transform parent, string label, string value)
//     {
//         var go  = UI(name, parent);
//         var img = go.AddComponent<Image>();
//         img.color = new Color(0.10f,0.10f,0.14f,1f); img.sprite = Spr(); img.type = Image.Type.Sliced;

//         var lgo = UI("L", go.transform);
//         var lrt = lgo.GetComponent<RectTransform>();
//         lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
//         lrt.offsetMin = new Vector2(14,36); lrt.offsetMax = new Vector2(-8,-6);
//         var lt = lgo.AddComponent<TextMeshProUGUI>();
//         lt.text = label; lt.fontSize = 12f; lt.color = TEXT_HINT;
//         lt.alignment = TextAlignmentOptions.Left | TextAlignmentOptions.Bottom;

//         var vgo = UI("V", go.transform);
//         var vrt = vgo.GetComponent<RectTransform>();
//         vrt.anchorMin = Vector2.zero; vrt.anchorMax = Vector2.one;
//         vrt.offsetMin = new Vector2(14,4); vrt.offsetMax = new Vector2(-8,-30);
//         var vt = vgo.AddComponent<TextMeshProUGUI>();
//         vt.text = value; vt.fontSize = 26f; vt.fontStyle = FontStyles.Bold;
//         vt.color = TEXT_WHITE;
//         vt.alignment = TextAlignmentOptions.Left | TextAlignmentOptions.Top;

//         return go;
//     }

//     static void AnchorStat(GameObject go, float anchorX, float w)
//     {
//         var rt = go.GetComponent<RectTransform>();
//         rt.anchorMin = rt.anchorMax = new Vector2(anchorX, 0.5f);
//         rt.pivot = new Vector2(anchorX, 0.5f);
//         rt.anchoredPosition = Vector2.zero;
//         rt.sizeDelta = new Vector2(w, 68f);
//     }

//     // ── Low-level helpers ────────────────────────────────────────────────────

//     static GameObject UI(string name, Transform parent)
//     {
//         var go = new GameObject(name);
//         Undo.RegisterCreatedObjectUndo(go, name);
//         go.transform.SetParent(parent, false);
//         go.AddComponent<RectTransform>();
//         return go;
//     }

//     static void Pin(GameObject go, float x, float y, float w, float h)
//     {
//         var rt = go.GetComponent<RectTransform>();
//         rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
//         rt.pivot = new Vector2(0, 1);
//         rt.anchoredPosition = new Vector2(x, y);
//         rt.sizeDelta = new Vector2(w, h);
//     }

//     static void Stretch(RectTransform rt, float i = 0)
//     {
//         rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
//         rt.offsetMin = new Vector2(i,i); rt.offsetMax = new Vector2(-i,-i);
//     }

//     static Sprite Spr() =>
//         AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");

//     static void Click(Button btn, AuthUIManager mgr, string method)
//     {
//         var a = System.Delegate.CreateDelegate(
//             typeof(UnityAction), mgr,
//             typeof(AuthUIManager).GetMethod(method)) as UnityAction;
//         UnityEventTools.AddPersistentListener(btn.onClick, a);
//     }
// }
// #endif