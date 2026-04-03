// (FULL FILE — AuthManager.cs)

using System;
using System.Collections.Generic;
using UnityEngine;
using Firebase.Auth;
using Firebase.Database;
using Firebase.Extensions;

public class AuthManager : MonoBehaviour
{
    public static AuthManager Instance;

    public event Action<UserData> OnLoginSuccess;
    public event Action<UserData> OnUserDataReady;
    public event Action<UserData> OnUserDataUpdated;
    public event Action<string>   OnAuthError;
    public event Action           OnLogout;

    public bool         IsInitialized   => isInitialized;
    public bool         IsLoggedIn      => currentUser != null;
    public FirebaseUser CurrentUser     => currentUser;

    public UserData CurrentUserData    { get; private set; }
    public UserData CurrentUserRuntime { get; private set; }

    private FirebaseAuth      auth;
    private DatabaseReference dbRef;
    private FirebaseUser      currentUser;
    private bool              isInitialized = false;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(this); return; }
    }

    private void Update()
    {
        if (isInitialized) return;
        if (FirebaseManager.Instance == null) return;
        if (!FirebaseManager.Instance.IsInitialized) return;
        SetupAuth();
    }

    private void OnDestroy()
    {
        if (auth != null)
            auth.StateChanged -= OnAuthStateChanged;
    }

    private void SetupAuth()
    {
        auth  = FirebaseAuth.DefaultInstance;
        dbRef = FirebaseDatabase.DefaultInstance.RootReference;
        isInitialized = true;
        auth.StateChanged += OnAuthStateChanged;
        Debug.Log("[AuthManager] Auth ready.");
    }

    private void OnAuthStateChanged(object sender, EventArgs e)
    {
        FirebaseUser newUser = auth.CurrentUser;

        if (newUser != null && currentUser == null)
        {
            currentUser = newUser;
            LoadUserData(currentUser.UserId);
        }
        else if (newUser == null && currentUser != null)
        {
            currentUser        = null;
            CurrentUserData    = null;
            CurrentUserRuntime = null;
            OnLogout?.Invoke();
        }
    }

    public void Register(string email, string password, string displayName)
    {
        if (!isInitialized) return;

        auth.CreateUserWithEmailAndPasswordAsync(email, password)
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted || task.IsCanceled)
                {
                    OnAuthError?.Invoke(ParseFirebaseError(task.Exception));
                    return;
                }

                currentUser = task.Result.User;
                SaveNewUserToDatabase(currentUser.UserId, email, displayName);
            });
    }

    public void Login(string email, string password)
    {
        if (!isInitialized) return;

        auth.SignInWithEmailAndPasswordAsync(email, password)
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted || task.IsCanceled)
                {
                    OnAuthError?.Invoke(ParseFirebaseError(task.Exception));
                }
            });
    }

    public void Logout() => auth?.SignOut();

    private void SaveNewUserToDatabase(string uid, string email, string displayName)
    {
        var newUser = new UserData
        {
            uid          = uid,
            email        = email,
            displayName  = displayName,
            level        = 1,
            xp           = 0,
            coins        = 0,
            wordsLearned = 0,
            createdAt    = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),

            bronzeMedals = 0,
            silverMedals = 0,
            goldMedals   = 0
        };

        var updates = new Dictionary<string, object>
        {
            ["uid"]          = newUser.uid,
            ["email"]        = newUser.email,
            ["displayName"]  = newUser.displayName,
            ["level"]        = newUser.level,
            ["xp"]           = newUser.xp,
            ["coins"]        = newUser.coins,
            ["wordsLearned"] = newUser.wordsLearned,
            ["createdAt"]    = newUser.createdAt,

            ["medals/bronze"] = 0,
            ["medals/silver"] = 0,
            ["medals/gold"]   = 0
        };

        dbRef.Child("users").Child(uid)
            .UpdateChildrenAsync(updates)
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted)
                {
                    OnAuthError?.Invoke("Không thể tạo tài khoản.");
                    return;
                }

                CurrentUserData    = newUser;
                CurrentUserRuntime = newUser;

                OnLoginSuccess?.Invoke(newUser);
                OnUserDataReady?.Invoke(newUser);
            });
    }

    public void LoadUserData(string uid)
    {
        dbRef.Child("users").Child(uid).GetValueAsync()
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted)
                {
                    OnAuthError?.Invoke("Không thể tải dữ liệu.");
                    return;
                }

                var snapshot = task.Result;

                if (!snapshot.Exists)
                {
                    SaveNewUserToDatabase(uid, "", "");
                    return;
                }

                UserData userData =
                    JsonUtility.FromJson<UserData>(snapshot.GetRawJsonValue());

                // ===== FIX: đọc nested medals =====

                var medals = snapshot.Child("medals");
                if (medals.Exists)
                {
                    userData.bronzeMedals =
                        int.Parse(medals.Child("bronze").Value?.ToString() ?? "0");

                    userData.silverMedals =
                        int.Parse(medals.Child("silver").Value?.ToString() ?? "0");

                    userData.goldMedals =
                        int.Parse(medals.Child("gold").Value?.ToString() ?? "0");
                }

                CurrentUserData    = userData;
                CurrentUserRuntime = userData;

                OnLoginSuccess?.Invoke(userData);
                OnUserDataReady?.Invoke(userData);
            });
    }

    public void RefreshUserData()
    {
        if (currentUser == null) return;
        LoadUserData(currentUser.UserId);
    }

    public void UpdateFullUserData(UserData user, int newWordsLearned = 0)
    {
        if (user == null) return;

        string uid = currentUser?.UserId;
        if (string.IsNullOrEmpty(uid)) return;

        user.wordsLearned += newWordsLearned;

        CurrentUserRuntime = user;
        CurrentUserData    = user;

        var updates = new Dictionary<string, object>
        {
            ["coins"]         = user.coins,
            ["xp"]            = user.xp,
            ["level"]         = user.level,
            ["wordsLearned"]  = user.wordsLearned,
            ["medals/bronze"] = user.bronzeMedals,
            ["medals/silver"] = user.silverMedals,
            ["medals/gold"]   = user.goldMedals
        };

        FirebaseDatabase.DefaultInstance
            .GetReference($"users/{uid}")
            .UpdateChildrenAsync(updates)
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted) return;

                OnUserDataReady?.Invoke(user);
                OnUserDataUpdated?.Invoke(user);
            });
    }

    private string ParseFirebaseError(AggregateException aggEx)
    {
        if (aggEx == null) return "Lỗi không xác định.";

        Exception inner = aggEx.InnerException;
        while (inner?.InnerException != null)
            inner = inner.InnerException;

        return inner?.Message ?? "Đăng nhập thất bại.";
    }
}