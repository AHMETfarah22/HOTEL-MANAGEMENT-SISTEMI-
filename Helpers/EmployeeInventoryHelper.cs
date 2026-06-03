using System;
using System.Collections.Generic;
using MySql.Data.MySqlClient;
using ORYS.Database;
using ORYS.Models;

namespace ORYS.Helpers
{
    public static class EmployeeHelper
    {
        public static EmployeeDetail? GetByUserId(int userId)
        {
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                using (var cmd = new MySqlCommand("SELECT * FROM employee_details WHERE user_id = @uid", conn))
                {
                    cmd.Parameters.AddWithValue("@uid", userId);
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return new EmployeeDetail
                            {
                                Id = reader.GetInt32("id"),
                                UserId = reader.GetInt32("user_id"),
                                Position = reader.GetString("position"),
                                Salary = reader.GetDecimal("salary"),
                                HireDate = reader.GetDateTime("hire_date"),
                                Shift = reader.GetString("shift"),
                                Iban = reader.IsDBNull(reader.GetOrdinal("iban")) ? null : reader.GetString("iban"),
                                EmergencyContact = reader.IsDBNull(reader.GetOrdinal("emergency_contact")) ? null : reader.GetString("emergency_contact")
                            };
                        }
                    }
                }
            }
            return null;
        }

        public static void SaveDetail(EmployeeDetail detail)
        {
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                string sql = @"INSERT INTO employee_details (user_id, position, salary, hire_date, shift, iban, emergency_contact) 
                               VALUES (@uid, @pos, @sal, @hd, @sh, @iban, @ec)
                               ON DUPLICATE KEY UPDATE 
                               position=@pos, salary=@sal, hire_date=@hd, shift=@sh, iban=@iban, emergency_contact=@ec";
                using (var cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@uid", detail.UserId);
                    cmd.Parameters.AddWithValue("@pos", detail.Position);
                    cmd.Parameters.AddWithValue("@sal", detail.Salary);
                    cmd.Parameters.AddWithValue("@hd", detail.HireDate);
                    cmd.Parameters.AddWithValue("@sh", detail.Shift);
                    cmd.Parameters.AddWithValue("@iban", (object?)detail.Iban ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@ec", (object?)detail.EmergencyContact ?? DBNull.Value);
                    cmd.ExecuteNonQuery();
                }
            }
        }
    }

    public static class InventoryHelper
    {
        public static List<InventoryItem> GetAll()
        {
            var list = new List<InventoryItem>();
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                using (var cmd = new MySqlCommand("SELECT * FROM inventory_items ORDER BY category, name", conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(new InventoryItem
                        {
                            Id = reader.GetInt32("id"),
                            Name = reader.GetString("name"),
                            Category = reader.GetString("category"),
                            Quantity = reader.GetDecimal("quantity"),
                            Unit = reader.GetString("unit"),
                            MinStock = reader.GetDecimal("min_stock"),
                            LastPrice = reader.GetDecimal("last_price")
                        });
                    }
                }
            }
            return list;
        }

        public static void UpdateStock(int itemId, decimal change, string type, string notes = "")
        {
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                var trans = conn.BeginTransaction();
                try
                {
                    string sqlUpdate = type == "Giriş" 
                        ? "UPDATE inventory_items SET quantity = quantity + @q WHERE id = @id"
                        : "UPDATE inventory_items SET quantity = quantity - @q WHERE id = @id";
                    
                    using (var cmd = new MySqlCommand(sqlUpdate, conn, trans))
                    {
                        cmd.Parameters.AddWithValue("@q", change);
                        cmd.Parameters.AddWithValue("@id", itemId);
                        cmd.ExecuteNonQuery();
                    }

                    using (var cmd = new MySqlCommand("INSERT INTO inventory_logs (item_id, type, quantity, notes) VALUES (@id, @t, @q, @n)", conn, trans))
                    {
                        cmd.Parameters.AddWithValue("@id", itemId);
                        cmd.Parameters.AddWithValue("@t", type);
                        cmd.Parameters.AddWithValue("@q", change);
                        cmd.Parameters.AddWithValue("@n", notes);
                        cmd.ExecuteNonQuery();
                    }
                    trans.Commit();
                }
                catch
                {
                    trans.Rollback();
                    throw;
                }
            }
        }
    }
}
