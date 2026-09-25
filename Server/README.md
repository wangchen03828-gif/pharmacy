# 登入系統後端（XAMPP）

遊戲的登入、註冊、忘記密碼都會連到 `http://localhost/unitymysql/`，所以**執行遊戲的那台電腦**要先架好這個後端。

## 安裝步驟

1. 安裝 XAMPP，開啟控制台，啟動 **Apache** 和 **MySQL**。
2. 開啟 http://localhost/phpmyadmin → 上方「匯入」→ 選 `database/unitybackend.sql` → 執行。
   - 會建立 `unitybackend` 資料庫和 `users` 資料表（空的，沒有任何帳號）。
   - 如果要沿用舊帳號，改匯入原本的 `unitybackend (1).sql`（這份含 email 和密碼雜湊，**不要放進 git**）。
3. 把整個 `unitymysql` 資料夾複製到 `C:\xampp\htdocs\`，變成 `C:\xampp\htdocs\unitymysql\Login.php`。
4. 開遊戲測試註冊和登入。

## 忘記密碼寄信（選用）

- 還沒設定寄信時，`config.php` 裡 `DEBUG_WRITE_CODE_TO_FILE = true`，驗證碼會寫進 `unitymysql/verification_log.txt`，打開就能看到。
- 要真的寄信：
  1. 到 https://github.com/PHPMailer/PHPMailer 下載，把 `src` 資料夾放到 `unitymysql/PHPMailer/src`。
  2. 寄件 Gmail 開啟兩步驟驗證，建立「應用程式密碼」。
  3. 在 `config.php` 填入 `SMTP_USER`（Gmail）和 `SMTP_PASS`（應用程式密碼），並把 `DEBUG_WRITE_CODE_TO_FILE` 改成 `false`。

## 檔案對照

| PHP | 對應 Unity 腳本 | 用途 |
|---|---|---|
| Login.php | 資料庫/Login.cs | 登入，回傳 JSON `loginResult` |
| RegisterUser.php | 資料庫/Register.cs | 註冊 |
| resetPasswordRequest.php | UI/腳本/ForgotPassword.cs | 寄送 6 位數驗證碼 |
| verifyCode.php | ForgotPassword.cs | 檢查驗證碼 |
| resetPassword.php | ForgotPassword.cs | 設定新密碼 |
| config.php / db.php | — | 資料庫與寄信設定、共用函式 |

密碼以 `sha256(salt + 密碼)` 儲存，格式和原本資料庫相同；登入時也接受 `sha256(密碼 + salt)`，讓舊帳號能繼續使用。
