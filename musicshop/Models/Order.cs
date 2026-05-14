using System;

namespace musicshop.Models
{
    public class Order
    {
        public int ID_order { get; set; }
        public string? Status_order { get; set; }
        public DateTime? Date_of_execution { get; set; }
        public int? ID_client { get; set; }
        public string? ClientName { get; set; }
        public decimal TotalCost { get; set; }
        public string? AlbumName { get; set; }
    }
}