// #if UNITY_EDITOR
// using System.Collections;
// using UnityEngine;
// using UnityEngine.UI;
// using UnityEngine.Networking;
// using TMPro;
// using UnityEditor;
// using System.IO;

// /// <summary>
// /// AvatarWidgetBuilder — Editor script.
// /// Menu: Tools > Build Avatar Widget
// /// - Tất cả hình ảnh / avatar / button đều TRÒN
// /// - Tự download icon Shop / Inventory / Logout từ icons8
// /// - Không dùng emoji (tránh lỗi font TMP)
// /// </summary>
// public class AvatarWidgetBuilder : Editor
// {
//     // URL icon từ icons8 (free, PNG 96px)
//     private const string ICON_SHOP      = "https://img.icons8.com/fluency/96/shopping-bag-full.png";
//     private const string ICON_INVENTORY = "https://img.icons8.com/fluency/96/backpack.png";
//     private const string ICON_LOGOUT    = "https://img.icons8.com/fluency/96/exit.png";

//     // Thư mục lưu icon
//     private const string ICON_SAVE_PATH = "Assets/Resources/AvatarIcons";

//     [MenuItem("Tools/Build Avatar Widget")]
//     public static void BuildAvatarWidget()
//     {
//         if (!Directory.Exists(ICON_SAVE_PATH))
//             Directory.CreateDirectory(ICON_SAVE_PATH);

//         Canvas bedroomCanvas = FindOrCreateCanvas("BedroomCanvas");
//         GameObject canvasGO  = bedroomCanvas.gameObject;

//         Transform existing = canvasGO.transform.Find("AvatarWidget");
//         if (existing != null) DestroyImmediate(existing.gameObject);

//         // ══════════════════════════════════════════════════════════
//         // ROOT — anchor top-right
//         // ══════════════════════════════════════════════════════════
//         GameObject root      = CreateUI("AvatarWidget", canvasGO.transform);
//         RectTransform rootRT = root.GetComponent<RectTransform>();
//         rootRT.anchorMin        = new Vector2(1f, 1f);
//         rootRT.anchorMax        = new Vector2(1f, 1f);
//         rootRT.pivot            = new Vector2(1f, 1f);
//         rootRT.anchoredPosition = new Vector2(-16f, -16f);
//         rootRT.sizeDelta        = new Vector2(100f, 100f);

//         // ── Nền XP ring (màu tối — hiện phần trống) ──────────────
//         GameObject xpBgGO = CreateUI("XPRingBg", root.transform);
//         Image xpBg        = xpBgGO.AddComponent<Image>();
//         xpBg.color        = new Color(0.1f, 0.1f, 0.12f, 0.6f);
//         xpBg.type         = Image.Type.Filled;
//         xpBg.fillMethod   = Image.FillMethod.Radial360;
//         xpBg.fillAmount   = 1f;
//         Stretch(xpBgGO);

//         // ── XP Ring (màu xanh ngọc) ───────────────────────────────
//         GameObject xpRingGO = CreateUI("XPRing", root.transform);
//         Image xpRing        = xpRingGO.AddComponent<Image>();
//         xpRing.color        = new Color(0f, 0.85f, 0.75f, 1f);
//         xpRing.type         = Image.Type.Filled;
//         xpRing.fillMethod   = Image.FillMethod.Radial360;
//         xpRing.fillOrigin   = (int)Image.Origin360.Top;
//         xpRing.fillAmount   = 0.65f;
//         Stretch(xpRingGO);

//         // ── Avatar Circle (tròn + Mask) ───────────────────────────
//         GameObject avatarGO = CreateUI("AvatarCircle", root.transform);
//         Image avatarBg      = avatarGO.AddComponent<Image>();
//         avatarBg.sprite     = BuildCircleSprite();
//         avatarBg.color      = new Color(0.82f, 0.75f, 0.68f, 1f);
//         avatarGO.AddComponent<Mask>().showMaskGraphic = true;
//         Button avatarBtn    = avatarGO.AddComponent<Button>();
//         RectTransform aRT   = avatarGO.GetComponent<RectTransform>();
//         aRT.anchorMin = new Vector2(0.1f, 0.1f);
//         aRT.anchorMax = new Vector2(0.9f, 0.9f);
//         aRT.offsetMin = aRT.offsetMax = Vector2.zero;

