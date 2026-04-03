using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// =============================================================================
// GameManager.cs — v3 (PATCHED)
// =============================================================================

// -----------------------------------------------------------------------------
// Struct chứa kết quả một phiên chơi
// -----------------------------------------------------------------------------
[Serializable]
public struct GameResult
{
    public int   correct;
    public int   total;
    public float timeTaken;
}

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public event Action<GameResult> OnGameCompleted;

    // ── UI ───────────────────────────────────────────────────────────────────
    [Header("UI")]
    [SerializeField] private TextMeshPro targetText;

    // ── Audio ────────────────────────────────────────────────────────────────
    [Header("Audio")]
    [SerializeField] private AudioClip bgMusic;
    private AudioSource musicSource;

    // ── Dữ liệu từ vựng ──────────────────────────────────────────────────────
    private List<VocabWord> allWords       = new List<VocabWord>();
    private List<VocabWord> remainingWords = new List<VocabWord>();
    private VocabWord currentTarget;
    private bool   gameReady         = false;

    private string _currentLessonId  = "";

    // =========================== PATCH START ===========================
    // Field lesson name để RewardManager đọc
    private string _currentLessonName = "";

    // Public property cho RewardManager đọc
    public string CurrentLessonId   => _currentLessonId;
    public string CurrentLessonName => _currentLessonName;
    // =========================== PATCH END =============================


    // ── Theo dõi kết quả ───────────────────────────────────────────────
    private int   _correctCount   = 0;
    private float _gameStartTime  = 0f;

    // ── Components ─────────────────────────────────────────────────────
    private TargetTextAnimator targetAnimator;
    private ConfettiEffect     confettiEffect;

    // ── Exit Button ────────────────────────────────────────────────────
    private GameObject exitButtonGO;

    // =========================================================================
    // LIFECYCLE
    // =========================================================================

    private void Awake()
    {
        Instance = this;

        musicSource             = gameObject.AddComponent<AudioSource>();
        musicSource.loop        = true;
        musicSource.playOnAwake = false;
        musicSource.volume      = 0.5f;

        confettiEffect = GetComponent<ConfettiEffect>();
        if (confettiEffect == null)
            confettiEffect = gameObject.AddComponent<ConfettiEffect>();
    }

    private void Start()
    {
        if (targetText != null)
        {
            targetAnimator = targetText.GetComponent<TargetTextAnimator>();
            if (targetAnimator == null)
                targetAnimator = targetText.gameObject.AddComponent<TargetTextAnimator>();
        }

        BuildExitButton();
    }

    // =========================================================================
    // BUILD EXIT BUTTON
    // =========================================================================

    private void BuildExitButton()
    {
        var canvasGO = new GameObject("ExitButtonCanvas");
        var canvas   = canvasGO.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;

        canvasGO.AddComponent<CanvasScaler>().uiScaleMode =
            CanvasScaler.ScaleMode.ScaleWithScreenSize;

        canvasGO.AddComponent<GraphicRaycaster>();

        exitButtonGO = new GameObject("ExitButton", typeof(RectTransform));
        exitButtonGO.transform.SetParent(canvasGO.transform, false);

        var rt = exitButtonGO.GetComponent<RectTransform>();
        rt.anchorMin        = new Vector2(1f, 1f);
        rt.anchorMax        = new Vector2(1f, 1f);
        rt.pivot            = new Vector2(1f, 1f);
        rt.anchoredPosition = new Vector2(-16f, -16f);
        rt.sizeDelta        = new Vector2(52f, 52f);

        var img        = exitButtonGO.AddComponent<Image>();
        img.color      = new Color(0.95f, 0.35f, 0.35f, 0.92f);
        img.sprite     = CreateCircleSprite();
        img.type       = Image.Type.Simple;
        img.preserveAspect = true;

        var shadowGO = new GameObject("Shadow", typeof(RectTransform));
        shadowGO.transform.SetParent(canvasGO.transform, false);
        shadowGO.transform.SetSiblingIndex(0);

        var shadowRT = shadowGO.GetComponent<RectTransform>();
        shadowRT.anchorMin        = rt.anchorMin;
        shadowRT.anchorMax        = rt.anchorMax;
        shadowRT.pivot            = rt.pivot;
        shadowRT.anchoredPosition = new Vector2(-14f, -18f);
        shadowRT.sizeDelta        = new Vector2(52f, 52f);

        var shadowImg = shadowGO.AddComponent<Image>();
        shadowImg.color  = new Color(0f,0f,0f,0.18f);
        shadowImg.sprite = img.sprite;

        var iconGO = new GameObject("Icon", typeof(RectTransform));
        iconGO.transform.SetParent(exitButtonGO.transform, false);

        var iconRT = iconGO.GetComponent<RectTransform>();
        iconRT.anchorMin = Vector2.zero;
        iconRT.anchorMax = Vector2.one;
        iconRT.offsetMin = iconRT.offsetMax = Vector2.zero;

        var iconTMP = iconGO.AddComponent<TextMeshProUGUI>();
        iconTMP.text = "X";
        iconTMP.fontSize = 22;
        iconTMP.fontStyle = FontStyles.Bold;
        iconTMP.color = Color.white;
        iconTMP.alignment = TextAlignmentOptions.Center;

        var btn = exitButtonGO.AddComponent<Button>();

        var colors = btn.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1f,0.8f,0.8f);
        colors.pressedColor = new Color(0.7f,0.2f,0.2f);

        btn.colors = colors;
        btn.transition = Selectable.Transition.ColorTint;
        btn.onClick.AddListener(ExitGameplay);

        canvasGO.SetActive(false);
        exitButtonGO = canvasGO;
    }

    private static Sprite CreateCircleSprite()
    {
        int size = 128;
        var tex = new Texture2D(size,size,TextureFormat.RGBA32,false);
        var pixels = new Color[size*size];

        float center = size/2f;
        float radius = size/2f-1;

        for(int y=0;y<size;y++)
        for(int x=0;x<size;x++)
        {
            float dx=x-center;
            float dy=y-center;
            float dist=Mathf.Sqrt(dx*dx+dy*dy);
            float alpha=Mathf.Clamp01(radius-dist+0.5f);

            pixels[y*size+x]=new Color(1,1,1,alpha);
        }

        tex.SetPixels(pixels);
        tex.Apply();

        return Sprite.Create(tex,new Rect(0,0,size,size),new Vector2(0.5f,0.5f),size);
    }

    // =========================================================================
    // ENTRY POINT
    // =========================================================================

    public void StartGameWithVocab(List<VocabWord> words)
    {
        if(words==null||words.Count==0)
        {
            Debug.LogWarning("[GameManager] StartGameWithVocab: danh sach rong.");
            return;
        }

        // =========================== PATCH START ===========================
        _currentLessonId   = words[0].lessonId;
        _currentLessonName = words[0].lessonName ?? words[0].lessonId;
        // =========================== PATCH END =============================

        allWords  = words;
        gameReady = true;

        _correctCount  = 0;
        _gameStartTime = Time.time;

        StopAllCoroutines();

        if(exitButtonGO!=null)
            exitButtonGO.SetActive(true);

        PlayBgMusic();
        StartNewRound();
    }

    public void ExitGameplay()
    {
        gameReady=false;

        StopAllCoroutines();
        StopBgMusic();

        BubbleManager.Instance?.ClearAllBubbles();

        if(targetText!=null)
            targetText.text="";

        if(exitButtonGO!=null)
            exitButtonGO.SetActive(false);

        BedroomManager.Instance?.ExitGameplay();
    }

    public void StartNewRound()
    {
        if(!gameReady) return;

        remainingWords=new List<VocabWord>(allWords);
        ShuffleList(remainingWords);

        SetNextTarget(true);
    }

    private void SetNextTarget(bool spawnAfter=false)
    {
        if(remainingWords.Count==0)
        {
            OnSessionComplete();
            return;
        }

        currentTarget=remainingWords[0];
        remainingWords.RemoveAt(0);

        if(targetText!=null)
            targetText.text=currentTarget.vietnamese;

        targetAnimator?.PlayNewTarget();

        if(spawnAfter)
        {
            BubbleManager.Instance.ClearAllBubbles();
            BubbleManager.Instance.SpawnGrid();
        }
    }

    public string GetCurrentTargetEnglish()
        => currentTarget?.english ?? "";

    public List<string> GetAllEnglishWords()
    {
        var list=new List<string>();
        foreach(var w in allWords)
            list.Add(w.english);

        return list;
    }

    public void OnBubbleClicked(BubbleController bubble)
    {
        if(currentTarget==null) return;

        if(bubble.GetWord()==currentTarget.english)
        {
            _correctCount++;
            bubble.Pop();
        }
    }

    public void OnFishEaten()
    {
        StartCoroutine(DelayedReset(0.5f));
    }

    private IEnumerator DelayedReset(float delay)
    {
        yield return new WaitForSeconds(delay);
        SetNextTarget(true);
    }

    private void OnSessionComplete()
    {
        if(targetText!=null)
            targetText.text="Bạn Đã Hoàn Thành !";

        confettiEffect?.Play();

        BubbleManager.Instance.ClearAllBubbles();
        StopBgMusic();

        if(exitButtonGO!=null)
            exitButtonGO.SetActive(false);

        if(!string.IsNullOrEmpty(_currentLessonId))
            VocabManager.Instance?.MarkLessonCompleted(_currentLessonId);

        gameReady=false;

        var result=new GameResult
        {
            correct=_correctCount,
            total=allWords.Count,
            timeTaken=Time.time-_gameStartTime
        };

        OnGameCompleted?.Invoke(result);
    }

    private void PlayBgMusic()
    {
        if(bgMusic==null||musicSource==null||musicSource.isPlaying)
            return;

        musicSource.clip=bgMusic;
        musicSource.Play();
    }

    private void StopBgMusic()
    {
        if(musicSource!=null&&musicSource.isPlaying)
            musicSource.Stop();
    }

    public void ForceStopMusic()
        => StopBgMusic();

    private void ShuffleList<T>(List<T> list)
    {
        for(int i=list.Count-1;i>0;i--)
        {
            int j=UnityEngine.Random.Range(0,i+1);
            (list[i],list[j])=(list[j],list[i]);
        }
    }
}