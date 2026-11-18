using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Valve.VR;
using Valve.VR.InteractionSystem;

/// <summary>
/// 讓場景裡的門（門.fbx）可以開關。不用掛在場景上：場景載入時自動找到所有門並加上這個元件。
///
/// 門.fbx 裡有兩個部分：「門框.001」（固定不動）和「文字.001」（門板＋玻璃＋把手＋門上的字）。
/// 這支腳本會在「文字.001」遠離把手的那一側建立轉軸，讓門板繞著它轉。
///
/// 操作：
///  - 戴頭盔：手靠近門板，按板機（GrabPinch）開／關。
///  - 免頭盔：滑鼠左鍵點門板，或看著門按 E。
/// 門會往「遠離你」的方向推開，再操作一次會關上。
/// </summary>
public class DoorSwing : MonoBehaviour
{
    public float openAngle = 90f;      // 打開的角度
    public float duration = 0.8f;      // 開關所需秒數
    public float handReach = 0.3f;     // 手離門板多近才算碰得到（公尺）
    public float lookReach = 3f;       // 免頭盔按 E 時，最遠可以操作的距離

    Transform pivot;
    Quaternion closedRotation;
    Quaternion targetRotation;
    Vector3 hingeToCenter;             // 轉軸到門板中心的水平方向（關門時）
    Collider leafCollider;
    bool isOpen;

    // ───────── 自動掛載 ─────────

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Register()
    {
        SceneManager.sceneLoaded += (scene, mode) => AttachToDoors(scene);
    }

    static void AttachToDoors(Scene scene)
    {
        var found = new List<Transform>();
        foreach (var root in scene.GetRootGameObjects())
            FindLeaves(root.transform, found);

        foreach (var leaf in found)
        {
            if (leaf.GetComponent<DoorSwing>() == null && leaf.GetComponentInParent<DoorSwing>() == null)
                leaf.gameObject.AddComponent<DoorSwing>();
        }
        if (found.Count > 0)
            Debug.Log("[DoorSwing] 找到 " + found.Count + " 扇門，已可開關。");
    }

    static void FindLeaves(Transform t, List<Transform> result)
    {
        // 門的根物件名稱以「門」開頭，底下名稱以「文字」開頭且有 MeshRenderer 的就是門板
        if (t.name.StartsWith("門"))
        {
            foreach (Transform child in t)
            {
                if (child.name.StartsWith("文字") && child.GetComponent<MeshFilter>() != null)
                    result.Add(child);
            }
        }
        foreach (Transform child in t)
            FindLeaves(child, result);
    }

    // ───────── 建立轉軸 ─────────

    void Start()
    {
        var mf = GetComponent<MeshFilter>();
        Mesh mesh = mf != null ? mf.sharedMesh : null;
        if (mesh == null)
        {
            enabled = false;
            return;
        }

        Vector3 center, widthDir, hinge;
        ComputeHinge(mesh, out center, out widthDir, out hinge);

        // 建一個轉軸物件，把門板放進去（保持世界位置不變）
        pivot = new GameObject(name + " 轉軸").transform;
        pivot.SetParent(transform.parent, true);
        pivot.position = hinge;
        pivot.rotation = Quaternion.identity;
        transform.SetParent(pivot, true);

        closedRotation = pivot.rotation;
        targetRotation = closedRotation;
        hingeToCenter = Vector3.ProjectOnPlane(center - hinge, Vector3.up);

        // 給門板一個碰撞體，滑鼠點擊、手的距離判斷都靠它
        leafCollider = GetComponent<Collider>();
        if (leafCollider == null)
        {
            var box = gameObject.AddComponent<BoxCollider>();
            box.center = mesh.bounds.center;
            box.size = mesh.bounds.size;
            leafCollider = box;
        }
    }

