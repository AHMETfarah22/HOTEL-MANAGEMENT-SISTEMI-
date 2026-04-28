using System;
using System.Collections.Generic;
using MySql.Data.MySqlClient;
using ORYS.Database;

namespace ORYS.Helpers
{
    /// <summary>
    /// Online (web sitesinden gelen) rezervasyon taleplerini yönetir.
    /// online_reservations tablosuyla çalışır.
    /// </summary>
    public static class OnlineReservationHelper
    {
        public class OnlineReservation
        {
            public int Id { get; set; }
            public string? ResCode { get; set; }
            public string FullName { get; set; } = "";
            public string Email { get; set; } = "";
            public string? Phone { get; set; }
            public string? TcNo { get; set; }
            public string Nationality { get; set; } = "Türkiye";
            public int RoomId { get; set; }
            public string RoomNumber { get; set; } = "";
            public string RoomTypeName { get; set; } = "";
            public DateTime CheckInDate { get; set; }
            public DateTime CheckOutDate { get; set; }
            public int Adults { get; set; }
            public int Children { get; set; }
            public decimal TotalPrice { get; set; }
            public string? Notes { get; set; }
            public string Status { get; set; } = "Bekliyor";
            public int? InternalResId { get; set; }
            public string? RejectReason { get; set; }
            public DateTime CreatedAt { get; set; }
            public int NightCount => (CheckOutDate - CheckInDate).Days;
        }

        /// <summary>
        /// online_reservations tablosunu oluşturur (yoksa).
        /// DatabaseHelper.InitializeDatabase() sonrasında çağrılır.
        /// </summary>
        public static void EnsureTableExists()
        {
            try
            {
                using var conn = DatabaseHelper.GetConnection();
                conn.Open();
                string sql = @"
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
                using var cmd = new MySqlCommand(sql, conn);
                cmd.ExecuteNonQuery();

                // Kolon yoksa ekle
                try {
                    using var alterCmd = new MySqlCommand("ALTER TABLE `online_reservations` ADD COLUMN `res_code` VARCHAR(10) UNIQUE NULL AFTER `id` ", conn);
                    alterCmd.ExecuteNonQuery();
                } catch { }
            }
            catch { /* Tablo zaten varsa veya bağlantı sorunu */ }
        }

        /// <summary>
        /// Bekleyen online rezervasyon sayısını döndürür (rozet için).
        /// </summary>
        public static int GetPendingCount()
        {
            try
            {
                using var conn = DatabaseHelper.GetConnection();
                conn.Open();
                using var cmd = new MySqlCommand(
                    "SELECT COUNT(*) FROM online_reservations WHERE status='Bekliyor'", conn);
                return Convert.ToInt32(cmd.ExecuteScalar());
            }
            catch { return 0; }
        }

