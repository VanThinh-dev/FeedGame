// // ============================================================
// //  MedalCardPrefabBuilder.cs — Assets/Editor/MedalCardPrefabBuilder.cs
// //
// //  Tools > Build Medal Card Prefab
// //
// //  Tạo MedalCardPrefab tại Assets/Prefabs/MedalCardPrefab.prefab
// //
// //  Layout của 1 card:
// //  MedalCardPrefab  (200×240, Image nền + outline)
// //  ├── BorderImage      ← viền màu theo loại huy chương
// //  ├── MedalImage       ← ảnh huy chương (100×100, top-center)
// //  ├── LessonNameText   ← TMP tên bài học
// //  └── DateText         ← TMP ngày đạt được
// // ============================================================
// #if UNITY_EDITOR
// using System.IO;
// using UnityEditor;
// using UnityEngine;
// using UnityEngine.UI;
// using TMPro;

// public static class MedalCardPrefabBuilder
// {
//     // ── Kích thước card ───────────────────────────────────────────────────────
//     const float CARD_W       = 200f;
//     const float CARD_H       = 240f;
//     const float ICON_SIZE    = 96f;
//     const float PADDING      = 12f;
//     const float BORDER_SIZE  = 3f;

//     // ── Màu nền card ──────────────────────────────────────────────────────────
//     static readonly Color CARD_BG      = new Color(0.10f, 0.11f, 0.16f, 0.97f);
//     static readonly Color BORDER_DEF   = new Color(0.80f, 0.50f, 0.20f, 0.80f); // đồng mặc định
//     static readonly Color TEXT_WHITE   = new Color(1f,    1f,    1f,    0.92f);
//     static readonly Color TEXT_DATE    = new Color(0.70f, 0.70f, 0.75f, 0.85f);

//     // ── Đường dẫn prefab ──────────────────────────────────────────────────────
//     const string PREFAB_PATH = "Assets/Prefabs/MedalCardPrefab.prefab";

//     // ════════════════════════════════════════════════════════════════════════
//     [MenuItem("Tools/Build Medal Card Prefab")]
//     public static void BuildMedalCardPrefab()
//     {
//         // Tạo thư mục Prefabs nếu chưa có
//         if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
//             AssetDatabase.CreateFolder("Assets", "Prefabs");

//         // ── Tạo root GameObject ───────────────────────────────────────────────
//         var root   = new GameObject("MedalCardPrefab");
//         var rootRT = root.AddComponent<RectTransform>();
//         rootRT.sizeDelta = new Vector2(CARD_W, CARD_H);

//         // Nền card
//         var bgImg   = root.AddComponent<Image>();
//         bgImg.color = CARD_BG;
//         bgImg.sprite= GetBuiltinSprite();
//         bgImg.type  = Image.Type.Sliced;

//         // ── Border (viền ngoài) ───────────────────────────────────────────────
//         var borderGO = MakeChild("BorderImage", root.transform);
//         var borderRT = borderGO.GetComponent<RectTransform>();
//         borderRT.anchorMin = Vector2.zero;
//         borderRT.anchorMax = Vector2.one;
//         borderRT.offsetMin = new Vector2(-BORDER_SIZE, -BORDER_SIZE);
//         borderRT.offsetMax = new Vector2( BORDER_SIZE,  BORDER_SIZE);

//         var borderImg         = borderGO.AddComponent<Image>();
//         borderImg.color       = BORDER_DEF;
//         borderImg.sprite      = GetBuiltinSprite();
//         borderImg.type        = Image.Type.Sliced;
//         borderImg.raycastTarget = false;

//         // Đẩy BorderImage xuống dưới Background (sibling index 0)
//         borderGO.transform.SetSiblingIndex(0);

//         // ── MedalImage (top-center) ───────────────────────────────────────────
//         var iconGO = MakeChild("MedalImage", root.transform);
//         var iconRT = iconGO.GetComponent<RectTransform>();
//         iconRT.anchorMin        = new Vector2(0.5f, 1f);
//         iconRT.anchorMax        = new Vector2(0.5f, 1f);
//         iconRT.pivot            = new Vector2(0.5f, 1f);
//         iconRT.anchoredPosition = new Vector2(0f, -PADDING);
//         iconRT.sizeDelta        = new Vector2(ICON_SIZE, ICON_SIZE);

