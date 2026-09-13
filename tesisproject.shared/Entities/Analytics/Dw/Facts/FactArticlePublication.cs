using System.ComponentModel.DataAnnotations;
using tesisproject.shared.Entities.Analytics.Dw.Dimensions;

namespace tesisproject.shared.Entities.Analytics.Dw.Facts;

public class FactArticlePublication
{
    [Key]
    public int FactArticlePublicationId { get; set; }

    public int ArticleKey { get; set; }
    public int ProductId { get; set; }
    public int ProductTypeKey { get; set; }
    public int? JournalKey { get; set; }
    public int? IndexingDatabaseKey { get; set; }
    public int? QuartileKey { get; set; }
    public int? VenueKey { get; set; }
    public int? AcademicTermKey { get; set; }
    public int? PublicationStatusKey { get; set; }
    public int? ResearchLineKey { get; set; }
    public int? FieldKey { get; set; }
    public int? ArticleFacultyKey { get; set; }
    public int? ProjectFacultyKey { get; set; }
    public int? ProjectId { get; set; }
    public int CreatedDateKey { get; set; }
    public int? PublishedDateKey { get; set; }

    public int ArticleCount { get; set; } = 1;
    public int AuthorCount { get; set; }
    public int IndexingCount { get; set; }
    public int? PageCount { get; set; }
    public decimal? Sjr { get; set; }
    public bool IsProjectResultFlag { get; set; }
    public bool? IsOpenAccessFlag { get; set; }
    public bool? HasInterculturalFlag { get; set; }

    public DimArticle Article { get; set; } = null!;
    public DimProductType ProductType { get; set; } = null!;
    public DimJournal? Journal { get; set; }
    public DimIndexingDatabase? IndexingDatabase { get; set; }
    public DimQuartile? Quartile { get; set; }
    public DimVenue? Venue { get; set; }
    public DimAcademicTerm? AcademicTerm { get; set; }
    public DimPublicationStatus? PublicationStatus { get; set; }
    public DimResearchLine? ResearchLine { get; set; }
    public DimField? Field { get; set; }
    public DimFaculty? ArticleFaculty { get; set; }
    public DimFaculty? ProjectFaculty { get; set; }
    public DimDate CreatedDate { get; set; } = null!;
    public DimDate? PublishedDate { get; set; }
}
