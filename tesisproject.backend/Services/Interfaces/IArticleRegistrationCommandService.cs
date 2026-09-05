using tesisproject.shared.DTOs.Articles;
using tesisproject.shared.Responses;

namespace tesisproject.backend.Services.Interfaces;

public interface IArticleRegistrationCommandService
{
    Task<ServiceResult<RegisterArticleAggregateResponse>> RegisterAsync(
        RegisterArticleAggregateRequest request,
        CancellationToken ct = default);
}
