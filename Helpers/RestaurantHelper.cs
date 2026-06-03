using System;
using System.Collections.Generic;
using MySql.Data.MySqlClient;
using ORYS.Database;
using ORYS.Models;

namespace ORYS.Helpers
{
    public static class RestaurantHelper
    {
        public static List<RestaurantProduct> GetAllProducts()
        {
            var products = new List<RestaurantProduct>();
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                string query = @"SELECT p.*, c.name as category_name 
                                 FROM restaurant_products p
                                 LEFT JOIN restaurant_categories c ON p.category_id = c.id
                                 WHERE p.is_active = 1";
                using (var cmd = new MySqlCommand(query, conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        products.Add(new RestaurantProduct
                        {
                            Id = reader.GetInt32("id"),
                            CategoryId = reader.GetInt32("category_id"),
                            CategoryName = reader.GetString("category_name"),
                            Name = reader.GetString("name"),
                            Price = reader.GetDecimal("price"),
                            IsActive = reader.GetBoolean("is_active")
                        });
                    }
                }
            }
            return products;
        }

        public static List<string> GetCategories()
        {
            var cats = new List<string>();
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                using (var cmd = new MySqlCommand("SELECT name FROM restaurant_categories", conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read()) cats.Add(reader.GetString(0));
                }
            }
            return cats;
        }

        public static int CreateOrder(RestaurantOrder order)
        {
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                var trans = conn.BeginTransaction();
                try
                {
                    string sqlOrder = @"INSERT INTO restaurant_orders (room_id, table_number, total_amount, status) 
                                        VALUES (@rid, @tbl, @amt, @stat); SELECT LAST_INSERT_ID();";
                    int orderId;
                    using (var cmd = new MySqlCommand(sqlOrder, conn, trans))
                    {
                        cmd.Parameters.AddWithValue("@rid", (object?)order.RoomId ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@tbl", (object?)order.TableNumber ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@amt", order.TotalAmount);
                        cmd.Parameters.AddWithValue("@stat", order.Status);
                        orderId = Convert.ToInt32(cmd.ExecuteScalar());
                    }

                    foreach (var item in order.Items)
                    {
                        string sqlItem = @"INSERT INTO restaurant_order_items (order_id, product_id, quantity, unit_price) 
                                           VALUES (@oid, @pid, @q, @up)";
                        using (var cmd = new MySqlCommand(sqlItem, conn, trans))
                        {
                            cmd.Parameters.AddWithValue("@oid", orderId);
                            cmd.Parameters.AddWithValue("@pid", item.ProductId);
                            cmd.Parameters.AddWithValue("@q", item.Quantity);
                            cmd.Parameters.AddWithValue("@up", item.UnitPrice);
                            cmd.ExecuteNonQuery();
                        }
                    }
                    trans.Commit();
                    return orderId;
                }
                catch
                {
                    trans.Rollback();
                    throw;
                }
            }
        }

        public static List<RestaurantOrder> GetActiveOrders()
        {
            var orders = new List<RestaurantOrder>();
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                string query = @"SELECT o.*, r.room_number 
                                 FROM restaurant_orders o
                                 LEFT JOIN rooms r ON o.room_id = r.id
                                 WHERE o.status = 'Aktif'
                                 ORDER BY o.created_at DESC";
                using (var cmd = new MySqlCommand(query, conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        orders.Add(new RestaurantOrder
                        {
                            Id = reader.GetInt32("id"),
                            RoomId = reader.IsDBNull(reader.GetOrdinal("room_id")) ? (int?)null : reader.GetInt32("room_id"),
                            RoomNumber = reader.IsDBNull(reader.GetOrdinal("room_number")) ? null : reader.GetString("room_number"),
                            TableNumber = reader.IsDBNull(reader.GetOrdinal("table_number")) ? null : reader.GetString("table_number"),
                            TotalAmount = reader.GetDecimal("total_amount"),
                            Status = reader.GetString("status"),
                            CreatedAt = reader.GetDateTime("created_at")
                        });
                    }
                }
            }
            return orders;
        }

        public static void UpdateOrderStatus(int orderId, string status)
        {
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                using (var cmd = new MySqlCommand("UPDATE restaurant_orders SET status=@s WHERE id=@id", conn))
                {
                    cmd.Parameters.AddWithValue("@s", status);
                    cmd.Parameters.AddWithValue("@id", orderId);
                    cmd.ExecuteNonQuery();
                }
            }
        }
        public static List<RestaurantOrder> GetOrdersByRoom(int roomId)
        {
            var orders = new List<RestaurantOrder>();
            using (var conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                string query = @"SELECT o.*, r.room_number 
                                 FROM restaurant_orders o
                                 LEFT JOIN rooms r ON o.room_id = r.id
                                 WHERE o.room_id = @rid AND o.status != 'Iptal'
                                 ORDER BY o.created_at DESC";
                using (var cmd = new MySqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@rid", roomId);
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            orders.Add(new RestaurantOrder
                            {
                                Id = reader.GetInt32("id"),
                                RoomId = reader.IsDBNull(reader.GetOrdinal("room_id")) ? (int?)null : reader.GetInt32("room_id"),
                                RoomNumber = reader.IsDBNull(reader.GetOrdinal("room_number")) ? null : reader.GetString("room_number"),
                                TableNumber = reader.IsDBNull(reader.GetOrdinal("table_number")) ? null : reader.GetString("table_number"),
                                TotalAmount = reader.GetDecimal("total_amount"),
                                Status = reader.GetString("status"),
                                CreatedAt = reader.GetDateTime("created_at"),
                                Items = new List<RestaurantOrderItem>()
                            });
                        }
                    }
                }

                // Sipariş detaylarını çek
                if (orders.Count > 0)
                {
                    string oids = string.Join(",", orders.Select(o => o.Id));
                    string itemQuery = $@"SELECT i.*, p.name 
                                          FROM restaurant_order_items i 
                                          JOIN restaurant_products p ON i.product_id = p.id 
                                          WHERE i.order_id IN ({oids})";
                    using (var cmd = new MySqlCommand(itemQuery, conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            int oid = reader.GetInt32("order_id");
                            var item = new RestaurantOrderItem
                            {
                                Id = reader.GetInt32("id"),
                                OrderId = oid,
                                ProductId = reader.GetInt32("product_id"),
                                ProductName = reader.GetString("name"),
                                Quantity = reader.GetInt32("quantity"),
                                UnitPrice = reader.GetDecimal("unit_price")
                            };
                            orders.FirstOrDefault(o => o.Id == oid)?.Items.Add(item);
                        }
                    }
                }
            }
            return orders;
        }
    }
}
