using System;

namespace ORYS.Models
{
    public class MaintenanceRequest
    {
        public int Id { get; set; }
        public int? RoomId { get; set; }
        public string? RoomNumber { get; set; }
        public string Category { get; set; } = "Genel";
        public string Description { get; set; } = string.Empty;
        public string Priority { get; set; } = "Orta"; // Düşük, Orta, Yüksek, Acil
        public string Status { get; set; } = "Bekliyor"; // Bekliyor, Devam Ediyor, Tamamlandi, Iptal
        public int? ReportedById { get; set; }
        public string? ReportedByName { get; set; }
        public string? AssignedTo { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
