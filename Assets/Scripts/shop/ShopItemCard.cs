using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using TMPro;

// ═══════════════════════════════════════════════════════════════
// ShopItemCard — gắn vào ShopItemCardPrefab
// Nhận dữ liệu từ ShopManager, hiển thị và xử lý Buy
// ═══════════════════════════════════════════════════════════════
public class ShopItemCard : MonoBehaviour
{
    [Header("UI References")]
    public Image    itemImage;       // Ảnh item (load từ URL)
    public TMP_Text itemNameText;
    public TMP_Text priceText;
    public Button   buyButton;
    public Image    buyButtonImage;  // Image của button (để đổi màu)
    public TMP_Text buyButtonLabel;
    public GameObject tooltipObj;    // "Không đủ xu" tooltip

    // ── Màu nút ──────────────────────────────────────────────────
    private readonly Color COLOR_CAN_BUY  = new Color(0.25f, 0.78f, 0.40f);
    private readonly Color COLOR_CANT_BUY = new Color(0.55f, 0.55f, 0.55f);

    private ShopItemData     data;
    private Action<ShopItemData> onBuy;

    // ══════════════════════════════════════════════════════════
    // Khởi tạo card với dữ liệu item
    // ══════════════════════════════════════════════════════════
    public void Setup(ShopItemData itemData, int playerCoins, Action<ShopItemData> buyCallback)
    {
        data  = itemData;
        onBuy = buyCallback;

        // Điền text
        if (itemNameText) itemNameText.text = data.name;
        if (priceText)    priceText.text    = $"{data.price} xu";

        // Trạng thái nút mua
        bool canBuy = playerCoins >= data.price;
        SetBuyState(canBuy);

        // Gán sự kiện nút
        buyButton?.onClick.RemoveAllListeners();
        buyButton?.onClick.AddListener(OnClickBuy);

        // Tooltip ẩn mặc định
        tooltipObj?.SetActive(false);

        // Load ảnh từ URL
        if (!string.IsNullOrEmpty(data.imageUrl))
            StartCoroutine(LoadImage(data.imageUrl));
    }

    // ── Đổi màu + interactable của nút mua ───────────────────────
    private void SetBuyState(bool canBuy)
    {
        if (buyButtonImage) buyButtonImage.color = canBuy ? COLOR_CAN_BUY : COLOR_CANT_BUY;
        // Luôn để button interactable để bắt sự kiện, xử lý logic bên trong OnClickBuy
        if (buyButton) buyButton.interactable = true;
        if (buyButtonLabel) buyButtonLabel.text = canBuy ? "Mua" : "Không đủ xu";
    }

    // ── Xử lý nhấn mua ──────────────────────────────────────────
    private void OnClickBuy()
    {
        // Kiểm tra lại coin tại thời điểm nhấn (coin có thể thay đổi sau nhiều lần mua)
        bool canAfford = ShopManager.Instance != null &&
                         ShopManager.Instance.PlayerCoins >= data.price;
        if (!canAfford)
        {
            // Hiện tooltip "Không đủ xu"
            if (tooltipObj != null)
            {
                StopAllCoroutines();
                StartCoroutine(ShowTooltip());
            }
            return;
        }
        onBuy?.Invoke(data);
    }

    // ── Hiện tooltip "Không đủ xu" trong 1.5 giây ───────────────
    private IEnumerator ShowTooltip()
    {
        tooltipObj?.SetActive(true);
        yield return new WaitForSeconds(1.5f);
        tooltipObj?.SetActive(false);
    }

    // ── Load ảnh item từ URL Firebase ───────────────────────────
    private IEnumerator LoadImage(string url)
    {
        using var req = UnityWebRequestTexture.GetTexture(url);
        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogWarning($"[ShopItemCard] Lỗi tải ảnh: {req.error}");
            yield break;
        }

        var tex    = DownloadHandlerTexture.GetContent(req);
        var sprite = Sprite.Create(tex,
            new Rect(0, 0, tex.width, tex.height),
            new Vector2(0.5f, 0.5f));

        if (itemImage)
        {
            itemImage.sprite          = sprite;
            itemImage.preserveAspect  = true;
            itemImage.color           = Color.white;
        }
    }
}