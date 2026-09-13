using tesisproject.shared.DTOs.Articles;
using tesisproject.shared.Responses;

namespace tesisproject.backend.Services.Unified.Interfaces;

public interface IUnifiedArticleRegistrationCommandService
{
    Task<ServiceResult<RegisterArticleAggregateResponse>> RegisterAsync(
        RegisterArticleAggregateRequest request,
        CancellationToken ct = default);
}
