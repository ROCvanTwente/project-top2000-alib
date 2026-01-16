namespace TemplateJwtProject.Models.DTOs
{
    public class ArtistDto
    {
        public int ArtistId { get; set; }              // ✅ nieuw
        public string ArtistName { get; set; } = default!;
        public string WikipediaUrl { get; set; } = "";

        public string Biography { get; set; } = "";
        public string Photo { get; set; } = "";

        public ArtistStatsDto Stats { get; set; } = new ArtistStatsDto();
        public List<ArtistSongDto> Songs { get; set; } = new List<ArtistSongDto>();
    }

    public class ArtistSongDto
    {
        public int SongId { get; set; }
        public string Titel { get; set; } = default!;
        public int? ReleaseYear { get; set; }
        public int HighestRank { get; set; }
        public string? ImgUrl { get; set; }
    }

    public class ArtistStatsDto
    {
        public int TotalSongsInTop2000 { get; set; }
        public int HighestRankOverall { get; set; }

        public SongSimpleDto OldestSong { get; set; } = new SongSimpleDto();
        public SongSimpleDto NewestSong { get; set; } = new SongSimpleDto();
    }

    public class SongSimpleDto
    {
        public string Titel { get; set; } = "";
        public int? ReleaseYear { get; set; } = null;
    }
}
