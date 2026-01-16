namespace TemplateJwtProject.Models.DTOs
{
    public class Top2000EntryDto
    {
        public int SongId { get; set; }
        public int Year { get; set; }
        public int Position { get; set; }
        public int? PositionLastYear { get; set; }
        public int? Difference { get; set; }
        public string? ImgUrl { get; set; }
        public string Titel { get; set; } = null!;
        public int ArtistId { get; set; }
        public string ArtistName { get; set; } = null!;
        public int? ReleaseYear { get; set; }
    }
}
