// Assets/Editor/DoraemonBuilder.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEditor;

public class DoraemonBuilder : Editor
{
    [MenuItem("Tools/Build Doraemon + HelpCanvas")]
    public static void Build()
    {
        Canvas root = FindObjectOfType<Canvas>();
        if (!root) { Debug.LogError("Không tìm thấy Canvas!"); return; }
        Transform rootT = root.transform;

        DestroyOld(rootT, "DoraemonHelper");
        DestroyOld(rootT, "HelpCanvas");

        // ── Doraemon ──
        GameObject dora = new GameObject("DoraemonHelper");
        dora.transform.SetParent(rootT, false);
        Canvas doraCanvas = dora.AddComponent<Canvas>();
        doraCanvas.overrideSorting = true;
        doraCanvas.sortingOrder = 60;
        dora.AddComponent<GraphicRaycaster>();
        Image doraImg = dora.AddComponent<Image>();
        doraImg.color = Color.white;
        doraImg.raycastTarget = true;
        RectTransform doraRT = dora.GetComponent<RectTransform>(); // ✅ Get không Add
        doraRT.anchorMin = doraRT.anchorMax = Vector2.zero;
        doraRT.pivot = Vector2.zero;
        doraRT.anchoredPosition = new Vector2(20f, 20f);
        doraRT.sizeDelta = new Vector2(90f, 90f);
        dora.AddComponent<DoraemonHelper>();

        // ── HelpCanvas ──
        GameObject helpGO = new GameObject("HelpCanvas");
        helpGO.transform.SetParent(rootT, false);
        Canvas helpCanvas = helpGO.AddComponent<Canvas>();
        helpCanvas.overrideSorting = true;
        helpCanvas.sortingOrder = 70;
        helpGO.AddComponent<GraphicRaycaster>();
        Fullscreen(helpGO.GetComponent<RectTransform>()); // ✅ Get không Add

        // Backdrop
        GameObject backdrop = new GameObject("Backdrop");
        backdrop.transform.SetParent(helpGO.transform, false);
        backdrop.AddComponent<Image>().color = new Color(0, 0, 0, 0.7f);
        Fullscreen(backdrop.GetComponent<RectTransform>()); // ✅ Get không Add

        // HelpPanel
        GameObject panel = new GameObject("HelpPanel");
        panel.transform.SetParent(helpGO.transform, false);
        panel.AddComponent<Image>().color = new Color(0.1f, 0.07f, 0.2f, 0.97f);
        RectTransform panelRT = panel.GetComponent<RectTransform>(); // ✅
        panelRT.anchorMin = new Vector2(0.05f, 0.10f);
        panelRT.anchorMax = new Vector2(0.95f, 0.93f);
        panelRT.offsetMin = panelRT.offsetMax = Vector2.zero;

        // CloseButton
        GameObject closeBtn = MakeButton(panel.transform, "CloseButton", "✕", 22, new Color(0.8f, 0.2f, 0.2f));
        RectTransform closeRT = closeBtn.GetComponent<RectTransform>();
        closeRT.anchorMin = closeRT.anchorMax = Vector2.one;
        closeRT.pivot = Vector2.one;
        closeRT.anchoredPosition = new Vector2(-10f, -10f);
        closeRT.sizeDelta = new Vector2(44f, 44f);

        // Title
        GameObject titleGO = new GameObject("Title");
        titleGO.transform.SetParent(panel.transform, false);
        var titleTMP = titleGO.AddComponent<TextMeshProUGUI>();
        titleTMP.text = "Trợ giúp";
        titleTMP.fontSize = 26;
        titleTMP.color = Color.white;
        titleTMP.alignment = TextAlignmentOptions.Center;
        RectTransform titleRT = titleGO.GetComponent<RectTransform>();
        titleRT.anchorMin = new Vector2(0.05f, 0.87f);
        titleRT.anchorMax = new Vector2(0.95f, 0.97f);
        titleRT.offsetMin = titleRT.offsetMax = Vector2.zero;

        // TabBar
        GameObject tabBar = new GameObject("TabBar");
        tabBar.transform.SetParent(panel.transform, false);
        var hlg = tabBar.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 8;
        hlg.padding = new RectOffset(8, 8, 4, 4);
        hlg.childForceExpandWidth  = true;
        hlg.childForceExpandHeight = true;
        RectTransform tabBarRT = tabBar.GetComponent<RectTransform>();
        tabBarRT.anchorMin = new Vector2(0f, 0.78f);
        tabBarRT.anchorMax = new Vector2(1f, 0.87f);
        tabBarRT.offsetMin = tabBarRT.offsetMax = Vector2.zero;

        GameObject tabFunc = MakeButton(tabBar.transform, "Tab_Function",  "Chức năng", 18, new Color(0.3f, 0.3f, 0.3f));
        GameObject tabHTP  = MakeButton(tabBar.transform, "Tab_HowToPlay", "Cách chơi",  18, new Color(0.3f, 0.3f, 0.3f));

        // FunctionPanel
        GameObject funcPanel = new GameObject("FunctionPanel");
        funcPanel.transform.SetParent(panel.transform, false);
        funcPanel.AddComponent<Image>().color = Color.clear;
        RectTransform funcPanelRT = funcPanel.GetComponent<RectTransform>();
        funcPanelRT.anchorMin = new Vector2(0f, 0.02f);
        funcPanelRT.anchorMax = new Vector2(1f, 0.78f);
        funcPanelRT.offsetMin = funcPanelRT.offsetMax = Vector2.zero;
        TextMeshProUGUI funcTMP = BuildScrollView(funcPanel.transform, "FunctionScrollView");

        // HowToPlayPanel
        GameObject htpPanel = new GameObject("HowToPlayPanel");
        htpPanel.transform.SetParent(panel.transform, false);
        htpPanel.AddComponent<Image>().color = Color.clear;
        RectTransform htpPanelRT = htpPanel.GetComponent<RectTransform>();
        htpPanelRT.anchorMin = new Vector2(0f, 0.02f);
        htpPanelRT.anchorMax = new Vector2(1f, 0.78f);
        htpPanelRT.offsetMin = htpPanelRT.offsetMax = Vector2.zero;
        TextMeshProUGUI htpTMP = BuildScrollView(htpPanel.transform, "HowToPlayScrollView");

        // Gán HelpCanvasController
        HelpCanvasController ctrl = helpGO.AddComponent<HelpCanvasController>();
        SerializedObject so = new SerializedObject(ctrl);
        so.FindProperty("tabFunctionBtn").objectReferenceValue  = tabFunc.GetComponent<Button>();
        so.FindProperty("tabHowToPlayBtn").objectReferenceValue = tabHTP.GetComponent<Button>();
        so.FindProperty("closeButton").objectReferenceValue     = closeBtn.GetComponent<Button>();
        so.FindProperty("functionPanel").objectReferenceValue   = funcPanel;
        so.FindProperty("howToPlayPanel").objectReferenceValue  = htpPanel;
        so.FindProperty("functionText").objectReferenceValue    = funcTMP;
        so.FindProperty("howToPlayText").objectReferenceValue   = htpTMP;
        so.ApplyModifiedPropertiesWithoutUndo();

        // Gán helpCanvas vào DoraemonHelper
        DoraemonHelper doraHelper = dora.GetComponent<DoraemonHelper>();
        SerializedObject doraSO = new SerializedObject(doraHelper);
        doraSO.FindProperty("helpCanvas").objectReferenceValue = helpGO;
        doraSO.ApplyModifiedPropertiesWithoutUndo();

        helpGO.SetActive(false);

        EditorUtility.SetDirty(dora);
        EditorUtility.SetDirty(helpGO);
        Selection.activeGameObject = dora;
        Debug.Log("[DoraemonBuilder] ✅ Build xong!");
    }

