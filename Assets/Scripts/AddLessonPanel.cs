using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Firebase.Database;
using Firebase.Extensions;

// ─────────────────────────────────────────────────────────────────────────────
// AddLessonPanel.cs — v7
//
// THAY ĐỔI SO VỚI v6:
//   • Lưu từ vựng vào  lessons/{lessonId}/words/{wordId}   (thay vì vocabulary/)
//   • KHÔNG còn ghi vào node "vocabulary" nữa
//   • wordCount vẫn lưu trong lessons/{lessonId}/wordCount
// ─────────────────────────────────────────────────────────────────────────────

public class AddLessonPanel : MonoBehaviour
{
    [Header("Input ten bai")]
    [SerializeField] private TMP_InputField lessonNameInput;

    [Header("ScrollRect bao WordList (chi scroll phan tu, khong bao InputField)")]
    [SerializeField] private ScrollRect wordScrollRect;

    [Header("Content chua cac WordRow (VerticalLayoutGroup + ContentSizeFitter)")]
    [SerializeField] private Transform wordListContent;

    [Header("Buttons")]
    [SerializeField] private Button addRowButton;
    [SerializeField] private Button saveButton;
    [SerializeField] private Button closeButton;

    [Header("Feedback")]
    [SerializeField] private TextMeshProUGUI statusText;

    // ── Màu ──────────────────────────────────────────────────────────────────
    private static readonly Color COL_ROW_ODD  = new Color(0.96f, 0.96f, 0.96f, 1f);
    private static readonly Color COL_ROW_EVEN = new Color(0.89f, 0.89f, 0.89f, 1f);
    private static readonly Color COL_DEL      = new Color(0.88f, 0.28f, 0.28f, 1f);

    // ── Runtime ───────────────────────────────────────────────────────────────
    private readonly List<(TMP_InputField eng, TMP_InputField viet, GameObject row)> rows
        = new List<(TMP_InputField, TMP_InputField, GameObject)>();

    // ═════════════════════════════════════════════════════════════════════════
    private void Awake()
    {
        if (addRowButton != null) addRowButton.onClick.AddListener(AddRow);
        if (saveButton   != null) saveButton.onClick.AddListener(OnSave);
        if (closeButton  != null) closeButton.onClick.AddListener(ClosePanel);
    }

    private void OnEnable()
    {
        ResetPanel();
        AddRow(); // 1 dòng trống sẵn
    }

    // ═════════════════════════════════════════════════════════════════════════
    // THÊM / XÓA DÒNG — giữ nguyên logic cũ
    // ═════════════════════════════════════════════════════════════════════════
    public void AddRow()
    {
        if (wordListContent == null)
        {
            Debug.LogError("[AddLessonPanel] wordListContent chua gan!");
            return;
        }

        Color bgColor = rows.Count % 2 == 0 ? COL_ROW_ODD : COL_ROW_EVEN;
        var (rowGO, engField, vietField) = CreateWordRow(bgColor, rows.Count + 1);
        rowGO.transform.SetParent(wordListContent, false);

        var delBtn = rowGO.transform.Find("DeleteButton")?.GetComponent<Button>();
        if (delBtn != null)
        {
            var captured = rowGO;
            delBtn.onClick.AddListener(() => RemoveRow(captured));
        }

        rows.Add((engField, vietField, rowGO));
        engField?.ActivateInputField();
        SetStatus("", Color.white);

        if (wordScrollRect != null)
            Canvas.ForceUpdateCanvases();
    }

    private void RemoveRow(GameObject rowGO)
    {
        rows.RemoveAll(r => r.row == rowGO);
        Destroy(rowGO);
        for (int i = 0; i < rows.Count; i++)
        {
            var img = rows[i].row.GetComponent<Image>();
            if (img != null)
                img.color = i % 2 == 0 ? COL_ROW_ODD : COL_ROW_EVEN;
        }
    }

