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
        public bool IncludeBreakfast { get; set; } = false;
        public decimal BreakfastPrice { get; set; } = 0;
        public bool IncludeDinner { get; set; } = false;
        public decimal DinnerPrice { get; set; } = 0;
        public string? Notes { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public int NightCount => (CheckOutDate - CheckInDate).Days;

        public string DropdownDisplay => $"#{Id} {GuestName} ({RoomNumber}) - {StatusDisplay}";

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
