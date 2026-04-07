// #if UNITY_EDITOR
// using UnityEngine;
// using UnityEngine.UI;
// using UnityEditor;
// using TMPro;

// // ─────────────────────────────────────────────────────────────────────────────
// // AddLessonPanelBuilder.cs — v6
// // Tools > Rebuild AddLessonPanel
// //
// // FIX so với v5:
// //   1. InputField tên bài KHÔNG bị ScrollRect chặn
// //      → Tên bài nằm NGOÀI ScrollRect, ở cố định phía trên
// //   2. Thêm nhiều hơn 4 dòng được
// //      → WordScrollRect chỉ bao phần danh sách từ, không bao cả popup
// //      → PopupBox dùng anchorMin/anchorMax cố định (không phụ thuộc ContentSizeFitter)
// //   3. Layout WordRow: [Field Anh] [Field Việt] [X nhỏ 36px]
// //
// // Hierarchy sau khi build:
// //   AddLessonPanel (dim overlay, fullscreen)
// //     PopupBox (anchor 4%-96% x, 5%-95% y)
// //       TopBar (title + X)
// //       NameSection (label + input) ← NGOÀI scrollview, nhập được bình thường
// //       HeaderRow (nhãn cột Anh / Việt)
// //       WordScrollRect (ScrollRect — chỉ phần danh sách từ)
// //         Viewport
// //           WordListContent (VLG + CSF) ← AddLessonPanel.cs insert rows vào đây
// //       BottomBar (AddRowButton | StatusText | SaveButton)
// // ─────────────────────────────────────────────────────────────────────────────

// public class AddLessonPanelBuilder : Editor
// {
//     private static readonly Color COL_WHITE    = Color.white;
//     private static readonly Color COL_BLUE     = new Color(0.20f, 0.55f, 0.95f, 1f);
//     private static readonly Color COL_GREEN    = new Color(0.22f, 0.72f, 0.42f, 1f);
//     private static readonly Color COL_RED      = new Color(0.88f, 0.28f, 0.28f, 1f);
//     private static readonly Color COL_BG_POPUP = new Color(0.97f, 0.97f, 0.97f, 1f);
//     private static readonly Color COL_TOPBAR   = new Color(0.25f, 0.52f, 0.88f, 1f);
//     private static readonly Color COL_INPUT_BG = new Color(0.91f, 0.91f, 0.91f, 1f);
//     private static readonly Color COL_HEADER   = new Color(0.85f, 0.85f, 0.85f, 1f);

//     [MenuItem("Tools/Rebuild AddLessonPanel")]
//     public static void RebuildAddLessonPanel()
//     {
//         // ── Tìm AddLessonPanel ───────────────────────────────────────────────
//         var addPanelGO = GameObject.Find("AddLessonPanel");
//         if (addPanelGO == null)
//         {
//             var vocabCanvas = GameObject.Find("VocabCanvas");
//             if (vocabCanvas != null)
//                 addPanelGO = FindDeep(vocabCanvas.transform, "AddLessonPanel")?.gameObject;
//         }

//         Transform panelParent = null;
//         if (addPanelGO == null)
//         {
//             var bg = GameObject.Find("Background");
//             panelParent = bg != null ? bg.transform : null;
//             if (panelParent == null)
//             {
//                 var vc = GameObject.Find("VocabCanvas");
//                 panelParent = vc != null ? vc.transform : null;
//             }
//             addPanelGO = new GameObject("AddLessonPanel");
//             if (panelParent != null) addPanelGO.transform.SetParent(panelParent, false);
//         }
//         else
//         {
//             panelParent = addPanelGO.transform.parent;
//         }

//         // ── Xóa children cũ ─────────────────────────────────────────────────
//         for (int i = addPanelGO.transform.childCount - 1; i >= 0; i--)
//             DestroyImmediate(addPanelGO.transform.GetChild(i).gameObject);
//         var oldComp = addPanelGO.GetComponent<AddLessonPanel>();
//         if (oldComp != null) DestroyImmediate(oldComp);

//         // ── Root overlay (dim fullscreen) ────────────────────────────────────
//         var rootRT  = addPanelGO.GetComponent<RectTransform>() ?? addPanelGO.AddComponent<RectTransform>();
//         Stretch(rootRT);
//         var rootImg = addPanelGO.GetComponent<Image>() ?? addPanelGO.AddComponent<Image>();
//         rootImg.color = new Color(0f, 0f, 0f, 0.55f);
//         addPanelGO.SetActive(false);

