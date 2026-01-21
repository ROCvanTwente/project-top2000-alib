using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TemplateJwtProject.Models;
using TemplateJwtProject.Models.DTOs;

namespace TemplateJwtProject.Controllers
{
    [ApiController]
    [Route("song")]
    public class SongController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly ILogger<SongController> _logger;

        public SongController(AppDbContext db, ILogger<SongController> logger)
        {
            _db = db;
            _logger = logger;
        }

        [HttpGet("details")]
        public IActionResult GetSongDetail(int id)
        {
            var song = _db.Songs
                .Include(s => s.Artist)
                .FirstOrDefault(s => s.SongId == id);

            if (song == null)
                return NotFound();

            var chartHistory = _db.Top2000Entry
                .Where(e => e.SongId == id)
                .OrderBy(e => e.Year)
                .Select(e => new ChartPointDto
                {
                    Year = e.Year,
                    Position = e.Position
                })
                .ToList();

            var dto = new SongDetailDto
            {
                SongId = song.SongId,
                Titel = song.Titel,
                ArtistId = song.ArtistId,
                ArtistName = song.Artist?.Name,
                ImgUrl = song.ImgUrl,
                ArtistPhoto = song.Artist?.Photo,
                ArtistBiography = song.Artist?.Biography,
                Lyrics = song.Lyrics,
                ReleaseYear = song.ReleaseYear,
                Youtube = song.Youtube,
                ChartHistory = chartHistory
            };
            return Ok(dto);
        }

        [HttpGet("getallsongs")]
        public IActionResult GetAllSongs()
        {
            List<SongDto> listSongs = new List<SongDto>();

            var songs = _db.Songs
                .Include(s => s.Artist).ToList();
            if (songs == null)
                return NotFound();

            foreach (Songs song in songs)
            {
                var dto = new SongDto
                {
                    SongId = song.SongId,
                    Titel = song.Titel,
                    ArtistName = song.Artist?.Name,
                    ReleaseYear = song.ReleaseYear,
                    ImgUrl = song.ImgUrl,
                    Youtube = song.Youtube
                };
                listSongs.Add(dto);
            }
            return Ok(listSongs);
        }

        [HttpPut("update/{songId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateSong(int songId, [FromBody] UpdateSongDto dto)
        {
            try
            {
                if (dto == null)
                {
                    return BadRequest(new { error = "Invalid request body" });
                }

                var song = await _db.Songs
                    .Include(s => s.Artist)
                    .FirstOrDefaultAsync(s => s.SongId == songId);

                if (song == null)
                {
                    return NotFound(new { error = "Song not found" });
                }

                // Validate and update ImgUrl
                if (dto.ImgUrl != null)
                {
                    var imgUrl = dto.ImgUrl.Trim();
                    if (!string.IsNullOrWhiteSpace(imgUrl))
                    {
                        if (!Uri.TryCreate(imgUrl, UriKind.Absolute, out var uriResult) ||
                            (uriResult.Scheme != Uri.UriSchemeHttp && uriResult.Scheme != Uri.UriSchemeHttps))
                        {
                            return BadRequest(new { error = "Invalid image URL format" });
                        }
                    }
                    song.ImgUrl = string.IsNullOrWhiteSpace(imgUrl) ? null : imgUrl;
                }

                // Update Lyrics (can be empty string)
                if (dto.Lyrics != null)
                {
                    song.Lyrics = dto.Lyrics.Trim();
                }

                // Validate and update Youtube URL
                if (dto.Youtube != null)
                {
                    var youtubeUrl = dto.Youtube.Trim();
                    if (!string.IsNullOrWhiteSpace(youtubeUrl))
                    {
                        if (!Uri.TryCreate(youtubeUrl, UriKind.Absolute, out var uriResult) ||
                            (uriResult.Scheme != Uri.UriSchemeHttp && uriResult.Scheme != Uri.UriSchemeHttps))
                        {
                            return BadRequest(new { error = "Invalid YouTube URL format" });
                        }
                    }
                    song.Youtube = string.IsNullOrWhiteSpace(youtubeUrl) ? null : youtubeUrl;
                }

                await _db.SaveChangesAsync();

                // Log admin action
                var username = User.Identity?.Name ?? "Unknown";
                _logger.LogInformation("Admin {Username} updated song {SongId} - {Title}", username, songId, song.Titel);

                // Return updated song details
                var updatedSong = await _db.Songs
                    .Include(s => s.Artist)
                    .FirstOrDefaultAsync(s => s.SongId == songId);

                return Ok(new
                {
                    message = "Song updated successfully",
                    song = new
                    {
                        songId = updatedSong!.SongId,
                        titel = updatedSong.Titel,
                        artistId = updatedSong.ArtistId,
                        artistName = updatedSong.Artist?.Name,
                        releaseYear = updatedSong.ReleaseYear,
                        imgUrl = updatedSong.ImgUrl ?? "",
                        lyrics = updatedSong.Lyrics ?? "",
                        youtube = updatedSong.Youtube ?? ""
                    }
                });
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error while updating song {SongId}", songId);
                return StatusCode(500, new { error = "Database error occurred" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while updating song {SongId}", songId);
                return StatusCode(500, new { error = "An unexpected error occurred" });
            }
        }
    }
}