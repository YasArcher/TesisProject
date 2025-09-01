using Microsoft.EntityFrameworkCore;
using tesisproject.backend.Services.Interfaces;
using tesisproject.backend.UnitOfWork.Interfaces;
using tesisproject.shared.DTOs.Group;
using tesisproject.shared.Entities.Core;
using Microsoft.EntityFrameworkCore;

namespace tesisproject.backend.Services.Implementations
{
    public class GroupService : IGroupService
    {
        private readonly IUnitOfWork _uow;
        private readonly IExternalUsersService _external;

        public GroupService(IUnitOfWork uow, IExternalUsersService external)
        {
            _uow = uow;
            _external = external;
        }

        public async Task<GroupResponseDTO> CreateAsync(CreateGroupRequestDTO request, CancellationToken ct)
        {
            var name = request.Name.Trim();
            if (await _uow.Groups.NameExistsAsync(name, ct))
                throw new ArgumentException("A group with the same name already exists.");

            var entity = new Group
            {
                GroupId = Guid.NewGuid(),
                GroupTypeId = request.GroupTypeId,
                Name = name
            };

            await _uow.Groups.AddAsync(entity, ct);
            await _uow.SaveChangesAsync();

            return new GroupResponseDTO
            {
                GroupId = entity.GroupId,
                GroupTypeId = entity.GroupTypeId,
                Name = entity.Name
            };
        }

        public async Task<GroupResponseDTO?> GetByIdAsync(Guid id, CancellationToken ct)
        {
            var group = await _uow.Groups.GetByIdAsync(id, includeMembers: true, ct);
            if (group is null) return null;

            return new GroupResponseDTO
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
        }

        public async Task<IEnumerable<GroupResponseDTO>> ListAsync(string? search, int skip, int take, CancellationToken ct)
        {
            var q = _uow.Groups.Query();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim();
                q = q.Where(g => EF.Functions.Like(g.Name, $"%{s}%"));
            }

            return await q.OrderBy(g => g.Name)
                          .Skip(skip)
                          .Take(take)
                          .Select(g => new GroupResponseDTO
                          {
                              GroupId = g.GroupId,
                              GroupTypeId = g.GroupTypeId,
                              Name = g.Name,
                              Members = new()
                          })
                          .ToListAsync(ct);
        }

        public async Task<GroupMemberResponseDTO> AddMemberAsync(Guid groupId, AddGroupMemberRequestDTO request, CancellationToken ct)
        {
            if (groupId != request.GroupId)
                throw new ArgumentException("Route id and payload GroupId must match.");

            var group = await _uow.Groups.GetByIdAsync(groupId, includeMembers: false, ct);
            if (group is null)
                throw new KeyNotFoundException("Group not found.");

            var existsUser = await _external.UserExistsAsync(request.ExternalUserId, ct);
            if (!existsUser)
                throw new ArgumentException("External user not found in external Users API.");

            var duplicated = await _uow.GroupMembers.ExistsAsync(request.GroupId, request.ExternalUserId, ct);
            if (duplicated)
                throw new ArgumentException("This user is already a member of the group.");

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

            return new GroupMemberResponseDTO
            {
                GroupMemberId = member.GroupMemberId,
                ExternalUserId = member.UserId,
                MemberRole = member.MemberRole,
                JoinedAt = member.JoinedAt
            };
        }

        public async Task<bool> RemoveMemberAsync(Guid groupId, Guid memberId, CancellationToken ct)
        {
            var member = await _uow.GroupMembers.GetByIdAsync(memberId, ct);
            if (member is null || member.GroupId != groupId) return false;

            await _uow.GroupMembers.RemoveAsync(member, ct);
            await _uow.SaveChangesAsync();
            return true;
        }
        async Task<IReadOnlyList<GroupResponseDTO>> IGroupService.ListAsync(
            string? search, int skip, int take, CancellationToken ct)
        {
            // saneo de paginación
            if (skip < 0) skip = 0;
            if (take <= 0) take = 20;
            const int MaxTake = 100;
            if (take > MaxTake) take = MaxTake;

            var q = _uow.Groups.Query(); // IQueryable<Group> AsNoTracking()

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim();
                q = q.Where(g => EF.Functions.Like(g.Name, $"%{s}%"));
            }

            var items = await q.OrderBy(g => g.Name)
                               .Skip(skip)
                               .Take(take)
                               .Select(g => new GroupResponseDTO
                               {
                                   GroupId = g.GroupId,
                                   GroupTypeId = g.GroupTypeId,
                                   Name = g.Name,
                                   // en listados no necesitamos miembros -> lista vacía
                                   Members = new List<GroupMemberResponseDTO>()
                               })
                               .ToListAsync(ct);

            // IReadOnlyList para exponer inmutabilidad
            return items;
        }
    }
}