    /// <summary>找出門板中心、寬度方向，以及遠離把手那一側的轉軸位置（世界座標）。</summary>
    void ComputeHinge(Mesh mesh, out Vector3 center, out Vector3 widthDir, out Vector3 hinge)
    {
        Vector3[] local = mesh.isReadable ? mesh.vertices : null;

        if (local == null || local.Length == 0)
        {
            // 網格不可讀時退回用外框估計：轉軸放在寬度方向的一端
            Bounds b = GetComponent<Renderer>().bounds;
            center = b.center;
            widthDir = b.size.x >= b.size.z ? Vector3.right : Vector3.forward;
            hinge = center - widthDir * (Vector3.Dot(b.extents, new Vector3(Mathf.Abs(widthDir.x), 0, Mathf.Abs(widthDir.z))));
            return;
        }

        // 轉成世界座標
        var pts = new Vector3[local.Length];
        Vector3 sum = Vector3.zero;
        for (int i = 0; i < local.Length; i++)
        {
            pts[i] = transform.TransformPoint(local[i]);
            sum += pts[i];
        }
        center = sum / pts.Length;

        // 用網格自己的三個軸找方向：最接近垂直的是高度，剩下兩個裡比較長的是寬度
        Vector3 size = mesh.bounds.size;
        Vector3[] axes =
        {
            transform.TransformVector(new Vector3(size.x, 0, 0)),
            transform.TransformVector(new Vector3(0, size.y, 0)),
            transform.TransformVector(new Vector3(0, 0, size.z)),
        };
        int heightAxis = 0;
        for (int i = 1; i < 3; i++)
            if (Mathf.Abs(Vector3.Dot(axes[i].normalized, Vector3.up)) > Mathf.Abs(Vector3.Dot(axes[heightAxis].normalized, Vector3.up)))
                heightAxis = i;
        int a = (heightAxis + 1) % 3, c = (heightAxis + 2) % 3;
        int widthAxis = axes[a].magnitude >= axes[c].magnitude ? a : c;
        widthDir = Vector3.ProjectOnPlane(axes[widthAxis], Vector3.up).normalized;
        Vector3 depthDir = Vector3.Cross(Vector3.up, widthDir);

        float uMin = float.MaxValue, uMax = float.MinValue, vMin = float.MaxValue, vMax = float.MinValue;
        foreach (var p in pts)
        {
            Vector3 d = p - center;
            float u = Vector3.Dot(d, widthDir), v = Vector3.Dot(d, depthDir);
            uMin = Mathf.Min(uMin, u); uMax = Mathf.Max(uMax, u);
            vMin = Mathf.Min(vMin, v); vMax = Mathf.Max(vMax, v);
        }

        // 把手是門板厚度方向最突出的部分：取厚度方向兩端各 20% 的點，看它們偏向寬度的哪一邊
        float vMid = (vMin + vMax) * 0.5f, vHalf = (vMax - vMin) * 0.5f;
        float handleU = 0f; int handleCount = 0;
        foreach (var p in pts)
        {
            Vector3 d = p - center;
            float v = Vector3.Dot(d, depthDir);
            if (Mathf.Abs(v - vMid) > vHalf * 0.6f)
            {
                handleU += Vector3.Dot(d, widthDir);
                handleCount++;
            }
        }
        handleU = handleCount > 0 ? handleU / handleCount : 0f;

        float hingeU = handleU >= 0f ? uMin : uMax;   // 轉軸在把手的另一側
        hinge = center + widthDir * hingeU + depthDir * vMid;
    }

    // ───────── 操作 ─────────

    void Update()
    {
        if (pivot == null)
            return;

        if (WantsToggle())
            Toggle();

        pivot.rotation = Quaternion.RotateTowards(pivot.rotation, targetRotation, openAngle / duration * Time.deltaTime);
    }

    bool WantsToggle()
    {
        if (VRInput.Ready)
        {
            // 戴頭盔：哪一隻手靠近門板就看那隻手的板機
            Player player = Player.instance;
            if (player == null)
                return false;
            foreach (Hand hand in player.hands)
            {
                if (hand == null || !hand.isActive || hand.currentAttachedObject != null)
                    continue;
                Vector3 p = hand.transform.position;
                if ((leafCollider.ClosestPoint(p) - p).sqrMagnitude > handReach * handReach)
                    continue;
                if (VRInput.GetStateDown(SteamVR_Actions.default_GrabPinch, hand.handType))
                {
                    lastUserPosition = p;
                    return true;
                }
            }
            return false;
        }

        // 免頭盔：滑鼠點門板，或看著門按 E
        Camera cam = Camera.main;
        if (cam == null)
            return false;
        Ray ray;
        if (UnityEngine.Input.GetMouseButtonDown(0))
            ray = cam.ScreenPointToRay(UnityEngine.Input.mousePosition);
        else if (UnityEngine.Input.GetKeyDown(KeyCode.E))
            ray = new Ray(cam.transform.position, cam.transform.forward);
        else
            return false;

        RaycastHit hit;
        if (leafCollider.Raycast(ray, out hit, lookReach))
        {
            lastUserPosition = cam.transform.position;
            return true;
        }
        return false;
    }

    Vector3 lastUserPosition;

    void Toggle()
    {
        isOpen = !isOpen;
        if (!isOpen)
        {
            targetRotation = closedRotation;
            return;
        }

        // 往遠離使用者的方向推開：門板以 +角度旋轉時的移動方向 = up × (轉軸→門板中心)
        Vector3 swingDir = Vector3.Cross(Vector3.up, hingeToCenter);
        Vector3 toUser = Vector3.ProjectOnPlane(lastUserPosition - pivot.position, Vector3.up);
        float sign = Vector3.Dot(swingDir, toUser) > 0f ? -1f : 1f;
        targetRotation = Quaternion.AngleAxis(sign * openAngle, Vector3.up) * closedRotation;
    }
}
