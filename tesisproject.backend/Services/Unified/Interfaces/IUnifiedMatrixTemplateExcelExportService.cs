using tesisproject.shared.DTOs.Export;
using tesisproject.shared.Responses;

namespace tesisproject.backend.Services.Unified.Interfaces
{
    public interface IUnifiedMatrixTemplateExcelExportService
    {
        Task<ServiceResult<byte[]>> GenerateExcelAsync(
            ExportByTemplateRequestDTO request,
            CancellationToken ct = default);
    }
}