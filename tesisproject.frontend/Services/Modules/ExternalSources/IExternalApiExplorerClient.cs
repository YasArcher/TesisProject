using tesisproject.shared.DTOs.ExternalApis;
using tesisproject.frontend.Services.Platform.Api;

namespace tesisproject.frontend.Services.Interfaces
{
    public interface IExternalApiExplorerClient
    {
        Task<List<ExternalApiProviderDto>> GetProvidersAsync(CancellationToken ct = default);
        Task<ExternalApiQueryResultDto?> QueryAsync(ExternalApiQueryRequest request, CancellationToken ct = default);
        Task<ExternalArticlePreviewDto?> EnrichArticleAsync(string providerKey, ExternalArticlePreviewDto article, CancellationToken ct = default);

        Task<HttpResponseWrapper<List<ExternalApiProviderDto>?>> GetProvidersResultAsync(CancellationToken ct = default);
        Task<HttpResponseWrapper<ExternalApiQueryResultDto?>> QueryResultAsync(ExternalApiQueryRequest request, CancellationToken ct = default);
        Task<HttpResponseWrapper<ExternalArticlePreviewDto?>> EnrichArticleResultAsync(string providerKey, ExternalArticlePreviewDto article, CancellationToken ct = default);
    }
}