        /// <summary>
        /// Tüm online rezervasyonları listeler (filtre: null=hepsi).
        /// </summary>
        public static List<OnlineReservation> GetAll(string? statusFilter = null)
        {
            var list = new List<OnlineReservation>();
            try
            {
                using var conn = DatabaseHelper.GetConnection();
                conn.Open();
                string sql = @"SELECT id, full_name, email, phone, tc_no, nationality,
                               room_id, room_number, room_type_name,
                               check_in_date, check_out_date, adults, children,
                               total_price, notes, status, internal_res_id, reject_reason, created_at
                               FROM online_reservations";
                if (!string.IsNullOrEmpty(statusFilter))
                    sql += " WHERE status = @st";
                sql += " ORDER BY created_at DESC";

                using var cmd = new MySqlCommand(sql, conn);
                if (!string.IsNullOrEmpty(statusFilter))
                    cmd.Parameters.AddWithValue("@st", statusFilter);

                using var rdr = cmd.ExecuteReader();
                while (rdr.Read())
                {
                    list.Add(new OnlineReservation
                    {
                        Id = rdr.GetInt32("id"),
                        FullName = rdr.GetString("full_name"),
                        Email = rdr.GetString("email"),
                        Phone = rdr.IsDBNull(rdr.GetOrdinal("phone")) ? null : rdr.GetString("phone"),
                        TcNo = rdr.IsDBNull(rdr.GetOrdinal("tc_no")) ? null : rdr.GetString("tc_no"),
                        Nationality = rdr.IsDBNull(rdr.GetOrdinal("nationality")) ? "Türkiye" : rdr.GetString("nationality"),
                        RoomId = rdr.GetInt32("room_id"),
                        RoomNumber = rdr.IsDBNull(rdr.GetOrdinal("room_number")) ? "" : rdr.GetString("room_number"),
                        RoomTypeName = rdr.IsDBNull(rdr.GetOrdinal("room_type_name")) ? "" : rdr.GetString("room_type_name"),
                        CheckInDate = rdr.GetDateTime("check_in_date"),
                        CheckOutDate = rdr.GetDateTime("check_out_date"),
                        Adults = rdr.GetInt32("adults"),
                        Children = rdr.GetInt32("children"),
                        TotalPrice = rdr.IsDBNull(rdr.GetOrdinal("total_price")) ? 0 : rdr.GetDecimal("total_price"),
                        Notes = rdr.IsDBNull(rdr.GetOrdinal("notes")) ? null : rdr.GetString("notes"),
                        Status = rdr.GetString("status"),
                        InternalResId = rdr.IsDBNull(rdr.GetOrdinal("internal_res_id")) ? null : rdr.GetInt32("internal_res_id"),
                        RejectReason = rdr.IsDBNull(rdr.GetOrdinal("reject_reason")) ? null : rdr.GetString("reject_reason"),
                        CreatedAt = rdr.GetDateTime("created_at"),
                    });
                }
            }
            catch { }
            return list;
        }

        /// <summary>
        /// Online rezervasyonu onayla:
        /// 1. Misafiri guests'e ekle (veya mevcut olanı bul)
        /// 2. reservations tablosuna ekle
        /// 3. Oda durumunu Reserved yap
        /// 4. online_reservations durumunu Onaylandi yap
        /// </summary>
        public static (bool Success, string Message) Approve(OnlineReservation onlineRes)
        {
            try
            {
                using var client = new System.Net.Http.HttpClient();
                client.BaseAddress = new Uri("http://localhost:5050");
                
                var response = client.PutAsync($"/api/reservations/online/{onlineRes.Id}/approve", null).Result;
                var content = response.Content.ReadAsStringAsync().Result;

                if (response.IsSuccessStatusCode)
                {
                    return (true, "✅ Onaylandı ve misafire mail gönderildi!");
                }
                else
                {
                    return (false, $"❌ API Hatası: {content}");
                }
            }
            catch (Exception ex)
            {
                // API hatası alırsak yerel veritabanı mantığını yedek olarak kullanabiliriz
                // Ama şu an kullanıcı API üzerinden gitmek istediği için hata döndürüyoruz.
                return (false, $"❌ Bağlantı Hatası (API Kapalı mı?): {ex.Message}");
            }
        }

        /// <summary>
        /// Online rezervasyonu reddet.
        /// </summary>
        public static (bool Success, string Message) Reject(int id, string? reason)
        {
            try
            {
                using var client = new System.Net.Http.HttpClient();
                client.BaseAddress = new Uri("http://localhost:5050");
                
                var payload = new { reason = reason };
                var json = System.Text.Json.JsonSerializer.Serialize(payload);
                var contentReq = new System.Net.Http.StringContent(json, System.Text.Encoding.UTF8, "application/json");

                var response = client.PutAsync($"/api/reservations/online/{id}/reject", contentReq).Result;
                var contentRes = response.Content.ReadAsStringAsync().Result;

                if (response.IsSuccessStatusCode)
                {
                    return (true, "❌ Reddedildi ve misafire mail gönderildi.");
                }
                else
                {
                    return (false, $"❌ API Hatası: {contentRes}");
                }
            }
            catch (Exception ex)
            {
                return (false, $"❌ Bağlantı Hatası: {ex.Message}");
            }
        }
    }
}
