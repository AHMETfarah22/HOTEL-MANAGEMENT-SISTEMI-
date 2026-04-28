using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using ORYS.WebApi.Database;
using ORYS.WebApi.Models;

namespace ORYS.WebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RoomsController : ControllerBase
    {
        private readonly DbConnectionFactory _db;

        public RoomsController(DbConnectionFactory db)
        {
            _db = db;
        }

        /// <summary>
        /// GET /api/rooms/available?checkIn=2026-04-10&checkOut=2026-04-12
        /// Belirli tarih aralığında müsait odaları döndürür.
        /// </summary>
        [HttpGet("available")]
        public IActionResult GetAvailableRooms([FromQuery] string checkIn, [FromQuery] string checkOut)
        {
            if (!DateTime.TryParse(checkIn, out var checkInDate) ||
                !DateTime.TryParse(checkOut, out var checkOutDate))
                return BadRequest(new { error = "Geçersiz tarih formatı. Örnek: 2026-04-10" });

            if (checkInDate >= checkOutDate)
                return BadRequest(new { error = "Giriş tarihi çıkış tarihinden önce olmalıdır." });

            if (checkInDate < DateTime.Today)
                return BadRequest(new { error = "Giriş tarihi bugünden önce olamaz." });

            var rooms = new List<RoomDto>();

            try
            {
                using var conn = _db.GetConnection();
                conn.Open();

                string query = @"
                    SELECT r.id, r.room_number, r.floor, r.capacity, r.price_per_night, r.description,
                           rt.name AS room_type_name
                    FROM rooms r
                    LEFT JOIN room_types rt ON r.room_type_id = rt.id
                    WHERE r.status != 'Maintenance'
                    AND r.id NOT IN (
                        SELECT room_id FROM reservations
                        WHERE status IN ('Bekliyor','Onaylandi','GirisYapildi')
                        AND check_in_date < @checkOut AND check_out_date > @checkIn
                    )
                    AND r.id NOT IN (
                        SELECT room_id FROM online_reservations
                        WHERE status = 'Bekliyor'
                        AND check_in_date < @checkOut AND check_out_date > @checkIn
                    )
                    ORDER BY r.price_per_night ASC";

                using var cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@checkIn", checkInDate.ToString("yyyy-MM-dd"));
                cmd.Parameters.AddWithValue("@checkOut", checkOutDate.ToString("yyyy-MM-dd"));

                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    rooms.Add(new RoomDto
                    {
                        Id = reader.GetInt32("id"),
                        RoomNumber = reader.GetString("room_number"),
                        RoomTypeName = reader.IsDBNull(reader.GetOrdinal("room_type_name")) ? "" : reader.GetString("room_type_name"),
                        Floor = reader.GetInt32("floor"),
                        Capacity = reader.GetInt32("capacity"),
                        PricePerNight = reader.GetDecimal("price_per_night"),
                        Description = reader.IsDBNull(reader.GetOrdinal("description")) ? null : reader.GetString("description"),
                    });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"Veritabanı hatası: {ex.Message}" });
            }

            return Ok(rooms);
        }

        /// <summary>
        /// GET /api/rooms/all
        /// Tüm odaları (durumdan bağımsız) döndürür.
        /// </summary>
        [HttpGet("all")]
        public IActionResult GetAllRooms()
        {
            var rooms = new List<RoomDto>();
            try
            {
                using var conn = _db.GetConnection();
                conn.Open();
                string query = @"
                    SELECT r.id, r.room_number, r.floor, r.capacity, r.price_per_night, r.description, r.status,
                           rt.name AS room_type_name
                    FROM rooms r
                    LEFT JOIN room_types rt ON r.room_type_id = rt.id
                    WHERE r.status != 'Maintenance'
                    ORDER BY r.price_per_night ASC";
                using var cmd = new MySqlCommand(query, conn);
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    rooms.Add(new RoomDto
                    {
                        Id = reader.GetInt32("id"),
                        RoomNumber = reader.GetString("room_number"),
                        RoomTypeName = reader.IsDBNull(reader.GetOrdinal("room_type_name")) ? "" : reader.GetString("room_type_name"),
                        Floor = reader.GetInt32("floor"),
                        Capacity = reader.GetInt32("capacity"),
                        PricePerNight = reader.GetDecimal("price_per_night"),
                        Description = reader.IsDBNull(reader.GetOrdinal("description")) ? null : reader.GetString("description"),
                        Status = reader.IsDBNull(reader.GetOrdinal("status")) ? "Available" : reader.GetString("status"),
                    });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"Veritabanı hatası: {ex.Message}" });
            }
            return Ok(rooms);
        }

        /// <summary>
        /// GET /api/rooms/types
        /// Tüm oda tiplerini döndürür.
        /// </summary>
        [HttpGet("types")]
        public IActionResult GetRoomTypes()
        {
            var types = new List<object>();
            try
            {
                using var conn = _db.GetConnection();
                conn.Open();
                using var cmd = new MySqlCommand("SELECT id, name, description, base_price, capacity FROM room_types ORDER BY id", conn);
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    types.Add(new
                    {
                        id = reader.GetInt32("id"),
                        name = reader.GetString("name"),
                        description = reader.IsDBNull(reader.GetOrdinal("description")) ? null : reader.GetString("description"),
                        basePrice = reader.GetDecimal("base_price"),
                        capacity = reader.GetInt32("capacity"),
                    });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
            return Ok(types);
        }
    }
}
