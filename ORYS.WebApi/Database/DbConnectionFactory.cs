using MySql.Data.MySqlClient;

namespace ORYS.WebApi.Database
{
    public class DbConnectionFactory
    {
        private readonly string _connectionString;

        public DbConnectionFactory(string server, string port, string dbName, string userId, string password)
        {
            _connectionString = $"Server={server};Port={port};Database={dbName};Uid={userId};Pwd={password};SslMode=Preferred;CharSet=utf8mb4;";
        }

        public MySqlConnection GetConnection()
        {
            // Bulut ortamı (Render vb.) için ortam değişkenini kontrol et
            string? cloudConnString = Environment.GetEnvironmentVariable("CONNECTION_STRING");
            
            if (!string.IsNullOrEmpty(cloudConnString))
            {
                return new MySqlConnection(cloudConnString);
            }

            return new MySqlConnection(_connectionString);
        }

        /// <summary>
        /// online_reservations tablosunu oluşturur (yoksa)
        /// </summary>
        public void InitializeOnlineTable()
        {
            // Önce veritabanına bağlanmayı dene (DB yoksa oluştur)
            string connWithoutDb = _connectionString.Replace("Database=orys_db;", "").Replace("Database=orys_db ", " ");
            
            try
            {
                using var conn = GetConnection();
                conn.Open();

                string createTable = @"
                    CREATE TABLE IF NOT EXISTS `online_reservations` (
                        `id`              INT AUTO_INCREMENT PRIMARY KEY,
                        `res_code`        VARCHAR(10) UNIQUE NULL,
                        `full_name`       VARCHAR(100) NOT NULL,
                        `email`           VARCHAR(100) NOT NULL,
                        `phone`           VARCHAR(20),
                        `tc_no`           VARCHAR(11),
                        `nationality`     VARCHAR(50) DEFAULT 'Türkiye',
                        `room_id`         INT NOT NULL,
                        `room_number`     VARCHAR(10),
                        `room_type_name`  VARCHAR(50),
                        `check_in_date`   DATE NOT NULL,
                        `check_out_date`  DATE NOT NULL,
                        `adults`          INT DEFAULT 1,
                        `children`        INT DEFAULT 0,
                        `total_price`     DECIMAL(10,2),
                        `notes`           TEXT,
                        `status`          ENUM('Bekliyor','Onaylandi','Reddedildi') DEFAULT 'Bekliyor',
                        `internal_res_id` INT NULL,
                        `reject_reason`   TEXT NULL,
                        `created_at`      DATETIME DEFAULT CURRENT_TIMESTAMP,
                        FOREIGN KEY (`room_id`) REFERENCES `rooms`(`id`)
                    ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;";

                using var cmd = new MySqlCommand(createTable, conn);
                cmd.ExecuteNonQuery();

                // Kolon yoksa ekle (Migration emulation)
                try {
                    using var alterCmd = new MySqlCommand("ALTER TABLE `online_reservations` ADD COLUMN `res_code` VARCHAR(10) UNIQUE NULL AFTER `id` ", conn);
                    alterCmd.ExecuteNonQuery();
                } catch { /* Muhtemelen zaten var */ }

                Console.WriteLine("✅ online_reservations tablosu hazır.");

                // Mevcut boş kodları doldur
                string fillCodes = @"
                    UPDATE online_reservations 
                    SET res_code = CONCAT('AFM', FLOOR(100000 + RAND() * 899999)) 
                    WHERE res_code IS NULL OR res_code = '';";
                using var fillCmd = new MySqlCommand(fillCodes, conn);
                fillCmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ DB bağlantı hatası: {ex.Message}");
            }
        }
    }
}
