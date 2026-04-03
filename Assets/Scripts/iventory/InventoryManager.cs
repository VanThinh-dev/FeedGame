using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using TMPro;
using Firebase.Database;
using Firebase.Auth;

// ═══════════════════════════════════════════════════════════════
// InventoryItemData — dữ liệu 1 item trong túi đồ
// ═══════════════════════════════════════════════════════════════
[Serializable]
public class InventoryItemData
{
    public string itemId;
    public int    quantity;
    public long   purchasedAt;

    // Lấy từ shop_items
    public string name;
    public string imageUrl;
    public string description;
    public int    price;
    public int    ownerCount;
}

// ═══════════════════════════════════════════════════════════════
// InventoryManager — singleton quản lý Inventory Canvas
// ═══════════════════════════════════════════════════════════════
public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance { get; private set; }

    // ── References (gán trên Inspector) ──────────────────────────
    [Header("Canvas")]
    public GameObject inventoryCanvas;

    [Header("Close")]
    public Button closeBtn;

    [Header("Item List (trái)")]
    public Transform  itemListGrid;
    public GameObject inventoryCardPrefab;

    [Header("Detail Panel (phải)")]
    public GameObject detailPanel;
    public Image      detailImage;
    public TMP_Text   detailNameText;
    public TMP_Text   detailDescText;
    public TMP_Text   ownerCountText;
    public TMP_Text   quantityOwnedText;

    // ── Dữ liệu nội bộ ─────────────────────────────────────────
    private List<InventoryItemData> itemList     = new();
    private List<GameObject>        spawnedCards = new();

    // ── Trạng thái slide ─────────────────────────────────────────
    private bool      detailVisible  = false;
    private Coroutine slideCoroutine;

    // ══════════════════════════════════════════════════════════
    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        closeBtn?.onClick.AddListener(CloseInventory);
        inventoryCanvas?.SetActive(false);
        detailPanel?.SetActive(false);
    }

    // ══════════════════════════════════════════════════════════
    // Mở Inventory
    // ══════════════════════════════════════════════════════════
    public void OpenInventory()
    {
        // QUAN TRỌNG: SetActive(true) TRƯỚC khi StartCoroutine
        // vì coroutine không chạy được trên inactive GameObject
        inventoryCanvas?.SetActive(true);
        detailPanel?.SetActive(false);
        detailVisible = false;

        StopAllCoroutines();
        StartCoroutine(LoadInventory());
    }

    public void CloseInventory()
    {
        inventoryCanvas?.SetActive(false);
    }

    // ══════════════════════════════════════════════════════════
    // Load dữ liệu từ Firebase
    // ══════════════════════════════════════════════════════════
    private IEnumerator LoadInventory()
    {
        string uid = FirebaseAuth.DefaultInstance?.CurrentUser?.UserId;
        if (string.IsNullOrEmpty(uid)) yield break;

        var invTask = FirebaseDatabase.DefaultInstance
            .GetReference($"inventory/{uid}").GetValueAsync();
        yield return new WaitUntil(() => invTask.IsCompleted);

        if (invTask.Exception != null) { Debug.LogError(invTask.Exception); yield break; }

        if (!invTask.Result.Exists) { BuildGrid(); yield break; }

        var shopTask = FirebaseDatabase.DefaultInstance
            .GetReference("shop_items").GetValueAsync();
        yield return new WaitUntil(() => shopTask.IsCompleted);

        var shopMap = new Dictionary<string, DataSnapshot>();
        if (shopTask.Result.Exists)
            foreach (DataSnapshot s in shopTask.Result.Children)
                shopMap[s.Key] = s;

        itemList.Clear();
        foreach (DataSnapshot child in invTask.Result.Children)
        {
            var item = new InventoryItemData
            {
                itemId      = child.Key,
                quantity    = int.Parse(child.Child("quantity").Value?.ToString() ?? "0"),
                purchasedAt = long.Parse(child.Child("purchasedAt").Value?.ToString() ?? "0"),
            };

            if (shopMap.TryGetValue(item.itemId, out var shopSnap))
            {
                item.name        = shopSnap.Child("name").Value?.ToString()        ?? item.itemId;
                item.imageUrl    = shopSnap.Child("imageUrl").Value?.ToString()    ?? "";
                item.description = shopSnap.Child("description").Value?.ToString() ?? "";
                item.price       = int.Parse(shopSnap.Child("price").Value?.ToString()      ?? "0");
                item.ownerCount  = int.Parse(shopSnap.Child("ownerCount").Value?.ToString() ?? "0");
            }

            itemList.Add(item);
        }

        BuildGrid();
    }

    // ══════════════════════════════════════════════════════════
    // Xây dựng grid
    // ══════════════════════════════════════════════════════════
    private void BuildGrid()
    {
        foreach (var c in spawnedCards) Destroy(c);
        spawnedCards.Clear();

        foreach (var item in itemList)
        {
            var go   = Instantiate(inventoryCardPrefab, itemListGrid);
            var card = go.GetComponent<InventoryCard>();
            card?.Setup(item, OnSelectItem);
            spawnedCards.Add(go);
        }
    }

    // ══════════════════════════════════════════════════════════
    // Chọn item → hiện Detail Panel
    // ══════════════════════════════════════════════════════════
    private void OnSelectItem(InventoryItemData item)
    {
        if (detailNameText)    detailNameText.text    = item.name;
        if (detailDescText)    detailDescText.text    = item.description;
        if (ownerCountText)    ownerCountText.text    = $"Số người sở hữu: {item.ownerCount}";
        if (quantityOwnedText) quantityOwnedText.text = $"Bạn có: {item.quantity}";

        if (detailImage) detailImage.color = new Color(0.85f, 0.85f, 0.85f);

        if (!string.IsNullOrEmpty(item.imageUrl))
            StartCoroutine(LoadDetailImage(item.imageUrl));

        // Nếu panel đang ẩn → slide in; đang hiện → chỉ update nội dung, không slide lại
        if (!detailVisible)
        {
            if (slideCoroutine != null) StopCoroutine(slideCoroutine);
            slideCoroutine = StartCoroutine(SlideDetailPanel(true));
        }
    }

    private IEnumerator LoadDetailImage(string url)
    {
        using var req = UnityWebRequestTexture.GetTexture(url);
        yield return req.SendWebRequest();
        if (req.result != UnityWebRequest.Result.Success) yield break;

        var tex    = DownloadHandlerTexture.GetContent(req);
        var sprite = Sprite.Create(tex,
            new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
        if (detailImage)
        {
            detailImage.sprite         = sprite;
            detailImage.preserveAspect = true;
            detailImage.color          = Color.white;
        }
    }

    // ══════════════════════════════════════════════════════════
    // Slide animation — dùng pivot trái (0,0.5)
    // DetailPanel có anchorMin.x=0.60, anchorMax.x=1.0, pivot.x=0
    // → anchoredPosition.x=0  : đang hiện đúng vị trí
    // → anchoredPosition.x=+W : bị đẩy sang phải (ẩn)
    // ══════════════════════════════════════════════════════════
    private IEnumerator SlideDetailPanel(bool show)
    {
        if (!detailPanel) yield break;

        detailPanel.SetActive(true);
        var rt = detailPanel.GetComponent<RectTransform>();

        // Chờ 1 frame để Unity tính toán kích thước thực tế
        yield return null;

        float panelWidth = rt.rect.width;
        if (panelWidth <= 0f) panelWidth = 300f; // fallback

        float startX = show ? panelWidth : 0f;
        float endX   = show ? 0f         : panelWidth;
        float t = 0f;

        rt.anchoredPosition = new Vector2(startX, 0f);

        while (t < 1f)
        {
            t += Time.deltaTime / 0.25f;
            float x = Mathf.Lerp(startX, endX, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t)));
            rt.anchoredPosition = new Vector2(x, 0f);
            yield return null;
        }

        rt.anchoredPosition = new Vector2(endX, 0f);
        if (!show) detailPanel.SetActive(false);
        detailVisible = show;
    }
}