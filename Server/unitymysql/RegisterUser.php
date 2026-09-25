<?php
// 對應 Unity：Assets/Scripts/資料庫/Register.cs
// 輸入：registerUser、registerPass、registerEmail
// 成功時回傳文字需包含「使用者已成功創建」，其他文字會直接顯示在錯誤視窗
require_once __DIR__ . '/db.php';

$username = post_value('registerUser');
$password = isset($_POST['registerPass']) ? $_POST['registerPass'] : '';
$email    = post_value('registerEmail');

if ($username === '') {
    exit('使用者名稱不能為空，請輸入使用者名稱。');
}
if ($password === '') {
    exit('密碼不能為空，請輸入密碼。');
}
if (!filter_var($email, FILTER_VALIDATE_EMAIL)) {
    exit('請輸入有效的電子郵件。');
}

$conn = db_connect();

$stmt = $conn->prepare('SELECT 1 FROM users WHERE username = ? LIMIT 1');
$stmt->bind_param('s', $username);
$stmt->execute();
$stmt->store_result();
if ($stmt->num_rows > 0) {
    exit('使用者名稱已存在，請換一個。');
}
$stmt->close();

$stmt = $conn->prepare('SELECT 1 FROM users WHERE email = ? LIMIT 1');
$stmt->bind_param('s', $email);
$stmt->execute();
$stmt->store_result();
if ($stmt->num_rows > 0) {
    exit('此電子郵件已被註冊。');
}
$stmt->close();

$salt = make_salt();
$hash = hash_password($password, $salt);

$stmt = $conn->prepare('INSERT INTO users (username, salt, password, email) VALUES (?, ?, ?, ?)');
$stmt->bind_param('ssss', $username, $salt, $hash, $email);

if ($stmt->execute()) {
    echo '使用者已成功創建';
} else {
    echo '註冊失敗，請稍後再試。';
}
$stmt->close();
$conn->close();
