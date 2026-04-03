// using System;
// using System.Collections.Generic;
// using UnityEngine;
// using Firebase.Auth;
// using Firebase.Database;
// using Firebase.Extensions;

// // =============================================================================
// // AuthManager.cs — STABLE + HOT-RELOAD
// //
// // FIX so với v4:
// //   • GetFriendlyError: dùng try/catch toàn bộ, không crash khi Firebase
// //     throw exception kiểu lạ (AggregateException lồng nhiều tầng)
// //   • UpdateFullUserData: fire OnUserDataReady sau khi Firebase confirm save
// //   • RefreshUserData(): public, gọi được từ BedroomManager khi quay lại
// //   • OnUserDataUpdated: event riêng cho các UI cần biết data đã thay đổi
// // =============================================================================

// public class AuthManager : MonoBehaviour
// {
//     public static AuthManager Instance;

//     // ===== EVENTS =============================================================
//     public event Action<UserData> OnLoginSuccess;   // fire sau khi load DB xong
//     public event Action<UserData> OnUserDataReady;  // alias — UI dùng cái này
//     public event Action<UserData> OnUserDataUpdated;// fire sau UpdateFullUserData
//     public event Action<string>   OnAuthError;
//     public event Action           OnLogout;

//     // ===== STATE ==============================================================
//     public bool         IsInitialized   => isInitialized;
//     public bool         IsLoggedIn      => currentUser != null;
//     public FirebaseUser CurrentUser     => currentUser;

//     public UserData CurrentUserData    { get; private set; }
//     public UserData CurrentUserRuntime { get; private set; }

//     private FirebaseAuth      auth;
//     private DatabaseReference dbRef;
//     private FirebaseUser      currentUser;
//     private bool              isInitialized = false;

//     // =========================================================================
//     // LIFECYCLE
//     // =========================================================================
//    private void Awake()
//     {
//         if (Instance == null) Instance = this;
//         else { Destroy(this); return; }  // ← phải là Destroy(this)
//     }

//     private void Update()
//     {
//         if (isInitialized) return;
//         if (FirebaseManager.Instance == null) return;
//         if (!FirebaseManager.Instance.IsInitialized) return;
//         SetupAuth();
//     }

//     private void OnDestroy()
//     {
//         if (auth != null)
//             auth.StateChanged -= OnAuthStateChanged;
//     }

//     private void SetupAuth()
//     {
//         auth  = FirebaseAuth.DefaultInstance;
//         dbRef = FirebaseDatabase.DefaultInstance.RootReference;
//         isInitialized = true;
//         auth.StateChanged += OnAuthStateChanged;
//         Debug.Log("[AuthManager] Auth ready.");
//     }

//     private void OnAuthStateChanged(object sender, EventArgs e)
//     {
//         FirebaseUser newUser = auth.CurrentUser;

//         // ── LOGIN ─────────────────────────────────────────────────────────────
//         if (newUser != null && currentUser == null)
//         {
//             currentUser = newUser;
//             LoadUserData(currentUser.UserId);   // fire events sau khi có data
//         }
//         // ── LOGOUT ────────────────────────────────────────────────────────────
//         else if (newUser == null && currentUser != null)
//         {
//             currentUser        = null;
//             CurrentUserData    = null;
//             CurrentUserRuntime = null;
//             OnLogout?.Invoke();
//         }
//     }

//     // =========================================================================
//     // AUTH
//     // =========================================================================
//     public void Register(string email, string password, string displayName)
//     {
//         if (!isInitialized) return;

//         auth.CreateUserWithEmailAndPasswordAsync(email, password)
//             .ContinueWithOnMainThread(task =>
//             {
//                 if (task.IsFaulted || task.IsCanceled)
//                 {
//                     OnAuthError?.Invoke(ParseFirebaseError(task.Exception));
//                     return;
//                 }

//                 currentUser = task.Result.User;
//                 SaveNewUserToDatabase(currentUser.UserId, email, displayName);
//             });
//     }

