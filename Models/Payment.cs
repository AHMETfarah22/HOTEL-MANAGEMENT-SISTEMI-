namespace ORYS.Models
{
    public class Payment
    {
        public int Id { get; set; }
        public int ReservationId { get; set; }
        public string GuestName { get; set; } = string.Empty;
        public string RoomNumber { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string PaymentMethod { get; set; } = "Nakit";
        public DateTime PaymentDate { get; set; }
        public string? Notes { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }

        public string PaymentMethodDisplay => PaymentMethod switch
        {
            "Nakit" => "💵 Nakit",
            "Kredi Karti" => "💳 Kredi Kartı",
            "Havale" => "🏦 Havale",
            "Diger" => "📋 Diğer",
            _ => PaymentMethod
        };
    }
}
