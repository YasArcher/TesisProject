using tesisproject.shared.DTOs.Export;
using tesisproject.shared.Responses;

namespace tesisproject.backend.Services.Unified.Interfaces
{
    public interface IUnifiedExportTemplateExcelService
    {
        /// <summary>
        /// Genera un archivo Excel en memoria para la plantilla indicada.
        /// Usa el dataset plano de proyectos (ProjectFlatReportService)
        /// y las columnas configuradas en ExportTemplate / ExportTemplateColumn.
        /// </summary>
        Task<ServiceResult<byte[]>> GenerateExcelAsync(
            ExportRequestDTO request,
            CancellationToken ct = default);
    }
}