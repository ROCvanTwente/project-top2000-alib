using System.ComponentModel.DataAnnotations;

namespace TemplateJwtProject.Models
{
    public class Songs
    {
        [Key]
        public int SongId { get; set; }

        // Foreign key
        public int ArtistId { get; set; }

        public string Titel { get; set; } = null!;
        public int? ReleaseYear { get; set; }
        public string? ImgUrl { get; set; }
        public string? Lyrics { get; set; }
        public string? Youtube { get; set; }

        // Navigatie-eigenschappen
        public Artist Artist { get; set; } = null!;
        public ICollection<Top2000Entry> Top2000Entries { get; set; } = new List<Top2000Entry>();
    }
}
