using UnityEngine;

/// <summary>
/// 提示視窗管理，由 StepSkipper 在場景載入時自動掛到每個提示上：
///  1. 只出現一次：已經關掉的提示，不會再被場景裡的觸發重新叫出來。
///     （原作者有些觸發會重複觸發，例如警示牌每碰一次地面觸發點就再跳一次「拿出封鎖線」。）
///     被擋下來的那一幀會先把畫面藏起來，避免一直閃。
///  2. 一次只顯示一個：新提示出現時，把其他還開著的提示收起來，
///     避免兩個提示疊在同一個位置互相搶畫面（看起來會一直閃）。
/// </summary>
public class PopupOnce : MonoBehaviour
{
    public static bool allowReshow;   // 跳步（N/B）時暫時允許重新顯示
    bool seen;
    bool hideNextFrame;
    bool closeOthersNextFrame;

    void OnEnable()
    {
        if (seen && !allowReshow)
        {
            // OnEnable 裡不能直接關掉自己，留到 Update；這一幀先不畫出來
            hideNextFrame = true;
            SetVisible(false);
            return;
        }
        closeOthersNextFrame = true;
    }

    void Update()
    {
        if (hideNextFrame)
        {
            hideNextFrame = false;
            gameObject.SetActive(false);
            SetVisible(true);         // 還原，下次被允許顯示時才看得到
            return;
        }

        if (closeOthersNextFrame)
        {
            closeOthersNextFrame = false;
            if (transform.parent == null)
                return;
            foreach (Transform sibling in transform.parent)
            {
                if (sibling == transform || !sibling.gameObject.activeSelf)
                    continue;
                if (sibling.GetComponent<PopupOnce>() != null)
                    sibling.gameObject.SetActive(false);
            }
        }
    }

    /// <summary>跳步時被順便叫出來又收回去的提示：當作沒看過，之後還能正常出現。</summary>
    public void Forget()
    {
        seen = false;
        hideNextFrame = false;
        SetVisible(true);
    }

    void OnDisable()
    {
        seen = true;
    }

    readonly System.Collections.Generic.List<Behaviour> hiddenCanvases = new System.Collections.Generic.List<Behaviour>();
    readonly System.Collections.Generic.List<Renderer> hiddenRenderers = new System.Collections.Generic.List<Renderer>();

    void SetVisible(bool on)
    {
        if (!on)
        {
            foreach (var r in GetComponentsInChildren<Renderer>(true))
                if (r.enabled) { r.enabled = false; hiddenRenderers.Add(r); }
            foreach (var c in GetComponentsInChildren<Canvas>(true))
                if (c.enabled) { c.enabled = false; hiddenCanvases.Add(c); }
            return;
        }
        // 只還原剛剛被這裡藏起來的，原本就關著的不動
        foreach (var r in hiddenRenderers) if (r != null) r.enabled = true;
        foreach (var c in hiddenCanvases) if (c != null) c.enabled = true;
        hiddenRenderers.Clear();
        hiddenCanvases.Clear();
    }
}
