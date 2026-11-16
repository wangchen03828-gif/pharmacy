using UnityEngine;
using Valve.VR;

/// <summary>
/// 安全讀取 SteamVR 手把按鍵。
///  - 有頭盔（SteamVR 正常啟動）：跟直接呼叫 action.GetStateDown(...) 完全一樣。
///  - 沒頭盔（編輯器測試、免頭盔版）：SteamVR 沒啟動，直接讀手把會 NullReferenceException，
///    這裡改成讀鍵盤／滑鼠替代鍵，沒指定替代鍵就回傳 false。
///
/// 用法：把 grabAction.GetStateDown(hand) 改成 VRInput.GetStateDown(grabAction, hand, KeyCode.Mouse0)
///
/// 免頭盔時的替代鍵：
///  - 關閉提示視窗（手把 X 鍵）→ 鍵盤 X
///  - 抓取／板機 → 滑鼠左鍵（左右手都算，所以需要雙手的步驟在免頭盔模式也能觸發）
/// </summary>
public static class VRInput
{
    /// <summary>SteamVR 是否已經成功啟動、可以讀手把。</summary>
    public static bool Ready
    {
        get { return SteamVR.initializedState == SteamVR.InitializedStates.InitializeSuccess; }
    }

    public static bool GetStateDown(SteamVR_Action_Boolean action, SteamVR_Input_Sources source, KeyCode fallbackKey = KeyCode.None)
    {
        if (Ready && action != null)
            return action.GetStateDown(source);
        return fallbackKey != KeyCode.None && UnityEngine.Input.GetKeyDown(fallbackKey);
    }

    public static bool GetStateUp(SteamVR_Action_Boolean action, SteamVR_Input_Sources source, KeyCode fallbackKey = KeyCode.None)
    {
        if (Ready && action != null)
            return action.GetStateUp(source);
        return fallbackKey != KeyCode.None && UnityEngine.Input.GetKeyUp(fallbackKey);
    }

    public static bool GetState(SteamVR_Action_Boolean action, SteamVR_Input_Sources source, KeyCode fallbackKey = KeyCode.None)
    {
        if (Ready && action != null)
            return action.GetState(source);
        return fallbackKey != KeyCode.None && UnityEngine.Input.GetKey(fallbackKey);
    }
}
