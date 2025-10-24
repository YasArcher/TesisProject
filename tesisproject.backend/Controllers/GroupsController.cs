using Microsoft.AspNetCore.Mvc;
using tesisproject.backend.Controllers.Extensions;
using tesisproject.backend.Services.Interfaces;
using tesisproject.shared.DTOs.External;
using tesisproject.shared.DTOs.Group.Request;
using tesisproject.shared.DTOs.Group.Response;
using tesisproject.shared.Responses;

namespace tesisproject.backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class GroupsController : ControllerBase
    {
        private readonly IGroupService _service;
        public GroupsController(IGroupService service) => _service = service;

        // ================== Groups CRUD / Members ==================

        // POST: api/Groups
        [HttpPost]
        public async Task<ActionResult<ApiResponse<GroupResponseDTO>>> Create(
            [FromBody] AddGroupRequestDTO request,
            CancellationToken ct)
            => (await _service.CreateAsync(request, ct)).ToActionResult();

        // GET: api/Groups/{id}
        [HttpGet("{id:int}")]
        public async Task<ActionResult<ApiResponse<GroupResponseDTO>>> GetById(
            int id,
            CancellationToken ct)
            => (await _service.GetByIdAsync(id, ct)).ToActionResult();

        // GET: api/Groups/type/{type}
        [HttpGet("type/{type:int}")]
        public async Task<ActionResult<ApiResponse<IReadOnlyList<GroupResponseDTO>>>> List(
            int type,
            CancellationToken ct = default)
            => (await _service.ListAsync(type, ct)).ToActionResult();

        // PUT: api/Groups
        [HttpPut]
        public async Task<ActionResult<ApiResponse<GroupResponseDTO>>> Update(
            [FromBody] UpdateGroupRequestDTO request,
            CancellationToken ct)
            => (await _service.UpdateAsync(request, ct)).ToActionResult();

        // POST: api/Groups/members
        [HttpPost("members")]
        public async Task<ActionResult<ApiResponse<GroupMemberResponseDTO>>> AddMember(
            [FromBody] AddGroupMemberRequestDTO request,
            CancellationToken ct)
            => (await _service.AddMemberAsync(request, ct)).ToActionResult();

        // DELETE: api/Groups/{groupId}/members/{memberId}
        [HttpDelete("{groupId:int}/members/{memberId:int}")]
        public async Task<ActionResult<ApiResponse<NoContent>>> RemoveMember(
            int groupId,
            int memberId,
            CancellationToken ct)
            => (await _service.RemoveMemberAsync(groupId, memberId, ct)).ToActionResult();

        // GET: api/Groups/{groupId}/members   (usuarios externos del grupo)
        [HttpGet("{groupId:int}/members")]
        public async Task<ActionResult<ApiResponse<List<ExternalUserDTO>>>> GetExternalUsers(
            int groupId,
            CancellationToken ct)
            => (await _service.GetExternalUsersByGroupAsync(groupId, ct)).ToActionResult();

        // GET: api/Groups/by-project/{projectId}
        [HttpGet("by-project/{projectId:int}")]
        public async Task<ActionResult<ApiResponse<IReadOnlyList<GroupResponseDTO>>>> GetByProject(
            int projectId,
            CancellationToken ct)
            => (await _service.GetByProjectAsync(projectId, ct)).ToActionResult();

        // ================== External users (persona-centrado) ==================

        // GET: api/Groups/external-users     (todos desde el directorio)
        [HttpGet("external-users")]
        public async Task<ActionResult<ApiResponse<List<ExternalUserDTO>>>> GetAllExternalUsers(
            CancellationToken ct)
            => (await _service.GetAllExternalUsersAsync(ct)).ToActionResult();

        // GET: api/Groups/external-users/by-aspnet/{userId}
        [HttpGet("external-users/by-aspnet/{userId:int}")]
        public async Task<ActionResult<ApiResponse<ExternalUserDTO>>> GetExternalUserByAspNetId(
            int userId,
            CancellationToken ct)
            => (await _service.GetExternalUserByAspNetIdAsync(userId, ct)).ToActionResult();

        // GET: api/Groups/external-users/by-email
        // Ej: api/Groups/external-users/by-email?email=usuario@uta.edu.ec
        [HttpGet("external-users/by-email")]
        public async Task<ActionResult<ApiResponse<ExternalUserDTO>>> GetExternalUserByEmail(
            [FromQuery] string email,
            CancellationToken ct)
            => (await _service.GetExternalUserByEmailAsync(email, ct)).ToActionResult();
    }
}
