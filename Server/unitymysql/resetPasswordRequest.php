<?php
// 對應 Unity：Assets/Scenes/UI/腳本/ForgotPassword.cs（寄送／重新寄送驗證碼）
// 輸入：email
// 成功時回傳必須「完全等於」Verification email sent successfully.
require_once __DIR__ . '/db.php';

$email = post_value('email');
if (!filter_var($email, FILTER_VALIDATE_EMAIL)) {
    exit('Invalid email.');
}

$conn = db_connect();
$stmt = $conn->prepare('SELECT username FROM users WHERE email = ? LIMIT 1');
$stmt->bind_param('s', $email);
$stmt->execute();
$stmt->bind_result($username);
if (!$stmt->fetch()) {
    exit('Email not found.');
}
$stmt->close();

// 6 位數驗證碼
$code = str_pad((string)random_int(0, 999999), 6, '0', STR_PAD_LEFT);

$stmt = $conn->prepare('UPDATE users SET verification_code = ? WHERE email = ?');
$stmt->bind_param('ss', $code, $email);
$stmt->execute();
$stmt->close();
$conn->close();

$subject = '藥局 VR 訓練 - 重設密碼驗證碼';
$body    = "您好 {$username}：\n\n您的重設密碼驗證碼為：{$code}\n\n如果不是您本人操作，請忽略這封信。";

if (send_mail($email, $subject, $body)) {
    echo 'Verification email sent successfully.';
} elseif (DEBUG_WRITE_CODE_TO_FILE) {
    // 尚未設定寄信：把驗證碼寫進檔案方便測試
    file_put_contents(__DIR__ . '/verification_log.txt', date('Y-m-d H:i:s') . "  {$email}  {$code}\n", FILE_APPEND);
    echo 'Verification email sent successfully.';
} else {
    echo 'Failed to send email.';
}

function send_mail($to, $subject, $body)
{
    $src = __DIR__ . '/PHPMailer/src/';
    if (SMTP_USER === '' || !file_exists($src . 'PHPMailer.php')) {
        return false;
    }
    require_once $src . 'Exception.php';
    require_once $src . 'PHPMailer.php';
    require_once $src . 'SMTP.php';

    $mail = new PHPMailer\PHPMailer\PHPMailer(true);
    try {
        $mail->isSMTP();
        $mail->Host       = SMTP_HOST;
        $mail->Port       = SMTP_PORT;
        $mail->SMTPAuth   = true;
        $mail->SMTPSecure = PHPMailer\PHPMailer\PHPMailer::ENCRYPTION_STARTTLS;
        $mail->Username   = SMTP_USER;
        $mail->Password   = SMTP_PASS;
        $mail->CharSet    = 'UTF-8';
        $mail->setFrom(SMTP_USER, SMTP_FROM_NAME);
        $mail->addAddress($to);
        $mail->Subject = $subject;
        $mail->Body    = $body;
        $mail->send();
        return true;
    } catch (Exception $e) {
        error_log('寄信失敗：' . $mail->ErrorInfo);
        return false;
    }
}
