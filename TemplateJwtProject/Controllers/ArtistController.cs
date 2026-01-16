using Microsoft.AspNetCore.Mvc;
using TemplateJwtProject.Models;
using TemplateJwtProject.Models.DTOs;

namespace TemplateJwtProject.Controllers
{
    [ApiController]
    [Route("artists")]
    public class ArtistController : ControllerBase
    {
        private readonly AppDbContext _db;

        public ArtistController(AppDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        public IActionResult GetArtists(
            [FromQuery] int? artistId,
            [FromQuery] string? artist,
            [FromQuery] string? contains,
            [FromQuery] int? minSongs,
            [FromQuery] int? maxSongs,
            [FromQuery] int? highestRank,
            [FromQuery] bool? hasWiki,
            [FromQuery] int? minReleaseYear,
            [FromQuery] int? maxReleaseYear,
            [FromQuery] int? year
        )
        {
            // =====================================================
            // DETAILS: /artists?artistId=1  (heeft prioriteit)
            // DETAILS: /artists?artist=Queen
            // =====================================================
            if (artistId.HasValue || !string.IsNullOrWhiteSpace(artist))
            {
                var rows = _db.Top2000Entry
                    .Where(e =>
                        artistId.HasValue
                            ? e.Songs.Artist.ArtistId == artistId.Value
                            : e.Songs.Artist.Name.ToLower() == artist!.Trim().ToLower()
                    )
                    .Select(e => new
                    {
                        ArtistId = e.Songs.Artist.ArtistId,
                        ArtistName = e.Songs.Artist.Name,
                        Wiki = e.Songs.Artist.Wiki,
                        ImgUrl = e.Songs.ImgUrl,
                        Biography = e.Songs.Artist.Biography,
                        Photo = e.Songs.Artist.Photo,
                        SongTitle = e.Songs.Titel,
                        ReleaseYear = (int?)e.Songs.ReleaseYear,
                        Position = e.Position
                    })
                    .ToList();

                if (!rows.Any()) return NotFound();

                var songs = rows
                    .GroupBy(x => x.SongTitle.ToLower().Trim())
                    .Select(g =>
                    {
                        var first = g.First();
                        var bestRank = g.Select(x => x.Position)
                            .Where(p => p > 0)
                            .DefaultIfEmpty(int.MaxValue)
                            .Min();
                        if (bestRank == int.MaxValue) bestRank = 0;

                        return new ArtistSongDto
                        {
                            Titel = first.SongTitle,
                            ReleaseYear = first.ReleaseYear,
                            HighestRank = bestRank,
                            ImgUrl = first.ImgUrl
                        };
                    })
                    .OrderBy(s => s.HighestRank == 0 ? int.MaxValue : s.HighestRank)
                    .ThenBy(s => s.Titel)
                    .ToList();

                var highestRankOverall = rows
                    .Select(x => x.Position)
                    .Where(p => p > 0)
                    .DefaultIfEmpty(int.MaxValue)
                    .Min();
                if (highestRankOverall == int.MaxValue) highestRankOverall = 0;

                var oldestSong = new SongSimpleDto();
                var newestSong = new SongSimpleDto();

                var withYear = songs.Where(s => s.ReleaseYear.HasValue).ToList();
                if (withYear.Any())
                {
                    var oldest = withYear.OrderBy(s => s.ReleaseYear).First();
                    var newest = withYear.OrderByDescending(s => s.ReleaseYear).First();

                    oldestSong.Titel = oldest.Titel;
                    oldestSong.ReleaseYear = oldest.ReleaseYear;
                    newestSong.Titel = newest.Titel;
                    newestSong.ReleaseYear = newest.ReleaseYear;
                }

                return Ok(new ArtistDto
                {
                    ArtistId = rows.First().ArtistId,
                    ArtistName = rows.First().ArtistName,
                    WikipediaUrl = rows.First().Wiki ?? "",
                    Biography = rows.First().Biography ?? "",
                    Photo = rows.First().Photo ?? "",

                    Stats = new ArtistStatsDto
                    {
                        TotalSongsInTop2000 = songs.Count,
                        HighestRankOverall = highestRankOverall,
                        OldestSong = oldestSong,
                        NewestSong = newestSong
                    },

                    Songs = songs
                });
            }

            // =========================
            // LIST + FILTERS
            // =========================

            var entries = _db.Top2000Entry.AsQueryable();
            if (year.HasValue)
                entries = entries.Where(e => e.Year == year.Value);

            var flat = entries.Select(e => new
            {
                ArtistId = e.Songs.Artist.ArtistId,
                ArtistName = e.Songs.Artist.Name,
                Wiki = e.Songs.Artist.Wiki,
                ImgUrl = e.Songs.ImgUrl,
                Biography = e.Songs.Artist.Biography,
                Photo = e.Songs.Artist.Photo,
                SongTitle = e.Songs.Titel,
                ReleaseYear = (int?)e.Songs.ReleaseYear,
                Position = e.Position
            }).ToList();

            var grouped = flat
                .GroupBy(x => x.ArtistId)
                .Select(g =>
                {
                    var first = g.First();

                    var songs = g
                        .GroupBy(x => x.SongTitle.ToLower().Trim())
                        .Select(sg =>
                        {
                            var sf = sg.First();
                            var bestRank = sg.Select(x => x.Position)
                                .Where(p => p > 0)
                                .DefaultIfEmpty(int.MaxValue)
                                .Min();
                            if (bestRank == int.MaxValue) bestRank = 0;

                            return new ArtistSongDto
                            {
                                Titel = sf.SongTitle,
                                ReleaseYear = sf.ReleaseYear,
                                HighestRank = bestRank
                            };
                        })
                        .ToList();

                    var bestRankOverall = g.Select(x => x.Position)
                        .Where(p => p > 0)
                        .DefaultIfEmpty(int.MaxValue)
                        .Min();
                    if (bestRankOverall == int.MaxValue) bestRankOverall = 0;

                    var oldest = new SongSimpleDto();
                    var newest = new SongSimpleDto();

                    var withYear = songs.Where(s => s.ReleaseYear.HasValue).ToList();
                    if (withYear.Any())
                    {
                        var o = withYear.OrderBy(s => s.ReleaseYear).First();
                        var n = withYear.OrderByDescending(s => s.ReleaseYear).First();
                        oldest.Titel = o.Titel;
                        oldest.ReleaseYear = o.ReleaseYear;
                        newest.Titel = n.Titel;
                        newest.ReleaseYear = n.ReleaseYear;
                    }

                    return new ArtistDto
                    {
                        ArtistId = first.ArtistId,
                        ArtistName = first.ArtistName,
                        WikipediaUrl = first.Wiki ?? "",
                        Biography = first.Biography ?? "",
                        Photo = first.Photo ?? "",

                        Stats = new ArtistStatsDto
                        {
                            TotalSongsInTop2000 = songs.Count,
                            HighestRankOverall = bestRankOverall,
                            OldestSong = oldest,
                            NewestSong = newest
                        },

                        Songs = songs
                    };
                })
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(contains))
            {
                var c = contains.ToLower();
                grouped = grouped.Where(a => a.ArtistName.ToLower().Contains(c));
            }

            if (minSongs.HasValue)
                grouped = grouped.Where(a => a.Stats.TotalSongsInTop2000 >= minSongs.Value);

            if (maxSongs.HasValue)
                grouped = grouped.Where(a => a.Stats.TotalSongsInTop2000 <= maxSongs.Value);

            if (highestRank.HasValue)
                grouped = grouped.Where(a => a.Stats.HighestRankOverall <= highestRank.Value && a.Stats.HighestRankOverall != 0);

            if (hasWiki.HasValue)
            {
                grouped = hasWiki.Value
                    ? grouped.Where(a => !string.IsNullOrWhiteSpace(a.WikipediaUrl))
                    : grouped.Where(a => string.IsNullOrWhiteSpace(a.WikipediaUrl));
            }

            if (minReleaseYear.HasValue)
                grouped = grouped.Where(a => a.Stats.OldestSong.ReleaseYear >= minReleaseYear.Value);

            if (maxReleaseYear.HasValue)
                grouped = grouped.Where(a => a.Stats.NewestSong.ReleaseYear <= maxReleaseYear.Value);

            var result = grouped
                .OrderBy(a => a.Stats.HighestRankOverall == 0 ? int.MaxValue : a.Stats.HighestRankOverall)
                .ThenByDescending(a => a.Stats.TotalSongsInTop2000)
                .ToList();

            if (result.Count == 0) return NotFound();
            return Ok(result);
        }
    }
}
