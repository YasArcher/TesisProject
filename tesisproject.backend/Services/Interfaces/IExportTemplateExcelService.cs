using tesisproject.shared.DTOs.Export;
using tesisproject.shared.Responses;

namespace tesisproject.backend.Services.Interfaces
{
    public interface IExportTemplateExcelService
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