//         // Image bên trong để kéo sprite avatar
//         GameObject avatarImgGO = CreateUI("AvatarImage", avatarGO.transform);
//         avatarImgGO.AddComponent<Image>().color = new Color(0.72f, 0.65f, 0.58f, 1f);
//         Stretch(avatarImgGO);

//         // ── Level Badge (tròn, góc dưới phải) ────────────────────
//         GameObject badgeGO    = CreateUI("LevelBadge", root.transform);
//         Image badgeImg        = badgeGO.AddComponent<Image>();
//         badgeImg.sprite       = BuildCircleSprite();
//         badgeImg.color        = new Color(0.1f, 0.12f, 0.5f, 1f);
//         RectTransform bRT     = badgeGO.GetComponent<RectTransform>();
//         bRT.anchorMin         = new Vector2(0.62f, 0f);
//         bRT.anchorMax         = new Vector2(1f,   0.38f);
//         bRT.offsetMin         = bRT.offsetMax = Vector2.zero;

//         GameObject badgeTxtGO = CreateUI("LevelText", badgeGO.transform);
//         TMP_Text badgeTxt     = badgeTxtGO.AddComponent<TextMeshProUGUI>();
//         badgeTxt.text         = "Lv.1";
//         badgeTxt.fontSize     = 11;
//         badgeTxt.fontStyle    = FontStyles.Bold;
//         badgeTxt.alignment    = TextAlignmentOptions.Center;
//         badgeTxt.color        = Color.white;
//         Stretch(badgeTxtGO);

//         // ══════════════════════════════════════════════════════════
//         // Dropdown Panel
//         // ══════════════════════════════════════════════════════════
//         GameObject dropdown  = CreateUI("DropdownPanel", root.transform);
//         Image dropBg         = dropdown.AddComponent<Image>();
//         dropBg.color         = new Color(0.08f, 0.07f, 0.06f, 0.95f);
//         RectTransform dropRT = dropdown.GetComponent<RectTransform>();
//         dropRT.anchorMin        = new Vector2(0f, 0f);
//         dropRT.anchorMax        = new Vector2(2f, 0f);
//         dropRT.pivot            = new Vector2(1f, 1f);
//         dropRT.anchoredPosition = new Vector2(0f, -8f);
//         dropRT.sizeDelta        = new Vector2(0f, 200f);
//         dropdown.SetActive(false);

//         VerticalLayoutGroup dropVLG   = dropdown.AddComponent<VerticalLayoutGroup>();
//         dropVLG.padding               = new RectOffset(10, 10, 10, 10);
//         dropVLG.spacing               = 8f;
//         dropVLG.childControlHeight    = false;
//         dropVLG.childControlWidth     = true;
//         dropVLG.childForceExpandWidth = true;

//         // ── Row 1: Rooms ──────────────────────────────────────────
//         GameObject roomRow = CreateUI("RoomButtons", dropdown.transform);
//         SetHeight(roomRow, 80f);
//         HorizontalLayoutGroup rHLG = roomRow.AddComponent<HorizontalLayoutGroup>();
//         rHLG.spacing               = 8f;
//         rHLG.childControlWidth     = false;
//         rHLG.childControlHeight    = false;
//         rHLG.childForceExpandWidth = true;
//         rHLG.childAlignment        = TextAnchor.MiddleCenter;

