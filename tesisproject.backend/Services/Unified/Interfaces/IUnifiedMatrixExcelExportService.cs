using tesisproject.shared.Responses;

namespace tesisproject.backend.Services.Unified.Interfaces
{
    public interface IUnifiedMatrixExcelExportService
    {
        /// <summary>
        /// Genera el Excel de la matriz de proyectos usando
        /// el mismo layout que la pestaña de configuración.
        /// </summary>
        Task<ServiceResult<byte[]>> GenerateExcelAsync(
            IEnumerable<int>? projectIds = null,
            CancellationToken ct = default);
    }
}