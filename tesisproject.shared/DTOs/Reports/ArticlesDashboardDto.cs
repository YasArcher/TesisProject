using System.Collections.Generic;

namespace tesisproject.shared.DTOs.Reports
{
    public class ArticlesDashboardDto
    {
        public ArticlesDashboardFilterDto? AppliedFilter { get; set; }
        public ArticlesKpiSummaryDto? Kpis { get; set; }
        public List<ArticlesByYearDto> ByYear { get; set; } = new();
        public List<ArticlesByFieldDto> ByField { get; set; } = new();
        public List<ArticlesByResearchLineDto> ByResearchLine { get; set; } = new();
        public ArticlesQuartileStatsDto? Quartiles { get; set; }
        public ArticlesOpenAccessIndexingStatsDto? AccessIndexing { get; set; }
        public ArticlesTimeToPublicationStatsDto? TimeToPublication { get; set; }
    }
}
