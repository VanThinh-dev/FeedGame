// using System.Collections;
// using UnityEngine;

// // =============================================================================
// // BedroomManager.cs — v5
// //
// // FIX so với v4:
// //   • OpenMedalCanvas() gọi MedalManager.Instance.OpenMedalCanvas()
// //     thay vì chỉ SetActive(true) — để MedalManager load Firebase + bật canvas
// // =============================================================================

// public class BedroomManager : MonoBehaviour
// {
//     public static BedroomManager Instance { get; private set; }

//     // ── Canvases cần quản lý ─────────────────────────────────────────────────
//     [Header("Auth")]
//     [SerializeField] private GameObject authCanvas;

//     [Header("Canvases")]
//     [SerializeField] private GameObject bedroomCanvas;

//     [Header("Keo BackgroundCanvas vao day")]
//     [SerializeField] private GameObject backgroundCanvas;

//     [Header("Sub Canvases")]
//     [SerializeField] private GameObject vocabCanvas;
//     [SerializeField] private GameObject medalCanvas;   // vẫn giữ để HideAllSubCanvases ẩn được

//     // =========================================================================
//     // LIFECYCLE
//     // =========================================================================

//     private void Awake()
//     {
//         if (Instance != null && Instance != this) { Destroy(gameObject); return; }
//         Instance = this;

//         if (bedroomCanvas != null) bedroomCanvas.SetActive(false);
//     }

//     private void Start() => StartCoroutine(WaitAndSubscribe());

//     private void OnDestroy()
//     {
//         if (AuthManager.Instance != null)
//         {
//             AuthManager.Instance.OnLoginSuccess -= HandleLoginSuccess;
//             AuthManager.Instance.OnLogout       -= HandleLogoutEvent;
//         }
//     }

//     // =========================================================================
//     // SUBSCRIBE
//     // =========================================================================

//     private IEnumerator WaitAndSubscribe()
//     {
//         while (AuthManager.Instance == null) yield return null;

//         AuthManager.Instance.OnLoginSuccess += HandleLoginSuccess;
//         AuthManager.Instance.OnLogout       += HandleLogoutEvent;

//         while (!AuthManager.Instance.IsInitialized) yield return null;

//         if (AuthManager.Instance.IsLoggedIn)
//             AuthManager.Instance.LoadUserData(AuthManager.Instance.CurrentUser.UserId);
//     }

//     // =========================================================================
//     // EVENT HANDLERS
//     // =========================================================================

//     private void HandleLoginSuccess(UserData userData)
//     {
//         Debug.Log($"[BedroomManager] Chao {userData.displayName}");
//         if (authCanvas != null) authCanvas.SetActive(false);
//         ShowBedroom();
//     }

//     private void HandleLogoutEvent() => OnLogout();

//     // =========================================================================
//     // PUBLIC API
//     // =========================================================================

//     public void ShowBedroom()
//     {
//         if (backgroundCanvas != null) backgroundCanvas.SetActive(true);
//         if (bedroomCanvas    != null) bedroomCanvas.SetActive(true);
//         HideAllSubCanvases();
//     }

//     public void HideBedroom()
//     {
//         if (bedroomCanvas != null) bedroomCanvas.SetActive(false);
//     }

//     public void EnterGameplay()
//     {
//         HideAllSubCanvases();
//         if (bedroomCanvas    != null) bedroomCanvas.SetActive(false);
//         if (backgroundCanvas != null) backgroundCanvas.SetActive(false);
//         if (authCanvas       != null) authCanvas.SetActive(false);
//         Debug.Log("[BedroomManager] Vao gameplay — an tat ca canvas.");
//     }

//     public void ExitGameplay()
//     {
//         if (backgroundCanvas != null) backgroundCanvas.SetActive(true);
//         if (bedroomCanvas    != null) bedroomCanvas.SetActive(true);
//         HideAllSubCanvases();
//         Debug.Log("[BedroomManager] Thoat gameplay — hien lai Bedroom.");
//     }

//     public void OpenVocabCanvas()
//     {
//         HideAllSubCanvases();
//         if (vocabCanvas != null) vocabCanvas.SetActive(true);
//         else Debug.LogWarning("[BedroomManager] vocabCanvas chua gan!");
//     }

//     // -------------------------------------------------------------------------
//     // FIX v5: Gọi MedalManager để load Firebase + hiện canvas đúng cách
//     // Không dùng SetActive trực tiếp vì MedalCanvas cần load data trước
//     // -------------------------------------------------------------------------
//     public void OpenMedalCanvas()
//     {
//         HideAllSubCanvases();

//         if (MedalManager.Instance != null)
//         {
//             MedalManager.Instance.OpenMedalCanvas();
//         }
//         else
//         {
//             // Fallback nếu MedalManager chưa sẵn sàng
//             Debug.LogWarning("[BedroomManager] MedalManager.Instance chua co — fallback SetActive.");
//             if (medalCanvas != null) medalCanvas.SetActive(true);
//         }
//     }

//     public void CloseAllSubCanvases() => HideAllSubCanvases();

//     public void OnLogout()
//     {
//         HideBedroom();
//         if (backgroundCanvas != null) backgroundCanvas.SetActive(false);
//         if (authCanvas       != null) authCanvas.SetActive(true);
//     }

//     // =========================================================================
//     // PRIVATE HELPERS
//     // =========================================================================

//     private void HideAllSubCanvases()
//     {
//         if (vocabCanvas  != null) vocabCanvas.SetActive(false);
//         if (medalCanvas  != null) medalCanvas.SetActive(false);
//     }
// }