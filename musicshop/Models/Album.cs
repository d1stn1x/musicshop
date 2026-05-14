namespace musicshop.Models
{
    public class Album
    {
        public int ID_album { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int Year_of_release { get; set; }
        public int? ID_group { get; set; }
        public string? GroupName { get; set; }
        public decimal Price { get; set; }
        public string PriceText => Price > 0 ? $"{Price:N2} руб." : "—";
    }
}