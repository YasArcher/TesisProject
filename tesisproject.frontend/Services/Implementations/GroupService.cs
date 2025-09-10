using tesisproject.frontend.Services;
using tesisproject.frontend.Services.Interfaces;
using tesisproject.shared.DTOs.External;
using tesisproject.shared.DTOs.Group.Request;
using tesisproject.shared.DTOs.Group.Response;

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

        public Task<HttpResponseWrapper<List<GroupResponseDTO>?>> GetListAsync(string? search, int skip = 0, int take = 20, CancellationToken ct = default)
        {
            return _api.GetAsync<List<GroupResponseDTO>>($"{_baseUrl}?search={search}&skip={skip}&take={take}", ct);
        }

        public Task<HttpResponseWrapper<GroupResponseDTO?>> GetByIdAsync(int id, CancellationToken ct = default)
        {
            return _api.GetAsync<GroupResponseDTO>($"{_baseUrl}/{id}", ct);
        }

        public Task<HttpResponseWrapper<GroupResponseDTO?>> CreateAsync(AddGroupRequestDTO request, CancellationToken ct = default)
        {
            return _api.PostAsync<AddGroupRequestDTO, GroupResponseDTO>($"{_baseUrl}", request, ct);
        }
        public Task<HttpResponseWrapper<NoContent>> DeleteAsync(int id, CancellationToken ct = default)
        {
            throw new NotImplementedException();
        }

        // =========================
        //        MEMBERS
        // =========================

        public Task<HttpResponseWrapper<List<ExternalUserDTO>?>> GetMembersByGroupIdAsync(int groupId, CancellationToken ct = default)
        {
            return _api.GetAsync<List<ExternalUserDTO>>($"{_baseUrl}/{groupId}/external-users", ct);
        }

        public Task<HttpResponseWrapper<GroupMemberResponseDTO?>> AddMemberAsync(int groupId, AddGroupMemberRequestDTO request, CancellationToken ct = default)
        {
            return _api.PostAsync<AddGroupMemberRequestDTO, GroupMemberResponseDTO>($"{_baseUrl}/{groupId}/members", request, ct);
        }

        public Task<HttpResponseWrapper<NoContent>> RemoveMemberAsync(int groupId, int memberId, CancellationToken ct = default)
        {
            return _api.DeleteAsync($"{_baseUrl}/{groupId}/members/{memberId}", ct);
        }
    }
}
