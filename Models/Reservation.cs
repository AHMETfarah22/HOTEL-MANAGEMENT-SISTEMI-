namespace ORYS.Models
{
    public class Reservation
    {
        public int Id { get; set; }
        public int GuestId { get; set; }
        public string GuestName { get; set; } = string.Empty;
        public int RoomId { get; set; }
        public string RoomNumber { get; set; } = string.Empty;
        public DateTime CheckInDate { get; set; }
        public DateTime CheckOutDate { get; set; }
        public int Adults { get; set; } = 1;
        public int Children { get; set; } = 0;
        public string Status { get; set; } = "Bekliyor";
        public decimal TotalPrice { get; set; }
        public string? Notes { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public int NightCount => (CheckOutDate - CheckInDate).Days;

        public string StatusDisplay => Status switch
        {
            "Bekliyor" => "⏳ Bekliyor",
            "Onaylandi" => "✅ Onaylandı",
            "GirisYapildi" => "🏨 Giriş Yapıldı",
            "CikisYapildi" => "🚪 Çıkış Yapıldı",
            "Iptal" => "❌ İptal",
            _ => Status
        };
    }
}
