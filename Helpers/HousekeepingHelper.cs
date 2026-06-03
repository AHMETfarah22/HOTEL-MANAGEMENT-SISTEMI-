using System;
using System.Collections.Generic;
using MySql.Data.MySqlClient;
using ORYS.Database;
using ORYS.Models;

namespace ORYS.Helpers
{
    public static class HousekeepingHelper
    {
        public static List<HousekeepingLog> GetAllLogs()
        {
            var logs = new List<HousekeepingLog>();
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                string query = @"SELECT l.*, r.room_number, u.full_name as staff_name 
                                 FROM housekeeping_logs l
                                 JOIN rooms r ON l.room_id = r.id
                                 LEFT JOIN users u ON l.staff_id = u.id
                                 ORDER BY l.created_at DESC LIMIT 100";
                using (var cmd = new MySqlCommand(query, conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        logs.Add(new HousekeepingLog
                        {
                            Id = reader.GetInt32("id"),
                            RoomId = reader.GetInt32("room_id"),
                            RoomNumber = reader.GetString("room_number"),
                            StaffId = reader.IsDBNull(reader.GetOrdinal("staff_id")) ? (int?)null : reader.GetInt32("staff_id"),
                            StaffName = reader.IsDBNull(reader.GetOrdinal("staff_name")) ? "Sistem" : reader.GetString("staff_name"),
                            StatusFrom = reader.GetString("status_from"),
                            StatusTo = reader.GetString("status_to"),
                            Notes = reader.IsDBNull(reader.GetOrdinal("notes")) ? "" : reader.GetString("notes"),
                            CreatedAt = reader.GetDateTime("created_at")
                        });
                    }
                }
            }
            return logs;
        }

        public static void AddLog(int roomId, string from, string to, string notes = "")
        {
            int? staffId = AuthHelper.CurrentUser?.Id;
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                string query = @"INSERT INTO housekeeping_logs (room_id, staff_id, status_from, status_to, notes) 
                                 VALUES (@rid, @sid, @from, @to, @notes)";
                using (var cmd = new MySqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@rid", roomId);
                    cmd.Parameters.AddWithValue("@sid", (object?)staffId ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@from", from);
                    cmd.Parameters.AddWithValue("@to", to);
                    cmd.Parameters.AddWithValue("@notes", notes);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public static void ChangeCleaningStatus(int roomId, string newStatus, string notes = "")
        {
            // Önce mevcut durumu al
            string oldStatus = "Dirty";
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                using (var cmd = new MySqlCommand("SELECT cleaning_status FROM rooms WHERE id = @id", conn))
                {
                    cmd.Parameters.AddWithValue("@id", roomId);
                    oldStatus = cmd.ExecuteScalar()?.ToString() ?? "Dirty";
                }
            }

            // Durumu güncelle
            RoomHelper.UpdateCleaningStatus(roomId, newStatus);

            // Log ekle
            AddLog(roomId, oldStatus, newStatus, notes);
        }
        
        public static int GetDirtyRoomCount()
        {
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                using (var cmd = new MySqlCommand("SELECT COUNT(*) FROM rooms WHERE cleaning_status = 'Dirty'", conn))
                {
                    return Convert.ToInt32(cmd.ExecuteScalar());
                }
            }
        }
    }
}
