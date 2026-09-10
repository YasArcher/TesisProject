using tesisproject.shared.DTOs.Catalog.MemberRoleType.Request;
using tesisproject.shared.DTOs.Catalog.MemberRoleType.Response;
using tesisproject.shared.DTOs.Filters;
using tesisproject.shared.Responses;

namespace tesisproject.backend.Services.Unified.Interfaces
{
    public interface IUnifiedMemberRoleTypeService
    {
        Task<ServiceResult<IReadOnlyList<MemberRoleTypeListItemDTO>>> ListAsync(
            bool onlyActives = true,
            CancellationToken ct = default);

        Task<ServiceResult<MemberRoleTypeDetailDTO>> GetByIdAsync(
            int id,
            CancellationToken ct = default);

        Task<ServiceResult<MemberRoleTypeDetailDTO>> CreateAsync(
            AddMemberRoleTypeRequestDTO request,
            CancellationToken ct = default);

        Task<ServiceResult<MemberRoleTypeDetailDTO>> UpdateAsync(
            UpdateMemberRoleTypeRequestDTO request,
            CancellationToken ct = default);

        Task<ServiceResult<List<KeyValueItemDTO>>> GetKeyValuesAsync(
            string? term = null,
            int? take = null,
            CancellationToken ct = default);
    }
}
