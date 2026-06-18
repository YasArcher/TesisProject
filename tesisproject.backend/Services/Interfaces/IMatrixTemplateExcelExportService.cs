using tesisproject.shared.DTOs.Export;
using tesisproject.shared.Responses;

namespace tesisproject.backend.Services.Interfaces
{
    public interface IMatrixTemplateExcelExportService
    {
        Task<ServiceResult<byte[]>> GenerateExcelAsync(
            ExportByTemplateRequestDTO request,
            CancellationToken ct = default);
    }
}