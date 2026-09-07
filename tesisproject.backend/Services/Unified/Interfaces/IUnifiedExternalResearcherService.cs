using tesisproject.shared.DTOs.ExternalResearcher.Request;
using tesisproject.shared.DTOs.ExternalResearcher.Response;
using tesisproject.shared.DTOs.Filters;
using tesisproject.shared.Responses;

namespace tesisproject.backend.Services.Unified.Interfaces
{
    public interface IUnifiedExternalResearcherService
    {
        // LISTS
        Task<ServiceResult<IReadOnlyList<ExternalResearcherListItemDTO>>> ListAsync(
            string? term = null,
            int? institutionId = null,
            CancellationToken ct = default);

        Task<ServiceResult<ExternalResearcherDetailDTO>> GetByIdAsync(
            int id,
            CancellationToken ct = default);

        Task<ServiceResult<List<KeyValueItemDTO>>> GetKeyValuesAsync(
            string? term = null,
            int? institutionId = null,
            int? take = null,
            CancellationToken ct = default);

        // WRITES
        Task<ServiceResult<ExternalResearcherDetailDTO>> CreateAsync(
            ExternalResearcherCreateRequestDTO request,
            CancellationToken ct = default);

        Task<ServiceResult<ExternalResearcherDetailDTO>> UpdateAsync(
            ExternalResearcherUpdateRequestDTO request,
            CancellationToken ct = default);
    }
}