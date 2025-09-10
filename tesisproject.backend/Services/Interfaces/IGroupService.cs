using tesisproject.shared.DTOs.External;
using tesisproject.shared.DTOs.Group.Request;
using tesisproject.shared.DTOs.Group.Response;

namespace tesisproject.backend.Services.Interfaces
{
    public interface IGroupService
    {
        Task<GroupResponseDTO> CreateAsync(AddGroupRequestDTO request, CancellationToken ct);
        Task<GroupResponseDTO?> GetByIdAsync(int id, CancellationToken ct);
        Task<IReadOnlyList<GroupResponseDTO>> ListAsync(string? search, int skip, int take, CancellationToken ct);
        Task<GroupMemberResponseDTO> AddMemberAsync(int groupId, AddGroupMemberRequestDTO request, CancellationToken ct);
        Task<bool> RemoveMemberAsync(int groupId, int memberId, CancellationToken ct);
        Task<List<ExternalUserDTO>> GetExternalUsersByGroupAsync(int groupId, CancellationToken ct);
    }
}