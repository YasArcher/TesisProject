using tesisproject.frontend.Services.Interfaces;
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
            => await _api.GetAsync<List<ExternalApiProviderDto>>("api/external-api-explorer/providers", ct)
               ?? new List<ExternalApiProviderDto>();

        public Task<ExternalApiQueryResultDto?> QueryAsync(ExternalApiQueryRequest request, CancellationToken ct = default)
            => _api.PostAsync<ExternalApiQueryRequest, ExternalApiQueryResultDto>("api/external-api-explorer/query", request, ct);

        public Task<ExternalArticlePreviewDto?> EnrichArticleAsync(string providerKey, ExternalArticlePreviewDto article, CancellationToken ct = default)
            => _api.PostAsync<ExternalArticlePreviewDto, ExternalArticlePreviewDto>($"api/external-api-explorer/providers/{providerKey}/enrich", article, ct);

        public Task<ScopusInstitutionalStagingImportResultDto?> SendScopusInstitutionalDatasetToStagingAsync(ScopusInstitutionalStagingImportRequest request, CancellationToken ct = default)
            => _api.PostAsync<ScopusInstitutionalStagingImportRequest, ScopusInstitutionalStagingImportResultDto>("api/external-api-explorer/scopus/institutional-staging", request, ct);
    }
}
