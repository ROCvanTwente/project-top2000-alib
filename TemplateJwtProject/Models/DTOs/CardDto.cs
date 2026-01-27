using System;

namespace TemplateJwtProject.Models.DTOs
{
    public class CardDto
    {
        public string Title { get; set; } = string.Empty;
        public string? Subtitle { get; set; }
        public object Payload { get; set; } = new object();
    }
}

