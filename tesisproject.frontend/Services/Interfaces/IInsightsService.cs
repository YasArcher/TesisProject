using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using tesisproject.shared.DTOs.Reports;

namespace tesisproject.frontend.Services.Interfaces
{
    public interface IInsightsService
    {
        Task<List<ArticlesByYearDto>> GetArticlesByYearAsync(
            CancellationToken cancellationToken = default);

        Task<List<ArticlesByFieldDto>> GetArticlesByFieldAsync(
            CancellationToken cancellationToken = default);

        Task<List<ArticlesByResearchLineDto>> GetArticlesByResearchLineAsync(
            CancellationToken cancellationToken = default);

        Task<ArticlesKpiSummaryDto?> GetArticlesKpiSummaryAsync(
            CancellationToken cancellationToken = default);
    }
}
