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

        // GET: api/Groups?search=&skip=0&take=20
        [HttpGet]
        public async Task<ActionResult<ApiResponse<IReadOnlyList<GroupResponseDTO>>>> List(
            CancellationToken ct = default)
            => (await _service.ListAsync(ct)).ToActionResult();
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

        // GET: api/Groups/by-project/123
        [HttpGet("by-project/{projectId:int}")]
        public async Task<ActionResult<ApiResponse<IReadOnlyList<GroupResponseDTO>>>> GetByProject(
            int projectId,
            CancellationToken ct)
            => (await _service.GetByProjectAsync(projectId, ct)).ToActionResult();

    }
}
