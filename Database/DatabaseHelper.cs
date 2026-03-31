using System;
using System.IO;
using System.Text.Json;
using MySql.Data.MySqlClient;

namespace ORYS.Database
{
    /// <summary>
    /// MySQL veritabanı bağlantı yöneticisi - Otel Yönetim Sistemi
    /// </summary>
    public static class DatabaseHelper
    {
        public static string Server { get; private set; } = "localhost";
        public static string DatabaseName { get; private set; } = "orys_db";
        public static string UserId { get; private set; } = "root";
        public static string Password { get; private set; } = "";
        public static string Port { get; private set; } = "3306";

        private static string _connectionString = "";
        private static string ConnectionString
        {
            get
            {
                if (string.IsNullOrEmpty(_connectionString))
                {
                    try
                    {
                        string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.json");
                        if (File.Exists(path))
                        {
                            string json = File.ReadAllText(path);
                            using (JsonDocument doc = JsonDocument.Parse(json))
                            {
                                var db = doc.RootElement.GetProperty("Database");
                                Server = db.GetProperty("Server").GetString() ?? "localhost";
                                Port = db.GetProperty("Port").GetString() ?? "3306";
                                DatabaseName = db.GetProperty("DatabaseName").GetString() ?? "orys_db";
                                UserId = db.GetProperty("UserId").GetString() ?? "root";
                                Password = db.GetProperty("Password").GetString() ?? "";
                            }
                        }
                    }
                    catch { } // fallback to defaults

                    _connectionString = $"Server={Server};Port={Port};Database={DatabaseName};Uid={UserId};Pwd={Password};SslMode=Preferred;";
                }
                return _connectionString;
            }
        }

        public static MySqlConnection GetConnection()
        {
            return new MySqlConnection(ConnectionString);
        }

        public static bool TestConnection(out string errorMessage)
        {
            errorMessage = string.Empty;
            try
            {
                using (var connection = GetConnection())
                {
                    connection.Open();
                    return true;
                }
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
                return false;
            }
        }

