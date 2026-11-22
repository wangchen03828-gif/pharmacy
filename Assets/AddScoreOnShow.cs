using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AddScoreOnShow : MonoBehaviour
{
    public ScoreManager scoreManager; // 參考 ScoreManager 腳本
    public int scoreToAdd = 4; // 顯示後要加的分數

    private bool hasScored = false; // 確保每個物件只加一次分

    void Update()
    {
        // 如果物件被顯示且尚未加過分
        if (gameObject.activeSelf && !hasScored)
        {
            if (scoreManager == null)
                scoreManager = FindObjectOfType<ScoreManager>(); // 場景裡沒拉參考時自己找
            if (scoreManager != null)
            {
                scoreManager.AddScore(scoreToAdd); // 加分
                hasScored = true; // 確保只加分一次
                Debug.Log($"{gameObject.name} is now visible. Added {scoreToAdd} points.");
            }
            else
            {
                Debug.LogWarning(gameObject.name + "：找不到 ScoreManager，這個物件不加分。");
                hasScored = true; // 只提醒一次，不要每一幀洗版
            }
        }
    }
}
