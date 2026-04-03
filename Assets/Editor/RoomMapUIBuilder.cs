// // =============================================================================
// // RoomMapUIBuilder.cs — Assets/Editor/RoomMapUIBuilder.cs
// //
// // Tools > Build Room Map System
// //
// // Tạo:
// //   • TransitionOverlay (Image đen, alpha 0, stretch full screen)
// //   • RoomNameTag (Text tên phòng góc trên giữa)
// //   • RoomMapManagerGO (GameObject chứa RoomMapManager component)
// //   • Wire backgroundImage từ BedroomCanvas > BackgroundImage
// //   • Tạo sẵn 4 RoomConfig slots cho Bedroom/LivingRoom/PlayRoom/Garden
// //
// // FILE RIÊNG — không ép vào BedroomUIBuilder hay file builder nào khác.
// // =============================================================================

// #if UNITY_EDITOR
// using UnityEditor;
// using UnityEngine;
// using UnityEngine.UI;
// using TMPro;

// public static class RoomMapUIBuilder
// {
//     const float REF_W = 720f;
//     const float REF_H = 1280f;

//     // ── Colors ───────────────────────────────────────────────────────────────
//     static readonly Color TAG_BG     = new Color(0f,   0f,   0f,   0.35f);
//     static readonly Color TAG_TEXT   = new Color(1f,   1f,   1f,   0.90f);

//     // =========================================================================
//     [MenuItem("Tools/Build Room Map System")]
//     public static void BuildRoomMapSystem()
//     {
//         Undo.SetCurrentGroupName("Build Room Map System");
//         int grp = Undo.GetCurrentGroup();

//         // ── 1. Tìm BedroomCanvas để lấy reference ────────────────────────────
//         var bedroomCanvas = GameObject.Find("BedroomCanvas");
//         if (bedroomCanvas == null)
//         {
//             EditorUtility.DisplayDialog("Room Map Builder",
//                 "Không tìm thấy BedroomCanvas trong scene!\nHãy chạy 'Tools > Build Bedroom Scene UI' trước.",
//                 "OK");
//             return;
//         }

//         // ── 2. Lấy BackgroundImage từ BedroomCanvas ───────────────────────────
//         var bgImageGO = bedroomCanvas.transform.Find("BackgroundImage");
//         Image bgImage = bgImageGO != null ? bgImageGO.GetComponent<Image>() : null;

//         if (bgImage == null)
//         {
//             Debug.LogWarning("[RoomMapBuilder] Không tìm thấy BackgroundImage trong BedroomCanvas. " +
//                              "Bạn cần tự gán backgroundImage trong Inspector của RoomMapManager.");
//         }

//         // ── 3. Lấy Canvas (để đặt UI đúng parent) ───────────────────────────
//         var canvas = bedroomCanvas.GetComponent<Canvas>();

//         // ── 4. Tạo TransitionOverlay ──────────────────────────────────────────
//         // Xoá cũ nếu có
//         var oldOverlay = bedroomCanvas.transform.Find("TransitionOverlay");
//         if (oldOverlay != null) Undo.DestroyObjectImmediate(oldOverlay.gameObject);

//         var overlayGO = new GameObject("TransitionOverlay");
//         Undo.RegisterCreatedObjectUndo(overlayGO, "TransitionOverlay");
//         overlayGO.transform.SetParent(bedroomCanvas.transform, false);

//         // Đặt ở cuối hierarchy (render trên cùng trong BedroomCanvas)
//         overlayGO.transform.SetAsLastSibling();

//         var overlayRT = overlayGO.AddComponent<RectTransform>();
//         overlayRT.anchorMin = Vector2.zero;
//         overlayRT.anchorMax = Vector2.one;
//         overlayRT.offsetMin = Vector2.zero;
//         overlayRT.offsetMax = Vector2.zero;

//         var overlayImg = overlayGO.AddComponent<Image>();
//         overlayImg.color         = new Color(0f, 0f, 0f, 0f); // Trong suốt
//         overlayImg.raycastTarget = false;

//         // ── 5. Tạo RoomNameTag ────────────────────────────────────────────────
//         var oldTag = bedroomCanvas.transform.Find("RoomNameTag");
//         if (oldTag != null) Undo.DestroyObjectImmediate(oldTag.gameObject);

//         var tagGO = new GameObject("RoomNameTag");
//         Undo.RegisterCreatedObjectUndo(tagGO, "RoomNameTag");
//         tagGO.transform.SetParent(bedroomCanvas.transform, false);

