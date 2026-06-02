using tesisproject.backend.Services.Interfaces;
using tesisproject.backend.Services.Modules.Reporting;
using tesisproject.shared.DTOs.Articles;
using tesisproject.shared.DTOs.Imports;

namespace tesisproject.backend.Services.Implementations
{
    public class ArticleRegistrationService : IArticleRegistrationService
    {
        private readonly IArticleAggregatePersistenceService _articleAggregatePersistenceService;
        private readonly IBulkImportService _bulkImportService;
        private readonly IReportingRefreshQueue _reportingRefreshQueue;

        public ArticleRegistrationService(
            IArticleAggregatePersistenceService articleAggregatePersistenceService,
            IBulkImportService bulkImportService,
            IReportingRefreshQueue reportingRefreshQueue)
        {
            _articleAggregatePersistenceService = articleAggregatePersistenceService;
            _bulkImportService = bulkImportService;
            _reportingRefreshQueue = reportingRefreshQueue;
        }

        public async Task<RegisterArticleAggregateResponse> RegisterArticleAggregateAsync(
            RegisterArticleAggregateRequest request,
            CancellationToken ct = default)
        {
            if (request is null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            var result = await _articleAggregatePersistenceService.PersistAsync(request, ct);
            _reportingRefreshQueue.Enqueue($"Registro directo de artículo ({result.ArticleId}).");
            return result;
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
