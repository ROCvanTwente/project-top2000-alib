namespace TemplateJwtProject.Models
{
    public class Top2000Entry
    {
        // Composite key: SongId + Year
        public int SongId { get; set; }
        public int Year { get; set; }
        public int Position { get; set; }

        // Navigatie-eigenschap
        public Song Song { get; set; } = null!;
    }
}
