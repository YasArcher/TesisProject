using tesisproject.frontend.Services.Interfaces;
using tesisproject.shared.DTOs.AppUser;
using tesisproject.shared.DTOs.Group.Request;
using tesisproject.shared.DTOs.Group.Response;
using tesisproject.shared.Responses;

namespace tesisproject.frontend.Services.Implementations
{
    public class GroupService : IGroupService
    {
        private readonly IApiClient _api;
        private readonly string _baseUrl = "api/groups";

        public GroupService(IApiClient api) => _api = api;

        // =========================
        //        GROUPS
        // =========================

        public Task<HttpResponseWrapper<List<GroupResponseDTO>?>> GetListAsync(int type, CancellationToken ct = default)
        {
            return _api.GetAsync<List<GroupResponseDTO>>($"{_baseUrl}/type/{type}", ct);
        }

        public Task<HttpResponseWrapper<GroupResponseDTO?>> GetByIdAsync(int id, CancellationToken ct = default)
        {
            return _api.GetAsync<GroupResponseDTO>($"{_baseUrl}/{id}", ct);
        }

        public Task<HttpResponseWrapper<GroupResponseDTO?>> CreateAsync(AddGroupRequestDTO request, CancellationToken ct = default)
        {
            return _api.PostAsync<AddGroupRequestDTO, GroupResponseDTO>($"{_baseUrl}", request, ct);
        }
        public Task<HttpResponseWrapper<GroupResponseDTO?>> UpdateAsync(UpdateGroupRequestDTO request, CancellationToken ct = default)
        {
            return _api.PutAsync<UpdateGroupRequestDTO, GroupResponseDTO>($"{_baseUrl}", request, ct);
        }
        public Task<HttpResponseWrapper<NoContent>> DeleteAsync(int id, CancellationToken ct = default)
        {
            throw new NotImplementedException();
        }

        // =========================
        //        MEMBERS
        // =========================

        public Task<HttpResponseWrapper<List<ResolvedUserProfileDTO>?>> GetMembersByGroupIdAsync(int groupId, CancellationToken ct = default)
        {
            return _api.GetAsync<List<ResolvedUserProfileDTO>>($"{_baseUrl}/{groupId}/members", ct);
        }

        public Task<HttpResponseWrapper<GroupMemberResponseDTO?>> AddMemberAsync(AddGroupMemberRequestDTO request, CancellationToken ct = default)
        {
            return _api.PostAsync<AddGroupMemberRequestDTO, GroupMemberResponseDTO>($"{_baseUrl}/members", request, ct);
        }

        public Task<HttpResponseWrapper<NoContent?>> RemoveMemberAsync(int groupId, int memberId, CancellationToken ct = default)
        {
            return _api.DeleteAsync($"{_baseUrl}/{groupId}/members/{memberId}", ct);
        }
    }
}
