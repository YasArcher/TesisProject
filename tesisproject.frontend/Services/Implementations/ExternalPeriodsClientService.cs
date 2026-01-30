using tesisproject.frontend.Services.Interfaces;
using tesisproject.shared.Entities.External;

namespace tesisproject.frontend.Services.Implementations
{
    public class ExternalPeriodsClientService : IExternalPeriodsClientService
    {
        private readonly IApiClient _api;
        private readonly string _baseUrl = "external/externalperiods";

        public ExternalPeriodsClientService(IApiClient api)
            => _api = api;

        // =========================
        //           GET ALL
        // =========================

        public Task<HttpResponseWrapper<IReadOnlyList<ExternalAcademicPeriodModel>?>> GetAllAsync(
            CancellationToken ct = default)
        {
            // GET: api/external/externalperiods
            return _api.GetAsync<IReadOnlyList<ExternalAcademicPeriodModel>>(
                _baseUrl,
                ct
            );
        }
    }
}