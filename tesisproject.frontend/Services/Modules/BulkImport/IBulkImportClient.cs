using Microsoft.AspNetCore.Components.Forms;
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
        Task<BulkImportActionResultDto?> CorrectRowAsync(int batchId, int rowId, BulkImportRowCorrectionRequest request, CancellationToken ct = default);
        Task<BulkImportActionResultDto?> ValidateBatchAsync(int batchId, CancellationToken ct = default);
        Task<BulkImportActionResultDto?> ProcessBatchAsync(int batchId, CancellationToken ct = default);
    }
}
