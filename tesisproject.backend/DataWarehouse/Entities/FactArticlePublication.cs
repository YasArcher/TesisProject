namespace tesisproject.backend.DataWarehouse.Entities
{
    public class FactArticlePublication
    {
        public int Id { get; set; } 

        public int ArticleKey { get; set; }
        public DimArticle Article { get; set; } = null!;

        public int? VenueKey { get; set; }
        public DimVenue? Venue { get; set; }

        public int? FieldKey { get; set; }
        public DimField? Field { get; set; }

        public int? ResearchLineKey { get; set; }
        public DimResearchLine? ResearchLine { get; set; }

        public int? PublicationStatusKey { get; set; }
        public DimPublicationStatus? PublicationStatus { get; set; }

        public int? ProjectKey { get; set; }
        public DimProject? Project { get; set; }

        public int? AcademicTermKey { get; set; }
        public DimAcademicTerm? AcademicTerm { get; set; }

        public int CreatedDateKey { get; set; }
        public DimDate CreatedDate { get; set; } = null!;

        public int? PublicationDateKey { get; set; }
        public DimDate? PublicationDate { get; set; }

        // Métricas
        public int ArticleCount { get; set; }
        public int AuthorCount { get; set; }
        public int IndexingCount { get; set; }
        public int? PageCount { get; set; }

        public bool IsOpenAccess { get; set; }
        public bool IsProjectResult { get; set; }
        public bool HasInterculturalComponent { get; set; }

        public decimal? SJR { get; set; }
        public string? Quartile { get; set; }
    }
}
