using Microsoft.AspNetCore.Mvc;
using tesisproject.backend.Services.Interfaces;
using tesisproject.shared.DTOs.Group.Request;
using tesisproject.shared.DTOs.Group.Response;
using tesisproject.shared.DTOs.External; // <-- para ExternalUserDto

namespace tesisproject.backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class GroupsController : ControllerBase
    {
        private readonly IGroupService _service;

        public GroupsController(IGroupService service) => _service = service;

        [HttpPost]
        [ProducesResponseType(typeof(GroupResponseDTO), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Create([FromBody] AddGroupRequestDTO request, CancellationToken ct)
        {
            try
            {
                var resp = await _service.CreateAsync(request, ct);
                return CreatedAtAction(nameof(GetById), new { id = resp.GroupId }, resp);
            }
            catch (ArgumentException ex)
            {
                var isDuplicate = ex.Message.Contains("already exists", StringComparison.OrdinalIgnoreCase);
                return isDuplicate
                    ? Conflict(new { error = ex.Message })
                    : BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(GroupResponseDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(int id, CancellationToken ct)
        {
            var resp = await _service.GetByIdAsync(id, ct);
            return resp is null ? NotFound() : Ok(resp);
        }

        [HttpGet]
        [ProducesResponseType(typeof(IReadOnlyList<GroupResponseDTO>), StatusCodes.Status200OK)]
        public async Task<IActionResult> List(
            [FromQuery] string? search,
            [FromQuery] int skip = 0,
            [FromQuery] int take = 20,
            CancellationToken ct = default)
        {
            var items = await _service.ListAsync(search, skip, take, ct);
            return Ok(items);
        }

        [HttpPost("{id:int}/members")]
        [ProducesResponseType(typeof(GroupMemberResponseDTO), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> AddMember(int id, [FromBody] AddGroupMemberRequestDTO request, CancellationToken ct)
        {
            try
            {
                var resp = await _service.AddMemberAsync(id, request, ct);
                // si luego expones GET del miembro puntual, ajústalo aquí:
                return CreatedAtAction(nameof(GetById), new { id }, resp);
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { error = "Group not found." });
            }
            catch (ArgumentException ex)
            {
                var isDuplicate = ex.Message.Contains("already a member", StringComparison.OrdinalIgnoreCase);
                return isDuplicate
                    ? Conflict(new { error = ex.Message })
                    : BadRequest(new { error = ex.Message });
            }
        }

        [HttpDelete("{groupId:int}/members/{memberId:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> RemoveMember(int groupId, int memberId, CancellationToken ct)
        {
            var ok = await _service.RemoveMemberAsync(groupId, memberId, ct);
            return ok ? NoContent() : NotFound();
        }


        // Listar "external users" del grupo
        [HttpGet("{groupId:int}/members")]
        [ProducesResponseType(typeof(List<ExternalUserDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetExternalUsers(int groupId, CancellationToken ct)
        {
            try
            {
                // requiere IGroupService.GetExternalUsersByGroupAsync(Guid, CancellationToken)
                var users = await _service.GetExternalUsersByGroupAsync(groupId, ct);
                return Ok(users);
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { error = "Group not found." });
            }
        }
    }
}
