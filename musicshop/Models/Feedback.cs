namespace musicshop.Models
{
    public class Feedback
    {
        public int ID_feedback { get; set; }
        public string? Comment { get; set; }
        public int? Rate { get; set; }
        public int? ID_client { get; set; }
        public int? ID_album { get; set; }
        public string? ClientName { get; set; }
        public string? AlbumName { get; set; }
    }
}