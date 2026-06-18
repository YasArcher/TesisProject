using tesisproject.shared.DTOs.FacultyScope.Request;
using tesisproject.shared.DTOs.FacultyScope.Response;
using tesisproject.shared.Responses;

namespace tesisproject.backend.Services.Interfaces
{
    public interface IFacultyScopeService
    {
        Task<ServiceResult<IReadOnlyList<FacultyScopeResponseDTO>>> GetAllAsync(
            bool includeAssignments = false,
            CancellationToken ct = default);

        Task<ServiceResult<FacultyScopeResponseDTO>> GetByIdAsync(
            int facultyScopeId,
            bool includeAssignments = false,
            CancellationToken ct = default);

        Task<ServiceResult<FacultyScopeResponseDTO>> CreateAsync(
            CreateFacultyScopeRequestDTO request,
            CancellationToken ct = default);

        Task<ServiceResult<FacultyScopeResponseDTO>> UpdateAsync(
            int facultyScopeId,
            UpdateFacultyScopeRequestDTO request,
            CancellationToken ct = default);

        Task<ServiceResult<FacultyScopeResponseDTO>> SetFacultiesAsync(
            int facultyScopeId,
            SetFacultyScopeFacultiesRequestDTO request,
            CancellationToken ct = default);

        Task<ServiceResult<bool>> AssignScopeToUserAsync(
            int facultyScopeId,
            AssignFacultyScopeUserRequestDTO request,
            CancellationToken ct = default);

        Task<ServiceResult<bool>> UnassignScopeFromUserAsync(
            int facultyScopeId,
            int identityUserId, // AppUser.IdUser
            CancellationToken ct = default);

        /// <summary>
        /// Para tus GET filtrados: devuelve las FacultyId permitidas para ese usuario
        /// (assignments activos + faculties activas).
        /// </summary>
        Task<ServiceResult<IReadOnlyList<int>>> GetAllowedFacultyIdsForUserAsync(
            CancellationToken ct = default);
    }
}