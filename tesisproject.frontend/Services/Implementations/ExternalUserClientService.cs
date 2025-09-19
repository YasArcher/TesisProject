using tesisproject.frontend.Services.Interfaces;
using tesisproject.shared.DTOs.External;

namespace tesisproject.frontend.Services.Implementations
{
    public class ExternalUserClientService : IExternalUserService
    {
        private readonly IApiClient _api;
        private readonly string _baseUrl = "api/external/ExternalUsers";
        public ExternalUserClientService(IApiClient api) => _api = api;
        public Task<HttpResponseWrapper<List<ExternalUserDTO>?>> GetListAsync(CancellationToken ct = default)
        {
            return _api.GetAsync<List<ExternalUserDTO>>($"{_baseUrl}", ct);
        }
    }
}
