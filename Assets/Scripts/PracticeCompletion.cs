using System.Collections;
using System.Reflection;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 練習完成紀錄（每個帳號各自記在資料庫），不用掛在場景上，自動啟用：
///  - 登入成功時 Login.cs 會把帳號存到 CurrentUser。
///  - 練習場景（有「提示語/藥局」）：走到最後一個提示（清潔手部）並打開洗手槽的水，或把最後一個提示關掉，
///    就算完成 → 跳出「練習完成！請脫下頭盔，並按下X離開此模式」，同時暫停場景裡其他腳本，
///    按 X（鍵盤 X、左手把 X 或螢幕左上角紅色 X）回到選擇模式畫面（換場景時恢復），並把紀錄（帳號、場景、分數、頭盔/免頭盔）寫進資料庫。
///    用 N / 右手側鍵跳到最後也算。
///  - 選擇模式畫面（Practice）：向資料庫查這個帳號做完過哪些練習，按鈕右邊顯示「已完成」。
///
/// 伺服器端：Server/unitymysql/SaveCompletion.php、GetCompletions.php，資料表 practice_records
/// （Server/database/practice_records.sql）。
/// 沒有登入（例如在編輯器直接開練習場景）時一樣會跳完成視窗，但不會寫進資料庫。
/// </summary>
public class PracticeCompletion : MonoBehaviour
{
    public static string CurrentUser;

    const string ServerUrl = "http://localhost/unitymysql/";
    const string MenuScene = "Practice";
    const string FinishMessage = "練習完成！\n請脫下頭盔，並按下X離開此模式";
    const string ExitScene = "Practice";   // 跟左上角「結束」一樣回到選擇模式

    // 選擇模式畫面的按鈕名稱 → 對應的練習場景
    static readonly string[,] ButtonToScene =
    {
        { "液體潑灑", "調配室液體(練習)" },
        { "固體潑灑", "調配室固體(練習)" },
    };

    GameObject lastPopup;
    GameObject finishPopup;
    bool lastSeen;
    bool done;
    bool saving;
    bool waitingForExit;
    string sceneName;
    SinkSwitch[] sinks;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Register()
    {
        SceneManager.sceneLoaded += (scene, mode) =>
        {
            // 上一個模式做完時暫停了遊戲，換到新場景（選擇模式／重新開始模式）一律恢復
            Time.timeScale = 1f;

            if (scene.name == MenuScene)
            {
                var loader = new GameObject("[PracticeCompletion]");
                loader.hideFlags = HideFlags.HideInHierarchy;
                loader.AddComponent<PracticeCompletion>().StartCoroutine(LoadLabels(scene));
                return;
            }

            Transform popups = FindPopupRoot(scene);
            if (popups == null || popups.childCount == 0)
                return;
            var go = new GameObject("[PracticeCompletion]");
            go.hideFlags = HideFlags.HideInHierarchy;
            var tracker = go.AddComponent<PracticeCompletion>();
            tracker.sceneName = scene.name;
            tracker.lastPopup = popups.GetChild(popups.childCount - 1).gameObject;
        };
    }

    // ───────── 練習場景：偵測完成 ─────────

    void Update()
    {
        if (waitingForExit)
        {
            KeepOnlyFinishPopup();
            if (ExitPressed())
            {
                waitingForExit = false;
                StartCoroutine(Exit());
            }
            return;
        }

        if (lastPopup == null || done)
            return;

        if (lastPopup.activeInHierarchy)
        {
            lastSeen = true;
            if (SinkWaterOn())
                Finish();
        }
        else if (lastSeen)
        {
            // 最後一個提示出現過、現在被關掉了
            Finish();
        }
    }

    void Finish()
    {
        done = true;
        Debug.Log("[完成紀錄] " + sceneName + " 完成");
        StartCoroutine(ShowFinishPopup());
        StartCoroutine(Save());
    }

