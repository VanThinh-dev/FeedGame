using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using Firebase.Database;
using Firebase.Extensions;

public class AddLessonPanel : MonoBehaviour
{
    [Header("Input ten bai")]
    [SerializeField] private TMP_InputField inputField_LessonName;

    [Header("ScrollRect bao WordList")]
    [SerializeField] private ScrollRect wordScrollRect;

    [Header("Content chua cac WordRow")]
    [SerializeField] private Transform wordListContent;

    [Header("Prefab WordRow (Builder tu dong gan)")]
    [SerializeField] private GameObject wordRowPrefab;

    [Header("Buttons")]
    [SerializeField] private Button addRowButton;
    [SerializeField] private Button saveButton;
    [SerializeField] private Button closeButton;

    [Header("Feedback")]
    [SerializeField] private TextMeshProUGUI statusText;

    private static readonly Color COL_ROW_ODD  = new Color(0.96f, 0.96f, 0.96f, 1f);
    private static readonly Color COL_ROW_EVEN = new Color(0.89f, 0.89f, 0.89f, 1f);

    // Sprite tron dung chung cho tat ca DeleteButton, tao 1 lan
    private static Sprite _circleSprite;

    private readonly List<(TMP_InputField eng, TMP_InputField viet, GameObject row)> rows
        = new List<(TMP_InputField, TMP_InputField, GameObject)>();

    private TMP_InputField _tabFocused;

    private void Awake()
    {
        _circleSprite = CreateCircleSprite(64);

        if (addRowButton != null) addRowButton.onClick.AddListener(AddRow);
        if (saveButton   != null) saveButton.onClick.AddListener(OnSave);
        if (closeButton  != null) closeButton.onClick.AddListener(ClosePanel);
    }

    private void OnEnable()
    {
        ResetPanel();
        AddRow();
    }

    private void Update()
    {
        if (_tabFocused == null) return;
        if (!Input.GetKeyDown(KeyCode.Tab)) return;
        _tabFocused.onSubmit.Invoke(_tabFocused.text);
    }

    public void AddRow()
    {
        if (wordListContent == null) { Debug.LogError("[AddLessonPanel] wordListContent chua gan!"); return; }
        if (wordRowPrefab   == null) { Debug.LogError("[AddLessonPanel] wordRowPrefab chua gan! Chay Tools > AddLessonPanel Builder."); return; }

        var rowGO = Instantiate(wordRowPrefab, wordListContent);
        rowGO.name = $"WordRow_{rows.Count + 1}";

        var img = rowGO.GetComponent<Image>();
        if (img != null) img.color = rows.Count % 2 == 0 ? COL_ROW_ODD : COL_ROW_EVEN;

        var eng  = rowGO.transform.Find("Field_English")?.GetComponent<TMP_InputField>();
        var viet = rowGO.transform.Find("Field_Vietnamese")?.GetComponent<TMP_InputField>();

        if (eng == null || viet == null)
        {
            Debug.LogError("[AddLessonPanel] Prefab thieu Field_English hoac Field_Vietnamese!");
            Destroy(rowGO);
            return;
        }

        eng.text  = "";
        viet.text = "";

        // Style nut xoa thanh hinh tron do
        var delBtn = rowGO.transform.Find("DeleteButton")?.GetComponent<Button>();
        if (delBtn != null)
        {
            StyleDeleteButton(delBtn);
            var cap = rowGO;
            delBtn.onClick.RemoveAllListeners();
            delBtn.onClick.AddListener(() => RemoveRow(cap));
        }

        rows.Add((eng, viet, rowGO));
        RebindAllTabNavigation();
        SetStatus("", Color.white);

        if (wordScrollRect != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(wordListContent.GetComponent<RectTransform>());
    }

    // -------------------------------------------------------------------------
    // Ve sprite hinh tron bang Texture2D (khong can asset)
    // -------------------------------------------------------------------------
    private static Sprite CreateCircleSprite(int size)
    {
        var tex    = new Texture2D(size, size, TextureFormat.RGBA32, false);
        var pixels = new Color32[size * size];
        float r    = size / 2f;
        float cx   = r, cy = r;

        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float dx   = x - cx + 0.5f;
            float dy   = y - cy + 0.5f;
            float dist = Mathf.Sqrt(dx * dx + dy * dy);
            // Anti-alias o bien
            float alpha = Mathf.Clamp01(r - dist);
            pixels[y * size + x] = new Color32(255, 255, 255, (byte)(alpha * 255));
        }

        tex.SetPixels32(pixels);
        tex.Apply();

        return Sprite.Create(
            tex,
            new Rect(0, 0, size, size),
            new Vector2(0.5f, 0.5f),
            size   // pixels per unit = size -> sprite co normalized size 1x1
        );
    }

