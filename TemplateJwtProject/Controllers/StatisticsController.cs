using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Data;
using TemplateJwtProject.Models.DTOs;

namespace TemplateJwtProject.Controllers
{
    [ApiController]
    [Route("statistieken")]
    public class StatisticsController : ControllerBase
    {
        private readonly AppDbContext _db;

        public StatisticsController(AppDbContext db)
        {
            _db = db;
        }

        private class EntryRow
        {
            public int SongId { get; set; }
            public int Position { get; set; }
            public int? ReleaseYear { get; set; }
            public string Titel { get; set; } = string.Empty;
            public int ArtistId { get; set; }
            public string ArtistName { get; set; } = string.Empty;

            public EntryRow(int songId, int position, int? releaseYear, string titel, int artistId, string artistName)
            {
                SongId = songId; Position = position; ReleaseYear = releaseYear; Titel = titel; ArtistId = artistId; ArtistName = artistName;
            }
        }

        [HttpGet("{year}")]
        public async Task<ActionResult<StatisticsDto>> GetStatisticsForYear(int year, [FromQuery] int top = 10, [FromQuery] int topArtists = 3)
        {
            if (!await _db.Top2000Entry.AnyAsync())
                return NotFound(new { message = "geen top2000 data beschikbaar." });

            // Stored-procedure callers (return false on failure so we can fallback)
            async Task<(bool success, int minYear, int maxYear)> TryGetMinMaxYearsFromProc()
            {
                try
                {
                    // Don't dispose the DbContext-owned connection. Use OpenConnectionAsync/CloseConnectionAsync to manage state.
                    await _db.Database.OpenConnectionAsync();
                    using var cmd = _db.Database.GetDbConnection().CreateCommand();
                    cmd.CommandText = "sp_GetMinMaxYear";
                    cmd.CommandType = CommandType.StoredProcedure;
                    using var reader = await cmd.ExecuteReaderAsync();
                    if (await reader.ReadAsync())
                    {
                        var min = reader.IsDBNull(0) ? 0 : reader.GetInt32(0);
                        var max = reader.IsDBNull(1) ? 0 : reader.GetInt32(1);
                        return (true, min, max);
                    }
                    return (false, 0, 0);
                }
                catch
                {
                    return (false, 0, 0);
                }
                finally
                {
                    await _db.Database.CloseConnectionAsync();
                }
            }

            async Task<bool> TryGetEntriesFromProc(string procName, int procYear, int procTop, List<EntryRow> target)
            {
                try
                {
                    await _db.Database.OpenConnectionAsync();
                    using var cmd = _db.Database.GetDbConnection().CreateCommand();
                    cmd.CommandText = procName;
                    cmd.CommandType = CommandType.StoredProcedure;

                    var pYear = cmd.CreateParameter();
                    pYear.ParameterName = "@Year";
                    pYear.Value = procYear;
                    pYear.DbType = DbType.Int32;
                    cmd.Parameters.Add(pYear);

                    if (procTop >= 0)
                    {
                        var pTop = cmd.CreateParameter();
                        pTop.ParameterName = "@Top";
                        pTop.Value = procTop;
                        pTop.DbType = DbType.Int32;
                        cmd.Parameters.Add(pTop);
                    }

                    using var reader = await cmd.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        int songId = reader.GetInt32(reader.GetOrdinal("SongId"));
                        int position = reader.GetInt32(reader.GetOrdinal("Position"));
                        int? releaseYear = reader.IsDBNull(reader.GetOrdinal("ReleaseYear")) ? null : reader.GetInt32(reader.GetOrdinal("ReleaseYear"));
                        string titel = reader.IsDBNull(reader.GetOrdinal("Titel")) ? string.Empty : reader.GetString(reader.GetOrdinal("Titel"));
                        int artistId = reader.IsDBNull(reader.GetOrdinal("ArtistId")) ? 0 : reader.GetInt32(reader.GetOrdinal("ArtistId"));
                        string artistName = reader.IsDBNull(reader.GetOrdinal("ArtistName")) ? string.Empty : reader.GetString(reader.GetOrdinal("ArtistName"));
                        target.Add(new EntryRow(songId, position, releaseYear, titel, artistId, artistName));
                    }
                    return true;
                }
                catch
                {
                    return false;
                }
                finally
                {
                    await _db.Database.CloseConnectionAsync();
                }
            }

