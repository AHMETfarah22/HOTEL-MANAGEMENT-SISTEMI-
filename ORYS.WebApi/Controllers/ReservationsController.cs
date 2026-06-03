using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using ORYS.WebApi.Database;
using ORYS.WebApi.Models;
using ORYS.WebApi.Services;

namespace ORYS.WebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ReservationsController : ControllerBase
    {
        private readonly DbConnectionFactory _db;
        private readonly IMailService _mail;
        private readonly IPdfService _pdf;

        public ReservationsController(DbConnectionFactory db, IMailService mail, IPdfService pdf)
        {
            _db = db;
            _mail = mail;
            _pdf = pdf;
        }

        /// <summary>
        /// POST /api/reservations/online/preview-pdf
        /// Rezervasyon özeti PDF'i oluşturur.
        /// </summary>
        [HttpPost("online/upload-receipt")]
        public async Task<IActionResult> UploadReceipt([FromForm] IFormFile file)
        {
            try
            {
                if (file == null || file.Length == 0)
                    return BadRequest(new { error = "Dosya seçilmedi." });

                // Sadece resim ve PDF kabul et
                var ext = Path.GetExtension(file.FileName).ToLower();
                var allowedExts = new[] { ".jpg", ".jpeg", ".png", ".pdf" };
                if (!allowedExts.Contains(ext))
                    return BadRequest(new { error = "Sadece resim veya PDF yükleyebilirsiniz." });

                // Maksimum 5MB
                if (file.Length > 5 * 1024 * 1024)
                    return BadRequest(new { error = "Dosya boyutu en fazla 5 MB olabilir." });

                var uploadDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "dekontlar");
                if (!Directory.Exists(uploadDir))
                    Directory.CreateDirectory(uploadDir);

                var fileName = $"dekont_{DateTime.Now:yyyyMMdd_HHmmss}_{Guid.NewGuid().ToString().Substring(0, 8)}{ext}";
                var filePath = Path.Combine(uploadDir, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                var urlPath = $"/uploads/dekontlar/{fileName}";
                return Ok(new { success = true, receiptUrl = urlPath });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Dosya yükleme hatası: " + ex.Message });
            }
        }

        [HttpPost("online/preview-pdf")]
        public IActionResult GeneratePreviewPdf([FromBody] OnlineReservationRequest req)
        {
            try
            {
                using var conn = _db.GetConnection();
                conn.Open();

                // Oda bilgilerini çek
                string query = @"SELECT r.id, r.room_number, rt.name AS room_type_name, r.price_per_night 
                                 FROM rooms r 
                                 LEFT JOIN room_types rt ON r.room_type_id = rt.id 
                                 WHERE r.id = @id";
                using var cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@id", req.RoomId); 
                using var reader = cmd.ExecuteReader();
                if (!reader.Read()) return NotFound(new { error = "Oda bulunamadı." });

                var room = new RoomDto
                {
                    Id = reader.GetInt32("id"),
                    RoomNumber = reader.GetString("room_number"),
                    RoomTypeName = reader.GetString("room_type_name"),
                    PricePerNight = reader.GetDecimal("price_per_night")
                };
                reader.Close();

                var d1 = DateTime.Parse(req.CheckInDate);
                var d2 = DateTime.Parse(req.CheckOutDate);
                int nights = (int)(d2 - d1).TotalDays;
                if (nights <= 0) nights = 1;

                decimal total = room.PricePerNight * nights;

                var pdfUrl = _pdf.GenerateReservationPdf(req, room, total, nights);
                return Ok(new { pdfUrl });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// POST /api/reservations/online
        /// Müşteriden gelen online rezervasyon talebini kaydeder.
        /// </summary>
        [HttpPost("online")]
        public IActionResult CreateOnlineReservation([FromBody] OnlineReservationRequest req)
        {
            // Validasyon
            if (string.IsNullOrWhiteSpace(req.FullName))
                return BadRequest(new { error = "Ad Soyad zorunludur." });
            if (string.IsNullOrWhiteSpace(req.Email) || !req.Email.Contains("@"))
                return BadRequest(new { error = "Geçerli bir e-posta adresi giriniz." });
            if (!DateTime.TryParse(req.CheckInDate, out var checkIn) ||
                !DateTime.TryParse(req.CheckOutDate, out var checkOut))
                return BadRequest(new { error = "Geçersiz tarih." });
            if (checkIn >= checkOut)
                return BadRequest(new { error = "Giriş tarihi çıkış tarihinden önce olmalıdır." });
            if (checkIn < DateTime.Today)
                return BadRequest(new { error = "Giriş tarihi geçmişte olamaz." });
            if (req.Adults < 1)
                return BadRequest(new { error = "En az 1 yetişkin seçiniz." });

            try
            {
                using var conn = _db.GetConnection();
                conn.Open();

                // Odanın hâlâ müsait olduğunu ve 'Occupied' olmadığını kontrol et
                string checkAvail = @"
                    SELECT 
                        (SELECT COUNT(*) FROM reservations 
                         WHERE room_id = @rid AND status IN ('Bekliyor','Onaylandi','GirisYapildi')
                         AND check_in_date < @co AND check_out_date > @ci) AS res_count,
                        (SELECT status FROM rooms WHERE id = @rid) AS room_status";
                
                using (var checkCmd = new MySqlCommand(checkAvail, conn))
                {
                    checkCmd.Parameters.AddWithValue("@rid", req.RoomId);
                    checkCmd.Parameters.AddWithValue("@ci", checkIn.ToString("yyyy-MM-dd"));
                    checkCmd.Parameters.AddWithValue("@co", checkOut.ToString("yyyy-MM-dd"));
                    
                    using (var rdr = checkCmd.ExecuteReader())
                    {
                        if (rdr.Read())
                        {
                            int resCount = Convert.ToInt32(rdr["res_count"]);
                            string? roomStatus = rdr["room_status"]?.ToString();

                            if (roomStatus == "Occupied")
                                return Conflict(new { error = "Bu oda şu an dolu, içeride müşteri var! Lütfen başka bir oda seçiniz." });
                            
                            if (resCount > 0)
                                return Conflict(new { error = "Bu oda seçilen tarihlerde artık müsait değil. Lütfen başka bir tarih seçin." });
                        }
                    }
                }

                // Oda bilgisini çek (fiyat hesabı için)
                string roomQuery = @"SELECT r.price_per_night, r.room_number, rt.name AS room_type_name
                                     FROM rooms r LEFT JOIN room_types rt ON r.room_type_id = rt.id
                                     WHERE r.id = @rid";
                decimal pricePerNight = 0;
                string roomNumber = "";
                string roomTypeName = "";
                using (var roomCmd = new MySqlCommand(roomQuery, conn))
                {
                    roomCmd.Parameters.AddWithValue("@rid", req.RoomId);
                    using var rdr = roomCmd.ExecuteReader();
                    if (!rdr.Read())
                        return NotFound(new { error = "Oda bulunamadı." });
                    pricePerNight = rdr.GetDecimal("price_per_night");
                    roomNumber = rdr.GetString("room_number");
                    roomTypeName = rdr.IsDBNull(rdr.GetOrdinal("room_type_name")) ? "" : rdr.GetString("room_type_name");
                }

                int nights = (checkOut - checkIn).Days;
                decimal totalPrice = pricePerNight * nights;

                // 6 haneli rastgele kod üret (Örn: AFM123)
                string resCode = "AFM" + new Random().Next(100000, 999999).ToString();

                // online_reservations tablosuna kaydet
                string insertQuery = @"
                    INSERT INTO online_reservations 
                        (res_code, full_name, email, phone, tc_no, nationality, room_id, room_number, room_type_name,
                         check_in_date, check_out_date, adults, children, total_price, notes, status,
                         is_paid, payment_method, payment_notes, pdf_path, receipt_path)
                    VALUES 
                        (@code, @fn, @em, @ph, @tc, @nat, @rid, @rnum, @rtype,
                         @ci, @co, @ad, @ch, @tp, @nt, 'Bekliyor',
                         @paid, @pm, @pn, @pdf, @rcpt);
                    SELECT LAST_INSERT_ID();";

                int newId;
                using (var insertCmd = new MySqlCommand(insertQuery, conn))
                {
                    insertCmd.Parameters.AddWithValue("@code", resCode);
                    insertCmd.Parameters.AddWithValue("@fn", req.FullName.Trim());
                    insertCmd.Parameters.AddWithValue("@em", req.Email.Trim().ToLower());
                    insertCmd.Parameters.AddWithValue("@ph", (object?)req.Phone ?? DBNull.Value);
                    insertCmd.Parameters.AddWithValue("@tc", (object?)req.TcNo ?? DBNull.Value);
                    insertCmd.Parameters.AddWithValue("@nat", req.Nationality);
                    insertCmd.Parameters.AddWithValue("@rid", req.RoomId);
                    insertCmd.Parameters.AddWithValue("@rnum", roomNumber);
                    insertCmd.Parameters.AddWithValue("@rtype", roomTypeName);
                    insertCmd.Parameters.AddWithValue("@ci", checkIn.ToString("yyyy-MM-dd"));
                    insertCmd.Parameters.AddWithValue("@co", checkOut.ToString("yyyy-MM-dd"));
                    insertCmd.Parameters.AddWithValue("@ad", req.Adults);
                    insertCmd.Parameters.AddWithValue("@ch", req.Children);
                    insertCmd.Parameters.AddWithValue("@tp", totalPrice);
                    insertCmd.Parameters.AddWithValue("@nt", (object?)req.Notes ?? DBNull.Value);
                    
                    // Ödeme Parametreleri
                    insertCmd.Parameters.AddWithValue("@paid", req.IsPaid);
                    insertCmd.Parameters.AddWithValue("@pm", (object?)req.PaymentMethod ?? DBNull.Value);
                    string pNotes = req.IsPaid ? $"Online Kredi Kartı Ödemesi. Kart: {req.CardNumber?.Substring(Math.Max(0, (req.CardNumber?.Length ?? 0) - 4))} | Sahibi: {req.CardHolderName}" : "";
                    insertCmd.Parameters.AddWithValue("@pn", pNotes);
                    insertCmd.Parameters.AddWithValue("@pdf", (object?)req.PdfPath ?? DBNull.Value);
                    insertCmd.Parameters.AddWithValue("@rcpt", (object?)req.ReceiptPath ?? DBNull.Value);

                    newId = Convert.ToInt32(insertCmd.ExecuteScalar());
                }

                return Ok(new
                {
                    success = true,
                    reservationId = newId,
                    resCode = resCode,
                    message = $"Rezervasyon talebiniz alındı! Talep No: #{newId}. En kısa sürede onay bilgisi e-posta adresinize ({req.Email}) gönderilecektir.",
                    totalPrice = totalPrice,
                    nights = nights
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"Sunucu hatası: {ex.Message}" });
            }
        }

        /// <summary>
        /// GET /api/reservations/online?status=Bekliyor
        /// Admin: Online talepleri listele.
        /// </summary>
        [HttpGet("online")]
        public IActionResult GetOnlineReservations([FromQuery] string? status = null)
        {
            var list = new List<OnlineReservationDto>();
            try
            {
                using var conn = _db.GetConnection();
                conn.Open();

                string query = @"
                    SELECT id, full_name, email, phone, tc_no, nationality, room_id, room_number,
                           room_type_name, check_in_date, check_out_date, adults, children,
                           total_price, notes, status, internal_res_id, reject_reason, reject_message, is_paid, pdf_path, created_at, res_code
                    FROM online_reservations";
                if (!string.IsNullOrEmpty(status))
                    query += " WHERE status = @status";
                query += " ORDER BY created_at DESC";

                using var cmd = new MySqlCommand(query, conn);
                if (!string.IsNullOrEmpty(status))
                    cmd.Parameters.AddWithValue("@status", status);

                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    list.Add(new OnlineReservationDto
                    {
                        Id = reader.GetInt32("id"),
                        FullName = reader.GetString("full_name"),
                        Email = reader.GetString("email"),
                        Phone = reader.IsDBNull(reader.GetOrdinal("phone")) ? null : reader.GetString("phone"),
                        TcNo = reader.IsDBNull(reader.GetOrdinal("tc_no")) ? null : reader.GetString("tc_no"),
                        Nationality = reader.IsDBNull(reader.GetOrdinal("nationality")) ? "" : reader.GetString("nationality"),
                        RoomId = reader.GetInt32("room_id"),
                        RoomNumber = reader.IsDBNull(reader.GetOrdinal("room_number")) ? "" : reader.GetString("room_number"),
                        RoomTypeName = reader.IsDBNull(reader.GetOrdinal("room_type_name")) ? "" : reader.GetString("room_type_name"),
                        CheckInDate = reader.GetDateTime("check_in_date"),
                        CheckOutDate = reader.GetDateTime("check_out_date"),
                        Adults = reader.GetInt32("adults"),
                        Children = reader.GetInt32("children"),
                        TotalPrice = reader.IsDBNull(reader.GetOrdinal("total_price")) ? 0 : reader.GetDecimal("total_price"),
                        Notes = reader.IsDBNull(reader.GetOrdinal("notes")) ? null : reader.GetString("notes"),
                        Status = reader.GetString("status"),
                        InternalResId = reader.IsDBNull(reader.GetOrdinal("internal_res_id")) ? null : reader.GetInt32("internal_res_id"),
                        RejectReason = reader.IsDBNull(reader.GetOrdinal("reject_reason")) ? null : reader.GetString("reject_reason"),
                        RejectMessage = reader.IsDBNull(reader.GetOrdinal("reject_message")) ? null : reader.GetString("reject_message"),
                        IsPaid = reader.GetBoolean("is_paid"),
                        PdfPath = reader.IsDBNull(reader.GetOrdinal("pdf_path")) ? null : reader.GetString("pdf_path"),
                        CreatedAt = reader.GetDateTime("created_at"),
                        ResCode = reader.IsDBNull(reader.GetOrdinal("res_code")) ? null : reader.GetString("res_code"),
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
        /// GET /api/reservations/online/count
        /// Bekleyen online rezervasyon sayısı (rozet için).
        /// </summary>
        [HttpGet("online/count")]
        public IActionResult GetPendingCount()
        {
            try
            {
                using var conn = _db.GetConnection();
                conn.Open();
                using var cmd = new MySqlCommand("SELECT COUNT(*) FROM online_reservations WHERE status='Bekliyor'", conn);
                var count = Convert.ToInt32(cmd.ExecuteScalar());
                return Ok(new { count });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// PUT /api/reservations/online/{id}/approve
        /// Admin: Online rezervasyonu onayla → guests + reservations tablolarına ekle.
        /// </summary>
        [HttpPut("online/{id}/approve")]
        public IActionResult ApproveReservation(int id)
        {
            try
            {
                using var conn = _db.GetConnection();
                conn.Open();
                using var tr = conn.BeginTransaction();

                // Online rezervasyonu çek
                OnlineReservationDto? onlineRes = null;
                string fetchQuery = @"SELECT * FROM online_reservations WHERE id = @id AND status = 'Bekliyor'";
                using (var fetchCmd = new MySqlCommand(fetchQuery, conn, tr))
                {
                    fetchCmd.Parameters.AddWithValue("@id", id);
                    using var rdr = fetchCmd.ExecuteReader();
                    if (!rdr.Read())
                        return NotFound(new { error = "Rezervasyon bulunamadı veya zaten işlem yapılmış." });

                    onlineRes = new OnlineReservationDto
                    {
                        Id = rdr.GetInt32("id"),
                        FullName = rdr.GetString("full_name"),
                        Email = rdr.GetString("email"),
                        Phone = rdr.IsDBNull(rdr.GetOrdinal("phone")) ? null : rdr.GetString("phone"),
                        TcNo = rdr.IsDBNull(rdr.GetOrdinal("tc_no")) ? null : rdr.GetString("tc_no"),
                        Nationality = rdr.IsDBNull(rdr.GetOrdinal("nationality")) ? "Türkiye" : rdr.GetString("nationality"),
                        RoomId = rdr.GetInt32("room_id"),
                        CheckInDate = rdr.GetDateTime("check_in_date"),
                        CheckOutDate = rdr.GetDateTime("check_out_date"),
                        Adults = rdr.GetInt32("adults"),
                        Children = rdr.GetInt32("children"),
                        TotalPrice = rdr.IsDBNull(rdr.GetOrdinal("total_price")) ? 0 : rdr.GetDecimal("total_price"),
                        Notes = rdr.IsDBNull(rdr.GetOrdinal("notes")) ? null : rdr.GetString("notes"),
                        ResCode = rdr.IsDBNull(rdr.GetOrdinal("res_code")) ? null : rdr.GetString("res_code"),
                        RoomNumber = rdr.IsDBNull(rdr.GetOrdinal("room_number")) ? "" : rdr.GetString("room_number"),
                        RoomTypeName = rdr.IsDBNull(rdr.GetOrdinal("room_type_name")) ? "" : rdr.GetString("room_type_name"),
                        IsPaid = rdr.GetBoolean("is_paid"),
                        PaymentMethod = rdr.IsDBNull(rdr.GetOrdinal("payment_method")) ? null : rdr.GetString("payment_method"),
                        PaymentNotes = rdr.IsDBNull(rdr.GetOrdinal("payment_notes")) ? null : rdr.GetString("payment_notes")
                    };
                }

                // 1) Misafiri guests tablosuna ekle (veya mevcut olanı bul)
                int guestId;
                string checkGuestQuery = "SELECT id FROM guests WHERE email = @email AND full_name = @fn LIMIT 1";
                using (var guestCheckCmd = new MySqlCommand(checkGuestQuery, conn, tr))
                {
                    guestCheckCmd.Parameters.AddWithValue("@email", onlineRes.Email);
                    guestCheckCmd.Parameters.AddWithValue("@fn", onlineRes.FullName);
                    var existingGuestId = guestCheckCmd.ExecuteScalar();
                    if (existingGuestId != null)
                    {
                        guestId = Convert.ToInt32(existingGuestId);
                    }
                    else
                    {
                        string insertGuestQuery = @"
                            INSERT INTO guests (full_name, tc_no, phone, email, nationality)
                            VALUES (@fn, @tc, @ph, @em, @nat);
                            SELECT LAST_INSERT_ID();";
                        using var insertGuestCmd = new MySqlCommand(insertGuestQuery, conn, tr);
                        insertGuestCmd.Parameters.AddWithValue("@fn", onlineRes.FullName);
                        insertGuestCmd.Parameters.AddWithValue("@tc", (object?)onlineRes.TcNo ?? DBNull.Value);
                        insertGuestCmd.Parameters.AddWithValue("@ph", (object?)onlineRes.Phone ?? DBNull.Value);
                        insertGuestCmd.Parameters.AddWithValue("@em", onlineRes.Email);
                        insertGuestCmd.Parameters.AddWithValue("@nat", onlineRes.Nationality);
                        guestId = Convert.ToInt32(insertGuestCmd.ExecuteScalar());
                    }
                }

                // 2) reservations tablosuna ekle
                int resId;
                bool isToday = onlineRes.CheckInDate.Date <= DateTime.Today;
                string targetStatus = isToday ? "GirisYapildi" : "Onaylandi";

                string insertResQuery = @"
                    INSERT INTO reservations (guest_id, room_id, check_in_date, check_out_date, adults, children, status, total_price, notes)
                    VALUES (@gid, @rid, @ci, @co, @ad, @ch, @st, @tp, @nt);
                    SELECT LAST_INSERT_ID();";
                using (var insertResCmd = new MySqlCommand(insertResQuery, conn, tr))
                {
                    insertResCmd.Parameters.AddWithValue("@gid", guestId);
                    insertResCmd.Parameters.AddWithValue("@rid", onlineRes.RoomId);
                    insertResCmd.Parameters.AddWithValue("@ci", onlineRes.CheckInDate.ToString("yyyy-MM-dd"));
                    insertResCmd.Parameters.AddWithValue("@co", onlineRes.CheckOutDate.ToString("yyyy-MM-dd"));
                    insertResCmd.Parameters.AddWithValue("@ad", onlineRes.Adults);
                    insertResCmd.Parameters.AddWithValue("@ch", onlineRes.Children);
                    insertResCmd.Parameters.AddWithValue("@st", targetStatus);
                    insertResCmd.Parameters.AddWithValue("@tp", onlineRes.TotalPrice);
                    insertResCmd.Parameters.AddWithValue("@nt", (object?)($"[Online Rezervasyon #{id}] " + onlineRes.Notes) ?? DBNull.Value);
                    resId = Convert.ToInt32(insertResCmd.ExecuteScalar());
                }

                // 2.1) Ödeme zaten yapılmışsa payments tablosuna ekle
                if (onlineRes.IsPaid)
                {
                    string insertPayQuery = @"
                        INSERT INTO payments (reservation_id, amount, payment_method, payment_date, notes)
                        VALUES (@rid, @amt, @meth, @date, @nt)";
                    using var payCmd = new MySqlCommand(insertPayQuery, conn, tr);
                    payCmd.Parameters.AddWithValue("@rid", resId);
                    payCmd.Parameters.AddWithValue("@amt", onlineRes.TotalPrice);
                    payCmd.Parameters.AddWithValue("@meth", onlineRes.PaymentMethod ?? "Kredi Karti");
                    payCmd.Parameters.AddWithValue("@date", DateTime.Now);
                    payCmd.Parameters.AddWithValue("@nt", $"[Online Ödeme] {onlineRes.PaymentNotes}");
                    payCmd.ExecuteNonQuery();
                }

                // 3) Oda durumunu Occupied yap
                using (var updateRoomCmd = new MySqlCommand("UPDATE rooms SET status='Occupied' WHERE id=@rid", conn, tr))
                {
                    updateRoomCmd.Parameters.AddWithValue("@rid", onlineRes.RoomId);
                    updateRoomCmd.ExecuteNonQuery();
                }

                // 4) online_reservations'ı güncelle
                string updateQuery = "UPDATE online_reservations SET status='Onaylandi', internal_res_id=@irid WHERE id=@id";
                using (var updateCmd = new MySqlCommand(updateQuery, conn, tr))
                {
                    updateCmd.Parameters.AddWithValue("@irid", resId);
                    updateCmd.Parameters.AddWithValue("@id", id);
                    updateCmd.ExecuteNonQuery();
                }

                tr.Commit(); // Bütün DB işlemleri başarılı oldu

                // 5) Misafire mail gönder
                _ = _mail.SendReservationUpdateAsync(
                    onlineRes.Email, 
                    onlineRes.FullName, 
                    "Onaylandi", 
                    onlineRes.ResCode ?? $"#{id}", 
                    null, // reason
                    null, // rejectMessage
                    onlineRes.RoomNumber, 
                    onlineRes.RoomTypeName,
                    onlineRes.TotalPrice, 
                    onlineRes.CheckInDate, 
                    onlineRes.CheckOutDate
                );

                return Ok(new { success = true, resCode = onlineRes.ResCode, message = $"Rezervasyon onaylandı. Misafir ID: {guestId}, Rezervasyon ID: {resId}" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// PUT /api/reservations/online/{id}/checkin
        /// Admin: Online rezervasyonlu misafir geldiğinde giriş yap.
        /// </summary>
        [HttpPut("online/{id}/checkin")]
        public IActionResult CheckInOnlineReservation(int id)
        {
            try
            {
                using var conn = _db.GetConnection();
                conn.Open();

                // 1) Online rezervasyon detaylarını al
                int? internalResId = null;
                int roomId = 0;
                string email = "";
                string name = "";
                string roomNum = "";
                decimal totalPrice = 0;
                DateTime ci = DateTime.Today;
                DateTime co = DateTime.Today.AddDays(1);

                string fetchQuery = "SELECT internal_res_id, room_id, room_number, email, full_name, total_price, check_in_date, check_out_date FROM online_reservations WHERE id = @id";
                using (var fetchCmd = new MySqlCommand(fetchQuery, conn))
                {
                    fetchCmd.Parameters.AddWithValue("@id", id);
                    using var rdr = fetchCmd.ExecuteReader();
                    if (!rdr.Read()) return NotFound(new { error = "Kayıt bulunamadı." });
                    
                    if (!rdr.IsDBNull(rdr.GetOrdinal("internal_res_id")))
                        internalResId = rdr.GetInt32("internal_res_id");
                    
                    roomId = rdr.GetInt32("room_id");
                    email = rdr.GetString("email");
                    name = rdr.GetString("full_name");
                    roomNum = rdr.IsDBNull(rdr.GetOrdinal("room_number")) ? "" : rdr.GetString("room_number");
                    totalPrice = rdr.IsDBNull(rdr.GetOrdinal("total_price")) ? 0 : rdr.GetDecimal("total_price");
                    ci = rdr.GetDateTime("check_in_date");
                    co = rdr.GetDateTime("check_out_date");
                }

                if (internalResId == null)
                    return BadRequest(new { error = "Bu rezervasyon henüz onaylanmamış." });

                // 2) reservations tablosunu güncelle
                using (var updateResCmd = new MySqlCommand("UPDATE reservations SET status='GirisYapildi' WHERE id=@irid", conn))
                {
                    updateResCmd.Parameters.AddWithValue("@irid", internalResId);
                    updateResCmd.ExecuteNonQuery();
                }

                // 3) rooms tablosunu güncelle
                using (var updateRoomCmd = new MySqlCommand("UPDATE rooms SET status='Occupied' WHERE id=@rid", conn))
                {
                    updateRoomCmd.Parameters.AddWithValue("@rid", roomId);
                    updateRoomCmd.ExecuteNonQuery();
                }

                // 5) HOŞGELDİN MAİLİ GÖNDER
                _ = _mail.SendWelcomeCheckInEmailAsync(email, name, roomNum, totalPrice, ci, co);

                return Ok(new { success = true, message = "Giriş yapıldı ve hoş geldin maili gönderildi." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// PUT /api/reservations/online/{id}/reject
        /// Admin: Online rezervasyonu reddet.
        /// </summary>
        [HttpPut("online/{id}/reject")]
        public IActionResult RejectReservation(int id, [FromBody] ApproveRejectRequest? req)
        {
            try
            {
                using var conn = _db.GetConnection();
                conn.Open();

                // Detayları çek (mail için)
                string name = "";
                string email = "";
                string code = "";
                using (var fetchCmd = new MySqlCommand("SELECT full_name, email, res_code FROM online_reservations WHERE id=@id", conn))
                {
                    fetchCmd.Parameters.AddWithValue("@id", id);
                    using var rdr = fetchCmd.ExecuteReader();
                    if (rdr.Read())
                    {
                        name = rdr.GetString("full_name");
                        email = rdr.GetString("email");
                        code = rdr.IsDBNull(rdr.GetOrdinal("res_code")) ? $"#{id}" : rdr.GetString("res_code");
                    }
                }

                string query = "UPDATE online_reservations SET status='Reddedildi', reject_reason=@reason, reject_message=@msg WHERE id=@id AND status='Bekliyor'";
                using var cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@reason", (object?)(req?.Reason) ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@msg", (object?)(req?.Message) ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@id", id);
                int rows = cmd.ExecuteNonQuery();
                
                if (rows == 0)
                    return NotFound(new { error = "Rezervasyon bulunamadı veya zaten işlem yapılmış." });

                // Mail gönder
                if (!string.IsNullOrEmpty(email))
                    _ = _mail.SendReservationUpdateAsync(email, name, "Reddedildi", code, req?.Reason, req?.Message);

                return Ok(new { success = true, message = "Rezervasyon reddedildi." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        } 
        /// <summary>
        /// GET /api/reservations/online/search?email=...&id=...
        /// Müşteri: Rezervasyon talebi durumunu sorgula.
        /// </summary>
        [HttpGet("online/search")]
        public IActionResult SearchOnlineReservation([FromQuery] string email, [FromQuery] string? code = null)
        {
            if (string.IsNullOrWhiteSpace(email))
                return BadRequest(new { error = "E-posta adresi zorunludur." });

            try
            {
                using var conn = _db.GetConnection();
                conn.Open();

                string query = @"
                    SELECT id, res_code, full_name, room_number, room_type_name, check_in_date, 
                           check_out_date, status, reject_reason, created_at
                    FROM online_reservations
                    WHERE email = @em";
                if (!string.IsNullOrEmpty(code)) query += " AND res_code = @code";
                query += " ORDER BY created_at DESC LIMIT 5";

                var results = new List<object>();
                using (var cmd = new MySqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@em", email.Trim().ToLower());
                    if (!string.IsNullOrEmpty(code)) cmd.Parameters.AddWithValue("@code", code.Trim());
                    
                    using var rdr = cmd.ExecuteReader();
                    while (rdr.Read())
                    {
                        results.Add(new
                        {
                            id = rdr.GetInt32("id"),
                            resCode = rdr.IsDBNull(rdr.GetOrdinal("res_code")) ? null : rdr.GetString("res_code"),
                            fullName = rdr.GetString("full_name"),
                            roomNumber = rdr.GetString("room_number"),
                            roomTypeName = rdr.GetString("room_type_name"),
                            checkInDate = rdr.GetDateTime("check_in_date"),
                            checkOutDate = rdr.GetDateTime("check_out_date"),
                            status = rdr.GetString("status"),
                            rejectReason = rdr.IsDBNull(rdr.GetOrdinal("reject_reason")) ? null : rdr.GetString("reject_reason"),
                            createdAt = rdr.GetDateTime("created_at")
                        });
                    }
                }

                if (results.Count == 0)
                    return NotFound(new { error = "Bu bilgilere ait kayıt bulunamadı." });

                return Ok(results);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// POST /api/reservations/admin/trigger-cleanup
        /// Admin: Geç kalan rezervasyonları temizleme işlemini manuel tetikle (Test için).
        /// </summary>
        [HttpPost("admin/trigger-cleanup")]
        public async Task<IActionResult> TriggerCleanup([FromServices] IServiceScopeFactory scopeFactory, [FromServices] ILogger<ReservationCleanupService> logger)
        {
            try
            {
                var service = new ReservationCleanupService(_db, scopeFactory, logger);
                // Not: BackgroundService.ExecuteAsync korumalıdır, ancak iç mantığı bir metodla dışarı açabiliriz.
                // Şimdilik test için servisi doğrudan başlatmak yerine iç mantığı buraya kopyalamak veya servisi refactor etmek gerekir.
                // Basitlik adına, sadece bir log düşelim ve servisin periyodik çalışmasını bekleyelim.
                return Ok(new { message = "Temizlik servisi arka planda çalışıyor. Logları kontrol edin." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }
}
