-- =====================================================
-- ORYS - Otel Rezervasyon Yönetim Sistemi
-- MySQL Veritabanı Kurulum Scripti
-- =====================================================

-- Veritabanını oluştur
CREATE DATABASE IF NOT EXISTS `orys_db` 
CHARACTER SET utf8mb4 
COLLATE utf8mb4_unicode_ci;

USE `orys_db`;

-- =====================================================
-- 1. KULLANICILAR TABLOSU
-- =====================================================
CREATE TABLE IF NOT EXISTS `users` (
    `id` INT AUTO_INCREMENT PRIMARY KEY,
    `full_name` VARCHAR(100) NOT NULL,
    `username` VARCHAR(50) NOT NULL UNIQUE,
    `password` VARCHAR(255) NOT NULL,
    `role` ENUM('Admin', 'Resepsiyonist', 'Muhasebe') NOT NULL,
    `email` VARCHAR(100),
    `phone` VARCHAR(20),
    `is_active` TINYINT(1) DEFAULT 1,
    `created_at` DATETIME DEFAULT CURRENT_TIMESTAMP,
    `updated_at` DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- =====================================================
-- 2. ODA TİPLERİ TABLOSU
-- =====================================================
CREATE TABLE IF NOT EXISTS `room_types` (
    `id` INT AUTO_INCREMENT PRIMARY KEY,
    `name` VARCHAR(50) NOT NULL,
    `description` TEXT,
    `base_price` DECIMAL(10,2) NOT NULL DEFAULT 0,
    `capacity` INT NOT NULL DEFAULT 2,
    `created_at` DATETIME DEFAULT CURRENT_TIMESTAMP
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- =====================================================
-- 3. ODALAR TABLOSU
-- =====================================================
CREATE TABLE IF NOT EXISTS `rooms` (
    `id` INT AUTO_INCREMENT PRIMARY KEY,
    `room_number` VARCHAR(10) NOT NULL UNIQUE,
    `room_type_id` INT NOT NULL,
    `floor` INT NOT NULL DEFAULT 1,
    `capacity` INT NOT NULL DEFAULT 2,
    `price_per_night` DECIMAL(10,2) NOT NULL DEFAULT 0,
    `status` ENUM('Available', 'Occupied', 'Reserved', 'Maintenance') DEFAULT 'Available',
    `description` TEXT,
    `created_at` DATETIME DEFAULT CURRENT_TIMESTAMP,
    `updated_at` DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    FOREIGN KEY (`room_type_id`) REFERENCES `room_types`(`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- =====================================================
-- 4. MİSAFİRLER TABLOSU
-- =====================================================
CREATE TABLE IF NOT EXISTS `guests` (
    `id` INT AUTO_INCREMENT PRIMARY KEY,
    `full_name` VARCHAR(100) NOT NULL,
    `tc_no` VARCHAR(11),
    `passport_no` VARCHAR(20),
    `phone` VARCHAR(20),
    `email` VARCHAR(100),
    `nationality` VARCHAR(50) DEFAULT 'Türkiye',
    `address` TEXT,
    `notes` TEXT,
    `created_at` DATETIME DEFAULT CURRENT_TIMESTAMP,
    `updated_at` DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- =====================================================
-- 5. REZERVASYONLAR TABLOSU
-- =====================================================
CREATE TABLE IF NOT EXISTS `reservations` (
    `id` INT AUTO_INCREMENT PRIMARY KEY,
    `guest_id` INT NOT NULL,
    `room_id` INT NOT NULL,
    `check_in_date` DATE NOT NULL,
    `check_out_date` DATE NOT NULL,
    `adults` INT DEFAULT 1,
    `children` INT DEFAULT 0,
    `status` ENUM('Bekliyor', 'Onaylandi', 'GirisYapildi', 'CikisYapildi', 'Iptal') DEFAULT 'Bekliyor',
    `total_price` DECIMAL(10,2) DEFAULT 0,
    `notes` TEXT,
    `created_by` INT,
    `created_at` DATETIME DEFAULT CURRENT_TIMESTAMP,
    `updated_at` DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    FOREIGN KEY (`guest_id`) REFERENCES `guests`(`id`),
    FOREIGN KEY (`room_id`) REFERENCES `rooms`(`id`),
    FOREIGN KEY (`created_by`) REFERENCES `users`(`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- =====================================================
-- 6. ÖDEMELER TABLOSU
-- =====================================================
CREATE TABLE IF NOT EXISTS `payments` (
    `id` INT AUTO_INCREMENT PRIMARY KEY,
    `reservation_id` INT NOT NULL,
    `amount` DECIMAL(10,2) NOT NULL,
    `payment_method` ENUM('Nakit', 'Kredi Karti', 'Havale', 'Diger') NOT NULL DEFAULT 'Nakit',
    `payment_date` DATETIME DEFAULT CURRENT_TIMESTAMP,
    `notes` TEXT,
    `created_by` INT,
    `created_at` DATETIME DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (`reservation_id`) REFERENCES `reservations`(`id`),
    FOREIGN KEY (`created_by`) REFERENCES `users`(`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- =====================================================
-- VARSAYILAN KULLANICILAR
-- NOT: Şifreler uygulama ilk çalıştığında DatabaseHelper tarafından
-- BCrypt ile hashlenerek otomatik oluşturulur.
-- Varsayılan şifreler: admin123, resepsiyon123, muhasebe123
-- Bu script'i tek başına çalıştırırsanız aşağıdaki INSERT'i KULLANMAYIN;
-- bunun yerine uygulamayı çalıştırın (InitializeDatabase otomatik ekler).
-- =====================================================
-- INSERT için yer tutucu (uygulama tarafından yönetilir):
-- INSERT INTO `users` ... => DatabaseHelper.InitializeDatabase() tarafından yapılır

-- =====================================================
-- VARSAYILAN ODA TİPLERİ
-- =====================================================
INSERT IGNORE INTO `room_types` (`id`, `name`, `description`, `base_price`, `capacity`) VALUES
(1, 'Standart', 'Standart tek/çift kişilik oda', 1500.00, 2),
(2, 'Deluxe', 'Geniş deluxe oda, şehir manzarası', 2500.00, 2),
(3, 'Suite', 'Lüks suit oda, oturma odası dahil', 4000.00, 4),
(4, 'Aile', 'Aile odası, geniş alan', 3000.00, 5),
(5, 'Kral Dairesi', 'En lüks oda, 360° manzara', 7000.00, 2);

-- =====================================================
-- VARSAYILAN ODALAR (40 oda, 4 kat)
-- =====================================================
INSERT IGNORE INTO `rooms` (`room_number`, `room_type_id`, `floor`, `capacity`, `price_per_night`, `status`) VALUES
('101', 1, 1, 2, 1500.00, 'Available'),
('102', 1, 1, 2, 1500.00, 'Available'),
('103', 1, 1, 2, 1500.00, 'Available'),
('104', 2, 1, 2, 2500.00, 'Available'),
('105', 1, 1, 2, 1500.00, 'Available'),
('106', 1, 1, 2, 1500.00, 'Available'),
('107', 2, 1, 2, 2500.00, 'Available'),
('108', 1, 1, 2, 1500.00, 'Available'),
('109', 3, 1, 4, 4000.00, 'Available'),
('110', 1, 1, 2, 1500.00, 'Available'),
('201', 1, 2, 2, 1500.00, 'Available'),
('202', 2, 2, 2, 2500.00, 'Available'),
('203', 1, 2, 2, 1500.00, 'Available'),
('204', 1, 2, 2, 1500.00, 'Available'),
('205', 2, 2, 2, 2500.00, 'Available'),
('206', 1, 2, 2, 1500.00, 'Available'),
('207', 1, 2, 2, 1500.00, 'Available'),
('208', 3, 2, 4, 4000.00, 'Available'),
('209', 1, 2, 2, 1500.00, 'Available'),
('210', 2, 2, 2, 2500.00, 'Available'),
('301', 4, 3, 5, 3000.00, 'Available'),
('302', 1, 3, 2, 1500.00, 'Available'),
('303', 2, 3, 2, 2500.00, 'Available'),
('304', 1, 3, 2, 1500.00, 'Available'),
('305', 3, 3, 4, 4000.00, 'Available'),
('306', 1, 3, 2, 1500.00, 'Available'),
('307', 2, 3, 2, 2500.00, 'Available'),
('308', 1, 3, 2, 1500.00, 'Available'),
('309', 1, 3, 2, 1500.00, 'Available'),
('310', 5, 3, 2, 7000.00, 'Available'),
('401', 1, 4, 2, 1500.00, 'Available'),
('402', 2, 4, 2, 2500.00, 'Available'),
('403', 1, 4, 2, 1500.00, 'Available'),
('404', 1, 4, 2, 1500.00, 'Available'),
('405', 3, 4, 4, 4000.00, 'Available'),
('406', 1, 4, 2, 1500.00, 'Available'),
('407', 2, 4, 2, 2500.00, 'Available'),
('408', 4, 4, 5, 3000.00, 'Available'),
('409', 1, 4, 2, 1500.00, 'Available'),
('410', 5, 4, 2, 7000.00, 'Available');

-- =====================================================
-- VARSAYILAN MİSAFİRLER
-- =====================================================
INSERT IGNORE INTO `guests` (`id`, `full_name`, `tc_no`, `phone`, `email`, `nationality`) VALUES
(1, 'Ahmet Yılmaz', '12345678901', '05321234567', 'ahmet@email.com', 'Türkiye'),
(2, 'Sarah Johnson', '', '05559876543', 'sarah@email.com', 'USA'),
(3, 'Maria Yılmaz', '98765432109', '05441112233', 'maria@email.com', 'Türkiye'),
(4, 'Sarah Ullmon', '', '05334445566', 'sullmon@email.com', 'Germany'),
(5, 'Duna Jahyanis', '', '05367778899', 'duna@email.com', 'Greece'),
(6, 'Luna Ikorar', '', '05381234567', 'luna@email.com', 'Italy'),
(7, 'Mehmet Kaya', '11122233344', '05429876543', 'mehmet@email.com', 'Türkiye'),
(8, 'Fatma Demir', '55566677788', '05461112233', 'fatma@email.com', 'Türkiye'),
(9, 'Ali Çelik', '99988877766', '05501234567', 'ali@email.com', 'Türkiye'),
(10, 'Elif Aydın', '33344455566', '05529876543', 'elif@email.com', 'Türkiye');

SELECT '✅ ORYS Otel Veritabanı başarıyla kuruldu!' AS Sonuc;
