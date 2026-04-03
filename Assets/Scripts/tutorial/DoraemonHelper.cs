// Assets/Scripts/Tutorial/DoraemonHelper.cs
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;

public class DoraemonHelper : MonoBehaviour, IPointerClickHandler
{
    [Header("=== Sprite nhân vật ===")]
    [SerializeField] public Sprite characterSprite;

    [Header("=== HelpCanvas ===")]
    [SerializeField] private GameObject helpCanvas;

    [Header("=== Animation bounce ===")]
    [SerializeField] private float bounceSpeed  = 1.5f;
    [SerializeField] private float bounceHeight = 12f;

    private RectTransform rt;
    private Vector2 basePos;
    private Image img;

    void Awake()
    {
        rt  = GetComponent<RectTransform>();
        img = GetComponent<Image>();
        if (helpCanvas) helpCanvas.SetActive(false);
        transform.localScale = Vector3.zero; // ẩn đến khi login
    }

    void Start()
    {
        basePos = rt.anchoredPosition;
        if (img && characterSprite) img.sprite = characterSprite;
        StartCoroutine(WaitForLogin());
    }

    private IEnumerator WaitForLogin()
    {
        while (AuthManager.Instance == null) yield return null;

        if (AuthManager.Instance.IsLoggedIn)
        {
            ShowDoraemon();
            yield break;
        }

        AuthManager.Instance.OnLoginSuccess += OnLoggedIn;
    }

    private void OnLoggedIn(UserData _) => ShowDoraemon();

    private void ShowDoraemon() => transform.localScale = Vector3.one;

    void OnDestroy()
    {
        if (AuthManager.Instance != null)
            AuthManager.Instance.OnLoginSuccess -= OnLoggedIn;
    }

    void Update()
    {
        if (rt)
            rt.anchoredPosition = basePos +
                new Vector2(0, Mathf.Sin(Time.time * bounceSpeed) * bounceHeight);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (helpCanvas) helpCanvas.SetActive(!helpCanvas.activeSelf);
    }
}