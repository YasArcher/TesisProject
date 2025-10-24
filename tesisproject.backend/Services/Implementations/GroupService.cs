using Microsoft.EntityFrameworkCore;
using tesisproject.backend.Services.Interfaces;
using tesisproject.backend.UnitOfWork.Interfaces;
using tesisproject.backend.Repositories.Interfaces;
using tesisproject.shared.DTOs.External;
using tesisproject.shared.DTOs.Group.Request;
using tesisproject.shared.DTOs.Group.Response;
using tesisproject.shared.Entities.Catalogs;
using tesisproject.shared.Entities.Core;
using tesisproject.shared.Entities.External;
using tesisproject.shared.Responses;

namespace tesisproject.backend.Services.Implementations
{
    public class GroupService : IGroupService
    {
        private readonly IUnitOfWork _uow;
        private readonly IAspNetUserRepository _aspUsers;
        private readonly ICatalogRepository<MemberRoleType> _roles;
        private readonly IExternalDirectoryClient _directory;
        private readonly ILogger<GroupService> _logger;

        public GroupService(
            IUnitOfWork uow,
            IAspNetUserRepository aspUsers,
            ICatalogRepository<MemberRoleType> roles,
            IExternalDirectoryClient directory,
            ILogger<GroupService> logger)
        {
            _uow = uow;
            _aspUsers = aspUsers;
            _roles = roles;
            _directory = directory;
            _logger = logger;
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

        public async Task<ServiceResult<IReadOnlyList<GroupResponseDTO>>> ListAsync(int type, CancellationToken ct = default)
        {
            try
            {
                var items = await _uow.Groups
                    .Query()
                    .Where(g => g.GroupTypeId == type)
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

        // ======= External users (unificado aquí) =======

        public async Task<ServiceResult<List<ExternalUserDTO>>> GetExternalUsersByGroupAsync(int groupId, CancellationToken ct = default)
        {
            try
            {
                var exists = await _uow.Groups.ExistsAsync(g => g.GroupId == groupId, ct);
                if (!exists)
                    return ServiceResult<List<ExternalUserDTO>>.Fail("Group not found.", ErrorType.NotFound);

                // 1) Miembros del grupo
                var members = await _uow.GroupMembers.GetMembersByGroupAsync(groupId, ct);
                if (members is null || members.Count == 0)
                    return ServiceResult<List<ExternalUserDTO>>.Fail("No group members found.", ErrorType.NotFound);

                var userIds = members.Select(m => m.UserId).Distinct().ToList();
                if (userIds.Count == 0)
                    return ServiceResult<List<ExternalUserDTO>>.Fail("No associated ASP.NET users found.", ErrorType.Validation);

                // 2) Emails institucionales (desde repo de ASP)
                var emailByUserId = await _aspUsers.GetEmailsByUserIdsAsync(userIds, ct);
                var allEmails = emailByUserId.Values
                    .Where(e => !string.IsNullOrWhiteSpace(e))
                    .Select(e => e!.Trim().ToLowerInvariant())
                    .Distinct()
                    .ToList();

                if (allEmails.Count == 0)
                    return ServiceResult<List<ExternalUserDTO>>.Fail("No valid emails found for users.", ErrorType.Validation);

                // 3) Rehidratar roles si faltan (evitar N+1)
                if (members.Any(m => m.MemberRole == null && m.MemberRoleId != 0))
                {
                    var roleIds = members.Where(m => m.MemberRoleId != 0)
                        .Select(m => m.MemberRoleId)
                        .Distinct()
                        .ToList();

                    var rolesById = await _roles.GetByIdsAsync(
                        roleIds,
                        include: null,
                        ct: ct
                    );

                    foreach (var m in members)
                        if (m.MemberRole == null && m.MemberRoleId != 0 &&
                            rolesById.TryGetValue(m.MemberRoleId, out var role))
                            m.MemberRole = role;
                }

                // 4) Directorio externo en batch
                var dirRes = await _directory.GetByEmailsAsync(allEmails, ct);
                if (!dirRes.Success || dirRes.Data is null || dirRes.Data.Count == 0)
                    return ServiceResult<List<ExternalUserDTO>>.Fail("No external users found.");

                var byEmail = dirRes.Data
                    .Where(p => !string.IsNullOrWhiteSpace(p.Email))
                    .ToDictionary(p => p.Email.Trim().ToLowerInvariant());

                // 5) Merge (local + externo) → ExternalUserDTO
                var result = new List<ExternalUserDTO>();
                foreach (var member in members)
                {
                    if (!emailByUserId.TryGetValue(member.UserId, out var em) || string.IsNullOrWhiteSpace(em))
                        continue;

                    var email = em!.Trim().ToLowerInvariant();

                    if (!byEmail.TryGetValue(email, out var profile))
                        continue;

                    result.Add(ToExternalUserDTO(profile,
                        role: member.MemberRole?.Name,
                        groupId: member.GroupId,
                        memberId: member.GroupMemberId));
                }

                if (result.Count == 0)
                    return ServiceResult<List<ExternalUserDTO>>.Fail("No external users matched the group members.", ErrorType.NotFound);

                return ServiceResult<List<ExternalUserDTO>>.Ok(result, "External users by group retrieved");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error retrieving external users for group {GroupId}", groupId);
                return ServiceResult<List<ExternalUserDTO>>.Fail("Unexpected error.", ErrorType.Unexpected);
            }
        }

        public async Task<ServiceResult<ExternalUserDTO>> GetExternalUserByAspNetIdAsync(int userId, CancellationToken ct = default)
        {
            try
            {
                var local = await _aspUsers.GetEmailByUserIdAsync(userId, ct);
                if (local is null)
                    return ServiceResult<ExternalUserDTO>.Fail("ASP.NET user not found.", ErrorType.NotFound);

                var email = (local.Value.Email ?? string.Empty).Trim().ToLowerInvariant();
                if (string.IsNullOrWhiteSpace(email))
                    return ServiceResult<ExternalUserDTO>.Fail("User has no institutional email.", ErrorType.Validation);

                var dirRes = await _directory.GetByEmailsAsync(new[] { email }, ct);
                var profile = dirRes.Data?.FirstOrDefault();
                if (profile is null)
                    return ServiceResult<ExternalUserDTO>.Fail("External user not found for the given email.", ErrorType.NotFound);

                var dto = ToExternalUserDTO(profile, role: null, groupId: 0, memberId: 0);
                dto.UserId = local.Value.Id;

                return ServiceResult<ExternalUserDTO>.Ok(dto, "External user resolved by ASP.NET user id");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error resolving external user for ASP.NET user {UserId}", userId);
                return ServiceResult<ExternalUserDTO>.Fail("Unexpected error.", ErrorType.Unexpected);
            }
        }

        public async Task<ServiceResult<ExternalUserDTO>> GetExternalUserByEmailAsync(string institutionalEmail, CancellationToken ct = default)
        {
            var email = (institutionalEmail ?? string.Empty).Trim().ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(email))
                return ServiceResult<ExternalUserDTO>.Fail("Email is required.", ErrorType.Validation);

            try
            {
                var dirRes = await _directory.GetByEmailsAsync(new[] { email }, ct);
                var profile = dirRes.Data?.FirstOrDefault();
                if (profile is null)
                    return ServiceResult<ExternalUserDTO>.Fail("External user not found.", ErrorType.NotFound);

                var dto = ToExternalUserDTO(profile, role: null, groupId: 0, memberId: 0);
                return ServiceResult<ExternalUserDTO>.Ok(dto, "External user retrieved by email");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error retrieving external user by email {Email}", email);
                return ServiceResult<ExternalUserDTO>.Fail("Unexpected error.", ErrorType.Unexpected);
            }
        }

        public async Task<ServiceResult<List<ExternalUserDTO>>> GetAllExternalUsersAsync(CancellationToken ct = default)
        {
            try
            {
                var documents = (await _aspUsers.GetAllUsernamesAsync(ct))
                    .Select(u => u?.Trim())
                    .Where(u => !string.IsNullOrWhiteSpace(u))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                if (documents.Count == 0)
                    return ServiceResult<List<ExternalUserDTO>>.Fail("No ASP.NET users with valid UserName found.", ErrorType.NotFound);

                // Batch para no saturar el directorio externo
                const int BATCH = 500;
                var aggregated = new List<ExternalProfileDTO>(documents.Count);
                for (int i = 0; i < documents.Count; i += BATCH)
                {
                    var slice = documents.Skip(i).Take(BATCH);

                    var dirRes = await _directory.GetByDocumentsAsync(slice, ct);
                    if (dirRes.Success && dirRes.Data is { Count: > 0 })
                        aggregated.AddRange(dirRes.Data);
                }

                if (aggregated.Count == 0)
                    return ServiceResult<List<ExternalUserDTO>>.Fail("No external users found for provided documents.", ErrorType.NotFound);

                var list = aggregated
                    .Select(p => ToExternalUserDTO(p, role: null, groupId: 0, memberId: 0))
                    .ToList();

                return ServiceResult<List<ExternalUserDTO>>.Ok(list, "External users retrieved from documents");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error retrieving all external users by documents");
                return ServiceResult<List<ExternalUserDTO>>.Fail("Unexpected error.", ErrorType.Unexpected);
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

                var entity = new Group { GroupTypeId = request.GroupTypeId, Name = name };
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

                var local = await _aspUsers.GetEmailByUserIdAsync(request.ExternalUserId, ct);
                if (local is null)
                    return ServiceResult<GroupMemberResponseDTO>.Fail("ASP.NET user not found.", ErrorType.Validation);

                var email = (local.Value.Email ?? string.Empty).Trim().ToLowerInvariant();
                if (string.IsNullOrWhiteSpace(email))
                    return ServiceResult<GroupMemberResponseDTO>.Fail("User has no institutional email.", ErrorType.Validation);

                var dirRes = await _directory.GetByEmailsAsync(new[] { email }, ct);
                if (dirRes.Data is null || dirRes.Data.Count == 0)
                    return ServiceResult<GroupMemberResponseDTO>.Fail("External user not found in directory.", ErrorType.Validation);

                var duplicated = await _uow.GroupMembers.ExistsAsync(request.GroupId, request.ExternalUserId, ct);
                if (duplicated)
                    return ServiceResult<GroupMemberResponseDTO>.Fail("This user is already a member of the group.", ErrorType.Conflict);

                if (request.MemberRole == 0)
                    return ServiceResult<GroupMemberResponseDTO>.Fail("Member role is required.", ErrorType.Validation);

                var member = new GroupMember
                {
                    GroupId = request.GroupId,
                    UserId = request.ExternalUserId,
                    MemberRoleId = request.MemberRole,
                    JoinedAt = DateTime.UtcNow
                };

                await _uow.GroupMembers.AddAsync(member, ct);
                await _uow.SaveChangesAsync(ct);

                string roleName = string.Empty;
                if (member.MemberRoleId != 0)
                {
                    var rolesById = await _roles.GetByIdsAsync(
                        new[] { (int)member.MemberRoleId },
                        include: null,
                        ct: ct
                    );
                    if (rolesById.TryGetValue((int)member.MemberRoleId, out var role))
                        roleName = role.Name;
                }

                var dto = new GroupMemberResponseDTO
                {
                    GroupMemberId = member.GroupMemberId,
                    ExternalUserId = member.UserId,
                    MemberRole = roleName,
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

        // ================= Helpers =================

        private static ExternalUserDTO ToExternalUserDTO(
            ExternalProfileDTO p,
            string? role,
            int groupId,
            int memberId)
        {
            return new ExternalUserDTO
            {
                UserId = p.ExternalId,
                GroupId = groupId,
                UserGroupId = memberId,
                FullName = p.FullName,
                Document = p.Document,
                Phone = p.Phone,
                Email = p.Email,
                Position = p.Position,
                FacultyCareerId = p.FacultyCareerId,
                Role = role
            };
        }
    }
}