using Microsoft.EntityFrameworkCore;
using tesisproject.backend.Services.Interfaces;
using tesisproject.backend.UnitOfWork.Interfaces;
using tesisproject.shared.DTOs.External;
using tesisproject.shared.DTOs.Group.Request;
using tesisproject.shared.DTOs.Group.Response;
using tesisproject.shared.Entities.Core;
using tesisproject.shared.Responses;

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

        // ================= READS =================

        public async Task<ServiceResult<GroupResponseDTO>> GetByIdAsync(int id, CancellationToken ct = default)
        {
            try
            {
                var group = await _uow.Groups.GetByIdAsync(id, includeMembers: false, ct);
                if (group is null)
                    return ServiceResult<GroupResponseDTO>.Fail("Group not found.", ErrorType.NotFound);

                var dto = new GroupResponseDTO
                {
                    GroupId = group.GroupId,
                    GroupTypeId = group.GroupTypeId,
                    Name = group.Name
                };

                return ServiceResult<GroupResponseDTO>.Ok(dto, "Group retrieved");
            }
            catch (Exception ex)
            {
                return ServiceResult<GroupResponseDTO>.Fail(ex.Message, ErrorType.Unexpected);
            }
        }

        //Actualizar grupo con DTO

        public async Task<ServiceResult<GroupResponseDTO>> UpdateAsync(UpdateGroupRequestDTO request, CancellationToken ct = default)
        {
            try
            {
                var group = await _uow.Groups.GetByIdAsync(request.GroupId, includeMembers: false, ct);
                if (group is null)
                    return ServiceResult<GroupResponseDTO>.Fail("Group not found.", ErrorType.NotFound);
                var name = (request.Name ?? string.Empty).Trim();
                if (string.IsNullOrWhiteSpace(name))
                    return ServiceResult<GroupResponseDTO>.Fail("Group name is required.", ErrorType.Validation);
                //var exists = await _uow.Groups.NameExistsAsync(name, id, ct);
                //if (exists)
                //    return ServiceResult<GroupResponseDTO>.Fail("A group with the same name already exists.", ErrorType.Conflict);
                group.Name = name;
                group.GroupTypeId = request.GroupTypeId;
                _uow.Groups.Update(group);
                await _uow.SaveChangesAsync(ct);
                var dto = new GroupResponseDTO
                {
                    GroupId = group.GroupId,
                    GroupTypeId = group.GroupTypeId,
                    Name = group.Name
                };
                return ServiceResult<GroupResponseDTO>.Ok(dto, "Group updated");
            }
            catch (DbUpdateException dbex)
            {
                return ServiceResult<GroupResponseDTO>.Fail(dbex.InnerException?.Message ?? dbex.Message, ErrorType.Conflict);
            }
            catch (Exception ex)
            {
                return ServiceResult<GroupResponseDTO>.Fail(ex.Message, ErrorType.Unexpected);
            }
        }

        public async Task<ServiceResult<IReadOnlyList<GroupResponseDTO>>> ListAsync(CancellationToken ct = default)
        {
            try
            {

                var q = _uow.Groups.Query();
                var items = await q
                    .OrderBy(g => g.Name)
                    .Select(g => new GroupResponseDTO
                    {
                        GroupId = g.GroupId,
                        GroupTypeId = g.GroupTypeId,
                        Name = g.Name
                    })
                    .ToListAsync(ct);

                if (items.Count == 0)
                    return ServiceResult<IReadOnlyList<GroupResponseDTO>>.Fail("No groups found.", ErrorType.NotFound);

                return ServiceResult<IReadOnlyList<GroupResponseDTO>>.Ok(items, "Groups retrieved");
            }
            catch (Exception ex)
            {
                return ServiceResult<IReadOnlyList<GroupResponseDTO>>.Fail(ex.Message, ErrorType.Unexpected);
            }
        }

        // ================= WRITES =================

        public async Task<ServiceResult<GroupResponseDTO>> CreateAsync(AddGroupRequestDTO request, CancellationToken ct = default)
        {
            try
            {
                var name = (request.Name ?? string.Empty).Trim();
                if (string.IsNullOrWhiteSpace(name))
                    return ServiceResult<GroupResponseDTO>.Fail("Group name is required.", ErrorType.Validation);

                var exists = await _uow.Groups.NameExistsAsync(name, ct);
                if (exists)
                    return ServiceResult<GroupResponseDTO>.Fail("A group with the same name already exists.", ErrorType.Conflict);

                var entity = new Group
                {
                    GroupTypeId = request.GroupTypeId,
                    Name = name
                };

                await _uow.Groups.AddAsync(entity, ct);
                await _uow.SaveChangesAsync(ct);

                var dto = new GroupResponseDTO
                {
                    GroupId = entity.GroupId,
                    GroupTypeId = entity.GroupTypeId,
                    Name = entity.Name
                };

                return ServiceResult<GroupResponseDTO>.Ok(dto, "Group created");
            }
            catch (DbUpdateException dbex)
            {
                return ServiceResult<GroupResponseDTO>.Fail(dbex.InnerException?.Message ?? dbex.Message, ErrorType.Conflict);
            }
            catch (Exception ex)
            {
                return ServiceResult<GroupResponseDTO>.Fail(ex.Message, ErrorType.Unexpected);
            }
        }

        public async Task<ServiceResult<GroupMemberResponseDTO>> AddMemberAsync(AddGroupMemberRequestDTO request, CancellationToken ct = default)
        {
            try
            {

                var group = await _uow.Groups.GetByIdAsync(request.GroupId, includeMembers: false, ct);
                if (group is null)
                    return ServiceResult<GroupMemberResponseDTO>.Fail("Group not found.", ErrorType.NotFound);

                // External user must exist in external API (tablas externas según tu diseño)
                var externalUser = await _external.GetByIdAsync(request.ExternalUserId, ct);
                if (externalUser.Data is null)
                    return ServiceResult<GroupMemberResponseDTO>.Fail("External user not found.", ErrorType.Validation);

                // Avoid duplicates
                var duplicated = await _uow.GroupMembers.ExistsAsync(request.GroupId, request.ExternalUserId, ct);
                if (duplicated)
                    return ServiceResult<GroupMemberResponseDTO>.Fail("This user is already a member of the group.", ErrorType.Conflict);

                // Role required
                var role = (request.MemberRole ?? string.Empty).Trim();
                if (string.IsNullOrWhiteSpace(role))
                    return ServiceResult<GroupMemberResponseDTO>.Fail("Member role is required.", ErrorType.Validation);

                var member = new GroupMember
                {
                    GroupId = request.GroupId,
                    UserId = request.ExternalUserId,
                    MemberRole = role,
                    JoinedAt = DateTime.UtcNow
                };

                await _uow.GroupMembers.AddAsync(member, ct);
                await _uow.SaveChangesAsync(ct);

                var dto = new GroupMemberResponseDTO
                {
                    GroupMemberId = member.GroupMemberId,
                    ExternalUserId = member.UserId,
                    MemberRole = member.MemberRole,
                    JoinedAt = member.JoinedAt
                };

                return ServiceResult<GroupMemberResponseDTO>.Ok(dto, "Member added to group");
            }
            catch (DbUpdateException dbex)
            {
                return ServiceResult<GroupMemberResponseDTO>.Fail(dbex.InnerException?.Message ?? dbex.Message, ErrorType.Conflict);
            }
            catch (Exception ex)
            {
                return ServiceResult<GroupMemberResponseDTO>.Fail(ex.Message, ErrorType.Unexpected);
            }
        }

        public async Task<ServiceResult<NoContent>> RemoveMemberAsync(int groupId, int memberId, CancellationToken ct = default)
        {
            try
            {
                var member = await _uow.GroupMembers.GetByIdAsync(memberId, ct);
                if (member is null || member.GroupId != groupId)
                    return ServiceResult<NoContent>.Fail("Group member not found.", ErrorType.NotFound);

                _uow.GroupMembers.Remove(member);
                await _uow.SaveChangesAsync(ct);

                return ServiceResult<NoContent>.Ok(new NoContent(), "Member removed");
            }
            catch (DbUpdateException dbex)
            {
                return ServiceResult<NoContent>.Fail(dbex.InnerException?.Message ?? dbex.Message, ErrorType.Conflict);
            }
            catch (Exception ex)
            {
                return ServiceResult<NoContent>.Fail(ex.Message, ErrorType.Unexpected);
            }
        }

        public async Task<ServiceResult<List<ExternalUserDTO>>> GetExternalUsersByGroupAsync(int groupId, CancellationToken ct = default)
        {
            try
            {
                var exists = await _uow.Groups.ExistsAsync(g => g.GroupId == groupId, ct);
                if (!exists)
                    return ServiceResult<List<ExternalUserDTO>>.Fail("Group not found.", ErrorType.NotFound);

                // Consume API externa (recuerda: no crear tablas locales para usuario/facultad_carrera)
                var users = await _external.GetByGroupIdAsync(groupId, ct);

                if (users.Data is null || users.Data.Count == 0)
                    return ServiceResult<List<ExternalUserDTO>>.Fail("No external users found for this group.", ErrorType.NotFound);

                return ServiceResult<List<ExternalUserDTO>>.Ok(users.Data, "External users retrieved");
            }
            catch (Exception ex)
            {
                return ServiceResult<List<ExternalUserDTO>>.Fail(ex.Message, ErrorType.Unexpected);
            }
        }
        public async Task<ServiceResult<IReadOnlyList<GroupResponseDTO>>> GetByProjectAsync(int projectId, CancellationToken ct = default)
        {
            try
            {
                var groups = await _uow.Groups.GetByProjectIdAsync(projectId, ct);

                if (groups.Count == 0)
                    return ServiceResult<IReadOnlyList<GroupResponseDTO>>.Fail("Project not found or it has no associated groups.", ErrorType.NotFound);

                var dtos = groups.Select(g => new GroupResponseDTO
                {
                    GroupId = g.GroupId,
                    GroupTypeId = g.GroupTypeId,
                    Name = g.Name
                }).ToList();

                return ServiceResult<IReadOnlyList<GroupResponseDTO>>.Ok(dtos, "Groups by project retrieved");
            }
            catch (Exception ex)
            {
                return ServiceResult<IReadOnlyList<GroupResponseDTO>>.Fail(ex.Message, ErrorType.Unexpected);
            }
        }

    }
}
