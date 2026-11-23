-- 練習完成紀錄（每個帳號各自的紀錄）
-- 已經有 unitybackend 資料庫的話，在 phpMyAdmin 選 unitybackend → 匯入 這個檔案即可，不會動到 users。
USE `unitybackend`;

CREATE TABLE IF NOT EXISTS `practice_records` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `username` varchar(255) NOT NULL,
  `scene` varchar(255) NOT NULL,
  `score` int(11) DEFAULT NULL,
  `mode` varchar(16) NOT NULL DEFAULT '',
  `completed_at` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  KEY `username_scene` (`username`, `scene`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
