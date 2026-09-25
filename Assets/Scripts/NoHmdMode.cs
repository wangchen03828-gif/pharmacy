using UnityEngine;

/// <summary>
/// 免頭盔版專用：用「打包 > 免頭盔版」打包時會加上 NO_HMD 定義，
/// 遊戲一啟動就關閉 SteamVR，讓 SteamVR Player 自動改用鍵盤滑鼠操作 (2D Fallback)。
/// 要頭盔版與編輯器裡測試時，這段程式不會執行。
/// </summary>
public static class NoHmdMode
{
#if NO_HMD
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void DisableSteamVR()
    {
        Valve.VR.SteamVR.enabled = false;
        Debug.Log("[NoHmdMode] 免頭盔模式：已停用 SteamVR，改用鍵盤滑鼠。");
    }
#endif

    /// <summary>目前是否為免頭盔版（其他腳本需要判斷時可使用）。</summary>
    public static bool IsNoHmd
    {
        get
        {
#if NO_HMD
            return true;
#else
            return false;
#endif
        }
    }
}
