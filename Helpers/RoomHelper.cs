using System;
using System.Collections.Generic;
using MySql.Data.MySqlClient;
using ORYS.Database;
using ORYS.Models;

namespace ORYS.Helpers
{
    public static class RoomHelper
    {
        public static List<Room> GetAllRooms()
        {
            var rooms = new List<Room>();
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                string query = @"SELECT r.id, r.room_number, r.room_type_id, r.floor, r.capacity, 
                                 r.price_per_night, r.status, r.cleaning_status, r.description,
                                 rt.name AS room_type_name 
                                 FROM rooms r 
                                 LEFT JOIN room_types rt ON r.room_type_id = rt.id 
                                 ORDER BY r.room_number";
                using (var cmd = new MySqlCommand(query, conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        rooms.Add(new Room
                        {
                            Id = reader.GetInt32("id"),
                            RoomNumber = reader.GetString("room_number"),
                            RoomTypeId = reader.GetInt32("room_type_id"),
                            RoomTypeName = reader.IsDBNull(reader.GetOrdinal("room_type_name")) ? "" : reader.GetString("room_type_name"),
                            Floor = reader.GetInt32("floor"),
                            Capacity = reader.GetInt32("capacity"),
                            PricePerNight = reader.GetDecimal("price_per_night"),
                            Status = reader.GetString("status"),
                            CleaningStatus = reader.GetString("cleaning_status"),
                            Description = reader.IsDBNull(reader.GetOrdinal("description")) ? null : reader.GetString("description"),
                        });
                    }
                }
            }
            return rooms;
        }

