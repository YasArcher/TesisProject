using tesisproject.shared.DTOs.Articles;

namespace tesisproject.backend.Services.Analytic.Interfaces;

// Modulo articulos: lectura agregada y de solo consulta del DW de articulos.
public interface IArticleAnalyticsService
{
    Task<ArticleAnalyticsSummaryDto> GetSummaryAsync(int? year, string? search, CancellationToken ct);
}
