using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// BedroomBackground — gắn vào BackgroundImage.
/// Lộ slot [SerializeField] backgroundSprite để kéo ảnh pixel art vào Inspector.
/// Tự động apply sprite vào Image component khi thay đổi trong Editor hoặc lúc Start.
/// </summary>
[RequireComponent(typeof(Image))]
public class BedroomBackground : MonoBehaviour
{
    // ─── Inspector Slot ───────────────────────────────────────────
    [Header("Kéo ảnh pixel art phòng ngủ vào đây")]
    [SerializeField] public Sprite backgroundSprite;

    // ─── Unity Lifecycle ─────────────────────────────────────────
    private void Start()
    {
        ApplySprite();
    }

    /// <summary>
    /// Áp dụng sprite vào Image component.
    /// Gọi tự động lúc Start, hoặc gọi thủ công sau khi thay sprite runtime.
    /// </summary>
    public void ApplySprite()
    {
        if (backgroundSprite == null) return;

        Image img = GetComponent<Image>();
        img.sprite = backgroundSprite;
        img.color  = Color.white; // reset màu placeholder về trắng khi có ảnh thật
        img.type   = Image.Type.Simple;
        img.preserveAspect = false; // stretch full screen
    }

#if UNITY_EDITOR
    // Tự động preview trong Editor khi thay Sprite
    private void OnValidate()
    {
        if (backgroundSprite != null)
            ApplySprite();
    }
#endif
}