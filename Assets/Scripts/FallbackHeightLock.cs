using UnityEngine;

/// <summary>
/// 免頭盔（鍵盤滑鼠）模式的固定視角高度。
/// 由 NoHmdSpawnFix 自動掛到鍵盤滑鼠攝影機上，不用手動加。
///
/// 進場景時往下偵測地板，之後每一幀把攝影機高度固定在「地板 + eyeHeight」，
/// 所以 Q / E 和抬頭、低頭走路都不會改變高度。
/// 頭盔模式的高度由 Player 上的 PlayerMovementScript 處理。
/// </summary>
public class FallbackHeightLock : MonoBehaviour
{
    [Tooltip("眼睛離地板的高度（公尺）")]
    public float eyeHeight = 1.6f;

    float floorY;
    bool ready;

    void Start()
    {
        floorY = DetectFloor();
        ready = true;
        Debug.Log("[免頭盔] 地板高度 = " + floorY.ToString("F3") + "，視角固定在 " + eyeHeight + " 公尺");
    }

    void LateUpdate()
    {
        if (!ready)
            return;
        Vector3 p = transform.position;
        p.y = floorY + eyeHeight;
        transform.position = p;
    }

    float DetectFloor()
    {
        Transform playerRoot = transform.root;
        float fallback = transform.parent != null ? transform.parent.position.y : 0f;

        // 從攝影機上方往下打，略過天花板、玩家自己和可以拿起來的東西
        RaycastHit[] hits = Physics.RaycastAll(transform.position + Vector3.up * 1.5f, Vector3.down, 8f,
                                               Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        foreach (var hit in hits)
        {
            if (hit.point.y > transform.position.y + 1f)
                continue;
            if (hit.collider.transform.IsChildOf(playerRoot))
                continue;
            if (hit.collider.attachedRigidbody != null && !hit.collider.attachedRigidbody.isKinematic)
                continue;
            return hit.point.y;
        }
        return fallback;
    }
}
