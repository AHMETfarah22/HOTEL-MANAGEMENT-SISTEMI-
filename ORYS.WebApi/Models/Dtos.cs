namespace ORYS.WebApi.Models
{
    /// <summary>
    /// Müsait oda bilgisi (web sitesine gönderilir)
    /// </summary>
    public class RoomDto
    {
        public int Id { get; set; }
        public string RoomNumber { get; set; } = "";
        public string RoomTypeName { get; set; } = "";
        public int Floor { get; set; }
        public int Capacity { get; set; }
        public decimal PricePerNight { get; set; }
        public string? Description { get; set; }
        public string? Status { get; set; }
        public string FloorDisplay => $"{Floor}. Kat";
        public string PriceDisplay => $"{PricePerNight:N0} ₺ / gece";
    }

    /// <summary>
    /// Online rezervasyon talebi (müşteriden gelir)
    /// </summary>
    public class OnlineReservationRequest
    {
        public string FullName { get; set; } = "";
        public string Email { get; set; } = "";
        public string? Phone { get; set; }
        public string? TcNo { get; set; }
        public string Nationality { get; set; } = "Türkiye";
        public int RoomId { get; set; }
        public string CheckInDate { get; set; } = "";   // yyyy-MM-dd
        public string CheckOutDate { get; set; } = "";  // yyyy-MM-dd
        public int Adults { get; set; } = 1;
        public int Children { get; set; } = 0;
        public string? Notes { get; set; }
    }

    /// <summary>
    /// Online rezervasyon listesi (admin paneline gönderilir)
    /// </summary>
    public class OnlineReservationDto
    {
        public int Id { get; set; }
        public string? ResCode { get; set; }
        public string FullName { get; set; } = "";
        public string Email { get; set; } = "";
        public string? Phone { get; set; }
        public string? TcNo { get; set; }
        public string Nationality { get; set; } = "";
        public int RoomId { get; set; }
        public string RoomNumber { get; set; } = "";
        public string RoomTypeName { get; set; } = "";
        public DateTime CheckInDate { get; set; }
        public DateTime CheckOutDate { get; set; }
        public int Adults { get; set; }
        public int Children { get; set; }
        public decimal TotalPrice { get; set; }
        public string? Notes { get; set; }
        public string Status { get; set; } = "Bekliyor";
        public int? InternalResId { get; set; }
        public string? RejectReason { get; set; }
        public DateTime CreatedAt { get; set; }
        public int NightCount => (CheckOutDate - CheckInDate).Days;
    }

    /// <summary>
    /// Admin onay/red işlemi için
    /// </summary>
    public class ApproveRejectRequest
    {
        public string? Reason { get; set; }
    }
}
