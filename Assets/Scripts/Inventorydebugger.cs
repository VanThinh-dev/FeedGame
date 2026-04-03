using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// HƯỚNG DẪN SỬ DỤNG:
/// 1. Tạo GameObject rỗng trong Scene, đặt tên "InventoryDebugger"
/// 2. Gắn script này vào
/// 3. Gán các reference trong Inspector
/// 4. Chạy game → mở Inventory → nhấn nút "Run Debug" HOẶC tự chạy qua phím F9
/// 5. Đọc log trong Console, tìm dòng [❌] để thấy lỗi
/// </summary>
public class InventoryDebugger : MonoBehaviour
{
    [Header("=== GÁN VÀO INSPECTOR ===")]
    public GameObject inventoryCanvas;
    public Transform  itemListGrid;          // Content object
    public GameObject inventoryCardPrefab;
    public ScrollRect scrollView;
    public RectTransform viewport;

    [Header("Phím tắt debug (chạy runtime)")]
    public KeyCode debugKey = KeyCode.F9;

    // ─────────────────────────────────────────────────────────
    void Update()
    {
        if (Input.GetKeyDown(debugKey))
            StartCoroutine(RunFullDebug());
    }

    // ─────────────────────────────────────────────────────────
    [ContextMenu("Run Debug Now")]
    public void RunDebugFromMenu() => StartCoroutine(RunFullDebug());