        public static void UpdateRoomStatus(int roomId, string status)
        {
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                string query = "UPDATE rooms SET status = @status WHERE id = @id";
                using (var cmd = new MySqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@status", status);
                    cmd.Parameters.AddWithValue("@id", roomId);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public static void UpdateCleaningStatus(int roomId, string cleaningStatus)
        {
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                string query = "UPDATE rooms SET cleaning_status = @cs WHERE id = @id";
                using (var cmd = new MySqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@cs", cleaningStatus);
                    cmd.Parameters.AddWithValue("@id", roomId);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public static void AddRoom(Room room)
        {
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                string query = @"INSERT INTO rooms (room_number, room_type_id, floor, capacity, price_per_night, status, cleaning_status, description) 
                                 VALUES (@room_number, @room_type_id, @floor, @capacity, @price, @status, @cleaning_status, @desc)";
                using (var cmd = new MySqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@room_number", room.RoomNumber);
                    cmd.Parameters.AddWithValue("@room_type_id", room.RoomTypeId);
                    cmd.Parameters.AddWithValue("@floor", room.Floor);
                    cmd.Parameters.AddWithValue("@capacity", room.Capacity);
                    cmd.Parameters.AddWithValue("@price", room.PricePerNight);
                    cmd.Parameters.AddWithValue("@status", room.Status);
                    cmd.Parameters.AddWithValue("@cleaning_status", room.CleaningStatus);
                    cmd.Parameters.AddWithValue("@desc", (object?)room.Description ?? DBNull.Value);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public static void UpdateRoom(Room room)
        {
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                string query = @"UPDATE rooms SET room_number=@rn, room_type_id=@rt, floor=@f, 
                                 capacity=@c, price_per_night=@p, status=@s, cleaning_status=@cs, description=@d WHERE id=@id";
                using (var cmd = new MySqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@rn", room.RoomNumber);
                    cmd.Parameters.AddWithValue("@rt", room.RoomTypeId);
                    cmd.Parameters.AddWithValue("@f", room.Floor);
                    cmd.Parameters.AddWithValue("@c", room.Capacity);
                    cmd.Parameters.AddWithValue("@p", room.PricePerNight);
                    cmd.Parameters.AddWithValue("@s", room.Status);
                    cmd.Parameters.AddWithValue("@cs", room.CleaningStatus);
                    cmd.Parameters.AddWithValue("@d", (object?)room.Description ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@id", room.Id);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public static void DeleteRoom(int roomId)
        {
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                using (var cmd = new MySqlCommand("DELETE FROM rooms WHERE id=@id", conn))
                {
                    cmd.Parameters.AddWithValue("@id", roomId);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public static List<(int Id, string Name, decimal BasePrice, int Capacity)> GetRoomTypes()
        {
            var types = new List<(int, string, decimal, int)>();
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                using (var cmd = new MySqlCommand("SELECT * FROM room_types ORDER BY id", conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        types.Add((reader.GetInt32("id"), reader.GetString("name"),
                                   reader.GetDecimal("base_price"), reader.GetInt32("capacity")));
                    }
                }
            }
            return types;
        }

        public static int GetRoomCount(string? status = null)
        {
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                string query = status == null ? "SELECT COUNT(*) FROM rooms" : "SELECT COUNT(*) FROM rooms WHERE status=@s";
                using (var cmd = new MySqlCommand(query, conn))
                {
                    if (status != null) cmd.Parameters.AddWithValue("@s", status);
                    return Convert.ToInt32(cmd.ExecuteScalar());
                }
            }
        }

        /// <summary>
        /// Belirtilen tarih aralığında müsait odaları getirir (parameterized query ile güvenli)
        /// </summary>
        public static List<Room> GetAvailableRooms(DateTime checkIn, DateTime checkOut, int excludeResId = 0)
        {
            var rooms = new List<Room>();
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                string query = @"SELECT r.id, r.room_number, r.room_type_id, r.floor, r.capacity, 
                                 r.price_per_night, r.status, r.cleaning_status, r.description, 
                                 rt.name AS room_type_name 
                                 FROM rooms r
                                 LEFT JOIN room_types rt ON r.room_type_id = rt.id
                                 WHERE r.status != 'Maintenance'
                                 AND r.id NOT IN (
                                     SELECT room_id FROM reservations 
                                     WHERE status IN ('Bekliyor','Onaylandi','GirisYapildi')
                                     AND check_in_date < @checkOut AND check_out_date > @checkIn
                                     AND id != @excludeResId
                                 ) ORDER BY r.room_number";
                using (var cmd = new MySqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@checkIn", checkIn.Date);
                    cmd.Parameters.AddWithValue("@checkOut", checkOut.Date);
                    cmd.Parameters.AddWithValue("@excludeResId", excludeResId);
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            rooms.Add(new Room
                            {
                                Id = reader.GetInt32("id"),
                                RoomNumber = reader.GetString("room_number"),
                                RoomTypeId = reader.GetInt32("room_type_id"),
                                RoomTypeName = reader.IsDBNull(reader.GetOrdinal("room_type_name")) ? "" : reader.GetString("room_type_name"),
                                Floor = reader.GetInt32("floor"),
                                Capacity = reader.GetInt32("capacity"),
                                PricePerNight = reader.GetDecimal("price_per_night"),
                                Status = reader.GetString("status"),
                                CleaningStatus = reader.GetString("cleaning_status"),
                            });
                        }
                    }
                }
            }
            return rooms;
        }
        /// <summary>
        /// Oda durumlarını (Müsait, Dolu, Rezerve) aktif rezervasyonlarla karşılaştırır ve tutarsızlıkları düzeltir.
        /// </summary>
        public static void SyncRoomStatuses()
        {
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                
                // 1. Tüm odaları başlangıçta 'Available' (Müsait) kabul et (Bakımda olanlar hariç)
                string resetQuery = "UPDATE rooms SET status = 'Available' WHERE status != 'Maintenance'";
                using (var cmd = new MySqlCommand(resetQuery, conn)) cmd.ExecuteNonQuery();

                // 2. Şu an içeride olan (GirisYapildi) misafirlerin odalarını 'Occupied' (Dolu) yap
                string occupiedQuery = @"UPDATE rooms r 
                                         INNER JOIN reservations rv ON r.id = rv.room_id 
                                         SET r.status = 'Occupied' 
                                         WHERE rv.status = 'GirisYapildi'";
                using (var cmd = new MySqlCommand(occupiedQuery, conn)) cmd.ExecuteNonQuery();

                // 3. Gelecek (Onaylandi/Bekliyor) rezervasyonu olan odaları 'Reserved' (Rezerve) yap (Eğer Dolu değilse)
                // Sadece bugün veya gelecek tarihliler
                string reservedQuery = @"UPDATE rooms r 
                                         INNER JOIN reservations rv ON r.id = rv.room_id 
                                         SET r.status = 'Reserved' 
                                         WHERE rv.status IN ('Onaylandi', 'Bekliyor') 
                                         AND r.status = 'Available'
                                         AND rv.check_in_date <= CURDATE() AND rv.check_out_date >= CURDATE()";
                using (var cmd = new MySqlCommand(reservedQuery, conn)) cmd.ExecuteNonQuery();
            }
        }
    }
}
