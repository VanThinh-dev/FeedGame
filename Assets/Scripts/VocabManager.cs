using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Firebase.Database;
using Firebase.Extensions;

// ─────────────────────────────────────────────────────────────────────────────
// VocabManager.cs — v5  (FIXED)
//
// FIX so với v4:
//   • LoadCompletedLessonsFromFirebase() đọc trực tiếp từ Firebase
//     thay vì dùng CurrentUserData (hay bị null/stale).
//   • Dùng callback chain: load completedIds XONG → mới load lessons
//     → đảm bảo isCompleted được gán đúng.
//   • Xoá daHocPanel / chuaHocPanel khỏi VocabManager — việc show/hide panel
//     chỉ do VocabCanvasController đảm nhiệm, tránh double-reference conflict.
//   • OpenVocabCanvas() reload completedIds từ Firebase trước khi build cards.
//   • MarkLessonCompleted() cập nhật local completedIds ngay lập tức rồi
//     rebuild cards để tab phản ánh đúng trạng thái.
// ─────────────────────────────────────────────────────────────────────────────

public class VocabManager : MonoBehaviour
{
    public static VocabManager Instance { get; private set; }

    [Header("Root canvas — kéo VocabCanvas vào đây")]
    [SerializeField] public GameObject vocabCanvas;

    [Header("ScrollView Content (có GridLayoutGroup)")]
    [SerializeField] public Transform daHocContent;
    [SerializeField] public Transform chuaHocContent;

    [Header("Prefab card — kéo LessonCardPrefab vào đây")]
    [SerializeField] public GameObject lessonCardPrefab;

    // ── Events ────────────────────────────────────────────────────────────────
    public event Action<List<LessonData>, List<LessonData>> OnLessonsLoaded;

    // ── Internal state ────────────────────────────────────────────────────────
    private List<LessonData> allLessons       = new List<LessonData>();
    private List<LessonData> completedLessons = new List<LessonData>();
    private List<LessonData> pendingLessons   = new List<LessonData>();
    private HashSet<string>  completedIds     = new HashSet<string>();

    // ═════════════════════════════════════════════════════════════════════════
    // LIFECYCLE
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

        Debug.Log("[VocabManager]  Sẵn sàng load lessons.");

