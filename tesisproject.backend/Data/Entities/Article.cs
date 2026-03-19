using tesisproject.backend.Data.Entities;

public class Article
{
    public int Id { get; set; }
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
    public string? ExternalSource { get; set; }
    public string? ExternalId { get; set; }
    public int? VenueId { get; set; }
    public Venue? Venue { get; set; }
    public int? AcademicTermId { get; set; }
    public AcademicTerm? AcademicTerm { get; set; }
    public byte? PublicationStatusId { get; set; }
    public PublicationStatus? PublicationStatus { get; set; }
    public int? ResearchLineId { get; set; }
    public ResearchLine? ResearchLine { get; set; }
    public int? BroadFieldId { get; set; }
    public BroadField? BroadField { get; set; }
    public int? SpecificFieldId { get; set; }
    public SpecificField? SpecificField { get; set; }
    public int? DetailedFieldId { get; set; }
    public DetailedField? DetailedField { get; set; }
    public int? ProjectId { get; set; }
    public Project? Project { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsOpenAccess { get; set; }
    public ICollection<ArticleParticipant> Participants { get; set; } = new List<ArticleParticipant>();
    public ICollection<ArticleIndexing> Indexings { get; set; } = new List<ArticleIndexing>();
    public ICollection<ArticleFile> Files { get; set; } = new List<ArticleFile>();
    public ICollection<DynamicFieldValue> DynamicFieldValues { get; set; } = new List<DynamicFieldValue>();
}
