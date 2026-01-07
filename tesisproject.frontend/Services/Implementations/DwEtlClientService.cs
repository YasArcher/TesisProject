using tesisproject.frontend.Services.Interfaces;
using tesisproject.shared.Responses;

namespace tesisproject.frontend.Services.Implementations
{
    public class DwEtlClientService : IDwEtlClientService
    {
        private readonly IApiClient _api;
        private const string BaseUrl = "api/analytics/etl";

        public DwEtlClientService(IApiClient api)
            => _api = api;

        public Task<HttpResponseWrapper<NoContent>> RunFullAsync(CancellationToken ct = default)
        {
            // POST: api/analytics/etl/run-full
            // IApiClient requires a body => send an empty payload.
            return _api.PostAsync<object, NoContent>(
                $"{BaseUrl}/run-full",
                new { },
                ct
            );
        }
    }
}