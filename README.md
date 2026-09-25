# 調配室化療藥品潑灑處理 VR 訓練（交接說明）

大學專題作品。使用 VR 讓使用者練習在調配室處理**固體潑灑**（藥瓶破碎）與**液體潑灑**的標準流程，並有登入系統記錄使用者。

> 原作者已畢業，本文件是給接手學弟妹與醫院端的說明。專案內名稱帶「藥局」的舊東西（例如 `Assets/Scenes/藥局` 光照資料夾、執行檔名稱 `藥局.exe`）是歷史命名，內容都是調配室。

---

## 1. 取得專案（一定要照做，否則會有一堆 missing）

1. 安裝 **Git** 與 **Git LFS**（https://git-lfs.com），安裝完執行一次 `git lfs install`。
2. 用 git clone 下載，**不要用 GitHub 的「Download ZIP」**：ZIP 不含圖片、音效等大型檔案（約 850MB，存在 Git LFS），打開後材質會變粉紅色、貼圖會 missing。
3. 如果已經 clone 但圖片是壞的，在專案資料夾執行：
   ```
   git lfs pull
   ```
4. 用 **Unity Hub** 安裝 **Unity 2022.3.13f1**（版本要一致），再從 Hub 開啟專案。第一次開啟需要 10～30 分鐘建立 Library。

> 注意：Git LFS 的下載流量是算在原始專案擁有者的帳號，每月有限額。超過時 `git lfs pull` 會失敗、只拿到空殼檔。建議接手後在自己的 GitHub 帳號建立新專案，把完整內容（含 LFS）推上去，之後都用自己的。

## 2. 開發環境

| 項目 | 版本／說明 |
|---|---|
| Unity | 2022.3.13f1，URP |
| VR | SteamVR Plugin 2.7.3 + OpenVR XR Plugin 1.1.4（檔案在 `Assets/SteamVR/OpenVRUnityXRPackage/Editor/`） |
| 頭盔 | 用 SteamVR 支援的頭盔（例如 HTC VIVE），電腦要先裝 Steam 與 SteamVR |
| 登入後端 | XAMPP（Apache + MySQL），見 `Server/README.md` |

## 3. 場景與流程

```
loginmenu（登入／註冊）──登入成功──▶ Practice（選擇練習）
   └─ 忘記密碼 ▶ FindAccount          ├─▶ ConnectVR  ─▶ 調配室固體(練習)
                                       └─▶ ConnectVR1 ─▶ 調配室液體(練習)
```

| 場景 | 位置 | 說明 |
|---|---|---|
| loginmenu | Scenes/UI/場景 | 登入、註冊 |
| FindAccount | Scenes/UI/場景 | 忘記密碼（寄驗證碼） |
| Practice | Scenes/UI/場景 | 選擇固體或液體練習 |
| ConnectVR / ConnectVR1 | Scenes/UI/場景 | 提示戴上頭盔，再進入練習場景 |
| 調配室固體(練習) | Scenes | 固體潑灑：畚箕、玻璃碎片罐、硬質塑膠容器 |
| 調配室液體(練習) | Scenes | 液體潑灑：吸附墊 |
| 調配室(測驗) | Scenes | **未完成、未放入打包**。舊版測驗模式，固體液體混在一起，有數個預製物件已遺失，只供參考 |

切換場景是用 `SceneSwitcher` 元件的 `targetSceneName` 欄位（填場景檔名），改場景名稱時要一起改。

主要腳本：
- `Assets/Scripts/調配室腳本/`、`Assets/Scripts/藥局腳本/`：兩個練習場景的互動（穿脫防護裝備、潑灑處理步驟）
- `Assets/Scripts/資料庫/`：登入、註冊、切換場景
- `Assets/Scenes/UI/腳本/ForgotPassword.cs`：忘記密碼

## 4. 打包執行檔

Unity 上方選單 **打包**：

| 選單 | 用途 | 輸出 |
|---|---|---|
| 要頭盔版 (VR) | 正式使用，啟動就進 VR | `Builds/要頭盔執行檔/藥局.exe` |
| 免頭盔版 (鍵盤滑鼠) | 沒有頭盔時展示或測試，SteamVR 會切成 2D 模式 | `Builds/免頭盔執行檔/藥局.exe` |
| 兩個版本都打包 | | |

打包的場景以 File → Build Settings 裡勾選的為準，loginmenu 必須是第一個。

## 5. 醫院端部署

在要執行的電腦上：
1. 安裝 Steam、SteamVR，接好頭盔並確認 SteamVR 狀態為綠燈。
2. 安裝 XAMPP，照 `Server/README.md` 匯入資料庫、放好 PHP 檔，啟動 Apache 和 MySQL。
3. 把整個 `要頭盔執行檔` 資料夾複製過去（`藥局_Data` 資料夾要一起，不能只複製 .exe），執行 `藥局.exe`。

登入程式連的是 `http://localhost/unitymysql/`，所以**每台執行遊戲的電腦都要有自己的 XAMPP**。如果要多台共用同一個帳號資料庫，需把 `Login.cs`、`Register.cs`、`ForgotPassword.cs` 裡的 `localhost` 改成伺服器 IP 後重新打包。

## 6. 已知問題（待接手處理）

| 問題 | 影響 | 建議 |
|---|---|---|
| 兩個練習場景中「門」物件的 Animator Controller 遺失（從未上傳到 git） | 點門不會開關，Console 可能出現警告 | 參考 `Assets/Animations/door1.controller` 重做一個有 `isOpen` 參數的 Controller 指定給「門」，或移除 `DoorOpen` 元件 |
| `Assets/Scripts/PlayMovementScript.cs` 檔名與類別名稱 `PlayerMovementScript` 不一致 | Unity 找不到此腳本，VR 搖桿移動沒有作用 | 把檔案改名為 `PlayerMovementScript.cs`（連同 .meta），再到場景調整移動速度 |
| 調配室(測驗) 有遺失的預製物件與腳本 | 該場景無法正常使用 | 需要時再重做 |
| 登入寄信需要 Gmail 應用程式密碼 | 未設定時驗證碼只會寫進文字檔 | 見 `Server/README.md` |

## 7. 注意事項

- 原本資料庫匯出檔（含使用者 email 與密碼雜湊）**不要上傳到 GitHub**，`Server/database/unitybackend.sql` 是去除個資後的版本。
- 開發時不要直接在檔案總管裡搬移或改名 Unity 資源，要在 Unity 的 Project 視窗裡操作，否則 .meta 會對不上，造成 missing。
