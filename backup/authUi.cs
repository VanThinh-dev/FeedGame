// using UnityEngine;
// using UnityEngine.UI;
// using TMPro;

// /// <summary>
// /// AuthUIManager — quản lý UI Login / Register / Loading.
// /// Fix: null-check AuthManager.Instance trước mọi lần gọi.
// /// </summary>
// public class AuthUIManager : MonoBehaviour
// {
//     [Header("Panels")]
//     [SerializeField] private GameObject loginPanel;
//     [SerializeField] private GameObject registerPanel;
//     [SerializeField] private GameObject loadingPanel;

//     [Header("Login Fields")]
//     [SerializeField] private TMP_InputField loginEmailInput;
//     [SerializeField] private TMP_InputField loginPasswordInput;
//     [SerializeField] private Button         loginButton;
//     [SerializeField] private Button         goToRegisterButton;
//     [SerializeField] private TMP_Text       loginErrorText;

//     [Header("Register Fields")]
//     [SerializeField] private TMP_InputField registerNameInput;
//     [SerializeField] private TMP_InputField registerEmailInput;
//     [SerializeField] private TMP_InputField registerPasswordInput;
//     [SerializeField] private TMP_InputField registerConfirmPasswordInput;
//     [SerializeField] private Button         registerButton;
//     [SerializeField] private Button         goToLoginButton;
//     [SerializeField] private TMP_Text       registerErrorText;

//     private bool eventsBound = false;

//     // ─── Lifecycle ───────────────────────────────────────────────
//     private void Start()
//     {
//         // Tắt button cho đến khi AuthManager sẵn sàng
//         SetButtonsInteractable(false);
//         ShowLoginPanel();
//         HideError(loginErrorText);
//         HideError(registerErrorText);
//     }

//     private void Update()
//     {
//         if (!eventsBound) TryBindEvents();
//     }

//     private void TryBindEvents()
//     {
//         if (AuthManager.Instance == null) return;
//         if (!AuthManager.Instance.IsInitialized) return;

//         AuthManager.Instance.OnLoginSuccess += HandleLoginSuccess;
//         AuthManager.Instance.OnAuthError    += HandleAuthError;
//         AuthManager.Instance.OnLogout       += HandleLogout;

//         SetButtonsInteractable(true);
//         eventsBound = true;
//         Debug.Log("[AuthUIManager] ✅ Events bound, buttons enabled.");
//     }

//     private void OnDestroy()
//     {
//         if (AuthManager.Instance == null) return;
//         AuthManager.Instance.OnLoginSuccess -= HandleLoginSuccess;
//         AuthManager.Instance.OnAuthError    -= HandleAuthError;
//         AuthManager.Instance.OnLogout       -= HandleLogout;
//     }

//     // ─── Button Callbacks ────────────────────────────────────────

//     public void OnLoginButtonClicked()
//     {
//         // Guard: AuthManager phải tồn tại và đã init
//         if (!IsAuthReady()) return;

//         HideError(loginErrorText);
//         SetLoading(true);
//         AuthManager.Instance.Login(
//             loginEmailInput  != null ? loginEmailInput.text.Trim() : "",
//             loginPasswordInput != null ? loginPasswordInput.text    : "");
//     }

//     public void OnRegisterButtonClicked()
//     {
//         if (!IsAuthReady()) return;

//         HideError(registerErrorText);

//         string pass    = registerPasswordInput        != null ? registerPasswordInput.text        : "";
//         string confirm = registerConfirmPasswordInput != null ? registerConfirmPasswordInput.text : "";
//         if (pass != confirm)
//         { ShowError(registerErrorText, "Mật khẩu xác nhận không khớp."); return; }

//         SetLoading(true);
//         AuthManager.Instance.Register(
//             registerEmailInput != null ? registerEmailInput.text.Trim() : "",
//             pass,
//             registerNameInput  != null ? registerNameInput.text.Trim()  : "");
//     }

