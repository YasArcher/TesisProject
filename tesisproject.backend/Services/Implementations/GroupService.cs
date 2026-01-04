using Microsoft.EntityFrameworkCore;
using tesisproject.backend.Services.Interfaces;
using tesisproject.backend.UnitOfWork.Interfaces;
using tesisproject.shared.Common.External;
using tesisproject.shared.DTOs.Auth;
using tesisproject.shared.DTOs.AppUser;
using tesisproject.shared.DTOs.Group.Request;
using tesisproject.shared.DTOs.Group.Response;
using tesisproject.shared.Entities.Core;
using tesisproject.shared.Entities.External;
using tesisproject.shared.Responses;

namespace tesisproject.backend.Services.Implementations
{
    public class GroupService : IGroupService
    {
        private readonly IUnitOfWork _uow;
        private readonly IExternalDirectoryClient _directory;
        private readonly ILogger<GroupService> _logger;
        private readonly IAppUserService _appUsers;

        public GroupService(
            IUnitOfWork uow,
            IExternalDirectoryClient directory,
            ILogger<GroupService> logger,
            IAppUserService appUsers)
        {
            _uow = uow;
            _directory = directory;
            _logger = logger;
            _appUsers = appUsers;
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

        public async Task<ServiceResult<List<ResolvedUserProfileDTO>>> GetExternalUsersByGroupAsync(int groupId, CancellationToken ct = default)
        {
            try
            {
                var exists = await _uow.Groups.ExistsAsync(g => g.GroupId == groupId, ct);
                if (!exists)
                    return ServiceResult<List<ResolvedUserProfileDTO>>.Fail("Group not found.", ErrorType.NotFound);

                // 1) Miembros del grupo (tabla puente)
                var members = await _uow.GroupMembers.GetMembersByGroupAsync(groupId, ct);
                if (members is null || members.Count == 0)
                    return ServiceResult<List<ResolvedUserProfileDTO>>.Fail("No group members found.", ErrorType.NotFound);

                // ⚠️ En tu caso: member.UserId = AppUser.IdUser
                var appUserIds = members.Select(m => m.UserId).Distinct().ToList();
                if (appUserIds.Count == 0)
                    return ServiceResult<List<ResolvedUserProfileDTO>>.Fail("No associated app users found.", ErrorType.Validation);

                // 2) Resolver IdLocal (IdentityUser.Id) usando IAppUserRepository (sin inventar métodos)
                // Mapa: AppUser.IdUser -> AppUser.IdLocal
                var localIdByAppUserId = new Dictionary<int, int>();

                foreach (var idUser in appUserIds)
                {
                    var appUser = await _uow.AppUsers.GetByIdUserAsync(idUser, ct);
                    if (appUser?.IdLocal is int idLocal && idLocal > 0)
                        localIdByAppUserId[idUser] = idLocal;
                }

                if (localIdByAppUserId.Count == 0)
                    return ServiceResult<List<ResolvedUserProfileDTO>>.Fail("No local identity ids found for group members.", ErrorType.Validation);

                var localIds = localIdByAppUserId.Values.Distinct().ToList();

                // 3) Emails institucionales (ahora sí por IdLocal)
                var emailByLocalId = await _uow.AspNetUsers.GetEmailsByUserIdsAsync(localIds, ct);

                var allEmails = emailByLocalId.Values
                    .Where(e => !string.IsNullOrWhiteSpace(e))
                    .Select(e => e!.Trim().ToLowerInvariant())
                    .Distinct()
                    .ToList();

                if (allEmails.Count == 0)
                    return ServiceResult<List<ResolvedUserProfileDTO>>.Fail("No valid emails found for users.", ErrorType.Validation);

                // 4) Rehidratar roles si faltan (evitar N+1) - queda igual
                if (members.Any(m => m.MemberRole == null && m.MemberRoleId != 0))
                {
                    var roleIds = members.Where(m => m.MemberRoleId != 0)
                        .Select(m => m.MemberRoleId)
                        .Distinct()
                        .ToList();

                    var rolesById = await _uow.MemberRoleTypeRepository.GetByIdsAsync(
                        roleIds,
                        include: null,
                        ct: ct
                    );

                    foreach (var m in members)
                        if (m.MemberRole == null && m.MemberRoleId != 0 &&
                            rolesById.TryGetValue(m.MemberRoleId, out var role))
                            m.MemberRole = role;
                }

                // 5) Directorio externo en batch
                var dirRes = await _directory.GetByEmailsAsync(allEmails, ct);
                if (!dirRes.Success || dirRes.Data is null || dirRes.Data.Count == 0)
                    return ServiceResult<List<ResolvedUserProfileDTO>>.Fail("No external users found.");

                var byEmail = dirRes.Data
                    .Where(p => !string.IsNullOrWhiteSpace(p.Email))
                    .ToDictionary(p => p.Email.Trim().ToLowerInvariant());

                // 6) Merge (local + externo) → ExternalUserDTO
                var result = new List<ResolvedUserProfileDTO>();

                foreach (var member in members)
                {
                    // member.UserId = AppUser.IdUser -> resolver localId
                    if (!localIdByAppUserId.TryGetValue(member.UserId, out var localId))
                        continue;

                    if (!emailByLocalId.TryGetValue(localId, out var em) || string.IsNullOrWhiteSpace(em))
                        continue;

                    var email = em.Trim().ToLowerInvariant();

                    if (!byEmail.TryGetValue(email, out var profile))
                        continue;

                    result.Add(ToExternalUserDTO(profile,
                        role: member.MemberRole?.Name,
                        groupId: member.GroupId,
                        memberId: member.GroupMemberId,
                        memberRoleId: member.MemberRoleId));
                }

                if (result.Count == 0)
                    return ServiceResult<List<ResolvedUserProfileDTO>>.Fail("No external users matched the group members.", ErrorType.NotFound);

                return ServiceResult<List<ResolvedUserProfileDTO>>.Ok(result, "External users by group retrieved");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error retrieving external users for group {GroupId}", groupId);
                return ServiceResult<List<ResolvedUserProfileDTO>>.Fail("Unexpected error.", ErrorType.Unexpected);
            }
        }


        public async Task<ServiceResult<ResolvedUserProfileDTO>> GetExternalUserByAspNetIdAsync(int userId, CancellationToken ct = default)
        {
            try
            {
                var local = await _uow.AspNetUsers.GetEmailByUserIdAsync(userId, ct);
                if (local is null)
                    return ServiceResult<ResolvedUserProfileDTO>.Fail("ASP.NET user not found.", ErrorType.NotFound);

                var email = (local.Value.Email ?? string.Empty).Trim().ToLowerInvariant();
                if (string.IsNullOrWhiteSpace(email))
                    return ServiceResult<ResolvedUserProfileDTO>.Fail("User has no institutional email.", ErrorType.Validation);

                var dirRes = await _directory.GetByEmailsAsync(new[] { email }, ct);
                var profile = dirRes.Data?.FirstOrDefault();
                if (profile is null)
                    return ServiceResult<ResolvedUserProfileDTO>.Fail("External user not found for the given email.", ErrorType.NotFound);

                var dto = ToExternalUserDTO(profile, role: null, groupId: 0, memberId: 0, memberRoleId: 0);
                dto.UserId = local.Value.Id;

                return ServiceResult<ResolvedUserProfileDTO>.Ok(dto, "External user resolved by ASP.NET user id");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error resolving external user for ASP.NET user {UserId}", userId);
                return ServiceResult<ResolvedUserProfileDTO>.Fail("Unexpected error.", ErrorType.Unexpected);
            }
        }

        public async Task<ServiceResult<ResolvedUserProfileDTO>> GetExternalUserByEmailAsync(string institutionalEmail, CancellationToken ct = default)
        {
            var email = (institutionalEmail ?? string.Empty).Trim().ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(email))
                return ServiceResult<ResolvedUserProfileDTO>.Fail("Email is required.", ErrorType.Validation);

            try
            {
                var dirRes = await _directory.GetByEmailsAsync(new[] { email }, ct);
                var profile = dirRes.Data?.FirstOrDefault();
                if (profile is null)
                    return ServiceResult<ResolvedUserProfileDTO>.Fail("External user not found.", ErrorType.NotFound);

                var dto = ToExternalUserDTO(profile, role: null, groupId: 0, memberId: 0, memberRoleId: 0);
                return ServiceResult<ResolvedUserProfileDTO>.Ok(dto, "External user retrieved by email");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error retrieving external user by email {Email}", email);
                return ServiceResult<ResolvedUserProfileDTO>.Fail("Unexpected error.", ErrorType.Unexpected);
            }
        }