//         var iconImg = iconGO.AddComponent<Image>();
//         iconImg.preserveAspect = true;
//         iconImg.color          = Color.white;
//         // Sprite được kéo vào MedalCard.cs inspector

//         // ── LessonNameText ────────────────────────────────────────────────────
//         var nameGO = MakeChild("LessonNameText", root.transform);
//         var nameRT = nameGO.GetComponent<RectTransform>();
//         nameRT.anchorMin        = new Vector2(0f, 0.5f);
//         nameRT.anchorMax        = new Vector2(1f, 0.5f);
//         nameRT.pivot            = new Vector2(0.5f, 1f);
//         // Đặt ngay dưới icon
//         float nameY = -(PADDING + ICON_SIZE + 8f);
//         nameRT.anchoredPosition = new Vector2(0f, nameY);
//         nameRT.sizeDelta        = new Vector2(-PADDING * 2, 60f);

//         var nameTxt             = nameGO.AddComponent<TextMeshProUGUI>();
//         nameTxt.text            = "Ten Bai Hoc";
//         nameTxt.fontSize        = 14f;
//         nameTxt.fontStyle       = FontStyles.Bold;
//         nameTxt.color           = TEXT_WHITE;
//         nameTxt.alignment       = TextAlignmentOptions.Center;
//         nameTxt.overflowMode    = TextOverflowModes.Ellipsis;
//         nameTxt.enableWordWrapping = true;

//         // ── DateText ──────────────────────────────────────────────────────────
//         var dateGO = MakeChild("DateText", root.transform);
//         var dateRT = dateGO.GetComponent<RectTransform>();
//         dateRT.anchorMin        = new Vector2(0f, 0f);
//         dateRT.anchorMax        = new Vector2(1f, 0f);
//         dateRT.pivot            = new Vector2(0.5f, 0f);
//         dateRT.anchoredPosition = new Vector2(0f, PADDING);
//         dateRT.sizeDelta        = new Vector2(-PADDING * 2, 28f);

//         var dateTxt             = dateGO.AddComponent<TextMeshProUGUI>();
//         dateTxt.text            = "dd/MM/yyyy";
//         dateTxt.fontSize        = 12f;
//         dateTxt.color           = TEXT_DATE;
//         dateTxt.alignment       = TextAlignmentOptions.Center;

//         // ── Gán MedalCard component ───────────────────────────────────────────
//         var medalCard = root.AddComponent<MedalCard>();

//         // Dùng SerializedObject để wire các field private
//         var so = new SerializedObject(medalCard);
//         so.FindProperty("medalImage")    .objectReferenceValue = iconImg;
//         so.FindProperty("lessonNameText").objectReferenceValue = nameTxt;
//         so.FindProperty("dateText")      .objectReferenceValue = dateTxt;
//         so.FindProperty("borderImage")   .objectReferenceValue = borderImg;
//         so.ApplyModifiedProperties();

//         // ── Lưu thành Prefab ──────────────────────────────────────────────────
//         bool success;
//         var prefab = PrefabUtility.SaveAsPrefabAsset(root, PREFAB_PATH, out success);
//         Object.DestroyImmediate(root);

//         if (success)
//         {
//             AssetDatabase.Refresh();
//             Selection.activeObject = prefab;
//             EditorGUIUtility.PingObject(prefab);

//             EditorUtility.DisplayDialog(
//                 "Medal Card Prefab Builder",
//                 $"Da tao thanh cong!\n{PREFAB_PATH}\n\n" +
//                 "Viec can lam:\n" +
//                 "1. Mo prefab > MedalCard Inspector\n" +
//                 "   → keo goldMedalSprite, silverMedalSprite, bronzeMedalSprite\n\n" +
//                 "2. Keo prefab nay vao MedalManager.medalCardPrefab",
//                 "OK"
//             );
//         }
//         else
//         {
//             EditorUtility.DisplayDialog(
//                 "Medal Card Prefab Builder",
//                 $"Tao prefab that bai!\nKiem tra duong dan: {PREFAB_PATH}",
//                 "OK"
//             );
//         }
//     }

//     // ── Helpers ───────────────────────────────────────────────────────────────
//     static GameObject MakeChild(string name, Transform parent)
//     {
//         var go = new GameObject(name);
//         go.transform.SetParent(parent, false);
//         go.AddComponent<RectTransform>();
//         return go;
//     }

//     static Sprite GetBuiltinSprite() =>
//         AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
// }
// #endif