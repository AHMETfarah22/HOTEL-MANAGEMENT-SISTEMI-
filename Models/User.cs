namespace ORYS.Models
{
    /// <summary>
    /// Kullanıcı modeli - users tablosuna karşılık gelir
    /// </summary>
    public class User
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        /// <summary>
        /// Kullanıcının admin olup olmadığını kontrol eder
        /// </summary>
        public bool IsAdmin => Role == "Admin";

        /// <summary>
        /// Kullanıcının resepsiyonist olup olmadığını kontrol eder
        /// </summary>
        public bool IsReceptionist => Role == "Resepsiyonist";

        /// <summary>
        /// Kullanıcının muhasebeci olup olmadığını kontrol eder
        /// </summary>
        public bool IsAccountant => Role == "Muhasebe";
    }
}
