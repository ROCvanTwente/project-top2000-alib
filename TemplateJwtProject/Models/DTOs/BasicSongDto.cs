namespace TemplateJwtProject.Models.DTOs
{
    public class BasicSongDto
    {
        public int SongId { get; set; }
        public string Titel { get; set; } = string.Empty;
        public string ArtistName { get; set; } = string.Empty;
        public int? ReleaseYear { get; set; }
        public int? Position { get; set; }
        
        public int? PositionLastYear { get; set; }
    }
}

