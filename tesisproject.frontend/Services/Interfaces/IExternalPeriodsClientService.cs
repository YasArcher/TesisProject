using tesisproject.shared.Entities.External;

namespace tesisproject.frontend.Services.Interfaces
{
    public interface IExternalPeriodsClientService
    {
        Task<HttpResponseWrapper<IReadOnlyList<ExternalAcademicPeriodModel>?>> GetAllAsync(
            CancellationToken ct = default);
    }
}