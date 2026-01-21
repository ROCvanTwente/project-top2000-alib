using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TemplateJwtProject.Models.DTOs;

namespace TemplateJwtProject.Controllers;

[ApiController]
[Route("statistieken")]
public class StatisticsController : ControllerBase
{
    private readonly AppDbContext _db;

    public StatisticsController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet("{year}")]
    public async Task<ActionResult<StatisticsDto>> GetStatisticsForYear(int year, [FromQuery] int top = 10)
    {
        var minYear = await _db.Top2000Entry.MinAsync(e => e.Year);
        var maxYear = await _db.Top2000Entry.MaxAsync(e => e.Year);

        if (year < minYear || year > maxYear)
            return NotFound(new { message = $"jaar niet bekend. {minYear}..{maxYear}." });

        // Current year but limited to the requested top positions
        var entriesThisTop = await _db.Top2000Entry
            .Include(t => t.Songs).ThenInclude(s => s.Artist)
            .Where(t => t.Year == year && t.Position <= top)
            .ToListAsync();

        // Previous year - keep full list so we can detect rises from outside the top
        var entriesPrevAll = await _db.Top2000Entry
            .Include(t => t.Songs).ThenInclude(s => s.Artist)
            .Where(t => t.Year == year - 1)
            .ToListAsync();

        // Previous year's top (for dropped calculation)
        var entriesPrevTop = entriesPrevAll.Where(e => e.Position <= top).ToList();

        var prevDict = entriesPrevAll.ToDictionary(e => e.SongId, e => e.Position);

        var movements = entriesThisTop
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
            .OrderBy(m => m.Difference)
            .Take(top)
            .ToList();

        var thisIds = entriesThisTop.Select(e => e.SongId).ToHashSet();
        var prevTopIds = entriesPrevTop.Select(e => e.SongId).ToHashSet();

        var newEntries = entriesThisTop
            .Where(e => !prevTopIds.Contains(e.SongId))
            .Select(e => new BasicSongDto
            {
                SongId = e.SongId,
                Titel = e.Songs.Titel,
                ArtistName = e.Songs.Artist.Name,
                ReleaseYear = e.Songs.ReleaseYear
            })
            .Take(top)
            .ToList();

        var droppedEntries = entriesPrevTop
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

        // All-time classics unchanged (full DB range)
        var yearCount = maxYear - minYear + 1;
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

        var artistCounts = entriesThisTop
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