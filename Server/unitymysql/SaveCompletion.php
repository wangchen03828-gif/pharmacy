<?php
// 對應 Unity：Assets/Scripts/PracticeCompletion.cs
// 練習做完時新增一筆紀錄（同一個人可以做很多次，每次都記）
// 輸入：username、scene、score（可省略）、mode（VR / NoHMD）
// 輸出 JSON：{"ok": true} 或 {"ok": false, "error": "..."}
require_once __DIR__ . '/db.php';
header('Content-Type: application/json; charset=utf-8');

$username = post_value('username');
$scene    = post_value('scene');
$score    = post_value('score');
$mode     = post_value('mode');

if ($username === '' || $scene === '') {
    echo json_encode(['ok' => false, 'error' => '缺少 username 或 scene'], JSON_UNESCAPED_UNICODE);
    exit;
}

$conn = db_connect();

// 只接受資料庫裡真的存在的帳號
$stmt = $conn->prepare('SELECT 1 FROM users WHERE username = ? LIMIT 1');
$stmt->bind_param('s', $username);
$stmt->execute();
$stmt->store_result();
if ($stmt->num_rows === 0) {
    echo json_encode(['ok' => false, 'error' => '帳號不存在'], JSON_UNESCAPED_UNICODE);
    exit;
}
$stmt->close();

$scoreValue = ($score === '') ? null : intval($score);
$stmt = $conn->prepare('INSERT INTO practice_records (username, scene, score, mode) VALUES (?, ?, ?, ?)');
$stmt->bind_param('ssis', $username, $scene, $scoreValue, $mode);
$ok = $stmt->execute();
$error = $stmt->error;
$stmt->close();
$conn->close();

echo json_encode($ok ? ['ok' => true] : ['ok' => false, 'error' => $error], JSON_UNESCAPED_UNICODE);