//         // ── PopupBox ─────────────────────────────────────────────────────────
//         var popupBox  = new GameObject("PopupBox", typeof(RectTransform));
//         popupBox.transform.SetParent(addPanelGO.transform, false);
//         var popupRT   = popupBox.GetComponent<RectTransform>();
//         // Chiếm 92% chiều ngang, 90% chiều dọc, căn giữa
//         popupRT.anchorMin        = new Vector2(0.04f, 0.05f);
//         popupRT.anchorMax        = new Vector2(0.96f, 0.95f);
//         popupRT.offsetMin        = Vector2.zero;
//         popupRT.offsetMax        = Vector2.zero;
//         popupBox.AddComponent<Image>().color = COL_BG_POPUP;

//         // Dùng VerticalLayoutGroup để xếp TopBar / NameSection / HeaderRow / Scroll / Bottom
//         var popupVLG  = popupBox.AddComponent<VerticalLayoutGroup>();
//         popupVLG.spacing               = 0;
//         popupVLG.padding               = new RectOffset(0, 0, 0, 0);
//         popupVLG.childForceExpandWidth = true;
//         popupVLG.childForceExpandHeight = false;
//         popupVLG.childControlWidth     = true;
//         popupVLG.childControlHeight    = true;

//         // ── 1. TopBar ────────────────────────────────────────────────────────
//         var topBar   = new GameObject("TopBar", typeof(RectTransform));
//         topBar.transform.SetParent(popupBox.transform, false);
//         topBar.AddComponent<Image>().color = COL_TOPBAR;
//         var topLE    = topBar.AddComponent<LayoutElement>();
//         topLE.minHeight       = 60;
//         topLE.preferredHeight = 60;
//         topLE.flexibleHeight  = 0;

//         var topHLG   = topBar.AddComponent<HorizontalLayoutGroup>();
//         topHLG.padding              = new RectOffset(16, 8, 0, 0);
//         topHLG.childAlignment       = TextAnchor.MiddleLeft;
//         topHLG.childForceExpandWidth  = false;
//         topHLG.childForceExpandHeight = true;
//         topHLG.childControlWidth      = false;
//         topHLG.childControlHeight     = false;

//         var titleGO  = MakeTMP(topBar.transform, "Title", "Them bai moi", 20,
//                                Color.white, FontStyles.Bold);
//         titleGO.GetComponent<LayoutElement>().flexibleWidth = 1;

//         var closeBtn = MakeButton(topBar.transform, "CloseButton", "X", 18,
//                                   COL_RED, new Vector2(52, 52));

//         // ── 2. NameSection ───────────────────────────────────────────────────
//         // NẰNG NGOÀI ScrollRect → nhập được bình thường
//         var nameSection = new GameObject("NameSection", typeof(RectTransform));
//         nameSection.transform.SetParent(popupBox.transform, false);
//         var nameSectionLE = nameSection.AddComponent<LayoutElement>();
//         nameSectionLE.minHeight       = 76;
//         nameSectionLE.preferredHeight = 76;
//         nameSectionLE.flexibleHeight  = 0;

//         var nameVLG = nameSection.AddComponent<VerticalLayoutGroup>();
//         nameVLG.padding               = new RectOffset(12, 12, 8, 4);
//         nameVLG.spacing               = 2;
//         nameVLG.childForceExpandWidth = true;
//         nameVLG.childControlHeight    = false;
//         nameVLG.childControlWidth     = true;

//         var nameLbl  = MakeTMP(nameSection.transform, "NameLabel", "Ten bai hoc", 12,
//                                new Color(0.4f, 0.4f, 0.4f), FontStyles.Normal);
//         nameLbl.GetComponent<LayoutElement>().minHeight = 18;

//         var nameInput = MakeInputField(nameSection.transform, "InputField_LessonName",
//                                        "Nhap ten bai hoc...");
//         nameInput.GetComponent<LayoutElement>().preferredHeight = 44;

