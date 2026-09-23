using tesisproject.frontend.Services.Interfaces;
using tesisproject.shared.Responses;

namespace tesisproject.frontend.Services.Implementations;

/// <summary>
/// Modulo articulos: implementacion del cliente ETL aislada del ETL de proyectos.
/// </summary>
public sealed class ArticlesDwEtlClientService(IApiClient api) : IArticlesDwEtlClientService
{
    private const string BaseUrl = "etl/articles";

    public Task<HttpResponseWrapper<NoContent?>> RunFullLoadAsync(CancellationToken ct = default)
        => api.PostAsync<object, NoContent?>(
            $"{BaseUrl}/full-load",
            new { },
            ct);
}
