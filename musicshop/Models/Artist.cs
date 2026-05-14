namespace musicshop.Models
{
    public class Artist
    {
        public int ID_artist { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Surname { get; set; }
        public string? Genre { get; set; }
        public string? Biography { get; set; }
        public string FullName => $"{Name} {Surname}".Trim();
    }
}