using tesisproject.frontend.Services.Interfaces;
using tesisproject.frontend.Services.Platform.Api;
using tesisproject.shared.DTOs.ExternalApis;

namespace tesisproject.frontend.Services.Implementations
{
    public class ExternalApiExplorerClient : IExternalApiExplorerClient
    {
        private readonly IApiClient _api;

        public ExternalApiExplorerClient(IApiClient api)
        {
            _api = api;
        }

        public async Task<List<ExternalApiProviderDto>> GetProvidersAsync(CancellationToken ct = default)
            => (await GetProvidersResultAsync(ct)).Data ?? new List<ExternalApiProviderDto>();

        public async Task<ExternalApiQueryResultDto?> QueryAsync(ExternalApiQueryRequest request, CancellationToken ct = default)
            => await RequireDataAsync(
                QueryResultAsync(request, ct),
                "No pude consultar la fuente externa.");

        public async Task<ExternalArticlePreviewDto?> EnrichArticleAsync(string providerKey, ExternalArticlePreviewDto article, CancellationToken ct = default)
            => await RequireDataAsync(
                EnrichArticleResultAsync(providerKey, article, ct),
                "No pude enriquecer el artículo con la fuente externa.");

        public Task<HttpResponseWrapper<List<ExternalApiProviderDto>?>> GetProvidersResultAsync(CancellationToken ct = default)
            => _api.GetResultAsync<List<ExternalApiProviderDto>>("api/external-api-explorer/providers", ct);

        public Task<HttpResponseWrapper<ExternalApiQueryResultDto?>> QueryResultAsync(ExternalApiQueryRequest request, CancellationToken ct = default)
            => _api.PostResultAsync<ExternalApiQueryRequest, ExternalApiQueryResultDto>("api/external-api-explorer/query", request, ct);

        public Task<HttpResponseWrapper<ExternalArticlePreviewDto?>> EnrichArticleResultAsync(string providerKey, ExternalArticlePreviewDto article, CancellationToken ct = default)
            => _api.PostResultAsync<ExternalArticlePreviewDto, ExternalArticlePreviewDto>($"api/external-api-explorer/providers/{providerKey}/enrich", article, ct);

        private static async Task<T?> RequireDataAsync<T>(Task<HttpResponseWrapper<T?>> resultTask, string fallbackMessage)
        {
            var result = await resultTask;
            if (!result.Success)
            {
                throw new InvalidOperationException(result.Message ?? fallbackMessage);
            }

            return result.Data;
        }
    }
}
