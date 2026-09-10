namespace tesisproject.shared.DTOs.Articles
{
    public class RegisterArticleAggregateRequest
    {
        public string? FormKey { get; set; }
        public ArticleAggregateCoreDto Article { get; set; } = new();
        public ArticleVenueInputDto Venue { get; set; } = new();
        public ArticleVenueMetricInputDto VenueMetric { get; set; } = new();
        public List<DynamicFieldValueInputDto> DynamicFields { get; set; } = new();
        public List<ArticleParticipantAggregateDto> Participants { get; set; } = new();
        public List<int> IndexingSourceIds { get; set; } = new();
        public List<ArticleRegistrationFileDto> Files { get; set; } = new();
    }

    public class ArticleRegistrationFileDto
    {
        public string FileName { get; set; } = string.Empty;
        public string? FileUrl { get; set; }
        public string? Sha256 { get; set; }
    }

    public class ArticleAggregateCoreDto
    {
        // Required by Unified registration; no inferred product type or project.
        public int? ProductTypeId { get; set; }
        public int? ProjectId { get; set; }
        public string? IndexingDatabase { get; set; }
        public string? Title { get; set; }
        public string? Doi { get; set; }
        public short? Year { get; set; }
        public DateTime? PublishedAt { get; set; }
        public int? PageCount { get; set; }
        public string? PublicationUrl { get; set; }
        public bool IsProjectResult { get; set; }
        public bool HasInterculturalComponent { get; set; }
        public string? ProceedingsName { get; set; }
        public string? Proceedings { get; set; }
        public string? EventName { get; set; }
        public string? GroupName { get; set; }
        public string? Filiacion { get; set; }
        public int? AcademicTermId { get; set; }
        public byte? PublicationStatusId { get; set; }
        public int? ResearchLineId { get; set; }
        public int? BroadFieldId { get; set; }
        public int? SpecificFieldId { get; set; }
        public int? DetailedFieldId { get; set; }
        public int? FacultyId { get; set; }
        public bool IsOpenAccess { get; set; }
        public string? ExternalSource { get; set; }
        public string? ExternalId { get; set; }
    }

    public class ArticleVenueInputDto
    {
        public int? VenueId { get; set; }
        public string? JournalName { get; set; }
        public string? IssnCode { get; set; }
        public string? IssueNumber { get; set; }
        public string? VolumeNumber { get; set; }
        public string? JournalUrl { get; set; }
        public string? Type { get; set; }
    }

    public class ArticleVenueMetricInputDto
    {
        public short? Year { get; set; }
        public decimal? Sjr { get; set; }
        public string? Quartile { get; set; }
    }

    public class ArticleParticipantAggregateDto
    {
        public int Index { get; set; }
        public string? Identificacion { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string? Participacion { get; set; }
        public string? ParticipantType { get; set; }
        // Unified: directory person (id_usuario), verified using document/email;
        // the directory's ASP_ID is passed to the Unified identity boundary.
        public int? InstitutionalPersonId { get; set; }
        public int? ExternalResearcherId { get; set; }
        public bool IsPrimaryAuthor { get; set; }
        public string? Email { get; set; }
        public string? Orcid { get; set; }
        public string? Affiliation { get; set; }
        public string? ExternalAuthorId { get; set; }
        public List<DynamicFieldValueInputDto> DynamicFields { get; set; } = new();
    }

    public class DynamicFieldValueInputDto
    {
        public int? FieldId { get; set; }
        public string? FieldKey { get; set; }
        public string? ValueString { get; set; }
        public int? ValueInt { get; set; }
        public decimal? ValueDecimal { get; set; }
        public DateTime? ValueDate { get; set; }
        public bool? ValueBit { get; set; }
        public string? ValueJson { get; set; }
    }

    public class RegisterArticleAggregateResponse
    {
        public int ProductId { get; set; }
        public int ArticleId { get; set; }
        public List<int> ParticipantIds { get; set; } = new();
    }
}
