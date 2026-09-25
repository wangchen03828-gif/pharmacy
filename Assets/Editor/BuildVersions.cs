using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.XR.Management;
using UnityEngine;

/// <summary>
/// 選單「打包」：一鍵產生「要頭盔」與「免頭盔」兩種執行檔。
///  - 要頭盔：啟動時初始化 XR，直接進 VR。
///  - 免頭盔：不初始化 XR，並加上 NO_HMD 定義，SteamVR Player 會自動切到鍵盤滑鼠 (2D Fallback)。
/// 打包完成後會把 XR 設定還原成「要頭盔」，不影響平常在編輯器裡測試。
/// 輸出位置：專案資料夾/Builds/要頭盔執行檔、Builds/免頭盔執行檔
/// </summary>
public static class BuildVersions
{
    const string ExeName = "藥局.exe";

    [MenuItem("打包/要頭盔版 (VR)")]
    public static void BuildVR()
    {
        Build("要頭盔執行檔", useVR: true);
    }

    [MenuItem("打包/免頭盔版 (鍵盤滑鼠)")]
    public static void BuildNoHmd()
    {
        Build("免頭盔執行檔", useVR: false);
    }

    [MenuItem("打包/兩個版本都打包")]
    public static void BuildBoth()
    {
        if (Build("要頭盔執行檔", useVR: true))
            Build("免頭盔執行檔", useVR: false);
    }

    static bool Build(string folderName, bool useVR)
    {
        string[] scenes = EditorBuildSettings.scenes
            .Where(s => s.enabled)
            .Select(s => s.path)
            .ToArray();

        if (scenes.Length == 0)
        {
            Debug.LogError("[BuildVersions] Build Settings 裡沒有勾選任何場景。");
            return false;
        }

        var xrSettings = XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(BuildTargetGroup.Standalone);
        bool originalInit = xrSettings != null && xrSettings.InitManagerOnStart;

        string outputDir = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Builds", folderName);

        var options = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = Path.Combine(outputDir, ExeName),
            target = BuildTarget.StandaloneWindows64,
            targetGroup = BuildTargetGroup.Standalone,
            options = BuildOptions.None,
            extraScriptingDefines = useVR ? new string[0] : new[] { "NO_HMD" },
        };

        try
        {
            if (xrSettings != null)
            {
                xrSettings.InitManagerOnStart = useVR;
                EditorUtility.SetDirty(xrSettings);
            }

            BuildReport report = BuildPipeline.BuildPlayer(options);
            bool ok = report.summary.result == BuildResult.Succeeded;

            if (ok)
                Debug.Log($"[BuildVersions] {folderName} 打包完成：{outputDir}");
            else
                Debug.LogError($"[BuildVersions] {folderName} 打包失敗：{report.summary.result}");

            return ok;
        }
        finally
        {
            // 還原 XR 設定，避免影響編輯器 Play 測試
            if (xrSettings != null)
            {
                xrSettings.InitManagerOnStart = originalInit;
                EditorUtility.SetDirty(xrSettings);
                AssetDatabase.SaveAssets();
            }
        }
    }
}
