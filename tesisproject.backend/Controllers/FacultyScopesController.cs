using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using tesisproject.backend.Controllers.Extensions;
using tesisproject.backend.Services.Interfaces;
using tesisproject.backend.Utils;
using tesisproject.shared.DTOs.FacultyScope.Request;
using tesisproject.shared.DTOs.FacultyScope.Response;
using tesisproject.shared.Responses;

namespace tesisproject.backend.Controllers
{
    [Authorize(Roles = "superadmin")]
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class FacultyScopesController : ControllerBase
    {
        private readonly IFacultyScopeService _service;

        public FacultyScopesController(IFacultyScopeService service) => _service = service;

        // ========================= Scopes (CRUD) =========================

        [HttpGet]
        public async Task<ActionResult<ApiResponse<IReadOnlyList<FacultyScopeResponseDTO>>>> GetAll(
            [FromQuery] bool includeAssignments = false,
            CancellationToken ct = default)
            => (await _service.GetAllAsync(includeAssignments, ct)).ToActionResult();

        [HttpGet("{id:int}")]
        public async Task<ActionResult<ApiResponse<FacultyScopeResponseDTO>>> GetById(
            int id,
            [FromQuery] bool includeAssignments = false,
            CancellationToken ct = default)
            => (await _service.GetByIdAsync(id, includeAssignments, ct)).ToActionResult();

        [HttpPost]
        public async Task<ActionResult<ApiResponse<FacultyScopeResponseDTO>>> Create(
            [FromBody] CreateFacultyScopeRequestDTO request,
            CancellationToken ct = default)
            => (await _service.CreateAsync(request, ct)).ToActionResult();

        [HttpPut("{id:int}")]
        public async Task<ActionResult<ApiResponse<FacultyScopeResponseDTO>>> Update(
            int id,
            [FromBody] UpdateFacultyScopeRequestDTO request,
            CancellationToken ct = default)
            => (await _service.UpdateAsync(id, request, ct)).ToActionResult();

        /// <summary>
        /// Las que no vengan se desactivan, y las nuevas se crean.
        /// </summary>
        [HttpPut("{id:int}/faculties")]
        public async Task<ActionResult<ApiResponse<FacultyScopeResponseDTO>>> SetFaculties(
            int id,
            [FromBody] SetFacultyScopeFacultiesRequestDTO request,
            CancellationToken ct = default)
            => (await _service.SetFacultiesAsync(id, request, ct)).ToActionResult();

        // ========================= Assignments (User ↔ Scope) =========================

        [HttpPost("{facultyScopeId:int}/assign")]
        public async Task<ActionResult<ApiResponse<bool>>> Assign(
            int facultyScopeId,
            [FromBody] AssignFacultyScopeUserRequestDTO request,
            CancellationToken ct)
        {
            return (await _service.AssignScopeToUserAsync(facultyScopeId, request, ct))
                .ToActionResult();
        }

        [HttpDelete("{id:int}/assign/{userId:int}")]
        public async Task<ActionResult<ApiResponse<bool>>> UnassignScopeFromUser(
            int id,
            int userId,
            CancellationToken ct = default)
            => (await _service.UnassignScopeFromUserAsync(id, userId, ct)).ToActionResult();

        // ========================= Resolver Allowed Faculties =========================

        /// <summary>
        /// Devuelve las FacultyId permitidas para un usuario (AppUser.IdUser),
        /// calculadas desde assignments activos + faculties activas.
        /// </summary>
        [HttpGet("users/{userId:int}/allowed-faculties")]
        public async Task<ActionResult<ApiResponse<IReadOnlyList<int>>>> GetAllowedFacultiesForUser(
            int userId,
            CancellationToken ct = default)
            => (await _service.GetAllowedFacultyIdsForUserAsync(userId, ct)).ToActionResult();

        /// <summary>
        /// Variante "yo mismo" (útil para probar rápido desde el front).
        /// OJO: este controller está restringido a superadmin por clase.
        /// </summary>
        [HttpGet("me/allowed-faculties")]
        public async Task<ActionResult<ApiResponse<IReadOnlyList<int>>>> GetMyAllowedFaculties(
            CancellationToken ct = default)
        {
            var userId = User.GetUserId();
            if (userId is null)
                return Unauthorized(ApiResponse<IReadOnlyList<int>>.Fail("User not authenticated."));

            return (await _service.GetAllowedFacultyIdsForUserAsync(userId.Value, ct)).ToActionResult();
        }
    }
}