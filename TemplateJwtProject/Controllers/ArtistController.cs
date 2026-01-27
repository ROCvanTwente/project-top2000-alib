using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using TemplateJwtProject.Models;
using TemplateJwtProject.Models.DTOs;

namespace TemplateJwtProject.Controllers
{
    [ApiController]
    [Route("artists")]
    public class ArtistController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly ILogger<ArtistController> _logger;

        public ArtistController(AppDbContext db, ILogger<ArtistController> logger)
        {
            _db = db;
            _logger = logger;
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
            try
            {
                // =====================================================
                // DETAILS: /artists?artistId=1  (heeft prioriteit)
                // DETAILS: /artists?artist=Queen
                // =====================================================
                if (artistId.HasValue || !string.IsNullOrWhiteSpace(artist))
                {
                    var rows = _db.Top2000Entry
                        .Include(e => e.Songs)
                        .ThenInclude(s => s.Artist)
                        .Where(e =>
                            artistId.HasValue
                                ? e.Songs.Artist.ArtistId == artistId.Value
                                : e.Songs.Artist.Name.ToLower() == artist!.Trim().ToLower()
                        )
                        .Select(e => new
                        {
                            ArtistId = e.Songs.Artist.ArtistId,
                            SongId = e.Songs.SongId,
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
                                SongId = first.SongId,
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

                        oldestSong.SongId = oldest.SongId;
                        oldestSong.Titel = oldest.Titel;
                        oldestSong.ReleaseYear = oldest.ReleaseYear;

                        newestSong.SongId = newest.SongId;
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

                var entries = _db.Top2000Entry
                    .Include(e => e.Songs)
                    .ThenInclude(s => s.Artist)
                    .AsQueryable();

                if (year.HasValue)
                    entries = entries.Where(e => e.Year == year.Value);

                if (!string.IsNullOrWhiteSpace(contains))
                {
                    var c = contains.ToLower();
                    entries = entries.Where(e => e.Songs.Artist.Name.ToLower().Contains(c));
                }

                var flat = entries.Select(e => new
                {
                    ArtistId = e.Songs.Artist.ArtistId,
                    ArtistName = e.Songs.Artist.Name,
                    Wiki = e.Songs.Artist.Wiki,
                    ImgUrl = e.Songs.ImgUrl,
                    Biography = e.Songs.Artist.Biography,
                    Photo = e.Songs.Artist.Photo,
                    SongId = e.Songs.SongId,
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
                                    SongId = sf.SongId,
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

                            oldest.SongId = o.SongId;
                            oldest.Titel = o.Titel;
                            oldest.ReleaseYear = o.ReleaseYear;
                            newest.SongId = n.SongId;
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
            catch (SqlException ex) when (ex.Number == -2)
            {
                return StatusCode(504, new 
                { 
                    error = "Database query timeout",
                    message = "The request took too long to process. Please try adding more specific filters or contact support if the issue persists.",
                    details = ex.Message 
                });
            }
            catch (SqlException ex)
            {
                return StatusCode(500, new 
                { 
                    error = "Database error",
                    message = "An error occurred while querying the database.",
                    details = ex.Message 
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new 
                { 
                    error = "Internal server error",
                    message = "An unexpected error occurred while processing your request.",
                    details = ex.Message 
                });
            }
        }

        [HttpPut("update/{artistId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateArtist(int artistId, [FromBody] UpdateArtistDto dto)
        {
            try
            {
                if (dto == null)
                {
                    return BadRequest(new { error = "Invalid request body" });
                }

                var artist = await _db.Artist.FirstOrDefaultAsync(a => a.ArtistId == artistId);

                if (artist == null)
                {
                    return NotFound(new { error = "Artist not found" });
                }

                // Validate and update Wiki URL
                if (dto.Wiki != null)
                {
                    var wikiUrl = dto.Wiki.Trim();
                    if (!string.IsNullOrWhiteSpace(wikiUrl))
                    {
                        if (!Uri.TryCreate(wikiUrl, UriKind.Absolute, out var uriResult) ||
                            (uriResult.Scheme != Uri.UriSchemeHttp && uriResult.Scheme != Uri.UriSchemeHttps))
                        {
                            return BadRequest(new { error = "Invalid Wikipedia URL format" });
                        }
                    }
                    artist.Wiki = string.IsNullOrWhiteSpace(wikiUrl) ? null : wikiUrl;
                }

                // Update Biography (can be empty string)
                if (dto.Biography != null)
                {
                    artist.Biography = dto.Biography.Trim();
                }

                // Validate and update Photo URL
                if (dto.Photo != null)
                {
                    var photoUrl = dto.Photo.Trim();
                    if (!string.IsNullOrWhiteSpace(photoUrl))
                    {
                        if (!Uri.TryCreate(photoUrl, UriKind.Absolute, out var uriResult) ||
                            (uriResult.Scheme != Uri.UriSchemeHttp && uriResult.Scheme != Uri.UriSchemeHttps))
                        {
                            return BadRequest(new { error = "Invalid photo URL format" });
                        }
                    }
                    artist.Photo = string.IsNullOrWhiteSpace(photoUrl) ? null : photoUrl;
                }

                await _db.SaveChangesAsync();

                // Log admin action
                var username = User.Identity?.Name ?? "Unknown";
                _logger.LogInformation("Admin {Username} updated artist {ArtistId} - {ArtistName}", username, artistId, artist.Name);

                // Return updated artist details
                return Ok(new
                {
                    message = "Artist updated successfully",
                    artist = new
                    {
                        artistId = artist.ArtistId,
                        name = artist.Name,
                        wiki = artist.Wiki ?? "",
                        biography = artist.Biography ?? "",
                        photo = artist.Photo ?? ""
                    }
                });
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error while updating artist {ArtistId}", artistId);
                return StatusCode(500, new { error = "Database error occurred" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while updating artist {ArtistId}", artistId);
                return StatusCode(500, new { error = "An unexpected error occurred" });
            }
        }
    }
}
