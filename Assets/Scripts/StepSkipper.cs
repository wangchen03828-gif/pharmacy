using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 測試用「自動完成這一步」：在練習場景按鍵盤 N（或戴頭盔時按右手側鍵），程式會替你把這一步做完
/// （東西飛到腳本要的位置、該出現的物品出現、該穿上的穿上），然後跳出下一個提示。
/// 按 B 只回到上一個提示，不會復原動作。
///
/// 不用掛在場景上，場景載入時自動啟用（只在有「提示語/藥局」的場景生效）。
/// 提示順序 = Hierarchy 裡「提示語 > 藥局」底下子物件的排列順序；目前步驟 = 排在最後、正在顯示的提示。
///
/// 做法：找出「會讓下一個提示出現」的那個腳本（例如垃圾桶、口罩盒、警示牌），
/// 照那個腳本成功時會做的事情去做，再加上一小段飛過去的動畫。
/// 找不到對應腳本的步驟，就只換提示（Console 會寫「只換提示」）。
/// </summary>
public class StepSkipper : MonoBehaviour
{
    public KeyCode nextKey = KeyCode.N;
    public KeyCode backKey = KeyCode.B;
    public float flyTime = 0.5f;

    readonly List<GameObject> steps = new List<GameObject>();
    bool busy;
    static bool nextRequested;

    /// <summary>從別的地方要求跳過這一步（例如右手側鍵，見 VRExtraControls）。</summary>
    public static void RequestNext()
    {
        nextRequested = true;
    }

    // 會「叫出提示」的欄位名稱
    static readonly HashSet<string> ShowFields = new HashSet<string>
        { "tips", "tips1", "tips2", "tips3", "tips2List", "appear", "objectToShow", "newPopupWindow", "popupWindow", "hintDisplay" };

