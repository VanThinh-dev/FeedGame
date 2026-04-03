using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Firebase.Database;
using Firebase.Extensions;

// ─────────────────────────────────────────────────────────────────────────────
// VocabManager.cs — v4
//
// FIX so với v3:
//   • LoadVocabForLesson() gọi BedroomManager.EnterGameplay() TRƯỚC khi
//     StartGameWithVocab() → tránh BedroomCanvas che gameplay
// ─────────────────────────────────────────────────────────────────────────────

public class VocabManager : MonoBehaviour
{
    public static VocabManager Instance { get; private set; }

    [Header("Root canvas — kéo VocabCanvas vào đây")]
    [SerializeField] public GameObject vocabCanvas;

    [Header("Tab Panels")]
    [SerializeField] public GameObject daHocPanel;
    [SerializeField] public GameObject chuaHocPanel;

    [Header("ScrollView Content (có GridLayoutGroup)")]
    [SerializeField] public Transform daHocContent;
    [SerializeField] public Transform chuaHocContent;

    [Header("Prefab card — kéo LessonCardPrefab vào đây")]
    [SerializeField] public GameObject lessonCardPrefab;

    public event Action<List<LessonData>, List<LessonData>> OnLessonsLoaded;

    private List<LessonData> allLessons       = new List<LessonData>();
    private List<LessonData> completedLessons = new List<LessonData>();
    private List<LessonData> pendingLessons   = new List<LessonData>();
    private HashSet<string>  completedIds     = new HashSet<string>();

    // ═════════════════════════════════════════════════════════════════════════
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start() => StartCoroutine(WaitAndLoad());

    private IEnumerator WaitAndLoad()
    {
        while (FirebaseManager.Instance == null || !FirebaseManager.Instance.IsInitialized)
            yield return null;
        while (AuthManager.Instance == null || !AuthManager.Instance.IsInitialized)
            yield return null;
        while (!AuthManager.Instance.IsLoggedIn)
            yield return null;

        Debug.Log("[VocabManager] ✅ Sẵn sàng load lessons.");
        LoadCompletedLessonsFromUser();
        LoadLessonsFromFirebase();
    }

    // ═════════════════════════════════════════════════════════════════════════
    // LOAD DATA
    // ═════════════════════════════════════════════════════════════════════════

    private void LoadCompletedLessonsFromUser()
    {
        completedIds.Clear();
        var userData = AuthManager.Instance?.CurrentUserData;
        if (userData?.completedLessons != null)
            foreach (var id in userData.completedLessons)
                completedIds.Add(id);

        Debug.Log($"[VocabManager] completedIds: {completedIds.Count}");
    }

    public void LoadLessonsFromFirebase()
    {
        if (!FirebaseManager.Instance.IsInitialized) return;

        FirebaseDatabase.DefaultInstance.RootReference
            .Child("lessons")
            .GetValueAsync()
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted || task.IsCanceled)
                {
                    Debug.LogError("[VocabManager] Load lessons lỗi: " + task.Exception);
                    return;
                }

                allLessons.Clear();
                completedLessons.Clear();
                pendingLessons.Clear();

                foreach (var child in task.Result.Children)
                {
                    if (child.Key == "words") continue;

                    var lesson = new LessonData
                    {
                        id        = child.Key,
                        name      = child.Child("name").Value?.ToString()      ?? "Bài học",
                        wordCount = int.TryParse(child.Child("wordCount").Value?.ToString(), out int wc) ? wc : 0,
                        createdAt = child.Child("createdAt").Value?.ToString() ?? ""
                    };

                    if (lesson.wordCount == 0 && child.Child("words").ChildrenCount > 0)
                        lesson.wordCount = (int)child.Child("words").ChildrenCount;

                    lesson.isCompleted = completedIds.Contains(lesson.id);
                    allLessons.Add(lesson);

                    if (lesson.isCompleted) completedLessons.Add(lesson);
                    else                    pendingLessons.Add(lesson);
                }

                Debug.Log($"[VocabManager] Tổng: {allLessons.Count} | Đã học: {completedLessons.Count} | Chưa học: {pendingLessons.Count}");

