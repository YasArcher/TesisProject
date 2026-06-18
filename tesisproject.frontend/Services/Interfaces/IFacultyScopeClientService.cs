using tesisproject.shared.DTOs.FacultyScope.Request;
using tesisproject.shared.DTOs.FacultyScope.Response;
using tesisproject.shared.Responses;

namespace tesisproject.frontend.Services.Interfaces
{
    public interface IFacultyScopeClientService
    {
        Task<HttpResponseWrapper<List<FacultyScopeResponseDTO>?>> GetAllAsync(
            bool includeAssignments = false,
            CancellationToken ct = default);

        Task<HttpResponseWrapper<FacultyScopeResponseDTO?>> GetByIdAsync(
            int facultyScopeId,
            bool includeAssignments = false,
            CancellationToken ct = default);

        Task<HttpResponseWrapper<FacultyScopeResponseDTO?>> CreateAsync(
            CreateFacultyScopeRequestDTO request,
            CancellationToken ct = default);

        Task<HttpResponseWrapper<FacultyScopeResponseDTO?>> UpdateAsync(
            int facultyScopeId,
            UpdateFacultyScopeRequestDTO request,
            CancellationToken ct = default);

        Task<HttpResponseWrapper<FacultyScopeResponseDTO?>> SetFacultiesAsync(
            int facultyScopeId,
            SetFacultyScopeFacultiesRequestDTO request,
            CancellationToken ct = default);

        Task<HttpResponseWrapper<bool>> AssignScopeToUserAsync(
            int facultyScopeId,
            AssignFacultyScopeUserRequestDTO request,
            CancellationToken ct = default);

        Task<HttpResponseWrapper<NoContent?>> UnassignScopeFromUserAsync(
            int facultyScopeId,
            int userId,
            CancellationToken ct = default);

        Task<HttpResponseWrapper<List<string>?>> GetAllowedFacultiesForUserAsync(
            int userId,
            CancellationToken ct = default);

        Task<HttpResponseWrapper<List<string>?>> GetMyAllowedFacultiesAsync(
            CancellationToken ct = default);
    }
}