        /// <summary>
        /// Veritabanı ve tabloları oluşturur - OTEL YÖNETİM SİSTEMİ
        /// </summary>
        public static void InitializeDatabase()
        {
            string connWithoutDb = $"Server={Server};Port={Port};Uid={UserId};Pwd={Password};SslMode=Preferred;";

            using (var connection = new MySqlConnection(connWithoutDb))
            {
                connection.Open();

                string createDb = $"CREATE DATABASE IF NOT EXISTS `{DatabaseName}` CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;";
                using (var cmd = new MySqlCommand(createDb, connection))
                    cmd.ExecuteNonQuery();

                connection.ChangeDatabase(DatabaseName);

                // ===================== KULLANICILAR =====================
                string createUsersTable = @"
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
                    ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;";
                using (var cmd = new MySqlCommand(createUsersTable, connection))
                    cmd.ExecuteNonQuery();

                // ===================== ODA TİPLERİ =====================
                string createRoomTypes = @"
                    CREATE TABLE IF NOT EXISTS `room_types` (
                        `id` INT AUTO_INCREMENT PRIMARY KEY,
                        `name` VARCHAR(50) NOT NULL,
                        `description` TEXT,
                        `base_price` DECIMAL(10,2) NOT NULL DEFAULT 0,
                        `capacity` INT NOT NULL DEFAULT 2,
                        `created_at` DATETIME DEFAULT CURRENT_TIMESTAMP
                    ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;";
                using (var cmd = new MySqlCommand(createRoomTypes, connection))
                    cmd.ExecuteNonQuery();

                // ===================== ODALAR =====================
                string createRooms = @"
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
                    ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;";
                using (var cmd = new MySqlCommand(createRooms, connection))
                    cmd.ExecuteNonQuery();

                // ===================== MİSAFİRLER =====================
                string createGuests = @"
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
                    ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;";
                using (var cmd = new MySqlCommand(createGuests, connection))
                    cmd.ExecuteNonQuery();

                // ===================== REZERVASYONLAR =====================
                string createReservations = @"
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
                    ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;";
                using (var cmd = new MySqlCommand(createReservations, connection))
                    cmd.ExecuteNonQuery();

                // ===================== ÖDEMELER =====================
                string createPayments = @"
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
                    ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;";
                using (var cmd = new MySqlCommand(createPayments, connection))
                    cmd.ExecuteNonQuery();

                // ===================== VARSAYILAN VERİLER =====================

                // Varsayılan kullanıcılar (BCrypt hashlenmiş şifreler)
                // ON DUPLICATE KEY UPDATE ile her başlatmada varsayılan şifreler güncellenir
                string adminHash = ORYS.Helpers.AuthHelper.HashPassword("admin123");
                string resepsiyonHash = ORYS.Helpers.AuthHelper.HashPassword("resepsiyon123");
                string muhasebeHash = ORYS.Helpers.AuthHelper.HashPassword("muhasebe123");
                string insertUsers = $@"
                    INSERT INTO `users` (`full_name`, `username`, `password`, `role`, `email`)
                    VALUES 
                        ('Sistem Admin', 'admin', '{adminHash}', 'Admin', 'admin@orys.com'),
                        ('Resepsiyonist', 'resepsiyon', '{resepsiyonHash}', 'Resepsiyonist', 'resepsiyon@orys.com'),
                        ('Muhasebeci', 'muhasebe', '{muhasebeHash}', 'Muhasebe', 'muhasebe@orys.com')
                    ON DUPLICATE KEY UPDATE
                        `password` = IF(LENGTH(`password`) < 10, VALUES(`password`), `password`),
                        `role` = VALUES(`role`),
                        `is_active` = 1;";
                using (var cmd = new MySqlCommand(insertUsers, connection))
                    cmd.ExecuteNonQuery();

                // Eğer şifre hatalı hash içeriyorsa zorla güncelle (setup.sql'den gelen dummy hashler)
                string[] defaultUsers = { "admin", "resepsiyon", "muhasebe" };
                string[] defaultHashes = { adminHash, resepsiyonHash, muhasebeHash };
                for (int i = 0; i < defaultUsers.Length; i++)
                {
                    string checkHash = $"SELECT `password` FROM `users` WHERE `username` = '{defaultUsers[i]}'";
                    string? storedHash = null;
                    using (var checkCmd = new MySqlCommand(checkHash, connection))
                        storedHash = checkCmd.ExecuteScalar()?.ToString();

                    // Eğer hash BCrypt formatında değilse veya geçersizse güncelle
                    bool needsUpdate = false;
                    if (!string.IsNullOrEmpty(storedHash))
                    {
                        try { needsUpdate = !BCrypt.Net.BCrypt.Verify(GetDefaultPassword(defaultUsers[i]), storedHash); }
                        catch { needsUpdate = true; }
                    }
                    if (needsUpdate)
                    {
                        string fixHash = $"UPDATE `users` SET `password` = '{defaultHashes[i]}' WHERE `username` = '{defaultUsers[i]}'";
                        using (var fixCmd = new MySqlCommand(fixHash, connection))
                            fixCmd.ExecuteNonQuery();
                    }
                }

                // Varsayılan oda tipleri
                string insertRoomTypes = @"
                    INSERT IGNORE INTO `room_types` (`id`, `name`, `description`, `base_price`, `capacity`)
                    VALUES 
                        (1, 'Standart', 'Standart tek/çift kişilik oda', 1500.00, 2),
                        (2, 'Deluxe', 'Geniş deluxe oda, şehir manzarası', 2500.00, 2),
                        (3, 'Suite', 'Lüks suit oda, oturma odası dahil', 4000.00, 4),
                        (4, 'Aile', 'Aile odası, geniş alan', 3000.00, 5),
                        (5, 'Kral Dairesi', 'En lüks oda, 360° manzara', 7000.00, 2);";
                using (var cmd = new MySqlCommand(insertRoomTypes, connection))
                    cmd.ExecuteNonQuery();

                // Varsayılan odalar (40 oda, 4 kat, her katta 101-110 formatında)
                string checkRooms = "SELECT COUNT(*) FROM `rooms`";
                long roomCount = 0;
                using (var cmd = new MySqlCommand(checkRooms, connection))
                    roomCount = (long)cmd.ExecuteScalar();

                if (roomCount == 0)
                {
                    string insertRooms = @"
                        INSERT INTO `rooms` (`room_number`, `room_type_id`, `floor`, `capacity`, `price_per_night`, `status`) VALUES
                        ('101', 1, 1, 2, 1500.00, 'Available'),
                        ('102', 1, 1, 2, 1500.00, 'Occupied'),
                        ('103', 1, 1, 2, 1500.00, 'Available'),
                        ('104', 2, 1, 2, 2500.00, 'Occupied'),
                        ('105', 1, 1, 2, 1500.00, 'Reserved'),
                        ('106', 1, 1, 2, 1500.00, 'Available'),
                        ('107', 2, 1, 2, 2500.00, 'Occupied'),
                        ('108', 1, 1, 2, 1500.00, 'Available'),
                        ('109', 3, 1, 4, 4000.00, 'Maintenance'),
                        ('110', 1, 1, 2, 1500.00, 'Occupied'),
                        ('201', 1, 2, 2, 1500.00, 'Available'),
                        ('202', 2, 2, 2, 2500.00, 'Occupied'),
                        ('203', 1, 2, 2, 1500.00, 'Available'),
                        ('204', 1, 2, 2, 1500.00, 'Occupied'),
                        ('205', 2, 2, 2, 2500.00, 'Available'),
                        ('206', 1, 2, 2, 1500.00, 'Reserved'),
                        ('207', 1, 2, 2, 1500.00, 'Occupied'),
                        ('208', 3, 2, 4, 4000.00, 'Reserved'),
                        ('209', 1, 2, 2, 1500.00, 'Available'),
                        ('210', 2, 2, 2, 2500.00, 'Occupied'),
                        ('301', 4, 3, 5, 3000.00, 'Available'),
                        ('302', 1, 3, 2, 1500.00, 'Occupied'),
                        ('303', 2, 3, 2, 2500.00, 'Available'),
                        ('304', 1, 3, 2, 1500.00, 'Occupied'),
                        ('305', 3, 3, 4, 4000.00, 'Maintenance'),
                        ('306', 1, 3, 2, 1500.00, 'Available'),
                        ('307', 2, 3, 2, 2500.00, 'Occupied'),
                        ('308', 1, 3, 2, 1500.00, 'Available'),
                        ('309', 1, 3, 2, 1500.00, 'Occupied'),
                        ('310', 5, 3, 2, 7000.00, 'Reserved'),
                        ('401', 1, 4, 2, 1500.00, 'Available'),
                        ('402', 2, 4, 2, 2500.00, 'Available'),
                        ('403', 1, 4, 2, 1500.00, 'Occupied'),
                        ('404', 1, 4, 2, 1500.00, 'Available'),
                        ('405', 3, 4, 4, 4000.00, 'Reserved'),
                        ('406', 1, 4, 2, 1500.00, 'Available'),
                        ('407', 2, 4, 2, 2500.00, 'Occupied'),
                        ('408', 4, 4, 5, 3000.00, 'Available'),
                        ('409', 1, 4, 2, 1500.00, 'Maintenance'),
                        ('410', 5, 4, 2, 7000.00, 'Available');";
                    using (var cmd = new MySqlCommand(insertRooms, connection))
                        cmd.ExecuteNonQuery();
                }

                // Varsayılan misafirler
                string checkGuests = "SELECT COUNT(*) FROM `guests`";
                long guestCount = 0;
                using (var cmd = new MySqlCommand(checkGuests, connection))
                    guestCount = (long)cmd.ExecuteScalar();

                if (guestCount == 0)
                {
                    string insertGuests = @"
                        INSERT INTO `guests` (`full_name`, `tc_no`, `phone`, `email`, `nationality`) VALUES
                        ('Ahmet Yılmaz', '12345678901', '05321234567', 'ahmet@email.com', 'Türkiye'),
                        ('Sarah Johnson', '', '05559876543', 'sarah@email.com', 'USA'),
                        ('Maria Yılmaz', '98765432109', '05441112233', 'maria@email.com', 'Türkiye'),
                        ('Sarah Ullmon', '', '05334445566', 'sullmon@email.com', 'Germany'),
                        ('Duna Jahyanis', '', '05367778899', 'duna@email.com', 'Greece'),
                        ('Luna Ikorar', '', '05381234567', 'luna@email.com', 'Italy'),
                        ('Mehmet Kaya', '11122233344', '05429876543', 'mehmet@email.com', 'Türkiye'),
                        ('Fatma Demir', '55566677788', '05461112233', 'fatma@email.com', 'Türkiye'),
                        ('Ali Çelik', '99988877766', '05501234567', 'ali@email.com', 'Türkiye'),
                        ('Elif Aydın', '33344455566', '05529876543', 'elif@email.com', 'Türkiye');";
                    using (var cmd = new MySqlCommand(insertGuests, connection))
                        cmd.ExecuteNonQuery();
                }

                // Varsayılan rezervasyonlar
                string checkRes = "SELECT COUNT(*) FROM `reservations`";
                long resCount = 0;
                using (var cmd = new MySqlCommand(checkRes, connection))
                    resCount = (long)cmd.ExecuteScalar();

                if (resCount == 0)
                {
                    string today = DateTime.Now.ToString("yyyy-MM-dd");
                    string tomorrow = DateTime.Now.AddDays(1).ToString("yyyy-MM-dd");
                    string nextWeek = DateTime.Now.AddDays(7).ToString("yyyy-MM-dd");
                    string yesterday = DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd");
                    string threeDaysAgo = DateTime.Now.AddDays(-3).ToString("yyyy-MM-dd");

                    // room_id: 102=2, 104=4, 107=7, 110=10, 202=12, 204=14, 207=17, 210=20, 302=22, 304=24, 307=27, 309=29, 403=33, 407=37
                    string insertRes = $@"
                        INSERT INTO `reservations` (`guest_id`, `room_id`, `check_in_date`, `check_out_date`, `adults`, `children`, `status`, `total_price`, `created_by`) VALUES
                        (1, 2, '{threeDaysAgo}', '{today}', 2, 0, 'GirisYapildi', 4500.00, 1),
                        (2, 4, '{threeDaysAgo}', '{today}', 1, 0, 'GirisYapildi', 7500.00, 1),
                        (3, 7, '{yesterday}', '{tomorrow}', 2, 1, 'GirisYapildi', 3750.00, 1),
                        (7, 10, '{yesterday}', '{nextWeek}', 2, 0, 'GirisYapildi', 10500.00, 1),
                        (8, 12, '{threeDaysAgo}', '{tomorrow}', 1, 0, 'GirisYapildi', 10000.00, 1),
                        (9, 14, '{yesterday}', '{nextWeek}', 2, 0, 'GirisYapildi', 10500.00, 1),
                        (10, 22, '{threeDaysAgo}', '{today}', 2, 1, 'GirisYapildi', 4500.00, 1),
                        (4, 24, '{yesterday}', '{tomorrow}', 1, 0, 'GirisYapildi', 3000.00, 1),
                        (5, 29, '{threeDaysAgo}', '{today}', 2, 0, 'GirisYapildi', 4500.00, 1),
                        (6, 33, '{yesterday}', '{nextWeek}', 2, 0, 'GirisYapildi', 10500.00, 1),
                        (1, 5, '{today}', '{nextWeek}', 2, 0, 'Onaylandi', 10500.00, 1),
                        (2, 18, '{today}', '{nextWeek}', 2, 1, 'Onaylandi', 28000.00, 1),
                        (6, 30, '{tomorrow}', '{nextWeek}', 2, 0, 'Bekliyor', 42000.00, 1),
                        (3, 37, '{yesterday}', '{nextWeek}', 2, 0, 'GirisYapildi', 17500.00, 1);";
                    using (var cmd = new MySqlCommand(insertRes, connection))
                        cmd.ExecuteNonQuery();
                }
            }
        }

        /// <summary>
        /// Varsayılan kullanıcı şifrelerini döndürür (hash doğrulama için)
        /// </summary>
        private static string GetDefaultPassword(string username) => username switch
        {
            "admin"      => "admin123",
            "resepsiyon" => "resepsiyon123",
            "muhasebe"   => "muhasebe123",
            _            => ""
        };
    }
}
