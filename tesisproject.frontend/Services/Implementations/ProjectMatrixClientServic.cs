using System.Net.Http.Headers;
using tesisproject.frontend.Services.Interfaces;
using tesisproject.shared.DTOs.Matrices.Response;

namespace tesisproject.frontend.Services.Implementations
{
    public class ProjectMatrixClientService : IProjectMatrixClientService
    {
        private readonly IApiClient _api;
        private readonly string _baseUrl = "matrix/projects";

        public ProjectMatrixClientService(IApiClient api)
        {
            _api = api;
        }

        // =========================
        //        UPLOAD MATRIX
        // =========================
        public async Task<HttpResponseWrapper<ProjectMatrixUploadSummaryDTO?>> UploadAsync(
            Stream fileStream,
            string fileName,
            string contentType,
            CancellationToken ct = default)
        {
            if (fileStream is null)
                throw new ArgumentNullException(nameof(fileStream));

            if (string.IsNullOrWhiteSpace(fileName))
                throw new ArgumentException("File name is required.", nameof(fileName));

            // Si no se envía contentType, usamos uno por defecto
            if (string.IsNullOrWhiteSpace(contentType))
                contentType = "application/octet-stream";

            // Crear multipart/form-data
            using var form = new MultipartFormDataContent();

            // Crear el HttpContent del archivo
            var fileContent = new StreamContent(fileStream);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);

            // Agregar al form con el nombre "file" EXACTAMENTE igual al controller
            form.Add(
                fileContent,
                name: "file",
                fileName: fileName
            );

            // POST multipart
            return await _api.PostMultipartAsync<ProjectMatrixUploadSummaryDTO>(
                $"{_baseUrl}/upload",
                form,
                ct
            );
        }

        // =========================
        //        FLAT REPORT
        // =========================
        public async Task<HttpResponseWrapper<IReadOnlyList<ProjectFlatReportDTO>?>> GetFlatReportAsync(
            CancellationToken ct = default)
        {
            // GET simple al endpoint /api/matrix/projects/flat
            return await _api.GetAsync<IReadOnlyList<ProjectFlatReportDTO>>(
                $"{_baseUrl}/flat",
                ct
            );
        }
    }
}
