using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// =============================================================================
// RoomType enum — 6 phòng
// =============================================================================
public enum RoomType
{
    Bedroom    = 0,
    LivingRoom = 1,
    Kitchen    = 2,
    Bathroom   = 3,
    PlayRoom   = 4,
    Garden     = 5,
}

// =============================================================================
// RoomConfig — cấu hình từng phòng
// =============================================================================
[Serializable]
public class RoomConfig
{
    public RoomType roomType;
    [SerializeField] public Sprite backgroundSprite;
    [SerializeField] public string roomNameVN;        // "Phòng Ngủ", "Phòng Khách"...
    public int requiredLevel;                          // Level cần để mở khóa

    [SerializeField] public GameObject[] roomHitAreas;

    // ── Ambient Sound ─────────────────────────────────────────────────────────
    [Header("Ambient Sound (tuỳ chọn)")]
    [Tooltip("Tiếng môi trường riêng của phòng này. Để trống = im lặng.")]
    public AudioClip ambientClip;

    [Range(0f, 1f)]
    public float ambientVolume = 0.4f;
}

// =============================================================================
// RoomMapManager.cs — v3  (6 phòng + level lock + per-room ambient sound)
//
// ► Mỗi RoomConfig có thêm ambientClip + ambientVolume
// ► Khi chuyển phòng:
//     1. Fade out ambient phòng cũ (nếu có)
//     2. Tắt nhạc bedroom (BedroomManager.StopBedroomMusic)
//     3. Fade in ambient phòng mới (nếu có)
// ► Khi về Bedroom (GoToBedroom / SwitchRoom(Bedroom)):
//     1. Fade out ambient phòng hiện tại
//     2. Gọi BedroomManager.PlayBedroomMusic()
// ► TEST MODE: Tìm dòng "// [LEVEL-LOCK]" để bật/tắt kiểm tra level
// =============================================================================
public class RoomMapManager : MonoBehaviour
{
    public static RoomMapManager Instance { get; private set; }

    // ── UI References ─────────────────────────────────────────────────────────
    [Header("Background Image (BedroomCanvas > BackgroundImage)")]
    [SerializeField] private Image backgroundImage;

    [Header("Room Name Text")]
    [SerializeField] private TMP_Text roomNameText;

    [Header("Transition Overlay (Image đen để fade)")]
    [SerializeField] private Image transitionOverlay;

    // ── Room Configs ──────────────────────────────────────────────────────────
    [Header("Room Configs (6 phòng theo thứ tự enum)")]
    [SerializeField] private RoomConfig[] rooms;

    // ── Transition ────────────────────────────────────────────────────────────
    [Header("Transition Settings")]
    [SerializeField] private float fadeDuration = 0.25f;

    // ── Ambient Sound Settings ────────────────────────────────────────────────
    [Header("Ambient Sound Settings")]
    [Tooltip("Thời gian fade in/out ambient (giây)")]
    [SerializeField] private float ambientFadeDuration = 0.8f;

    // ── Internal ──────────────────────────────────────────────────────────────
    private RoomType    _currentRoom      = RoomType.Bedroom;
    private bool        _isTransiting     = false;
    private Sprite      _bedroomSprite;

    private AudioSource _ambientSource;       // AudioSource dùng cho ambient các phòng
    private Coroutine   _ambientFadeCoroutine;

    /// <summary>Gọi khi chuyển phòng xong. Param = RoomType mới.</summary>
    public event Action<RoomType> OnRoomChanged;

