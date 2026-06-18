// ArticlesAdvancedFilterDto.cs
namespace tesisproject.shared.DTOs.Reports
{
    public class ArticlesAdvancedFilterDto
    {
        // Filtros de tiempo
        public string? DateGranularity { get; set; } // "day", "month", "quarter", "year", "week"
        public string? DateLevel { get; set; } // "created", "publication"
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }

        // Filtros de dimensiones (múltiples selecciones)
        public List<int>? ResearchLineIds { get; set; }
        public List<int>? BroadFieldIds { get; set; }
        public List<int>? SpecificFieldIds { get; set; }
        public List<int>? DetailedFieldIds { get; set; }
        public List<int>? ProjectIds { get; set; }
        public List<int>? AcademicTermIds { get; set; }
        public List<int>? PublicationStatusIds { get; set; }
        public List<int>? VenueIds { get; set; }

        // Filtros de características
        public bool? IsOpenAccess { get; set; }
        public bool? IsProjectResult { get; set; }
        public bool? HasInterculturalComponent { get; set; }

        // Filtros de calidad
        public List<string>? Quartiles { get; set; } // "Q1", "Q2", "Q3", "Q4", "SIN_CUARTIL"

        // Filtros de indexación
        public List<int>? IndexingSourceIds { get; set; }

        // Filtros de métricas
        public decimal? MinSJR { get; set; }
        public decimal? MaxSJR { get; set; }

        // Paginación para tablas
        public int? Page { get; set; }
        public int? PageSize { get; set; }
        public string? SortBy { get; set; }
        public bool? SortDescending { get; set; }
    }
}