    // -------------------------------------------------------------------------
    // Ap dung sprite tron + mau do len DeleteButton
    // -------------------------------------------------------------------------
    private static void StyleDeleteButton(Button btn)
    {
        var btnImg = btn.GetComponent<Image>();
        if (btnImg == null) btnImg = btn.gameObject.AddComponent<Image>();

        btnImg.sprite = _circleSprite;
        btnImg.color  = new Color(0.88f, 0.22f, 0.22f, 1f); // do tuoi
        btnImg.type   = Image.Type.Simple;
        btnImg.preserveAspect = true;

        // Transition mau khi hover / press
        var colors           = btn.colors;
        colors.normalColor   = new Color(0.88f, 0.22f, 0.22f, 1f);
        colors.highlightedColor = new Color(1f,  0.35f, 0.35f, 1f);
        colors.pressedColor  = new Color(0.60f, 0.10f, 0.10f, 1f);
        colors.selectedColor = colors.normalColor;
        btn.colors           = colors;

        // RectTransform: ep vuong 30x30 de tron deu
        var rect = btn.GetComponent<RectTransform>();
        if (rect != null) rect.sizeDelta = new Vector2(30f, 30f);

        // LayoutElement: giu chieu rong co dinh, khong bi gian
        var le = btn.GetComponent<LayoutElement>();
        if (le == null) le = btn.gameObject.AddComponent<LayoutElement>();
        le.minWidth       = 30f;
        le.preferredWidth = 30f;
        le.flexibleWidth  = 0f;

        // Text X: dam, can giua
        var textTMP = btn.GetComponentInChildren<TextMeshProUGUI>();
        if (textTMP != null)
        {
            textTMP.text      = "X";       // ky tu X dep hon
            textTMP.fontSize  = 13f;
            textTMP.fontStyle = FontStyles.Bold;
            textTMP.alignment = TextAlignmentOptions.Center;
            textTMP.color     = Color.white;
        }
    }

    private void RemoveRow(GameObject rowGO)
    {
        var e = rows.Find(r => r.row == rowGO);
        if (e.eng  != null) { e.eng.onSubmit.RemoveAllListeners();  e.eng.onSelect.RemoveAllListeners();  e.eng.onDeselect.RemoveAllListeners(); }
        if (e.viet != null) { e.viet.onSubmit.RemoveAllListeners(); e.viet.onSelect.RemoveAllListeners(); e.viet.onDeselect.RemoveAllListeners(); }
        rows.RemoveAll(r => r.row == rowGO);
        Destroy(rowGO);

        for (int i = 0; i < rows.Count; i++)
        {
            var img = rows[i].row.GetComponent<Image>();
            if (img) img.color = i % 2 == 0 ? COL_ROW_ODD : COL_ROW_EVEN;
        }

        RebindAllTabNavigation();
    }

    private void BindRowTabNavigation(int i)
    {
        if (i < 0 || i >= rows.Count) return;
        var (eng, viet, _) = rows[i];

        eng.onSubmit.RemoveAllListeners();
        eng.onSelect.RemoveAllListeners();
        eng.onDeselect.RemoveAllListeners();
        eng.onSubmit.AddListener(_ => viet.Select());
        eng.onSelect.AddListener(_ => _tabFocused = eng);
        eng.onDeselect.AddListener(_ => { if (_tabFocused == eng) _tabFocused = null; });

        viet.onSubmit.RemoveAllListeners();
        viet.onSelect.RemoveAllListeners();
        viet.onDeselect.RemoveAllListeners();
        viet.onSubmit.AddListener(_ =>
        {
            int next = i + 1;
            if (next < rows.Count) rows[next].eng.Select();
            else AddRow();
        });
        viet.onSelect.AddListener(_ => _tabFocused = viet);
        viet.onDeselect.AddListener(_ => { if (_tabFocused == viet) _tabFocused = null; });
    }

