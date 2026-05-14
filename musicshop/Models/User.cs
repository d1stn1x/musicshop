using System;

namespace musicshop.Models
{
    public class User
    {
        public int       UserID       { get; set; }
        public string    Username     { get; set; } = string.Empty;
        public string    PasswordHash { get; set; } = string.Empty;
        public string?   FullName     { get; set; }
        public string    Role         { get; set; } = "Клиент";
        public string?   Email        { get; set; }
        public string?   Phone        { get; set; }
        public bool      IsActive     { get; set; } = true;
        public DateTime? CreatedAt    { get; set; }

        // Для отображения в DataGrid
        public string IsActiveText => IsActive ? "✅ Да" : "⛔ Нет";
    }
}
