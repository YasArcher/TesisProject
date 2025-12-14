using tesisproject.shared.Common.Utils;
using tesisproject.shared.DTOs.Matrices.Response;
using tesisproject.shared.Responses;

namespace tesisproject.backend.Services.Interfaces
{
    public interface IProjectFlatReportService
    {
        // ======================
        //       FLAT REPORT
        // ======================

        /// <summary>
        /// Returns a flat report of the whole operational database,
        /// using Project as grain and aggregating details as lists,
        /// wrapped in a ServiceResult for unified error handling.
        /// </summary>
        Task<ServiceResult<IReadOnlyList<ProjectFlatReportDTO>>> GetFlatReportAsync(
            IEnumerable<int>? projectIds = null,
            CancellationToken ct = default);
    }
}
