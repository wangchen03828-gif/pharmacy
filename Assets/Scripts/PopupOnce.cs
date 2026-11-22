using UnityEngine;

/// <summary>
/// 提示視窗「只出現一次」：已經關掉的提示，不會再被場景裡的觸發重新叫出來。
/// （原作者有些觸發會重複觸發，例如警示牌每碰一次地面觸發點就再跳一次「拿出封鎖線」。）
/// 由 StepSkipper 在場景載入時自動掛到每個提示上。
/// </summary>
public class PopupOnce : MonoBehaviour
{
    public static bool allowReshow;   // 跳步（N/B）時暫時允許重新顯示
    bool seen;
    bool hideNextFrame;

    void OnEnable()
    {
        if (seen && !allowReshow)
            hideNextFrame = true;     // OnEnable 裡不能直接關掉自己，留到 Update
    }

    void Update()
    {
        if (hideNextFrame)
        {
            hideNextFrame = false;
            gameObject.SetActive(false);
        }
    }

    /// <summary>跳步時被順便叫出來又收回去的提示：當作沒看過，之後還能正常出現。</summary>
    public void Forget()
    {
        seen = false;
        hideNextFrame = false;
    }

    void OnDisable()
    {
        seen = true;
    }
}
