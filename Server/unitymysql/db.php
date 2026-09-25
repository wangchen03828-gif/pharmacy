<?php
require_once __DIR__ . '/config.php';

header('Content-Type: text/plain; charset=utf-8');

function db_connect()
{
    mysqli_report(MYSQLI_REPORT_OFF);
    $conn = @new mysqli(DB_HOST, DB_USER, DB_PASS, DB_NAME);
    if ($conn->connect_error) {
        http_response_code(500);
        exit('資料庫連線失敗：' . $conn->connect_error);
    }
    $conn->set_charset('utf8mb4');
    return $conn;
}

function post_value($key)
{
    return isset($_POST[$key]) ? trim($_POST[$key]) : '';
}

/** 產生新的 salt（32 碼十六進位，與原資料庫格式相同） */
function make_salt()
{
    return bin2hex(random_bytes(16));
}

/** 新密碼一律用 sha256(salt + 密碼) 儲存（64 碼十六進位，與原資料庫格式相同） */
function hash_password($password, $salt)
{
    return hash('sha256', $salt . $password);
}

/**
 * 驗證密碼。舊帳號的雜湊方式無法確定是「salt+密碼」還是「密碼+salt」，
 * 所以兩種都接受，確保原本的帳號仍能登入。
 */
function verify_password($password, $salt, $storedHash)
{
    $candidates = [
        hash('sha256', $salt . $password),
        hash('sha256', $password . $salt),
    ];
    foreach ($candidates as $h) {
        if (hash_equals(strtolower($storedHash), $h)) {
            return true;
        }
    }
    return false;
}
