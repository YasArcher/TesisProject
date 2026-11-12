using tesisproject.frontend.Services.Interfaces;
using tesisproject.shared.DTOs.ExternalApis;

namespace tesisproject.frontend.Services.Implementations;

// Modulo articulos: mantiene las credenciales del proveedor fuera del navegador.
public sealed class ArticleExternalSearchClientService(IApiClient api) : IArticleExternalSearchClientService
{
    public Task<HttpResponseWrapper<ExternalApiQueryResultDto?>> SearchAsync(
        ExternalApiQueryRequest request, CancellationToken ct = default)
        => api.PostAsync<ExternalApiQueryRequest, ExternalApiQueryResultDto>(
            "articles/external-search", request, ct);
}
