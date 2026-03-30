using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using tesisproject.shared.DTOs.Imports;
using tesisproject.shared.DTOs.MassRegistration;

namespace tesisproject.backend.Services.Interfaces
{
    public interface IBulkImportService
    {
        Task<List<BulkImportBatchSummaryDto>> GetBatchesAsync(string? entityName = null, int take = 20, CancellationToken ct = default);
        Task<BulkImportTemplateDescriptorDto> GetTemplateDescriptorAsync(BulkImportTemplateRequest request, CancellationToken ct = default);
        Task<(byte[] Content, string FileName)> GenerateTemplateAsync(BulkImportTemplateRequest request, CancellationToken ct = default);
        Task<BulkImportBatchDetailDto> CreateBatchFromFileAsync(Stream fileStream, string fileName, string sourceType, string? notes, string? userId, CancellationToken ct = default);
    Task<BulkImportBatchDetailDto?> GetBatchAsync(int batchId, int previewRows = 25, CancellationToken ct = default);
    Task<BulkImportActionResultDto> CreateBatchFromExternalArticleAsync(ExternalArticleImportRequest request, string? userId, CancellationToken ct = default);
    Task<BulkImportActionResultDto> CreateBatchFromExternalArticlesAsync(ExternalArticlesImportRequest request, string? userId, CancellationToken ct = default);
    Task<BulkImportActionResultDto> CreateBatchFromMatrixAsync(RegistrationMatrixDetailDto matrix, bool validateAfterCreate, string? userId, CancellationToken ct = default);
    Task<BulkImportActionResultDto> CorrectRowAsync(int batchId, int rowId, BulkImportRowCorrectionRequest request, CancellationToken ct = default);
        Task<BulkImportActionResultDto> ValidateBatchAsync(int batchId, CancellationToken ct = default);
        Task<BulkImportActionResultDto> ProcessBatchAsync(int batchId, CancellationToken ct = default);
    }
}
