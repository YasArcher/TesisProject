using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using tesisproject.frontend.Services.Interfaces;
using tesisproject.shared.DTOs.Reports;

namespace tesisproject.frontend.Services.Implementations
{
    public class InsightsService : IInsightsService
    {
        private readonly IApiClient _apiClient;

        public InsightsService(IApiClient apiClient)
        {
            _apiClient = apiClient;
        }

        public async Task<List<ArticlesByYearDto>> GetArticlesByYearAsync(
            CancellationToken cancellationToken = default)
        {
            // GET /api/reports/articles/by-year
            var result = await _apiClient.GetAsync<List<ArticlesByYearDto>>(
                "api/reports/articles/by-year",
                cancellationToken);

            // Normalizamos null -> lista vacía
            return result ?? new List<ArticlesByYearDto>();
        }

        public async Task<List<ArticlesByFieldDto>> GetArticlesByFieldAsync(
            CancellationToken cancellationToken = default)
        {
            // GET /api/reports/articles/by-field
            var result = await _apiClient.GetAsync<List<ArticlesByFieldDto>>(
                "api/reports/articles/by-field",
                cancellationToken);

            return result ?? new List<ArticlesByFieldDto>();
        }

        public async Task<List<ArticlesByResearchLineDto>> GetArticlesByResearchLineAsync(
            CancellationToken cancellationToken = default)
        {
            // GET /api/reports/articles/by-research-line
            var result = await _apiClient.GetAsync<List<ArticlesByResearchLineDto>>(
                "api/reports/articles/by-research-line",
                cancellationToken);

            return result ?? new List<ArticlesByResearchLineDto>();
        }

        public Task<ArticlesKpiSummaryDto?> GetArticlesKpiSummaryAsync(
            CancellationToken cancellationToken = default)
        {
            // GET /api/reports/articles/kpis/summary
            return _apiClient.GetAsync<ArticlesKpiSummaryDto?>(
                "api/reports/articles/kpis/summary",
                cancellationToken);
        }
    }
}