    /// <summary>完成後畫面上只留「練習完成」這個提示，其他提示（不管誰叫出來的）一律收掉。</summary>
    void KeepOnlyFinishPopup()
    {
        if (finishPopup == null || finishPopup.transform.parent == null)
            return;
        foreach (Transform sibling in finishPopup.transform.parent)
            if (sibling.gameObject != finishPopup && sibling.gameObject.activeSelf)
                sibling.gameObject.SetActive(false);
        if (!finishPopup.activeSelf)
            finishPopup.SetActive(true);
    }

    /// <summary>X 鍵：鍵盤 X 或左手把 X。螢幕左上角的紅色 X 也可以退出（那顆是原本的 SceneSwitcher）。</summary>
    static bool ExitPressed()
    {
        if (Input.GetKeyDown(KeyCode.X))
            return true;
        return VRInput.Ready && Valve.VR.SteamVR_Actions.default_XButtonaction.GetStateDown(Valve.VR.SteamVR_Input_Sources.Any);
    }

    IEnumerator Exit()
    {
        // 等資料庫寫完再換場景，最多等 3 秒
        float t = 0f;
        while (saving && t < 3f) { t += Time.unscaledDeltaTime; yield return null; }
        SceneManager.LoadScene(ExitScene);
    }

    bool SinkWaterOn()
    {
        if (sinks == null)
            sinks = FindObjectsOfType<SinkSwitch>(true);
        foreach (var sink in sinks)
            if (sink.waterObject != null && sink.waterObject.activeInHierarchy)
                return true;
        return false;
    }

    /// <summary>複製最後一個提示當作「完成」視窗，外觀一樣。</summary>
    IEnumerator ShowFinishPopup()
    {
        // 等一下再跳，避免關掉上一個提示的那次 X 也把完成視窗一起關掉
        yield return new WaitForSecondsRealtime(0.4f);

        var popup = Instantiate(lastPopup, lastPopup.transform.parent);
        popup.name = "練習完成";
        var once = popup.GetComponent<PopupOnce>();
        if (once != null) DestroyImmediate(once);   // 不受「只出現一次」影響
        popup.SetActive(false);

        // 完成視窗按 X 是退出此模式，不是關掉視窗，所以拿掉 closewindow
        foreach (var mb in popup.GetComponentsInChildren<MonoBehaviour>(true))
            if (mb.GetType().Name == "closewindow")
                DestroyImmediate(mb);

        Text message = null;
        foreach (var t in popup.GetComponentsInChildren<Text>(true))
            if (message == null || t.name == "message") message = t;
        if (message != null)
            message.text = FinishMessage;

        // 其他提示先收起來，只留完成視窗
        foreach (Transform sibling in lastPopup.transform.parent)
            if (sibling.gameObject != popup && sibling.gameObject.activeSelf && sibling.GetComponent<PopupOnce>() != null)
                sibling.gameObject.SetActive(false);

        popup.transform.SetPositionAndRotation(lastPopup.transform.position, lastPopup.transform.rotation);
        finishPopup = popup;
        popup.SetActive(true);
        waitingForExit = true;
        PauseEverythingElse();
    }

    /// <summary>
    /// 做完之後把場景裡其他的腳本都暫停，只留下「按 X 離開」需要的：
    /// 這個腳本、左上角紅色 X（SceneSwitcher）、UI 與 SteamVR 本身（頭盔追蹤不能停，不然會暈）。
    /// 另外把時間暫停（timeScale = 0），物理碰撞也不會再觸發其他腳本。
    /// 離開此模式（換場景）時會自動恢復，見 Register。
    /// </summary>
    void PauseEverythingElse()
    {
        var scene = gameObject.scene;
        int count = 0;
        foreach (var mb in FindObjectsOfType<MonoBehaviour>())
        {
            if (mb == null || mb == this || mb.gameObject.scene != scene || !mb.enabled)
                continue;
            var type = mb.GetType();
            if (!string.IsNullOrEmpty(type.Namespace))
                continue;   // UnityEngine（UI、EventSystem）、Valve（SteamVR）、TMPro 等外掛不動
            if (mb is SceneSwitcher)
                continue;   // 左上角紅色 X
            mb.StopAllCoroutines();
            mb.enabled = false;
            count++;
        }
        Time.timeScale = 0f;
        Debug.Log("[完成紀錄] 已暫停其他 " + count + " 個腳本，按 X 離開此模式");
    }

