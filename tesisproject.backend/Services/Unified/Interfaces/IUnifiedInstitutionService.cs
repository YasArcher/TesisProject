using tesisproject.shared.DTOs.Filters;
using tesisproject.shared.DTOs.Institution.Request;
using tesisproject.shared.DTOs.Institution.Response;
using tesisproject.shared.Responses;

namespace tesisproject.backend.Services.Unified.Interfaces
{
    public interface IUnifiedInstitutionService
    {
        Task<ServiceResult<IReadOnlyList<InstitutionListItemDTO>>> ListAsync(
            bool onlyActives = true,
            CancellationToken ct = default);

        Task<ServiceResult<InstitutionDetailDTO>> GetByIdAsync(
            int id,
            CancellationToken ct = default);

        Task<ServiceResult<List<KeyValueItemDTO>>> GetKeyValuesAsync(
            string? term = null,
            int? take = null,
            CancellationToken ct = default);

        Task<ServiceResult<InstitutionDetailDTO>> CreateAsync(
            AddInstitutionRequestDTO request,
            CancellationToken ct = default);

        Task<ServiceResult<InstitutionDetailDTO>> UpdateAsync(
            UpdateInstitutionRequestDTO request,
            CancellationToken ct = default);
    }
}