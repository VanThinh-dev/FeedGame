using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Firebase;
using Firebase.Database;
using Firebase.Auth;

[Serializable]
public class ShopItemData
{
    public string id;
    public string name;
    public int    price;
    public string imageUrl;
    public string description;
    public int    ownerCount;
    public int    stock;
}

// =============================================================================
// ShopManager.cs — v3
//
// FIX: Sau khi SaveMedals() và SavePurchase() lưu Firebase xong,
//      gọi AuthManager.Instance.RefreshUserData() để load lại CurrentUserData
//      từ Firebase → MedalManager, BedroomManager, AvatarWidget sẽ thấy
//      số đúng mà không cần thoát/đăng nhập lại.
//
// Lý do không sync tay (ud.bronzeMedals = x):
//   AuthManager.LoadUserData() đọc medals từ nested node "medals/bronze"
//   bằng logic riêng — nếu ta chỉ gán thẳng vào CurrentUserData mà không
//   reload thì các component khác subscribe OnUserDataReady sẽ không biết.
// =============================================================================

public class ShopManager : MonoBehaviour
{
    public static ShopManager Instance { get; private set; }

    [Header("Canvas & Panels")]
    public GameObject shopCanvas;
    public GameObject shopPanel;
    public GameObject exchangePanel;

    [Header("Top Bar Tabs")]
    public Button tabShopBtn;
    public Button tabExchangeBtn;

    [Header("Coin Display")]
    public TMP_Text coinText;

    [Header("Shop - Grid & Pagination")]
    public Transform  itemGrid;
    public GameObject shopItemCardPrefab;
    public Button     prevPageBtn;
    public Button     nextPageBtn;
    public TMP_Text   pageIndicatorText;

    [Header("Exchange Panel")]
    public Button   bronze2SilverBtn;
    public Button   silver2GoldBtn;
    public TMP_Text bronzeCountText;
    public TMP_Text silverCountText;
    public TMP_Text goldCountText;

    [Header("Exchange Popup (Bronze→Silver)")]
    public GameObject exchangePopupBS;
    public TMP_Text   popupBS_BronzeNeed;
    public TMP_Text   popupBS_SilverGet;
    public TMP_Text   popupBS_BronzeOwned;
    public Button     popupBS_ConfirmBtn;
    public Button     popupBS_CloseBtn;

    [Header("Exchange Popup (Silver→Gold)")]
    public GameObject exchangePopupSG;
    public TMP_Text   popupSG_SilverNeed;
    public TMP_Text   popupSG_GoldGet;
    public TMP_Text   popupSG_SilverOwned;
    public Button     popupSG_ConfirmBtn;
    public Button     popupSG_CloseBtn;

    [Header("Close")]
    public Button closeBtn;

    [Header("Thông báo")]
    public TMP_Text buySuccessText;

    // ── Internal ──────────────────────────────────────────────────────────────
    private List<ShopItemData> allItems     = new();
    private List<GameObject>   spawnedCards = new();

    private const int ITEMS_PER_PAGE  = 6;
    private int       currentPage     = 0;

    private int playerCoins;
    private int bronzeMedal;
    private int silverMedal;
    private int goldMedal;

    public int PlayerCoins => playerCoins;

    private const int BRONZE_PER_SILVER = 10;
    private const int SILVER_PER_GOLD   = 20;

    // =========================================================================
    // LIFECYCLE
    // =========================================================================

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        closeBtn?.onClick.AddListener(CloseShop);
        tabShopBtn?.onClick.AddListener(() => SwitchTab(true));
        tabExchangeBtn?.onClick.AddListener(() => SwitchTab(false));
        prevPageBtn?.onClick.AddListener(PrevPage);
        nextPageBtn?.onClick.AddListener(NextPage);

        bronze2SilverBtn?.onClick.AddListener(() => OpenExchangePopup(true));
        silver2GoldBtn?.onClick.AddListener(() => OpenExchangePopup(false));

        popupBS_ConfirmBtn?.onClick.AddListener(() => DoExchange(true));
        popupBS_CloseBtn?.onClick.AddListener(() => exchangePopupBS?.SetActive(false));
        popupSG_ConfirmBtn?.onClick.AddListener(() => DoExchange(false));
        popupSG_CloseBtn?.onClick.AddListener(() => exchangePopupSG?.SetActive(false));

