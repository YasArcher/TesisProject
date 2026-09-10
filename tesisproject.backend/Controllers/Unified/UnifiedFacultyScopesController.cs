using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using tesisproject.backend.Controllers.Extensions;
using tesisproject.backend.Services.Interfaces;
using tesisproject.backend.Services.Unified.Interfaces;
using tesisproject.shared.DTOs.FacultyScope.Request;
using tesisproject.shared.DTOs.FacultyScope.Response;
using tesisproject.shared.Responses;

namespace tesisproject.backend.Controllers.Unified
{
    [Authorize(Roles = "superadmin")]
    [ApiController]
    [Route("api/facultyscopes")]
    [Produces("application/json")]
    public class UnifiedFacultyScopesController : ControllerBase
    {
        private readonly IUnifiedFacultyScopeService _service;
        private readonly ICurrentUserService _currentUser;

        public UnifiedFacultyScopesController(
            IUnifiedFacultyScopeService service,
            ICurrentUserService currentUser)
        {
            _service = service;
            _currentUser = currentUser;
        }

        // ========================= Scopes (CRUD) =========================

        [HttpGet]
        public async Task<ActionResult<ServiceResult<IReadOnlyList<FacultyScopeResponseDTO>>>> GetAll(
            [FromQuery] bool includeAssignments = false,
            CancellationToken ct = default)
            => (await _service.GetAllAsync(includeAssignments, ct)).ToActionResult();

        [HttpGet("{id:int}")]
        public async Task<ActionResult<ServiceResult<FacultyScopeResponseDTO>>> GetById(
            int id,
            [FromQuery] bool includeAssignments = false,
            CancellationToken ct = default)
            => (await _service.GetByIdAsync(id, includeAssignments, ct)).ToActionResult();

        [HttpPost]
        public async Task<ActionResult<ServiceResult<FacultyScopeResponseDTO>>> Create(
            [FromBody] CreateFacultyScopeRequestDTO request,
            CancellationToken ct = default)
            => (await _service.CreateAsync(new UnifiedCreateFacultyScopeRequestDTO(request.Name, request.FacultyIds), ct)).ForHttpField("externalFacultyIds", nameof(request.FacultyIds)).ToActionResult();

        [HttpPut("{id:int}")]
        public async Task<ActionResult<ServiceResult<FacultyScopeResponseDTO>>> Update(
            int id,
            [FromBody] UpdateFacultyScopeRequestDTO request,
            CancellationToken ct = default)
            => (await _service.UpdateAsync(id, request, ct)).ToActionResult();

        /// <summary>
        /// Las que no vengan se desactivan, y las nuevas se crean.
        /// </summary>
        [HttpPut("{id:int}/faculties")]
        public async Task<ActionResult<ServiceResult<FacultyScopeResponseDTO>>> SetFaculties(
            int id,
            [FromBody] SetFacultyScopeFacultiesRequestDTO request,
            CancellationToken ct = default)
            => (await _service.SetFacultiesAsync(id, new UnifiedSetFacultyScopeFacultiesRequestDTO(request.FacultyIds), ct)).ForHttpField("externalFacultyIds", nameof(request.FacultyIds)).ToActionResult();

        // ========================= Assignments (User ↔ Scope) =========================

        [HttpPost("{facultyScopeId:int}/assign")]
        public async Task<ActionResult<ServiceResult<bool>>> Assign(
            int facultyScopeId,
            [FromBody] AssignFacultyScopeUserRequestDTO request,
            CancellationToken ct)
            => (await _service.AssignScopeToUserAsync(facultyScopeId, request, ct))
                .ToActionResult();

        [HttpDelete("{id:int}/assign/{userId:int}")]
        public async Task<ActionResult<ServiceResult<bool>>> UnassignScopeFromUser(
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
        public async Task<ActionResult<ServiceResult<IReadOnlyList<int>>>> GetAllowedFacultiesForUser(
            int userId,
            CancellationToken ct = default)
            => (await _service.GetAllowedFacultyIdsForUserAsync(ct)).ToActionResult();

        /// <summary>
        /// Variante "yo mismo" (útil para probar rápido desde el front).
        /// OJO: este controller está restringido a superadmin por clase.
        /// </summary>
        [HttpGet("me/allowed-faculties")]
        public async Task<ActionResult<ServiceResult<IReadOnlyList<int>>>> GetMyAllowedFaculties(
            CancellationToken ct = default)
            => (await _service.GetAllowedFacultyIdsForUserAsync(ct)).ToActionResult();
    }
}