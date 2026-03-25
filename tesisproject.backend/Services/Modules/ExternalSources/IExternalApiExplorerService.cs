using tesisproject.shared.DTOs.ExternalApis;

namespace tesisproject.backend.Services.Interfaces
{
    public interface IExternalApiExplorerService
    {
        Task<List<ExternalApiProviderDto>> GetProvidersAsync(CancellationToken ct = default);
        Task<ExternalApiQueryResultDto> QueryAsync(ExternalApiQueryRequest request, CancellationToken ct = default);
        Task<ExternalArticlePreviewDto> EnrichArticleAsync(string providerKey, ExternalArticlePreviewDto article, CancellationToken ct = default);
    }
}
