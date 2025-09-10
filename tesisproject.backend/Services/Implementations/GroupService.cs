using Microsoft.EntityFrameworkCore;
using tesisproject.backend.Repositories.Interfaces;
using tesisproject.backend.Services.Interfaces;
using tesisproject.backend.UnitOfWork.Interfaces;
using tesisproject.shared.DTOs.External;
using tesisproject.shared.DTOs.Group.Request;
using tesisproject.shared.DTOs.Group.Response;
using tesisproject.shared.Entities.Core;

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

        public async Task<GroupResponseDTO> CreateAsync(AddGroupRequestDTO request, CancellationToken ct)
        {
            var name = (request.Name ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Group name is required.");

            if (await _uow.Groups.NameExistsAsync(name, ct))
                throw new ArgumentException("A group with the same name already exists.");

            var entity = new Group
            {
                GroupTypeId = request.GroupTypeId,
                Name = name
            };

            await _uow.Groups.AddAsync(entity, ct);
            await _uow.SaveChangesAsync(ct);

            return new GroupResponseDTO
            {
                GroupId = entity.GroupId,
                GroupTypeId = entity.GroupTypeId,
                Name = entity.Name
            };
        }

        public async Task<GroupResponseDTO?> GetByIdAsync(int id, CancellationToken ct)
        {
            var group = await _uow.Groups.GetByIdAsync(id, includeMembers: true, ct);
            if (group is null) return null;

            return new GroupResponseDTO
            {
                GroupId = group.GroupId,
                GroupTypeId = group.GroupTypeId,
                Name = group.Name
            };
        }

        // ÚNICO ListAsync que coincide con la interfaz
        public async Task<IReadOnlyList<GroupResponseDTO>> ListAsync(string? search, int skip, int take, CancellationToken ct)
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
                                   Name = g.Name
                               })
                               .ToListAsync(ct);

            return items; // List<T> es IReadOnlyList<T> compatible en retorno
        }

        public async Task<GroupMemberResponseDTO> AddMemberAsync(int groupId, AddGroupMemberRequestDTO request, CancellationToken ct)
        {
            if (groupId != request.GroupId)
                throw new ArgumentException("Route id and payload GroupId must match.");

            var group = await _uow.Groups.GetByIdAsync(groupId, includeMembers: false, ct);
            if (group is null)
                throw new KeyNotFoundException("Group not found.");

            // Validar usuario externo
            var externalUser = await _external.GetByIdAsync(request.ExternalUserId, ct);
            if (externalUser is null)
                throw new ArgumentException("External user not found in external Users API.");

            // Evitar duplicados
            var duplicated = await _uow.GroupMembers.ExistsAsync(request.GroupId, request.ExternalUserId, ct);
            if (duplicated)
                throw new ArgumentException("This user is already a member of the group.");

            // Validar rol
            var role = (request.MemberRole ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(role))
                throw new ArgumentException("Member role is required.");

            var member = new GroupMember
            {
                GroupId = request.GroupId,
                UserId = request.ExternalUserId,
                MemberRole = role,
                JoinedAt =  DateTime.UtcNow
            };

            await _uow.GroupMembers.AddAsync(member, ct);
            await _uow.SaveChangesAsync(ct);

            return new GroupMemberResponseDTO
            {
                GroupMemberId = member.GroupMemberId,
                ExternalUserId = member.UserId,
                MemberRole = member.MemberRole,
                JoinedAt = member.JoinedAt
            };
        }

        public async Task<bool> RemoveMemberAsync(int groupId, int memberId, CancellationToken ct)
        {
            var member = await _uow.GroupMembers.GetByIdAsync(memberId, ct);
            if (member is null || member.GroupId != groupId)
                return false;
            _uow.GroupMembers.Remove(member);
            var rows = await _uow.SaveChangesAsync(ct);
            return rows > 0;
        }
        public async Task<List<ExternalUserDTO>> GetExternalUsersByGroupAsync(int groupId, CancellationToken ct)
        {
            // (opcional pero recomendado) valida que el grupo exista
            if (!await _uow.Groups.ExistsAsync(g => g.GroupId == groupId, ct))
                throw new KeyNotFoundException("Group not found.");


            // delega en tu servicio de integración
            return await _external.GetByGroupIdAsync(groupId, ct);
        }
    }
}