//         // ── 3. HeaderRow (nhãn cột) ──────────────────────────────────────────
//         var headerRow  = new GameObject("HeaderRow", typeof(RectTransform));
//         headerRow.transform.SetParent(popupBox.transform, false);
//         headerRow.AddComponent<Image>().color = COL_HEADER;
//         var headerLE   = headerRow.AddComponent<LayoutElement>();
//         headerLE.minHeight       = 28;
//         headerLE.preferredHeight = 28;
//         headerLE.flexibleHeight  = 0;

//         var headerHLG  = headerRow.AddComponent<HorizontalLayoutGroup>();
//         headerHLG.padding               = new RectOffset(8, 8, 4, 4);
//         headerHLG.spacing               = 6;
//         headerHLG.childForceExpandWidth = true;
//         headerHLG.childControlWidth     = true;
//         headerHLG.childControlHeight    = true;

//         var hEng  = MakeTMP(headerRow.transform, "H_Eng",  "Tieng Anh",  12,
//                             new Color(0.35f, 0.35f, 0.35f), FontStyles.Bold);
//         hEng.GetComponent<LayoutElement>().flexibleWidth = 1;

//         var hViet = MakeTMP(headerRow.transform, "H_Viet", "Tieng Viet", 12,
//                             new Color(0.35f, 0.35f, 0.35f), FontStyles.Bold);
//         hViet.GetComponent<LayoutElement>().flexibleWidth = 1;

//         // spacer cho cột X
//         var hSpacer = new GameObject("H_Spacer", typeof(RectTransform));
//         hSpacer.transform.SetParent(headerRow.transform, false);
//         var hSpacerLE = hSpacer.AddComponent<LayoutElement>();
//         hSpacerLE.minWidth    = 42;
//         hSpacerLE.flexibleWidth = 0;

//         // ── 4. WordScrollRect — chỉ bao danh sách từ ─────────────────────────
//         var scrollGO    = new GameObject("WordScrollRect", typeof(RectTransform));
//         scrollGO.transform.SetParent(popupBox.transform, false);
//         var scrollLE    = scrollGO.AddComponent<LayoutElement>();
//         scrollLE.flexibleHeight = 1;  // chiếm toàn bộ không gian còn lại
//         scrollLE.minHeight      = 80;
//         scrollGO.AddComponent<Image>().color = new Color(0.93f, 0.93f, 0.93f, 1f);

//         var scrollRect  = scrollGO.AddComponent<ScrollRect>();
//         scrollRect.horizontal          = false;
//         scrollRect.scrollSensitivity   = 30;

//         var viewport    = new GameObject("Viewport", typeof(RectTransform));
//         viewport.transform.SetParent(scrollGO.transform, false);
//         Stretch(viewport.GetComponent<RectTransform>());
//         viewport.AddComponent<Image>().color = new Color(1, 1, 1, 0.01f);
//         viewport.AddComponent<Mask>().showMaskGraphic = false;

//         var wordListContent = new GameObject("WordListContent", typeof(RectTransform));
//         wordListContent.transform.SetParent(viewport.transform, false);
//         var wcRT = wordListContent.GetComponent<RectTransform>();
//         wcRT.anchorMin = new Vector2(0, 1);
//         wcRT.anchorMax = new Vector2(1, 1);
//         wcRT.pivot     = new Vector2(0.5f, 1);
//         wcRT.anchoredPosition = Vector2.zero;
//         wcRT.sizeDelta        = Vector2.zero;

//         var wcVLG = wordListContent.AddComponent<VerticalLayoutGroup>();
//         wcVLG.spacing                = 3;
//         wcVLG.padding                = new RectOffset(6, 6, 4, 4);
//         wcVLG.childForceExpandWidth  = true;
//         wcVLG.childForceExpandHeight = false;
//         wcVLG.childControlWidth      = true;
//         wcVLG.childControlHeight     = false;

//         wordListContent.AddComponent<ContentSizeFitter>().verticalFit =
//             ContentSizeFitter.FitMode.PreferredSize;

//         scrollRect.viewport = viewport.GetComponent<RectTransform>();
//         scrollRect.content  = wcRT;

