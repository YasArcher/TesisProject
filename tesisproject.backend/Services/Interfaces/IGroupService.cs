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
        Task<ServiceResult<IReadOnlyList<GroupResponseDTO>>> ListAsync(int type ,CancellationToken ct = default);
        Task<ServiceResult<GroupMemberResponseDTO>> AddMemberAsync(AddGroupMemberRequestDTO request, CancellationToken ct = default);
        Task<ServiceResult<GroupResponseDTO>> UpdateAsync(UpdateGroupRequestDTO request, CancellationToken ct = default);
        Task<ServiceResult<NoContent>> RemoveMemberAsync(int groupId, int memberId, CancellationToken ct = default);
        Task<ServiceResult<List<ExternalUserDTO>>> GetExternalUsersByGroupAsync(int groupId, CancellationToken ct = default);
        Task<ServiceResult<IReadOnlyList<GroupResponseDTO>>> GetByProjectAsync(int projectId, CancellationToken ct = default);

    }
}