//         var tagRT = tagGO.AddComponent<RectTransform>();
//         // Góc trên giữa
//         tagRT.anchorMin        = new Vector2(0.5f, 1f);
//         tagRT.anchorMax        = new Vector2(0.5f, 1f);
//         tagRT.pivot            = new Vector2(0.5f, 1f);
//         tagRT.sizeDelta        = new Vector2(300f, 52f);
//         tagRT.anchoredPosition = new Vector2(0f, -12f);

//         // Background tối mờ
//         var tagBg = tagGO.AddComponent<Image>();
//         tagBg.color  = TAG_BG;
//         tagBg.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
//         tagBg.type   = Image.Type.Sliced;

//         // Text tên phòng
//         var tagTextGO = new GameObject("RoomNameText");
//         tagTextGO.transform.SetParent(tagGO.transform, false);
//         var tagTextRT = tagTextGO.AddComponent<RectTransform>();
//         tagTextRT.anchorMin = Vector2.zero;
//         tagTextRT.anchorMax = Vector2.one;
//         tagTextRT.offsetMin = new Vector2(8f, 4f);
//         tagTextRT.offsetMax = new Vector2(-8f, -4f);

//         var tagTxt = tagTextGO.AddComponent<TextMeshProUGUI>();
//         tagTxt.text      = "Phòng Ngủ";
//         tagTxt.fontSize  = 20;
//         tagTxt.fontStyle = FontStyles.Bold;
//         tagTxt.alignment = TextAlignmentOptions.Center;
//         tagTxt.color     = TAG_TEXT;

//         // ── 6. Tạo RoomMapManager GameObject ─────────────────────────────────
//         var oldMgr = GameObject.Find("RoomMapManager");
//         if (oldMgr != null) Undo.DestroyObjectImmediate(oldMgr);

//         var mgrGO = new GameObject("RoomMapManager");
//         Undo.RegisterCreatedObjectUndo(mgrGO, "RoomMapManager");

//         var mgr = mgrGO.AddComponent<RoomMapManager>();

//         // ── 7. Wire các field qua SerializedObject ────────────────────────────
//         var so = new SerializedObject(mgr);

//         // backgroundImage
//         so.FindProperty("backgroundImage").objectReferenceValue = bgImage;

//         // roomNameText
//         so.FindProperty("roomNameText").objectReferenceValue = tagTxt;

//         // transitionOverlay
//         so.FindProperty("transitionOverlay").objectReferenceValue = overlayImg;

//         // ── 8. Tạo 4 RoomConfig default ──────────────────────────────────────
//         var roomsProp = so.FindProperty("rooms");
//         roomsProp.arraySize = 4;

//         SetRoomConfig(roomsProp.GetArrayElementAtIndex(0), RoomType.Bedroom,    "Phòng Ngủ",    1);
//         SetRoomConfig(roomsProp.GetArrayElementAtIndex(1), RoomType.LivingRoom, "Phòng Khách",  5);
//         SetRoomConfig(roomsProp.GetArrayElementAtIndex(2), RoomType.PlayRoom,   "Phòng Chơi",   10);
//         SetRoomConfig(roomsProp.GetArrayElementAtIndex(3), RoomType.Garden,     "Sân Vườn",     15);

//         so.ApplyModifiedProperties();

//         // ── 9. Gắn RoomMapManager vào cùng scene ─────────────────────────────
//         // (không cần DontDestroyOnLoad vì single scene)

//         Undo.CollapseUndoOperations(grp);

//         EditorUtility.DisplayDialog(
//             "Room Map Builder",
//             "Room Map System đã được tạo!\n\n" +
//             "Việc còn lại:\n" +
//             "1. Kéo sprite ảnh vào RoomMapManager > Rooms > [n] > Background Sprite\n" +
//             "2. Kéo DeskHitArea, MedalHitArea, v.v. vào Room Hit Areas của từng phòng\n" +
//             "3. Kéo shopCanvas, inventoryCanvas vào AvatarWidgetController nếu chưa có",
//             "OK");
//     }

//     // =========================================================================
//     // Helper — điền giá trị cho 1 phần tử RoomConfig trong mảng
//     // =========================================================================
//     private static void SetRoomConfig(SerializedProperty prop, RoomType roomType,
//                                       string nameVN, int requiredLevel)
//     {
//         prop.FindPropertyRelative("roomType").enumValueIndex  = (int)roomType;
//         prop.FindPropertyRelative("roomNameVN").stringValue   = nameVN;
//         prop.FindPropertyRelative("requiredLevel").intValue   = requiredLevel;
//         // backgroundSprite và roomHitAreas để null — user tự kéo vào sau
//     }
// }
// #endif