//         // ── 5. BottomBar (AddRowButton | StatusText | SaveButton) ─────────────
//         var bottomBar  = new GameObject("BottomBar", typeof(RectTransform));
//         bottomBar.transform.SetParent(popupBox.transform, false);
//         bottomBar.AddComponent<Image>().color = new Color(0.94f, 0.94f, 0.94f, 1f);
//         var bottomLE   = bottomBar.AddComponent<LayoutElement>();
//         bottomLE.minHeight       = 64;
//         bottomLE.preferredHeight = 64;
//         bottomLE.flexibleHeight  = 0;

//         var bottomHLG  = bottomBar.AddComponent<HorizontalLayoutGroup>();
//         bottomHLG.padding               = new RectOffset(12, 12, 8, 8);
//         bottomHLG.spacing               = 8;
//         bottomHLG.childAlignment        = TextAnchor.MiddleCenter;
//         bottomHLG.childForceExpandWidth = false;
//         bottomHLG.childControlWidth     = false;
//         bottomHLG.childControlHeight    = false;

//         var addRowBtn  = MakeButton(bottomBar.transform, "AddRowButton", "+ Them tu",
//                                     15, COL_BLUE, new Vector2(150, 46));
//         addRowBtn.GetComponent<LayoutElement>().flexibleWidth = 0;

//         var statusSection = new GameObject("StatusSection", typeof(RectTransform));
//         statusSection.transform.SetParent(bottomBar.transform, false);
//         var statusSectionLE = statusSection.AddComponent<LayoutElement>();
//         statusSectionLE.flexibleWidth = 1;
//         var statusTMP = statusSection.AddComponent<TextMeshProUGUI>();
//         statusTMP.fontSize  = 13;
//         statusTMP.alignment = TextAlignmentOptions.Center;
//         statusTMP.color     = Color.gray;

//         var saveBtn = MakeButton(bottomBar.transform, "SaveButton", "Luu bai",
//                                  15, COL_GREEN, new Vector2(150, 46));
//         saveBtn.GetComponent<LayoutElement>().flexibleWidth = 0;

//         // ══════════════════════════════════════════════════════════════════════
//         // WIRE AddLessonPanel component
//         // ══════════════════════════════════════════════════════════════════════
//         var comp   = addPanelGO.AddComponent<AddLessonPanel>();
//         var compSO = new SerializedObject(comp);

//         SetProp(compSO, "lessonNameInput", nameInput.GetComponent<TMP_InputField>());
//         SetProp(compSO, "wordScrollRect",  scrollRect);
//         SetProp(compSO, "wordListContent", wordListContent.transform);
//         SetProp(compSO, "addRowButton",    addRowBtn.GetComponent<Button>());
//         SetProp(compSO, "saveButton",      saveBtn.GetComponent<Button>());
//         SetProp(compSO, "closeButton",     closeBtn.GetComponent<Button>());
//         SetProp(compSO, "statusText",      statusTMP);

//         compSO.ApplyModifiedProperties();
//         EditorUtility.SetDirty(addPanelGO);

//         // ── Wire VocabCanvasController ────────────────────────────────────────
//         var ctrl = addPanelGO.GetComponentInParent<VocabCanvasController>();
//         if (ctrl == null)
//         {
//             var vc = GameObject.Find("VocabCanvas");
//             if (vc != null) ctrl = vc.GetComponent<VocabCanvasController>();
//         }
//         if (ctrl != null)
//         {
//             var ctrlSO = new SerializedObject(ctrl);
//             SetProp(ctrlSO, "addLessonPanel", addPanelGO);
//             ctrlSO.ApplyModifiedProperties();
//             EditorUtility.SetDirty(ctrl);
//         }

//         UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
//             UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
//         Selection.activeGameObject = addPanelGO;

//         Debug.Log("[AddLessonPanelBuilder v6] Xong!\n" +
//                   "Hierarchy:\n" +
//                   "  AddLessonPanel (dim overlay)\n" +
//                   "    PopupBox (VLG)\n" +
//                   "      TopBar: [Title] [X]\n" +
//                   "      NameSection: [Label] [InputField]  ← NGOAI scroll\n" +
//                   "      HeaderRow: [Tieng Anh] [Tieng Viet] [spacer]\n" +
//                   "      WordScrollRect > Viewport > WordListContent (VLG+CSF)\n" +
//                   "      BottomBar: [+Them tu] [Status] [Luu bai]\n");
//     }

//     // ═════════════════════════════════════════════════════════════════════════
//     // HELPERS
//     // ═════════════════════════════════════════════════════════════════════════

