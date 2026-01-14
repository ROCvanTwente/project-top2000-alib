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
        public IActionResult GetArtists()
        {
            var result = _db.Top2000Entry
                .Select(e => new
                {
                    ArtistName = e.Songs.Artist.Name,
                    SongTitle = e.Songs.Titel,
                    // evt. extra: ReleaseYear = e.Songs.ReleaseYear
                })
                .GroupBy(x => x.ArtistName.ToLower())
                .Select(g => new ArtistTop2000Dto
                {
                    Name = g.First().ArtistName,

                    // Unieke liedjes op basis van titel (niet SongId)
                    SongsInTop2000 = g
                        .Select(x => x.SongTitle.ToLower().Trim())
                        .Distinct()
                        .Count()
                })
                .OrderByDescending(a => a.SongsInTop2000)
                .ToList();

            if (result.Count == 0) return NotFound();
            return Ok(result);
        }
    }
}
