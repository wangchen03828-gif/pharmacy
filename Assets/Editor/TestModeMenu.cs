using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.XR.Management;
using UnityEngine;

/// <summary>
/// 選單「打包 > 測試模式」：一鍵切換在編輯器裡按 Play 時要不要用頭盔。
///  - 有頭盔 (VR)：移除 NO_HMD、啟動時初始化 XR → 戴頭盔測試。
///  - 免頭盔 (鍵盤滑鼠)：加上 NO_HMD、不初始化 XR → WASD 移動、右鍵轉視角、左鍵抓取、X 關提示視窗。
/// 這兩個設定存在 ProjectSettings 裡，commit 時不要把 ProjectSettings.asset、
/// XRGeneralSettingsPerBuildTarget.asset 的變更推上去，避免影響其他人。
/// 打包時不受影響：「打包」選單會自己設定好再還原。
/// </summary>
public static class TestModeMenu
{
    public const string NoHmdDefine = "NO_HMD";
    const string VRMenu = "打包/測試模式/有頭盔 (VR)";
    const string NoHmdMenu = "打包/測試模式/免頭盔 (鍵盤滑鼠)";

    [MenuItem(VRMenu, false, 100)]
    static void UseVR()
    {
        SetNoHmdDefine(false);
        SetXRInitOnStart(true);
        Debug.Log("[測試模式] 已切換成「有頭盔」：記得先開 SteamVR 並接好頭盔。");
    }

    [MenuItem(NoHmdMenu, false, 101)]
    static void UseNoHmd()
    {
        SetNoHmdDefine(true);
        SetXRInitOnStart(false);
        Debug.Log("[測試模式] 已切換成「免頭盔」：WASD 移動、右鍵轉視角、左鍵抓取、X 關閉提示視窗。");
    }

    [MenuItem(VRMenu, true)]
    static bool UseVRValidate()
    {
        Menu.SetChecked(VRMenu, !HasNoHmdDefine());
        return true;
    }

    [MenuItem(NoHmdMenu, true)]
    static bool UseNoHmdValidate()
    {
        Menu.SetChecked(NoHmdMenu, HasNoHmdDefine());
        return true;
    }

    public static bool HasNoHmdDefine()
    {
        string defines = PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.Standalone);
        return defines.Split(';').Select(d => d.Trim()).Contains(NoHmdDefine);
    }

    public static void SetNoHmdDefine(bool on)
    {
        var list = PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.Standalone)
            .Split(';')
            .Select(d => d.Trim())
            .Where(d => d.Length > 0 && d != NoHmdDefine)
            .ToList();
        if (on)
            list.Add(NoHmdDefine);
        PlayerSettings.SetScriptingDefineSymbols(NamedBuildTarget.Standalone, string.Join(";", list));
    }

    static void SetXRInitOnStart(bool on)
    {
        var xr = XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(BuildTargetGroup.Standalone);
        if (xr == null)
        {
            Debug.LogWarning("[測試模式] 找不到 XR Plug-in Management 的 Standalone 設定。");
            return;
        }
        xr.InitManagerOnStart = on;
        EditorUtility.SetDirty(xr);
        AssetDatabase.SaveAssets();
    }
}
