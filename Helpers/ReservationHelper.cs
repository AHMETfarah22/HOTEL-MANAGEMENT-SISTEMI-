using System;
using System.Collections.Generic;
using MySql.Data.MySqlClient;
using ORYS.Database;
using ORYS.Models;

namespace ORYS.Helpers
{
    public static class ReservationHelper
    {
        public static List<Reservation> GetAllReservations()
        {
            var list = new List<Reservation>();
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                string query = @"SELECT rv.id, rv.guest_id, rv.room_id, rv.check_in_date, rv.check_out_date,
                                 rv.adults, rv.children, rv.status, rv.total_price, rv.notes, rv.created_by,
                                 rv.include_breakfast, rv.breakfast_price, rv.include_dinner, rv.dinner_price,
                                 g.full_name AS guest_name, rm.room_number 
                                 FROM reservations rv
                                 LEFT JOIN guests g ON rv.guest_id = g.id
                                 LEFT JOIN rooms rm ON rv.room_id = rm.id
                                 ORDER BY rv.check_in_date DESC";
                using (var cmd = new MySqlCommand(query, conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read()) list.Add(ReadReservation(reader));
                }
            }
            return list;
        }

        public static List<Reservation> GetActiveReservations()
        {
            var list = new List<Reservation>();
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                string query = @"SELECT rv.id, rv.guest_id, rv.room_id, rv.check_in_date, rv.check_out_date,
                                 rv.adults, rv.children, rv.status, rv.total_price, rv.notes, rv.created_by,
                                 rv.include_breakfast, rv.breakfast_price, rv.include_dinner, rv.dinner_price,
                                 g.full_name AS guest_name, rm.room_number 
                                 FROM reservations rv
                                 LEFT JOIN guests g ON rv.guest_id = g.id
                                 LEFT JOIN rooms rm ON rv.room_id = rm.id
                                 WHERE rv.status IN ('Bekliyor', 'Onaylandi', 'GirisYapildi')
                                 ORDER BY rv.check_in_date";
                using (var cmd = new MySqlCommand(query, conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read()) list.Add(ReadReservation(reader));
                }
            }
            return list;
        }

        public static List<Reservation> GetTodayCheckIns()
        {
            var list = new List<Reservation>();
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                // DATE(check_in_date) = @today kullanarak kesin eşleşme sağlıyoruz
                string query = @"SELECT rv.id, rv.guest_id, rv.room_id, rv.check_in_date, rv.check_out_date,
                                 rv.adults, rv.children, rv.status, rv.total_price, rv.notes, rv.created_by,
                                 rv.include_breakfast, rv.breakfast_price, rv.include_dinner, rv.dinner_price,
                                 g.full_name AS guest_name, rm.room_number 
                                 FROM reservations rv
                                 LEFT JOIN guests g ON rv.guest_id = g.id
                                 LEFT JOIN rooms rm ON rv.room_id = rm.id
                                 WHERE DATE(rv.check_in_date) = @today
                                 AND rv.status IN ('Bekliyor', 'Onaylandi')
                                 ORDER BY rv.check_in_date";
                using (var cmd = new MySqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@today", DateTime.Today.ToString("yyyy-MM-dd"));
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read()) list.Add(ReadReservation(reader));
                    }
                }
            }
            return list;
        }

        public static List<Reservation> GetTodayCheckOuts()
        {
            var list = new List<Reservation>();
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                string query = @"SELECT rv.id, rv.guest_id, rv.room_id, rv.check_in_date, rv.check_out_date,
                                 rv.adults, rv.children, rv.status, rv.total_price, rv.notes, rv.created_by,
                                 rv.include_breakfast, rv.breakfast_price, rv.include_dinner, rv.dinner_price,
                                 g.full_name AS guest_name, rm.room_number 
                                 FROM reservations rv
                                 LEFT JOIN guests g ON rv.guest_id = g.id
                                 LEFT JOIN rooms rm ON rv.room_id = rm.id
                                 WHERE DATE(rv.check_out_date) = @today
                                 AND rv.status = 'GirisYapildi'
                                 ORDER BY rv.check_out_date";
                using (var cmd = new MySqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@today", DateTime.Today.ToString("yyyy-MM-dd"));
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read()) list.Add(ReadReservation(reader));
                    }
                }
            }
            return list;
        }

        public static List<Reservation> GetFilteredCheckIns(DateTime start, DateTime end, int? roomId = null)
        {
            var list = new List<Reservation>();
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                string query = @"SELECT rv.id, rv.guest_id, rv.room_id, rv.check_in_date, rv.check_out_date,
                                 rv.adults, rv.children, rv.status, rv.total_price, rv.notes, rv.created_by,
                                 rv.include_breakfast, rv.breakfast_price, rv.include_dinner, rv.dinner_price,
                                 g.full_name AS guest_name, rm.room_number 
                                 FROM reservations rv
                                 LEFT JOIN guests g ON rv.guest_id = g.id
                                 LEFT JOIN rooms rm ON rv.room_id = rm.id
                                 WHERE rv.check_in_date >= @start AND rv.check_in_date <= @end
                                 AND rv.status IN ('Bekliyor', 'Onaylandi')";
                if (roomId.HasValue) query += " AND rv.room_id = @rid";
                query += " ORDER BY rv.check_in_date";
                using (var cmd = new MySqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@start", start.ToString("yyyy-MM-dd 00:00:00"));
                    cmd.Parameters.AddWithValue("@end", end.ToString("yyyy-MM-dd 23:59:59"));
                    if (roomId.HasValue) cmd.Parameters.AddWithValue("@rid", roomId.Value);
                    using (var reader = cmd.ExecuteReader()) { while (reader.Read()) list.Add(ReadReservation(reader)); }
                }
            }
            return list;
        }

        public static List<Reservation> GetFilteredCheckOuts(DateTime start, DateTime end, int? roomId = null)
        {
            var list = new List<Reservation>();
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                string query = @"SELECT rv.id, rv.guest_id, rv.room_id, rv.check_in_date, rv.check_out_date,
                                 rv.adults, rv.children, rv.status, rv.total_price, rv.notes, rv.created_by,
                                 rv.include_breakfast, rv.breakfast_price, rv.include_dinner, rv.dinner_price,
                                 g.full_name AS guest_name, rm.room_number 
                                 FROM reservations rv
                                 LEFT JOIN guests g ON rv.guest_id = g.id
                                 LEFT JOIN rooms rm ON rv.room_id = rm.id
                                 WHERE rv.check_out_date >= @start AND rv.check_out_date <= @end
                                 AND rv.status = 'GirisYapildi'";
                if (roomId.HasValue) query += " AND rv.room_id = @rid";
                query += " ORDER BY rv.check_out_date";
                using (var cmd = new MySqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@start", start.ToString("yyyy-MM-dd 00:00:00"));
                    cmd.Parameters.AddWithValue("@end", end.ToString("yyyy-MM-dd 23:59:59"));
                    if (roomId.HasValue) cmd.Parameters.AddWithValue("@rid", roomId.Value);
                    using (var reader = cmd.ExecuteReader()) { while (reader.Read()) list.Add(ReadReservation(reader)); }
                }
            }
            return list;
        }

        public static decimal CalculateRemainingBalance(int reservationId)
        {
            try
            {
                using var conn = DatabaseHelper.GetConnection();
                conn.Open();
                
                // 1) Oda Toplam Ücreti
                decimal totalPrice = 0;
                using (var cmd = new MySqlCommand("SELECT total_price FROM reservations WHERE id = @id", conn))
                {
                    cmd.Parameters.AddWithValue("@id", reservationId);
                    totalPrice = Convert.ToDecimal(cmd.ExecuteScalar());
                }

                // 2) Restoran Ekstraları (Odaya yazılan ve ödenmemiş olanlar)
                decimal extras = 0;
                using (var cmd = new MySqlCommand("SELECT SUM(total_amount) FROM restaurant_orders WHERE room_id = (SELECT room_id FROM reservations WHERE id = @id) AND status = 'OdayaYaz'", conn))
                {
                    cmd.Parameters.AddWithValue("@id", reservationId);
                    var res = cmd.ExecuteScalar();
                    extras = res == DBNull.Value ? 0 : Convert.ToDecimal(res);
                }

                // 3) Yapılan Toplam Ödeme
                decimal paid = PaymentHelper.GetTotalPaid(reservationId);

                return (totalPrice + extras) - paid;
            }
            catch { return 0; }
        }

        public static List<Reservation> GetActiveInHouseGuests()
        {
            var list = new List<Reservation>();
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                string query = @"SELECT rv.id, rv.guest_id, rv.room_id, rv.check_in_date, rv.check_out_date,
                                 rv.adults, rv.children, rv.status, rv.total_price, rv.notes, rv.created_by,
                                 rv.include_breakfast, rv.breakfast_price, rv.include_dinner, rv.dinner_price,
                                 g.full_name AS guest_name, rm.room_number 
                                 FROM reservations rv
                                 LEFT JOIN guests g ON rv.guest_id = g.id
                                 LEFT JOIN rooms rm ON rv.room_id = rm.id
                                 WHERE rv.status = 'GirisYapildi'
                                 ORDER BY rm.room_number";
                using (var cmd = new MySqlCommand(query, conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read()) list.Add(ReadReservation(reader));
                }
            }
            return list;
        }

        private static bool IsRoomAvailable(int roomId, DateTime checkIn, DateTime checkOut, int excludeResId = 0)
        {
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                string query = @"SELECT COUNT(*) FROM reservations 
                                 WHERE room_id = @rid 
                                 AND status IN ('Bekliyor', 'Onaylandi', 'GirisYapildi')
                                 AND id != @eid
                                 AND check_in_date < @co AND check_out_date > @ci";
                using (var cmd = new MySqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@rid", roomId);
                    cmd.Parameters.AddWithValue("@eid", excludeResId);
                    cmd.Parameters.AddWithValue("@ci", checkIn.Date);
                    cmd.Parameters.AddWithValue("@co", checkOut.Date);
                    return Convert.ToInt32(cmd.ExecuteScalar()) == 0;
                }
            }
        }

        public static int AddReservation(Reservation res)
        {
            if (!IsRoomAvailable(res.RoomId, res.CheckInDate, res.CheckOutDate))
                throw new InvalidOperationException("Seçilen oda bu tarihler arasında zaten dolu veya başka bir rezervasyon mevcut!");

            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                string query = @"INSERT INTO reservations (guest_id, room_id, check_in_date, check_out_date, 
                                 adults, children, status, total_price, notes, created_by,
                                 include_breakfast, breakfast_price, include_dinner, dinner_price) 
                                 VALUES (@gid, @rid, @ci, @co, @a, @ch, @st, @tp, @n, @cb, @ib, @bp, @id, @dp); SELECT LAST_INSERT_ID();";
                using (var cmd = new MySqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@gid", res.GuestId);
                    cmd.Parameters.AddWithValue("@rid", res.RoomId);
                    cmd.Parameters.AddWithValue("@ci", res.CheckInDate);
                    cmd.Parameters.AddWithValue("@co", res.CheckOutDate);
                    cmd.Parameters.AddWithValue("@a", res.Adults);
                    cmd.Parameters.AddWithValue("@ch", res.Children);
                    cmd.Parameters.AddWithValue("@st", res.Status);
                    cmd.Parameters.AddWithValue("@tp", res.TotalPrice);
                    cmd.Parameters.AddWithValue("@n", (object?)res.Notes ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@cb", (object?)res.CreatedBy ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@ib", res.IncludeBreakfast);
                    cmd.Parameters.AddWithValue("@bp", res.BreakfastPrice);
                    cmd.Parameters.AddWithValue("@id", res.IncludeDinner);
                    cmd.Parameters.AddWithValue("@dp", res.DinnerPrice);
                    return Convert.ToInt32(cmd.ExecuteScalar());
                }
            }
        }

        public static void UpdateReservation(Reservation res)
        {
            if (!IsRoomAvailable(res.RoomId, res.CheckInDate, res.CheckOutDate, res.Id))
                throw new InvalidOperationException("Seçilen oda bu tarihler arasında zaten dolu veya başka bir rezervasyon mevcut!");

            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                // NOT: @incDinner parametresi @id ile karışmaması için yeniden adlandırıldı
                string query = @"UPDATE reservations SET guest_id = @gid, room_id = @rid, check_in_date = @ci, 
                                 check_out_date = @co, adults = @a, children = @ch, status = @st, 
                                 total_price = @tp, notes = @n, include_breakfast = @ib, 
                                 breakfast_price = @bp, include_dinner = @incDinner, dinner_price = @dp WHERE id = @resId";
                using (var cmd = new MySqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@gid", res.GuestId);
                    cmd.Parameters.AddWithValue("@rid", res.RoomId);
                    cmd.Parameters.AddWithValue("@ci", res.CheckInDate);
                    cmd.Parameters.AddWithValue("@co", res.CheckOutDate);
                    cmd.Parameters.AddWithValue("@a", res.Adults);
                    cmd.Parameters.AddWithValue("@ch", res.Children);
                    cmd.Parameters.AddWithValue("@st", res.Status);
                    cmd.Parameters.AddWithValue("@tp", res.TotalPrice);
                    cmd.Parameters.AddWithValue("@n", (object?)res.Notes ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@ib", res.IncludeBreakfast);
                    cmd.Parameters.AddWithValue("@bp", res.BreakfastPrice);
                    cmd.Parameters.AddWithValue("@incDinner", res.IncludeDinner); // @id ile karışmaması için
                    cmd.Parameters.AddWithValue("@dp", res.DinnerPrice);
                    cmd.Parameters.AddWithValue("@resId", res.Id);               // @id'den @resId'ye değiştirildi
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public static void UpdateReservationStatus(int id, string status)
        {
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                using (var cmd = new MySqlCommand("UPDATE reservations SET status=@s WHERE id=@id", conn))
                {
                    cmd.Parameters.AddWithValue("@s", status);
                    cmd.Parameters.AddWithValue("@id", id);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public static void AddExtraCharge(int id, decimal amountToAdd, string extraNote)
        {
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                string query = "UPDATE reservations SET total_price = total_price + @amt, notes = CONCAT(IFNULL(notes, ''), '\n', @nt) WHERE id = @id";
                using (var cmd = new MySqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@amt", amountToAdd);
                    cmd.Parameters.AddWithValue("@nt", $"[{DateTime.Now:dd.MM HH:mm}] EKSTRA HİZMET: {extraNote} (+{amountToAdd:N0}₺)");
                    cmd.Parameters.AddWithValue("@id", id);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public static void CheckIn(int reservationId, int roomId)
        {
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                // Sadece status güncellenir. Orijinal check_in_date korunur (planlı tarih silinmemeli).
                string query = "UPDATE reservations SET status='GirisYapildi' WHERE id=@id";
                using (var cmd = new MySqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@id", reservationId);
                    cmd.ExecuteNonQuery();
                }
            }
            RoomHelper.UpdateRoomStatus(roomId, "Occupied");
        }

        public static void CheckOut(int reservationId, int roomId)
        {
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                // Sadece status güncellenir. Orijinal check_out_date korunur (planlı tarih silinmemeli).
                string query = "UPDATE reservations SET status='CikisYapildi' WHERE id=@id";
                using (var cmd = new MySqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@id", reservationId);
                    cmd.ExecuteNonQuery();
                }
            }
            RoomHelper.UpdateRoomStatus(roomId, "Available");
        }

        public static void DeleteReservation(int id)
        {
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                using (var cmd = new MySqlCommand("DELETE FROM reservations WHERE id=@id", conn))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public static decimal GetTodayRevenue()
        {
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                string todayStart = DateTime.Now.ToString("yyyy-MM-dd 00:00:00");
                string todayEnd = DateTime.Now.ToString("yyyy-MM-dd 23:59:59");
                string query = "SELECT COALESCE(SUM(amount),0) FROM payments WHERE payment_date >= @start AND payment_date <= @end";
                using (var cmd = new MySqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@start", todayStart);
                    cmd.Parameters.AddWithValue("@end", todayEnd);
                    return Convert.ToDecimal(cmd.ExecuteScalar());
                }
            }
        }

        public static decimal GetMonthlyRevenue()
        {
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                string query = "SELECT COALESCE(SUM(amount),0) FROM payments WHERE MONTH(payment_date) = MONTH(CURDATE()) AND YEAR(payment_date) = YEAR(CURDATE())";
                using (var cmd = new MySqlCommand(query, conn))
                    return Convert.ToDecimal(cmd.ExecuteScalar());
            }
        }

        public static void ExtendReservationAndAddExtraCharge(int reservationId, DateTime newCheckOutDate, decimal extraPrice, string note)
        {
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                string query = @"UPDATE reservations 
                                 SET check_out_date = @co, 
                                     total_price = total_price + @amt, 
                                     notes = CONCAT(IFNULL(notes, ''), '\n', @nt) 
                                 WHERE id = @id";
                using (var cmd = new MySqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@co", newCheckOutDate);
                    cmd.Parameters.AddWithValue("@amt", extraPrice);
                    cmd.Parameters.AddWithValue("@nt", note);
                    cmd.Parameters.AddWithValue("@id", reservationId);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        private static Reservation ReadReservation(MySqlDataReader reader)
        {
            return new Reservation
            {
                Id = reader.GetInt32("id"),
                GuestId = reader.GetInt32("guest_id"),
                GuestName = reader.IsDBNull(reader.GetOrdinal("guest_name")) ? "" : reader.GetString("guest_name"),
                RoomId = reader.GetInt32("room_id"),
                RoomNumber = reader.IsDBNull(reader.GetOrdinal("room_number")) ? "" : reader.GetString("room_number"),
                CheckInDate = reader.GetDateTime("check_in_date"),
                CheckOutDate = reader.GetDateTime("check_out_date"),
                Adults = reader.GetInt32("adults"),
                Children = reader.GetInt32("children"),
                Status = reader.GetString("status"),
                TotalPrice = reader.GetDecimal("total_price"),
                IncludeBreakfast = reader.GetBoolean("include_breakfast"),
                BreakfastPrice = reader.GetDecimal("breakfast_price"),
                IncludeDinner = reader.GetBoolean("include_dinner"),
                DinnerPrice = reader.GetDecimal("dinner_price"),
                Notes = reader.IsDBNull(reader.GetOrdinal("notes")) ? null : reader.GetString("notes"),
                CreatedBy = reader.IsDBNull(reader.GetOrdinal("created_by")) ? null : reader.GetInt32("created_by"),
            };
        }
    }
}