//         (string id, string label, int lv)[] rooms =
//         {
//             ("BedroomBtn",    "Phong ngu",   1),
//             ("LivingRoomBtn", "Phong khach", 5),
//             ("PlayRoomBtn",   "Phong choi",  10),
//             ("GardenBtn",     "San vuon",    15),
//         };
//         foreach (var r in rooms) CreateRoomButton(r.id, r.label, r.lv, roomRow.transform);

//         // ── Row 2: Actions ────────────────────────────────────────
//         GameObject actionRow = CreateUI("ActionButtons", dropdown.transform);
//         SetHeight(actionRow, 80f);
//         HorizontalLayoutGroup aHLG = actionRow.AddComponent<HorizontalLayoutGroup>();
//         aHLG.spacing               = 8f;
//         aHLG.childControlWidth     = false;
//         aHLG.childControlHeight    = false;
//         aHLG.childForceExpandWidth = true;
//         aHLG.childAlignment        = TextAnchor.MiddleCenter;

//         CreateActionButton("ShopBtn",      "Shop",       new Color(0.15f, 0.45f, 0.9f,  1f), actionRow.transform);
//         CreateActionButton("InventoryBtn", "Tui do",     new Color(0.45f, 0.25f, 0.75f, 1f), actionRow.transform);
//         CreateActionButton("LogoutBtn",    "Dang xuat",  new Color(0.48f, 0.26f, 0.08f, 1f), actionRow.transform);

//         // ══════════════════════════════════════════════════════════
//         // Gán references → AvatarWidgetController
//         // ══════════════════════════════════════════════════════════
//         AvatarWidgetController ctrl = root.AddComponent<AvatarWidgetController>();
//         SerializedObject so         = new SerializedObject(ctrl);

//         AssignRef(so, "xpRingImage",        xpRingGO.GetComponent<Image>());
//         AssignRef(so, "avatarCircleImage",  avatarImgGO.GetComponent<Image>());
//         AssignRef(so, "avatarCircleButton", avatarBtn);
//         AssignRef(so, "levelBadgeText",     badgeTxtGO.GetComponent<TMP_Text>());
//         AssignRef(so, "dropdownPanel",      dropdown);

//         Transform roomT = dropdown.transform.Find("RoomButtons");
//         if (roomT != null)
//         {
//             AssignRef(so, "bedroomBtn",            FindBtn(roomT, "BedroomBtn"));
//             AssignRef(so, "livingRoomBtn",         FindBtn(roomT, "LivingRoomBtn"));
//             AssignRef(so, "playRoomBtn",           FindBtn(roomT, "PlayRoomBtn"));
//             AssignRef(so, "gardenBtn",             FindBtn(roomT, "GardenBtn"));
//             AssignRef(so, "bedroomLockOverlay",    FindGO(roomT,  "BedroomBtn/Circle/LockOverlay"));
//             AssignRef(so, "livingRoomLockOverlay", FindGO(roomT,  "LivingRoomBtn/Circle/LockOverlay"));
//             AssignRef(so, "playRoomLockOverlay",   FindGO(roomT,  "PlayRoomBtn/Circle/LockOverlay"));
//             AssignRef(so, "gardenLockOverlay",     FindGO(roomT,  "GardenBtn/Circle/LockOverlay"));
//             AssignRef(so, "livingRoomLockText",    FindTMP(roomT, "LivingRoomBtn/Circle/LockOverlay/LockText"));
//             AssignRef(so, "playRoomLockText",      FindTMP(roomT, "PlayRoomBtn/Circle/LockOverlay/LockText"));
//             AssignRef(so, "gardenLockText",        FindTMP(roomT, "GardenBtn/Circle/LockOverlay/LockText"));
//         }

//         Transform actT = dropdown.transform.Find("ActionButtons");
//         if (actT != null)
//         {
//             AssignRef(so, "shopBtn",      FindBtn(actT, "ShopBtn"));
//             AssignRef(so, "inventoryBtn", FindBtn(actT, "InventoryBtn"));
//             AssignRef(so, "logoutBtn",    FindBtn(actT, "LogoutBtn"));
//         }

