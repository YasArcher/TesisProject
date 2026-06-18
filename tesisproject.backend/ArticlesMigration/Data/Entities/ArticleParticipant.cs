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
        public string? ParticipantType { get; set; }
        public int? InstitutionalPersonId { get; set; }
        public bool IsPrimaryAuthor { get; set; }
        public string? Email { get; set; }
        public string? Orcid { get; set; }
        public string? Affiliation { get; set; }
        public string? ExternalAuthorId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        public ICollection<ArticleParticipantDynamicFieldValue> DynamicFieldValues { get; set; } = new List<ArticleParticipantDynamicFieldValue>();
    }
}
