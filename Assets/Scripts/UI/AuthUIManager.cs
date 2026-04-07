using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// =============================================================================
// AuthUIManager.cs — FIX BUTTONS + FIX CURSOR + TAB/ENTER NAVIGATION
//
// TÍNH NĂNG MỚI:
//   • Nhấn Enter hoặc Tab tại mỗi field → focus sang field tiếp theo
//   • Login  : Email → Password → [Submit]
//   • Register: Name → Email → Password → Confirm → [Submit]
//   • Hoạt động trên cả Android (keyboard Next) lẫn Laptop (Tab/Enter)
//   • onSubmit bắt Enter | Update() bắt Tab | lineType=SingleLine trên tất cả
// =============================================================================

public class AuthUIManager : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject loginPanel;
    [SerializeField] private GameObject registerPanel;
    [SerializeField] private GameObject loadingPanel;

    [Header("Login Fields")]
    [SerializeField] private TMP_InputField loginEmailInput;
    [SerializeField] private TMP_InputField loginPasswordInput;
    [SerializeField] private Button         loginButton;
    [SerializeField] private Button         goToRegisterButton;
    [SerializeField] private TMP_Text       loginErrorText;

    [Header("Register Fields")]
    [SerializeField] private TMP_InputField registerNameInput;
    [SerializeField] private TMP_InputField registerEmailInput;
    [SerializeField] private TMP_InputField registerPasswordInput;
    [SerializeField] private TMP_InputField registerConfirmPasswordInput;
    [SerializeField] private Button         registerButton;
    [SerializeField] private Button         goToLoginButton;
    [SerializeField] private TMP_Text       registerErrorText;

    // field đang được focus — để Update() biết Tab đang ở đâu
    private TMP_InputField _tabFocused;

    // =========================================================================
    // LIFECYCLE
    // =========================================================================

    private void Awake()
    {
        // ── onClick ───────────────────────────────────────────────────────────
        if (loginButton)        loginButton.onClick.AddListener(OnLoginButtonClicked);
        if (registerButton)     registerButton.onClick.AddListener(OnRegisterButtonClicked);
        if (goToRegisterButton) goToRegisterButton.onClick.AddListener(OnGoToRegisterClicked);
        if (goToLoginButton)    goToLoginButton.onClick.AddListener(OnGoToLoginClicked);

        SetButtonsInteractable(true);

        // ── Cursor ────────────────────────────────────────────────────────────
        FixCaret(loginEmailInput);
        FixCaret(loginPasswordInput);
        FixCaret(registerNameInput);
        FixCaret(registerEmailInput);
        FixCaret(registerPasswordInput);
        FixCaret(registerConfirmPasswordInput);

        // ── Tab / Enter navigation ────────────────────────────────────────────
        // Login chain
        BindTabField(loginEmailInput,    () => FocusField(loginPasswordInput));
        BindTabField(loginPasswordInput, () => OnLoginButtonClicked());

        // Register chain
        BindTabField(registerNameInput,            () => FocusField(registerEmailInput));
        BindTabField(registerEmailInput,           () => FocusField(registerPasswordInput));
        BindTabField(registerPasswordInput,        () => FocusField(registerConfirmPasswordInput));
        BindTabField(registerConfirmPasswordInput, () => OnRegisterButtonClicked());
    }

    private void Start()
    {
        ShowLoginPanel();
        HideError(loginErrorText);
        HideError(registerErrorText);
        StartCoroutine(BindAuthEvents());
    }

    // Bắt phím Tab trên laptop
    // TMP_InputField không xử lý Tab nên phải bắt thủ công ở Update
    private void Update()
    {
        if (_tabFocused == null) return;
        if (!Input.GetKeyDown(KeyCode.Tab)) return;
        // Gọi onSubmit listener đã gán — cùng logic với Enter
        _tabFocused.onSubmit.Invoke(_tabFocused.text);
    }

    private void OnDestroy()
    {
        if (loginButton)        loginButton.onClick.RemoveListener(OnLoginButtonClicked);
        if (registerButton)     registerButton.onClick.RemoveListener(OnRegisterButtonClicked);
        if (goToRegisterButton) goToRegisterButton.onClick.RemoveListener(OnGoToRegisterClicked);
        if (goToLoginButton)    goToLoginButton.onClick.RemoveListener(OnGoToLoginClicked);

        UnbindTabField(loginEmailInput);
        UnbindTabField(loginPasswordInput);
        UnbindTabField(registerNameInput);
        UnbindTabField(registerEmailInput);
        UnbindTabField(registerPasswordInput);
        UnbindTabField(registerConfirmPasswordInput);

        if (AuthManager.Instance == null) return;
        AuthManager.Instance.OnLoginSuccess -= HandleLoginSuccess;
        AuthManager.Instance.OnAuthError    -= HandleAuthError;
        AuthManager.Instance.OnLogout       -= HandleLogout;
    }

    // =========================================================================
    // TAB / ENTER HELPERS
    // =========================================================================

    // Gán navigation cho 1 field:
    //   lineType  = SingleLine → Enter sinh onSubmit thay vì xuống dòng
    //   onSubmit  → gọi onNext (focus field kế hoặc Submit)
    //   onSelect  → lưu vào _tabFocused để Update() bắt Tab
    private void BindTabField(TMP_InputField field, System.Action onNext)
    {
        if (field == null) return;
        field.lineType = TMP_InputField.LineType.SingleLine;
        field.onSubmit.AddListener(_ => onNext());
        field.onSelect.AddListener(_ => _tabFocused = field);
        field.onDeselect.AddListener(_ => { if (_tabFocused == field) _tabFocused = null; });
    }

    private void UnbindTabField(TMP_InputField field)
    {
        if (field == null) return;
        field.onSubmit.RemoveAllListeners();
        field.onSelect.RemoveAllListeners();
        field.onDeselect.RemoveAllListeners();
    }

    private void FocusField(TMP_InputField field)
    {
        if (field == null) return;
        field.Select();
        field.ActivateInputField();
    }

    // =========================================================================
    // BIND EVENTS (Firebase)
    // =========================================================================

    private IEnumerator BindAuthEvents()
    {
        while (AuthManager.Instance == null)
            yield return null;
        while (!AuthManager.Instance.IsInitialized)
            yield return null;

        AuthManager.Instance.OnLoginSuccess += HandleLoginSuccess;
        AuthManager.Instance.OnAuthError    += HandleAuthError;
        AuthManager.Instance.OnLogout       += HandleLogout;

        Debug.Log("[AuthUIManager] ✅ Events bound.");
    }

    // =========================================================================
    // BUTTON CALLBACKS
    // =========================================================================

    public void OnLoginButtonClicked()
    {
        if (!IsAuthReady()) return;
        HideError(loginErrorText);
        SetLoading(true);

        string email    = loginEmailInput    != null ? loginEmailInput.text.Trim() : "";
        string password = loginPasswordInput != null ? loginPasswordInput.text     : "";
        AuthManager.Instance.Login(email, password);
    }

    public void OnRegisterButtonClicked()
    {
        if (!IsAuthReady()) return;
        HideError(registerErrorText);

        string pass    = registerPasswordInput        != null ? registerPasswordInput.text        : "";
        string confirm = registerConfirmPasswordInput != null ? registerConfirmPasswordInput.text : "";

        if (pass != confirm)
        {
            ShowError(registerErrorText, "Mật khẩu xác nhận không khớp.");
            return;
        }

        SetLoading(true);
        string email = registerEmailInput != null ? registerEmailInput.text.Trim() : "";
        string name  = registerNameInput  != null ? registerNameInput.text.Trim()  : "";
        AuthManager.Instance.Register(email, pass, name);
    }

    public void OnLogoutButtonClicked()
    {
        if (!IsAuthReady()) return;
        AuthManager.Instance.Logout();
    }

    public void OnGoToRegisterClicked() { HideError(loginErrorText);    ShowRegisterPanel(); }
    public void OnGoToLoginClicked()    { HideError(registerErrorText); ShowLoginPanel();    }

    // =========================================================================
    // EVENT HANDLERS
    // =========================================================================

    private void HandleLoginSuccess(UserData d) { SetLoading(false); }

    private void HandleAuthError(string msg)
    {
        SetLoading(false);
        if (loginPanel != null && loginPanel.activeSelf)
            ShowError(loginErrorText, msg);
        else
            ShowError(registerErrorText, msg);
    }

    private void HandleLogout()
    {
        ClearInputs();
        HideError(loginErrorText);
        HideError(registerErrorText);
        ShowLoginPanel();
    }

    // =========================================================================
    // PANEL NAVIGATION
    // =========================================================================

    private void ShowLoginPanel()
    {
        SetActive(loginPanel,    true);
        SetActive(registerPanel, false);
        SetActive(loadingPanel,  false);
    }

    private void ShowRegisterPanel()
    {
        SetActive(loginPanel,    false);
        SetActive(registerPanel, true);
        SetActive(loadingPanel,  false);
    }

    // =========================================================================
    // HELPERS
    // =========================================================================

    private bool IsAuthReady()
    {
        if (AuthManager.Instance == null)
        {
            Debug.LogError("[AuthUIManager] AuthManager.Instance là null!");
            var lbl = (loginPanel != null && loginPanel.activeSelf) ? loginErrorText : registerErrorText;
            ShowError(lbl, "Hệ thống chưa sẵn sàng, vui lòng đợi...");
            return false;
        }
        if (!AuthManager.Instance.IsInitialized)
        {
            var lbl = (loginPanel != null && loginPanel.activeSelf) ? loginErrorText : registerErrorText;
            ShowError(lbl, "Đang kết nối Firebase, vui lòng đợi...");
            return false;
        }
        return true;
    }

    private void FixCaret(TMP_InputField field)
    {
        if (field == null) return;
        field.customCaretColor = true;
        field.caretColor       = new Color(0.1f, 0.1f, 0.1f, 1f);
        field.caretWidth       = 2;
        field.caretBlinkRate   = 0.85f;
    }

    private void SetButtonsInteractable(bool on)
    {
        if (loginButton)        loginButton.interactable        = on;
        if (registerButton)     registerButton.interactable     = on;
        if (goToRegisterButton) goToRegisterButton.interactable = on;
        if (goToLoginButton)    goToLoginButton.interactable    = on;
    }

    private void SetLoading(bool on)
    {
        SetActive(loadingPanel, on);
        if (loginButton)    loginButton.interactable    = !on;
        if (registerButton) registerButton.interactable = !on;
    }

    private void ShowError(TMP_Text lbl, string msg)
    { if (lbl) { lbl.text = msg; lbl.gameObject.SetActive(true); } }

    private void HideError(TMP_Text lbl)
    { if (lbl) { lbl.text = "";  lbl.gameObject.SetActive(false); } }

    private void SetActive(GameObject go, bool v)
    { if (go) go.SetActive(v); }

    private void ClearInputs()
    {
        if (loginEmailInput)              loginEmailInput.text              = "";
        if (loginPasswordInput)           loginPasswordInput.text           = "";
        if (registerNameInput)            registerNameInput.text            = "";
        if (registerEmailInput)           registerEmailInput.text           = "";
        if (registerPasswordInput)        registerPasswordInput.text        = "";
        if (registerConfirmPasswordInput) registerConfirmPasswordInput.text = "";
    }
}