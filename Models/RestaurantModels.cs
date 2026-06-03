namespace ORYS.Models
{
    public class RestaurantProduct
    {
        public int Id { get; set; }
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class RestaurantOrder
    {
        public int Id { get; set; }
        public int? RoomId { get; set; }
        public string? RoomNumber { get; set; }
        public string? TableNumber { get; set; }
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = "Aktif"; // Aktif, Tamamlandi, Iptal, OdayaYazildi
        public DateTime CreatedAt { get; set; }
        public List<RestaurantOrderItem> Items { get; set; } = new List<RestaurantOrderItem>();
    }

    public class RestaurantOrderItem
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal Total => Quantity * UnitPrice;
    }
}