//         so.ApplyModifiedProperties();
//         EditorUtility.SetDirty(root);
//         Selection.activeGameObject = root;

//         // Download icon bất đồng bộ
//         EditorCoroutineHelper.Start(DownloadIcons(actT));

//         Debug.Log("[AvatarWidgetBuilder] ✅ Widget đã tạo xong. Đang download icon...");
//     }

//     // ══════════════════════════════════════════════════════════════
//     // Download icon từ internet
//     // ══════════════════════════════════════════════════════════════

//     private static IEnumerator DownloadIcons(Transform actT)
//     {
//         (string btn, string url, string file)[] list =
//         {
//             ("ShopBtn",      ICON_SHOP,      "icon_shop.png"),
//             ("InventoryBtn", ICON_INVENTORY, "icon_inventory.png"),
//             ("LogoutBtn",    ICON_LOGOUT,    "icon_logout.png"),
//         };

//         foreach (var (btn, url, file) in list)
//         {
//             string path = Path.Combine(ICON_SAVE_PATH, file);

//             if (!File.Exists(path))
//             {
//                 using var req = UnityWebRequestTexture.GetTexture(url);
//                 yield return req.SendWebRequest();

//                 if (req.result != UnityWebRequest.Result.Success)
//                 {
//                     Debug.LogWarning($"[Builder] Lỗi tải icon {file}: {req.error}");
//                     continue;
//                 }

//                 var tex = DownloadHandlerTexture.GetContent(req);
//                 File.WriteAllBytes(path, tex.EncodeToPNG());
//                 AssetDatabase.ImportAsset(path);

//                 var imp = (TextureImporter)AssetImporter.GetAtPath(path);
//                 if (imp != null)
//                 {
//                     imp.textureType         = TextureImporterType.Sprite;
//                     imp.spriteImportMode    = SpriteImportMode.Single;
//                     imp.alphaIsTransparency = true;
//                     imp.SaveAndReimport();
//                 }
//             }

//             var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
//             if (sprite == null) continue;

//             var iconImg = actT?.Find($"{btn}/Circle/Icon")?.GetComponent<Image>();
//             if (iconImg != null)
//             {
//                 iconImg.sprite         = sprite;
//                 iconImg.color          = Color.white;
//                 iconImg.preserveAspect = true;
//                 EditorUtility.SetDirty(iconImg);
//             }
//             Debug.Log($"[Builder] ✅ Icon OK: {btn}");
//         }

//         AssetDatabase.SaveAssets();
//         AssetDatabase.Refresh();
//     }

//     // ══════════════════════════════════════════════════════════════
//     // Room Button (tròn)
//     // ══════════════════════════════════════════════════════════════

//     private static void CreateRoomButton(string id, string label, int reqLv, Transform parent)
//     {
//         GameObject go = CreateUI(id, parent);
//         go.GetComponent<RectTransform>().sizeDelta = new Vector2(64f, 80f);

//         // Circle
//         GameObject circle  = CreateUI("Circle", go.transform);
//         Image circImg      = circle.AddComponent<Image>();
//         circImg.sprite     = BuildCircleSprite();
//         circImg.color      = new Color(0.22f, 0.18f, 0.14f, 1f);
//         circle.AddComponent<Mask>().showMaskGraphic = true;
//         circle.AddComponent<Button>();
//         RectTransform cRT  = circle.GetComponent<RectTransform>();
//         cRT.anchorMin = new Vector2(0.1f, 0.28f);
//         cRT.anchorMax = new Vector2(0.9f, 1f);
//         cRT.offsetMin = cRT.offsetMax = Vector2.zero;

//         // Room icon placeholder
//         GameObject iconGO = CreateUI("RoomIcon", circle.transform);
//         iconGO.AddComponent<Image>().color = new Color(0.65f, 0.6f, 0.55f, 0.8f);
//         Stretch(iconGO);

