<?php
// 對應 Unity：ForgotPassword.cs 的驗證碼檢查
// 輸入：token（使用者輸入的驗證碼）
// 成功時回傳必須「完全等於」Code verified successfully.
require_once __DIR__ . '/db.php';

$token = post_value('token');
if ($token === '') {
    exit('Invalid code.');
}

$conn = db_connect();
$stmt = $conn->prepare('SELECT 1 FROM users WHERE verification_code = ? LIMIT 1');
$stmt->bind_param('s', $token);
$stmt->execute();
$stmt->store_result();

echo $stmt->num_rows > 0 ? 'Code verified successfully.' : 'Invalid code.';

$stmt->close();
$conn->close();
