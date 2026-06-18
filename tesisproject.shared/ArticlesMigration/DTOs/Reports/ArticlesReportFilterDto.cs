using System;
using System.Collections.Generic;

namespace tesisproject.shared.DTOs.Reports
{
    public class ArticlesReportFilterDto
    {
        // ====== Dimensión Tiempo ======
        public List<int>? Years { get; set; }
        public List<int>? Quarters { get; set; }
        public List<int>? Months { get; set; }
        public List<int>? Weeks { get; set; }
        public List<int>? Days { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }

        // ====== Dimensión Académica ======
        public List<int>? ResearchLineIds { get; set; }
        public List<int>? BroadFieldIds { get; set; }
        public List<int>? SpecificFieldIds { get; set; }
        public List<int>? DetailedFieldIds { get; set; }

        // ====== Dimensión de Proyectos / Programas ======
        public List<int>? ProjectIds { get; set; }
        public List<int>? AcademicTermIds { get; set; }

        // ====== Dimensión de Publicación ======
        public List<int>? StatusIds { get; set; }
        public List<int>? Quartiles { get; set; }
        public List<int>? IndexingSourceIds { get; set; }
        public List<int>? VenueIds { get; set; }

        public bool? IsOpenAccess { get; set; }
        public bool? IndexedInScopus { get; set; }
    }
}
