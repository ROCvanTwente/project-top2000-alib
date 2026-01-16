using System.Collections.Generic;

namespace TemplateJwtProject.Models.DTOs
{
    public class StatisticsDto
    {
        public int Year { get; set; }
        public List<MovementDto> BiggestRises { get; set; } = new();
        public List<MovementDto> BiggestFalls { get; set; } = new();
        public List<BasicSongDto> NewEntries { get; set; } = new();
        public List<BasicSongDto> DroppedEntries { get; set; } = new();
        public List<BasicSongDto> AllTimeClassics { get; set; } = new();
        public List<ArtistCountDto> ArtistCounts { get; set; } = new();
    }

    public class MovementDto
    {
        public int SongId { get; set; }
        public string Titel { get; set; } = string.Empty;
        public string ArtistName { get; set; } = string.Empty;
        public int Position { get; set; }
        public int PositionLastYear { get; set; }
        public int Difference { get; set; } // positive = rose, negative = fell
    }

    public class BasicSongDto
    {
        public int SongId { get; set; }
        public string Titel { get; set; } = string.Empty;
        public string ArtistName { get; set; } = string.Empty;
        public int? ReleaseYear { get; set; }
    }

    public class ArtistCountDto
    {
        public int ArtistId { get; set; }
        public string ArtistName { get; set; } = string.Empty;
        public int Count { get; set; }
    }
}

