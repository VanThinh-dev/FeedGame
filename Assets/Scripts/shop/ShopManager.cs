using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
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
    public Button bronze2SilverBtn;
    public Button silver2GoldBtn;
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
    public TMP_Text buySuccessText;      // dùng chung cho cả mua lẫn đổi huy chương

    private List<ShopItemData> allItems     = new();
    private List<GameObject>   spawnedCards = new();
    private const int ITEMS_PER_PAGE        = 6;
    private int currentPage                 = 0;

    private int playerCoins;
    private int bronzeMedal, silverMedal, goldMedal;

    public int PlayerCoins => playerCoins;

    private const int BRONZE_PER_SILVER = 10;
    private const int SILVER_PER_GOLD   = 20;

    // ══════════════════════════════════════════════════════════
    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
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

        // Đảm bảo text thông báo ẩn lúc đầu
        buySuccessText?.gameObject.SetActive(false);
    }

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

    // ══════════════════════════════════════════════════════════
    private IEnumerator LoadAll()
    {
        yield return StartCoroutine(LoadPlayerData());
        yield return StartCoroutine(LoadShopItems());
        RefreshUI();
    }

    private IEnumerator LoadPlayerData()
    {
        string uid = FirebaseAuth.DefaultInstance?.CurrentUser?.UserId;
        if (string.IsNullOrEmpty(uid)) yield break;

        var task = FirebaseDatabase.DefaultInstance
            .GetReference($"users/{uid}").GetValueAsync();
        yield return new WaitUntil(() => task.IsCompleted);

        if (task.Exception != null) { Debug.LogError(task.Exception); yield break; }
        var snap = task.Result;
        playerCoins = int.Parse(snap.Child("coins").Value?.ToString() ?? "0");
        bronzeMedal = int.Parse(snap.Child("medals").Child("bronze").Value?.ToString() ?? "0");
        silverMedal = int.Parse(snap.Child("medals").Child("silver").Value?.ToString() ?? "0");
        goldMedal   = int.Parse(snap.Child("medals").Child("gold").Value?.ToString()   ?? "0");
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
            var item = new ShopItemData
            {
                id          = child.Key,
                name        = child.Child("name").Value?.ToString() ?? "",
                price       = int.Parse(child.Child("price").Value?.ToString() ?? "0"),
                imageUrl    = child.Child("imageUrl").Value?.ToString() ?? "",
                description = child.Child("description").Value?.ToString() ?? "",
                ownerCount  = int.Parse(child.Child("ownerCount").Value?.ToString() ?? "0"),
                stock       = int.Parse(child.Child("stock").Value?.ToString() ?? "0"),
            };
            allItems.Add(item);
        }
    }

    // ══════════════════════════════════════════════════════════
    private void RefreshUI()
    {
        if (coinText) coinText.text = $"{playerCoins}";
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

        if (itemGrid == null || !itemGrid.gameObject.activeInHierarchy)
            return;

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
        if (tabShopBtn)     tabShopBtn.GetComponent<Image>().color     = isShop ? active : inactive;
        if (tabExchangeBtn) tabExchangeBtn.GetComponent<Image>().color = isShop ? inactive : active;
    }

    // ══════════════════════════════════════════════════════════
    // Mua item
    // ══════════════════════════════════════════════════════════
    public void OnBuyItem(ShopItemData item)
    {
        if (playerCoins < item.price)
        {
            Debug.Log("[Shop] Không đủ xu!");
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

        RefreshUI();
        BuildPage();
        StartCoroutine(ShowSuccessMessage($"✓ Mua thành công: {item.name}"));  // ← thêm
    }

    // ══════════════════════════════════════════════════════════
    // Đổi huy chương
    // ══════════════════════════════════════════════════════════
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
            { Debug.Log("[Shop] Không đủ huy chương đồng!"); return; }
            bronzeMedal -= BRONZE_PER_SILVER;
            silverMedal += 1;
            exchangePopupBS?.SetActive(false);
        }
        else
        {
            if (silverMedal < SILVER_PER_GOLD)
            { Debug.Log("[Shop] Không đủ huy chương bạc!"); return; }
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

        RefreshUI();
        StartCoroutine(ShowSuccessMessage(" Đổi huy chương thành công!"));  // ← thêm
    }

    // ══════════════════════════════════════════════════════════
    // Coroutine hiển thị thông báo — dùng chung
    // ══════════════════════════════════════════════════════════
    private IEnumerator ShowSuccessMessage(string message)
    {
        if (buySuccessText == null) yield break;
        buySuccessText.text = message;
        buySuccessText.gameObject.SetActive(true);
        yield return new WaitForSeconds(2f);
        buySuccessText.gameObject.SetActive(false);
    }
}