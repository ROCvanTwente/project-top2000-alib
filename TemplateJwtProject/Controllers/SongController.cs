using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TemplateJwtProject.Models;
using TemplateJwtProject.Models.DTOs;

namespace TemplateJwtProject.Controllers
{
    [ApiController]
    [Route("song")]
    public class SongController : ControllerBase
    {
        private readonly AppDbContext _db;

        public SongController(AppDbContext db)
        {
            _db = db;
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
                ArtistName = song.Artist?.Name,
                ArtistPhoto = song.Artist?.Photo,
                ArtistBiography = song.Artist?.Biography,
                Lyrics = song.Lyrics,
                ReleaseYear = song.ReleaseYear,
                ChartHistory = chartHistory
            };
            return Ok(dto);
        }

        [HttpGet("getAllSongs")]
        public IActionResult GetAllSongs()
        {
            List<SongDetailDto> listSongs = new List<SongDetailDto>();

            var songs = _db.Songs
                .Include(s => s.Artist).ToList();
            if (songs == null)
                return NotFound();

            foreach (Songs song in songs)
            {
                var dto = new SongDetailDto
                {
                    SongId = song.SongId,
                    Titel = song.Titel,             // ✔ werkt nu
                    ArtistName = song.Artist?.Name,
                    ReleaseYear = song.ReleaseYear
                };
                listSongs.Add(dto);
            }
            return Ok(listSongs);
        }
    }
}