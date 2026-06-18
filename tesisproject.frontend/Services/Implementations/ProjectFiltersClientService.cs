using tesisproject.frontend.Services.Interfaces;
using tesisproject.shared.DTOs.Filters;

namespace tesisproject.frontend.Services.Implementations
{
    public class ProjectFiltersClientService : IProjectFiltersClientService
    {
        private readonly IApiClient _api;
        public ProjectFiltersClientService(IApiClient api) => _api = api;
        private const string BaseUrl = "projects/filters";

        public Task<HttpResponseWrapper<ProjectsFilterBootstrapDTO?>> GetBootstrapAsync(CancellationToken ct = default)
        {
           return _api.GetAsync<ProjectsFilterBootstrapDTO>($"{BaseUrl}/bootstrap", ct);
        }
    }
}
