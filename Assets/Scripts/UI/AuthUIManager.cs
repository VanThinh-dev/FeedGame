using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// =============================================================================
// AuthUIManager.cs — FIX BUTTONS
//
// ROOT CAUSE:
//   • SetButtonsInteractable(false) trong Start() + chờ IsInitialized trong Update
//     → nếu Firebase chậm hoặc Update chạy sau 1 frame thì buttons bị disabled lâu
//   • Button onClick KHÔNG được gán bằng code → phụ thuộc hoàn toàn vào Inspector
//
// FIX:
//   • Gán onClick cho tất cả buttons bằng code trong Awake() — không phụ thuộc Inspector
//   • Buttons luôn interactable = true, chỉ disable trong lúc loading
//   • Dùng Coroutine thay Update để bind events — sạch hơn, không poll mỗi frame
//   • IsAuthReady() vẫn giữ để guard trước khi gọi Firebase
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

    // =========================================================================
    // LIFECYCLE
    // =========================================================================

    private void Awake()
    {
        // ── Gán onClick bằng code — không phụ thuộc Inspector ────────────────
        // Các listener cũ trong Inspector vẫn chạy bình thường, AddListener chỉ thêm
        if (loginButton)        loginButton.onClick.AddListener(OnLoginButtonClicked);
        if (registerButton)     registerButton.onClick.AddListener(OnRegisterButtonClicked);
        if (goToRegisterButton) goToRegisterButton.onClick.AddListener(OnGoToRegisterClicked);
        if (goToLoginButton)    goToLoginButton.onClick.AddListener(OnGoToLoginClicked);

        // ── Buttons luôn bật — không disable trước khi Firebase ready ────────
        SetButtonsInteractable(true);
    }

    private void Start()
    {
        ShowLoginPanel();
        HideError(loginErrorText);
        HideError(registerErrorText);

        // Bind events Firebase qua Coroutine, không dùng Update
        StartCoroutine(BindAuthEvents());
    }

    private void OnDestroy()
    {
        // Gỡ listener code
        if (loginButton)        loginButton.onClick.RemoveListener(OnLoginButtonClicked);
        if (registerButton)     registerButton.onClick.RemoveListener(OnRegisterButtonClicked);
        if (goToRegisterButton) goToRegisterButton.onClick.RemoveListener(OnGoToRegisterClicked);
        if (goToLoginButton)    goToLoginButton.onClick.RemoveListener(OnGoToLoginClicked);

        // Gỡ event AuthManager
        if (AuthManager.Instance == null) return;
        AuthManager.Instance.OnLoginSuccess -= HandleLoginSuccess;
        AuthManager.Instance.OnAuthError    -= HandleAuthError;
        AuthManager.Instance.OnLogout       -= HandleLogout;
    }

    // =========================================================================
    // BIND EVENTS — Coroutine thay Update, không poll mỗi frame
    // =========================================================================

    private IEnumerator BindAuthEvents()
    {
        // Chờ AuthManager tồn tại
        while (AuthManager.Instance == null)
            yield return null;

        // Chờ Firebase init xong
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

    private void HandleLoginSuccess(UserData d)
    {
        SetLoading(false);
        // BedroomManager lo việc ẩn AuthCanvas và hiện Bedroom
    }

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