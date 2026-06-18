namespace tesisproject.shared.DTOs.Articles
{
    public class ArticleParticipantRequest
    {
        public int Index { get; set; }
        public string? Identificacion { get; set; }
        public string Nombre { get; set; } = default!;
        public string? Participacion { get; set; }
    }
}