    // ───────── 啟用 ─────────

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Register()
    {
        SceneManager.sceneLoaded += (scene, mode) =>
        {
            Transform root = FindPopupRoot(scene);
            if (root == null)
                return;
            var go = new GameObject("[StepSkipper]");
            go.hideFlags = HideFlags.HideInHierarchy;
            var skipper = go.AddComponent<StepSkipper>();
            foreach (Transform child in root)
            {
                skipper.steps.Add(child.gameObject);
                if (child.GetComponent<PopupOnce>() == null)
                    child.gameObject.AddComponent<PopupOnce>();
            }
            Debug.Log("[跳步] 已啟用：按 N 自動完成這一步、B 回上一個提示（共 " + skipper.steps.Count + " 個提示）");
        };
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

    void Update()
    {
        if (busy)
            return;
        bool requested = nextRequested;
        nextRequested = false;
        if (Input.GetKeyDown(nextKey) || requested)
            StartCoroutine(Next());
        else if (Input.GetKeyDown(backKey))
            Back();
    }

    int CurrentIndex()
    {
        for (int i = steps.Count - 1; i >= 0; i--)
            if (steps[i] != null && steps[i].activeSelf)
                return i;
        return -1;
    }

    void ShowOnly(GameObject popup)
    {
        foreach (var s in steps)
            if (s != null && s != popup && s.activeSelf)
                s.SetActive(false);
        PopupOnce.allowReshow = true;
        popup.SetActive(true);
        PopupOnce.allowReshow = false;
    }

    void Back()
    {
        int cur = CurrentIndex();
        if (cur <= 0)
            return;
        ShowOnly(steps[cur - 1]);
        Debug.Log("[跳步] 回到 " + steps[cur - 1].name);
    }

    // ───────── 自動完成 ─────────

    IEnumerator Next()
    {
        busy = true;
        int cur = CurrentIndex();

        if (cur == steps.Count - 1)
        {
            // 最後一步「清潔手部」：打開洗手槽的水
            foreach (var sink in FindAll("SinkSwitch"))
                SetActive(Get(sink, "waterObject") as GameObject, true);
            Debug.Log("[跳步] 已打開洗手槽的水，流程完成。按左上角紅色 X 結束。");
            busy = false;
            yield break;
        }

        GameObject nextPopup = steps[cur + 1];
        currentTarget = nextPopup;
        var actions = new List<string>();

        // 找出所有「會叫出下一個提示」的腳本；生成物品的先做，DisplayObject 最後做
        var showers = FindShowers(nextPopup);
        showers.Sort((a, b) => Priority(a.comp).CompareTo(Priority(b.comp)));
        foreach (var s in showers)
            yield return StartCoroutine(Complete(s.comp, s.field, actions));

        CleanUpWornLeftovers();
        ShowOnly(nextPopup);
        string what = actions.Count > 0 ? string.Join("、", actions) : "只換提示";
        Debug.Log("[跳步] " + (cur >= 0 ? steps[cur].name : "（無）") + " → " + nextPopup.name + "（" + what + "）");
        busy = false;
    }

    GameObject currentTarget;

    static void HidePopup(GameObject popup)
    {
        if (popup == null || !popup.activeSelf) return;
        popup.SetActive(false);
        var once = popup.GetComponent<PopupOnce>();
        if (once != null) once.Forget();
    }

    /// <summary>已經穿在身上（人形模型上對應物件已顯示）的物品，如果手上那份還留在場景裡，就收掉。</summary>
    static void CleanUpWornLeftovers()
    {
        foreach (var item in FindAll("WearableItem", "gogglesWearableItem"))
        {
            var worn = Get(item, "correspondingWornObject") as GameObject;
            if (worn != null && worn.activeSelf && item.gameObject.activeInHierarchy)
                item.gameObject.SetActive(false);
        }
    }

    struct Shower { public MonoBehaviour comp; public string field; }

    List<Shower> FindShowers(GameObject popup)
    {
        var result = new List<Shower>();
        foreach (var comp in FindObjectsOfType<MonoBehaviour>(true))
        {
            if (comp == null || comp is StepSkipper || comp is PopupOnce)
                continue;
            string type = comp.GetType().Name;
            if (type == "ShowOnTouch")
                continue;
            foreach (var f in comp.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (!ShowFields.Contains(f.Name))
                    continue;
                if (type == "closewindow" && f.Name == "popupWindow")
                    continue; // closewindow 的 popupWindow 是它自己
                if (Contains(f.GetValue(comp), popup))
                    result.Add(new Shower { comp = comp, field = f.Name });
            }
        }
        return result;
    }

    static int Priority(MonoBehaviour comp)
    {
        switch (comp.GetType().Name)
        {
            case "DisplayObject": return 2;
            case "closewindow": return 3;
            default: return 1;
        }
    }

    IEnumerator Complete(MonoBehaviour c, string field, List<string> actions)
    {
        switch (c.GetType().Name)
        {
            case "closewindow":
                break;

            case "VRButtonHandler":
                if (!(bool)(Get(c, "scoreAdded") ?? true))
                {
                    AddScore(Get(c, "scoreManager"), 5);
                    Set(c, "scoreAdded", true);
                }
                Set(c, "popupShown", true);
                actions.Add("碰對講機");
                break;

            case "TrashBinCollision":
            {
                var item = Get(c, "collisionObject") as GameObject;
                var bin = Get(c, "trashBin") as GameObject;
                if (item != null)
                {
                    if (!item.activeInHierarchy)
                        PlaceInFrontOfCamera(item);
                    if (bin != null)
                        yield return Fly(item, bin.transform.position + Vector3.up * 0.4f);
                    item.SetActive(false);
                    AddScore(Get(c, "scoreManager"), 5);
                    actions.Add(item.name + " 丟進垃圾桶");
                }
                break;
            }

            case "MaskController":
                Call(c, "ShowMask");
                yield return WearSpawned(Get(c, "mask") as GameObject, actions);
                break;

            case "SterileGloveController":
                Call(c, "ShowGlove");
                yield return WearSpawned(Get(c, "glove") as GameObject, actions);
                break;

            case "CBRNSuitController":
                Call(c, "ShowSuit");
                yield return WearSpawned(Get(c, "suit") as GameObject, actions);
                break;

            case "GarmentBagController":
            {
                // 夾鏈袋一次可能吐出好幾樣（帽套、手套、鞋套），只留這一步要的，其他收回去等它自己的步驟
                var head = Get(c, "headCover") as GameObject;
                var shoe = Get(c, "shoeCover") as GameObject;
                var gloves = Get(c, "glovesList") as IList;
                var gloveTips = Get(c, "tips2List") as IList;
                GameObject target = null;
                if (field == "tips1") target = head;
                else if (field == "tips3") target = shoe;
                else if (gloveTips != null && gloves != null)
                {
                    int gi = gloveTips.IndexOf(currentTarget);
                    if (gi >= 0 && gi < gloves.Count) target = gloves[gi] as GameObject;
                }

                bool headWas = head != null && head.activeSelf;
                bool shoeWas = shoe != null && shoe.activeSelf;
                var glovesWas = new List<bool>();
                if (gloves != null) foreach (var g in gloves) glovesWas.Add(g is GameObject go && go.activeSelf);

                Call(c, "TryShowAccessories");

                if (head != null && head != target && !headWas && head.activeSelf)
                {
                    head.SetActive(false);
                    Set(c, "headCoverShown", false);
                    HidePopup(Get(c, "tips1") as GameObject);
                }
                if (shoe != null && shoe != target && !shoeWas && shoe.activeSelf)
                {
                    shoe.SetActive(false);
                    Set(c, "shoeCoverShown", false);
                    HidePopup(Get(c, "tips3") as GameObject);
                }
                if (gloves != null && target != null)
                {
                    var shownList = Get(c, "glovesShownList") as List<bool>;
                    for (int i = 0; i < gloves.Count; i++)
                    {
                        if (!(gloves[i] is GameObject g) || g == target || glovesWas[i] || !g.activeSelf)
                            continue;
                        g.SetActive(false);
                        if (shownList != null && i < shownList.Count) shownList[i] = false;
                        if (gloveTips != null && i < gloveTips.Count) HidePopup(gloveTips[i] as GameObject);
                    }
                }

                if (target != null)
                    yield return WearSpawned(target, actions);
                else if (gloves != null)
                {
                    foreach (var g in gloves)
                        if (g is GameObject glove && glove.activeInHierarchy)
                            yield return WearSpawned(glove, actions);
                }
                break;
            }

            case "PharmacyZipBagController2":
            {
                string n = field.Substring(field.Length - 1); // tips1 → 1
                var bag = Get(c, "singleZiplockBag" + n) as GameObject;
                if (bag != null && !bag.activeSelf)
                {
                    Call(c, "ShowSingleZiplockBag", bag);
                    Set(c, "ziplockBag" + n + "Shown", true);
                }
                actions.Add("取出任由袋");
                break;
            }

            case "TissueBoxController":
            {
                string n = field.Substring(field.Length - 1);
                var tissue = Get(c, "singleTissue" + n) as GameObject;
                var box = Get(c, "tissueBox") as GameObject;
                if (tissue != null && !tissue.activeSelf)
                {
                    tissue.SetActive(true);
                    if (box != null)
                        tissue.transform.position = box.transform.position + (Vector3)(Get(c, "tissueOffset") ?? Vector3.up * 0.1f);
                    Set(c, "tissue" + n + "Shown", true);
                }
                actions.Add("取出擦手紙");
                break;
            }

            case "TissueManager":
                if (field == "tips1" && !(bool)(Get(c, "isTouched") ?? true))
                {
                    Set(c, "isTouched", true);
                    Call(c, "DisplayPads");
                    actions.Add("擦手紙吸附");
                }
                else if (field == "tips2" && !(bool)(Get(c, "isRotationStarted") ?? true))
                {
                    Set(c, "isWetEffectComplete", true);
                    Set(c, "isRotationStarted", true);
                    Call(c, "RotateWrinkledTissues");
                    actions.Add("擦手紙收緊成團");
                }
                break;

            case "PharmacyWaterCollision":
                SetActive(Get(c, "parentObject") as GameObject, true);
                Set(c, "isTouched", true);
                actions.Add("倒水到潑灑區");
                break;

            case "CapHidden":
                SetActive(Get(c, "cap") as GameObject, false);
                Set(c, "isCapHidden", true);
                actions.Add("打開瓶蓋");
                break;

            case "WaterLevelControl":
                if (field == "tips1")
                {
                    SetActive(Get(c, "lowWater") as GameObject, true);
                    Set(c, "isLowWaterTriggered", true);
                    Set(c, "firstTriggeredTag", "BleachStream");
                    actions.Add("漂白水倒進罐子");
                }
                else if (field == "tips2")
                {
                    SetActive(Get(c, "highWater") as GameObject, true);
                    Set(c, "isHighWaterTriggered", true);
                    actions.Add("礦泉水倒進罐子");
                }
                else if (field == "tips3")
                {
                    SetActive(Get(c, "lowWater") as GameObject, false);
                    SetActive(Get(c, "highWater") as GameObject, false);
                    SetActive(Get(c, "mixWaterLevel") as GameObject, true);
                    SetActive(Get(c, "cap") as GameObject, true);
                    Set(c, "isCapVisible", true);
                    Set(c, "isShakeComplete", true);
                    actions.Add("蓋上瓶蓋並搖晃");
                }
                break;

            case "CollisionHandler":
                SetActive(Get(c, "collision2") as GameObject, false);
                SetActive(Get(c, "appear") as GameObject, true);
                actions.Add("擦手紙碰任由袋");
                break;

            case "PharmacySignSwitch":
            {
                var sign = Get(c, "foldedSign") as GameObject;
                var spot = Get(c, "collisionObject") as GameObject;
                if (sign != null && spot != null)
                    yield return Fly(sign, spot.transform.position + Vector3.up * 0.3f);
                SetRenderers(sign, false);
                SetActive(Get(c, "unfoldedSign") as GameObject, true);
                actions.Add("警示牌放到門前");
                break;
            }

            case "BarrierControl":
            {
                if ((bool)(Get(c, "hasUnfoldedShown") ?? false))
                    break;
                var folded = Get(c, "foldingBarricade") as GameObject;
                var b1 = Get(c, "button1") as GameObject;
                var b2 = Get(c, "button2") as GameObject;
                if (folded != null && b1 != null && b2 != null)
                    yield return Fly(folded, (b1.transform.position + b2.transform.position) / 2f);
                SetRenderers(folded, false);
                SetActive(Get(c, "unfoldedBarricade") as GameObject, true);
                SetActive(b1, false);
                SetActive(b2, false);
                Set(c, "button1Touched", true);
                Set(c, "button2Touched", true);
                Set(c, "hasUnfoldedShown", true);
                actions.Add("展開封鎖線");
                break;
            }

            case "BroomInteraction":
                SetActive(Get(c, "objectToShow") as GameObject, true);
                SetActive(Get(c, "objectToHide1") as GameObject, false);
                actions.Add("掃起玻璃碎片");
                break;

            case "PharmacyCapHidde":
                if (field == "tips1")
                {
                    SetActive(Get(c, "cap") as GameObject, false);
                    SetActive(Get(c, "hintDisplay") as GameObject, true);
                    Set(c, "isCapHidden", true);
                    actions.Add("打開塑膠容器");
                }
                else if (field == "tips2")
                {
                    SetActive(Get(c, "objectToHideAfterDustpan") as GameObject, false);
                    SetActive(Get(c, "cap") as GameObject, true);
                    Set(c, "dustpanTriggered", true);
                    actions.Add("碎片倒進容器並蓋上");
                }
                break;

            case "AbsorbPadController":
            {
                // 吸附墊放到潑灑藥物上：吸附墊飛到潑灑處，潑灑物消失，吸附墊開始變濕
                var spill = FindWithTag("SpillObject", c.gameObject);
                if (spill != null && spill.activeInHierarchy)
                {
                    yield return Fly(c.gameObject, spill.transform.position + Vector3.up * 0.05f);
                    spill.SetActive(false);
                }
                Call(c, "DisplayPads");
                actions.Add("吸附墊放到潑灑藥物上");
                break;
            }

            case "DisplayObject":
                yield return CompleteDisplayObject(c.gameObject, actions);
                break;
        }
    }

    /// <summary>DisplayObject：它所在的物件一出現就會叫出提示。先處理會讓這個物件出現的腳本，再讓它出現。</summary>
    IEnumerator CompleteDisplayObject(GameObject owner, List<string> actions)
    {
        if (owner.activeInHierarchy)
            yield break;

        // 潑灑箱：空箱子飛到藥櫃，換成裝滿的箱子
        foreach (var box in FindAll("SplitBox"))
        {
            if (Get(box, "fullSprinklerBox") as GameObject != owner)
                continue;
            var empty = Get(box, "emptySprinklerBox") as GameObject;
            var cabinet = FindWithTag("MedicineCabinet", empty);
            if (empty != null && cabinet != null)
                yield return Fly(empty, cabinet.transform.position + Vector3.up * 0.5f);
            SetActive(empty, false);
            owner.SetActive(true);
            foreach (var r in owner.GetComponentsInChildren<Renderer>()) r.enabled = true;
            foreach (var col in owner.GetComponentsInChildren<Collider>()) col.enabled = true;
            actions.Add("潑灑箱放到藥櫃");
        }

        // 潑灑箱蓋子：換成打開的蓋子
        foreach (var lid in FindAll("SprayBoxLidController"))
        {
            if (Get(lid, "displayLid") as GameObject != owner)
                continue;
            SetActive(Get(lid, "hiddenLid") as GameObject, false);
            actions.Add("打開潑灑箱蓋子");
        }

        // 穿戴：手上的物品飛到臉上消失，身上的物件出現
        foreach (var item in FindAll("WearableItem", "gogglesWearableItem"))
        {
            if (Get(item, "correspondingWornObject") as GameObject != owner)
                continue;
            var go = item.gameObject;
            if (go.activeInHierarchy)
                yield return Fly(go, HeadPosition());
            go.SetActive(false);
            actions.Add("穿上" + go.name);
        }

        // 吸附墊：收成團
        foreach (var pad in FindAll("WrinkledPadController"))
        {
            if (Get(pad, "wrinkledPad") as GameObject != owner)
                continue;
            var pad6 = Get(pad, "absorbentPad6") as GameObject;
            if (pad6 != null)
            {
                owner.transform.SetPositionAndRotation(pad6.transform.position, pad6.transform.rotation);
                pad6.SetActive(false);
            }
            owner.transform.SetParent(null);
            actions.Add("吸附墊收成團");
        }

        // 父物件沒顯示的話一起打開，物件才會真的出現
        for (Transform t = owner.transform.parent; t != null; t = t.parent)
            if (!t.gameObject.activeSelf && t.name != "提示語" && !t.name.Contains("隱藏"))
                t.gameObject.SetActive(true);
        owner.SetActive(true);
    }

    /// <summary>剛生成的穿戴物：飛到臉上，換成身上的版本。</summary>
    IEnumerator WearSpawned(GameObject item, List<string> actions)
    {
        if (item == null || !item.activeInHierarchy)
            yield break;
        MonoBehaviour wearable = null;
        foreach (var mb in item.GetComponentsInChildren<MonoBehaviour>(true))
        {
            string n = mb.GetType().Name;
            if (n == "WearableItem" || n == "gogglesWearableItem") { wearable = mb; break; }
        }
        if (wearable == null)
            yield break;
        yield return new WaitForSeconds(0.2f);
        yield return Fly(item, HeadPosition());
        wearable.gameObject.SetActive(false);
        SetActive(Get(wearable, "correspondingWornObject") as GameObject, true);
        actions.Add("穿上" + item.name);
    }

    // ───────── 動畫與工具 ─────────

    IEnumerator Fly(GameObject obj, Vector3 target)
    {
        if (obj == null || !obj.activeInHierarchy)
            yield break;
        var rb = obj.GetComponent<Rigidbody>();
        bool wasKinematic = rb != null && rb.isKinematic;
        if (rb != null) rb.isKinematic = true;
        Vector3 start = obj.transform.position;
        for (float t = 0; t < flyTime; t += Time.deltaTime)
        {
            float k = Mathf.SmoothStep(0, 1, t / flyTime);
            obj.transform.position = Vector3.Lerp(start, target, k) + Vector3.up * Mathf.Sin(k * Mathf.PI) * 0.2f;
            yield return null;
        }
        obj.transform.position = target;
        if (rb != null) { rb.isKinematic = wasKinematic; rb.velocity = Vector3.zero; }
    }

    static Vector3 HeadPosition()
    {
        var cam = Camera.main;
        return cam != null ? cam.transform.position + cam.transform.forward * 0.15f : Vector3.zero;
    }

    static void PlaceInFrontOfCamera(GameObject obj)
    {
        var cam = Camera.main;
        if (cam != null)
            obj.transform.position = cam.transform.position + cam.transform.forward * 0.5f;
        obj.SetActive(true);
    }

    static GameObject FindWithTag(string tag, GameObject near)
    {
        GameObject[] found;
        try { found = GameObject.FindGameObjectsWithTag(tag); }
        catch (UnityException) { return null; }
        GameObject best = null;
        float bestDist = float.MaxValue;
        foreach (var g in found)
        {
            float d = near != null ? (g.transform.position - near.transform.position).sqrMagnitude : 0f;
            if (d < bestDist) { bestDist = d; best = g; }
        }
        return best;
    }

    static List<MonoBehaviour> FindAll(params string[] typeNames)
    {
        var list = new List<MonoBehaviour>();
        foreach (var mb in FindObjectsOfType<MonoBehaviour>(true))
            if (mb != null && Array.IndexOf(typeNames, mb.GetType().Name) >= 0)
                list.Add(mb);
        return list;
    }

    static void AddScore(object scoreManager, int points)
    {
        if (scoreManager is MonoBehaviour sm)
            Call(sm, "AddScore", points);
    }

    static void SetActive(GameObject go, bool on)
    {
        if (go != null) go.SetActive(on);
    }

    static void SetRenderers(GameObject go, bool on)
    {
        if (go == null) return;
        foreach (var r in go.GetComponentsInChildren<Renderer>()) r.enabled = on;
    }

    static bool Contains(object value, GameObject target)
    {
        if (value is GameObject g) return g == target;
        if (value is IEnumerable list && !(value is string))
            foreach (var v in list)
                if (v is GameObject lg && lg == target) return true;
        return false;
    }

    const BindingFlags All = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    static object Get(object obj, string name)
    {
        var f = obj.GetType().GetField(name, All);
        return f != null ? f.GetValue(obj) : null;
    }

    static void Set(object obj, string name, object value)
    {
        var f = obj.GetType().GetField(name, All);
        if (f != null) f.SetValue(obj, value);
    }

    static void Call(MonoBehaviour obj, string name, params object[] args)
    {
        var m = obj.GetType().GetMethod(name, All);
        if (m == null) return;
        object r = m.Invoke(obj, args.Length > 0 ? args : null);
        if (r is IEnumerator routine && obj.isActiveAndEnabled)
            obj.StartCoroutine(routine);
    }
}
