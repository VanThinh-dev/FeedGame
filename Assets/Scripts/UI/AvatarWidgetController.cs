using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// =============================================================================
// AvatarWidgetController.cs
//
// FIX Bug 1: Button listener bị ADD 2 LẦN
//   → BindButton() gọi RemoveAllListeners() trước AddListener
//   → Xóa hết On Click() trong Inspector của tất cả button
//
// FIX Bug 2: Race condition với Firebase async
//   → Subscribe AuthManager.OnUserDataReady (fire SAU KHI Firebase load xong)
//
// FIX Bug 3: OpenShop/OpenInventory gọi khi canvas inactive
//   → ShopManager.OpenShop() và InventoryManager.OpenInventory()
//     tự SetActive(true) canvas TRƯỚC khi StartCoroutine
// =============================================================================

public class AvatarWidgetController : MonoBehaviour
{
    [Header("XP Ring & Avatar")]
    [SerializeField] private Image    xpRingImage;
    [SerializeField] private Image    avatarCircleImage;
    [SerializeField] public  Sprite   avatarSprite;
    [SerializeField] private Button   avatarCircleButton;
    [SerializeField] private TMP_Text levelBadgeText;
    [SerializeField] private TMP_Text xpProgressText;
    [SerializeField] private TMP_Text displayNameText;

    [Header("XP Ring Animation")]
    [SerializeField] private float xpAnimDuration  = 0.6f;
    [SerializeField] private Color xpRingColor     = new Color(0.20f, 0.85f, 0.40f, 1f);
    [SerializeField] private Color xpRingFullColor = new Color(1.00f, 0.85f, 0.10f, 1f);

    [Header("Dropdown Panel")]
    [SerializeField] private GameObject dropdownPanel;
    [SerializeField] private float      slideDistance = 80f;
    [SerializeField] private float      animDuration  = 0.25f;

    [Header("Room Buttons (6 phong)")]
    [SerializeField] private Button bedroomBtn;
    [SerializeField] private Button livingRoomBtn;
    [SerializeField] private Button kitchenBtn;
    [SerializeField] private Button bathroomBtn;
    [SerializeField] private Button playRoomBtn;
    [SerializeField] private Button gardenBtn;

    [Header("Room Lock Overlays (6 phong)")]
    [SerializeField] private GameObject bedroomLockOverlay;
    [SerializeField] private GameObject livingRoomLockOverlay;
    [SerializeField] private GameObject kitchenLockOverlay;
    [SerializeField] private GameObject bathroomLockOverlay;
    [SerializeField] private GameObject playRoomLockOverlay;
    [SerializeField] private GameObject gardenLockOverlay;

    [Header("Room Lock Level Texts")]
    [SerializeField] private TMP_Text livingRoomLockText;
    [SerializeField] private TMP_Text kitchenLockText;
    [SerializeField] private TMP_Text bathroomLockText;
    [SerializeField] private TMP_Text playRoomLockText;
    [SerializeField] private TMP_Text gardenLockText;

    [Header("Room Button Icons")]
    [SerializeField] public Sprite bedroomIcon;
    [SerializeField] public Sprite livingRoomIcon;
    [SerializeField] public Sprite kitchenIcon;
    [SerializeField] public Sprite bathroomIcon;
    [SerializeField] public Sprite playRoomIcon;
    [SerializeField] public Sprite gardenIcon;

    [Header("Action Buttons")]
    [SerializeField] private Button shopBtn;
    [SerializeField] private Button inventoryBtn;
    [SerializeField] private Button logoutBtn;

    private const int LEVEL_BEDROOM     = 1;
    private const int LEVEL_LIVING_ROOM = 5;
    private const int LEVEL_KITCHEN     = 10;
    private const int LEVEL_BATHROOM    = 15;
    private const int LEVEL_PLAY_ROOM   = 20;
    private const int LEVEL_GARDEN      = 25;

