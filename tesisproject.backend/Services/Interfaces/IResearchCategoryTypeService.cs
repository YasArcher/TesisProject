using tesisproject.shared.DTOs.Catalog.ResearchCategoryType.Request;
using tesisproject.shared.DTOs.Catalog.ResearchCategoryType.Response;
using tesisproject.shared.Responses;

namespace tesisproject.backend.Services.Interfaces
{
    public interface IResearchCategoryTypeService
    {
        Task<ServiceResult<IReadOnlyList<ResearchCategoryTypeListItemDTO>>> ListAsync( bool onlyActives = true, CancellationToken ct = default);
        Task<ServiceResult<ResearchCategoryTypeDetailDTO>> GetByIdAsync(int id, CancellationToken ct = default);
        Task<ServiceResult<int>> CreateAsync(ResearchCategoryTypeCreateRequestDTO dto, CancellationToken ct = default);
        Task<ServiceResult<bool>> UpdateAsync(int id, ResearchCategoryTypeUpdateRequestDTO dto, CancellationToken ct = default);
        Task<ServiceResult<bool>> DeleteAsync( int id, CancellationToken ct = default);
    }
}