    // ═════════════════════════════════════════════════════════════════════════
    // LƯU FIREBASE
    // ─────────────────────────────────────────────────────────────────────────
    // CẤU TRÚC MỚI:
    //   lessons/
    //     {lessonId}/
    //       name        : "Fruit"
    //       wordCount   : 3
    //       createdAt   : "..."
    //       words/
    //         {wordId}/
    //           english    : "apple"
    //           vietnamese : "táo"
    // ═════════════════════════════════════════════════════════════════════════
    private void OnSave()
    {
           Debug.Log($"[AddLessonPanel] Firebase init: {FirebaseManager.Instance?.IsInitialized} | " +
              $"Logged in: {AuthManager.Instance?.IsLoggedIn} | " +
              $"User: {AuthManager.Instance?.CurrentUser?.UserId}");

    if (FirebaseManager.Instance == null || !FirebaseManager.Instance.IsInitialized)
    {
        SetStatus("(!) Firebase chua san sang!", Color.red);
        return;
    }
    if (AuthManager.Instance == null || !AuthManager.Instance.IsLoggedIn)
    {
        SetStatus("(!) Chua dang nhap!", Color.red);
        return;
    }
    
        var lessonName = lessonNameInput?.text.Trim() ?? "";
        if (string.IsNullOrEmpty(lessonName))
        {
            SetStatus("(!) Nhap ten bai hoc!", Color.red);
            return;
        }

        var validWords = new List<(string eng, string viet)>();
        foreach (var row in rows)
        {
            var eng  = row.eng?.text.Trim()  ?? "";
            var viet = row.viet?.text.Trim() ?? "";
            if (!string.IsNullOrEmpty(eng) && !string.IsNullOrEmpty(viet))
                validWords.Add((eng, viet));
        }

        if (validWords.Count == 0)
{
    SetStatus("(!) Chưa có từ nào hợp lệ!", Color.red);
    return;
}

if (validWords.Count < 9)
{
    SetStatus($"(!) ần nhập ít nhất 9 từ! (hiện tại: {validWords.Count}/9)", Color.red);
    return;
}

        saveButton.interactable = false;
        SetStatus("Đang Lưu...", Color.yellow);

        var db        = FirebaseDatabase.DefaultInstance.RootReference;
        var lessonRef = db.Child("lessons").Push();
        var lessonId  = lessonRef.Key;

        // ── Bước 1: Tạo node lesson (metadata) ───────────────────────────────
        lessonRef.SetValueAsync(new Dictionary<string, object>
        {
            { "name",      lessonName       },
            { "wordCount", validWords.Count },
            { "createdAt", DateTime.UtcNow.ToString("o") }
        })
        .ContinueWithOnMainThread(lt =>
{
    if (lt.IsFaulted)
    {
        // Lấy toàn bộ exception chain
        Exception ex = lt.Exception;
        while (ex?.InnerException != null) ex = ex.InnerException;
        var err = ex?.Message ?? "unknown";
        Debug.LogError("[AddLessonPanel]  Lỗi bước 1 chi tiết: " + err);
        Debug.LogError("[AddLessonPanel]  Full exception: " + lt.Exception?.ToString());
        SetStatus("Loi luu bai: " + err, Color.red);
        saveButton.interactable = true;
        return;
    }

            // ── Bước 2: Lưu từng từ vào lessons/{lessonId}/words/{wordId} ───
            var updates = new Dictionary<string, object>();
            foreach (var (eng, viet) in validWords)
            {
                // Push() tạo key ngẫu nhiên cho mỗi từ
                var wordKey = db.Child("lessons").Child(lessonId).Child("words").Push().Key;
                updates[$"lessons/{lessonId}/words/{wordKey}/english"]    = eng;
                updates[$"lessons/{lessonId}/words/{wordKey}/vietnamese"] = viet;
            }

            db.UpdateChildrenAsync(updates).ContinueWithOnMainThread(vt =>
            {
                if (vt.IsFaulted)
                {
                    SetStatus("Loi luu tu vung!", Color.red);
                }
                else
                {
                    SetStatus($"Đã lưu '{lessonName}' ({validWords.Count} bài)!", Color.green);
                    VocabManager.Instance?.LoadLessonsFromFirebase();
                    Invoke(nameof(ClosePanel), 1.2f);
                }
                saveButton.interactable = true;
            });
        });
    }