//         // Lock overlay
//         if (reqLv > 1)
//         {
//             GameObject lockGO = CreateUI("LockOverlay", circle.transform);
//             lockGO.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.65f);
//             Stretch(lockGO);

//             GameObject ltGO = CreateUI("LockText", lockGO.transform);
//             TMP_Text lt     = ltGO.AddComponent<TextMeshProUGUI>();
//             lt.text         = $"Lv.{reqLv}";
//             lt.fontSize     = 13;
//             lt.fontStyle    = FontStyles.Bold;
//             lt.alignment    = TextAlignmentOptions.Center;
//             lt.color        = new Color(1f, 0.85f, 0.2f, 1f);
//             Stretch(ltGO);
//         }

//         // Label
//         GameObject lblGO = CreateUI("Label", go.transform);
//         TMP_Text lbl     = lblGO.AddComponent<TextMeshProUGUI>();
//         lbl.text         = label;
//         lbl.fontSize     = 8;
//         lbl.alignment    = TextAlignmentOptions.Center;
//         lbl.color        = new Color(0.9f, 0.85f, 0.8f, 1f);
//         RectTransform lRT = lblGO.GetComponent<RectTransform>();
//         lRT.anchorMin = new Vector2(0f, 0f);
//         lRT.anchorMax = new Vector2(1f, 0.28f);
//         lRT.offsetMin = lRT.offsetMax = Vector2.zero;
//     }

//     // ══════════════════════════════════════════════════════════════
//     // Action Button (tròn: icon trên, text dưới)
//     // ══════════════════════════════════════════════════════════════

//     private static void CreateActionButton(string id, string label, Color color, Transform parent)
//     {
//         GameObject go = CreateUI(id, parent);
//         go.GetComponent<RectTransform>().sizeDelta = new Vector2(64f, 80f);

//         // Circle
//         GameObject circle  = CreateUI("Circle", go.transform);
//         Image circImg      = circle.AddComponent<Image>();
//         circImg.sprite     = BuildCircleSprite();
//         circImg.color      = color;
//         circle.AddComponent<Mask>().showMaskGraphic = true;
//         Button btn         = circle.AddComponent<Button>();
//         ColorBlock cb      = btn.colors;
//         cb.highlightedColor = new Color(
//             Mathf.Min(color.r + 0.15f, 1f),
//             Mathf.Min(color.g + 0.15f, 1f),
//             Mathf.Min(color.b + 0.15f, 1f), 1f);
//         cb.pressedColor = new Color(
//             Mathf.Max(color.r - 0.1f, 0f),
//             Mathf.Max(color.g - 0.1f, 0f),
//             Mathf.Max(color.b - 0.1f, 0f), 1f);
//         btn.colors = cb;
//         RectTransform cRT = circle.GetComponent<RectTransform>();
//         cRT.anchorMin = new Vector2(0.1f, 0.28f);
//         cRT.anchorMax = new Vector2(0.9f, 1f);
//         cRT.offsetMin = cRT.offsetMax = Vector2.zero;

//         // Icon (sẽ có sprite sau khi download)
//         GameObject iconGO      = CreateUI("Icon", circle.transform);
//         Image iconImg          = iconGO.AddComponent<Image>();
//         iconImg.color          = new Color(1f, 1f, 1f, 0.85f);
//         iconImg.preserveAspect = true;
//         RectTransform iRT      = iconGO.GetComponent<RectTransform>();
//         iRT.anchorMin = new Vector2(0.18f, 0.18f);
//         iRT.anchorMax = new Vector2(0.82f, 0.82f);
//         iRT.offsetMin = iRT.offsetMax = Vector2.zero;

