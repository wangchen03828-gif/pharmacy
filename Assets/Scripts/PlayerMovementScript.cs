using UnityEngine;
using Valve.VR;
using Valve.VR.InteractionSystem;

/// <summary>
/// VR 搖桿移動 + 轉向 + 固定視角高度。掛在 SteamVR 的 Player 物件上。
///
/// 左手搖桿：前後左右移動，方向以「視角的水平方向」為準（低頭、抬頭不會往地下鑽或飛起來）。
/// 右手搖桿：左右扳轉向。預設每扳一次轉 30 度（Snap，比較不會暈）；可改成 Smooth 連續轉。
///
/// 高度（heightMode）：
///  - CalibrateOnStart：進場景時量一次，把攝影機調到「地板 + eyeHeight」。
///    之後蹲下、彎腰視角會跟著降低，可以撿地上的東西、擦地板。
///    左手 Y 鍵（或鍵盤 R）可以重新校正。
///  - LockEveryFrame（預設）：每一幀都把攝影機鎖在「地板 + eyeHeight」，完全固定。
///    注意：鎖住後蹲下視角不會變，但手會跟著身體往下，碰不到地面的東西。
///  - Off：不調整，用頭盔量到的真實高度。
/// 左手側鍵（Grip）按住可以蹲下，見 VRExtraControls。
/// </summary>
public class PlayerMovementScript : MonoBehaviour
{
    public enum HeightMode { Off, CalibrateOnStart, LockEveryFrame }
    public enum TurnMode { Off, Snap, Smooth }

    [Header("移動")]
    public SteamVR_Action_Vector2 input;       // 搖桿
    public float speed = 1f;                   // 移動速度（公尺/秒）
    [Tooltip("搖桿推不到這個量就不移動，避免手把漂移")]
    public float deadZone = 0.15f;

    [Header("轉向（右手搖桿）")]
    public TurnMode turnMode = TurnMode.Snap;
    [Tooltip("Snap 模式每次轉幾度")]
    public float snapAngle = 30f;
    [Tooltip("Smooth 模式每秒轉幾度")]
    public float smoothTurnSpeed = 90f;

    [Header("高度")]
    public HeightMode heightMode = HeightMode.LockEveryFrame;
    [Tooltip("眼睛離地板的高度（公尺）")]
    public float eyeHeight = 1.6f;
    [Tooltip("勾選後用下面的 floorY 當地板高度；不勾則進場景時自動往下偵測地板")]
    public bool useManualFloorY = false;
    public float floorY = 0f;
    [Tooltip("重新校正高度的按鍵（左手 Y）")]
    public SteamVR_Action_Boolean recalibrateAction;

    [Header("舊版欄位（已不使用，保留讓場景設定不會遺失）")]
    public SteamVR_Action_Boolean AButtonAction;
    public SteamVR_Action_Boolean BButtonAction;
    public float verticalSpeed;

    bool calibrated;
    float waitStartTime = -1f;

    void Start()
    {
        // 場景裡沒指定時，預設用左手 Y 鍵重新校正高度
        if (recalibrateAction == null)
            recalibrateAction = SteamVR_Actions.default_Ybuttonaction;
    }

    void Update()
    {
        // 沒接頭盔（編輯器測試或免頭盔版）時 SteamVR 沒有啟動，交給鍵盤滑鼠模式控制
        if (!VRInput.Ready || Player.instance == null)
            return;

        Transform head = Player.instance.hmdTransform;
        if (head == null)
            return;

        Move(head);
        Turn(head);
        UpdateHeight(head);
    }

    void Move(Transform head)
    {
        if (input == null)
            return;

        Vector2 axis = input.GetAxis(SteamVR_Input_Sources.LeftHand);
        if (axis.magnitude < deadZone)
            return;

        // 視角的水平前方與右方
        Vector3 forward = Vector3.ProjectOnPlane(head.forward, Vector3.up);
        Vector3 right = Vector3.ProjectOnPlane(head.right, Vector3.up);
        if (forward.sqrMagnitude < 0.0001f)   // 正上方/正下方看時用頭頂方向當前方
            forward = Vector3.ProjectOnPlane(head.up, Vector3.up);
        forward.Normalize();
        right.Normalize();

        Vector3 move = forward * axis.y + right * axis.x;
        transform.position += move * speed * Time.deltaTime;
    }

    bool snapArmed = true;

    void Turn(Transform head)
    {
        if (input == null || turnMode == TurnMode.Off)
            return;

        float x = input.GetAxis(SteamVR_Input_Sources.RightHand).x;

        if (turnMode == TurnMode.Smooth)
        {
            if (Mathf.Abs(x) < deadZone)
                return;
            // 以頭為中心轉，轉的時候人不會被甩出去
            transform.RotateAround(head.position, Vector3.up, x * smoothTurnSpeed * Time.deltaTime);
            return;
        }

        // Snap：扳過 0.6 轉一次，要回到中間（小於 0.3）才能再轉下一次
        if (snapArmed && Mathf.Abs(x) > 0.6f)
        {
            transform.RotateAround(head.position, Vector3.up, Mathf.Sign(x) * snapAngle);
            snapArmed = false;
        }
        else if (Mathf.Abs(x) < 0.3f)
        {
            snapArmed = true;
        }
    }

    void UpdateHeight(Transform head)
    {
        if (heightMode == HeightMode.Off)
            return;

        if (heightMode == HeightMode.CalibrateOnStart)
        {
            bool pressed = VRInput.GetStateDown(recalibrateAction, SteamVR_Input_Sources.LeftHand, KeyCode.R);
            if (pressed)
                calibrated = false;
            if (calibrated)
                return;

            // 剛進場景時頭盔位置可能還沒更新，等半秒再量
            if (waitStartTime < 0f)
                waitStartTime = Time.time;
            if (!pressed && Time.time - waitStartTime < 0.5f)
                return;
        }

        float floor = useManualFloorY ? floorY : DetectFloor(head);
        // LockEveryFrame 時左手側鍵可以蹲下（VRExtraControls）
        float crouch = heightMode == HeightMode.LockEveryFrame ? VRExtraControls.CrouchOffset : 0f;
        float offset = (floor + eyeHeight - crouch) - head.position.y;
        transform.position += Vector3.up * offset;
        calibrated = true;
    }

    float detectedFloor;
    bool floorDetected;

    float DetectFloor(Transform head)
    {
        if (floorDetected)
            return detectedFloor;

        // 從頭的位置往下打射線，找第一個不屬於玩家自己的碰撞體當地板
        detectedFloor = transform.position.y; // 找不到時退回 Player 原點高度
        // 從頭上方 1.5 公尺往下打（頭剛好在地板以下時也找得到），略過天花板等高於頭部 1 公尺的東西
        RaycastHit[] hits = Physics.RaycastAll(head.position + Vector3.up * 1.5f, Vector3.down, 8f,
                                               ~0, QueryTriggerInteraction.Ignore);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        foreach (var hit in hits)
        {
            if (hit.point.y > head.position.y + 1f)
                continue;
            if (hit.collider.transform.IsChildOf(transform))
                continue;
            if (hit.collider.attachedRigidbody != null && !hit.collider.attachedRigidbody.isKinematic)
                continue; // 跳過可以拿起來的東西
            detectedFloor = hit.point.y;
            break;
        }
        floorDetected = true;
        Debug.Log("[PlayerMovement] 地板高度 = " + detectedFloor.ToString("F3") + "，眼睛高度設為 " + eyeHeight + " 公尺");
        return detectedFloor;
    }
}
