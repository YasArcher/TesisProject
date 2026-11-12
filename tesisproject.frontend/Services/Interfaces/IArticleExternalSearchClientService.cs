using tesisproject.shared.DTOs.ExternalApis;

namespace tesisproject.frontend.Services.Interfaces;

// Modulo articulos: consulta externa de solo lectura.
public interface IArticleExternalSearchClientService
{
    Task<HttpResponseWrapper<ExternalApiQueryResultDto?>> SearchAsync(
        ExternalApiQueryRequest request, CancellationToken ct = default);
}