//         // Label
//         GameObject lblGO = CreateUI("Label", go.transform);
//         TMP_Text lbl     = lblGO.AddComponent<TextMeshProUGUI>();
//         lbl.text         = label;   // plain text, không emoji
//         lbl.fontSize     = 9;
//         lbl.alignment    = TextAlignmentOptions.Center;
//         lbl.color        = new Color(0.9f, 0.85f, 0.8f, 1f);
//         RectTransform lRT = lblGO.GetComponent<RectTransform>();
//         lRT.anchorMin = new Vector2(0f, 0f);
//         lRT.anchorMax = new Vector2(1f, 0.28f);
//         lRT.offsetMin = lRT.offsetMax = Vector2.zero;
//     }

//     // ══════════════════════════════════════════════════════════════
//     // Procedural circle sprite (anti-aliased)
//     // ══════════════════════════════════════════════════════════════

//     private static Sprite BuildCircleSprite()
//     {
//         const int size = 128;
//         var tex        = new Texture2D(size, size, TextureFormat.RGBA32, false);
//         float r        = size * 0.5f;
//         for (int y = 0; y < size; y++)
//         for (int x = 0; x < size; x++)
//         {
//             float dx    = x - r + 0.5f;
//             float dy    = y - r + 0.5f;
//             float alpha = Mathf.Clamp01(r - Mathf.Sqrt(dx * dx + dy * dy));
//             tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
//         }
//         tex.Apply();
//         return Sprite.Create(tex,
//             new Rect(0, 0, size, size),
//             new Vector2(0.5f, 0.5f), 100f);
//     }

//     // ══════════════════════════════════════════════════════════════
//     // Tiện ích
//     // ══════════════════════════════════════════════════════════════

//     private static GameObject CreateUI(string name, Transform parent)
//     {
//         var go = new GameObject(name, typeof(RectTransform));
//         go.transform.SetParent(parent, false);
//         return go;
//     }

//     private static void Stretch(GameObject go)
//     {
//         var rt   = go.GetComponent<RectTransform>();
//         rt.anchorMin = Vector2.zero;
//         rt.anchorMax = Vector2.one;
//         rt.offsetMin = rt.offsetMax = Vector2.zero;
//     }

//     private static void SetHeight(GameObject go, float h)
//     {
//         var rt = go.GetComponent<RectTransform>();
//         rt.sizeDelta = new Vector2(rt.sizeDelta.x, h);
//     }

//     private static Canvas FindOrCreateCanvas(string name)
//     {
//         var found = GameObject.Find(name);
//         if (found != null) return found.GetComponent<Canvas>();
//         var go     = new GameObject(name);
//         var c      = go.AddComponent<Canvas>();
//         c.renderMode = RenderMode.ScreenSpaceOverlay;
//         go.AddComponent<CanvasScaler>();
//         go.AddComponent<GraphicRaycaster>();
//         return c;
//     }

//     private static void AssignRef(SerializedObject so, string field, Object obj)
//     {
//         var p = so.FindProperty(field);
//         if (p != null) p.objectReferenceValue = obj;
//         else Debug.LogWarning($"[Builder] Field không tìm thấy: {field}");
//     }

//     // Tìm Button trong Circle child
//     private static Button FindBtn(Transform t, string btnName)
//         => t.Find($"{btnName}/Circle")?.GetComponent<Button>();

//     private static GameObject FindGO(Transform t, string path)
//         => t.Find(path)?.gameObject;

//     private static TMP_Text FindTMP(Transform t, string path)
//         => t.Find(path)?.GetComponent<TMP_Text>();
// }

// // ══════════════════════════════════════════════════════════════════
// // Chạy coroutine trong Editor (không cần MonoBehaviour)
// // ══════════════════════════════════════════════════════════════════
// public static class EditorCoroutineHelper
// {
//     public static void Start(IEnumerator routine)
//         => Step(routine);

//     private static void Step(IEnumerator routine)
//     {
//         if (!routine.MoveNext()) return;
//         if (routine.Current is UnityWebRequestAsyncOperation op)
//             op.completed += _ => Step(routine);
//         else
//             EditorApplication.delayCall += () => Step(routine);
//     }
// }
// #endif