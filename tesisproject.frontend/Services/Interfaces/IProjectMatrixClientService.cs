using tesisproject.shared.DTOs.Matrices.Response;

namespace tesisproject.frontend.Services.Interfaces
{
    public interface IProjectMatrixClientService
    {
        // =========================
        //        UPLOAD MATRIX
        // =========================
        Task<HttpResponseWrapper<ProjectMatrixUploadSummaryDTO?>> UploadAsync(
            Stream fileStream,
            string fileName,
            string contentType,
            CancellationToken ct = default);

        // =========================
        //        FLAT REPORT
        // =========================
        Task<HttpResponseWrapper<IReadOnlyList<ProjectFlatReportDTO>?>> GetFlatReportAsync(
            CancellationToken ct = default);
    }
}