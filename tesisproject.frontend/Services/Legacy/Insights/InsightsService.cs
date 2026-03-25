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

        // Firma alineada con la interfaz (nullable)
        public Task<ArticlesQuartileStatsDto?> GetArticlesQuartileStatsAsync(
            CancellationToken cancellationToken = default)
        {
            // GET /api/reports/articles/quality/quartiles
            return _apiClient.GetAsync<ArticlesQuartileStatsDto?>(
                "api/reports/articles/quality/quartiles",
                cancellationToken);
        }

        // Permitimos filtro opcional. Si es null, mandamos objeto vacío.
        public async Task<ArticlesDashboardDto> GetArticlesDashboardAsync(
            ArticlesDashboardFilterDto? filter = null,
            CancellationToken cancellationToken = default)
        {
            filter ??= new ArticlesDashboardFilterDto();

            var result = await _apiClient.PostAsync<ArticlesDashboardFilterDto, ArticlesDashboardDto>(
                "api/reports/articles/dashboard",
                filter,
                cancellationToken);

            // Nunca devolvemos null al componente
            return result ?? new ArticlesDashboardDto
            {
                AppliedFilter = filter,
                ByYear = new List<ArticlesByYearDto>(),
                ByField = new List<ArticlesByFieldDto>(),
                ByResearchLine = new List<ArticlesByResearchLineDto>(),
                Quartiles = new ArticlesQuartileStatsDto(),
                AccessIndexing = new ArticlesOpenAccessIndexingStatsDto(),
                TimeToPublication = new ArticlesTimeToPublicationStatsDto()
            };
        }
        public async Task<List<ArticlesTimeSeriesDto>> GetArticlesTimeSeriesAsync(
            ArticlesFilterDto filter,
            CancellationToken cancellationToken = default)
        {
            var result = await _apiClient.PostAsync<ArticlesFilterDto, List<ArticlesTimeSeriesDto>>(
                "api/reports/articles/time-series",
                filter,
                cancellationToken);

            return result ?? new List<ArticlesTimeSeriesDto>();
        }
        public async Task<ArticlesDetailedResultDto> GetArticlesDetailedAsync(
            ArticlesDashboardFilterDto filter,
            CancellationToken cancellationToken = default)
        {
            filter ??= new ArticlesDashboardFilterDto();

            var result = await _apiClient.PostAsync<ArticlesDashboardFilterDto, ArticlesDetailedResultDto>(
                "api/reports/articles/detailed",
                filter,
                cancellationToken);

            return result ?? new ArticlesDetailedResultDto
            {
                AppliedFilter = filter,
                Rows = new List<ArticleReportRowDto>(),
                Kpis = new ArticlesKpiSummaryDto()
            };
        }
    }
}
