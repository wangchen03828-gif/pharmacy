<?php
// 對應 Unity：Assets/Scripts/PracticeCompletion.cs
// 查詢某個帳號做完過哪些練習
// 輸入：username
// 輸出 JSON：{"scenes": ["調配室液體(練習)", ...]}
require_once __DIR__ . '/db.php';
header('Content-Type: application/json; charset=utf-8');

$username = post_value('username');
$scenes = [];

if ($username !== '') {
    $conn = db_connect();
    $stmt = $conn->prepare('SELECT DISTINCT scene FROM practice_records WHERE username = ?');
    $stmt->bind_param('s', $username);
    $stmt->execute();
    $stmt->bind_result($scene);
    while ($stmt->fetch()) {
        $scenes[] = $scene;
    }
    $stmt->close();
    $conn->close();
}

echo json_encode(['scenes' => $scenes], JSON_UNESCAPED_UNICODE);