    private void RebindAllTabNavigation()
    {
        for (int i = 0; i < rows.Count; i++)
            BindRowTabNavigation(i);
    }

    private void OnSave()
    {
        if (FirebaseManager.Instance == null || !FirebaseManager.Instance.IsInitialized) { SetStatus("(!) Firebase chua san sang!", Color.red); return; }
        if (AuthManager.Instance == null || !AuthManager.Instance.IsLoggedIn) { SetStatus("(!) Chua dang nhap!", Color.red); return; }

        var lessonName = inputField_LessonName?.text.Trim() ?? "";
        if (string.IsNullOrEmpty(lessonName)) { SetStatus("(!) Nhap ten bai hoc!", Color.red); return; }

        var validWords = new List<(string eng, string viet)>();
        foreach (var row in rows)
        {
            var eng  = row.eng?.text.Trim()  ?? "";
            var viet = row.viet?.text.Trim() ?? "";
            if (!string.IsNullOrEmpty(eng) && !string.IsNullOrEmpty(viet))
                validWords.Add((eng, viet));
        }

        if (validWords.Count == 0) { SetStatus("(!) Chua co tu nao hop le!", Color.red); return; }
        if (validWords.Count < 9)  { SetStatus($"(!) Can it nhat 9 tu! ({validWords.Count}/9)", Color.red); return; }

        saveButton.interactable = false;
        SetStatus("Dang Luu...", Color.yellow);

        var db        = FirebaseDatabase.DefaultInstance.RootReference;
        var lessonRef = db.Child("lessons").Push();
        var lessonId  = lessonRef.Key;

        lessonRef.SetValueAsync(new Dictionary<string, object>
        {
            { "name",      lessonName                   },
            { "wordCount", validWords.Count              },
            { "createdAt", DateTime.UtcNow.ToString("o") }
        })
        .ContinueWithOnMainThread(lt =>
        {
            if (lt.IsFaulted)
            {
                Exception ex = lt.Exception;
                while (ex?.InnerException != null) ex = ex.InnerException;
                SetStatus("Loi luu bai: " + (ex?.Message ?? "unknown"), Color.red);
                saveButton.interactable = true;
                return;
            }

            var updates = new Dictionary<string, object>();
            foreach (var (eng, viet) in validWords)
            {
                var key = db.Child("lessons").Child(lessonId).Child("words").Push().Key;
                updates[$"lessons/{lessonId}/words/{key}/english"]   = eng;
                updates[$"lessons/{lessonId}/words/{key}/vietnamese"] = viet;
            }

            db.UpdateChildrenAsync(updates).ContinueWithOnMainThread(vt =>
            {
                if (vt.IsFaulted)
                    SetStatus("Loi luu tu vung!", Color.red);
                else
                {
                    SetStatus($"Da luu '{lessonName}' ({validWords.Count} tu)!", Color.green);
                    VocabManager.Instance?.LoadLessonsFromFirebase();
                    Invoke(nameof(ClosePanel), 1.2f);
                }
                saveButton.interactable = true;
            });
        });
    }

    private void SetStatus(string msg, Color color)
    {
        if (statusText == null) return;
        statusText.text  = msg;
        statusText.color = color;
    }

    private void ResetPanel()
    {
        foreach (var r in rows) if (r.row != null) Destroy(r.row);
        rows.Clear();
        _tabFocused = null;
        if (inputField_LessonName != null) inputField_LessonName.text = "";
        if (statusText            != null) statusText.text            = "";
        if (saveButton            != null) saveButton.interactable    = true;
    }

    public void ClosePanel() => gameObject.SetActive(false);
}