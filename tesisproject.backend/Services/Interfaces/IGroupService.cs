using tesisproject.shared.DTOs.External;
using tesisproject.shared.DTOs.Group.Request;
using tesisproject.shared.DTOs.Group.Response;
using tesisproject.shared.Responses;

namespace tesisproject.backend.Services.Interfaces
{
    public interface IGroupService
    {
        Task<ServiceResult<GroupResponseDTO>> CreateAsync(AddGroupRequestDTO request, CancellationToken ct = default);
        Task<ServiceResult<GroupResponseDTO>> GetByIdAsync(int id, CancellationToken ct = default);
        Task<ServiceResult<IReadOnlyList<GroupResponseDTO>>> ListAsync(string? search, int skip, int take, CancellationToken ct = default);
        Task<ServiceResult<GroupMemberResponseDTO>> AddMemberAsync(int groupId, AddGroupMemberRequestDTO request, CancellationToken ct = default);
        Task<ServiceResult<NoContent>> RemoveMemberAsync(int groupId, int memberId, CancellationToken ct = default);
        Task<ServiceResult<List<ExternalUserDTO>>> GetExternalUsersByGroupAsync(int groupId, CancellationToken ct = default);
    }
}
