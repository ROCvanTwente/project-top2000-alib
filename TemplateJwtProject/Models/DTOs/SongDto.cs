namespace TemplateJwtProject.Models.DTOs
{
    public class SongDto
    {
        public int SongId { get; set; }
        public string Titel { get; set; }
        public string ArtistName { get; set; }
        public string ImgUrl { get; set; }
        public int? ReleaseYear { get; set; }
        public string Youtube { get; set; }
    }
}
