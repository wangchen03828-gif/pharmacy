<?php
// 對應 Unity：Assets/Scripts/資料庫/Login.cs
// 輸入：loginUser、loginPass
// 輸出 JSON：{"loginResult": "登入成功" | "帳號或密碼錯誤" | "使用者名稱不存在"}
require_once __DIR__ . '/db.php';
header('Content-Type: application/json; charset=utf-8');

function respond($result)
{
    // storedPassword 欄位保留給 Unity 的 LoginResponse 類別，但不回傳真正的雜湊值
    echo json_encode(['storedPassword' => '', 'loginResult' => $result], JSON_UNESCAPED_UNICODE);
    exit;
}

$username = post_value('loginUser');
$password = isset($_POST['loginPass']) ? $_POST['loginPass'] : '';

if ($username === '') {
    respond('使用者名稱不存在');
}

$conn = db_connect();
$stmt = $conn->prepare('SELECT salt, password FROM users WHERE username = ? LIMIT 1');
$stmt->bind_param('s', $username);
$stmt->execute();
$stmt->bind_result($salt, $storedHash);

if (!$stmt->fetch()) {
    respond('使用者名稱不存在');
}
$stmt->close();
$conn->close();

respond(verify_password($password, $salt, $storedHash) ? '登入成功' : '帳號或密碼錯誤');
