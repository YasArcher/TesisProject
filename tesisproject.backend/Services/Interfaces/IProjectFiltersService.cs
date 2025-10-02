using tesisproject.shared.DTOs.Filters;
using tesisproject.shared.Responses;

namespace tesisproject.backend.Services.Interfaces
{
    public interface IProjectFiltersService
    {
        
        Task<ServiceResult<ProjectsFilterBootstrapDTO>> GetBootstrapAsync(string? include, CancellationToken ct = default);
    }
}
