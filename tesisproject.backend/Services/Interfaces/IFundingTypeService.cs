using tesisproject.shared.DTOs.Catalog.FundingType.Request;
using tesisproject.shared.DTOs.Catalog.FundingType.Response;
using tesisproject.shared.DTOs.Filters;
using tesisproject.shared.Responses;

namespace tesisproject.backend.Services.Interfaces
{
    public interface IFundingTypeService
    {
        Task<ServiceResult<IReadOnlyList<FundingTypeListItemDTO>>> ListAsync(
            bool onlyActives = true,
            CancellationToken ct = default);

        Task<ServiceResult<FundingTypeListItemDTO>> GetByIdAsync(
            int id,
            CancellationToken ct = default);

        Task<ServiceResult<FundingTypeListItemDTO>> CreateAsync(
            AddFundingTypeRequestDTO request,
            CancellationToken ct = default);

        Task<ServiceResult<FundingTypeListItemDTO>> UpdateAsync(
            UpdateFundingTypeRequestDTO request,
            CancellationToken ct = default);

        Task<ServiceResult<List<KeyValueItemDTO>>> GetKeyValuesAsync(
            string? term = null,
            int? take = null,
            CancellationToken ct = default);
    }
}