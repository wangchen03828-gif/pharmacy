using UnityEngine;
using UnityEngine.SceneManagement;
using Valve.VR;
using Valve.VR.InteractionSystem;

/// <summary>
/// 手把側鍵（Grip）的額外功能，場景裡有 SteamVR Player 就自動啟用，不用手動掛：
///  - 左手側鍵按住：蹲下（視角往下降 crouchDepth 公尺），放開站起來。
///  - 右手側鍵按一下：跳過這一步（等於鍵盤 N）。
///
/// 側鍵本來也是「抓東西」的鍵，所以只有在「手沒有碰到可抓的物品、手上也沒拿東西」時才算數；
/// 手靠近物品按側鍵還是抓取，不會誤觸蹲下或跳步。
///
/// 免頭盔（鍵盤滑鼠）模式：按住 C 蹲下；跳步照舊按 N。
/// </summary>
public class VRExtraControls : MonoBehaviour
{
    /// <summary>目前蹲下的深度（公尺），PlayerMovementScript / FallbackHeightLock 會把眼睛高度扣掉這個值。</summary>
    public static float CrouchOffset { get; private set; }

    public static float crouchDepth = 0.6f;   // 蹲下時視角下降多少
    public static float crouchSpeed = 3f;     // 每秒下降／上升幾公尺

    bool leftCrouching;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Register()
    {
        SceneManager.sceneLoaded += (scene, mode) =>
        {
            CrouchOffset = 0f;
            if (FindObjectOfType<VRExtraControls>() != null)
                return;
            if (FindObjectOfType<Player>() == null)
                return;
            var go = new GameObject("[VRExtraControls]");
            go.hideFlags = HideFlags.HideInHierarchy;
            go.AddComponent<VRExtraControls>();
        };
    }

    void Update()
    {
        bool crouch;
        if (VRInput.Ready)
        {
            var grip = SteamVR_Actions.default_GrabGrip;
            Player player = Player.instance;

            // 左手側鍵：按下的瞬間手是空的才開始蹲，之後按住都算
            if (grip.GetStateDown(SteamVR_Input_Sources.LeftHand) && HandIsFree(player != null ? player.leftHand : null))
                leftCrouching = true;
            if (!grip.GetState(SteamVR_Input_Sources.LeftHand))
                leftCrouching = false;
            crouch = leftCrouching;

            // 右手側鍵：跳過這一步
            if (grip.GetStateDown(SteamVR_Input_Sources.RightHand) && HandIsFree(player != null ? player.rightHand : null))
                StepSkipper.RequestNext();
        }
        else
        {
            crouch = Input.GetKey(KeyCode.C);
        }

        float target = crouch ? crouchDepth : 0f;
        CrouchOffset = Mathf.MoveTowards(CrouchOffset, target, crouchSpeed * Time.deltaTime);
    }

    static bool HandIsFree(Hand hand)
    {
        if (hand == null)
            return true;
        if (hand.hoveringInteractable != null)
            return false;
        if (hand.currentAttachedObject != null)
            return false;
        return true;
    }
}
