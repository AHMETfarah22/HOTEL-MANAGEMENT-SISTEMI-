using System;
using System.Collections.Generic;
using MySql.Data.MySqlClient;
using ORYS.Database;
using ORYS.Models;

namespace ORYS.Helpers
{
    public static class PaymentHelper
    {
        public static List<Payment> GetAllPayments()
        {
            var list = new List<Payment>();
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                string query = @"SELECT py.id, py.reservation_id, py.amount, py.payment_method, 
                                 py.payment_date, py.notes, py.created_by,
                                 g.full_name AS guest_name, rm.room_number
                                 FROM payments py
                                 LEFT JOIN reservations rv ON py.reservation_id = rv.id
                                 LEFT JOIN guests g ON rv.guest_id = g.id
                                 LEFT JOIN rooms rm ON rv.room_id = rm.id
                                 ORDER BY py.payment_date DESC";
                using (var cmd = new MySqlCommand(query, conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read()) list.Add(ReadPayment(reader));
                }
            }
            return list;
        }

        public static decimal GetTotalPaidForReservation(int reservationId)
        {
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                using (var cmd = new MySqlCommand("SELECT COALESCE(SUM(amount),0) FROM payments WHERE reservation_id=@rid", conn))
                {
                    cmd.Parameters.AddWithValue("@rid", reservationId);
                    return Convert.ToDecimal(cmd.ExecuteScalar());
                }
            }
        }

        public static int AddPayment(Payment payment)
        {
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                string query = @"INSERT INTO payments (reservation_id, amount, payment_method, payment_date, notes, created_by) 
                                 VALUES (@rid, @amt, @pm, @pd, @n, @cb); SELECT LAST_INSERT_ID();";
                using (var cmd = new MySqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@rid", payment.ReservationId);
                    cmd.Parameters.AddWithValue("@amt", payment.Amount);
                    cmd.Parameters.AddWithValue("@pm", payment.PaymentMethod);
                    cmd.Parameters.AddWithValue("@pd", payment.PaymentDate);
                    cmd.Parameters.AddWithValue("@n", (object?)payment.Notes ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@cb", (object?)payment.CreatedBy ?? DBNull.Value);
                    return Convert.ToInt32(cmd.ExecuteScalar());
                }
            }
        }

        public static void DeletePayment(int id)
        {
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                using (var cmd = new MySqlCommand("DELETE FROM payments WHERE id=@id", conn))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public static decimal GetDailyRevenue(DateTime date)
        {
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                string start = date.ToString("yyyy-MM-dd 00:00:00");
                string end = date.ToString("yyyy-MM-dd 23:59:59");
                string query = "SELECT COALESCE(SUM(amount),0) FROM payments WHERE payment_date >= @start AND payment_date <= @end";
                using (var cmd = new MySqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@start", start);
                    cmd.Parameters.AddWithValue("@end", end);
                    return Convert.ToDecimal(cmd.ExecuteScalar());
                }
            }
        }

        public static decimal GetMonthlyRevenue(int month, int year)
        {
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                string startDate = $"{year}-{month:D2}-01 00:00:00";
                string endDate = new DateTime(year, month, DateTime.DaysInMonth(year, month)).ToString("yyyy-MM-dd 23:59:59");
                string query = "SELECT COALESCE(SUM(amount),0) FROM payments WHERE payment_date >= @start AND payment_date <= @end";
                using (var cmd = new MySqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@start", startDate);
                    cmd.Parameters.AddWithValue("@end", endDate);
                    return Convert.ToDecimal(cmd.ExecuteScalar());
                }
            }
        }

        public static List<(string Method, decimal Total, int Count)> GetPaymentMethodStats()
        {
            var list = new List<(string, decimal, int)>();
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                string query = "SELECT payment_method, SUM(amount) AS total, COUNT(*) AS cnt FROM payments GROUP BY payment_method ORDER BY total DESC";
                using (var cmd = new MySqlCommand(query, conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add((reader.GetString("payment_method"), reader.GetDecimal("total"), reader.GetInt32("cnt")));
                    }
                }
            }
            return list;
        }

        private static Payment ReadPayment(MySqlDataReader reader)
        {
            return new Payment
            {
                Id = reader.GetInt32("id"),
                ReservationId = reader.GetInt32("reservation_id"),
                GuestName = reader.IsDBNull(reader.GetOrdinal("guest_name")) ? "" : reader.GetString("guest_name"),
                RoomNumber = reader.IsDBNull(reader.GetOrdinal("room_number")) ? "" : reader.GetString("room_number"),
                Amount = reader.GetDecimal("amount"),
                PaymentMethod = reader.GetString("payment_method"),
                PaymentDate = reader.GetDateTime("payment_date"),
                Notes = reader.IsDBNull(reader.GetOrdinal("notes")) ? null : reader.GetString("notes"),
                CreatedBy = reader.IsDBNull(reader.GetOrdinal("created_by")) ? null : reader.GetInt32("created_by"),
            };
        }
    }
}
