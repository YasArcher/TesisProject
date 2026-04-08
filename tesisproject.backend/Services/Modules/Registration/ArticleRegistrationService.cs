using tesisproject.backend.Services.Interfaces;
using tesisproject.shared.DTOs.Articles;
using tesisproject.shared.DTOs.Imports;

namespace tesisproject.backend.Services.Implementations
{
    public class ArticleRegistrationService : IArticleRegistrationService
    {
        private readonly IArticleAggregatePersistenceService _articleAggregatePersistenceService;
        private readonly IBulkImportService _bulkImportService;

        public ArticleRegistrationService(
            IArticleAggregatePersistenceService articleAggregatePersistenceService,
            IBulkImportService bulkImportService)
        {
            _articleAggregatePersistenceService = articleAggregatePersistenceService;
            _bulkImportService = bulkImportService;
        }

        public async Task<RegisterArticleAggregateResponse> RegisterArticleAggregateAsync(
            RegisterArticleAggregateRequest request,
            CancellationToken ct = default)
        {
            if (request is null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            return await _articleAggregatePersistenceService.PersistAsync(request, ct);
        }

        public async Task<BulkImportActionResultDto> SubmitArticleAggregateForReviewAsync(
            RegisterArticleAggregateRequest request,
            string? userId,
            CancellationToken ct = default)
        {
            if (request is null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            await _articleAggregatePersistenceService.ValidateRequestAsync(request, ct);
            return await _bulkImportService.CreateBatchFromAuthorSubmissionAsync(request, validateAfterCreate: true, userId, ct);
        }
    }
}
