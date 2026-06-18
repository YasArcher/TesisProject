// [ARTICLES-MIGRATION] Origen: sistema de articulos. Pendiente de adaptar/fusionar con arquitectura de proyectos.
using System.Collections.Generic;

namespace tesisproject.shared.DTOs.Reports
{
    public class ArticlesDetailedResultDto
    {
        public ArticlesDashboardFilterDto? AppliedFilter { get; set; }
        public List<ArticleReportRowDto> Rows { get; set; } = new();
        public ArticlesKpiSummaryDto? Kpis { get; set; }
    }
}

