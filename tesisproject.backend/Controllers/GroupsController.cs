using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using tesisproject.backend.Services.Interfaces;
using tesisproject.backend.UnitOfWork.Interfaces;
using tesisproject.shared.Entities.Core;
using tesisproject.shared.DTOs.Group;

namespace tesisproject.backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class GroupsController : ControllerBase
    {
        private readonly IUnitOfWork _uow;
        private readonly IExternalUsersService _externalUsers;

        public GroupsController(IUnitOfWork uow, IExternalUsersService externalUsers)
        {
            _uow = uow;
            _externalUsers = externalUsers;
        }

        [HttpPost]
        [ProducesResponseType(typeof(GroupResponseDTO), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Create([FromBody] CreateGroupRequestDTO request, CancellationToken ct)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);

            var name = request.Name.Trim();
            if (await _uow.Groups.NameExistsAsync(name, ct))
                return BadRequest(new { error = "A group with the same name already exists." });

            var entity = new Group
            {
                GroupId = Guid.NewGuid(),
                GroupTypeId = request.GroupTypeId,
                Name = name
            };

            await _uow.Groups.AddAsync(entity, ct);
            await _uow.SaveChangesAsync();

            var resp = new GroupResponseDTO
            {
                GroupId = entity.GroupId,
                GroupTypeId = entity.GroupTypeId,
                Name = entity.Name
            };

            return CreatedAtAction(nameof(GetById), new { id = entity.GroupId }, resp);
        }

        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(GroupResponseDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
        {
            var group = await _uow.Groups.GetByIdAsync(id, includeMembers: true, ct);
            if (group is null) return NotFound();

            var resp = new GroupResponseDTO
            {
                GroupId = group.GroupId,
                GroupTypeId = group.GroupTypeId,
                Name = group.Name,
                Members = group.Members.Select(m => new GroupMemberResponseDTO
                {
                    GroupMemberId = m.GroupMemberId,
                    ExternalUserId = m.UserId,
                    MemberRole = m.MemberRole,
                    JoinedAt = m.JoinedAt
                }).ToList()
            };

            return Ok(resp);
        }

        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<GroupResponseDTO>), StatusCodes.Status200OK)]
        public IActionResult List([FromQuery] string? search, [FromQuery] int skip = 0, [FromQuery] int take = 20)
        {
            var q = _uow.Groups.Query();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim();
                q = q.Where(g => EF.Functions.Like(g.Name, $"%{s}%"));
            }

            var items = q.OrderBy(g => g.Name)
                         .Skip(skip)
                         .Take(take)
                         .Select(g => new GroupResponseDTO
                         {
                             GroupId = g.GroupId,
                             GroupTypeId = g.GroupTypeId,
                             Name = g.Name,
                             Members = new()
                         })
                         .ToList();

            return Ok(items);
        }

        [HttpPost("{id:guid}/members")]
        [ProducesResponseType(typeof(GroupMemberResponseDTO), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> AddMember(Guid id, [FromBody] AddGroupMemberRequestDTO request, CancellationToken ct)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);
            if (id != request.GroupId)
                return BadRequest(new { error = "Route id and payload GroupId must match." });

            var group = await _uow.Groups.GetByIdAsync(id, includeMembers: false, ct);
            if (group is null) return NotFound(new { error = "Group not found." });

            // Valida usuario externo (ahora int):
            var exists = await _externalUsers.UserExistsAsync(request.ExternalUserId, ct);
            if (!exists) return BadRequest(new { error = "External user not found in external Users API." });

            // Evitar duplicado:
            if (await _uow.GroupMembers.ExistsAsync(request.GroupId, request.ExternalUserId, ct))
                return BadRequest(new { error = "This user is already a member of the group." });

            var member = new GroupMember
            {
                GroupMemberId = Guid.NewGuid(),
                GroupId = request.GroupId,
                UserId = request.ExternalUserId,
                MemberRole = request.MemberRole!.Trim(),
                JoinedAt = request.JoinedAt
            };

            await _uow.GroupMembers.AddAsync(member, ct);
            await _uow.SaveChangesAsync();

            var resp = new GroupMemberResponseDTO
            {
                GroupMemberId = member.GroupMemberId,
                ExternalUserId = member.UserId,
                MemberRole = member.MemberRole,
                JoinedAt = member.JoinedAt
            };

            return CreatedAtAction(nameof(GetById), new { id = request.GroupId }, resp);
        }


        [HttpDelete("{groupId:guid}/members/{memberId:guid}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> RemoveMember(Guid groupId, Guid memberId, CancellationToken ct)
        {
            var member = await _uow.GroupMembers.GetByIdAsync(memberId, ct);
            if (member is null || member.GroupId != groupId) return NotFound();

            await _uow.GroupMembers.RemoveAsync(member, ct);
            await _uow.SaveChangesAsync();
            return NoContent();
        }
    }
}
