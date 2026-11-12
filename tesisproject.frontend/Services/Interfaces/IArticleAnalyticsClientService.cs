using tesisproject.shared.DTOs.Articles;

namespace tesisproject.frontend.Services.Interfaces;

// Modulo articulos: cliente de agregados analiticos para reporteria e inteligencia.
public interface IArticleAnalyticsClientService
{
    Task<HttpResponseWrapper<ArticleAnalyticsSummaryDto?>> GetSummaryAsync(
        int? year, string? search, CancellationToken ct = default);
}
