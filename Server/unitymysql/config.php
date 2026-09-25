<?php
// ===== 資料庫設定（XAMPP 預設值）=====
define('DB_HOST', '127.0.0.1');
define('DB_USER', 'root');
define('DB_PASS', '');            // XAMPP 預設 root 沒有密碼
define('DB_NAME', 'unitybackend');

// ===== 忘記密碼寄信設定（Gmail 範例）=====
// 1. 到 https://github.com/PHPMailer/PHPMailer 下載，把裡面的 src 資料夾放到 unitymysql/PHPMailer/src
// 2. Gmail 要先開啟兩步驟驗證，再建立「應用程式密碼」填在 SMTP_PASS（不是 Gmail 登入密碼）
define('SMTP_HOST', 'smtp.gmail.com');
define('SMTP_PORT', 587);
define('SMTP_USER', '');          // 寄件 Gmail，例如 yourname@gmail.com
define('SMTP_PASS', '');          // 16 碼應用程式密碼
define('SMTP_FROM_NAME', '藥局 VR 訓練');

// 測試用：還沒設定寄信時設為 true，驗證碼會寫進 unitymysql/verification_log.txt 方便測試
// 正式使用前請改回 false
define('DEBUG_WRITE_CODE_TO_FILE', true);
