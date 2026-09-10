namespace tesisproject.backend.Data.ReadModels;

/// <summary>Read-only projection of canonical product attributes and optional article structure.</summary>
public sealed class ArticleReadModel
{
    public int ProductId { get; init; }
    public int? ProjectId { get; init; }
    public int ProductTypeId { get; init; }
    public string Title { get; init; } = string.Empty;
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
    public string? Doi { get; init; }
    public string? Journal { get; init; }
    public string? IndexingDatabase { get; init; }
    public decimal? Sjr { get; init; }
    public string? SjrRaw { get; init; }
    public string? Quartile { get; init; }
    public string? Issn { get; init; }
    public short? Year { get; init; }
    public string? YearRaw { get; init; }
    public string? PublicationUrl { get; init; }
    public int? ArticleId { get; init; }
    /// <summary>Explicit article faculty only. No fallback to the project faculty.</summary>
    public int? FacultyId { get; init; }
    public int? ProjectFacultyId { get; init; }
    public int? AcademicTermId { get; init; }
    public int? ResearchLineId { get; init; }
    public byte? PublicationStatusId { get; init; }
    public int? BroadFieldId { get; init; }
    public int? SpecificFieldId { get; init; }
    public int? DetailedFieldId { get; init; }
    public int? VenueId { get; init; }
    public string? ExternalSource { get; init; }
    public string? ExternalId { get; init; }
    public DateTime? PublishedAt { get; init; }
    public int? PageCount { get; init; }
    public bool? HasInterculturalComponent { get; init; }
    public bool? IsOpenAccess { get; init; }
    public string? ProceedingsName { get; init; }
    public string? Proceedings { get; init; }
    public string? EventName { get; init; }
    public string? GroupName { get; init; }
    public string? Filiacion { get; init; }
}
