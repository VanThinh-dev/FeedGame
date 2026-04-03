#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

// ─────────────────────────────────────────────────────────────────────────────
// SceneFixer.cs  (Editor Script)
// Đặt vào: Assets/Scripts/Editor/
//
// Tools > Fix Scene — sửa toàn bộ lỗi cấu trúc:
//   1. Tạo VocabManager GameObject (nếu chưa có)
//   2. Di chuyển VocabCanvas ra khỏi BedroomManager → vào đúng chỗ
//   3. Wire lại VocabManager.vocabCanvas
//   4. Tạo DeskHitArea trên BedroomCanvas nếu chưa có
//   5. Kiểm tra VocabManager.Instance sẽ tìm được khi runtime
// ─────────────────────────────────────────────────────────────────────────────

public class SceneFixer : Editor
{
    [MenuItem("Tools/Fix Scene — Vocab Not Opening")]
    public static void FixScene()
    {
        int fixCount = 0;

        // ══════════════════════════════════════════════════════════════════════
        // BƯỚC 1: Đảm bảo VocabManager GameObject tồn tại trong scene
        // ══════════════════════════════════════════════════════════════════════
        VocabManager vocabManager = Object.FindFirstObjectByType<VocabManager>();

        if (vocabManager == null)
        {
            var vmGO = new GameObject("VocabManager");
            vocabManager = vmGO.AddComponent<VocabManager>();
            Debug.Log("[SceneFixer] ✅ Tạo VocabManager GameObject.");
            fixCount++;
        }
        else
        {
            Debug.Log($"[SceneFixer] VocabManager đã có trên: '{vocabManager.gameObject.name}'");
        }

        // ══════════════════════════════════════════════════════════════════════
        // BƯỚC 2: Tìm VocabCanvas
        // ══════════════════════════════════════════════════════════════════════
        GameObject vocabCanvas = GameObject.Find("VocabCanvas");

        if (vocabCanvas == null)
        {
            Debug.LogError("[SceneFixer] ❌ Không tìm thấy VocabCanvas trong scene!\n" +
                           "Hãy chạy Tools > Build Vocab Canvas trước.");
            return;
        }

        // ══════════════════════════════════════════════════════════════════════
        // BƯỚC 3: VocabCanvas phải là ROOT (hoặc con của Canvas gốc)
        //         KHÔNG được là con của BedroomManager
        // ══════════════════════════════════════════════════════════════════════
        bool isChildOfBedroomManager = false;
        Transform check = vocabCanvas.transform.parent;
        while (check != null)
        {
            if (check.GetComponent<BedroomManager>() != null)
            {
                isChildOfBedroomManager = true;
                break;
            }
            check = check.parent;
        }

        if (isChildOfBedroomManager)
        {
            // Di chuyển VocabCanvas lên root của scene
            vocabCanvas.transform.SetParent(null, false);

            // Đảm bảo Canvas component đúng
            var canvas = vocabCanvas.GetComponent<Canvas>();
            if (canvas == null)
            {
                canvas = vocabCanvas.AddComponent<Canvas>();
                canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 20;
                vocabCanvas.AddComponent<CanvasScaler>();
                vocabCanvas.AddComponent<GraphicRaycaster>();
            }

            Debug.Log("[SceneFixer] ✅ Di chuyển VocabCanvas ra root scene (thoát khỏi BedroomManager).");
            fixCount++;
        }
        else
        {
            Debug.Log("[SceneFixer] VocabCanvas đã ở đúng vị trí.");
        }

        // ══════════════════════════════════════════════════════════════════════
        // BƯỚC 4: Wire VocabManager ↔ VocabCanvas
        // ══════════════════════════════════════════════════════════════════════
        var vmSO = new SerializedObject(vocabManager);

        // vocabCanvas field
        var vcProp = vmSO.FindProperty("vocabCanvas");
        if (vcProp != null && vcProp.objectReferenceValue == null)
        {
            vcProp.objectReferenceValue = vocabCanvas;
            Debug.Log("[SceneFixer] ✅ Gán vocabCanvas vào VocabManager.");
            fixCount++;
        }

        // daHocPanel
        var daHocPanel = vocabCanvas.transform.Find("Background/ContentPanel/DaHocPanel")
                      ?? vocabCanvas.transform.Find("DaHocPanel")
                      ?? FindDeep(vocabCanvas.transform, "DaHocPanel");
        if (daHocPanel != null)
        {
            SetIfNull(vmSO, "daHocPanel", daHocPanel.gameObject);

            var daHocContent = FindDeep(daHocPanel, "LessonListContent")
                            ?? FindDeep(daHocPanel, "Content");
            if (daHocContent != null)
                SetIfNull(vmSO, "daHocContent", daHocContent);
        }

        // chuaHocPanel
        var chuaHocPanel = vocabCanvas.transform.Find("Background/ContentPanel/ChuaHocPanel")
                        ?? FindDeep(vocabCanvas.transform, "ChuaHocPanel");
        if (chuaHocPanel != null)
        {
            SetIfNull(vmSO, "chuaHocPanel", chuaHocPanel.gameObject);

            var chuaHocContent = FindDeep(chuaHocPanel, "LessonListContent")
                              ?? FindDeep(chuaHocPanel, "Content");
            if (chuaHocContent != null)
                SetIfNull(vmSO, "chuaHocContent", chuaHocContent);
        }

        // lessonCardPrefab
        var cardPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/LessonCardPrefab.prefab");
        if (cardPrefab == null)
            cardPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/LessonCardPrefab.prefab");
        if (cardPrefab != null)
            SetIfNull(vmSO, "lessonCardPrefab", cardPrefab);
        else
            Debug.LogWarning("[SceneFixer] ⚠ Không tìm thấy LessonCardPrefab — chạy Tools > Build All Vocab Prefabs.");

        vmSO.ApplyModifiedProperties();
        EditorUtility.SetDirty(vocabManager);

        // ══════════════════════════════════════════════════════════════════════
        // BƯỚC 5: Wire BedroomManager.vocabCanvas
        // ══════════════════════════════════════════════════════════════════════
        var bedroomManager = Object.FindFirstObjectByType<BedroomManager>();
        if (bedroomManager != null)
        {
            var bmSO     = new SerializedObject(bedroomManager);
            var vcBMProp = bmSO.FindProperty("vocabCanvas");
            if (vcBMProp != null)
            {
                vcBMProp.objectReferenceValue = vocabCanvas;
                bmSO.ApplyModifiedProperties();
                EditorUtility.SetDirty(bedroomManager);
                Debug.Log("[SceneFixer] ✅ BedroomManager.vocabCanvas đã wire.");
                fixCount++;
            }
        }

        // ══════════════════════════════════════════════════════════════════════
        // BƯỚC 6: Tạo / Sửa DeskHitArea
        // ══════════════════════════════════════════════════════════════════════
        FixDeskHitArea(bedroomManager);
        fixCount++;

        // ══════════════════════════════════════════════════════════════════════
        // BƯỚC 7: VocabCanvasController — wire nếu chưa có
        // ══════════════════════════════════════════════════════════════════════
        var ctrl = vocabCanvas.GetComponent<VocabCanvasController>();
        if (ctrl != null)
        {
            WireVocabCanvasController(ctrl, vocabCanvas);
        }

        // ══════════════════════════════════════════════════════════════════════
        // VocabCanvas mặc định ẩn
        // ══════════════════════════════════════════════════════════════════════
        vocabCanvas.SetActive(false);

        // ── Đánh dấu scene dirty ─────────────────────────────────────────────
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

        Debug.Log($"[SceneFixer] ✅ Hoàn thành! Đã sửa {fixCount} vấn đề.\n" +
                  "Nhớ Ctrl+S để lưu scene.");
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // DESKH ITAREA
    // ═══════════════════════════════════════════════════════════════════════════

    private static void FixDeskHitArea(BedroomManager bedroomManager)
    {
        // Tìm DeskHitArea đã có
        var existing = GameObject.Find("DeskHitArea");

        if (existing != null)
        {
            // Gắn script nếu chưa có
            if (existing.GetComponent<DeskHitArea>() == null)
            {
                existing.AddComponent<DeskHitArea>();
                Debug.Log("[SceneFixer] ✅ Gắn DeskHitArea script vào GameObject đã có.");
            }

            // Đảm bảo có Button
            if (existing.GetComponent<Button>() == null)
            {
                existing.AddComponent<Button>();
                var img = existing.GetComponent<Image>() ?? existing.AddComponent<Image>();
                img.color = new Color(1, 1, 1, 0.01f); // trong suốt — chỉ là hitbox
                Debug.Log("[SceneFixer] ✅ Thêm Button vào DeskHitArea.");
            }

            EditorUtility.SetDirty(existing);
            return;
        }

        // Không tìm thấy — tạo mới bên trong BedroomCanvas
        var bedroomCanvas = bedroomManager != null
            ? bedroomManager.gameObject.transform.Find("BedroomCanvas")
              ?? GameObject.Find("BedroomCanvas")?.transform
            : null;

        // Fallback: đặt dưới root scene
        Transform parent = bedroomCanvas ?? (bedroomManager != null
            ? bedroomManager.transform
            : null);

        var go   = new GameObject("DeskHitArea");
        if (parent != null) go.transform.SetParent(parent, false);

        // RectTransform — đặt ở vùng bàn học (khoảng giữa-dưới màn hình)
        var rt   = go.AddComponent<RectTransform>();
        rt.anchorMin        = new Vector2(0.15f, 0.05f);
        rt.anchorMax        = new Vector2(0.65f, 0.40f);
        rt.offsetMin        = Vector2.zero;
        rt.offsetMax        = Vector2.zero;

        // Image trong suốt làm hitbox
        var hitboxImage = go.AddComponent<Image>();
        hitboxImage.color = new Color(1, 1, 1, 0.01f);

        // Button
        go.AddComponent<Button>();

        // DeskHitArea script
        go.AddComponent<DeskHitArea>();

        EditorUtility.SetDirty(go);

        Debug.Log("[SceneFixer] ✅ Tạo DeskHitArea mới.\n" +
                  "⚠ Kiểm tra vị trí RectTransform trong Scene view — kéo cho đúng vùng bàn học!");
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // VOCABCANVASCONTROLLER WIRING
    // ═══════════════════════════════════════════════════════════════════════════

    private static void WireVocabCanvasController(VocabCanvasController ctrl, GameObject vocabCanvas)
    {
        var so = new SerializedObject(ctrl);

        var closeBtn = FindDeep(vocabCanvas.transform, "CloseButton");
        if (closeBtn != null) SetIfNull(so, "closeButton", closeBtn.GetComponent<Button>());

        var tabDaHoc = FindDeep(vocabCanvas.transform, "Tab_DaHoc");
        if (tabDaHoc != null) SetIfNull(so, "tabDaHocButton", tabDaHoc.GetComponent<Button>());

        var tabChuaHoc = FindDeep(vocabCanvas.transform, "Tab_ChuaHoc");
        if (tabChuaHoc != null) SetIfNull(so, "tabChuaHocButton", tabChuaHoc.GetComponent<Button>());

        var addBtn = FindDeep(vocabCanvas.transform, "AddButton");
        if (addBtn != null) SetIfNull(so, "addButton", addBtn.GetComponent<Button>());

        var daHocPanel   = FindDeep(vocabCanvas.transform, "DaHocPanel");
        var chuaHocPanel = FindDeep(vocabCanvas.transform, "ChuaHocPanel");
        var addPanel     = FindDeep(vocabCanvas.transform, "AddLessonPanel");

        if (daHocPanel   != null) SetIfNull(so, "daHocPanel",   daHocPanel.gameObject);
        if (chuaHocPanel != null) SetIfNull(so, "chuaHocPanel", chuaHocPanel.gameObject);
        if (addPanel     != null) SetIfNull(so, "addLessonPanel", addPanel.gameObject);

        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(ctrl);
        Debug.Log("[SceneFixer] ✅ VocabCanvasController đã wire xong.");
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // DIAGNOSTIC — in ra toàn bộ trạng thái để debug
    // ═══════════════════════════════════════════════════════════════════════════

    [MenuItem("Tools/Diagnose Vocab Scene")]
    public static void DiagnoseScene()
    {
        Debug.Log("═══════════════════════════════════");
        Debug.Log("[Diagnose] Bắt đầu kiểm tra scene...");

        var vm = Object.FindFirstObjectByType<VocabManager>();
        Debug.Log(vm != null
            ? $"[Diagnose] ✅ VocabManager: '{vm.gameObject.name}'"
            : "[Diagnose] ❌ VocabManager KHÔNG TỒN TẠI trong scene!");

        if (vm != null)
        {
            var so = new SerializedObject(vm);
            LogField(so, "vocabCanvas",    "vocabCanvas");
            LogField(so, "daHocPanel",     "daHocPanel");
            LogField(so, "chuaHocPanel",   "chuaHocPanel");
            LogField(so, "daHocContent",   "daHocContent");
            LogField(so, "chuaHocContent", "chuaHocContent");
            LogField(so, "lessonCardPrefab","lessonCardPrefab");
        }

        var vc = GameObject.Find("VocabCanvas");
        if (vc != null)
        {
            Debug.Log($"[Diagnose] ✅ VocabCanvas tìm thấy. Parent: '{(vc.transform.parent?.name ?? "ROOT")}' | Active: {vc.activeSelf}");
            Debug.Log($"[Diagnose]    có Canvas component: {vc.GetComponent<Canvas>() != null}");
            Debug.Log($"[Diagnose]    có VocabManager script: {vc.GetComponent<VocabManager>() != null}");
            Debug.Log($"[Diagnose]    có VocabCanvasController: {vc.GetComponent<VocabCanvasController>() != null}");
        }
        else
        {
            Debug.LogError("[Diagnose] ❌ VocabCanvas KHÔNG TÌM THẤY!");
        }

        var desk = GameObject.Find("DeskHitArea");
        if (desk != null)
        {
            Debug.Log($"[Diagnose] ✅ DeskHitArea: '{desk.name}' | Active: {desk.activeSelf}");
            Debug.Log($"[Diagnose]    có Button: {desk.GetComponent<Button>() != null}");
            Debug.Log($"[Diagnose]    có DeskHitArea script: {desk.GetComponent<DeskHitArea>() != null}");
        }
        else
        {
            Debug.LogWarning("[Diagnose] ⚠ DeskHitArea KHÔNG TÌM THẤY — cần tạo hitbox trên bàn học.");
        }

        var bm = Object.FindFirstObjectByType<BedroomManager>();
        if (bm != null)
        {
            var bmSO = new SerializedObject(bm);
            LogField(bmSO, "vocabCanvas", "BedroomManager.vocabCanvas");
        }

        Debug.Log("═══════════════════════════════════");
        Debug.Log("[Diagnose] Xong! Đọc log phía trên để xác định vấn đề.");
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // HELPERS
    // ═══════════════════════════════════════════════════════════════════════════

    private static Transform FindDeep(Transform root, string name)
    {
        if (root.name == name) return root;
        foreach (Transform child in root)
        {
            var found = FindDeep(child, name);
            if (found != null) return found;
        }
        return null;
    }

    private static void SetIfNull(SerializedObject so, string propName, Object value)
    {
        var prop = so.FindProperty(propName);
        if (prop == null)
        {
            Debug.LogWarning($"[SceneFixer] Không tìm thấy property: '{propName}'");
            return;
        }
        if (prop.objectReferenceValue == null && value != null)
        {
            prop.objectReferenceValue = value;
            Debug.Log($"[SceneFixer] ✅ Set '{propName}' = {value.name}");
        }
    }

    private static void LogField(SerializedObject so, string propName, string label)
    {
        var prop = so.FindProperty(propName);
        if (prop == null)
            Debug.Log($"[Diagnose]    {label}: ❌ KHÔNG CÓ FIELD NÀY");
        else if (prop.objectReferenceValue == null)
            Debug.LogWarning($"[Diagnose]    {label}: ⚠ NULL — chưa gán!");
        else
            Debug.Log($"[Diagnose]    {label}: ✅ '{prop.objectReferenceValue.name}'");
    }
}
#endif