    private bool          dropdownOpen    = false;
    private RectTransform dropdownRect;
    private Vector2       dropdownClosedPos;
    private Vector2       dropdownOpenPos;
    private float         currentFill     = 0f;
    private Coroutine     xpAnimCoroutine;

    // =========================================================================
    // LIFECYCLE
    // =========================================================================
    private void Awake()
    {
        if (dropdownPanel != null)
        {
            dropdownRect      = dropdownPanel.GetComponent<RectTransform>();
            dropdownClosedPos = dropdownRect.anchoredPosition + Vector2.up * slideDistance;
            dropdownOpenPos   = dropdownRect.anchoredPosition;
        }
    }

    private void Start()
    {
        if (dropdownPanel != null) dropdownPanel.SetActive(false);
        if (xpRingImage   != null) xpRingImage.color = xpRingColor;
        if (avatarCircleImage != null && avatarSprite != null)
            avatarCircleImage.sprite = avatarSprite;

        SetLockText(livingRoomLockText, LEVEL_LIVING_ROOM);
        SetLockText(kitchenLockText,    LEVEL_KITCHEN);
        SetLockText(bathroomLockText,   LEVEL_BATHROOM);
        SetLockText(playRoomLockText,   LEVEL_PLAY_ROOM);
        SetLockText(gardenLockText,     LEVEL_GARDEN);

        // Xóa hết On Click() trong Inspector của tất cả button
        // BindButton đã lo việc bind, không cần gán thêm trong Inspector

        // [SỬA] bedroom dùng GoToBedroom thay vì SwitchRoom — về giao diện chính
        BindButton(bedroomBtn,    GoToBedroom);
        BindButton(livingRoomBtn, () => SwitchRoom(RoomType.LivingRoom));
        BindButton(kitchenBtn,    () => SwitchRoom(RoomType.Kitchen));
        BindButton(bathroomBtn,   () => SwitchRoom(RoomType.Bathroom));
        BindButton(playRoomBtn,   () => SwitchRoom(RoomType.PlayRoom));
        BindButton(gardenBtn,     () => SwitchRoom(RoomType.Garden));

        BindButton(avatarCircleButton, ToggleDropdown);
        BindButton(shopBtn,            OpenShop);
        BindButton(inventoryBtn,       OpenInventory);
        BindButton(logoutBtn,          OnLogoutClicked);

        BindAuthEvents();
    }

    // Xóa listener cũ rồi gán mới — tránh double-fire khi Inspector cũng có On Click()
    private void BindButton(Button btn, UnityEngine.Events.UnityAction action)
    {
        if (btn == null) return;
        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(action);
    }

    private void BindAuthEvents()
    {
        if (AuthManager.Instance == null) return;

        AuthManager.Instance.OnUserDataReady += HandleUserDataReady;
        AuthManager.Instance.OnLogout        += HandleLogout;

        // Nếu đã có data rồi (scene reload, v.v.) thì lấy luôn
        var existing = AuthManager.Instance.CurrentUserData;
        if (existing != null && !string.IsNullOrEmpty(existing.uid))
            HandleUserDataReady(existing);
    }

    private void OnDestroy()
    {
        if (AuthManager.Instance == null) return;
        AuthManager.Instance.OnUserDataReady -= HandleUserDataReady;
        AuthManager.Instance.OnLogout        -= HandleLogout;
    }

    // =========================================================================
    // EVENT HANDLERS
    // =========================================================================
    private void HandleUserDataReady(UserData userData)
    {
        if (userData == null) return;
        currentFill = userData.XpFillAmount;
        SetXpRingDirect(currentFill);
        UpdateLevelBadge(userData.level);
        UpdateXpText(userData);
        UpdateRoomLocks(userData.level);
        if (displayNameText != null) displayNameText.text = userData.displayName;
    }

    private void HandleLogout()
    {
        if (dropdownPanel != null) dropdownPanel.SetActive(false);
        dropdownOpen = false;
    }