//     public void OnLogoutButtonClicked()
//     {
//         if (!IsAuthReady()) return;
//         AuthManager.Instance.Logout();
//     }

//     public void OnGoToRegisterClicked() { HideError(loginErrorText);    ShowRegisterPanel(); }
//     public void OnGoToLoginClicked()    { HideError(registerErrorText); ShowLoginPanel();    }

//     // ─── Event Handlers ──────────────────────────────────────────

//     private void HandleLoginSuccess(UserData d)
//     {
//         SetLoading(false);
//         // BedroomManager lo việc ẩn AuthCanvas và hiện Bedroom
//     }

//     private void HandleAuthError(string msg)
//     {
//         SetLoading(false);
//         if (loginPanel != null && loginPanel.activeSelf)
//             ShowError(loginErrorText, msg);
//         else
//             ShowError(registerErrorText, msg);
//     }

//     private void HandleLogout()
//     {
//         ClearInputs();
//         HideError(loginErrorText);
//         HideError(registerErrorText);
//         ShowLoginPanel();
//         // BedroomManager.HandleLogoutEvent lo việc hiện lại AuthCanvas
//     }

//     // ─── Panel Navigation ────────────────────────────────────────

//     private void ShowLoginPanel()
//     {
//         SetActive(loginPanel,    true);
//         SetActive(registerPanel, false);
//         SetActive(loadingPanel,  false);
//     }

//     private void ShowRegisterPanel()
//     {
//         SetActive(loginPanel,    false);
//         SetActive(registerPanel, true);
//         SetActive(loadingPanel,  false);
//     }

//     // ─── Helpers ─────────────────────────────────────────────────

//     /// <summary>Kiểm tra AuthManager sẵn sàng trước khi gọi API.</summary>
//     private bool IsAuthReady()
//     {
//         if (AuthManager.Instance == null)
//         {
//             Debug.LogError("[AuthUIManager] AuthManager.Instance là null! Kiểm tra FirebaseRoot trong scene.");
//             ShowError(loginPanel != null && loginPanel.activeSelf ? loginErrorText : registerErrorText,
//                       "Hệ thống chưa sẵn sàng, vui lòng đợi...");
//             return false;
//         }
//         if (!AuthManager.Instance.IsInitialized)
//         {
//             ShowError(loginPanel != null && loginPanel.activeSelf ? loginErrorText : registerErrorText,
//                       "Đang kết nối Firebase, vui lòng đợi...");
//             return false;
//         }
//         return true;
//     }

//     private void SetButtonsInteractable(bool on)
//     {
//         if (loginButton)        loginButton.interactable        = on;
//         if (registerButton)     registerButton.interactable     = on;
//         if (goToRegisterButton) goToRegisterButton.interactable = on;
//         if (goToLoginButton)    goToLoginButton.interactable    = on;
//     }

//     private void SetLoading(bool on)
//     {
//         SetActive(loadingPanel, on);
//         if (loginButton)    loginButton.interactable    = !on;
//         if (registerButton) registerButton.interactable = !on;
//     }

//     private void ShowError(TMP_Text lbl, string msg)
//     { if (lbl) { lbl.text = msg; lbl.gameObject.SetActive(true); } }

//     private void HideError(TMP_Text lbl)
//     { if (lbl) { lbl.text = "";  lbl.gameObject.SetActive(false); } }

//     private void SetActive(GameObject go, bool v)
//     { if (go) go.SetActive(v); }

//     private void ClearInputs()
//     {
//         if (loginEmailInput)              loginEmailInput.text              = "";
//         if (loginPasswordInput)           loginPasswordInput.text           = "";
//         if (registerNameInput)            registerNameInput.text            = "";
//         if (registerEmailInput)           registerEmailInput.text           = "";
//         if (registerPasswordInput)        registerPasswordInput.text        = "";
//         if (registerConfirmPasswordInput) registerConfirmPasswordInput.text = "";
//     }
// }