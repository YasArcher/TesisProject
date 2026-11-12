using tesisproject.shared.DTOs.ExternalApis;
using tesisproject.shared.Responses;

namespace tesisproject.backend.Services.External.Interfaces;

// Modulo articulos: consulta externa de metadatos, sin persistencia.
public interface IArticleExternalSearchService
{
    Task<ServiceResult<ExternalApiQueryResultDto>> SearchAsync(ExternalApiQueryRequest request, CancellationToken ct);
}