    // =========================================================================
    // XP RING
    // =========================================================================
    private void SetXpRingDirect(float fill)
    {
        if (xpRingImage == null) return;
        xpRingImage.fillAmount = fill;
        xpRingImage.color      = fill >= 1f ? xpRingFullColor : xpRingColor;
    }

    private void AnimateXpRing(float targetFill)
    {
        if (xpAnimCoroutine != null) StopCoroutine(xpAnimCoroutine);
        xpAnimCoroutine = StartCoroutine(XpRingRoutine(currentFill, targetFill));
    }

    private IEnumerator XpRingRoutine(float from, float to)
    {
        float elapsed = 0f;
        while (elapsed < xpAnimDuration)
        {
            elapsed += Time.deltaTime;
            float t    = Mathf.SmoothStep(0f, 1f, elapsed / xpAnimDuration);
            float fill = Mathf.Lerp(from, to, t);
            if (xpRingImage != null)
            {
                xpRingImage.fillAmount = fill;
                xpRingImage.color      = fill >= 0.98f ? xpRingFullColor : xpRingColor;
            }
            yield return null;
        }
        if (xpRingImage != null)
        {
            xpRingImage.fillAmount = to;
            xpRingImage.color      = to >= 1f ? xpRingFullColor : xpRingColor;
        }
        currentFill     = to;
        xpAnimCoroutine = null;
    }

    // =========================================================================
    // TEXT UI
    // =========================================================================
    private void UpdateLevelBadge(int level)
    {
        if (levelBadgeText != null) levelBadgeText.text = $"Lv.{level}";
    }

    private void UpdateXpText(UserData userData)
    {
        if (xpProgressText != null)
            xpProgressText.text = $"{userData.XpInCurrentLevel}/{UserData.XP_PER_LEVEL} XP";
    }

    private void SetLockText(TMP_Text txt, int level)
    {
        if (txt != null) txt.text = $"Lv.{level}";
    }

    // =========================================================================
    // ROOM LOCKS
    // =========================================================================
    private void UpdateRoomLocks(int userLevel)
    {
        // Bỏ comment để bật lock theo level
        // SetRoomLock(bedroomLockOverlay,    bedroomBtn,    userLevel, LEVEL_BEDROOM);
        SetRoomLock(livingRoomLockOverlay, livingRoomBtn, userLevel, LEVEL_LIVING_ROOM);
        SetRoomLock(kitchenLockOverlay,    kitchenBtn,    userLevel, LEVEL_KITCHEN);
        SetRoomLock(bathroomLockOverlay,   bathroomBtn,   userLevel, LEVEL_BATHROOM);
        SetRoomLock(playRoomLockOverlay,   playRoomBtn,   userLevel, LEVEL_PLAY_ROOM);
        SetRoomLock(gardenLockOverlay,     gardenBtn,     userLevel, LEVEL_GARDEN);
        // HideAllLockOverlays();
    }

    private void HideAllLockOverlays()
    {
        foreach (var o in new[] {
            bedroomLockOverlay, livingRoomLockOverlay, kitchenLockOverlay,
            bathroomLockOverlay, playRoomLockOverlay,  gardenLockOverlay })
            if (o != null) o.SetActive(false);

        foreach (var b in new[] {
            bedroomBtn, livingRoomBtn, kitchenBtn,
            bathroomBtn, playRoomBtn,  gardenBtn })
            if (b != null) b.interactable = true;
    }

    private void SetRoomLock(GameObject overlay, Button btn, int userLevel, int required)
    {
        bool locked = userLevel < required;
        if (overlay != null) overlay.SetActive(locked);
        if (btn     != null) btn.interactable = !locked;
    }