        public async Task<ServiceResult<List<ResolvedUserProfileDTO>>> GetAllExternalUsersAsync(CancellationToken ct = default)
        {
            try
            {
                // 1) Llamar al directorio externo para traer TODOS los perfiles
                var dirRes = await _directory.GetAllAsync(ct);

                if (!dirRes.Success || dirRes.Data is null || dirRes.Data.Count == 0)
                    return ServiceResult<List<ResolvedUserProfileDTO>>.Fail(
                        "No external users found.",
                        ErrorType.NotFound
                    );

                // 2) Mapear a tu DTO de dominio
                var list = dirRes.Data
                    .Select(p => ToExternalUserDTO(
                        p,
                        role: null,     // sin contexto de grupo aquí
                        groupId: 0,
                        memberId: 0,
                        memberRoleId: 0
                    ))
                    .ToList();

                return ServiceResult<List<ResolvedUserProfileDTO>>.Ok(list, "External users retrieved");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error retrieving all external users");
                return ServiceResult<List<ResolvedUserProfileDTO>>.Fail("Unexpected error.", ErrorType.Unexpected);
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

        public async Task<ServiceResult<GroupMemberResponseDTO>> AddMemberAsync(
            AddGroupMemberRequestDTO request,
            CancellationToken ct = default)
        {
            try
            {
                // 1) Validar grupo
                var group = await _uow.Groups.GetByIdAsync(request.GroupId, includeMembers: false, ct);
                if (group is null)
                    return ServiceResult<GroupMemberResponseDTO>.Fail("Group not found.", ErrorType.NotFound);

                if (request.MemberRole == 0)
                    return ServiceResult<GroupMemberResponseDTO>.Fail("Member role is required.", ErrorType.Validation);

                var email = (request.Email ?? string.Empty).Trim().ToLowerInvariant();
                if (string.IsNullOrWhiteSpace(email))
                    return ServiceResult<GroupMemberResponseDTO>.Fail("Institutional email is required.", ErrorType.Validation);

                var document = (request.Document ?? string.Empty).Trim();
                if (string.IsNullOrWhiteSpace(document))
                    return ServiceResult<GroupMemberResponseDTO>.Fail("Document is required.", ErrorType.Validation);

                // 2) Construir RegisterRequest directo desde el DTO
                var registerDto = new RegisterRequest
                {
                    Email = email,
                    Username = document,
                    Password = "Temporal#123",
                    AspUserId = request.AspUserId
                };

                // 3) Asegurar AppUser (Identity + AppUser)
                var ensureResult = await _appUsers.EnsureAppUserAsync(registerDto, ct);
                if (!ensureResult.Success)
                {
                    return ServiceResult<GroupMemberResponseDTO>.Fail(
                        ensureResult.Message ?? "Failed to ensure app user.",
                        ensureResult.Error);
                }

                var appUserId = ensureResult.Data; // IdUser (PK de APP_USER)

                // 4) Resolver IdLocal (IdentityUser.Id) para GroupMember.UserId
                var appUser = await _uow.AppUsers.GetByIdAsync(new object[] { appUserId }, ct);
                if (appUser is null)
                    return ServiceResult<GroupMemberResponseDTO>.Fail("Unable to resolve ASP.NET user from app user.", ErrorType.Unexpected);

                var aspNetUserId = appUser.IdUser;

                // 5) Validar duplicado
                var duplicated = await _uow.GroupMembers.ExistsAsync(request.GroupId, aspNetUserId, ct);
                if (duplicated)
                    return ServiceResult<GroupMemberResponseDTO>.Fail("This user is already a member of the group.", ErrorType.Conflict);

                // 6) Crear GroupMember
                var member = new GroupMember
                {
                    GroupId = request.GroupId,
                    UserId = aspNetUserId,
                    MemberRoleId = request.MemberRole,
                    JoinedAt = DateTime.UtcNow
                };

                await _uow.GroupMembers.AddAsync(member, ct);
                await _uow.SaveChangesAsync(ct);

                // 7) Resolver nombre del rol (igual que antes)
                string roleName = string.Empty;
                if (member.MemberRoleId != 0)
                {
                    var rolesById = await _uow.MemberRoleTypeRepository.GetByIdsAsync(
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
                return ServiceResult<GroupMemberResponseDTO>.Fail(
                    dbex.InnerException?.Message ?? dbex.Message,
                    ErrorType.Conflict);
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

        private static ResolvedUserProfileDTO ToExternalUserDTO(
            ExternalUserProfileModel p,
            string? role,
            int groupId,
            int memberId,
            int memberRoleId)
        {
            var chosen = ExternalCareerSelector.SelectBestCareer(
                profile: p,
                preferredTeacherFacultyCareerId: null, // si en algún flujo lo tienes, pásalo
                preferredFacultyCareerId: null,        // idem
                preferredFacultyId: null,              // idem
                onlyActivePreferred: true);

            return new ResolvedUserProfileDTO
            {
                UserId = p.ExternalId,
                GroupId = groupId,
                UserGroupId = memberId,
                FullName = p.FullName,
                Document = p.Document,
                Phone = p.Phone,
                Email = p.Email,
                Position = p.Position,
                FacultyCareerId = chosen?.FacultyCareerId,
                Role = role,
                AspNetUserId = p.AspId,
                MemberRoleId = memberRoleId,
            };
        }

    }
}
