using System.Collections.Generic;
using tesisproject.shared.DTOs.Reports;

namespace tesisproject.backend.BI.Reports
{
    public class ArticlesStatisticsReportModel
    {
        // Filtros aplicados
        public int? MinYear { get; set; }
        public int? MaxYear { get; set; }

        // Secciones a incluir
        public bool IncludeKpis { get; set; }
        public bool IncludeByYear { get; set; }
        public bool IncludeByField { get; set; }
        public bool IncludeByResearchLine { get; set; }

        // Datos
        public ArticlesKpiSummaryDto Kpis { get; set; } = new();
        public List<ArticlesByYearDto> ByYear { get; set; } = new();
        public List<ArticlesByFieldDto> ByField { get; set; } = new();
        public List<ArticlesByResearchLineDto> ByResearchLine { get; set; } = new();
    }
}
