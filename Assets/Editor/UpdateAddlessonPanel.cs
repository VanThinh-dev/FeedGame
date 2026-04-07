using UnityEngine;
using UnityEngine.UI;
using TMPro;

#if UNITY_EDITOR
using UnityEditor;

public class AddLessonPanelBuilder : EditorWindow
{
    [MenuItem("Tools/update AddLessonPanel Builder")]
    public static void ShowWindow()
    {
        GetWindow<AddLessonPanelBuilder>("AddLessonPanel Builder");
    }

    private void OnGUI()
    {
        GUILayout.Label("AddLessonPanel Builder", EditorStyles.boldLabel);
        GUILayout.Space(8);

        if (GUILayout.Button("1. Tao WordRow Prefab tu Scene", GUILayout.Height(36)))
            CreateWordRowPrefab();

        GUILayout.Space(4);

        if (GUILayout.Button("2. Gan prefab vao AddLessonPanel", GUILayout.Height(36)))
            AssignPrefab();

        GUILayout.Space(4);

        if (GUILayout.Button(">>> Chay ca 2 buoc <<<", GUILayout.Height(44)))
        {
            CreateWordRowPrefab();
            AssignPrefab();
        }
    }

    // -------------------------------------------------------------------------
    // BUOC 1: Tim WordRow trong scene, neu khong co thi tu tao bang code
    // -------------------------------------------------------------------------
    private static void CreateWordRowPrefab()
    {
        // Tao thu muc prefab neu chua co
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
            AssetDatabase.CreateFolder("Assets", "Prefabs");

        // Thu tim WordRow co san trong scene truoc
        var panel = FindObjectOfType<AddLessonPanel>();
        Transform wordRow = null;

        if (panel != null)
        {
            var wordListContent = FindDeepChild(panel.transform, "WordListContent");
            if (wordListContent != null)
            {
                foreach (Transform child in wordListContent)
                {
                    if (child.name.StartsWith("WordRow"))
                    {
                        wordRow = child;
                        break;
                    }
                }
            }
        }

        // Neu khong tim thay WordRow trong scene -> tu tao bang code
        if (wordRow == null)
        {
            Debug.Log("[Builder] Khong tim thay WordRow trong scene. Tu dong tao WordRow bang code...");
            wordRow = CreateWordRowGameObject();

            if (wordRow == null)
            {
                Debug.LogError("[Builder] Tao WordRow that bai!");
                return;
            }
        }

        // Kiem tra cac field bat buoc
        if (!ValidateWordRow(wordRow)) return;

        // Luu prefab
        string path = "Assets/Prefabs/WordRow.prefab";
        var prefab = PrefabUtility.SaveAsPrefabAsset(wordRow.gameObject, path);

        if (prefab != null)
            Debug.Log($"[Builder] Da tao prefab: {path}");
        else
            Debug.LogError("[Builder] Luu prefab that bai!");

        // Xoa GameObject tam neu la do ta tao (khong co parent trong scene)
        if (wordRow.parent == null)
            Object.DestroyImmediate(wordRow.gameObject);

        AssetDatabase.Refresh();
    }

    // -------------------------------------------------------------------------
    // Tao WordRow hoan chinh bang code (khong can co san trong scene)
    // -------------------------------------------------------------------------
    private static Transform CreateWordRowGameObject()
    {
        // --- Root: WordRow ---
        var rowGO = new GameObject("WordRow_1");
        var rowRect = rowGO.AddComponent<RectTransform>();
        rowRect.sizeDelta = new Vector2(0f, 40f);

        var rowImg = rowGO.AddComponent<Image>();
        rowImg.color = new Color(0.96f, 0.96f, 0.96f, 1f);

        var rowLayout = rowGO.AddComponent<HorizontalLayoutGroup>();
        rowLayout.spacing = 6f;
        rowLayout.padding = new RectOffset(6, 6, 4, 4);
        rowLayout.childAlignment         = TextAnchor.MiddleLeft;
        rowLayout.childControlWidth      = true;
        rowLayout.childControlHeight     = true;
        rowLayout.childForceExpandWidth  = false; // TAT de LayoutElement tung child tu quyet dinh
        rowLayout.childForceExpandHeight = false;

        // --- Field_English (flexible, chiem phan con lai) ---
        var engField = CreateInputField(rowGO.transform, "Field_English", "English...");
        var engLE = engField.gameObject.AddComponent<LayoutElement>();
        engLE.flexibleWidth  = 1f;
        engLE.minWidth       = 60f;

        // --- Field_Vietnamese (flexible, chiem phan con lai) ---
        var vietField = CreateInputField(rowGO.transform, "Field_Vietnamese", "Vietnamese...");
        var vietLE = vietField.gameObject.AddComponent<LayoutElement>();
        vietLE.flexibleWidth = 1f;
        vietLE.minWidth      = 60f;

        // --- DeleteButton ---
        var delGO = new GameObject("DeleteButton");
        delGO.transform.SetParent(rowGO.transform, false);

        var delRect = delGO.AddComponent<RectTransform>();
        delRect.sizeDelta = new Vector2(32f, 32f);

        var delImg = delGO.AddComponent<Image>();
        delImg.color = new Color(0.85f, 0.25f, 0.25f, 1f);

        var delBtn = delGO.AddComponent<Button>();

        // Layout element de khoa chieu rong nut xoa
        var delLE = delGO.AddComponent<LayoutElement>();
        delLE.minWidth      = 32f;
        delLE.preferredWidth = 32f;
        delLE.flexibleWidth  = 0f;

        // Text "X" tren nut xoa
        var delTextGO = new GameObject("Text");
        delTextGO.transform.SetParent(delGO.transform, false);
        var delTextRect = delTextGO.AddComponent<RectTransform>();
        delTextRect.anchorMin = Vector2.zero;
        delTextRect.anchorMax = Vector2.one;
        delTextRect.offsetMin = Vector2.zero;
        delTextRect.offsetMax = Vector2.zero;

        var delTMP = delTextGO.AddComponent<TextMeshProUGUI>();
        delTMP.text      = "X";
        delTMP.fontSize  = 14f;
        delTMP.alignment = TextAlignmentOptions.Center;
        delTMP.color     = Color.white;

        Debug.Log("[Builder] Da tao WordRow bang code voi: Field_English, Field_Vietnamese, DeleteButton");
        return rowGO.transform;
    }

