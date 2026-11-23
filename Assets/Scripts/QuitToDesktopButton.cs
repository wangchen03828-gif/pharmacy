using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 在「選擇模式」畫面（Practice 場景）右下角加一顆「退出至桌面」按鈕。
/// 外觀直接複製畫面上「液體潑灑」那顆按鈕，所以風格一致；不用改場景檔。
/// </summary>
public static class QuitToDesktopButton
{
    const string SceneName = "Practice";
    const string TemplateButton = "液體潑灑";
    const string Label = "退出至桌面";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Register()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != SceneName)
            return;

        Button template = null;
        foreach (var b in Object.FindObjectsOfType<Button>(true))
            if (b.gameObject.scene == scene && b.name == TemplateButton) { template = b; break; }
        if (template == null)
        {
            Debug.LogWarning("[退出按鈕] 找不到「" + TemplateButton + "」按鈕，無法建立退出按鈕。");
            return;
        }

        var canvas = template.GetComponentInParent<Canvas>();
        var go = Object.Instantiate(template.gameObject, canvas.transform, false);
        go.name = "退出至桌面";

        // 拿掉複製過來的換場景腳本，避免按了跑去液體練習
        foreach (var mb in go.GetComponents<MonoBehaviour>())
            if (!(mb is Graphic) && !(mb is Selectable))
                Object.DestroyImmediate(mb);

        var button = go.GetComponent<Button>();
        button.onClick = new Button.ButtonClickedEvent();
        button.onClick.AddListener(Quit);

        var text = go.GetComponentInChildren<Text>(true);
        if (text != null)
            text.text = Label;

        // 放在畫面右下角
        var rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(1, 0);
        rt.pivot = new Vector2(1, 0);
        rt.sizeDelta = new Vector2(220, 48);
        rt.anchoredPosition = new Vector2(-30, 30);
        go.transform.SetAsLastSibling();
    }

    static void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
