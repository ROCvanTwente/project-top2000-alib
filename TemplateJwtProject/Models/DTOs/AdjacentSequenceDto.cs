using System.Collections.Generic;

namespace TemplateJwtProject.Models.DTOs
{
    public class AdjacentSequenceDto
    {
        public int ArtistId { get; set; }
        public string ArtistName { get; set; } = string.Empty;
        public List<int> Positions { get; set; } = new();
        public List<BasicSongDto> Songs { get; set; } = new();
    }
}

