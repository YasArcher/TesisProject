using tesisproject.shared.DTOs.Group;

namespace tesisproject.backend.Services.Interfaces
{
    public interface IGroupService
    {
        Task<GroupResponseDTO> CreateAsync(CreateGroupRequestDTO request, CancellationToken ct);
        Task<GroupResponseDTO?> GetByIdAsync(Guid id, CancellationToken ct);
        Task<IReadOnlyList<GroupResponseDTO>> ListAsync(string? search, int skip, int take, CancellationToken ct);
        Task<GroupMemberResponseDTO> AddMemberAsync(Guid groupId, AddGroupMemberRequestDTO request, CancellationToken ct);
        Task<bool> RemoveMemberAsync(Guid groupId, Guid memberId, CancellationToken ct);
    }
}