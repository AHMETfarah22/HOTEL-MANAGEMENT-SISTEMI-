using System;
using System.Collections.Generic;
using MySql.Data.MySqlClient;
using ORYS.Database;
using ORYS.Models;

namespace ORYS.Helpers
{
    public static class MaintenanceHelper
    {
        public static List<MaintenanceRequest> GetAllRequests()
        {
            var list = new List<MaintenanceRequest>();
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                string query = @"SELECT m.*, r.room_number, u.full_name as reporter_name 
                                 FROM maintenance_requests m
                                 LEFT JOIN rooms r ON m.room_id = r.id
                                 LEFT JOIN users u ON m.reported_by = u.id
                                 ORDER BY CASE m.priority 
                                    WHEN 'Acil' THEN 1 
                                    WHEN 'Yüksek' THEN 2 
                                    WHEN 'Orta' THEN 3 
                                    ELSE 4 END, m.created_at DESC";
                using (var cmd = new MySqlCommand(query, conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(new MaintenanceRequest
                        {
                            Id = reader.GetInt32("id"),
                            RoomId = reader.IsDBNull(reader.GetOrdinal("room_id")) ? (int?)null : reader.GetInt32("room_id"),
                            RoomNumber = reader.IsDBNull(reader.GetOrdinal("room_number")) ? "Genel" : reader.GetString("room_number"),
                            Category = reader.GetString("category"),
                            Description = reader.GetString("description"),
                            Priority = reader.GetString("priority"),
                            Status = reader.GetString("status"),
                            ReportedById = reader.IsDBNull(reader.GetOrdinal("reported_by")) ? (int?)null : reader.GetInt32("reported_by"),
                            ReportedByName = reader.IsDBNull(reader.GetOrdinal("reporter_name")) ? "Sistem" : reader.GetString("reporter_name"),
                            AssignedTo = reader.IsDBNull(reader.GetOrdinal("assigned_to")) ? null : reader.GetString("assigned_to"),
                            CreatedAt = reader.GetDateTime("created_at"),
                            UpdatedAt = reader.GetDateTime("updated_at")
                        });
                    }
                }
            }
            return list;
        }

        public static void CreateRequest(MaintenanceRequest req)
        {
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                string sql = @"INSERT INTO maintenance_requests (room_id, category, description, priority, reported_by, assigned_to) 
                               VALUES (@rid, @cat, @desc, @prio, @rep, @asg)";
                using (var cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@rid", (object?)req.RoomId ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@cat", req.Category);
                    cmd.Parameters.AddWithValue("@desc", req.Description);
                    cmd.Parameters.AddWithValue("@prio", req.Priority);
                    cmd.Parameters.AddWithValue("@rep", (object?)AuthHelper.CurrentUser?.Id ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@asg", (object?)req.AssignedTo ?? DBNull.Value);
                    cmd.ExecuteNonQuery();
                }

                // Eğer odayla ilgiliyse odanın durumunu "Maintenance" yapalım mı?
                // Genelde arıza bildirildiğinde oda hemen bakıma alınır.
                if (req.RoomId != null)
                {
                    RoomHelper.UpdateRoomStatus(req.RoomId.Value, "Maintenance");
                }
            }
        }

        public static void UpdateStatus(int id, string status)
        {
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                using (var cmd = new MySqlCommand("UPDATE maintenance_requests SET status=@s WHERE id=@id", conn))
                {
                    cmd.Parameters.AddWithValue("@s", status);
                    cmd.Parameters.AddWithValue("@id", id);
                    cmd.ExecuteNonQuery();
                }

                // Eğer tamamlandıysa ve oda arızasıysa odayı "Available" yapalım mı?
                // Bu tehlikeli olabilir çünkü oda temizlenmiş olmayabilir.
                // Şimdilik sadece log amaçlı kalsın.
                if (status == "Tamamlandi")
                {
                    // Opsiyonel: Oda durumunu geri alma mantığı eklenebilir.
                }
            }
        }

        public static int GetPendingCount()
        {
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                using (var cmd = new MySqlCommand("SELECT COUNT(*) FROM maintenance_requests WHERE status IN ('Bekliyor', 'Devam Ediyor')", conn))
                {
                    return Convert.ToInt32(cmd.ExecuteScalar());
                }
            }
        }
    }
}
