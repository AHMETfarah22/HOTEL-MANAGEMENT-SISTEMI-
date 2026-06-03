using System;

namespace ORYS.Models
{
    public class EmployeeDetail
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Position { get; set; } = string.Empty;
        public decimal Salary { get; set; }
        public DateTime HireDate { get; set; }
        public string Shift { get; set; } = "Gündüz"; // Gündüz, Gece, Vardiyalı
        public string? Iban { get; set; }
        public string? EmergencyContact { get; set; }
    }

    public class InventoryItem
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public string Unit { get; set; } = "Adet";
        public decimal MinStock { get; set; }
        public decimal LastPrice { get; set; }
    }
}
