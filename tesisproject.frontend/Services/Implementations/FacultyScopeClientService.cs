using tesisproject.frontend.Services.Interfaces;
using tesisproject.shared.DTOs.FacultyScope.Request;
using tesisproject.shared.DTOs.FacultyScope.Response;
using tesisproject.shared.Responses;

namespace tesisproject.frontend.Services.Implementations
{
    public class FacultyScopeClientService : IFacultyScopeClientService
    {
        private readonly IApiClient _api;

        // Controller: [Route("api/[controller]")] => api/facultyscopes
        private const string BaseUrl = "facultyscopes";

        public FacultyScopeClientService(IApiClient api) => _api = api;

        public Task<HttpResponseWrapper<List<FacultyScopeResponseDTO>?>> GetAllAsync(
            bool includeAssignments = false,
            CancellationToken ct = default)
        {
            // GET: api/facultyscopes?includeAssignments=true|false
            return _api.GetAsync<List<FacultyScopeResponseDTO>>(
                $"{BaseUrl}?includeAssignments={includeAssignments.ToString().ToLowerInvariant()}",
                ct);
        }

        public Task<HttpResponseWrapper<FacultyScopeResponseDTO?>> GetByIdAsync(
            int facultyScopeId,
            bool includeAssignments = false,
            CancellationToken ct = default)
        {
            // GET: api/facultyscopes/{id}?includeAssignments=true|false
            return _api.GetAsync<FacultyScopeResponseDTO>(
                $"{BaseUrl}/{facultyScopeId}?includeAssignments={includeAssignments.ToString().ToLowerInvariant()}",
                ct);
        }

        public Task<HttpResponseWrapper<FacultyScopeResponseDTO?>> CreateAsync(
            CreateFacultyScopeRequestDTO request,
            CancellationToken ct = default)
        {
            // POST: api/facultyscopes
            return _api.PostAsync<CreateFacultyScopeRequestDTO, FacultyScopeResponseDTO>(
                BaseUrl,
                request,
                ct);
        }

        public Task<HttpResponseWrapper<FacultyScopeResponseDTO?>> UpdateAsync(
            int facultyScopeId,
            UpdateFacultyScopeRequestDTO request,
            CancellationToken ct = default)
        {
            // PUT: api/facultyscopes/{id}
            return _api.PutAsync<UpdateFacultyScopeRequestDTO, FacultyScopeResponseDTO>(
                $"{BaseUrl}/{facultyScopeId}",
                request,
                ct);
        }

        public Task<HttpResponseWrapper<FacultyScopeResponseDTO?>> SetFacultiesAsync(
            int facultyScopeId,
            SetFacultyScopeFacultiesRequestDTO request,
            CancellationToken ct = default)
        {
            // PUT: api/facultyscopes/{id}/faculties
            return _api.PutAsync<SetFacultyScopeFacultiesRequestDTO, FacultyScopeResponseDTO>(
                $"{BaseUrl}/{facultyScopeId}/faculties",
                request,
                ct);
        }

        public Task<HttpResponseWrapper<bool>> AssignScopeToUserAsync(
            int facultyScopeId,
            AssignFacultyScopeUserRequestDTO request,
            CancellationToken ct = default)
        {
            return _api.PostAsync<AssignFacultyScopeUserRequestDTO, bool>(
                $"{BaseUrl}/{facultyScopeId}/assign",
                request,
                ct);
        }

        public Task<HttpResponseWrapper<NoContent?>> UnassignScopeFromUserAsync(
            int facultyScopeId,
            int userId,
            CancellationToken ct = default)
        {
            // DELETE: api/facultyscopes/{id}/assign/{userId}
            // Backend devuelve ApiResponse<bool>, pero DeleteAsync parsea ApiResponse<object> y marca success igual.
            return _api.DeleteAsync($"{BaseUrl}/{facultyScopeId}/assign/{userId}", ct);
        }

        public Task<HttpResponseWrapper<List<string>?>> GetAllowedFacultiesForUserAsync(
            int userId,
            CancellationToken ct = default)
        {
            // GET: api/facultyscopes/users/{userId}/allowed-faculties
            return _api.GetAsync<List<string>>($"{BaseUrl}/users/{userId}/allowed-faculties", ct);
        }

        public Task<HttpResponseWrapper<List<string>?>> GetMyAllowedFacultiesAsync(
            CancellationToken ct = default)
        {
            // GET: api/facultyscopes/me/allowed-faculties
            return _api.GetAsync<List<string>>($"{BaseUrl}/me/allowed-faculties", ct);
        }
    }
}