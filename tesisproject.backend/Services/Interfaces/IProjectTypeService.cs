
using tesisproject.shared.DTOs.Catalog.ProjectType.Request;
using tesisproject.shared.DTOs.Catalog.ProjectType.Response;
using tesisproject.shared.DTOs.Filters;
using tesisproject.shared.Responses;

namespace tesisproject.backend.Services.Interfaces
{
    public interface IProjectTypeService
    {
        Task<ServiceResult<IReadOnlyList<ProjectTypeListItemDTO>>> ListAsync(
            bool onlyActives = true,
            CancellationToken ct = default);

        Task<ServiceResult<ProjectTypeDetailDTO>> GetByIdAsync(
            int id,
            CancellationToken ct = default);

        Task<ServiceResult<ProjectTypeDetailDTO>> CreateAsync(
            AddProjectTypeRequestDTO request,
            CancellationToken ct = default);

        Task<ServiceResult<ProjectTypeDetailDTO>> UpdateAsync(
            UpdateProjectTypeRequestDTO request,
            CancellationToken ct = default);

        Task<ServiceResult<List<KeyValueItemDTO>>> GetKeyValuesAsync(
            string? term = null,
            int? take = null,
            CancellationToken ct = default);
    }
}
