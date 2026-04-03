using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using TMPro;

// ═══════════════════════════════════════════════════════════════
// InventoryCard — gắn vào InventoryCardPrefab
// ═══════════════════════════════════════════════════════════════
public class InventoryCard : MonoBehaviour
{
    [Header("UI References")]
    public Image    itemImage;
    public TMP_Text itemNameText;
    public TMP_Text quantityText;
    public Button   selectButton;    // Button toàn card

    private InventoryItemData data;
    private Action<InventoryItemData> onSelect;

    // ── Màu highlight khi được chọn ─────────────────────────────
    private Image cardBg;
    private Color normalColor    = new Color(1f, 1f, 1f);
    private Color selectedColor  = new Color(0.82f, 0.93f, 1.00f);

    // ══════════════════════════════════════════════════════════
    void Awake()
    {
        cardBg = GetComponent<Image>();
    }

    // ══════════════════════════════════════════════════════════
    // Setup
    // ══════════════════════════════════════════════════════════
    public void Setup(InventoryItemData itemData, Action<InventoryItemData> selectCallback)
    {
        data     = itemData;
        onSelect = selectCallback;

        if (itemNameText)  itemNameText.text  = data.name;
        if (quantityText)  quantityText.text  = $"x{data.quantity}";

        // Gán sự kiện
        selectButton?.onClick.RemoveAllListeners();
        selectButton?.onClick.AddListener(OnClickSelect);

        // Load ảnh
        if (!string.IsNullOrEmpty(data.imageUrl))
            StartCoroutine(LoadImage(data.imageUrl));
    }

    // ── Nhấn chọn card ──────────────────────────────────────────
    private void OnClickSelect()
    {
        onSelect?.Invoke(data);
        // Highlight card này (reset các card khác qua parent)
        ResetSiblings();
        if (cardBg) cardBg.color = selectedColor;
    }

    // Reset màu tất cả card cùng level rồi highlight card này
    private void ResetSiblings()
    {
        if (!transform.parent) return;
        foreach (Transform sib in transform.parent)
        {
            var sibCard = sib.GetComponent<InventoryCard>();
            if (sibCard && sibCard.cardBg)
                sibCard.cardBg.color = normalColor;
        }
    }

    // ── Load ảnh ─────────────────────────────────────────────────
    private IEnumerator LoadImage(string url)
    {
        using var req = UnityWebRequestTexture.GetTexture(url);
        yield return req.SendWebRequest();
        if (req.result != UnityWebRequest.Result.Success) yield break;

        var tex    = DownloadHandlerTexture.GetContent(req);
        var sprite = Sprite.Create(tex,
            new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
        if (itemImage)
        {
            itemImage.sprite          = sprite;
            itemImage.preserveAspect  = true;
            itemImage.color           = Color.white;
        }
    }
}