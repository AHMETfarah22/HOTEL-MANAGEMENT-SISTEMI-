using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using ORYS.WebApi.Database;
using ORYS.WebApi.Models;

namespace ORYS.WebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PaymentsController : ControllerBase
    {
        private readonly DbConnectionFactory _db;

        public PaymentsController(DbConnectionFactory db)
        {
            _db = db;
        }

        [HttpGet("balance/{resId}")]
        public IActionResult GetBalance(int resId)
        {
            try
            {
                using var conn = _db.GetConnection();
                conn.Open();
                
                // Oda Toplam
                decimal total = 0;
                string guestName = "";
                using (var cmd = new MySqlCommand("SELECT rv.total_price, g.full_name FROM reservations rv JOIN guests g ON rv.guest_id = g.id WHERE rv.id = @id", conn))
                {
                    cmd.Parameters.AddWithValue("@id", resId);
                    using var rdr = cmd.ExecuteReader();
                    if (rdr.Read()) {
                        total = rdr.GetDecimal(0);
                        guestName = rdr.GetString(1);
                    } else return NotFound();
                }

                // Ekstralar
                decimal extras = 0;
                using (var cmd = new MySqlCommand("SELECT SUM(total_amount) FROM restaurant_orders WHERE room_id = (SELECT room_id FROM reservations WHERE id = @id) AND status = 'OdayaYaz'", conn))
                {
                    cmd.Parameters.AddWithValue("@id", resId);
                    var res = cmd.ExecuteScalar();
                    extras = res == DBNull.Value ? 0 : Convert.ToDecimal(res);
                }

                // Ödenen
                decimal paid = 0;
                using (var cmd = new MySqlCommand("SELECT SUM(amount) FROM payments WHERE reservation_id = @id", conn))
                {
                    cmd.Parameters.AddWithValue("@id", resId);
                    var res = cmd.ExecuteScalar();
                    paid = res == DBNull.Value ? 0 : Convert.ToDecimal(res);
                }

                return Ok(new { 
                    reservationId = resId,
                    guestName,
                    totalRoomPrice = total,
                    extras,
                    totalPaid = paid,
                    balance = (total + extras) - paid 
                });
            }
            catch (Exception ex) { return StatusCode(500, new { error = ex.Message }); }
        }

        [HttpPost("checkout-pay")]
        public IActionResult ProcessCheckoutPayment([FromBody] CheckoutPaymentRequest req)
        {
            try
            {
                using var conn = _db.GetConnection();
                conn.Open();
                using var tr = conn.BeginTransaction();

                try
                {
                    // 1) Ödemeyi kaydet
                    string sqlPay = @"INSERT INTO payments (reservation_id, amount, payment_method, notes) 
                                      VALUES (@rid, @amt, 'Kredi Karti', @nt)";
                    using (var cmd = new MySqlCommand(sqlPay, conn, tr))
                    {
                        cmd.Parameters.AddWithValue("@rid", req.ReservationId);
                        cmd.Parameters.AddWithValue("@amt", req.Amount);
                        cmd.Parameters.AddWithValue("@nt", $"[Online Checkout] Kart: {req.CardNumber.Substring(Math.Max(0, req.CardNumber.Length - 4))} Sahibi: {req.CardHolderName}");
                        cmd.ExecuteNonQuery();
                    }

                    // 2) Eğer borç sıfırlandıysa restoran siparişlerini 'Odendi' yap
                    // Bu opsiyonel ama iyi bir pratik
                    string sqlUpdateOrders = "UPDATE restaurant_orders SET status = 'Odendi' WHERE room_id = (SELECT room_id FROM reservations WHERE id = @rid) AND status = 'OdayaYaz'";
                    using (var cmd = new MySqlCommand(sqlUpdateOrders, conn, tr))
                    {
                        cmd.Parameters.AddWithValue("@rid", req.ReservationId);
                        cmd.ExecuteNonQuery();
                    }

                    tr.Commit();
                    return Ok(new { success = true, message = "Ödeme başarıyla alındı." });
                }
                catch (Exception ex)
                {
                    tr.Rollback();
                    throw ex;
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }
        [HttpGet]
        public IActionResult GetAllPayments(
            [FromQuery] string? startDate = null,
            [FromQuery] string? endDate = null,
            [FromQuery] string? search = null,
            [FromQuery] string? method = null)
        {
            var list = new List<PaymentDto>();
            try
            {
                using var conn = _db.GetConnection();
                conn.Open();

                string query = @"
                    SELECT py.id, py.reservation_id, py.amount, py.payment_method, 
                           py.payment_date, py.notes,
                           g.full_name AS guest_name, rm.room_number
                    FROM payments py
                    LEFT JOIN reservations rv ON py.reservation_id = rv.id
                    LEFT JOIN guests g ON rv.guest_id = g.id
                    LEFT JOIN rooms rm ON rv.room_id = rm.id
                    WHERE 1=1";

                if (!string.IsNullOrEmpty(startDate))
                    query += " AND py.payment_date >= @start";
                if (!string.IsNullOrEmpty(endDate))
                    query += " AND py.payment_date <= @end";
                if (!string.IsNullOrEmpty(method) && method != "Tüm Yöntemler")
                    query += " AND py.payment_method = @method";
                if (!string.IsNullOrEmpty(search))
                {
                    query += @" AND (g.full_name LIKE @search 
                                OR rm.room_number LIKE @search 
                                OR py.notes LIKE @search 
                                OR py.id = @searchVal 
                                OR py.reservation_id = @searchVal)";
                }

                query += " ORDER BY py.payment_date DESC";

                using var cmd = new MySqlCommand(query, conn);
                if (!string.IsNullOrEmpty(startDate))
                    cmd.Parameters.AddWithValue("@start", startDate + " 00:00:00");
                if (!string.IsNullOrEmpty(endDate))
                    cmd.Parameters.AddWithValue("@end", endDate + " 23:59:59");
                if (!string.IsNullOrEmpty(method) && method != "Tüm Yöntemler")
                    cmd.Parameters.AddWithValue("@method", method);
                if (!string.IsNullOrEmpty(search))
                {
                    cmd.Parameters.AddWithValue("@search", $"%{search}%");
                    int.TryParse(search, out int sVal);
                    cmd.Parameters.AddWithValue("@searchVal", sVal);
                }

                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    list.Add(new PaymentDto
                    {
                        Id = reader.GetInt32("id"),
                        ReservationId = reader.GetInt32("reservation_id"),
                        GuestName = reader.IsDBNull(reader.GetOrdinal("guest_name")) ? "" : reader.GetString("guest_name"),
                        RoomNumber = reader.IsDBNull(reader.GetOrdinal("room_number")) ? "" : reader.GetString("room_number"),
                        Amount = reader.GetDecimal("amount"),
                        PaymentMethod = reader.GetString("payment_method"),
                        PaymentDate = reader.GetDateTime("payment_date"),
                        Notes = reader.IsDBNull(reader.GetOrdinal("notes")) ? null : reader.GetString("notes"),
                    });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
            return Ok(list);
        }

        /// <summary>
        /// GET /api/payments/stats
        /// Ödeme yöntemi istatistiklerini döndürür.
        /// </summary>
        [HttpGet("stats")]
        public IActionResult GetPaymentStats()
        {
            var stats = new List<object>();
            try
            {
                using var conn = _db.GetConnection();
                conn.Open();
                string query = "SELECT payment_method, SUM(amount) AS total, COUNT(*) AS cnt FROM payments GROUP BY payment_method ORDER BY total DESC";
                using var cmd = new MySqlCommand(query, conn);
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    stats.Add(new
                    {
                        method = reader.GetString("payment_method"),
                        total = reader.GetDecimal("total"),
                        count = reader.GetInt32("cnt")
                    });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
            return Ok(stats);
        }
    }
}
