using Microsoft.EntityFrameworkCore;
using System.Globalization;
using tesisproject.backend.Services.Interfaces;
using tesisproject.backend.UnitOfWork.Interfaces;
using tesisproject.shared.Common.External;
using tesisproject.shared.DTOs.AppUser;
using tesisproject.shared.DTOs.Auth;
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
        private readonly IExternalPeriodsClient _periods;
        private readonly IExternalDistributivosService _distributivos;


        public GroupService(
            IUnitOfWork uow,
            IExternalDirectoryClient directory,
            ILogger<GroupService> logger,
            IAppUserService appUsers,
            IExternalPeriodsClient periods,
            IExternalDistributivosService distributivos)
        {
            _uow = uow;
            _directory = directory;
            _logger = logger;
            _appUsers = appUsers;
            _periods = periods;
            _distributivos = distributivos;
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
                var members = await _uow.GroupMembers.GetMembersByGroupAsync(groupId, ct: ct);
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
                    .GroupBy(p => p.Email!.Trim().ToLowerInvariant())
                    .ToDictionary(g => g.Key, g => g.First());

                // 6) Merge (local + externo) → ExternalUserDTO
                var result = new List<ResolvedUserProfileDTO>();

                // Cargar UNA sola vez periodos + distributivos para TODOS los correos del grupo
                var extLoad = await LoadExternalPeriodsAndDistributivosAsync(allEmails, ct);
                if (!extLoad.Success || extLoad.Data is null)
                    return ServiceResult<List<ResolvedUserProfileDTO>>.Fail(
                        extLoad.Message ?? "Failed loading external data.",
                        extLoad.Error);

                var periods = extLoad.Data.Periods;
                var distributivos = extLoad.Data.Distributivos;

                foreach (var member in members)
                {
                    if (!localIdByAppUserId.TryGetValue(member.UserId, out var localId))
                        continue;

                    if (!emailByLocalId.TryGetValue(localId, out var em) || string.IsNullOrWhiteSpace(em))
                        continue;

                    var email = em.Trim().ToLowerInvariant();

                    if (!byEmail.TryGetValue(email, out var profile))
                        continue;

                    result.Add(await ToExternalUserDTOAsync(
                        profile,
                        role: member.MemberRole?.Name,
                        groupId: member.GroupId,
                        memberId: member.GroupMemberId,
                        memberRoleId: member.MemberRoleId,
                        periods: periods,
                        distributivos: distributivos,
                        ct: ct));
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

                var extLoad = await LoadExternalPeriodsAndDistributivosAsync(new[] { email }, ct);
                if (!extLoad.Success || extLoad.Data is null)
                    return ServiceResult<ResolvedUserProfileDTO>.Fail(
                        extLoad.Message ?? "Failed loading external periods/distributivos.",
                        extLoad.Error
                    );

                var dto = await ToExternalUserDTOAsync(
                    profile,
                    role: null,
                    groupId: 0,
                    memberId: 0,
                    memberRoleId: 0,
                    periods: extLoad.Data.Periods,
                    distributivos: extLoad.Data.Distributivos,
                    ct: ct
                );

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

                var extLoad = await LoadExternalPeriodsAndDistributivosAsync(new[] { email }, ct);
                if (!extLoad.Success || extLoad.Data is null)
                    return ServiceResult<ResolvedUserProfileDTO>.Fail(
                        extLoad.Message ?? "Failed loading external periods/distributivos.",
                        extLoad.Error
                    );

                var dto = await ToExternalUserDTOAsync(
                    profile,
                    role: null,
                    groupId: 0,
                    memberId: 0,
                    memberRoleId: 0,
                    periods: extLoad.Data.Periods,
                    distributivos: extLoad.Data.Distributivos,
                    ct: ct
                );

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

                // 1) Correos de todos los perfiles (normalizados y únicos)
                var emailsDistinct = (dirRes.Data ?? new List<ExternalUserProfileModel>())
                    .Where(x => !string.IsNullOrWhiteSpace(x.Email))
                    .Select(x => x.Email!.Trim().ToLowerInvariant())
                    .Distinct()
                    .ToList();

                // 2) Cargar UNA sola vez periodos + distributivos para esos correos
                var extLoad = await LoadExternalPeriodsAndDistributivosAsync(emailsDistinct, ct);
                if (!extLoad.Success || extLoad.Data is null)
                    return ServiceResult<List<ResolvedUserProfileDTO>>.Fail(
                        extLoad.Message ?? "Failed loading external periods/distributivos.",
                        extLoad.Error
                    );

                // 3) Mapear en paralelo usando lo ya cargado
                var list = (await Task.WhenAll(
                        (dirRes.Data ?? new List<ExternalUserProfileModel>())
                            .Select(p => ToExternalUserDTOAsync(
                                p,
                                role: null,
                                groupId: 0,
                                memberId: 0,
                                memberRoleId: 0,
                                periods: extLoad.Data.Periods,
                                distributivos: extLoad.Data.Distributivos,
                                ct: ct))
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

                // 5.1) Regla: si agrega Coordinador Principal (1), actualizar facultad del proyecto
                if (request.MemberRole == 1)
                {
                    // (A) Resolver FacultyId (prioridad: FacultyId directo)
                    int? facultyId = request.FacultyId;

                    // Si solo te mandan FacultyCareerId, aquí deberías traducir a FacultyId.
                    // Si aún no tienes esa tabla/repositorio, NO inventes: obliga FacultyId.
                    if (facultyId is null || facultyId <= 0)
                        return ServiceResult<GroupMemberResponseDTO>.Fail(
                            "FacultyId is required when adding a Coordinador Principal.",
                            ErrorType.Validation);

                    // (B) Traer el proyecto asociado a este grupo (1 grupo -> 1 proyecto)
                    var project = await _uow.Projects
                        .Query(asNoTracking: false)
                        .FirstOrDefaultAsync(p => p.ProjectGroupId == request.GroupId, ct);

                    if (project is null)
                        return ServiceResult<GroupMemberResponseDTO>.Fail(
                            "No project is associated to this group.",
                            ErrorType.NotFound);

                    // (C) Actualizar facultad del proyecto
                    project.FacultyId = facultyId.Value;
                    _uow.Projects.Update(project);

                    // (D) Recomendado: cerrar cualquier Coordinador Principal activo previo (histórico)
                    var actives = await _uow.GroupMembers
                        .QueryByGroup(request.GroupId, asNoTracking: false)
                        .Where(m => m.LeftAt == null && m.MemberRoleId == 1)
                        .ToListAsync(ct);

                    foreach (var m in actives)
                    {
                        m.LeftAt = DateTime.UtcNow;
                        _uow.GroupMembers.Update(m);
                    }
                }

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

        public async Task<ServiceResult<NoContent>> RemoveMemberAsync(
            int groupId,
            int memberId,
            CancellationToken ct = default)
        {
            try
            {
                // Usa el genérico: GetByIdAsync(object[] keyValues)
                var member = await _uow.GroupMembers.GetByIdAsync(new object[] { memberId }, ct);

                if (member is null || member.GroupId != groupId)
                    return ServiceResult<NoContent>.Fail("Group member not found.", ErrorType.NotFound);

                if (member.LeftAt is not null)
                    return ServiceResult<NoContent>.Ok(new NoContent(), "Member already disabled");

                // Soft delete lógico
                member.LeftAt = DateTime.UtcNow; // o DateTime.Now si trabajas con hora local

                _uow.GroupMembers.Update(member);
                await _uow.SaveChangesAsync(ct);

                return ServiceResult<NoContent>.Ok(new NoContent(), "Member disabled");
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

        private sealed record ExternalPeriodsAndDistributivos(
    IReadOnlyList<ExternalAcademicPeriodModel> Periods,
    IReadOnlyList<ExternalTeacherDistributivoModel> Distributivos
);

        private static string? NormalizeEmail(string? email)
        {
            if (string.IsNullOrWhiteSpace(email)) return null;
            var e = email.Trim().ToLowerInvariant();
            return string.IsNullOrWhiteSpace(e) ? null : e;
        }

        /// <summary>
        /// Carga períodos académicos + distributivos (batch por correos).
        /// Dedup distributivos por (Email, PeriodId) eligiendo determinísticamente el Max(Hours).
        /// </summary>
        private async Task<ServiceResult<ExternalPeriodsAndDistributivos>> LoadExternalPeriodsAndDistributivosAsync(
            IEnumerable<string> emails,
            CancellationToken ct)
        {
            // ===== 1) Periodos (1 llamada)
            var periodsRes = await _periods.GetAllAsync(ct);

            if (!periodsRes.Success || periodsRes.Data is null || periodsRes.Data.Count == 0)
            {
                return ServiceResult<ExternalPeriodsAndDistributivos>.Fail(
                    periodsRes.Message ?? "No academic periods found.",
                    periodsRes.Error == 0 ? ErrorType.NotFound : periodsRes.Error
                );
            }

            var periods = periodsRes.Data
                .Where(p => p is not null)
                .Where(p => p.StartDate <= p.EndDate)
                .OrderBy(p => p.StartDate)
                .ToList();

            // ===== 2) Correos normalizados
            var emailList = (emails ?? Enumerable.Empty<string>())
                .Select(NormalizeEmail)
                .Where(e => e is not null)
                .Select(e => e!)          // <- aquí conviertes a string
                .Distinct()
                .ToList();

            // ===== 3) Distributivos (1 llamada batch)
            var distributivos = new List<ExternalTeacherDistributivoModel>();

            if (emailList.Count > 0)
            {
                var distRes = await _distributivos.GetDistributivosByCorreosAsync(emailList!, ct);

                if (distRes.Success && distRes.Data is not null)
                {
                    distributivos = distRes.Data
                        .Where(d => !string.IsNullOrWhiteSpace(d.Email))
                        // Dedup por (email, periodId) => Max(Hours)
                        .GroupBy(d => new { Email = d.Email!.Trim().ToLowerInvariant(), d.PeriodId })
                        .Select(g => g.OrderByDescending(x => x.Hours).First())
                        .ToList();
                }
            }

            return ServiceResult<ExternalPeriodsAndDistributivos>.Ok(
                new ExternalPeriodsAndDistributivos(periods, distributivos),
                "External periods and distributivos loaded"
            );
        }


        private Task<ResolvedUserProfileDTO> ToExternalUserDTOAsync(
            ExternalUserProfileModel p,
            string? role,
            int groupId,
            int memberId,
            int memberRoleId,
            IReadOnlyList<ExternalAcademicPeriodModel> periods,
            IReadOnlyList<ExternalTeacherDistributivoModel> distributivos,
            CancellationToken ct)
        {
            var now = DateTime.UtcNow;

            // Filtrar distributivos SOLO de este usuario (por correo)
            var email = NormalizeEmail(p.Email);
            var userDist = email is null
                ? new List<ExternalTeacherDistributivoModel>()
                : distributivos
                    .Where(d => NormalizeEmail(d.Email) == email)
                    .ToList();

            ExternalTeacherFacultyCareerModel? chosen = null;

            if (p.Careers is { Count: > 0 })
            {
                chosen = ExternalCareerSelector.SelectProjectCareer(
                    profile: p,
                    distributivos: userDist,
                    projectStartDate: now,
                    periods: periods.ToList(), // si tu selector exige List, puedes mantenerlo así
                    onlyActivePreferred: true);

                chosen ??= ExternalCareerSelector.SelectBestCareer(
                    profile: p,
                    onlyActivePreferred: true);
            }

            return Task.FromResult(new ResolvedUserProfileDTO
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
            });
        }

        public async Task<ServiceResult<ProjectMembersReportDTO>> GetProjectMembersReportAsync(
            int projectId,
            CancellationToken ct = default)
        {
            try
            {
                // =========================
                // 1) Proyecto: grupo + fechas
                // =========================
                var projectInfo = await _uow.Projects
                    .Query()
                    .Where(p => p.ProjectId == projectId)
                    .Select(p => new
                    {
                        GroupId = p.ProjectGroupId,
                        StartDate = p.StartDate,            // DateTime?
                        EndDate = p.RealEndDate,            // DateTime?
                        DurationMonths = p.DurationInMonths // int
                    })
                    .FirstOrDefaultAsync(ct);

                if (projectInfo is null)
                    return ServiceResult<ProjectMembersReportDTO>.Fail("Project not found.", ErrorType.NotFound);

                if (projectInfo.GroupId <= 0)
                    return ServiceResult<ProjectMembersReportDTO>.Fail(
                        "Project not found or it has no associated group.",
                        ErrorType.NotFound);

                if (projectInfo.StartDate is null)
                    return ServiceResult<ProjectMembersReportDTO>.Fail(
                        "Project StartDate is required to generate the report.",
                        ErrorType.Validation);

                var projectStart = projectInfo.StartDate.Value.Date;
                var realEnd = (projectInfo.EndDate ?? DateTime.UtcNow).Date;

                if (realEnd < projectStart)
                    realEnd = projectStart;

                // =========================
                // 2) Miembros del grupo (histórico completo; incluye duplicados)
                // =========================
                var members = await _uow.GroupMembers
                    .QueryByGroup(projectInfo.GroupId, asNoTracking: true)
                    .ToListAsync(ct);

                if (members.Count == 0)
                    return ServiceResult<ProjectMembersReportDTO>.Fail(
                        "No group members found for the project.",
                        ErrorType.NotFound);

                // =========================
                // 3) Prórrogas (cada una equivale a 6 meses)
                // =========================
                var extensions = await _uow.ProjectExtensions.GetByProjectAsync(projectId, ct);
                extensions = extensions
                    .Where(e => e.ProjectExtensionTypeId is 1 or 2)
                    .OrderBy(e => e.ProjectExtensionId)
                    .ToList();

                var extensionCount = extensions.Count;

                // Fin del bloque BASE = realEnd - (6 * #prórrogas)
                var baseEnd = realEnd.AddMonths(-6 * extensionCount);
                if (baseEnd < projectStart) baseEnd = projectStart;

                // =========================
                // 4) Roles (Designación)
                // =========================
                var roleIds = members
                    .Where(m => m.MemberRoleId != 0)
                    .Select(m => m.MemberRoleId)
                    .Distinct()
                    .ToList();

                var roleNameById = new Dictionary<int, string>();
                if (roleIds.Count > 0)
                {
                    var rolesById = await _uow.MemberRoleTypeRepository.GetByIdsAsync(roleIds, include: null, ct: ct);
                    roleNameById = rolesById.ToDictionary(k => k.Key, v => v.Value.Name);
                }

                // =========================
                // 5) Resolver AppUserId -> AspNetUserId (IdLocal) (solo para consulta; dedup)
                // =========================
                var distinctAppUserIds = members
                    .Select(m => m.UserId) // AppUserId
                    .Where(id => id > 0)
                    .Distinct()
                    .ToList();

                if (distinctAppUserIds.Count == 0)
                    return ServiceResult<ProjectMembersReportDTO>.Fail(
                        "No associated app users found for project members.",
                        ErrorType.Validation);

                var aspNetIdByAppUserId = new Dictionary<int, int>();
                var aspNetIds = new HashSet<int>();

                for (int i = 0; i < distinctAppUserIds.Count; i++)
                {
                    var appUserId = distinctAppUserIds[i];
                    var appUser = await _uow.AppUsers.GetByIdAsync(new object[] { appUserId }, ct);

                    if (appUser?.IdLocal is int idLocal && idLocal > 0)
                    {
                        aspNetIdByAppUserId[appUserId] = idLocal;
                        aspNetIds.Add(idLocal);
                    }
                }

                if (aspNetIds.Count == 0)
                    return ServiceResult<ProjectMembersReportDTO>.Fail(
                        "No local identity ids found for project members.",
                        ErrorType.Validation);

                // =========================
                // 6) AspNetUserId -> Email (batch)
                // =========================
                var emailByAspId = await _uow.AspNetUsers.GetEmailsByUserIdsAsync(aspNetIds.ToList(), ct);

                var emailsDistinct = emailByAspId.Values
                    .Where(e => !string.IsNullOrWhiteSpace(e))
                    .Select(e => e!.Trim().ToLowerInvariant())
                    .Distinct()
                    .ToList();

                if (emailsDistinct.Count == 0)
                    return ServiceResult<ProjectMembersReportDTO>.Fail(
                        "No institutional emails found for project members.",
                        ErrorType.Validation);

                // =========================
                // 7) Directorio externo (FullName)
                // =========================
                var dirRes = await _directory.GetByEmailsAsync(emailsDistinct, ct);

                var profileByEmail = (dirRes.Success && dirRes.Data is not null)
                    ? dirRes.Data
                        .Where(p => !string.IsNullOrWhiteSpace(p.Email))
                        .GroupBy(p => p.Email!.Trim().ToLowerInvariant())
                        .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase)
                    : new Dictionary<string, ExternalUserProfileModel>(StringComparer.OrdinalIgnoreCase);

                // =========================
                // 8) Periodos académicos (externos) + Distributivos (externos)
                // =========================
                var extLoad = await LoadExternalPeriodsAndDistributivosAsync(emailsDistinct, ct);
                if (!extLoad.Success || extLoad.Data is null)
                {
                    return ServiceResult<ProjectMembersReportDTO>.Fail(
                        extLoad.Message ?? "Failed loading external periods/distributivos.",
                        extLoad.Error
                    );
                }

                var periods = extLoad.Data.Periods.ToList();          // si necesitas List
                var distributivos = extLoad.Data.Distributivos.ToList();

                // =========================
                // 9) Precomputar "períodos con horas" por cada registro histórico (incluye duplicados)
                // =========================
                var memberInfos = new List<MemberResolvedInfo>(members.Count);
                var ranges = new List<ExternalCareerSelector.EmailDateRange>(members.Count);

                foreach (var m in members)
                {
                    if (!aspNetIdByAppUserId.TryGetValue(m.UserId, out var aspId))
                        continue;

                    if (!emailByAspId.TryGetValue(aspId, out var em) || string.IsNullOrWhiteSpace(em))
                        continue;

                    var email = em.Trim().ToLowerInvariant();

                    var fullName = profileByEmail.TryGetValue(email, out var profile)
                        ? (profile.FullName ?? string.Empty)
                        : string.Empty;

                    var designation = roleNameById.TryGetValue(m.MemberRoleId, out var roleName)
                        ? roleName
                        : string.Empty;

                    // Para el cálculo de intersecciones, normalizamos joined/left a algo estable
                    var joined = (m.JoinedAt ?? projectStart).Date;
                    var left = (m.LeftAt ?? realEnd).Date;
                    if (left < joined) left = joined;

                    memberInfos.Add(new MemberResolvedInfo
                    {
                        Member = m,
                        Email = email,
                        FullName = fullName,
                        Designation = designation,
                        Joined = joined,
                        Left = m.LeftAt?.Date // null si activo, se resuelve con openEndedEndDate
                    });

                    ranges.Add(new ExternalCareerSelector.EmailDateRange(
                        Email: email,
                        EntryDate: joined,
                        ExitDate: m.LeftAt?.Date
                    ));
                }

                // Alineación 1:1 entre ranges y memberInfos
                var allPeriodHours = ExternalCareerSelector.ResolvePeriodHoursByRanges(
                    rows: ranges,
                    periods: periods,
                    distributivos: distributivos,
                    openEndedEndDate: realEnd
                );

                for (int i = 0; i < memberInfos.Count; i++)
                    memberInfos[i].PeriodHours = allPeriodHours[i] ?? Array.Empty<ExternalCareerSelector.PeriodHours>();

                // =========================
                // 10) Construcción de secciones (BASE + PRÓRROGAS)
                // =========================
                var sections = new List<ProjectMembersReportSectionDTO>();

                // A) BASE
                sections.Add(BuildSection(
                    title: projectInfo.DurationMonths > 0
                        ? $"DURACIÓN APROBADA CON RESOLUCIÓN (BASE) ({projectInfo.DurationMonths} MESES)"
                        : "DURACIÓN APROBADA CON RESOLUCIÓN (BASE)",
                    blockStart: projectStart,
                    blockEnd: baseEnd,
                    infos: memberInfos,
                    periods: periods,
                    onlyActiveMembers: false,
                    onlyCoordinatorPrincipal: false
                ));

                // B) PRÓRROGAS
                for (int i = 0; i < extensionCount; i++)
                {
                    var ext = extensions[i];

                    var extStart = baseEnd.AddMonths(6 * i);
                    var extEnd = baseEnd.AddMonths(6 * (i + 1));

                    if (extStart < projectStart) extStart = projectStart;
                    if (extEnd > realEnd) extEnd = realEnd;
                    if (extEnd < extStart) extEnd = extStart;

                    var onlyCoordinator = ext.ProjectExtensionTypeId == 2; // ampliación de plazo

                    var title = ext.ProjectExtensionTypeId == 2
                        ? $"DURACIÓN APROBADA CON RESOLUCIÓN (PRÓRROGA {i + 1}) (6 MESES) - SOLO COORDINADOR PRINCIPAL"
                        : $"DURACIÓN APROBADA CON RESOLUCIÓN (PRÓRROGA {i + 1}) (6 MESES)";

                    sections.Add(BuildSection(
                        title: title,
                        blockStart: extStart,
                        blockEnd: extEnd,
                        infos: memberInfos,
                        periods: periods,
                        onlyActiveMembers: true,               // PRÓRROGAS: quitar a todos con LeftAt
                        onlyCoordinatorPrincipal: onlyCoordinator
                    ));
                }

                var dto = new ProjectMembersReportDTO
                {
                    Sections = sections
                };

                return ServiceResult<ProjectMembersReportDTO>.Ok(dto, "Project members report generated.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error generating project members report for project {ProjectId}", projectId);
                return ServiceResult<ProjectMembersReportDTO>.Fail("Unexpected error.", ErrorType.Unexpected);
            }

            // =========================
            // Helpers locales
            // =========================

            ProjectMembersReportSectionDTO BuildSection(
                string title,
                DateTime blockStart,
                DateTime blockEnd,
                List<MemberResolvedInfo> infos,
                List<ExternalAcademicPeriodModel> periods,
                bool onlyActiveMembers,
                bool onlyCoordinatorPrincipal)
            {
                // Períodos que intersectan el bloque
                var blockPeriodIds = periods
                    .Where(p => p.StartDate.Date <= blockEnd.Date && blockStart.Date <= p.EndDate.Date)
                    .Select(p => p.PeriodId)
                    .ToHashSet();

                IEnumerable<MemberResolvedInfo> candidates = infos;

                if (onlyActiveMembers)
                    candidates = candidates.Where(x => x.Member.LeftAt is null);

                if (onlyCoordinatorPrincipal)
                {
                    // CP activo más reciente (JoinedAt más alto)
                    var cp = candidates
                        .Where(x => x.Member.MemberRoleId == 1)
                        .OrderByDescending(x => x.Member.JoinedAt ?? DateTime.MinValue)
                        .FirstOrDefault();

                    candidates = cp is null ? Enumerable.Empty<MemberResolvedInfo>() : new[] { cp };
                }

                var rows = new List<ProjectMemberReportRowDTO>();

                foreach (var x in candidates)
                {
                    // Rango del miembro (activo => hasta blockEnd)
                    var memberStart = (x.Member.JoinedAt ?? blockStart).Date;
                    var memberEnd = (x.Member.LeftAt ?? blockEnd).Date;

                    // Intersección con bloque
                    var start = memberStart < blockStart.Date ? blockStart.Date : memberStart;
                    var end = memberEnd > blockEnd.Date ? blockEnd.Date : memberEnd;

                    if (end < start)
                        continue;

                    // REGLA CLAVE:
                    // Solo entra al reporte si tiene al menos un período del bloque con Hours > 0
                    var hasResearchHoursInThisBlock = x.PeriodHours.Any(ph =>
                        ph.Hours > 0m && blockPeriodIds.Contains(ph.PeriodId));

                    if (!hasResearchHoursInThisBlock)
                        continue;

                    var (months, days) = CalculateMonthsDays(start, end);

                    rows.Add(new ProjectMemberReportRowDTO
                    {
                        FullName = x.FullName,
                        DesignationInProject = x.Designation,
                        ParticipationPeriod = BuildPeriodLabel(start, end),
                        Months = months,
                        Days = days
                    });
                }

                return new ProjectMembersReportSectionDTO
                {
                    Title = title,
                    Rows = rows
                };
            }

            static (int Months, int Days) CalculateMonthsDays(DateTime start, DateTime end)
            {
                int months = 0;
                var cursor = start;

                while (cursor.AddMonths(1) <= end)
                {
                    cursor = cursor.AddMonths(1);
                    months++;
                }

                var days = (end - cursor).Days;
                if (days < 0) days = 0;

                return (months, days);
            }

            static string BuildPeriodLabel(DateTime start, DateTime end)
            {
                var culture = GetSpanishCulture();

                string fmt(DateTime d)
                    => $"{culture.DateTimeFormat.GetMonthName(d.Month).ToLowerInvariant()}-{d.Year}";

                return $"{fmt(start)}/{fmt(end)}";
            }

            static CultureInfo GetSpanishCulture()
            {
                try { return CultureInfo.GetCultureInfo("es-EC"); }
                catch
                {
                    try { return CultureInfo.GetCultureInfo("es-ES"); }
                    catch { return CultureInfo.InvariantCulture; }
                }
            }
        }
        // DTO interno para no recalcular mappings
        sealed class MemberResolvedInfo
        {
            public GroupMember Member { get; set; } = default!;
            public string Email { get; set; } = string.Empty;
            public string FullName { get; set; } = string.Empty;
            public string Designation { get; set; } = string.Empty;

            public DateTime Joined { get; set; }
            public DateTime? Left { get; set; }

            public IReadOnlyList<ExternalCareerSelector.PeriodHours> PeriodHours { get; set; }
                = Array.Empty<ExternalCareerSelector.PeriodHours>();
        }
    }
}