        shopCanvas?.SetActive(false);
        buySuccessText?.gameObject.SetActive(false);
    }

    // =========================================================================
    // MỞ / ĐÓNG
    // =========================================================================

    public void OpenShop()
    {
        shopCanvas?.SetActive(true);
        SwitchTab(true);
        StopAllCoroutines();
        StartCoroutine(LoadAll());
    }

    public void CloseShop()
    {
        shopCanvas?.SetActive(false);
    }

    // =========================================================================
    // LOAD DATA
    // =========================================================================

    private IEnumerator LoadAll()
    {
        // Đọc từ CurrentUserData (in-memory, không cần Firebase round-trip)
        LoadPlayerDataFromCache();
        yield return StartCoroutine(LoadShopItems());
        RefreshUI();
    }

    private void LoadPlayerDataFromCache()
    {
        var ud = AuthManager.Instance?.CurrentUserData;
        if (ud == null)
        {
            playerCoins = bronzeMedal = silverMedal = goldMedal = 0;
            return;
        }
        playerCoins = ud.coins;
        bronzeMedal = ud.bronzeMedals;
        silverMedal = ud.silverMedals;
        goldMedal   = ud.goldMedals;
    }

    private IEnumerator LoadShopItems()
    {
        var task = FirebaseDatabase.DefaultInstance
            .GetReference("shop_items").GetValueAsync();
        yield return new WaitUntil(() => task.IsCompleted);

        if (task.Exception != null) { Debug.LogError(task.Exception); yield break; }

        allItems.Clear();
        foreach (DataSnapshot child in task.Result.Children)
        {
            allItems.Add(new ShopItemData
            {
                id          = child.Key,
                name        = child.Child("name").Value?.ToString()        ?? "",
                price       = int.Parse(child.Child("price").Value?.ToString()      ?? "0"),
                imageUrl    = child.Child("imageUrl").Value?.ToString()    ?? "",
                description = child.Child("description").Value?.ToString() ?? "",
                ownerCount  = int.Parse(child.Child("ownerCount").Value?.ToString() ?? "0"),
                stock       = int.Parse(child.Child("stock").Value?.ToString()      ?? "0"),
            });
        }
    }

    // =========================================================================
    // UI
    // =========================================================================

    private void RefreshUI()
    {
        if (coinText)        coinText.text        = $"{playerCoins}";
        if (bronzeCountText) bronzeCountText.text = bronzeMedal.ToString();
        if (silverCountText) silverCountText.text = silverMedal.ToString();
        if (goldCountText)   goldCountText.text   = goldMedal.ToString();

        if (shopPanel != null && shopPanel.activeInHierarchy)
            BuildPage();
    }

    private void BuildPage()
    {
        foreach (var c in spawnedCards) Destroy(c);
        spawnedCards.Clear();

        if (itemGrid == null || !itemGrid.gameObject.activeInHierarchy) return;

        int totalPages = Mathf.Max(1, Mathf.CeilToInt((float)allItems.Count / ITEMS_PER_PAGE));
        currentPage    = Mathf.Clamp(currentPage, 0, totalPages - 1);

        if (pageIndicatorText) pageIndicatorText.text = $"{currentPage + 1}/{totalPages}";
        if (prevPageBtn) prevPageBtn.interactable = currentPage > 0;
        if (nextPageBtn) nextPageBtn.interactable = currentPage < totalPages - 1;

        int start = currentPage * ITEMS_PER_PAGE;
        int end   = Mathf.Min(start + ITEMS_PER_PAGE, allItems.Count);

        for (int i = start; i < end; i++)
        {
            var go   = Instantiate(shopItemCardPrefab, itemGrid);
            var card = go.GetComponent<ShopItemCard>();
            card?.Setup(allItems[i], playerCoins, OnBuyItem);
            spawnedCards.Add(go);
        }
    }

    private void PrevPage() { currentPage--; BuildPage(); }
    private void NextPage() { currentPage++; BuildPage(); }

    private void SwitchTab(bool isShop)
    {
        shopPanel?.SetActive(isShop);
        exchangePanel?.SetActive(!isShop);

        Color active   = new Color(0.25f, 0.65f, 1f);
        Color inactive = new Color(0.55f, 0.55f, 0.55f);
        if (tabShopBtn)     tabShopBtn.GetComponent<Image>().color     = isShop ? active   : inactive;
        if (tabExchangeBtn) tabExchangeBtn.GetComponent<Image>().color = isShop ? inactive : active;
    }

    // =========================================================================
    // MUA ITEM
    // =========================================================================

    public void OnBuyItem(ShopItemData item)
    {
        if (playerCoins < item.price)
        {
            StartCoroutine(ShowSuccessMessage("Không đủ xu!"));
            return;
        }

        string uid = FirebaseAuth.DefaultInstance?.CurrentUser?.UserId;
        if (string.IsNullOrEmpty(uid)) return;

        playerCoins -= item.price;
        StartCoroutine(SavePurchase(uid, item));
    }

    private IEnumerator SavePurchase(string uid, ShopItemData item)
    {
        var db    = FirebaseDatabase.DefaultInstance;
        var tasks = new List<System.Threading.Tasks.Task>();

        tasks.Add(db.GetReference($"users/{uid}/coins").SetValueAsync(playerCoins));

        var invRef  = db.GetReference($"inventory/{uid}/{item.id}");
        var getTask = invRef.GetValueAsync();
        yield return new WaitUntil(() => getTask.IsCompleted);

        int qty = 0;
        if (getTask.Result.Exists)
            qty = int.Parse(getTask.Result.Child("quantity").Value?.ToString() ?? "0");

        var invData = new Dictionary<string, object>
        {
            ["quantity"]    = qty + 1,
            ["purchasedAt"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        };
        tasks.Add(invRef.UpdateChildrenAsync(invData));

        if (qty == 0)
            tasks.Add(db.GetReference($"shop_items/{item.id}/ownerCount")
                .SetValueAsync(item.ownerCount + 1));

        foreach (var t in tasks)
            yield return new WaitUntil(() => t.IsCompleted);

        // ── Cập nhật CurrentUserData từ Firebase (load lại toàn bộ) ──────────
        AuthManager.Instance?.RefreshUserData();

        RefreshUI();
        BuildPage();
        StartCoroutine(ShowSuccessMessage($" Mua thành công: {item.name}"));
    }

    // =========================================================================
    // ĐỔI HUY CHƯƠNG
    // =========================================================================

    private void OpenExchangePopup(bool isBronzeToSilver)
    {
        if (isBronzeToSilver)
        {
            if (exchangePopupSG != null) exchangePopupSG.SetActive(false);
            if (popupBS_BronzeNeed)  popupBS_BronzeNeed.text  = $"{BRONZE_PER_SILVER} Đồng";
            if (popupBS_SilverGet)   popupBS_SilverGet.text   = "→  1 Bạc";
            if (popupBS_BronzeOwned) popupBS_BronzeOwned.text = $"Bạn có: {bronzeMedal} Đồng";
            if (exchangePopupBS != null) exchangePopupBS.SetActive(true);
        }
        else
        {
            if (exchangePopupBS != null) exchangePopupBS.SetActive(false);
            if (popupSG_SilverNeed)  popupSG_SilverNeed.text  = $"{SILVER_PER_GOLD} Bạc";
            if (popupSG_GoldGet)     popupSG_GoldGet.text     = "→  1 Vàng";
            if (popupSG_SilverOwned) popupSG_SilverOwned.text = $"Bạn có: {silverMedal} Bạc";
            if (exchangePopupSG != null) exchangePopupSG.SetActive(true);
        }
    }

    private void DoExchange(bool isBronzeToSilver)
    {
        string uid = FirebaseAuth.DefaultInstance?.CurrentUser?.UserId;
        if (string.IsNullOrEmpty(uid)) return;

        if (isBronzeToSilver)
        {
            if (bronzeMedal < BRONZE_PER_SILVER)
            {
                StartCoroutine(ShowSuccessMessage("Không đủ huy chương đồng!"));
                return;
            }
            bronzeMedal -= BRONZE_PER_SILVER;
            silverMedal += 1;
            exchangePopupBS?.SetActive(false);
        }
        else
        {
            if (silverMedal < SILVER_PER_GOLD)
            {
                StartCoroutine(ShowSuccessMessage("Không đủ huy chương bạc!"));
                return;
            }
            silverMedal -= SILVER_PER_GOLD;
            goldMedal   += 1;
            exchangePopupSG?.SetActive(false);
        }

        StartCoroutine(SaveMedals(uid));
    }

    private IEnumerator SaveMedals(string uid)
    {
        var updates = new Dictionary<string, object>
        {
            ["medals/bronze"] = bronzeMedal,
            ["medals/silver"] = silverMedal,
            ["medals/gold"]   = goldMedal,
        };

        var task = FirebaseDatabase.DefaultInstance
            .GetReference($"users/{uid}").UpdateChildrenAsync(updates);
        yield return new WaitUntil(() => task.IsCompleted);

        if (task.Exception != null)
        {
            Debug.LogError($"[ShopManager] Luu medals loi: {task.Exception}");
            yield break;
        }

        // ── KEY FIX: Load lại CurrentUserData từ Firebase sau khi lưu xong ──
        // AuthManager.LoadUserData() đọc nested medals/bronze đúng cách
        // và fire OnUserDataReady → BedroomManager, AvatarWidget tự cập nhật
        AuthManager.Instance?.RefreshUserData();

        RefreshUI();
        StartCoroutine(ShowSuccessMessage(" Đổi huy chương thành công!"));
    }

    // =========================================================================
    // THÔNG BÁO
    // =========================================================================

    private IEnumerator ShowSuccessMessage(string message)
    {
        if (buySuccessText == null) yield break;
        buySuccessText.text = message;
        buySuccessText.gameObject.SetActive(true);
        yield return new WaitForSeconds(2f);
        buySuccessText.gameObject.SetActive(false);
    }
}