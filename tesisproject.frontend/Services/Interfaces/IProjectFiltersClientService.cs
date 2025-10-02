using tesisproject.shared.DTOs.Filters;
using tesisproject.shared.Responses;

namespace tesisproject.frontend.Services.Interfaces
{
    public interface IProjectFiltersClientService
    {
        Task<HttpResponseWrapper<ProjectsFilterBootstrapDTO?>>
            GetBootstrapAsync(CancellationToken ct = default);
    }
}
