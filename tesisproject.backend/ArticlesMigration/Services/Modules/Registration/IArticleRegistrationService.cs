using tesisproject.shared.DTOs.Articles;
using tesisproject.shared.DTOs.Imports;

namespace tesisproject.backend.Services.Interfaces
{
    public interface IArticleRegistrationService
    {
        Task<RegisterArticleAggregateResponse> RegisterArticleAggregateAsync(
            RegisterArticleAggregateRequest request,
            CancellationToken ct = default);
        Task<BulkImportActionResultDto> SubmitArticleAggregateForReviewAsync(
            RegisterArticleAggregateRequest request,
            string? userId,
            CancellationToken ct = default);
    }
}