    // ═════════════════════════════════════════════════════════════════════════
    // TẠO WORDROW BẰNG CODE — giữ nguyên v6
    // ═════════════════════════════════════════════════════════════════════════
    private static (GameObject row, TMP_InputField eng, TMP_InputField viet)
        CreateWordRow(Color bgColor, int number)
    {
        var rowGO = new GameObject($"WordRow_{number}", typeof(RectTransform));
        var rowImg = rowGO.AddComponent<Image>();
        rowImg.color = bgColor;

        var le = rowGO.AddComponent<LayoutElement>();
        le.minHeight       = 52;
        le.preferredHeight = 52;
        le.flexibleWidth   = 1;

        var hlg = rowGO.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing                = 6;
        hlg.padding                = new RectOffset(8, 8, 6, 6);
        hlg.childForceExpandWidth  = true;
        hlg.childForceExpandHeight = true;
        hlg.childControlWidth      = true;
        hlg.childControlHeight     = true;
        hlg.childAlignment         = TextAnchor.MiddleCenter;

        var engField  = MakeInputField(rowGO.transform, "Field_English",    "Tiếng Anh...");
        var vietField = MakeInputField(rowGO.transform, "Field_Vietnamese", "Tiếng Việt...");

        var delGO = new GameObject("DeleteButton", typeof(RectTransform));
        delGO.transform.SetParent(rowGO.transform, false);
        delGO.AddComponent<Image>().color = COL_DEL;
        delGO.AddComponent<Button>();

        var delLE = delGO.AddComponent<LayoutElement>();
        delLE.minWidth        = 36;
        delLE.preferredWidth  = 36;
        delLE.minHeight       = 36;
        delLE.preferredHeight = 36;
        delLE.flexibleWidth   = 0;
        delLE.flexibleHeight  = 0;

        var lbl   = new GameObject("Label", typeof(RectTransform));
        lbl.transform.SetParent(delGO.transform, false);
        var lblRT = lbl.GetComponent<RectTransform>();
        lblRT.anchorMin = Vector2.zero; lblRT.anchorMax = Vector2.one;
        lblRT.offsetMin = lblRT.offsetMax = Vector2.zero;
        var lblTMP = lbl.AddComponent<TextMeshProUGUI>();
        lblTMP.text      = "X";
        lblTMP.fontSize  = 14;
        lblTMP.fontStyle = FontStyles.Bold;
        lblTMP.color     = Color.white;
        lblTMP.alignment = TextAlignmentOptions.Center;

        return (rowGO, engField, vietField);
    }

    private static TMP_InputField MakeInputField(Transform parent, string name, string ph)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        go.AddComponent<Image>().color = Color.white;

        var le = go.AddComponent<LayoutElement>();
        le.flexibleWidth  = 1;
        le.minWidth       = 50;
        le.flexibleHeight = 1;

        var field = go.AddComponent<TMP_InputField>();

        var ta   = new GameObject("Text Area", typeof(RectTransform));
        ta.transform.SetParent(go.transform, false);
        var taRT = ta.GetComponent<RectTransform>();
        taRT.anchorMin = Vector2.zero; taRT.anchorMax = Vector2.one;
        taRT.offsetMin = new Vector2(6, 2); taRT.offsetMax = new Vector2(-6, -2);
        ta.AddComponent<RectMask2D>();

        var phGO = new GameObject("Placeholder", typeof(RectTransform));
        phGO.transform.SetParent(ta.transform, false);
        Stretch(phGO.GetComponent<RectTransform>());
        var phT  = phGO.AddComponent<TextMeshProUGUI>();
        phT.text      = ph;
        phT.fontSize  = 13;
        phT.color     = new Color(0.6f, 0.6f, 0.6f);
        phT.fontStyle = FontStyles.Italic;
        phT.alignment = TextAlignmentOptions.MidlineLeft;

        var txtGO = new GameObject("Text", typeof(RectTransform));
        txtGO.transform.SetParent(ta.transform, false);
        Stretch(txtGO.GetComponent<RectTransform>());
        var txtT  = txtGO.AddComponent<TextMeshProUGUI>();
        txtT.fontSize  = 13;
        txtT.color     = Color.black;
        txtT.alignment = TextAlignmentOptions.MidlineLeft;

        field.textViewport  = taRT;
        field.textComponent = txtT;
        field.placeholder   = phT;
        return field;
    }

    private static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    private void SetStatus(string msg, Color color)
    {
        if (statusText == null) return;
        statusText.text  = msg;
        statusText.color = color;
    }

    private void ResetPanel()
    {
        foreach (var r in rows)
            if (r.row != null) Destroy(r.row);
        rows.Clear();

        if (lessonNameInput != null) lessonNameInput.text = "";
        if (statusText      != null) statusText.text      = "";
        if (saveButton      != null) saveButton.interactable = true;
    }

    public void ClosePanel() => gameObject.SetActive(false);
}