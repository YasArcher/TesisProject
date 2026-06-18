using tesisproject.shared.DTOs.Articles;

namespace tesisproject.backend.Services.Interfaces
{
    public interface IArticleAggregatePersistenceService
    {
        Task ValidateRequestAsync(RegisterArticleAggregateRequest request, CancellationToken ct = default);
        Task<RegisterArticleAggregateResponse> PersistAsync(RegisterArticleAggregateRequest request, CancellationToken ct = default);
    }
}
