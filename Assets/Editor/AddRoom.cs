// =============================================================================
// RoomMapUIBuilder.cs — Assets/Editor/RoomMapUIBuilder.cs  (v2 — 6 phòng)
//
// Tools > Build Room Map System (v2)
//
// Tạo:
//   • TransitionOverlay (Image đen, alpha 0, stretch full screen)
//   • RoomNameTag (Text tên phòng góc trên giữa)
//   • RoomMapManagerGO (GameObject chứa RoomMapManager component)
//   • Wire backgroundImage từ BedroomCanvas > BackgroundImage
//   • 6 RoomConfig slots: Bedroom/LivingRoom/Kitchen/Bathroom/PlayRoom/Garden
// =============================================================================

#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public static class RoomMapUIBuilder
{
    const float REF_W = 720f;
    const float REF_H = 1280f;

    static readonly Color TAG_BG   = new Color(0f, 0f, 0f, 0.35f);
    static readonly Color TAG_TEXT = new Color(1f, 1f, 1f, 0.90f);

    // =========================================================================
    [MenuItem("Tools/Build Room Map System (v2 — 6 phòng)")]
    public static void BuildRoomMapSystem()
    {
        Undo.SetCurrentGroupName("Build Room Map System v2");
        int grp = Undo.GetCurrentGroup();

        // ── 1. Tìm BedroomCanvas ─────────────────────────────────────────────
        var bedroomCanvas = GameObject.Find("BedroomCanvas");
        if (bedroomCanvas == null)
        {
            EditorUtility.DisplayDialog("Room Map Builder",
                "Không tìm thấy BedroomCanvas trong scene!\n" +
                "Hãy chạy 'Tools > Build Bedroom Scene UI' trước.", "OK");
            return;
        }

        // ── 2. Lấy BackgroundImage ────────────────────────────────────────────
        var bgImageGO = bedroomCanvas.transform.Find("BackgroundImage");
        Image bgImage = bgImageGO != null ? bgImageGO.GetComponent<Image>() : null;
        if (bgImage == null)
            Debug.LogWarning("[RoomMapBuilder] Không tìm thấy BackgroundImage — tự gán trong Inspector.");

        // ── 3. Tạo TransitionOverlay ──────────────────────────────────────────
        var oldOverlay = bedroomCanvas.transform.Find("TransitionOverlay");
        if (oldOverlay != null) Undo.DestroyObjectImmediate(oldOverlay.gameObject);

        var overlayGO = new GameObject("TransitionOverlay");
        Undo.RegisterCreatedObjectUndo(overlayGO, "TransitionOverlay");
        overlayGO.transform.SetParent(bedroomCanvas.transform, false);
        overlayGO.transform.SetAsLastSibling();

        var overlayRT         = overlayGO.AddComponent<RectTransform>();
        overlayRT.anchorMin   = Vector2.zero;
        overlayRT.anchorMax   = Vector2.one;
        overlayRT.offsetMin   = Vector2.zero;
        overlayRT.offsetMax   = Vector2.zero;

        var overlayImg            = overlayGO.AddComponent<Image>();
        overlayImg.color          = new Color(0f, 0f, 0f, 0f);
        overlayImg.raycastTarget  = false;

        // ── 4. Tạo RoomNameTag ────────────────────────────────────────────────
        var oldTag = bedroomCanvas.transform.Find("RoomNameTag");
        if (oldTag != null) Undo.DestroyObjectImmediate(oldTag.gameObject);

        var tagGO = new GameObject("RoomNameTag");
        Undo.RegisterCreatedObjectUndo(tagGO, "RoomNameTag");
        tagGO.transform.SetParent(bedroomCanvas.transform, false);

        var tagRT                  = tagGO.AddComponent<RectTransform>();
        tagRT.anchorMin            = new Vector2(0.5f, 1f);
        tagRT.anchorMax            = new Vector2(0.5f, 1f);
        tagRT.pivot                = new Vector2(0.5f, 1f);
        tagRT.sizeDelta            = new Vector2(300f, 52f);
        tagRT.anchoredPosition     = new Vector2(0f, -12f);

        var tagBg      = tagGO.AddComponent<Image>();
        tagBg.color    = TAG_BG;
        tagBg.sprite   = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        tagBg.type     = Image.Type.Sliced;

        var tagTextGO = new GameObject("RoomNameText");
        tagTextGO.transform.SetParent(tagGO.transform, false);
        var tagTextRT       = tagTextGO.AddComponent<RectTransform>();
        tagTextRT.anchorMin = Vector2.zero;
        tagTextRT.anchorMax = Vector2.one;
        tagTextRT.offsetMin = new Vector2(8f, 4f);
        tagTextRT.offsetMax = new Vector2(-8f, -4f);

        var tagTxt       = tagTextGO.AddComponent<TextMeshProUGUI>();
        tagTxt.text      = "Phòng Ngủ";
        tagTxt.fontSize  = 20;
        tagTxt.fontStyle = FontStyles.Bold;
        tagTxt.alignment = TextAlignmentOptions.Center;
        tagTxt.color     = TAG_TEXT;

        // ── 5. Tạo RoomMapManager ─────────────────────────────────────────────
        var oldMgr = GameObject.Find("RoomMapManager");
        if (oldMgr != null) Undo.DestroyObjectImmediate(oldMgr);

        var mgrGO = new GameObject("RoomMapManager");
        Undo.RegisterCreatedObjectUndo(mgrGO, "RoomMapManager");
        var mgr = mgrGO.AddComponent<RoomMapManager>();

        // ── 6. Wire fields ────────────────────────────────────────────────────
        var so = new SerializedObject(mgr);
        so.FindProperty("backgroundImage").objectReferenceValue  = bgImage;
        so.FindProperty("roomNameText").objectReferenceValue     = tagTxt;
        so.FindProperty("transitionOverlay").objectReferenceValue = overlayImg;

        // ── 7. 6 RoomConfig ───────────────────────────────────────────────────
        var roomsProp = so.FindProperty("rooms");
        roomsProp.arraySize = 6;

        SetRoomConfig(roomsProp.GetArrayElementAtIndex(0), RoomType.Bedroom,    "Phòng Ngủ",   1);
        SetRoomConfig(roomsProp.GetArrayElementAtIndex(1), RoomType.LivingRoom, "Phòng Khách", 5);
        SetRoomConfig(roomsProp.GetArrayElementAtIndex(2), RoomType.Kitchen,    "Phòng Bếp",   10);
        SetRoomConfig(roomsProp.GetArrayElementAtIndex(3), RoomType.Bathroom,   "Phòng Tắm",   15);
        SetRoomConfig(roomsProp.GetArrayElementAtIndex(4), RoomType.PlayRoom,   "Phòng Chơi",  20);
        SetRoomConfig(roomsProp.GetArrayElementAtIndex(5), RoomType.Garden,     "Sân Vườn",    25);

        so.ApplyModifiedProperties();

        Undo.CollapseUndoOperations(grp);

        EditorUtility.DisplayDialog(
            "Room Map Builder v2",
            "Room Map System (6 phòng) đã được tạo!\n\n" +
            "Việc còn lại:\n" +
            "1. Kéo sprite ảnh vào RoomMapManager > Rooms > [n] > Background Sprite\n" +
            "2. Kéo Hit Areas vào từng RoomConfig\n" +
            "3. Chạy 'Tools > Build AvatarWidget (v3)' để tạo 2 nút phòng mới\n\n" +
            "Level lock đang TẮT (test mode). Tìm '[LEVEL-LOCK]' trong code để bật lại.",
            "OK");
    }

    private static void SetRoomConfig(SerializedProperty prop, RoomType roomType,
                                      string nameVN, int requiredLevel)
    {
        prop.FindPropertyRelative("roomType").enumValueIndex = (int)roomType;
        prop.FindPropertyRelative("roomNameVN").stringValue  = nameVN;
        prop.FindPropertyRelative("requiredLevel").intValue  = requiredLevel;
    }
}
#endif