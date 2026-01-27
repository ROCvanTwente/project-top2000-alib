namespace TemplateJwtProject.Models.DTOs
{
    public class ArtistStatDto
    {
        public int ArtistId { get; set; }
        public string ArtistName { get; set; } = string.Empty;
        public int Count { get; set; }
        public double AvgPosition { get; set; }
        public int BestPosition { get; set; }
    }
}

