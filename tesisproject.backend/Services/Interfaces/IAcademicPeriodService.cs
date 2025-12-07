using tesisproject.shared.DTOs.Catalog.AcademicPeriod.Request;
using tesisproject.shared.DTOs.Catalog.AcademicPeriod.Response;
using tesisproject.shared.DTOs.Filters;
using tesisproject.shared.Responses;

namespace tesisproject.backend.Services.Interfaces
{
    public interface IAcademicPeriodService
    {
        Task<ServiceResult<IReadOnlyList<AcademicPeriodListItemDTO>>> ListAsync(
            bool onlyActives = true,
            CancellationToken ct = default);

        Task<ServiceResult<AcademicPeriodListItemDTO>> GetByIdAsync(
            int id,
            CancellationToken ct = default);

        Task<ServiceResult<List<KeyValueItemDTO>>> GetKeyValuesAsync(
            string? term = null,
            int? take = null,
            CancellationToken ct = default);

        Task<ServiceResult<AcademicPeriodListItemDTO>> CreateAsync(
            AcademicPeriodCreateRequestDTO request,
            CancellationToken ct = default);

        Task<ServiceResult<AcademicPeriodListItemDTO>> UpdateAsync(
            AcademicPeriodUpdateRequestDTO request,
            CancellationToken ct = default);
    }
}