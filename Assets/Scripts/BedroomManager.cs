using System.Collections;
using UnityEngine;

// =============================================================================
// BedroomManager.cs — v6  (+ Bedroom Music)
//
// THÊM MỚI so với v5:
//   • [Header("Bedroom Music")] — kéo AudioClip nhạc nền vào bedroomMusic
//   • bedroomAudioSource tự tạo runtime, loop = true, volume tuỳ chỉnh
//   • Nhạc CHỈ phát khi đang ở Bedroom:
//       - Bật: ShowBedroom(), ExitGameplay(), HandleLoginSuccess()
//       - Tắt: EnterGameplay(), HideBedroom(), OnLogout()
//   • StopBedroomMusic() public → RoomMapManager gọi khi chuyển sang phòng khác
// =============================================================================

public class BedroomManager : MonoBehaviour
{
    public static BedroomManager Instance { get; private set; }

    // ── Canvases ──────────────────────────────────────────────────────────────
    [Header("Auth")]
    [SerializeField] private GameObject authCanvas;

    [Header("Canvases")]
    [SerializeField] private GameObject bedroomCanvas;

    [Header("Keo BackgroundCanvas vao day")]
    [SerializeField] private GameObject backgroundCanvas;

    [Header("Sub Canvases")]
    [SerializeField] private GameObject vocabCanvas;
    [SerializeField] private GameObject medalCanvas;

    [Header("Gameplay Background")]
    [SerializeField] private GameObject gameplayBackground;     

    // ── Bedroom Music ─────────────────────────────────────────────────────────
    [Header("Bedroom Music")]
    [Tooltip("Kéo AudioClip nhạc nền phòng ngủ vào đây")]
    [SerializeField] private AudioClip bedroomMusic;

    [Range(0f, 1f)]
    [SerializeField] private float bedroomMusicVolume = 0.5f;

    [Tooltip("Thời gian fade in/out nhạc (giây). 0 = bật/tắt ngay)")]
    [SerializeField] private float musicFadeDuration = 1.0f;

    // ── Internal ──────────────────────────────────────────────────────────────
    private AudioSource _bedroomAudioSource;
    private Coroutine   _musicFadeCoroutine;

    // =========================================================================
    // LIFECYCLE
    // =========================================================================

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (bedroomCanvas != null) bedroomCanvas.SetActive(false);

