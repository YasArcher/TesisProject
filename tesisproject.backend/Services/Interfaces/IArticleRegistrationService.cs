using tesisproject.shared.DTOs.Articles;

namespace tesisproject.backend.Services.Interfaces
{
    public interface IArticleRegistrationService
    {
        Task<RegisterArticleAggregateResponse> RegisterArticleAggregateAsync(
            RegisterArticleAggregateRequest request,
            CancellationToken ct = default);
    }
}
