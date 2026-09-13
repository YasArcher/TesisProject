using tesisproject.shared.Responses;

namespace tesisproject.backend.Services.Analytic.Interfaces;

public interface IArticlesDwEtlService
{
    Task<ServiceResult<NoContent>> RunFullLoadAsync(CancellationToken ct = default);
}