    IEnumerator Save()
    {
        if (string.IsNullOrEmpty(CurrentUser))
        {
            Debug.LogWarning("[完成紀錄] 沒有登入，這次的完成紀錄不會寫進資料庫。");
            yield break;
        }
        saving = true;

        var form = new WWWForm();
        form.AddField("username", CurrentUser);
        form.AddField("scene", sceneName);
        int? score = ReadScore();
        if (score.HasValue) form.AddField("score", score.Value);
        form.AddField("mode", VRInput.Ready ? "VR" : "NoHMD");

        using (var www = UnityWebRequest.Post(ServerUrl + "SaveCompletion.php", form))
        {
            yield return www.SendWebRequest();
            if (www.result != UnityWebRequest.Result.Success)
                Debug.LogError("[完成紀錄] 寫入資料庫失敗：" + www.error);
            else
                Debug.Log("[完成紀錄] " + CurrentUser + " / " + sceneName + "：" + www.downloadHandler.text);
        }
        saving = false;
    }

    static int? ReadScore()
    {
        var sm = FindObjectOfType<ScoreManager>();
        if (sm == null) return null;
        var f = typeof(ScoreManager).GetField("score", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        return f != null ? (int?)(int)f.GetValue(sm) : null;
    }

    static Transform FindPopupRoot(Scene scene)
    {
        foreach (var rootObj in scene.GetRootGameObjects())
        {
            if (rootObj.name != "提示語")
                continue;
            Transform t = rootObj.transform.Find("藥局");
            if (t != null)
                return t;
        }
        return null;
    }

    // ───────── 選擇模式畫面：顯示已完成 ─────────

    [System.Serializable]
    class CompletionList { public string[] scenes; }

    static IEnumerator LoadLabels(Scene scene)
    {
        if (string.IsNullOrEmpty(CurrentUser))
            yield break;

        var form = new WWWForm();
        form.AddField("username", CurrentUser);
        using (var www = UnityWebRequest.Post(ServerUrl + "GetCompletions.php", form))
        {
            yield return www.SendWebRequest();
            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("[完成紀錄] 讀取資料庫失敗：" + www.error);
                yield break;
            }

            CompletionList list = null;
            try { list = JsonUtility.FromJson<CompletionList>(www.downloadHandler.text); }
            catch { Debug.LogError("[完成紀錄] 伺服器回傳格式不對：" + www.downloadHandler.text); }
            if (list == null || list.scenes == null)
                yield break;

            for (int i = 0; i < ButtonToScene.GetLength(0); i++)
            {
                if (System.Array.IndexOf(list.scenes, ButtonToScene[i, 1]) < 0)
                    continue;
                foreach (var button in FindObjectsOfType<Button>(true))
                {
                    if (button.gameObject.scene != scene || button.name != ButtonToScene[i, 0])
                        continue;
                    AddLabel(button);
                    break;
                }
            }
        }
    }

    static void AddLabel(Button button)
    {
        if (button.transform.Find("已完成") != null)
            return;
        var source = button.GetComponentInChildren<Text>(true);
        var go = new GameObject("已完成", typeof(RectTransform));
        go.transform.SetParent(button.transform, false);

        var rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(1, 0.5f);
        rt.pivot = new Vector2(0, 0.5f);
        rt.anchoredPosition = new Vector2(12, 0);
        rt.sizeDelta = new Vector2(140, 48);

        var text = go.AddComponent<Text>();
        text.text = "已完成";
        text.color = new Color(0.1f, 0.6f, 0.2f);
        text.alignment = TextAnchor.MiddleLeft;
        text.raycastTarget = false;
        if (source != null)
        {
            text.font = source.font;
            text.fontSize = source.fontSize;
            text.fontStyle = FontStyle.Bold;
        }
        else
        {
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 24;
        }
    }
}
