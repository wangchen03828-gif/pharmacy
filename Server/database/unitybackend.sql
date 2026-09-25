-- 藥局 VR 訓練 - 登入系統資料庫（只有資料表結構，不含任何帳號資料）
-- 使用方式：XAMPP 開啟 MySQL → phpMyAdmin → 匯入 此檔案
-- 資料表欄位與原本的 unitybackend 完全相同

CREATE DATABASE IF NOT EXISTS `unitybackend` DEFAULT CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
USE `unitybackend`;

SET SQL_MODE = "NO_AUTO_VALUE_ON_ZERO";
SET time_zone = "+00:00";

CREATE TABLE IF NOT EXISTS `users` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `username` varchar(255) NOT NULL,
  `salt` varchar(255) NOT NULL,
  `password` varchar(255) NOT NULL,
  `email` varchar(255) NOT NULL,
  `verification_code` varchar(255) DEFAULT NULL,
  PRIMARY KEY (`id`),
  UNIQUE KEY `email` (`email`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COLLATE=utf8_unicode_ci;