    // -------------------------------------------------------------------------
    // Tao TMP_InputField day du (Text Area + Placeholder + Text)
    // -------------------------------------------------------------------------
    private static TMP_InputField CreateInputField(Transform parent, string name, string placeholder)
    {
        // Container
        var fieldGO = new GameObject(name);
        fieldGO.transform.SetParent(parent, false);

        var fieldRect = fieldGO.AddComponent<RectTransform>();
        fieldRect.sizeDelta = new Vector2(0f, 32f);

        var fieldImg = fieldGO.AddComponent<Image>();
        fieldImg.color = Color.white;

        var inputField = fieldGO.AddComponent<TMP_InputField>();

        // Text Area (viewport)
        var textAreaGO = new GameObject("Text Area");
        textAreaGO.transform.SetParent(fieldGO.transform, false);
        var textAreaRect = textAreaGO.AddComponent<RectTransform>();
        textAreaRect.anchorMin = Vector2.zero;
        textAreaRect.anchorMax = Vector2.one;
        textAreaRect.offsetMin = new Vector2(6f, 2f);
        textAreaRect.offsetMax = new Vector2(-6f, -2f);
        textAreaGO.AddComponent<RectMask2D>();

        // Placeholder
        var placeholderGO = new GameObject("Placeholder");
        placeholderGO.transform.SetParent(textAreaGO.transform, false);
        var placeholderRect = placeholderGO.AddComponent<RectTransform>();
        placeholderRect.anchorMin = Vector2.zero;
        placeholderRect.anchorMax = Vector2.one;
        placeholderRect.offsetMin = Vector2.zero;
        placeholderRect.offsetMax = Vector2.zero;

        var placeholderTMP = placeholderGO.AddComponent<TextMeshProUGUI>();
        placeholderTMP.text      = placeholder;
        placeholderTMP.fontSize  = 14f;
        placeholderTMP.color     = new Color(0.5f, 0.5f, 0.5f, 0.75f);
        placeholderTMP.alignment = TextAlignmentOptions.MidlineLeft;

        // Text
        var textGO = new GameObject("Text");
        textGO.transform.SetParent(textAreaGO.transform, false);
        var textRect = textGO.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        var textTMP = textGO.AddComponent<TextMeshProUGUI>();
        textTMP.fontSize  = 14f;
        textTMP.color     = Color.black;
        textTMP.alignment = TextAlignmentOptions.MidlineLeft;

        // Gan vao TMP_InputField
        inputField.textViewport   = textAreaRect;
        inputField.textComponent  = textTMP;
        inputField.placeholder    = placeholderTMP;
        inputField.caretWidth     = 2;

        return inputField;
    }

    // -------------------------------------------------------------------------
    // Kiem tra WordRow co du Field_English, Field_Vietnamese (TMP_InputField)
    // -------------------------------------------------------------------------
    private static bool ValidateWordRow(Transform wordRow)
    {
        var fieldEng = wordRow.Find("Field_English");
        if (fieldEng == null)
        {
            Debug.LogError("[Builder] WordRow thieu Field_English!");
            return false;
        }
        if (fieldEng.GetComponent<TMP_InputField>() == null)
        {
            Debug.LogError("[Builder] Field_English thieu TMP_InputField!");
            return false;
        }

        var fieldViet = wordRow.Find("Field_Vietnamese");
        if (fieldViet == null)
        {
            Debug.LogError("[Builder] WordRow thieu Field_Vietnamese!");
            return false;
        }
        if (fieldViet.GetComponent<TMP_InputField>() == null)
        {
            Debug.LogError("[Builder] Field_Vietnamese thieu TMP_InputField!");
            return false;
        }

        return true;
    }

    // -------------------------------------------------------------------------
    // BUOC 2: Gan prefab vao field wordRowPrefab cua AddLessonPanel
    // -------------------------------------------------------------------------
    private static void AssignPrefab()
    {
        var panel = FindObjectOfType<AddLessonPanel>();
        if (panel == null)
        {
            Debug.LogError("[Builder] Khong tim thay AddLessonPanel!");
            return;
        }

        string path = "Assets/Prefabs/WordRow.prefab";
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab == null)
        {
            Debug.LogError($"[Builder] Khong tim thay prefab tai {path}. Hay chay buoc 1 truoc!");
            return;
        }

        var so   = new SerializedObject(panel);
        var prop = so.FindProperty("wordRowPrefab");

        if (prop == null)
        {
            Debug.LogError("[Builder] Khong tim thay field 'wordRowPrefab' trong AddLessonPanel!");
            return;
        }

        prop.objectReferenceValue = prefab;
        so.ApplyModifiedProperties();

        EditorUtility.SetDirty(panel);
        Debug.Log("[Builder] Da gan WordRow.prefab vao AddLessonPanel.wordRowPrefab!");
    }

    // -------------------------------------------------------------------------
    // Helper: tim child theo ten bat ke do sau
    // -------------------------------------------------------------------------
    private static Transform FindDeepChild(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name == name) return child;
            var found = FindDeepChild(child, name);
            if (found != null) return found;
        }
        return null;
    }
}
#endif