        // Tạo AudioSource riêng cho bedroom music
        _bedroomAudioSource             = gameObject.AddComponent<AudioSource>();
        _bedroomAudioSource.clip        = bedroomMusic;
        _bedroomAudioSource.loop        = true;
        _bedroomAudioSource.playOnAwake = false;
        _bedroomAudioSource.volume      = 0f;
    }

    private void Start() => StartCoroutine(WaitAndSubscribe());

    private void OnDestroy()
    {
        if (AuthManager.Instance != null)
        {
            AuthManager.Instance.OnLoginSuccess  -= HandleLoginSuccess;
            AuthManager.Instance.OnLogout        -= HandleLogoutEvent;
            AuthManager.Instance.OnUserDataReady -= HandleUserDataReady;
        }
    }

    // =========================================================================
    // SUBSCRIBE
    // =========================================================================

    private IEnumerator WaitAndSubscribe()
    {
        while (AuthManager.Instance == null) yield return null;

        AuthManager.Instance.OnLoginSuccess  += HandleLoginSuccess;
        AuthManager.Instance.OnLogout        += HandleLogoutEvent;
        AuthManager.Instance.OnUserDataReady += HandleUserDataReady;

        while (!AuthManager.Instance.IsInitialized) yield return null;

        if (AuthManager.Instance.IsLoggedIn)
            AuthManager.Instance.LoadUserData(AuthManager.Instance.CurrentUser.UserId);
    }

    // =========================================================================
    // EVENT HANDLERS
    // =========================================================================

    private void HandleLoginSuccess(UserData userData)
    {
        if (gameplayBackground != null) gameplayBackground.SetActive(false);
        Debug.Log($"[BedroomManager] Chao {userData.displayName}");
        if (authCanvas != null) authCanvas.SetActive(false);
        ShowBedroom(skipRefresh: true);
    }

    private void HandleLogoutEvent() => OnLogout();

    private void HandleUserDataReady(UserData userData)
    {
        Debug.Log($"[BedroomManager] Hot-reload OK: level={userData.level} xp={userData.xp} coins={userData.coins}");
    }

    // =========================================================================
    // PUBLIC API
    // =========================================================================

    /// <summary>
    /// Hiện Bedroom. Mặc định gọi RefreshUserData() để đồng bộ data Firebase.
    /// Truyền skipRefresh=true khi gọi ngay sau login (data đã có sẵn).
    /// </summary>
    public void ShowBedroom(bool skipRefresh = false)
    {
        if (backgroundCanvas != null) backgroundCanvas.SetActive(true);
        if (bedroomCanvas    != null) bedroomCanvas.SetActive(true);
        HideAllSubCanvases();
        PlayBedroomMusic();

        if (!skipRefresh)
        {
            Debug.Log("[BedroomManager] ShowBedroom → RefreshUserData");
            AuthManager.Instance?.RefreshUserData();
        }
    }

    public void HideBedroom()
    {
        if (bedroomCanvas != null) bedroomCanvas.SetActive(false);
        StopBedroomMusic();
    }

    public void EnterGameplay()
    {
        HideAllSubCanvases();
        if (bedroomCanvas    != null) bedroomCanvas.SetActive(false);
        if (backgroundCanvas != null) backgroundCanvas.SetActive(false);
        if (authCanvas       != null) authCanvas.SetActive(false);
        if (gameplayBackground != null) gameplayBackground.SetActive(true);
        StopBedroomMusic();
        Debug.Log("[BedroomManager] Vao gameplay — an tat ca canvas.");
    }

    /// <summary>
    /// Gọi khi thoát gameplay → tự động hot-reload data từ Firebase.
    /// </summary>
    public void ExitGameplay()
    {
        if (gameplayBackground != null) gameplayBackground.SetActive(false);
        if (backgroundCanvas != null) backgroundCanvas.SetActive(true);
        if (bedroomCanvas    != null) bedroomCanvas.SetActive(true);
        HideAllSubCanvases();
        PlayBedroomMusic();

        Debug.Log("[BedroomManager] ExitGameplay → RefreshUserData");
        AuthManager.Instance?.RefreshUserData();
    }

    public void OpenVocabCanvas()
    {
        HideAllSubCanvases();
        if (vocabCanvas != null) vocabCanvas.SetActive(true);
        else Debug.LogWarning("[BedroomManager] vocabCanvas chua gan!");
    }

    public void OpenMedalCanvas()
    {
        HideAllSubCanvases();

        if (MedalManager.Instance != null)
            MedalManager.Instance.OpenMedalCanvas();
        else
        {
            Debug.LogWarning("[BedroomManager] MedalManager.Instance chua co — fallback SetActive.");
            if (medalCanvas != null) medalCanvas.SetActive(true);
        }
    }

    public void CloseAllSubCanvases() => HideAllSubCanvases();

    public void OnLogout()
    {
        HideBedroom(); // HideBedroom đã gọi StopBedroomMusic()
        if (backgroundCanvas != null) backgroundCanvas.SetActive(true);
        if (authCanvas       != null) authCanvas.SetActive(true);
    }

    // =========================================================================
    // MUSIC — PUBLIC (để RoomMapManager tắt khi chuyển phòng)
    // =========================================================================

    /// <summary>
    /// Bật nhạc phòng ngủ (fade in). Tự bỏ qua nếu chưa gán clip.
    /// </summary>
    public void PlayBedroomMusic()
    {
        if (bedroomMusic == null || _bedroomAudioSource == null) return;

        // Nếu đang phát rồi thì không làm gì
        if (_bedroomAudioSource.isPlaying) return;

        _bedroomAudioSource.clip = bedroomMusic;
        _bedroomAudioSource.Play();
        FadeMusic(_bedroomAudioSource, bedroomMusicVolume);
        Debug.Log("[BedroomManager] ♪ Bedroom music ON");
    }

    /// <summary>
    /// Tắt nhạc phòng ngủ (fade out). Gọi từ RoomMapManager khi chuyển phòng.
    /// </summary>
    public void StopBedroomMusic()
    {
        if (_bedroomAudioSource == null || !_bedroomAudioSource.isPlaying) return;
        FadeMusic(_bedroomAudioSource, 0f, stopAfterFade: true);
        Debug.Log("[BedroomManager] ♪ Bedroom music OFF");
    }

    // =========================================================================
    // MUSIC — PRIVATE HELPERS
    // =========================================================================

    private void FadeMusic(AudioSource source, float targetVolume, bool stopAfterFade = false)
    {
        if (_musicFadeCoroutine != null) StopCoroutine(_musicFadeCoroutine);
        _musicFadeCoroutine = StartCoroutine(FadeRoutine(source, targetVolume, stopAfterFade));
    }

    private IEnumerator FadeRoutine(AudioSource source, float targetVolume, bool stopAfterFade)
    {
        float startVolume = source.volume;
        float elapsed     = 0f;

        if (musicFadeDuration <= 0f)
        {
            source.volume = targetVolume;
        }
        else
        {
            while (elapsed < musicFadeDuration)
            {
                elapsed       += Time.deltaTime;
                source.volume  = Mathf.Lerp(startVolume, targetVolume, elapsed / musicFadeDuration);
                yield return null;
            }
            source.volume = targetVolume;
        }

        if (stopAfterFade) source.Stop();
    }

    // =========================================================================
    // PRIVATE HELPERS
    // =========================================================================

    private void HideAllSubCanvases()
    {
        if (vocabCanvas != null) vocabCanvas.SetActive(false);
        if (medalCanvas != null) medalCanvas.SetActive(false);
    }
}