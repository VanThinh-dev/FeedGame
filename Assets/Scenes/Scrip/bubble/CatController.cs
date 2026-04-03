using System.Collections;
using UnityEngine;

public class CatController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;

    [Header("Detection")]
    // Khoảng cách để mèo "ăn" cá, chỉnh trong Inspector nếu cần
    [SerializeField] private float catchRadius = 0.8f;

    [Header("Animation")]
    [SerializeField] private Animator animator;
    // Tên Bool parameter trong Animator Controller của mèo
    [SerializeField] private string runParam   = "isRunning";
    [SerializeField] private string catchParam = "isCatching";

    [Header("Sound")]
    // Kéo file âm thanh meow vào đây trong Inspector
    [SerializeField] private AudioClip meowSound;

    [Header("Timing")]
    [SerializeField] private float catchDuration = 2f;

    private FishItem trackedFish = null;
    private bool isCatching = false;

    private void Awake()
    {
        if (animator == null)
            animator = GetComponent<Animator>();

        // FIX ÂM THANH: Không dùng AudioSource component
        // Dùng AudioSource.PlayClipAtPoint() để tránh duplicate
    }

    private void Update()
    {
        if (isCatching) return;

        // Tìm cá nếu chưa có target hoặc target đã bị xóa
        if (trackedFish == null)
            trackedFish = FindNearestFish();

        if (trackedFish != null)
        {
            float dist = Vector2.Distance(transform.position, trackedFish.transform.position);

            // Đủ gần → ăn cá
            if (dist <= catchRadius)
            {
                CatchFish(trackedFish);
                return;
            }

            // Chạy theo trục X về phía cá, giữ nguyên trục Y của mèo
            Vector3 targetPos = new Vector3(
                trackedFish.transform.position.x,
                transform.position.y,
                0
            );

            // Flip sprite mèo theo hướng chạy
            float dir = targetPos.x - transform.position.x;
            if (Mathf.Abs(dir) > 0.05f)
            {
                transform.localScale = new Vector3(
                    Mathf.Sign(dir) * Mathf.Abs(transform.localScale.x),
                    transform.localScale.y,
                    transform.localScale.z
                );
            }

            SetAnim(runParam, true);
            transform.position = Vector3.MoveTowards(
                transform.position, targetPos, moveSpeed * Time.deltaTime
            );
        }
        else
        {
            // Không có cá → đứng yên
            SetAnim(runParam, false);
        }
    }

    private FishItem FindNearestFish()
    {
        FishItem[] allFish = FindObjectsByType<FishItem>(FindObjectsSortMode.None);
        FishItem nearest = null;
        float minDist = Mathf.Infinity;

        foreach (FishItem fish in allFish)
        {
            // FIX LỖI 4: Chỉ theo cá đã chạm đất, tránh mèo kêu ngay khi bóng vỡ
            if (!fish.HasLanded()) continue;

            float dist = Vector3.Distance(transform.position, fish.transform.position);
            if (dist < minDist)
            {
                minDist = dist;
                nearest = fish;
            }
        }

        return nearest;
    }

    // Được gọi từ Update() khi mèo đủ gần cá
    public void CatchFish(FishItem fish)
    {
        // FIX LỖI 2: Guard isCatching tránh gọi nhiều lần cùng lúc
        if (isCatching) return;

        trackedFish = null; // reset target trước để tránh gọi lại
        StartCoroutine(CatchRoutine(fish));
    }

    private IEnumerator CatchRoutine(FishItem fish)
    {
        isCatching = true;

        SetAnim(runParam,   false);
        SetAnim(catchParam, true);

        // Xóa cá ngay lập tức để tránh mèo bắt 2 lần
        if (fish != null) fish.Disappear();

        // FIX LỖI 2: Dùng PlayClipAtPoint thay AudioSource.PlayOneShot
        // PlayOneShot có thể bị gọi nhiều lần nếu Update() chạy trước khi isCatching = true
        if (meowSound != null)
            AudioSource.PlayClipAtPoint(meowSound, Camera.main.transform.position);

        yield return new WaitForSeconds(catchDuration);

        SetAnim(catchParam, false);
        isCatching = false;

        if (GameManager.Instance != null)
        GameManager.Instance.OnFishEaten();
    }

    // Bật/tắt animation parameter kiểu Bool
    private void SetAnim(string param, bool value)
    {
        if (animator != null)
            animator.SetBool(param, value);
    }
    
}