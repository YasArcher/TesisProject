using Microsoft.AspNetCore.Mvc;
using tesisproject.backend.Services.Interfaces;
using tesisproject.shared.DTOs.Group;

namespace tesisproject.backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class GroupsController : ControllerBase
    {
        private readonly IGroupService _service;

        public GroupsController(IGroupService service)
            => _service = service;

        [HttpPost]
        [ProducesResponseType(typeof(GroupResponseDTO), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Create([FromBody] CreateGroupRequestDTO request, CancellationToken ct)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);
            try
            {
                var resp = await _service.CreateAsync(request, ct);
                return CreatedAtAction(nameof(GetById), new { id = resp.GroupId }, resp);
            }
            catch (ArgumentException ex) { return BadRequest(new { error = ex.Message }); }
        }

        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(GroupResponseDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
        {
            var resp = await _service.GetByIdAsync(id, ct);
            return resp is null ? NotFound() : Ok(resp);
        }

        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<GroupResponseDTO>), StatusCodes.Status200OK)]
        public async Task<IActionResult> List([FromQuery] string? search, [FromQuery] int skip = 0, [FromQuery] int take = 20, CancellationToken ct = default)
            => Ok(await _service.ListAsync(search, skip, take, ct));

        [HttpPost("{id:guid}/members")]
        [ProducesResponseType(typeof(GroupMemberResponseDTO), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> AddMember(Guid id, [FromBody] AddGroupMemberRequestDTO request, CancellationToken ct)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);
            try
            {
                var resp = await _service.AddMemberAsync(id, request, ct);
                return CreatedAtAction(nameof(GetById), new { id }, resp);
            }
            catch (KeyNotFoundException) { return NotFound(new { error = "Group not found." }); }
            catch (ArgumentException ex) { return BadRequest(new { error = ex.Message }); }
        }

        [HttpDelete("{groupId:guid}/members/{memberId:guid}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> RemoveMember(Guid groupId, Guid memberId, CancellationToken ct)
            => (await _service.RemoveMemberAsync(groupId, memberId, ct)) ? NoContent() : NotFound();
    }
}