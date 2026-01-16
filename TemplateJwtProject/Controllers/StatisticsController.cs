using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using TemplateJwtProject.Models.DTOs;

namespace TemplateJwtProject.Controllers
{
    [ApiController]
    [Route("statistieken")]
    public class StatisticsController : ControllerBase
    {
        private readonly AppDbContext _db;

        public StatisticsController(AppDbContext db) => _db = db;

        [HttpGet("{year}")]
        public async Task<ActionResult<StatisticsDto>> GetStatisticsForYear(int year, [FromQuery] int top = 10)
        {
            var minYear = await _db.Top2000Entry.MinAsync(e => e.Year);
            var maxYear = await _db.Top2000Entry.MaxAsync(e => e.Year);

            if (year < minYear || year > maxYear)
                return NotFound(new { message = $"Year out of range. Valid years: {minYear}..{maxYear}." });

            var entriesThis = await _db.Top2000Entry
                .Include(t => t.Songs).ThenInclude(s => s.Artist)
                .Where(t => t.Year == year)
                .ToListAsync();

            var entriesPrev = await _db.Top2000Entry
                .Include(t => t.Songs).ThenInclude(s => s.Artist)
                .Where(t => t.Year == year - 1)
                .ToListAsync();

            var prevDict = entriesPrev.ToDictionary(e => e.SongId, e => e.Position);

            // Movements: compute only for songs present in both years
            var movements = entriesThis
                .Where(e => prevDict.ContainsKey(e.SongId))
                .Select(e => new MovementDto
                {
                    SongId = e.SongId,
                    Titel = e.Songs.Titel,
                    ArtistName = e.Songs.Artist.Name,
                    Position = e.Position,
                    PositionLastYear = prevDict[e.SongId],
                    Difference = prevDict[e.SongId] - e.Position // positive = rose
                })
                .ToList();

            var biggestRises = movements
                .Where(m => m.Difference > 0)
                .OrderByDescending(m => m.Difference)
                .Take(top)
                .ToList();

            var biggestFalls = movements
                .Where(m => m.Difference < 0)
                .OrderBy(m => m.Difference) // most negative first (largest fall)
                .Take(top)
                .ToList();

            var thisIds = entriesThis.Select(e => e.SongId).ToHashSet();
            var prevIds = entriesPrev.Select(e => e.SongId).ToHashSet();

            var newEntries = entriesThis
                .Where(e => !prevIds.Contains(e.SongId))
                .Select(e => new BasicSongDto
                {
                    SongId = e.SongId,
                    Titel = e.Songs.Titel,
                    ArtistName = e.Songs.Artist.Name,
                    ReleaseYear = e.Songs.ReleaseYear
                })
                .Take(top)
                .ToList();

            var droppedEntries = entriesPrev
                .Where(e => !thisIds.Contains(e.SongId))
                .Select(e => new BasicSongDto
                {
                    SongId = e.SongId,
                    Titel = e.Songs.Titel,
                    ArtistName = e.Songs.Artist.Name,
                    ReleaseYear = e.Songs.ReleaseYear
                })
                .Take(top)
                .ToList();

            // All-time classics: songs that appear in every year in DB range
            var yearCount = (maxYear - minYear) + 1;
            var classics = await _db.Top2000Entry
                .Include(t => t.Songs).ThenInclude(s => s.Artist)
                .GroupBy(t => new { t.SongId, t.Songs.Titel, t.Songs.ArtistId, t.Songs.Artist.Name, t.Songs.ReleaseYear })
                .Where(g => g.Select(x => x.Year).Distinct().Count() == yearCount)
                .Select(g => new BasicSongDto
                {
                    SongId = g.Key.SongId,
                    Titel = g.Key.Titel,
                    ArtistName = g.Key.Name,
                    ReleaseYear = g.Key.ReleaseYear
                })
                .ToListAsync();

            // Artist counts for this year
            var artistCounts = entriesThis
                .GroupBy(e => new { e.Songs.ArtistId, e.Songs.Artist.Name })
                .Select(g => new ArtistCountDto
                {
                    ArtistId = g.Key.ArtistId,
                    ArtistName = g.Key.Name,
                    Count = g.Count()
                })
                .OrderByDescending(a => a.Count)
                .Take(top)
                .ToList();

            var stats = new StatisticsDto
            {
                Year = year,
                BiggestRises = biggestRises,
                BiggestFalls = biggestFalls,
                NewEntries = newEntries,
                DroppedEntries = droppedEntries,
                AllTimeClassics = classics,
                ArtistCounts = artistCounts
            };

            return Ok(stats);
        }
    }
}