//     public void Login(string email, string password)
//     {
//         if (!isInitialized) return;

//         auth.SignInWithEmailAndPasswordAsync(email, password)
//             .ContinueWithOnMainThread(task =>
//             {
//                 if (task.IsFaulted || task.IsCanceled)
//                 {
//                     OnAuthError?.Invoke(ParseFirebaseError(task.Exception));
//                 }
//                 // SUCCESS: OnAuthStateChanged tự bắt → LoadUserData → events
//             });
//     }

//     public void Logout() => auth?.SignOut();

//     // =========================================================================
//     // DATABASE
//     // =========================================================================
//     private void SaveNewUserToDatabase(string uid, string email, string displayName)
//     {
//         var newUser = new UserData
//         {
//             uid          = uid,
//             email        = email,
//             displayName  = displayName,
//             level        = 1,
//             xp           = 0,
//             coins        = 0,
//             wordsLearned = 0,
//             createdAt    = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
//         };

//         dbRef.Child("users").Child(uid)
//             .SetRawJsonValueAsync(JsonUtility.ToJson(newUser))
//             .ContinueWithOnMainThread(task =>
//             {
//                 if (task.IsFaulted)
//                 {
//                     OnAuthError?.Invoke("Không thể tạo tài khoản. Vui lòng thử lại.");
//                     Debug.LogError($"[AuthManager] SaveNewUser failed: {task.Exception?.Message}");
//                     return;
//                 }

//                 CurrentUserData    = newUser;
//                 CurrentUserRuntime = newUser;

//                 OnLoginSuccess?.Invoke(newUser);
//                 OnUserDataReady?.Invoke(newUser);
//                 Debug.Log("[AuthManager] New user created & ready.");
//             });
//     }

//     public void LoadUserData(string uid)
//     {
//         dbRef.Child("users").Child(uid).GetValueAsync()
//             .ContinueWithOnMainThread(task =>
//             {
//                 if (task.IsFaulted)
//                 {
//                     OnAuthError?.Invoke("Không thể tải dữ liệu. Vui lòng thử lại.");
//                     Debug.LogError($"[AuthManager] LoadUserData failed: {task.Exception?.Message}");
//                     return;
//                 }

//                 var snapshot = task.Result;

//                 if (!snapshot.Exists)
//                 {
//                     SaveNewUserToDatabase(uid, "", "");
//                     return;
//                 }

//                 UserData userData = JsonUtility.FromJson<UserData>(snapshot.GetRawJsonValue());

//                 CurrentUserData    = userData;
//                 CurrentUserRuntime = userData;

//                 Debug.Log($"[AuthManager] UserDataReady: level={userData.level} xp={userData.xp} coins={userData.coins}");

//                 // Thứ tự quan trọng: legacy trước, mới sau
//                 OnLoginSuccess?.Invoke(userData);
//                 OnUserDataReady?.Invoke(userData);
//             });
//     }

//     /// <summary>
//     /// Pull data mới nhất từ Firebase — gọi khi quay về Bedroom hoặc cần refresh UI.
//     /// Sau khi xong sẽ fire OnUserDataReady → toàn bộ UI tự cập nhật.
//     /// </summary>
//     public void RefreshUserData()
//     {
//         if (currentUser == null)
//         {
//             Debug.LogWarning("[AuthManager] RefreshUserData: chưa đăng nhập.");
//             return;
//         }
//         Debug.Log("[AuthManager] RefreshUserData — pulling Firebase...");
//         LoadUserData(currentUser.UserId);
//     }

//     // =========================================================================
//     // UPDATE USER
//     // =========================================================================
//     public void UpdateFullUserData(UserData user, int newWordsLearned = 0)
//     {
//         if (user == null) return;

//         string uid = currentUser?.UserId;
//         if (string.IsNullOrEmpty(uid)) return;

//         user.wordsLearned  += newWordsLearned;
//         CurrentUserRuntime  = user;
//         CurrentUserData     = user;