    // =========================================================================
    // LIFECYCLE
    // =========================================================================
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        // Tạo AudioSource riêng để phát ambient
        _ambientSource             = gameObject.AddComponent<AudioSource>();
        _ambientSource.loop        = true;
        _ambientSource.playOnAwake = false;
        _ambientSource.volume      = 0f;
    }

    private void Start()
    {
        if (backgroundImage != null)
            _bedroomSprite = backgroundImage.sprite;

        if (transitionOverlay != null)
        {
            transitionOverlay.color         = new Color(0f, 0f, 0f, 0f);
            transitionOverlay.raycastTarget = false;
        }
        ApplyRoomImmediate(RoomType.Bedroom);

        // Bedroom music bật qua BedroomManager (nếu đã có)
        // RoomMapManager không tự phát nhạc bedroom
    }

    // =========================================================================
    // PUBLIC API
    // =========================================================================
    public void SwitchRoom(RoomType targetRoom)
    {
        if (_isTransiting)              return;
        if (targetRoom == _currentRoom) return;

        var config = GetConfig(targetRoom);
        if (config == null)
        {
            Debug.LogWarning($"[RoomMapManager] Không tìm thấy config cho {targetRoom}");
            return;
        }

        // ── [LEVEL-LOCK] ── Bỏ comment block dưới để bật kiểm tra level ──────
        UserData user = AuthManager.Instance?.CurrentUserData;
        if (user != null && user.level < config.requiredLevel)
        {
            Debug.Log($"[RoomMapManager] Cần Level {config.requiredLevel} để vào {targetRoom}");
            return;
        }
        // ── [/LEVEL-LOCK] ─────────────────────────────────────────────────────

        StartCoroutine(TransitionRoutine(targetRoom));
    }

    /// <summary>
    /// Về lại Bedroom — restore sprite gốc, tắt ambient, bật nhạc bedroom.
    /// </summary>
    public void GoToBedroom()
    {
        if (_isTransiting) return;
        if (_currentRoom == RoomType.Bedroom) return;

        _currentRoom = RoomType.Bedroom;

        if (backgroundImage != null)
            backgroundImage.sprite = _bedroomSprite;

        if (roomNameText != null)
            roomNameText.text = "Phòng Ngủ";

        // Tắt ambient phòng cũ, bật nhạc bedroom
        StopAmbient();
        BedroomManager.Instance?.PlayBedroomMusic();

        Debug.Log("[RoomMapManager] Về Phòng Ngủ — ambient OFF, bedroom music ON");
    }

    public RoomType CurrentRoom => _currentRoom;

    // =========================================================================
    // TRANSITION
    // =========================================================================
    private IEnumerator TransitionRoutine(RoomType targetRoom)
    {
        _isTransiting = true;
        yield return StartCoroutine(FadeOverlay(0f, 1f));
        ApplyRoomImmediate(targetRoom);

        // ── Xử lý âm thanh khi chuyển phòng ──────────────────────────────────
        if (targetRoom == RoomType.Bedroom)
        {
            // Về bedroom: tắt ambient, bật nhạc bedroom
            StopAmbient();
            BedroomManager.Instance?.PlayBedroomMusic();
        }
        else
        {
            // Sang phòng khác: tắt nhạc bedroom, phát ambient phòng mới
            BedroomManager.Instance?.StopBedroomMusic();
            var config = GetConfig(targetRoom);
            PlayAmbient(config);
        }
        // ─────────────────────────────────────────────────────────────────────

        yield return StartCoroutine(FadeOverlay(1f, 0f));
        _isTransiting = false;
        OnRoomChanged?.Invoke(targetRoom);
        Debug.Log($"[RoomMapManager] Đã chuyển sang: {targetRoom}");
    }

    private IEnumerator FadeOverlay(float fromAlpha, float toAlpha)
    {
        if (transitionOverlay == null) yield break;
        transitionOverlay.raycastTarget = true;
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t  = Mathf.Clamp01(elapsed / fadeDuration);
            transitionOverlay.color = new Color(0f, 0f, 0f, Mathf.Lerp(fromAlpha, toAlpha, t));
            yield return null;
        }
        transitionOverlay.color         = new Color(0f, 0f, 0f, toAlpha);
        transitionOverlay.raycastTarget = toAlpha > 0.01f;
    }

    // =========================================================================
    // APPLY ROOM
    // =========================================================================
    private void ApplyRoomImmediate(RoomType targetRoom)
    {
        var oldConfig = GetConfig(_currentRoom);
        if (oldConfig?.roomHitAreas != null)
            foreach (var go in oldConfig.roomHitAreas)
                if (go != null) go.SetActive(false);

        var newConfig = GetConfig(targetRoom);
        if (newConfig != null)
        {
            if (backgroundImage != null && newConfig.backgroundSprite != null)
                backgroundImage.sprite = newConfig.backgroundSprite;

            if (roomNameText != null)
                roomNameText.text = newConfig.roomNameVN;

            if (newConfig.roomHitAreas != null)
                foreach (var go in newConfig.roomHitAreas)
                    if (go != null) go.SetActive(true);
        }

        _currentRoom = targetRoom;
    }

    // =========================================================================
    // AMBIENT SOUND
    // =========================================================================

    /// <summary>
    /// Phát ambient của phòng. Nếu config không có clip thì im lặng (fade out clip cũ).
    /// </summary>
    private void PlayAmbient(RoomConfig config)
    {
        if (_ambientFadeCoroutine != null) StopCoroutine(_ambientFadeCoroutine);

        if (config == null || config.ambientClip == null)
        {
            // Phòng không có ambient → fade out rồi stop
            _ambientFadeCoroutine = StartCoroutine(FadeAmbient(0f, stopAfterFade: true));
            return;
        }

        // Đổi clip
        _ambientSource.Stop();
        _ambientSource.clip   = config.ambientClip;
        _ambientSource.volume = 0f;
        _ambientSource.Play();

        _ambientFadeCoroutine = StartCoroutine(FadeAmbient(config.ambientVolume));
        Debug.Log($"[RoomMapManager] ♪ Ambient ON → {config.roomType} ({config.ambientClip.name})");
    }

    /// <summary>
    /// Fade out và dừng ambient hiện tại.
    /// </summary>
    private void StopAmbient()
    {
        if (!_ambientSource.isPlaying) return;
        if (_ambientFadeCoroutine != null) StopCoroutine(_ambientFadeCoroutine);
        _ambientFadeCoroutine = StartCoroutine(FadeAmbient(0f, stopAfterFade: true));
        Debug.Log("[RoomMapManager] ♪ Ambient OFF");
    }

    private IEnumerator FadeAmbient(float targetVolume, bool stopAfterFade = false)
    {
        float startVolume = _ambientSource.volume;
        float elapsed     = 0f;

        if (ambientFadeDuration <= 0f)
        {
            _ambientSource.volume = targetVolume;
        }
        else
        {
            while (elapsed < ambientFadeDuration)
            {
                elapsed               += Time.deltaTime;
                _ambientSource.volume  = Mathf.Lerp(startVolume, targetVolume, elapsed / ambientFadeDuration);
                yield return null;
            }
            _ambientSource.volume = targetVolume;
        }

        if (stopAfterFade) _ambientSource.Stop();
    }

    // =========================================================================
    // HELPER
    // =========================================================================
    private RoomConfig GetConfig(RoomType roomType)
    {
        if (rooms == null) return null;
        foreach (var cfg in rooms)
            if (cfg.roomType == roomType) return cfg;
        return null;
    }

    /// <summary>Kiểm tra phòng có bị khóa không (dùng cho UI lock overlay).</summary>
    public bool IsRoomLocked(RoomType room)
    {
        var config = GetConfig(room);
        if (config == null) return false;
        UserData user = AuthManager.Instance?.CurrentUserData;
        return user != null && user.level < config.requiredLevel;
    }

    /// <summary>Lấy level yêu cầu của phòng.</summary>
    public int GetRequiredLevel(RoomType room)
    {
        return GetConfig(room)?.requiredLevel ?? 0;
    }
}