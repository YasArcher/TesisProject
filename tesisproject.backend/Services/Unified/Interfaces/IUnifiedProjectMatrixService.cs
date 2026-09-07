using tesisproject.shared.Common.Utils;
using tesisproject.shared.DTOs.Matrices.Response;
using tesisproject.shared.Responses;

namespace tesisproject.backend.Services.Unified.Interfaces
{
    public interface IUnifiedProjectMatrixService
    {
        // ======================
        //        IMPORT
        // ======================

        /// <summary>
        /// Procesa la subida inicial de un archivo de matriz de proyectos.
        /// Lee el archivo y devuelve un resumen básico dentro de un ServiceResult.
        /// Más adelante aquí se dispararán las secciones:
        /// proyecto, finanzas, visitas, prórrogas, participantes, etc.
        /// </summary>

        Task<ServiceResult<ProjectMatrixUploadSummaryDTO>> UploadAsync(Stream fileStream, string fileName, string contentType, CancellationToken ct = default);
        // ======================
        //     CONFIG / EXPORT
        // ======================
        // Más adelante agregaremos:
        // - Obtener plantillas
        // - Guardar configuración de columnas
        // - Exportar matrices filtradas
    }
}