//         var updates = new Dictionary<string, object>
//         {
//             ["coins"]         = user.coins,
//             ["xp"]            = user.xp,
//             ["level"]         = user.level,
//             ["wordsLearned"]  = user.wordsLearned,
//             ["medals/bronze"] = user.bronzeMedals,
//             ["medals/silver"] = user.silverMedals,
//             ["medals/gold"]   = user.goldMedals
//         };

//         FirebaseDatabase.DefaultInstance
//             .GetReference($"users/{uid}")
//             .UpdateChildrenAsync(updates)
//             .ContinueWithOnMainThread(task =>
//             {
//                 if (task.IsFaulted)
//                 {
//                     Debug.LogError($"[AuthManager] UpdateFullUserData failed: {task.Exception?.Message}");
//                     return;
//                 }

//                 // Thông báo UI sau khi Firebase confirm save
//                 OnUserDataReady?.Invoke(user);
//                 OnUserDataUpdated?.Invoke(user);
//                 Debug.Log($"[AuthManager] Saved OK → Level={user.level} XP={user.xp} Coins={user.coins}");
//             });
//     }

//     // =========================================================================
//     // ERROR HELPER — an toàn, không crash dù Firebase throw bất kỳ loại gì
//     // =========================================================================
//     private string ParseFirebaseError(AggregateException aggEx)
//     {
//         if (aggEx == null) return "Đã xảy ra lỗi không xác định.";

//         string msg = "";

//         // Duyệt tất cả inner exceptions, an toàn
//         try
//         {
//             Exception inner = aggEx.InnerException;
//             while (inner?.InnerException != null)
//                 inner = inner.InnerException;

//             msg = inner?.Message ?? aggEx.Message ?? "";
//         }
//         catch (Exception ex)
//         {
//             Debug.LogWarning($"[AuthManager] ParseFirebaseError fallback: {ex.Message}");
//             return "Đăng nhập thất bại. Vui lòng thử lại.";
//         }

//         Debug.Log($"[AuthManager] Raw Firebase error: {msg}");

//         string s = msg.ToLower();

//         if (s.Contains("no user record") || s.Contains("user-not-found") ||
//             s.Contains("there is no user"))
//             return "Email này chưa được đăng ký. Vui lòng tạo tài khoản mới.";

//         if (s.Contains("wrong-password") || s.Contains("invalid-credential") ||
//             s.Contains("invalid credential") || s.Contains("password is invalid") ||
//             s.Contains("invalid login"))
//             return "Email hoặc mật khẩu không đúng. Vui lòng thử lại.";

//         if (s.Contains("too-many-requests") || s.Contains("too many") ||
//             s.Contains("access to this account has been temporarily"))
//             return "Đăng nhập sai quá nhiều lần. Vui lòng thử lại sau ít phút.";

//         if (s.Contains("email-already-in-use") || s.Contains("already in use"))
//             return "Email này đã được sử dụng. Vui lòng dùng email khác.";

//         if (s.Contains("invalid-email") || s.Contains("badly formatted"))
//             return "Địa chỉ email không hợp lệ. Vui lòng kiểm tra lại.";

//         if (s.Contains("weak-password") || s.Contains("password should be at least"))
//             return "Mật khẩu quá yếu. Vui lòng dùng ít nhất 6 ký tự.";

//         if (s.Contains("user-disabled"))
//             return "Tài khoản này đã bị vô hiệu hóa.";

//         if (s.Contains("network") || s.Contains("unable to resolve") ||
//             s.Contains("network_error") || s.Contains("connection"))
//             return "Lỗi kết nối mạng. Vui lòng kiểm tra internet.";

//         if (s.Contains("internal error") || s.Contains("an internal"))
//             return "Lỗi máy chủ Firebase. Vui lòng thử lại sau.";

//         // Fallback an toàn
//         if (msg.Length > 100) msg = msg.Substring(0, 100) + "...";
//         return $"Đăng nhập thất bại: {msg}";
//     }
// }