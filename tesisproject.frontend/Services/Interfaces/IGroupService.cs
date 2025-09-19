using tesisproject.shared.DTOs.External;
using tesisproject.shared.DTOs.Group.Request;
using tesisproject.shared.DTOs.Group.Response;
using tesisproject.shared.Responses;

namespace tesisproject.frontend.Services.Interfaces
{
    public interface IGroupService
    {
        // =========================
        //        GROUPS
        // =========================
        /// <summary>Get a paginated list of groups.</summary>
        Task<HttpResponseWrapper<List<GroupResponseDTO>?>> GetListAsync( CancellationToken ct = default);

        /// <summary>Get a single group by its id.</summary>
        Task<HttpResponseWrapper<GroupResponseDTO?>> GetByIdAsync(int id, CancellationToken ct = default);

        /// <summary>Create a new group.</summary>
        Task<HttpResponseWrapper<GroupResponseDTO?>> CreateAsync(AddGroupRequestDTO request, CancellationToken ct = default);
        /// <summary>Update a group.</summary>
        Task<HttpResponseWrapper<GroupResponseDTO?>> UpdateAsync(UpdateGroupRequestDTO request, CancellationToken ct = default);
        /// <summary>Delete a group.</summary>
        Task<HttpResponseWrapper<NoContent>> DeleteAsync(int id, CancellationToken ct = default);

        // =========================
        //        MEMBERS
        // =========================
        /// <summary>Get external users (members) by group id.</summary>
        Task<HttpResponseWrapper<List<ExternalUserDTO>?>> GetMembersByGroupIdAsync(int groupId, CancellationToken ct = default);

        /// <summary>Add a member to an existing group.</summary>
        Task<HttpResponseWrapper<GroupMemberResponseDTO?>> AddMemberAsync(AddGroupMemberRequestDTO request, CancellationToken ct = default);

        /// <summary>Remove a member from a group.</summary>
        Task<HttpResponseWrapper<NoContent?>> RemoveMemberAsync(int groupId, int memberId, CancellationToken ct = default);

        // Future:
        // Task<HttpResponseWrapper<NoContent>> UpdateMemberRoleAsync(int groupId, Guid memberId, UpdateRoleRequestDto request, CancellationToken ct = default);
    }
}
