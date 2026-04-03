#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// BedroomUIBuilder — Editor Script.
/// Menu: Tools > Build Bedroom Scene UI
/// Tạo toàn bộ hierarchy BedroomCanvas trong SampleScene một lần duy nhất.
/// Sau khi build: kéo ảnh pixel art phòng ngủ vào slot backgroundSprite là xong.
/// </summary>
public class BedroomUIBuilder : EditorWindow
{
    [MenuItem("Tools/Build Bedroom Scene UI")]
    public static void BuildBedroomUI()
    {
        // ── 0. Tìm hoặc tạo EventSystem ───────────────────────────
        EnsureEventSystem();

        // ── 1. Tạo BedroomCanvas (Canvas gốc) ────────────────────
        GameObject canvasGO = CreateCanvas("BedroomCanvas", sortOrder: 5);
        Canvas canvas = canvasGO.GetComponent<Canvas>();

        // ── 2. BackgroundImage — stretch full screen ──────────────
        GameObject bgGO = CreateFullStretchImage(canvasGO.transform, "BackgroundImage");
        Image bgImage = bgGO.GetComponent<Image>();
        bgImage.color = new Color(0.18f, 0.13f, 0.22f); // màu placeholder tím tối

        // Gắn component BedroomBackground để lộ slot Sprite trong Inspector
        BedroomBackground bgComp = bgGO.AddComponent<BedroomBackground>();
        // bgComp.backgroundSprite sẽ được kéo vào Inspector sau

        // ── 3. DeskHitArea — vùng bàn học (bottom-left) ──────────
        // Vị trí: góc trái dưới, chiều rộng ~30%, chiều cao ~40% màn hình
        GameObject deskBtn = CreateHitArea(
            parent     : canvasGO.transform,
            name       : "DeskHitArea",
            anchorMin  : new Vector2(0f, 0f),       // góc trái dưới
            anchorMax  : new Vector2(0.45f, 0.42f), // 45% width, 42% height
            label      : "📚 Bàn học\n(kéo vùng này vào đúng vị trí)"
        );

        // Gán onClick → BedroomManager.OpenVocabCanvas
        // (phải gán bằng tay qua Inspector hoặc code sau khi BedroomManager tồn tại)
        Button deskButton = deskBtn.GetComponent<Button>();

        // ── 4. MedalHitArea — vùng kệ/tủ (top-right) ────────────
        GameObject medalBtn = CreateHitArea(
            parent     : canvasGO.transform,
            name       : "MedalHitArea",
            anchorMin  : new Vector2(0.55f, 0.60f),  // 55%→100% width, 60%→85% height
            anchorMax  : new Vector2(1.0f, 0.85f),
            label      : "🏅 Kệ huy chương\n(kéo vùng này vào đúng vị trí)"
        );
        Button medalButton = medalBtn.GetComponent<Button>();

        // ── 5. AvatarWidget placeholder (góc phải trên) ──────────
        GameObject avatarGO = CreateAvatarWidget(canvasGO.transform);

        // ── 6. Tạo BedroomManager GameObject ─────────────────────
        GameObject managerGO = new GameObject("BedroomManager");
        BedroomManager bedroomMgr = managerGO.AddComponent<BedroomManager>();

        // Gán BedroomCanvas vào slot
        SerializedObject so = new SerializedObject(bedroomMgr);
        so.FindProperty("bedroomCanvas").objectReferenceValue = canvasGO;
        so.ApplyModifiedProperties();

        // ── 7. BedroomCanvas mặc định ẩn ─────────────────────────
        canvasGO.SetActive(false);

        // ── 8. Đánh dấu scene dirty để Unity biết cần save ────────
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Debug.Log("[BedroomUIBuilder] ✅ Build xong! Nhớ:\n" +
                  "1. Kéo ảnh pixel art phòng ngủ vào BackgroundImage > BedroomBackground > backgroundSprite\n" +
                  "2. Điều chỉnh RectTransform của DeskHitArea & MedalHitArea cho khớp ảnh\n" +
                  "3. Gán onClick của DeskHitArea → BedroomManager.OpenVocabCanvas\n" +
                  "4. Gán onClick của MedalHitArea → BedroomManager.OpenMedalCanvas\n" +
                  "5. Kéo VocabCanvas & MedalCanvas vào các slot tương ứng của BedroomManager");

        Selection.activeGameObject = canvasGO;
    }

    // ─── Helper: Tạo Canvas UI ────────────────────────────────────
    private static GameObject CreateCanvas(string name, int sortOrder)
    {
        GameObject go = new GameObject(name);

        Canvas canvas = go.AddComponent<Canvas>();
        canvas.renderMode    = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder  = sortOrder;

        CanvasScaler scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution  = new Vector2(720, 1280);
        scaler.screenMatchMode      = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight   = 0.5f;

        go.AddComponent<GraphicRaycaster>();
        return go;
    }

