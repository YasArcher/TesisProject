using tesisproject.shared.DTOs.Filters;
using tesisproject.shared.DTOs.Project.Response;
using tesisproject.shared.Responses;

namespace tesisproject.backend.Services.Unified.Interfaces
{
    public interface IUnifiedProjectsFiltersService
    {
        /// include: "states,types,extensionTypes" (si null => trae todo)
        Task<ServiceResult<ProjectsFilterBootstrapDTO>> GetBootstrapAsync(CancellationToken ct = default);
        
    }
}
