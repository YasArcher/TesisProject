using tesisproject.frontend.Services.Interfaces;
using tesisproject.shared.DTOs.AppUser;

namespace tesisproject.frontend.Services.Implementations
{
    public class ExternalUserClientService : IExternalUserService
    {
        private readonly IApiClient _api;
        private readonly string _baseUrl = "groups/external-users";
        public ExternalUserClientService(IApiClient api) => _api = api;
        public Task<HttpResponseWrapper<List<ResolvedUserProfileDTO>?>> GetListAsync(CancellationToken ct = default)
        {
            return _api.GetAsync<List<ResolvedUserProfileDTO>>($"{_baseUrl}", ct);
        }
    }
}
