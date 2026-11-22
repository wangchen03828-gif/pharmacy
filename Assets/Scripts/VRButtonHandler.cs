using UnityEngine;
using Valve.VR.InteractionSystem;

public class VRButtonHandler : MonoBehaviour
{
    public GameObject popupWindow; // 拖入你的提示窗口
    public ScoreManager scoreManager; // 拖入 ScoreManager 物件

    private bool scoreAdded = false; // 追蹤是否已經加過分數
    private bool popupShown = false; // 提示只跳一次：之後手再經過對講機不會把流程拉回這一步

    // 當控制器的手碰到按鈕的時候
    private void OnHandHoverBegin(Hand hand)
    {
        // 顯示提示窗口（只顯示一次；沒指定提示的對講機直接略過）
        if (popupShown || popupWindow == null)
            return;
        popupWindow.SetActive(true);
        popupShown = true;

        // 如果還沒加過分數，加分
        if (!scoreAdded && scoreManager != null)
        {
            scoreManager.AddScore(5);
            scoreAdded = true; // 記錄已經加過分數
        }
        else if (scoreManager == null)
        {
            Debug.LogError("ScoreManager is not assigned in VRButtonHandler.");
        }
    }

    private void OnHandHoverEnd(Hand hand)
    {
        // 隱藏提示窗口 (如果需要)
        // popupWindow.SetActive(false);
    }
}
