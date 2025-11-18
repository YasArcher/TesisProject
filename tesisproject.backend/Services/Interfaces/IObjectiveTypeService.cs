using tesisproject.shared.DTOs.Catalog.ObjectiveType.Request;
using tesisproject.shared.DTOs.Catalog.ObjectiveType.Response;
using tesisproject.shared.DTOs.Filters;
using tesisproject.shared.Responses;

namespace tesisproject.backend.Services.Interfaces
{
    public interface IObjectiveTypeService
    {
        Task<ServiceResult<IReadOnlyList<ObjectiveTypeListItemDTO>>> ListAsync(
            bool onlyActives = true,
            CancellationToken ct = default);

        Task<ServiceResult<ObjectiveTypeDetailDTO>> GetByIdAsync(
            int id,
            CancellationToken ct = default);

        Task<ServiceResult<ObjectiveTypeDetailDTO>> CreateAsync(
            AddObjectiveTypeRequestDTO request,
            CancellationToken ct = default);

        Task<ServiceResult<ObjectiveTypeDetailDTO>> UpdateAsync(
            UpdateObjectiveTypeRequestDTO request,
            CancellationToken ct = default);

        Task<ServiceResult<List<KeyValueItemDTO>>> GetKeyValuesAsync(
            string? term = null,
            int? take = null,
            CancellationToken ct = default);
    }
}