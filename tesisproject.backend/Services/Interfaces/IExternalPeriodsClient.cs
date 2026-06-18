using tesisproject.shared.Entities.External;
using tesisproject.shared.Responses;

namespace tesisproject.backend.Services.Interfaces
{
    public interface IExternalPeriodsClient
    {
        Task<ServiceResult<IReadOnlyList<ExternalAcademicPeriodModel>>> GetAllAsync(
            CancellationToken ct = default);

        Task<ServiceResult<IReadOnlyList<ExternalAcademicPeriodModel>>> GetByNamesAsync(
            IEnumerable<string> names,
            CancellationToken ct = default);

        Task<ServiceResult<ExternalAcademicPeriodModel>> GetByIdAsync(
            int id,
            CancellationToken ct = default);
    }
}