namespace musicshop.Models
{
    public class Group
    {
        public int ID_group { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Genre { get; set; }
        public string? Biography { get; set; }
    }
}