                BuildLessonCards();
                OnLessonsLoaded?.Invoke(completedLessons, pendingLessons);
            });
    }

    // ═════════════════════════════════════════════════════════════════════════
    // BUILD CARDS
    // ═════════════════════════════════════════════════════════════════════════

    private void BuildLessonCards()
    {
        if (lessonCardPrefab == null)
        {
            Debug.LogWarning("[VocabManager] ❌ lessonCardPrefab chưa gán!");
            return;
        }
        BuildCardList(daHocContent,   completedLessons);
        BuildCardList(chuaHocContent, pendingLessons);
    }

    private void BuildCardList(Transform container, List<LessonData> lessons)
    {
        if (container == null) return;

        foreach (Transform child in container)
            Destroy(child.gameObject);

        foreach (var lesson in lessons)
        {
            var go   = Instantiate(lessonCardPrefab, container);
            var card = go.GetComponent<LessonCard>();
            if (card != null) card.Setup(lesson, OnLessonCardTapped);
        }
    }

    // ═════════════════════════════════════════════════════════════════════════
    // LESSON SELECTION
    // ═════════════════════════════════════════════════════════════════════════

    private void OnLessonCardTapped(LessonData lesson)
    {
        Debug.Log($"[VocabManager] Chọn bài: {lesson.name} (id={lesson.id})");
        LoadVocabForLesson(lesson.id);
    }

    public void LoadVocabForLesson(string lessonId)
    {
        Debug.Log($"[VocabManager] Load vocab cho lesson: {lessonId}");

        FirebaseDatabase.DefaultInstance.RootReference
            .Child("lessons")
            .Child(lessonId)
            .Child("words")
            .GetValueAsync()
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted || task.IsCanceled)
                {
                    Debug.LogError("[VocabManager] Load vocab lỗi: " + task.Exception);
                    return;
                }

                var words = new List<VocabWord>();
                foreach (var child in task.Result.Children)
                {
                    var w = new VocabWord
                    {
                        id         = child.Key,
                        english    = child.Child("english").Value?.ToString()    ?? "",
                        vietnamese = child.Child("vietnamese").Value?.ToString() ?? "",
                        lessonId   = lessonId
                    };
                    if (!string.IsNullOrEmpty(w.english)) words.Add(w);
                }

                if (words.Count == 0)
                {
                    Debug.LogWarning($"[VocabManager] Bài '{lessonId}' chưa có từ vựng!");
                    return;
                }

                Debug.Log($"[VocabManager] Load được {words.Count} từ → chuyển sang gameplay.");

                // ── Thứ tự quan trọng ─────────────────────────────────────
                // 1. Đóng VocabCanvas
                CloseVocabCanvas();
                // 2. Ẩn BedroomCanvas (tránh che gameplay)
                BedroomManager.Instance?.EnterGameplay();
                // 3. Bắt đầu game
                GameManager.Instance?.StartGameWithVocab(words);
            });
    }

    // ═════════════════════════════════════════════════════════════════════════
    // MARK COMPLETED
    // ═════════════════════════════════════════════════════════════════════════

    public void MarkLessonCompleted(string lessonId)
    {
        if (completedIds.Contains(lessonId)) return;
        completedIds.Add(lessonId);

        var uid = AuthManager.Instance?.CurrentUser?.UserId;
        if (string.IsNullOrEmpty(uid)) return;

        FirebaseDatabase.DefaultInstance.RootReference
            .Child("users").Child(uid).Child("completedLessons").Child(lessonId)
            .SetValueAsync(true)
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsCompletedSuccessfully)
                    Debug.Log($"[VocabManager] ✅ Đánh dấu hoàn thành: {lessonId}");
            });

        if (vocabCanvas != null && vocabCanvas.activeSelf)
            LoadLessonsFromFirebase();
    }

    // ═════════════════════════════════════════════════════════════════════════
    // OPEN / CLOSE
    // ═════════════════════════════════════════════════════════════════════════

    public void OpenVocabCanvas()
    {
        if (vocabCanvas == null) { Debug.LogWarning("[VocabManager] vocabCanvas chưa gán!"); return; }
        vocabCanvas.SetActive(true);
        ShowTabChuaHoc();
        LoadCompletedLessonsFromUser();
        LoadLessonsFromFirebase();
    }

    public void CloseVocabCanvas()
    {
        if (vocabCanvas != null) vocabCanvas.SetActive(false);
    }

    public void ShowTabDaHoc()
    {
        if (daHocPanel   != null) daHocPanel.SetActive(true);
        if (chuaHocPanel != null) chuaHocPanel.SetActive(false);
    }

    public void ShowTabChuaHoc()
    {
        if (daHocPanel   != null) daHocPanel.SetActive(false);
        if (chuaHocPanel != null) chuaHocPanel.SetActive(true);
    }
}