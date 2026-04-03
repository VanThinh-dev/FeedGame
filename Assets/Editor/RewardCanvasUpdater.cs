// ============================================================
//  RewardCanvasUpdater.cs — Assets/Editor/RewardCanvasUpdater.cs
//
//  Tools > Update Reward Canvas (Add Medal Card)
//
//  Không tạo lại RewardCanvas — chỉ THÊM MedalCard vào RewardPanel hiện có.
//
//  Sau khi chạy tool:
//  RewardPanel
//  ├── CardRow          ← đã có (CoinCard + XpCard)
//  ├── MedalCard (MỚI) ← thêm vào giữa CardRow và ConfirmButton
//  │   ├── MedalImage   ← [SerializeField] Sprite — kéo vào sau
//  │   └── MedalTypeText ← "Huy Chuong Dong"
//  └── ConfirmButton    ← đã có
// ============================================================
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public static class RewardCanvasUpdater
{
    // ── Theme ────────────────────────────────────────────────────────────────
    static readonly Color MEDAL_CARD_BG    = new Color(0.85f, 0.50f, 0.15f, 0.20f);
    static readonly Color MEDAL_CARD_BDR   = new Color(0.90f, 0.58f, 0.20f, 0.50f);
    static readonly Color MEDAL_TEXT_COLOR = new Color(1.00f, 0.88f, 0.50f, 1.00f);
    static readonly Color TEXT_WHITE       = new Color(1f, 1f, 1f, 0.90f);

    // ── Card size ─────────────────────────────────────────────────────────────
    const float CARD_W     = 280f;
    const float CARD_H     = 160f;
    const float ICON_SIZE  = 100f;
    const float GAP        = 16f;

    // ════════════════════════════════════════════════════════════════════════
    [MenuItem("Tools/Update Reward Canvas (Add Medal Card)")]
    public static void UpdateRewardCanvas()
    {
        Undo.SetCurrentGroupName("Update Reward Canvas");
        int grp = Undo.GetCurrentGroup();

        // ── Tìm RewardCanvas ─────────────────────────────────────────────────
        var canvasGO = GameObject.Find("RewardCanvas");
        if (canvasGO == null)
        {
            EditorUtility.DisplayDialog(
                "Reward Canvas Updater",
                "Khong tim thay 'RewardCanvas' trong scene.\n" +
                "Hay chay 'Tools > Build Reward Canvas' truoc.",
                "OK"
            );
            return;
        }

        // ── Tìm RewardPanel ──────────────────────────────────────────────────
        var rewardPanel = canvasGO.transform.Find("RewardPanel");
        if (rewardPanel == null)
        {
            // Thử tìm sâu hơn
            rewardPanel = FindDeep(canvasGO.transform, "RewardPanel");
            if (rewardPanel == null)
            {
                EditorUtility.DisplayDialog(
                    "Reward Canvas Updater",
                    "Khong tim thay 'RewardPanel' trong RewardCanvas.",
                    "OK"
                );
                return;
            }
        }

        // ── Xóa MedalCard cũ nếu đã tồn tại ─────────────────────────────────
        var existingMedal = rewardPanel.Find("MedalCard");
        if (existingMedal != null)
            Undo.DestroyObjectImmediate(existingMedal.gameObject);

        // ── Tìm ConfirmButton để chèn MedalCard TRƯỚC nó ─────────────────────
        var confirmBtn = FindDeep(rewardPanel, "ConfirmButton");

        // ── Tạo MedalCard ─────────────────────────────────────────────────────
        var medalCard   = MakeGO("MedalCard", rewardPanel);
        var medalCardRT = medalCard.GetComponent<RectTransform>();

        // Vị trí: giữa CardRow và ConfirmButton
        // Mặc định dùng VLayout — đặt giữa màn hình theo anchor
        medalCardRT.anchorMin        = new Vector2(0.5f, 0.5f);
        medalCardRT.anchorMax        = new Vector2(0.5f, 0.5f);
        medalCardRT.pivot            = new Vector2(0.5f, 0.5f);
        medalCardRT.sizeDelta        = new Vector2(CARD_W, CARD_H);
        // Đặt anchoredPosition dựa trên layout thực tế — chỉnh sau nếu cần
        medalCardRT.anchoredPosition = new Vector2(0f, -CARD_H * 0.5f - GAP);

        // Nền card huy chương
        var bgImg   = medalCard.AddComponent<Image>();
        bgImg.color = MEDAL_CARD_BG;
        bgImg.sprite= GetBuiltinSprite();
        bgImg.type  = Image.Type.Sliced;

        // Đặt sibling index trước ConfirmButton
        if (confirmBtn != null)
            medalCard.transform.SetSiblingIndex(confirmBtn.GetSiblingIndex());

        // ── Icon huy chương ───────────────────────────────────────────────────
        var iconGO = MakeGO("MedalImage", medalCard.transform);
        var iconRT = iconGO.GetComponent<RectTransform>();
        iconRT.anchorMin        = new Vector2(0f, 0.5f);
        iconRT.anchorMax        = new Vector2(0f, 0.5f);
        iconRT.pivot            = new Vector2(0f, 0.5f);
        iconRT.anchoredPosition = new Vector2(GAP, 0f);
        iconRT.sizeDelta        = new Vector2(ICON_SIZE, ICON_SIZE);

        var iconImg = iconGO.AddComponent<Image>();
        // Sprite kéo vào Inspector sau khi tạo xong

        // ── Text cột bên phải ─────────────────────────────────────────────────
        var textCol   = MakeGO("TextCol", medalCard.transform);
        var textColRT = textCol.GetComponent<RectTransform>();
        textColRT.anchorMin        = new Vector2(0f, 0f);
        textColRT.anchorMax        = new Vector2(1f, 1f);
        textColRT.offsetMin        = new Vector2(ICON_SIZE + GAP * 2, GAP);
        textColRT.offsetMax        = new Vector2(-GAP, -GAP);

        var vlg                   = textCol.AddComponent<VerticalLayoutGroup>();
        vlg.spacing               = 6f;
        vlg.childForceExpandHeight = false;
        vlg.childForceExpandWidth  = true;
        vlg.childAlignment         = TextAnchor.MiddleLeft;

        // Label nhỏ
        var labelGO  = MakeGO("Label", textCol.transform);
        var labelLE  = labelGO.AddComponent<LayoutElement>();
        labelLE.preferredHeight = 24f;
        var labelTxt         = labelGO.AddComponent<TextMeshProUGUI>();
        labelTxt.text        = "Phan Thuong";
        labelTxt.fontSize    = 16f;
        labelTxt.color       = TEXT_WHITE;
        labelTxt.fontStyle   = FontStyles.Normal;
        labelTxt.alignment   = TextAlignmentOptions.Left;

        // Tên loại huy chương
        var typGO   = MakeGO("MedalTypeText", textCol.transform);
        var typeLE  = typGO.AddComponent<LayoutElement>();
        typeLE.preferredHeight = 44f;
        var typeTxt          = typGO.AddComponent<TextMeshProUGUI>();
        typeTxt.text         = "Huy Chuong Dong";
        typeTxt.fontSize     = 26f;
        typeTxt.fontStyle    = FontStyles.Bold;
        typeTxt.color        = MEDAL_TEXT_COLOR;
        typeTxt.alignment    = TextAlignmentOptions.Left;

        // ── Gán vào RewardManager ─────────────────────────────────────────────
        var rmGO = GameObject.Find("RewardManager");
        if (rmGO != null)
        {
            var rm = rmGO.GetComponent<RewardManager>();
            if (rm != null)
            {
                var so = new SerializedObject(rm);

                var medalCardProp = so.FindProperty("medalCard");
                if (medalCardProp != null)
                    medalCardProp.objectReferenceValue = medalCard;

                var medalIconProp = so.FindProperty("medalIcon");
                if (medalIconProp != null)
                    medalIconProp.objectReferenceValue = iconImg;

                var medalTypeProp = so.FindProperty("medalTypeText");
                if (medalTypeProp != null)
                    medalTypeProp.objectReferenceValue = typeTxt;

                so.ApplyModifiedProperties();
                Debug.Log("[RewardCanvasUpdater] Da gan MedalCard vao RewardManager.");
            }
        }

        medalCard.SetActive(false); // ẩn mặc định — RewardManager bật khi cần

        Undo.CollapseUndoOperations(grp);

        EditorUtility.DisplayDialog(
            "Reward Canvas Updater",
            "Da them MedalCard vao RewardPanel!\n\n" +
            "Viec can lam:\n" +
            "1. Chon MedalImage > keo Sprite huy chuong dong vao\n" +
            "2. Trong RewardManager Inspector > keo bronzeMedalSprite\n" +
            "3. Chinh anchoredPosition cua MedalCard cho phu hop layout",
            "OK"
        );
    }

    // ── Helpers ───────────────────────────────────────────────────────────────
    static Transform FindDeep(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name == name) return child;
            var found = FindDeep(child, name);
            if (found != null) return found;
        }
        return null;
    }

    static GameObject MakeGO(string name, Transform parent)
    {
        var go = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(go, name);
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
        return go;
    }

    static GameObject MakeGO(string name, GameObject parent) =>
        MakeGO(name, parent.transform);

    static Sprite GetBuiltinSprite() =>
        AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
}
#endif