            async Task<bool> TryGetEarlierSongIdsProc(int beforeYear, List<int> target)
            {
                try
                {
                    await _db.Database.OpenConnectionAsync();
                    using var cmd = _db.Database.GetDbConnection().CreateCommand();
                    cmd.CommandText = "sp_GetEarlierSongIds";
                    cmd.CommandType = CommandType.StoredProcedure;

                    var pBefore = cmd.CreateParameter();
                    pBefore.ParameterName = "@BeforeYear";
                    pBefore.Value = beforeYear;
                    pBefore.DbType = DbType.Int32;
                    cmd.Parameters.Add(pBefore);

                    using var reader = await cmd.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        target.Add(reader.GetInt32(0));
                    }
                    return true;
                }
                catch
                {
                    return false;
                }
                finally
                {
                    await _db.Database.CloseConnectionAsync();
                }
            }

            async Task<List<BasicSongDto>?> TryGetClassicsProc()
            {
                try
                {
                    var list = new List<BasicSongDto>();
                    await _db.Database.OpenConnectionAsync();
                    using var cmd = _db.Database.GetDbConnection().CreateCommand();
                    cmd.CommandText = "sp_GetAllTimeClassics";
                    cmd.CommandType = CommandType.StoredProcedure;
                    using var reader = await cmd.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        list.Add(new BasicSongDto
                        {
                            SongId = reader.GetInt32(reader.GetOrdinal("SongId")),
                            Titel = reader.IsDBNull(reader.GetOrdinal("Titel")) ? string.Empty : reader.GetString(reader.GetOrdinal("Titel")),
                            ArtistName = reader.IsDBNull(reader.GetOrdinal("ArtistName")) ? string.Empty : reader.GetString(reader.GetOrdinal("ArtistName")),
                            ReleaseYear = reader.IsDBNull(reader.GetOrdinal("ReleaseYear")) ? null : reader.GetInt32(reader.GetOrdinal("ReleaseYear"))
                        });
                    }
                    return list;
                }
                catch
                {
                    return null;
                }
                finally
                {
                    await _db.Database.CloseConnectionAsync();
                }
            }

            async Task<List<OneHitDto>?> TryGetOneHitsProc()
            {
                try
                {
                    var list = new List<OneHitDto>();
                    await _db.Database.OpenConnectionAsync();
                    using var cmd = _db.Database.GetDbConnection().CreateCommand();
                    cmd.CommandText = "sp_GetOneHitWonders";
                    cmd.CommandType = CommandType.StoredProcedure;
                    using var reader = await cmd.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        list.Add(new OneHitDto
                        {
                            SongId = reader.GetInt32(reader.GetOrdinal("SongId")),
                            Titel = reader.IsDBNull(reader.GetOrdinal("Titel")) ? string.Empty : reader.GetString(reader.GetOrdinal("Titel")),
                            ArtistName = reader.IsDBNull(reader.GetOrdinal("ArtistName")) ? string.Empty : reader.GetString(reader.GetOrdinal("ArtistName")),
                            ReleaseYear = reader.IsDBNull(reader.GetOrdinal("ReleaseYear")) ? null : reader.GetInt32(reader.GetOrdinal("ReleaseYear")),
                            Position = reader.GetInt32(reader.GetOrdinal("Position")),
                            Year = reader.GetInt32(reader.GetOrdinal("Year"))
                        });
                    }
                    return list;
                }
                catch
                {
                    return null;
                }
                finally
                {
                    await _db.Database.CloseConnectionAsync();
                }
            }

            async Task<List<BasicSongDto>?> TryGetReentriesProc(int procYear, int procTop)
            {
                try
                {
                    var list = new List<BasicSongDto>();
                    await _db.Database.OpenConnectionAsync();
                    using var cmd = _db.Database.GetDbConnection().CreateCommand();
                    cmd.CommandText = "sp_GetReentries";
                    cmd.CommandType = CommandType.StoredProcedure;

                    var pYear = cmd.CreateParameter();
                    pYear.ParameterName = "@Year";
                    pYear.Value = procYear;
                    pYear.DbType = DbType.Int32;
                    cmd.Parameters.Add(pYear);

                    var pTop = cmd.CreateParameter();
                    pTop.ParameterName = "@Top";
                    pTop.Value = procTop;
                    pTop.DbType = DbType.Int32;
                    cmd.Parameters.Add(pTop);

                    using var reader = await cmd.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        list.Add(new BasicSongDto
                        {
                            SongId = reader.GetInt32(reader.GetOrdinal("SongId")),
                            Titel = reader.IsDBNull(reader.GetOrdinal("Titel")) ? string.Empty : reader.GetString(reader.GetOrdinal("Titel")),
                            ArtistName = reader.IsDBNull(reader.GetOrdinal("ArtistName")) ? string.Empty : reader.GetString(reader.GetOrdinal("ArtistName")),
                            ReleaseYear = reader.IsDBNull(reader.GetOrdinal("ReleaseYear")) ? null : reader.GetInt32(reader.GetOrdinal("ReleaseYear")),
                            Position = reader.GetInt32(reader.GetOrdinal("Position")),
                            PositionLastYear = reader.IsDBNull(reader.GetOrdinal("PositionLastYear")) ? null : reader.GetInt32(reader.GetOrdinal("PositionLastYear"))
                        });
                    }
                    return list;
                }
                catch
                {
                    return null;
                }
                finally
                {
                    await _db.Database.CloseConnectionAsync();
                }
            }

            // Prepare containers
            int minYear; int maxYear;
            var entriesThisTop = new List<EntryRow>();
            var entriesPrevAll = new List<EntryRow>();
            var earlierSongIds = new List<int>();

            // Try SPs first
            var minMax = await TryGetMinMaxYearsFromProc();
            if (minMax.success)
            {
                minYear = minMax.minYear;
                maxYear = minMax.maxYear;
            }
            else
            {
                minYear = await _db.Top2000Entry.MinAsync(e => e.Year);
                maxYear = await _db.Top2000Entry.MaxAsync(e => e.Year);
            }

            bool gotThisTop = await TryGetEntriesFromProc("sp_GetEntriesThisTop", year, top, entriesThisTop);
            bool gotPrevAll = await TryGetEntriesFromProc("sp_GetEntriesForYear", year - 1, -1, entriesPrevAll);
            bool gotEarlier = await TryGetEarlierSongIdsProc(year - 1, earlierSongIds);

            // Fallbacks to EF for any missing data 
            if (!gotThisTop)
            {
                var entities = await _db.Top2000Entry
                    .Include(t => t.Songs).ThenInclude(s => s.Artist)
                    .Where(t => t.Year == year && t.Position <= top)
                    .OrderBy(t => t.Position)
                    .ToListAsync();

                entriesThisTop = entities.Select(e => new EntryRow(e.SongId, e.Position, e.Songs.ReleaseYear, e.Songs.Titel, e.Songs.ArtistId, e.Songs.Artist.Name)).ToList();
            }

            if (!gotPrevAll)
            {
                var entities = await _db.Top2000Entry
                    .Include(t => t.Songs).ThenInclude(s => s.Artist)
                    .Where(t => t.Year == year - 1)
                    .OrderBy(t => t.Position)
                    .ToListAsync();

                entriesPrevAll = entities.Select(e => new EntryRow(e.SongId, e.Position, e.Songs.ReleaseYear, e.Songs.Titel, e.Songs.ArtistId, e.Songs.Artist.Name)).ToList();
            }

            if (!gotEarlier)
            {
                earlierSongIds = await _db.Top2000Entry
                    .Where(e => e.Year < year - 1)
                    .Select(e => e.SongId)
                    .Distinct()
                    .ToListAsync();
            }

            var classics = await TryGetClassicsProc() ?? new List<BasicSongDto>();
            var oneHitEntries = await TryGetOneHitsProc() ?? new List<OneHitDto>();

            // Try stored-proc for reentries first
            var reentriesFromProc = await TryGetReentriesProc(year, top);

            // Compute differences
            var prevPositions = entriesPrevAll.ToDictionary(e => e.SongId, e => e.Position);
            var thisPositions = entriesThisTop.ToDictionary(e => e.SongId, e => e.Position);

            var biggestRises = new List<MovementDto>();
            var biggestFalls = new List<MovementDto>();
            var unchanged = new List<BasicSongDto>();
            var samePosition = new List<MovementDto>();
            var adjacentRuns = new List<BasicSongDto>();
            var newEntries = new List<BasicSongDto>();
            var droppedEntries = new List<BasicSongDto>();

            foreach (var e in entriesThisTop)
            {
                var dtoBasic = new BasicSongDto { SongId = e.SongId, Titel = e.Titel, ArtistName = e.ArtistName, ReleaseYear = e.ReleaseYear, Position = e.Position };

                if (prevPositions.TryGetValue(e.SongId, out var lastPos))
                {
                    int diff = lastPos - e.Position; // positive => rose
                    if (diff > 0)
                        biggestRises.Add(new MovementDto { SongId = e.SongId, Titel = e.Titel, ArtistName = e.ArtistName, Position = e.Position, ReleaseYear = e.ReleaseYear, PositionLastYear = lastPos, Difference = diff });
                    else if (diff < 0)
                        biggestFalls.Add(new MovementDto { SongId = e.SongId, Titel = e.Titel, ArtistName = e.ArtistName, Position = e.Position, ReleaseYear = e.ReleaseYear, PositionLastYear = lastPos, Difference = diff });
                    else
                    {
                        unchanged.Add(dtoBasic);
                        samePosition.Add(new MovementDto { SongId = e.SongId, Titel = e.Titel, ArtistName = e.ArtistName, Position = e.Position, ReleaseYear = e.ReleaseYear, PositionLastYear = lastPos, Difference = 0 });
                    }
                }
                else
                {
                    newEntries.Add(dtoBasic);
                }
            }

            // dropped entries = songs present previous year but not in this year (top)
            var droppedIds = entriesPrevAll.Select(e => e.SongId).Where(id => !thisPositions.ContainsKey(id)).ToHashSet();
            droppedEntries = entriesPrevAll.Where(e => droppedIds.Contains(e.SongId)).Select(e => new BasicSongDto { SongId = e.SongId, Titel = e.Titel, ArtistName = e.ArtistName, ReleaseYear = e.ReleaseYear, PositionLastYear = e.Position }).ToList();

            // Adjacent artist runs (2+ consecutive positions by same artist)
            var groupedByArtist = entriesThisTop
                .OrderBy(e => e.Position)
                .GroupBy(e => new { e.ArtistId, e.ArtistName });
             var adjacentSequences = new List<AdjacentSequenceDto>();
             foreach (var g in groupedByArtist)
             {
                var ordered = g.OrderBy(e => e.Position).ThenBy(e => e.SongId).ToList();
                 var currentSeq = new List<EntryRow>();

                for (int i = 0; i < ordered.Count; i++)
                {
                    var s = ordered[i];

                    if (currentSeq.Count == 0)
                    {
                        currentSeq.Add(s);
                        continue;
                    }

                    var last = currentSeq[currentSeq.Count - 1];
                    if (s.Position == last.Position + 1)
                    {
                        // consecutive => extend sequence
                        currentSeq.Add(s);
                    }
                    else
                    {
                        // broken sequence -> flush if length > 1
                        if (currentSeq.Count > 1)
                        {
                            var seqDto = new AdjacentSequenceDto
                            {
                                ArtistId = g.Key.ArtistId,
                                ArtistName = g.Key.ArtistName,
                                Positions = currentSeq.Select(r => r.Position).ToList(),
                                Songs = currentSeq.Select(r => new BasicSongDto { SongId = r.SongId, Titel = r.Titel, ArtistName = r.ArtistName, ReleaseYear = r.ReleaseYear, Position = r.Position }).ToList()
                            };
                            adjacentSequences.Add(seqDto);
                            adjacentRuns.AddRange(seqDto.Songs);
                        }
                        // start new sequence with current item
                        currentSeq = new List<EntryRow> { s };
                    }
                }

                // flush any trailing sequence
                if (currentSeq.Count > 1)
                {
                    var seqDto = new AdjacentSequenceDto
                    {
                        ArtistId = g.Key.ArtistId,
                        ArtistName = g.Key.ArtistName,
                        Positions = currentSeq.Select(r => r.Position).ToList(),
                        Songs = currentSeq.Select(r => new BasicSongDto { SongId = r.SongId, Titel = r.Titel, ArtistName = r.ArtistName, ReleaseYear = r.ReleaseYear, Position = r.Position }).ToList()
                    };
                    adjacentSequences.Add(seqDto);
                    adjacentRuns.AddRange(seqDto.Songs);
                }
            }
            // deduplicate flattened adjacentRuns by SongId while preserving first occurrence order
            adjacentRuns = adjacentRuns
                .GroupBy(s => s.SongId)
                .Select(g => g.First())
                .ToList();
            // expose sequences in the final DTO later

            // Artist stats, counts and top artists
            var artistStats = entriesThisTop.GroupBy(e => new { e.ArtistId, e.ArtistName })
                .Select(g => new ArtistStatDto
                {
                    ArtistId = g.Key.ArtistId,
                    ArtistName = g.Key.ArtistName,
                    Count = g.Count(),
                    AvgPosition = g.Average(x => x.Position),
                    BestPosition = g.Min(x => x.Position)
                })
                .OrderByDescending(a => a.Count)
                .ToList();

            var artistCounts = artistStats.Select(a => new ArtistCountDto { ArtistId = a.ArtistId, ArtistName = a.ArtistName, Count = a.Count }).ToList();

            var topArtistList = artistStats
                .GroupBy(a => a.Count)
                .OrderByDescending(g => g.Key)
                .SelectMany(g => g)
                .Take(topArtists)
                .ToList();

            // Compose final DTO
            var stats = new StatisticsDto
            {
                Year = year,
                BiggestRises = biggestRises.OrderByDescending(m => m.Difference).Take(top).ToList(),
                BiggestFalls = biggestFalls.OrderBy(m => m.Difference).Take(top).ToList(),
                NewEntries = newEntries.OrderBy(n => n.Titel).ToList(),
                DroppedEntries = droppedEntries.OrderBy(d => d.Titel).ToList(),
                Reentries = new List<BasicSongDto>(),
                Unchanged = unchanged.OrderBy(u => u.Titel).ToList(),
                AdjacentArtistRuns = adjacentRuns,
                AllTimeClassics = classics,
                OneHitWonders = oneHitEntries,
                TopArtists = topArtistList,
                ArtistCounts = artistCounts,

                Movements = biggestRises.Concat(biggestFalls).ToList(),
                SamePosition = samePosition,
                AdjacentSequences = adjacentSequences,
                SingleAppearances = oneHitEntries.Select(o => new BasicSongDto { SongId = o.SongId, Titel = o.Titel, ArtistName = o.ArtistName, ReleaseYear = o.ReleaseYear, Position = o.Position }).ToList(),
                ArtistStats = artistStats
            };

            // UI cards
            stats.Cards = new List<CardDto>
            {
                new CardDto { Title = "Grootste dalers", Subtitle = $"Top {top}", Payload = stats.BiggestFalls },
                new CardDto { Title = "Grootste stijgers", Subtitle = $"Top {top}", Payload = stats.BiggestRises },
                new CardDto { Title = "Alle liedjes in alle edities", Subtitle = "Gesorteerd op titel", Payload = stats.AllTimeClassics },
                new CardDto { Title = "Nieuwe binnenkomers", Subtitle = $"Top {top}", Payload = stats.NewEntries },
                new CardDto { Title = "Uitgevallen", Subtitle = $"Verloren uit top {top}", Payload = stats.DroppedEntries },
                new CardDto { Title = "Opnieuw binnengekomen", Subtitle = "Nieuwe binnenkomers maar eerder aanwezig", Payload = stats.Reentries },
                new CardDto { Title = "Onveranderde posities", Subtitle = $"Blijven op dezelfde plek in {year}", Payload = stats.Unchanged },
                new CardDto { Title = "Aansluitende posities door dezelfde artiest", Subtitle = "2+ opvolgende posities", Payload = stats.AdjacentArtistRuns },
                new CardDto { Title = "One-hit wonders", Subtitle = "Slechts 1 keer in de TOP2000", Payload = stats.OneHitWonders },
                new CardDto { Title = $"Top artiesten (top {topArtists} met gelijke aantallen)", Subtitle = $"Jaar {year}", Payload = stats.TopArtists },
                new CardDto { Title = "Artiesten Top (count)", Subtitle = $"Top {top} artists in positions", Payload = stats.ArtistCounts }
            };

            // --- New: compute reentries (songs present this year but not in previous year, yet present in earlier years) ---
            var reentries = new List<BasicSongDto>();
            var pureNewEntries = new List<BasicSongDto>();

            if (reentriesFromProc != null)
            {
                // prefer stored-proc result
                reentries.AddRange(reentriesFromProc);

                // remove reentries from newEntries to get pure new entries
                var reentrySet = reentriesFromProc.Select(r => r.SongId).ToHashSet();
                pureNewEntries.AddRange(newEntries.Where(n => !reentrySet.Contains(n.SongId)).ToList());
            }
            else
            {
                // fallback: compute using EF and earlierSongIds
                var newEntryIds = newEntries.Select(n => n.SongId).ToList();
                var reentryIds = newEntryIds.Where(id => earlierSongIds.Contains(id)).ToList();

                // EF fallback: also look directly in the DB for any of these songs appearing before year-1
                var reentryIdsFromDb = await _db.Top2000Entry
                    .Where(e => e.Year < year - 1 && newEntryIds.Contains(e.SongId))
                    .Select(e => e.SongId)
                    .Distinct()
                    .ToListAsync();

                reentryIds = reentryIds.Union(reentryIdsFromDb).Distinct().ToList();

                if (reentryIds.Any())
                {
                    // Fetch the most recent prior entry for each reentry id (Year < year) in a single query
                    var prevEntriesForReentries = await _db.Top2000Entry
                        .Where(e => e.Year < year && reentryIds.Contains(e.SongId))
                        .OrderByDescending(e => e.Year)
                        .ToListAsync();

                    // group by song and take the first (latest year)
                    var lastBySong = prevEntriesForReentries
                        .GroupBy(e => e.SongId)
                        .ToDictionary(g => g.Key, g => g.First());

                    foreach (var n in newEntries)
                    {
                        if (lastBySong.TryGetValue(n.SongId, out var lastEntry))
                        {
                            reentries.Add(new BasicSongDto
                            {
                                SongId = n.SongId,
                                Titel = n.Titel,
                                ArtistName = n.ArtistName,
                                ReleaseYear = n.ReleaseYear,
                                Position = n.Position,
                                PositionLastYear = lastEntry.Position
                            });
                        }
                        else
                        {
                            // not found in earlier entries, treat as pure new
                            pureNewEntries.Add(n);
                        }
                    }
                }
                else
                {
                    pureNewEntries.AddRange(newEntries);
                }
            }

            stats.Reentries = reentries.OrderBy(r => r.Titel).ToList();
            stats.NewEntries = pureNewEntries.OrderBy(n => n.Titel).ToList();

            return Ok(stats);
        }
    }
}
