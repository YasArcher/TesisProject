using tesisproject.shared.DTOs.AppUser;
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

        Task<HttpResponseWrapper<List<GroupResponseDTO>?>> GetListAsync(int type, CancellationToken ct = default);
        Task<HttpResponseWrapper<GroupResponseDTO?>> GetByIdAsync(int id, CancellationToken ct = default);
        Task<HttpResponseWrapper<GroupResponseDTO?>> CreateAsync(AddGroupRequestDTO request, CancellationToken ct = default);
        Task<HttpResponseWrapper<GroupResponseDTO?>> UpdateAsync(UpdateGroupRequestDTO request, CancellationToken ct = default);
        Task<HttpResponseWrapper<NoContent>> DeleteAsync(int id, CancellationToken ct = default);

        // =========================
        //        MEMBERS
        // =========================

        Task<HttpResponseWrapper<List<ResolvedUserProfileDTO>?>> GetMembersByGroupIdAsync(int groupId, CancellationToken ct = default);
        Task<HttpResponseWrapper<GroupMemberResponseDTO?>> AddMemberAsync(AddGroupMemberRequestDTO request, CancellationToken ct = default);
        Task<HttpResponseWrapper<NoContent?>> RemoveMemberAsync(int groupId, int memberId, CancellationToken ct = default);

        // =========================
        //   PROJECT MEMBERS REPORT
        // =========================

        Task<HttpResponseWrapper<ProjectMembersReportDTO?>> GetProjectMembersReportAsync(int projectId, CancellationToken ct = default);
    }
}