using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TemplateJwtProject.Models.DTOs;

namespace TemplateJwtProject.Controllers
{
    [ApiController]
    [Route("top2000")]
    public class Top2000Controller : ControllerBase
    {
        private readonly AppDbContext _db;

        public Top2000Controller(AppDbContext db)
        {
            _db = db;
        }

        [HttpGet("{year}")]
        public async Task<ActionResult<IEnumerable<Top2000EntryDto>>> GetTop2000ForYear(
    int year,
    [FromQuery] int? artistId,
    [FromQuery] int? songId,
    [FromQuery] string? sort,      // <-- Toegevoegd
    [FromQuery] int limit
)
        {
            var minYear = await _db.Top2000Entry.MinAsync(e => e.Year);
            var maxYear = await _db.Top2000Entry.MaxAsync(e => e.Year);

            if (year < minYear || year > maxYear)
            {
                return NotFound(new
                {
                    message = $"Dit jaar bestaat niet in de Top2000 database. Geldige jaren: {minYear} t/m {maxYear}."
                });
            }

            var query = _db.Top2000Entry
                .Include(t => t.Songs)
                .ThenInclude(s => s.Artist)
                .Where(t => t.Year == year);

            if (artistId.HasValue)
            {
                query = query.Where(t => t.Songs.ArtistId == artistId.Value);
            }

            if (songId.HasValue)
            {
                query = query.Where(t => t.SongId == songId.Value);
            }

            // ⭐ SORTERING TOEGEVOEGD — ZONDER JE BESTAANDE CODE TE VERANDEREN
            query = sort?.ToLower() switch
            {
                "artist" => query.OrderBy(t => t.Songs.Artist.Name),
                "title" => query.OrderBy(t => t.Songs.Titel),
                "release" => query.OrderByDescending(t => t.Songs.ReleaseYear),
                _ => query.OrderBy(t => t.Position) // default = Rank
            };

            var entries = await query.ToListAsync();

            if (!entries.Any())
            {
                return NotFound(new
                {
                    message =
                        songId.HasValue ? $"Geen data gevonden voor jaar {year} met songId {songId}."
                        : artistId.HasValue ? $"Geen data gevonden voor jaar {year} met artistId {artistId}."
                        : $"Geen Top2000 data gevonden voor jaar {year}."
                });
            }

            var lastYearEntries = await _db.Top2000Entry
                .Where(t => t.Year == year - 1)
                .ToDictionaryAsync(t => t.SongId, t => t.Position);

            var result = entries.Select(t => new Top2000EntryDto
            {
                SongId = t.SongId,
                Year = t.Year,
                Position = t.Position,
                PositionLastYear = lastYearEntries.ContainsKey(t.SongId) ? lastYearEntries[t.SongId] : null,
                Difference = lastYearEntries.ContainsKey(t.SongId) ? lastYearEntries[t.SongId] - t.Position : null,
                Titel = t.Songs.Titel,
                ArtistId = t.Songs.ArtistId,
                ArtistName = t.Songs.Artist.Name,
                ReleaseYear = t.Songs.ReleaseYear,
                ImgUrl = t.Songs.ImgUrl
            });

            // ⭐ LIMIT TOEGEVOEGD (OPTIONEEL)
            if (limit != 0)
                result = result.Take(limit);

            return Ok(result);
        }
    }
}
