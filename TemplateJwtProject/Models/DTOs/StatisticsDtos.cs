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
        public List<BasicSongDto> Reentries { get; set; } = new();
        public List<BasicSongDto> Unchanged { get; set; } = new();
        public List<BasicSongDto> AdjacentArtistRuns { get; set; } = new();
        public List<BasicSongDto> AllTimeClassics { get; set; } = new();
        public List<OneHitDto> OneHitWonders { get; set; } = new();
        public List<ArtistStatDto> TopArtists { get; set; } = new();
        public List<ArtistCountDto> ArtistCounts { get; set; } = new();

        // UI-friendly card wrapper
        public List<CardDto> Cards { get; set; } = new();

        // Extra fields expected by the frontend
        public List<MovementDto>? Movements { get; set; }
        public List<MovementDto>? SamePosition { get; set; }
        public List<AdjacentSequenceDto>? AdjacentSequences { get; set; }
        public List<BasicSongDto>? SingleAppearances { get; set; }
        public List<ArtistStatDto>? ArtistStats { get; set; }
    }
}