    // ─── Helper: Image stretch full screen ───────────────────────
    private static GameObject CreateFullStretchImage(Transform parent, string name)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin    = Vector2.zero;
        rt.anchorMax    = Vector2.one;
        rt.offsetMin    = Vector2.zero;
        rt.offsetMax    = Vector2.zero;
        return go;
    }

    // ─── Helper: Tạo HitArea (Button trong suốt + label debug) ───
    private static GameObject CreateHitArea(
        Transform parent,
        string name,
        Vector2 anchorMin,
        Vector2 anchorMax,
        string label)
    {
        // Container
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin  = anchorMin;
        rt.anchorMax  = anchorMax;
        rt.offsetMin  = Vector2.zero;
        rt.offsetMax  = Vector2.zero;

        // Image trong suốt (cần để Button nhận raycast)
        Image img = go.AddComponent<Image>();
        img.color = new Color(1, 1, 0, 0f); // alpha=0, invisible

        // Button
        Button btn = go.AddComponent<Button>();
        ColorBlock cb = btn.colors;
        cb.highlightedColor = new Color(1, 1, 0, 0.15f); // vàng mờ khi hover (debug)
        btn.colors = cb;

        // Label debug — chỉ thấy trong Editor, có thể xoá sau
        GameObject labelGO = new GameObject("DebugLabel", typeof(RectTransform));
        labelGO.transform.SetParent(go.transform, false);
        RectTransform lrt = labelGO.GetComponent<RectTransform>();
        lrt.anchorMin = Vector2.zero;
        lrt.anchorMax = Vector2.one;
        lrt.offsetMin = Vector2.zero;
        lrt.offsetMax = Vector2.zero;

        TextMeshProUGUI tmp = labelGO.AddComponent<TextMeshProUGUI>();
        tmp.text      = label;
        tmp.fontSize  = 22;
        tmp.color     = new Color(1, 1, 0, 0.6f); // vàng mờ — debug only
        tmp.alignment = TextAlignmentOptions.Center;

        return go;
    }

    // ─── Helper: AvatarWidget placeholder ────────────────────────
    private static GameObject CreateAvatarWidget(Transform parent)
    {
        // Widget container — góc phải trên
        GameObject go = new GameObject("AvatarWidget", typeof(RectTransform));
        go.transform.SetParent(parent, false);

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin  = new Vector2(0.72f, 0.84f);
        rt.anchorMax  = new Vector2(1.0f,  1.0f);
        rt.offsetMin  = new Vector2(8, 8);
        rt.offsetMax  = new Vector2(-8, -8);

        // Background mờ
        Image bg = go.AddComponent<Image>();
        bg.color = new Color(0, 0, 0, 0.35f);

        // ── Avatar Icon slot ──────────────────────────────────────
        GameObject iconGO = new GameObject("AvatarIcon", typeof(RectTransform), typeof(Image));
        iconGO.transform.SetParent(go.transform, false);
        RectTransform iconRt = iconGO.GetComponent<RectTransform>();
        iconRt.anchorMin = new Vector2(0.05f, 0.15f);
        iconRt.anchorMax = new Vector2(0.55f, 0.85f);
        iconRt.offsetMin = Vector2.zero;
        iconRt.offsetMax = Vector2.zero;
        Image iconImg = iconGO.GetComponent<Image>();
        iconImg.color = new Color(0.6f, 0.8f, 1f); // xanh nhạt placeholder

        // ── Display Name ──────────────────────────────────────────
        GameObject nameGO = new GameObject("DisplayName", typeof(RectTransform));
        nameGO.transform.SetParent(go.transform, false);
        RectTransform nameRt = nameGO.GetComponent<RectTransform>();
        nameRt.anchorMin = new Vector2(0.55f, 0.5f);
        nameRt.anchorMax = new Vector2(1f,    1f);
        nameRt.offsetMin = new Vector2(4, 0);
        nameRt.offsetMax = Vector2.zero;
        TextMeshProUGUI nameTmp = nameGO.AddComponent<TextMeshProUGUI>();
        nameTmp.text      = "Player";
        nameTmp.fontSize  = 20;
        nameTmp.color     = Color.white;
        nameTmp.fontStyle = FontStyles.Bold;
        nameTmp.alignment = TextAlignmentOptions.MidlineLeft;

        // ── XP / Coin label ───────────────────────────────────────
        GameObject xpGO = new GameObject("XpLabel", typeof(RectTransform));
        xpGO.transform.SetParent(go.transform, false);
        RectTransform xpRt = xpGO.GetComponent<RectTransform>();
        xpRt.anchorMin = new Vector2(0.55f, 0f);
        xpRt.anchorMax = new Vector2(1f,    0.5f);
        xpRt.offsetMin = new Vector2(4, 0);
        xpRt.offsetMax = Vector2.zero;
        TextMeshProUGUI xpTmp = xpGO.AddComponent<TextMeshProUGUI>();
        xpTmp.text      = "⭐ 0 XP  🪙 0";
        xpTmp.fontSize  = 16;
        xpTmp.color     = new Color(1f, 0.9f, 0.4f);
        xpTmp.alignment = TextAlignmentOptions.MidlineLeft;

        // Gắn AvatarWidget script (BƯỚC 3 sẽ bổ sung)
        // go.AddComponent<AvatarWidget>(); // uncomment khi có script

        return go;
    }

    // ─── Helper: Đảm bảo có EventSystem trong scene ───────────────
    private static void EnsureEventSystem()
    {
        if (Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject es = new GameObject("EventSystem");
            es.AddComponent<UnityEngine.EventSystems.EventSystem>();
            es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }
    }
}
#endif