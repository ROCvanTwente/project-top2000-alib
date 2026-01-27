namespace TemplateJwtProject.Models.DTOs
{
    public class SongDetailDto
    {
        public int SongId { get; set; }
        public int ArtistId { get; set; }
        public string Titel { get; set; }
        public string ImgUrl { get; set; }
        public string ArtistName { get; set; }
        public string ArtistPhoto { get; set; }
        public string ArtistBiography { get; set; }
        public string Lyrics { get; set; }
        public int? ReleaseYear { get; set; }
        public string Youtube { get; set; }
        public List<ChartPointDto> ChartHistory { get; set; }
    }

}
