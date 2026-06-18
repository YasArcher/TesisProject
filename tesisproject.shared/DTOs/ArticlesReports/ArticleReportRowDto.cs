// [ARTICLES-MIGRATION] Origen: sistema de articulos. Pendiente de adaptar/fusionar con arquitectura de proyectos.
using System;

namespace tesisproject.shared.DTOs.Reports
{
    public class ArticleReportRowDto
    {
        public int ArticleKey { get; set; }

        // Fechas crudas
        public DateTime? CreatedDate { get; set; }
        public DateTime? PublicationDate { get; set; }

        // Desglose de fecha (para filtros por año/mes/día en el frontend)
        public int? CreatedYear { get; set; }
        public int? CreatedMonth { get; set; }
        public int? CreatedDay { get; set; }

        public int? PublicationYear { get; set; }
        public int? PublicationMonth { get; set; }
        public int? PublicationDay { get; set; }

        // Académico / proyecto
        public string? AcademicTermName { get; set; }
        public string? ProjectName { get; set; }
        public string? ResearchLineName { get; set; }

        // OCDE
        public string? BroadFieldName { get; set; }
        public string? SpecificFieldName { get; set; }
        public string? DetailedFieldName { get; set; }

        // Publicación
        public string? PublicationStatusName { get; set; }
        public string? VenueName { get; set; }

        // Calidad / acceso
        public string? Quartile { get; set; }
        public bool IsOpenAccess { get; set; }
        public bool IndexedInScopus { get; set; }

        // Métrica principal (conteo del fact)
        public int ArticleCount { get; set; }
    }
}

