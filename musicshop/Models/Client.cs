namespace musicshop.Models
{
    public class Client
    {
        public int ID_client { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Surname { get; set; }
        public string? Telephone { get; set; }
        public string FullName => $"{Name} {Surname}".Trim();
    }
}