//     private static GameObject MakeButton(Transform parent, string name,
//         string label, int fontSize, Color bgColor, Vector2 size)
//     {
//         var go  = new GameObject(name, typeof(RectTransform));
//         go.transform.SetParent(parent, false);
//         go.GetComponent<RectTransform>().sizeDelta = size;
//         go.AddComponent<Image>().color = bgColor;
//         go.AddComponent<Button>();
//         go.AddComponent<LayoutElement>();

//         var lbl  = new GameObject("Label", typeof(RectTransform));
//         lbl.transform.SetParent(go.transform, false);
//         var lblRT = lbl.GetComponent<RectTransform>();
//         lblRT.anchorMin = Vector2.zero; lblRT.anchorMax = Vector2.one;
//         lblRT.offsetMin = lblRT.offsetMax = Vector2.zero;
//         var tmp  = lbl.AddComponent<TextMeshProUGUI>();
//         tmp.text      = label;
//         tmp.fontSize  = fontSize;
//         tmp.color     = Color.white;
//         tmp.fontStyle = FontStyles.Bold;
//         tmp.alignment = TextAlignmentOptions.Center;
//         return go;
//     }

//     private static GameObject MakeTMP(Transform parent, string name, string text,
//         int size, Color color, FontStyles style)
//     {
//         var go  = new GameObject(name, typeof(RectTransform));
//         go.transform.SetParent(parent, false);
//         go.AddComponent<LayoutElement>();
//         var tmp = go.AddComponent<TextMeshProUGUI>();
//         tmp.text      = text;
//         tmp.fontSize  = size;
//         tmp.color     = color;
//         tmp.fontStyle = style;
//         tmp.alignment = TextAlignmentOptions.MidlineLeft;
//         return go;
//     }

//     private static GameObject MakeInputField(Transform parent, string name, string placeholder)
//     {
//         var go    = new GameObject(name, typeof(RectTransform));
//         go.transform.SetParent(parent, false);
//         go.AddComponent<Image>().color = new Color(0.91f, 0.91f, 0.91f);
//         go.AddComponent<LayoutElement>();

//         var field = go.AddComponent<TMP_InputField>();

//         var ta    = new GameObject("Text Area", typeof(RectTransform));
//         ta.transform.SetParent(go.transform, false);
//         var taRT  = ta.GetComponent<RectTransform>();
//         taRT.anchorMin = Vector2.zero; taRT.anchorMax = Vector2.one;
//         taRT.offsetMin = new Vector2(10, 4); taRT.offsetMax = new Vector2(-10, -4);
//         ta.AddComponent<RectMask2D>();

//         var ph    = new GameObject("Placeholder", typeof(RectTransform));
//         ph.transform.SetParent(ta.transform, false);
//         Stretch(ph.GetComponent<RectTransform>());
//         var phT   = ph.AddComponent<TextMeshProUGUI>();
//         phT.text      = placeholder; phT.fontSize = 15;
//         phT.color     = new Color(0.6f, 0.6f, 0.6f);
//         phT.fontStyle = FontStyles.Italic;

//         var txt   = new GameObject("Text", typeof(RectTransform));
//         txt.transform.SetParent(ta.transform, false);
//         Stretch(txt.GetComponent<RectTransform>());
//         var txtT  = txt.AddComponent<TextMeshProUGUI>();
//         txtT.fontSize = 15; txtT.color = Color.black;

//         field.textViewport  = taRT;
//         field.textComponent = txtT;
//         field.placeholder   = phT;
//         return go;
//     }

//     private static void Stretch(RectTransform rt)
//     {
//         rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
//         rt.offsetMin = rt.offsetMax = Vector2.zero;
//     }

//     private static void SetProp(SerializedObject so, string prop, Object val)
//     {
//         var p = so.FindProperty(prop);
//         if (p != null) p.objectReferenceValue = val;
//         else Debug.LogWarning($"[AddLessonPanelBuilder] Khong tim thay property: '{prop}'");
//     }

//     private static Transform FindDeep(Transform root, string name)
//     {
//         if (root.name == name) return root;
//         foreach (Transform c in root)
//         {
//             var f = FindDeep(c, name);
//             if (f != null) return f;
//         }
//         return null;
//     }
// }
// #endif