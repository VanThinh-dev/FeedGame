using System.Collections;
using UnityEngine;

// Singleton nhỏ để chạy coroutine khi object gốc sắp bị Destroy
// Không cần kéo vào scene — tự tạo khi cần
public class EffectRunner : MonoBehaviour
{
    private static EffectRunner _instance;

    private static EffectRunner Instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject go = new GameObject("EffectRunner");
                DontDestroyOnLoad(go);          // Tồn tại xuyên scene
                _instance = go.AddComponent<EffectRunner>();
            }
            return _instance;
        }
    }

    // Gọi từ bất kỳ đâu để chạy coroutine không phụ thuộc object gốc
    public static void Run(IEnumerator routine)
    {
        Instance.StartCoroutine(routine);
    }
}