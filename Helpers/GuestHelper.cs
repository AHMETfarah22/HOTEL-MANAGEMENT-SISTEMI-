using System;
using System.Collections.Generic;
using MySql.Data.MySqlClient;
using ORYS.Database;
using ORYS.Models;

namespace ORYS.Helpers
{
    public static class GuestHelper
    {
        public static List<Guest> GetAllGuests()
        {
            var guests = new List<Guest>();
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                using (var cmd = new MySqlCommand("SELECT * FROM guests ORDER BY full_name", conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        guests.Add(ReadGuest(reader));
                    }
                }
            }
            return guests;
        }

        public static List<Guest> SearchGuests(string searchText)
        {
            var guests = new List<Guest>();
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                string query = @"SELECT * FROM guests 
                                 WHERE full_name LIKE @s OR tc_no LIKE @s OR phone LIKE @s OR email LIKE @s
                                 ORDER BY full_name";
                using (var cmd = new MySqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@s", $"%{searchText}%");
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            guests.Add(ReadGuest(reader));
                        }
                    }
                }
            }
            return guests;
        }

        public static Guest? GetGuestById(int id)
        {
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                using (var cmd = new MySqlCommand("SELECT * FROM guests WHERE id=@id", conn))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read()) return ReadGuest(reader);
                    }
                }
            }
            return null;
        }

        public static int AddGuest(Guest guest)
        {
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                string query = @"INSERT INTO guests (full_name, tc_no, passport_no, phone, email, nationality, address, notes) 
                                 VALUES (@fn, @tc, @pp, @ph, @em, @nat, @addr, @notes); SELECT LAST_INSERT_ID();";
                using (var cmd = new MySqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@fn", guest.FullName);
                    cmd.Parameters.AddWithValue("@tc", (object?)guest.TcNo ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@pp", (object?)guest.PassportNo ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@ph", (object?)guest.Phone ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@em", (object?)guest.Email ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@nat", guest.Nationality);
                    cmd.Parameters.AddWithValue("@addr", (object?)guest.Address ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@notes", (object?)guest.Notes ?? DBNull.Value);
                    return Convert.ToInt32(cmd.ExecuteScalar());
                }
            }
        }

        public static void UpdateGuest(Guest guest)
        {
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                string query = @"UPDATE guests SET full_name=@fn, tc_no=@tc, passport_no=@pp, phone=@ph, 
                                 email=@em, nationality=@nat, address=@addr, notes=@notes WHERE id=@id";
                using (var cmd = new MySqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@fn", guest.FullName);
                    cmd.Parameters.AddWithValue("@tc", (object?)guest.TcNo ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@pp", (object?)guest.PassportNo ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@ph", (object?)guest.Phone ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@em", (object?)guest.Email ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@nat", guest.Nationality);
                    cmd.Parameters.AddWithValue("@addr", (object?)guest.Address ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@notes", (object?)guest.Notes ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@id", guest.Id);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public static void DeleteGuest(int id)
        {
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                using (var cmd = new MySqlCommand("DELETE FROM guests WHERE id=@id", conn))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public static int GetGuestCount()
        {
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                using (var cmd = new MySqlCommand("SELECT COUNT(*) FROM guests", conn))
                    return Convert.ToInt32(cmd.ExecuteScalar());
            }
        }

        private static Guest ReadGuest(MySqlDataReader reader)
        {
            return new Guest
            {
                Id = reader.GetInt32("id"),
                FullName = reader.GetString("full_name"),
                TcNo = reader.IsDBNull(reader.GetOrdinal("tc_no")) ? null : reader.GetString("tc_no"),
                PassportNo = reader.IsDBNull(reader.GetOrdinal("passport_no")) ? null : reader.GetString("passport_no"),
                Phone = reader.IsDBNull(reader.GetOrdinal("phone")) ? null : reader.GetString("phone"),
                Email = reader.IsDBNull(reader.GetOrdinal("email")) ? null : reader.GetString("email"),
                Nationality = reader.IsDBNull(reader.GetOrdinal("nationality")) ? "Türkiye" : reader.GetString("nationality"),
                Address = reader.IsDBNull(reader.GetOrdinal("address")) ? null : reader.GetString("address"),
                Notes = reader.IsDBNull(reader.GetOrdinal("notes")) ? null : reader.GetString("notes"),
            };
        }
    }
}
