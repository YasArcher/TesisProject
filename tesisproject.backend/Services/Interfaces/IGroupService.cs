using tesisproject.shared.DTOs.AppUser;
using tesisproject.shared.DTOs.Group.Request;
using tesisproject.shared.DTOs.Group.Response;
using tesisproject.shared.Responses;

namespace tesisproject.backend.Services.Interfaces
{
    /// <summary>
    /// Orchestrates group management (members, roles, listing) and
    /// external directory lookups related to groups or specific users.
    /// </summary>
    public interface IGroupService
    {
        // ===== Groups (existing) =====
        Task<ServiceResult<GroupResponseDTO>> CreateAsync(AddGroupRequestDTO request, CancellationToken ct = default);
        Task<ServiceResult<GroupResponseDTO>> GetByIdAsync(int id, CancellationToken ct = default);
        Task<ServiceResult<IReadOnlyList<GroupResponseDTO>>> ListAsync(int type, CancellationToken ct = default);
        Task<ServiceResult<GroupMemberResponseDTO>> AddMemberAsync(AddGroupMemberRequestDTO request, CancellationToken ct = default);
        Task<ServiceResult<GroupResponseDTO>> UpdateAsync(UpdateGroupRequestDTO request, CancellationToken ct = default);
        Task<ServiceResult<NoContent>> RemoveMemberAsync(int groupId, int memberId, CancellationToken ct = default);
        Task<ServiceResult<IReadOnlyList<GroupResponseDTO>>> GetByProjectAsync(int projectId, CancellationToken ct = default);

        // ===== External users (moved from IExternalUsersService) =====
        /// <summary>
        /// Retrieves external users for a given group by resolving local members (emails/roles)
        /// and querying the external directory in batch.
        /// </summary>
        Task<ServiceResult<List<ResolvedUserProfileDTO>>> GetExternalUsersByGroupAsync(int groupId, CancellationToken ct = default);

        /// <summary>
        /// Resolves an external user profile for a given ASP.NET user id
        /// by reading the local email and querying the external directory.
        /// </summary>
        Task<ServiceResult<ResolvedUserProfileDTO>> GetExternalUserByAspNetIdAsync(int userId, CancellationToken ct = default);

        /// <summary>
        /// Retrieves an external user profile directly by institutional email.
        /// </summary>
        Task<ServiceResult<ResolvedUserProfileDTO>> GetExternalUserByEmailAsync(string institutionalEmail, CancellationToken ct = default);

        /// <summary>
        /// Retrieves all external users from the external directory (if supported).
        /// </summary>
        Task<ServiceResult<List<ResolvedUserProfileDTO>>> GetAllExternalUsersAsync(CancellationToken ct = default);
        Task<ServiceResult<ProjectMembersReportDTO>> GetProjectMembersReportAsync(
            int projectId,
            CancellationToken ct = default);
    }
}
