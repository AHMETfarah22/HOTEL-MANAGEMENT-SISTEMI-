using System;

namespace ORYS.Models
{
    public class HousekeepingLog
    {
        public int Id { get; set; }
        public int RoomId { get; set; }
        public string RoomNumber { get; set; } = string.Empty;
        public int? StaffId { get; set; }
        public string? StaffName { get; set; }
        public string StatusFrom { get; set; } = string.Empty;
        public string StatusTo { get; set; } = string.Empty;
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
