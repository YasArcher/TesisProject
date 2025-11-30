using tesisproject.shared.DTOs.Catalog.FundingType.Request;
using tesisproject.shared.DTOs.Catalog.FundingType.Response;
using tesisproject.shared.DTOs.Catalog.ResearchCategoryGroup.Request;
using tesisproject.shared.DTOs.Catalog.ResearchCategoryGroup.Response;
using tesisproject.shared.DTOs.Filters;
using tesisproject.shared.Responses;

namespace tesisproject.backend.Services.Interfaces
{
    public interface IResearchCategoryGroupService
    {
        Task<ServiceResult<IReadOnlyList<ResearchCategoryGroupListItemDTO>>> ListAsync(
            bool onlyActives = true,
            CancellationToken ct = default);

        Task<ServiceResult<ResearchCategoryGroupListItemDTO>> GetByIdAsync(
            int id,
            CancellationToken ct = default);

        Task<ServiceResult<ResearchCategoryGroupListItemDTO>> CreateAsync(
            AddResearchCategoryGroupDTO request,
            CancellationToken ct = default);

        Task<ServiceResult<ResearchCategoryGroupListItemDTO>> UpdateAsync(
            UpdateResearchCategoryGroupDTO request,
            CancellationToken ct = default);

        Task<ServiceResult<List<KeyValueItemDTO>>> GetKeyValuesAsync(
            string? term = null,
            int? take = null,
            CancellationToken ct = default);
    }
}
