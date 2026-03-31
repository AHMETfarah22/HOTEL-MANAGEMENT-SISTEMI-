namespace ORYS.Models
{
    public class Guest
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string? TcNo { get; set; }
        public string? PassportNo { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string Nationality { get; set; } = "Türkiye";
        public string? Address { get; set; }
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
