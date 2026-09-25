<?php
// 對應 Unity：ForgotPassword.cs 的重設密碼
// 輸入：token（驗證碼）、password（新密碼）
// 成功時回傳需包含 Password reset successfully.，其他文字會顯示在錯誤視窗
require_once __DIR__ . '/db.php';

$token    = post_value('token');
$password = isset($_POST['password']) ? $_POST['password'] : '';

if ($password === '') {
    exit('密碼不能為空，請輸入新密碼。');
}
if ($token === '') {
    exit('驗證碼錯誤，請重新申請。');
}

$conn = db_connect();
$salt = make_salt();
$hash = hash_password($password, $salt);

// 重設後清除驗證碼，避免同一組驗證碼被重複使用
$stmt = $conn->prepare('UPDATE users SET salt = ?, password = ?, verification_code = NULL WHERE verification_code = ?');
$stmt->bind_param('sss', $salt, $hash, $token);
$stmt->execute();

echo $stmt->affected_rows > 0 ? 'Password reset successfully.' : '驗證碼錯誤或已使用過，請重新申請。';

$stmt->close();
$conn->close();