    static TextMeshProUGUI BuildScrollView(Transform parent, string svName)
    {
        // ScrollView
        GameObject sv = new GameObject(svName);
        sv.transform.SetParent(parent, false);
        // ✅ new GameObject không có RectTransform → AddComponent
        RectTransform svRT = sv.AddComponent<RectTransform>();
        Fullscreen(svRT);
        sv.AddComponent<Image>().color = Color.clear;
        ScrollRect sr = sv.AddComponent<ScrollRect>();
        sr.horizontal        = false;
        sr.vertical          = true;
        sr.movementType      = ScrollRect.MovementType.Clamped;
        sr.scrollSensitivity = 30f;

        // Viewport
        GameObject vp = new GameObject("Viewport");
        vp.transform.SetParent(sv.transform, false);
        RectTransform vpRT = vp.AddComponent<RectTransform>();
        Fullscreen(vpRT);
        Image vpImg = vp.AddComponent<Image>();
        vpImg.color = new Color(1f, 1f, 1f, 0.004f); // ✅ alpha > 0 để Mask hoạt động
        vp.AddComponent<Mask>().showMaskGraphic = false;

        // Content
        GameObject content = new GameObject("Content");
        content.transform.SetParent(vp.transform, false);
        RectTransform cRT = content.AddComponent<RectTransform>();
        cRT.anchorMin        = new Vector2(0f, 1f);
        cRT.anchorMax        = new Vector2(1f, 1f);
        cRT.pivot            = new Vector2(0.5f, 1f);
        cRT.anchoredPosition = Vector2.zero;
        cRT.sizeDelta        = Vector2.zero;
        VerticalLayoutGroup vlg = content.AddComponent<VerticalLayoutGroup>();
        vlg.padding             = new RectOffset(12, 12, 12, 12);
        vlg.spacing             = 0f;
        vlg.childForceExpandWidth  = true;
        vlg.childForceExpandHeight = false;
        vlg.childControlWidth      = true;
        vlg.childControlHeight     = true;
        content.AddComponent<ContentSizeFitter>().verticalFit
            = ContentSizeFitter.FitMode.PreferredSize;

        // Text
        GameObject textGO = new GameObject("Text");
        textGO.transform.SetParent(content.transform, false);
        TextMeshProUGUI tmp = textGO.AddComponent<TextMeshProUGUI>();
        tmp.fontSize           = 17;
        tmp.color              = Color.white;
        tmp.enableWordWrapping = true;
        tmp.overflowMode       = TextOverflowModes.Overflow;
        tmp.text               = "...";

        sr.viewport = vpRT;
        sr.content  = cRT;

        return tmp;
    }

    static void Fullscreen(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        rt.sizeDelta = Vector2.zero;
    }

    static void DestroyOld(Transform parent, string name)
    {
        Transform old = parent.Find(name);
        if (old) DestroyImmediate(old.gameObject);
    }

    static GameObject MakeButton(Transform parent, string goName, string label,
        int fontSize, Color bgColor)
    {
        GameObject go = new GameObject(goName);
        go.transform.SetParent(parent, false);
        go.AddComponent<Image>().color = bgColor;
        go.AddComponent<Button>();
        GameObject txt = new GameObject("Text");
        txt.transform.SetParent(go.transform, false);
        var tmp = txt.AddComponent<TextMeshProUGUI>();
        tmp.text      = label;
        tmp.fontSize  = fontSize;
        tmp.color     = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;
        RectTransform tRT = txt.GetComponent<RectTransform>();
        tRT.anchorMin = Vector2.zero;
        tRT.anchorMax = Vector2.one;
        tRT.offsetMin = tRT.offsetMax = Vector2.zero;
        return go;
    }
}