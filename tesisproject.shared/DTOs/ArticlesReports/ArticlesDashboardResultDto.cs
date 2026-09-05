using System.Collections.Generic;

namespace tesisproject.shared.DTOs.Reports
{
    public class ArticlesDashboardResultDto
    {
        public ArticlesKpiSummaryDto? Kpis { get; set; }

        public List<ArticlesByYearDto> ByYear { get; set; } = new();
        public List<ArticlesByFieldDto> ByField { get; set; } = new();
        public List<ArticlesByResearchLineDto> ByResearchLine { get; set; } = new();

        public ArticlesQuartileStatsDto? QuartileStats { get; set; }
    }
}
