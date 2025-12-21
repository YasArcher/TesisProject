using tesisproject.shared.Entities.External;
using tesisproject.shared.Responses;

namespace tesisproject.backend.Services.Interfaces
{
    public interface IExternalPeriodsClient
    {
        Task<ServiceResult<IReadOnlyList<ExternalAcademicPeriodDTO>>> GetAllAsync(
            CancellationToken ct = default);

        Task<ServiceResult<IReadOnlyList<ExternalAcademicPeriodDTO>>> GetByNamesAsync(
            IEnumerable<string> names,
            CancellationToken ct = default);

        Task<ServiceResult<ExternalAcademicPeriodDTO>> GetByIdAsync(
            int id,
            CancellationToken ct = default);
    }
}