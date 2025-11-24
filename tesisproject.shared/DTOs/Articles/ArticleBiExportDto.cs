namespace tesisproject.shared.DTOs.Articles
{
    public class ArticleBiExportDto
    {
        // Identificación básica
        public int Id { get; set; }
        public string? Title { get; set; }
        public string? Doi { get; set; }
        public short? Year { get; set; }
        public DateTime? PublishedAt { get; set; }
        public int? PageCount { get; set; }
        public string? PublicationUrl { get; set; }

        // Revista / Venue
        public string? VenueName { get; set; }
        public string? IssnCode { get; set; }
        public string? IssueNumber { get; set; }
        public string? VolumeNumber { get; set; }
        public string? JournalUrl { get; set; }

        // Clasificación académica / OCDE / Proyecto (por NOMBRE)
        public string? AcademicTermName { get; set; }
        public string? PublicationStatusName { get; set; }
        public string? ResearchLineName { get; set; }
        public string? BroadFieldName { get; set; }
        public string? SpecificFieldName { get; set; }
        public string? DetailedFieldName { get; set; }
        public string? ProjectName { get; set; }

        // Flags
        public bool IsProjectResult { get; set; }
        public bool HasInterculturalComponent { get; set; }
        public bool IsOpenAccess { get; set; }

        // Evento / grupo / filiación
        public string? ProceedingsName { get; set; }
        public string? Proceedings { get; set; }
        public string? EventName { get; set; }
        public string? GroupName { get; set; }
        public string? Filiacion { get; set; }

        // Métricas
        public decimal? Sjr { get; set; }
        public string? Quartile { get; set; }

        // Indexación (texto plano para BI)
        public string? IndexingSources { get; set; }      // "Scopus | WOS | Scielo"
        public int IndexingSourcesCount { get; set; }

        // Autores
        public string? Authors { get; set; }              // "Apellido1 Nombre1 | Apellido2 Nombre2"
        public int AuthorsCount { get; set; }
    }
}