        // Luôn load completedIds từ Firebase trước, sau đó mới load lessons
        LoadCompletedLessonsFromFirebase(onDone: LoadLessonsFromFirebase);
    }

    // ═════════════════════════════════════════════════════════════════════════
    // LOAD COMPLETED IDS — đọc thẳng từ Firebase (không dùng cache UserData)
    // ═════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Đọc node users/{uid}/completedLessons từ Firebase.
    /// Sau khi xong sẽ gọi onDone() nếu có.
    /// </summary>
    private void LoadCompletedLessonsFromFirebase(Action onDone = null)
    {
        var uid = AuthManager.Instance?.CurrentUser?.UserId;
        if (string.IsNullOrEmpty(uid))
        {
            Debug.LogWarning("[VocabManager] Chưa có uid, bỏ qua load completedLessons.");
            onDone?.Invoke();
            return;
        }

        FirebaseDatabase.DefaultInstance.RootReference
            .Child("users").Child(uid).Child("completedLessons")
            .GetValueAsync()
            .ContinueWithOnMainThread(task =>
            {
                completedIds.Clear();

                if (task.IsCompletedSuccessfully && task.Result.Exists)
                {
                    foreach (var child in task.Result.Children)
                    {
                        // Giá trị true hoặc key tồn tại đều tính là completed
                        if (child.Value?.ToString() == "true" || child.Exists)
                            completedIds.Add(child.Key);
                    }
                }

                Debug.Log($"[VocabManager] completedIds từ Firebase: {completedIds.Count}");
                onDone?.Invoke();
            });
    }

    // ═════════════════════════════════════════════════════════════════════════
    // LOAD LESSONS — chỉ gọi SAU KHI completedIds đã sẵn sàng
    // ═════════════════════════════════════════════════════════════════════════

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
                        wordCount = int.TryParse(
                                        child.Child("wordCount").Value?.ToString(),
                                        out int wc) ? wc : 0,
                        createdAt = child.Child("createdAt").Value?.ToString() ?? ""
                    };

                    // Fallback: đếm số từ trong node words nếu wordCount == 0
                    if (lesson.wordCount == 0 && child.Child("words").ChildrenCount > 0)
                        lesson.wordCount = (int)child.Child("words").ChildrenCount;

                    // ✅ FIX: dùng completedIds đã load từ Firebase
                    lesson.isCompleted = completedIds.Contains(lesson.id);
                    allLessons.Add(lesson);

                    if (lesson.isCompleted) completedLessons.Add(lesson);
                    else                    pendingLessons.Add(lesson);
                }

                Debug.Log($"[VocabManager] Tổng: {allLessons.Count} " +
                          $"| Đã học: {completedLessons.Count} " +
                          $"| Chưa học: {pendingLessons.Count}");

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
            Debug.LogWarning("[VocabManager]  lessonCardPrefab chưa gán!");
            return;
        }
        BuildCardList(daHocContent,   completedLessons);
        BuildCardList(chuaHocContent, pendingLessons);
    }

    private void BuildCardList(Transform container, List<LessonData> lessons)
    {
        if (container == null) return;

        // Xoá card cũ
        for (int i = container.childCount - 1; i >= 0; i--)
            Destroy(container.GetChild(i).gameObject);

        foreach (var lesson in lessons)
        {
            var go   = Instantiate(lessonCardPrefab, container);
            var card = go.GetComponent<LessonCard>();
            if (card != null)
                card.Setup(lesson, OnLessonCardTapped);
            else
                Debug.LogWarning("[VocabManager] LessonCardPrefab thiếu component LessonCard!");
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

                // Thứ tự quan trọng:
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

        // Cập nhật local ngay lập tức
        completedIds.Add(lessonId);

        // Cập nhật card trong danh sách allLessons
        foreach (var lesson in allLessons)
        {
            if (lesson.id == lessonId)
            {
                lesson.isCompleted = true;
                break;
            }
        }

        var uid = AuthManager.Instance?.CurrentUser?.UserId;
        if (string.IsNullOrEmpty(uid)) return;

        // Ghi lên Firebase
        FirebaseDatabase.DefaultInstance.RootReference
            .Child("users").Child(uid).Child("completedLessons").Child(lessonId)
            .SetValueAsync(true)
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsCompletedSuccessfully)
                    Debug.Log($"[VocabManager]  Đánh dấu hoàn thành: {lessonId}");
                else
                    Debug.LogError($"[VocabManager]  Lỗi ghi completedLesson: {task.Exception}");
            });

        // Rebuild cards nếu canvas đang mở để tab phản ánh đúng
        if (vocabCanvas != null && vocabCanvas.activeSelf)
            LoadLessonsFromFirebase();
    }

    // ═════════════════════════════════════════════════════════════════════════
    // OPEN / CLOSE
    // ═════════════════════════════════════════════════════════════════════════

    public void OpenVocabCanvas()
    {
        if (vocabCanvas == null)
        {
            Debug.LogWarning("[VocabManager] vocabCanvas chưa gán!");
            return;
        }

        vocabCanvas.SetActive(true);

        // ✅ FIX: load completedIds mới nhất từ Firebase TRƯỚC khi build cards
        LoadCompletedLessonsFromFirebase(onDone: LoadLessonsFromFirebase);
    }

    public void CloseVocabCanvas()
    {
        if (vocabCanvas != null) vocabCanvas.SetActive(false);
    }

    // ── Tab switching — giữ lại để VocabCanvasController vẫn gọi được ─────────
    // ✅ FIX: bỏ việc bật/tắt daHocPanel/chuaHocPanel ở đây.
    //    VocabCanvasController sẽ là nơi duy nhất điều khiển panels.
    //    Hai hàm này giữ lại để không breaking nếu code khác đang gọi.
    public void ShowTabDaHoc()   { /* Panel switching handled by VocabCanvasController */ }
    public void ShowTabChuaHoc() { /* Panel switching handled by VocabCanvasController */ }
}