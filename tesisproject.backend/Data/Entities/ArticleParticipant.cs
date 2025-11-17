namespace tesisproject.backend.Data.Entities
{
    public class ArticleParticipant
    {
        public int Id { get; set; }

        public int ArticleId { get; set; }
        public Article Article { get; set; } = default!;

        public int Index { get; set; }

        public string? Identificacion { get; set; }

        public string Nombre { get; set; } = default!;

        public string? Participacion { get; set; }
    }
}
