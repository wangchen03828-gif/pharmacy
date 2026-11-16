using UnityEngine;
using UnityEngine.SceneManagement;
using Valve.VR.InteractionSystem;

/// <summary>
/// 免頭盔（鍵盤滑鼠）模式的出生點修正。
///
/// 練習場景裡，原作者把 Player 底下的 SteamVRObjects（頭盔用）移到了房間裡的正確位置，
/// 但 NoSteamVRFallbackObjects（鍵盤滑鼠用）還留在 Player 原點，
/// 所以沒接頭盔時攝影機會出生在牆邊、往上看。
///
/// 這支腳本在每個場景載入後檢查：如果 SteamVR Player 切到了鍵盤滑鼠模式，
/// 就把鍵盤滑鼠攝影機移到頭盔版的同一個位置，並改成平視。
/// 有頭盔時不會做任何事；不需要掛在場景裡。
/// </summary>
public class NoHmdSpawnFix : MonoBehaviour
{
    const float TimeoutSeconds = 10f;
    float startTime;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Register()
    {
        SceneManager.sceneLoaded += (scene, mode) =>
        {
            var go = new GameObject("[NoHmdSpawnFix]");
            go.hideFlags = HideFlags.HideInHierarchy;
            go.AddComponent<NoHmdSpawnFix>();
        };
    }

    void Start()
    {
        startTime = Time.realtimeSinceStartup;
    }

    void Update()
    {
        // 等 SteamVR Player 決定好要用頭盔還是鍵盤滑鼠（Player.Start 會等 SteamVR 初始化結束）
        if (Time.realtimeSinceStartup - startTime > TimeoutSeconds)
        {
            Destroy(gameObject);
            return;
        }

        Player player = Player.instance;
        if (player == null || player.rigSteamVR == null || player.rig2DFallback == null)
            return;

        if (player.rigSteamVR.activeSelf)
        {
            // 頭盔模式：不需要修正
            Destroy(gameObject);
            return;
        }

        if (!player.rig2DFallback.activeSelf)
            return;

        // 鍵盤滑鼠模式：移到頭盔版的出生位置
        Transform fallback = player.rig2DFallback.transform;
        Transform vr = player.rigSteamVR.transform;
        fallback.localPosition = vr.localPosition;
        fallback.localRotation = vr.localRotation;

        // 攝影機改成平視（保留左右方向）
        var cam = fallback.GetComponentInChildren<FallbackCameraController>(true);
        if (cam != null)
        {
            Vector3 e = cam.transform.localEulerAngles;
            cam.transform.localEulerAngles = new Vector3(0f, e.y, 0f);
        }

        Destroy(gameObject);
    }
}
