// ─────────────────────────────────────────────────────────────────────────────
// VocabCanvasFixer.cs  —  để vào Assets/Editor/  — chạy 1 lần rồi xóa
//
// FIX:
//   1. Background Image trong VocabCanvas đang block raycast lên CloseButton
//      → Tắt "Raycast Target" trên Image nền (không cần nhận click)
//   2. Sort Order: VocabCanvas(20) > BedroomCanvas(10)
//      → CloseButton không bị canvas khác che
// ─────────────────────────────────────────────────────────────────────────────
#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;

public class VocabCanvasFixer
{
    [MenuItem("Tools/Fix — VocabCanvas CloseButton & Sort Orders")]
    static void Fix()
    {
        int changes = 0;
        var all = Resources.FindObjectsOfTypeAll<GameObject>();

        foreach (var go in all)
        {
            if (!go.scene.IsValid()) continue;

            // ── 1. Fix Sort Order ─────────────────────────────────────────────
            var cv = go.GetComponent<Canvas>();
            if (cv != null)
            {
                int target = -1;
                if (go.name == "BackgroundCanvas") target = 0;
                if (go.name == "BedroomCanvas")    target = 10;
                if (go.name == "AuthCanvas")       target = 10;
                if (go.name == "VocabCanvas")      target = 20;

                if (target >= 0 && cv.sortingOrder != target)
                {
                    Undo.RecordObject(cv, "Fix SortOrder");
                    cv.sortingOrder    = target;
                    cv.overrideSorting = true;
                    EditorUtility.SetDirty(cv);
                    Debug.Log($"[Fix] {go.name} sortingOrder = {target}");
                    changes++;
                }
            }

            // ── 2. Tắt Raycast Target trên Image nền của VocabCanvas ──────────
            // Đây là nguyên nhân CloseButton bị block:
            // Image "Background" nằm sau CloseButton nhưng vẫn ăn raycast
            if (go.name == "Background" && go.transform.parent?.name == "VocabCanvas")
            {
                var img = go.GetComponent<Image>();
                if (img != null && img.raycastTarget)
                {
                    Undo.RecordObject(img, "Disable Raycast on Background");
                    img.raycastTarget = false;
                    EditorUtility.SetDirty(img);
                    Debug.Log("[Fix] VocabCanvas/Background — raycastTarget = false");
                    changes++;
                }
            }

            // ── 3. Tắt Raycast Target trên tất cả Image trang trí (không phải Button) ──
            // Quét toàn bộ con của VocabCanvas, tắt raycast trên Image thuần trang trí
            if (go.name == "VocabCanvas")
            {
                var images = go.GetComponentsInChildren<Image>(true);
                foreach (var img in images)
                {
                    // Chỉ tắt những Image KHÔNG có Button component trên cùng GameObject
                    bool hasButton = img.GetComponent<Button>() != null;
                    bool isScrollbar = img.GetComponent<Scrollbar>() != null;

                    // Image nền (Background, TopBar, panels...) không cần raycast
                    string n = img.gameObject.name;
                    bool isDecorative =
                        n == "Background"   ||
                        n == "TopBar"       ||
                        n == "DaHocPanel"   ||
                        n == "ChuaHocPanel" ||
                        n == "ContentPanel" ||
                        n == "HeaderRow"    ||
                        n == "NameSection"  ||
                        n == "PopupBox";

                    if (isDecorative && !hasButton && !isScrollbar && img.raycastTarget)
                    {
                        Undo.RecordObject(img, "Disable Raycast Decorative");
                        img.raycastTarget = false;
                        EditorUtility.SetDirty(img);
                        Debug.Log($"[Fix] Tắt raycast: VocabCanvas/.../{img.gameObject.name}");
                        changes++;
                    }
                }
            }
        }

        EditorSceneManager.MarkAllScenesDirty();

        string msg = changes > 0
            ? $"✅ Đã fix {changes} thứ.\nSave scene (Ctrl+S) rồi test lại CloseButton."
            : "Không tìm thấy gì cần fix.\nKiểm tra tên GameObject có đúng không.";

        EditorUtility.DisplayDialog("VocabCanvas Fixer", msg, "OK");
    }
}
#endif