    // ═════════════════════════════════════════════════════════
    // MAIN DEBUG ROUTINE
    // ═════════════════════════════════════════════════════════
    private IEnumerator RunFullDebug()
    {
        yield return null; // đợi 1 frame cho layout tính toán

        Log("════════════════════════════════════════");
        Log("   INVENTORY DEBUGGER START");
        Log("════════════════════════════════════════");

        // ── 1. Kiểm tra Canvas ───────────────────────────────
        Log("\n[BƯỚC 1] Kiểm tra InventoryCanvas");
        if (inventoryCanvas == null)
        {
            LogError("inventoryCanvas CHƯA ĐƯỢC GÁN trong Inspector!");
        }
        else
        {
            LogBool("Canvas không null",           true);
            LogBool("Canvas activeSelf",           inventoryCanvas.activeSelf);
            LogBool("Canvas activeInHierarchy",    inventoryCanvas.activeInHierarchy);

            var canvas = inventoryCanvas.GetComponent<Canvas>();
            if (canvas == null) LogError("Không tìm thấy component Canvas trên GameObject này!");
            else
            {
                LogBool("Canvas component enabled", canvas.enabled);
                Log($"     Canvas renderMode = {canvas.renderMode}");
                if (canvas.renderMode == RenderMode.ScreenSpaceCamera && canvas.worldCamera == null)
                    LogError("Canvas dùng ScreenSpaceCamera nhưng Camera chưa gán!");
            }
        }

        // ── 2. Kiểm tra itemListGrid (Content) ───────────────
        Log("\n[BƯỚC 2] Kiểm tra itemListGrid (Content)");
        if (itemListGrid == null)
        {
            LogError("itemListGrid CHƯA ĐƯỢC GÁN trong Inspector!");
        }
        else
        {
            LogBool("itemListGrid không null",        true);
            LogBool("itemListGrid activeSelf",        itemListGrid.gameObject.activeSelf);
            LogBool("itemListGrid activeInHierarchy", itemListGrid.gameObject.activeInHierarchy);

            var rt = itemListGrid.GetComponent<RectTransform>();
            Log($"     anchoredPosition = {rt.anchoredPosition}");
            Log($"     sizeDelta        = {rt.sizeDelta}");
            Log($"     rect             = {rt.rect}");

            if (rt.rect.width < 1f || rt.rect.height < 1f)
                LogError($"Content có kích thước gần = 0! ({rt.rect.width}x{rt.rect.height}) — card spawn vào đây sẽ không thấy");

            // Kiểm tra layout group
            var glg = itemListGrid.GetComponent<GridLayoutGroup>();
            var vlg = itemListGrid.GetComponent<VerticalLayoutGroup>();
            var hlg = itemListGrid.GetComponent<HorizontalLayoutGroup>();
            if (glg != null)
            {
                LogOK("Có GridLayoutGroup");
                Log($"     cellSize={glg.cellSize}, spacing={glg.spacing}, constraint={glg.constraint}");
            }
            else if (vlg != null) LogOK("Có VerticalLayoutGroup");
            else if (hlg != null) LogWarn("Có HorizontalLayoutGroup — phù hợp nếu card nằm ngang, nhưng lạ cho item list");
            else LogWarn("KHÔNG có Layout Group trên Content — card sẽ chồng lên nhau tại vị trí (0,0)");

            // Kiểm tra ContentSizeFitter
            var csf = itemListGrid.GetComponent<ContentSizeFitter>();
            if (csf == null) LogWarn("Không có ContentSizeFitter trên Content — scroll có thể không hoạt động đúng");
            else LogOK($"Có ContentSizeFitter: H={csf.horizontalFit}, V={csf.verticalFit}");
        }

        // ── 3. Kiểm tra ScrollView & Viewport ────────────────
        Log("\n[BƯỚC 3] Kiểm tra ScrollView");
        if (scrollView == null)
            LogWarn("scrollView chưa gán — bỏ qua kiểm tra ScrollRect");
        else
        {
            LogBool("ScrollRect enabled",            scrollView.enabled);
            LogBool("ScrollRect activeInHierarchy",  scrollView.gameObject.activeInHierarchy);
            Log($"     content gán trong ScrollRect = {(scrollView.content == null ? "NULL!" : scrollView.content.name)}");

            if (scrollView.content == null)
                LogError("ScrollRect.content CHƯA GÁN — kéo 'Content' vào field Content của ScrollRect!");
            else if (itemListGrid != null && scrollView.content != itemListGrid)
                LogWarn($"ScrollRect.content ({scrollView.content.name}) KHÁC với itemListGrid ({itemListGrid.name}) — có thể không đồng nhất");

            if (viewport != null)
            {
                var mask = viewport.GetComponent<Mask>();
                var rm   = viewport.GetComponent<RectMask2D>();
                if (mask == null && rm == null)
                    LogWarn("Viewport không có Mask hoặc RectMask2D — nội dung sẽ tràn ra ngoài nhưng vẫn thấy");
                else
                    LogOK("Viewport có Mask/RectMask2D");

                Log($"     Viewport rect = {viewport.rect}");
                if (viewport.rect.width < 1f || viewport.rect.height < 1f)
                    LogError("Viewport kích thước gần = 0! Card sẽ bị ẩn hết bởi Mask");
            }
        }

        // ── 4. Kiểm tra Prefab ───────────────────────────────
        Log("\n[BƯỚC 4] Kiểm tra inventoryCardPrefab");
        if (inventoryCardPrefab == null)
        {
            LogError("inventoryCardPrefab CHƯA ĐƯỢC GÁN trong Inspector!");
        }
        else
        {
            LogBool("Prefab không null", true);
            LogBool("Prefab activeSelf", inventoryCardPrefab.activeSelf);
            if (!inventoryCardPrefab.activeSelf)
                LogError("Prefab đang BỊ TẮT (inactive)! Coroutine sẽ không chạy được sau khi Instantiate");

            var prefabRt = inventoryCardPrefab.GetComponent<RectTransform>();
            if (prefabRt != null)
            {
                Log($"     Prefab sizeDelta = {prefabRt.sizeDelta}");
                if (prefabRt.sizeDelta.x < 1f || prefabRt.sizeDelta.y < 1f)
                    LogError("Prefab có sizeDelta gần = 0 — card sẽ không nhìn thấy dù đã spawn");
            }

            var card = inventoryCardPrefab.GetComponent<InventoryCard>();
            if (card == null) LogError("Prefab KHÔNG có component InventoryCard!");
            else               LogOK("Prefab có InventoryCard component");
        }

        // ── 5. Test spawn thật ───────────────────────────────
        Log("\n[BƯỚC 5] Test spawn card giả (không cần Firebase)");
        if (inventoryCardPrefab != null && itemListGrid != null)
        {
            var testGo = Instantiate(inventoryCardPrefab, itemListGrid);
            testGo.SetActive(true);
            testGo.name = "[DEBUG_TEST_CARD]";

            yield return null; // đợi layout
            yield return null;

            var rt = testGo.GetComponent<RectTransform>();
            Log($"     Test card spawned!");
            Log($"     activeInHierarchy = {testGo.activeInHierarchy}");
            Log($"     anchoredPosition  = {rt.anchoredPosition}");
            Log($"     sizeDelta         = {rt.sizeDelta}");
            Log($"     rect              = {rt.rect}");
            Log($"     worldCorners:");

            Vector3[] corners = new Vector3[4];
            rt.GetWorldCorners(corners);
            Log($"       BottomLeft  = {corners[0]}");
            Log($"       TopLeft     = {corners[1]}");
            Log($"       TopRight    = {corners[2]}");
            Log($"       BottomRight = {corners[3]}");

            if (rt.rect.width < 1f || rt.rect.height < 1f)
                LogError("Card test có kích thước = 0 sau khi spawn! Lỗi do layout/prefab size");
            else
                LogOK($"Card test có kích thước hợp lệ: {rt.rect.width}x{rt.rect.height}");

            // Kiểm tra card có nằm trong viewport không
            if (viewport != null)
            {
                Vector3[] vpCorners = new Vector3[4];
                viewport.GetWorldCorners(vpCorners);
                Rect vpRect = new Rect(vpCorners[0].x, vpCorners[0].y,
                    vpCorners[2].x - vpCorners[0].x,
                    vpCorners[2].y - vpCorners[0].y);

                bool insideVP = vpRect.Overlaps(new Rect(corners[0].x, corners[0].y,
                    corners[2].x - corners[0].x,
                    corners[2].y - corners[0].y));

                if (insideVP) LogOK("Card test NẰM TRONG viewport — đáng lẽ phải thấy!");
                else          LogError("Card test NẰM NGOÀI viewport — bị Mask cắt mất!");
            }

            // Xóa card test sau 3 giây
            Destroy(testGo, 3f);
            Log("     (Card test sẽ tự xóa sau 3 giây)");
        }

        // ── 6. Kiểm tra hierarchy path ───────────────────────
        Log("\n[BƯỚC 6] Hierarchy path của itemListGrid");
        if (itemListGrid != null)
        {
            string path = GetHierarchyPath(itemListGrid.transform);
            Log($"     {path}");
        }

        Log("\n════════════════════════════════════════");
        Log("   INVENTORY DEBUGGER END");
        Log("   Tìm dòng [❌] để biết vấn đề!");
        Log("════════════════════════════════════════");
    }

    // ═════════════════════════════════════════════════════════
    // HELPERS
    // ═════════════════════════════════════════════════════════
    private string GetHierarchyPath(Transform t)
    {
        string path = t.name;
        while (t.parent != null)
        {
            t    = t.parent;
            path = t.name + " → " + path;
        }
        return path;
    }

    private void Log(string msg)      => Debug.Log($"<color=cyan>[Debug]</color> {msg}");
    private void LogOK(string msg)    => Debug.Log($"<color=green>[]</color> {msg}");
    private void LogWarn(string msg)  => Debug.LogWarning($"[] {msg}");
    private void LogError(string msg) => Debug.LogError($"[] {msg}");
    private void LogBool(string label, bool val)
    {
        if (val) LogOK($"{label} = {val}");
        else     LogError($"{label} = {val}");
    }
}