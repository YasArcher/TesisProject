using Microsoft.AspNetCore.Components.Forms;
using tesisproject.frontend.Services.Platform.Api;
using tesisproject.shared.DTOs.Imports;

namespace tesisproject.frontend.Services.Interfaces
{
    public interface IBulkImportClient
    {
        Task<List<BulkImportBatchSummaryDto>> GetBatchesAsync(string? entityName = "Article", int take = 20, CancellationToken ct = default);
        Task<(byte[] Content, string FileName, string ContentType)> GenerateTemplateAsync(BulkImportTemplateRequest request, CancellationToken ct = default);
        Task<BulkImportBatchDetailDto?> UploadAsync(IBrowserFile file, string sourceType, string? notes, CancellationToken ct = default);
        Task<BulkImportBatchDetailDto?> GetBatchAsync(int batchId, int previewRows = 25, CancellationToken ct = default);
        Task<BulkImportActionResultDto?> CreateBatchFromExternalArticleAsync(ExternalArticleImportRequest request, CancellationToken ct = default);
        Task<BulkImportActionResultDto?> CreateBatchFromExternalArticlesAsync(ExternalArticlesImportRequest request, CancellationToken ct = default);
        Task<BulkImportActionResultDto?> CorrectRowAsync(int batchId, int rowId, BulkImportRowCorrectionRequest request, CancellationToken ct = default);
        Task<BulkImportActionResultDto?> ValidateBatchAsync(int batchId, CancellationToken ct = default);
        Task<BulkImportActionResultDto?> ProcessBatchAsync(int batchId, CancellationToken ct = default);

        Task<HttpResponseWrapper<List<BulkImportBatchSummaryDto>?>> GetBatchesResultAsync(string? entityName = "Article", int take = 20, CancellationToken ct = default);
        Task<HttpResponseWrapper<BulkImportBatchDetailDto?>> GetBatchResultAsync(int batchId, int previewRows = 25, CancellationToken ct = default);
        Task<HttpResponseWrapper<BulkImportActionResultDto?>> CreateBatchFromExternalArticleResultAsync(ExternalArticleImportRequest request, CancellationToken ct = default);
        Task<HttpResponseWrapper<BulkImportActionResultDto?>> CreateBatchFromExternalArticlesResultAsync(ExternalArticlesImportRequest request, CancellationToken ct = default);
        Task<HttpResponseWrapper<BulkImportActionResultDto?>> CorrectRowResultAsync(int batchId, int rowId, BulkImportRowCorrectionRequest request, CancellationToken ct = default);
        Task<HttpResponseWrapper<BulkImportActionResultDto?>> ValidateBatchResultAsync(int batchId, CancellationToken ct = default);
        Task<HttpResponseWrapper<BulkImportActionResultDto?>> ProcessBatchResultAsync(int batchId, CancellationToken ct = default);
    }
}