    // =========================================================================
    // DROPDOWN
    // =========================================================================
    private void ToggleDropdown()
    {
        dropdownOpen = !dropdownOpen;
        if (dropdownOpen)
        {
            dropdownPanel.SetActive(true);
            StopDropdownCoroutines();
            StartCoroutine(AnimateDropdown(dropdownClosedPos, dropdownOpenPos));
        }
        else
        {
            StopDropdownCoroutines();
            StartCoroutine(CloseDropdownAfterAnim());
        }
    }

    private IEnumerator AnimateDropdown(Vector2 from, Vector2 to)
    {
        float elapsed = 0f;
        while (elapsed < animDuration)
        {
            elapsed += Time.deltaTime;
            dropdownRect.anchoredPosition = Vector2.Lerp(from, to,
                Mathf.SmoothStep(0f, 1f, elapsed / animDuration));
            yield return null;
        }
        dropdownRect.anchoredPosition = to;
    }

    private IEnumerator CloseDropdownAfterAnim()
    {
        yield return AnimateDropdown(dropdownOpenPos, dropdownClosedPos);
        dropdownPanel.SetActive(false);
    }

    private void StopDropdownCoroutines()
    {
        StopCoroutine("AnimateDropdown");
        StopCoroutine("CloseDropdownAfterAnim");
    }

    // =========================================================================
    // ROOM SWITCH
    // =========================================================================

    // [THÊM] về bedroom — đóng dropdown rồi gọi GoToBedroom trong RoomMapManager
    private void GoToBedroom()
    {
        CloseDropdownImmediate();
        RoomMapManager.Instance?.GoToBedroom();
    }

    private void SwitchRoom(RoomType room)
    {
        CloseDropdownImmediate();
        if (RoomMapManager.Instance != null)
            RoomMapManager.Instance.SwitchRoom(room);
        else
            Debug.LogWarning("[AvatarWidget] RoomMapManager.Instance null!");
    }

    // =========================================================================
    // ACTION BUTTONS
    // =========================================================================
    private void OpenShop()
    {
        CloseDropdownImmediate();

        if (ShopManager.Instance != null)
        {
            ShopManager.Instance.OpenShop();
            return;
        }

        var mgr = FindObjectOfType<ShopManager>(includeInactive: true);
        if (mgr != null)
        {
            StartCoroutine(DelayedOpenShop(mgr));
            return;
        }

        Debug.LogWarning("[AvatarWidget] Không tìm thấy ShopManager!");
    }

    private IEnumerator DelayedOpenShop(ShopManager mgr)
    {
        mgr.gameObject.SetActive(true);
        yield return null;
        var target = ShopManager.Instance ?? mgr;
        target.OpenShop();
    }

    private void OpenInventory()
    {
        CloseDropdownImmediate();

        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.OpenInventory();
            return;
        }

        var mgr = FindObjectOfType<InventoryManager>(includeInactive: true);
        if (mgr != null)
        {
            StartCoroutine(DelayedOpenInventory(mgr));
            return;
        }

        Debug.LogWarning("[AvatarWidget] Không tìm thấy InventoryManager!");
    }

    private IEnumerator DelayedOpenInventory(InventoryManager mgr)
    {
        mgr.gameObject.SetActive(true);
        yield return null;
        var target = InventoryManager.Instance ?? mgr;
        target.OpenInventory();
    }

    private void OnLogoutClicked()
    {
        CloseDropdownImmediate();
        AuthManager.Instance?.Logout();
    }

    private void CloseDropdownImmediate()
    {
        dropdownOpen = false;
        if (dropdownPanel != null) dropdownPanel.SetActive(false);
    }

    // =========================================================================
    // PUBLIC API
    // =========================================================================
    public void RefreshWidget(UserData userData)
    {
        float newFill = userData.XpFillAmount;
        UpdateLevelBadge(userData.level);
        UpdateXpText(userData);
        UpdateRoomLocks(userData.level);
        if (displayNameText != null) displayNameText.text = userData.displayName;
        AnimateXpRing(newFill);
        currentFill = newFill;
    }
}