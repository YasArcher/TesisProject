using Microsoft.EntityFrameworkCore;
using System.Globalization;
using tesisproject.backend.Services.Interfaces;
using tesisproject.backend.Services.Unified.Interfaces;
using tesisproject.backend.UnitOfWork.Unified.Interfaces;
using tesisproject.shared.Auth;
using tesisproject.shared.Common.External;
using tesisproject.shared.DTOs.AppUser;
using tesisproject.shared.DTOs.Auth;
using tesisproject.shared.DTOs.Group.Request;
using tesisproject.shared.DTOs.Group.Response;
using tesisproject.backend.Data.UnifiedEntities.Core;
using tesisproject.shared.Entities.External;
using tesisproject.shared.Enums;
using tesisproject.shared.Responses;
using tesisproject.shared.Errors;

namespace tesisproject.backend.Services.Unified.Implementations
{
    public class UnifiedGroupService : IUnifiedGroupService
    {
        private readonly IUnifiedUnitOfWork _uow;
        private readonly IUnifiedIdentityProvisioningService _identity;
        private readonly IUnifiedIdentityQueryService _identityQuery;
        private readonly IExternalDirectoryClient _directory;
        private readonly ILogger<UnifiedGroupService> _logger;
        private readonly IExternalPeriodsClient _periods;
        private readonly IExternalDistributivosService _distributivos;
























        private const string CultureEsEc = "es-EC";

        private const string CultureEsEs = "es-ES";

        private const string TemporaryPassword = "Temporal#123";







        // ================= Constants =================
        private const int ExtensionDurationMonths = 6;


        // Mensajes (mantener texto EXACTO)













































        public UnifiedGroupService(
            IUnifiedUnitOfWork uow,
            IExternalDirectoryClient directory,
            ILogger<UnifiedGroupService> logger,
            IExternalPeriodsClient periods,
            IExternalDistributivosService distributivos,
            IUnifiedIdentityProvisioningService identity, IUnifiedIdentityQueryService identityQuery)
        {
            _uow = uow;
            _identity = identity;
            _identityQuery = identityQuery;
            _directory = directory;
            _logger = logger;
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
                    return ServiceResult<GroupResponseDTO>.Fail(ErrorMessages.UnifiedLegacy.GroupService_GroupNotFoundMessage, ErrorType.NotFound, ErrorCodes.Common.NotFound);

                var dto = ToGroupResponseDTO(group);
                return ServiceResult<GroupResponseDTO>.Ok(dto, ErrorMessages.UnifiedLegacy.GroupService_GroupRetrievedMessage);
            }
            catch (Exception ex)
            {
                return ServiceResult<GroupResponseDTO>.Fail(ex.Message, ErrorType.Unexpected, ErrorCodes.Common.UnexpectedError);
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
                    return ServiceResult<IReadOnlyList<GroupResponseDTO>>.Fail(ErrorMessages.UnifiedLegacy.GroupService_NoGroupsFoundMessage, ErrorType.NotFound, ErrorCodes.Common.NotFound);

                return ServiceResult<IReadOnlyList<GroupResponseDTO>>.Ok(items, ErrorMessages.UnifiedLegacy.GroupService_GroupsRetrievedMessage);
            }
            catch (Exception ex)
            {
                return ServiceResult<IReadOnlyList<GroupResponseDTO>>.Fail(ex.Message, ErrorType.Unexpected, ErrorCodes.Common.UnexpectedError);
            }
        }

        public async Task<ServiceResult<IReadOnlyList<GroupResponseDTO>>> GetByProjectAsync(int projectId, CancellationToken ct = default)
        {
            try
            {
                var groups = await _uow.Groups.GetByProjectIdAsync(projectId, ct);

                if (groups.Count == 0)
                    return ServiceResult<IReadOnlyList<GroupResponseDTO>>.Fail(ErrorMessages.UnifiedLegacy.GroupService_ProjectNotFoundOrNoAssociatedGroupsMessage, ErrorType.NotFound, ErrorCodes.Common.NotFound);

                var dtos = groups.Select(g => new GroupResponseDTO
                {
                    GroupId = g.GroupId,
                    GroupTypeId = g.GroupTypeId,
                    Name = g.Name
                }).ToList();

                return ServiceResult<IReadOnlyList<GroupResponseDTO>>.Ok(dtos, ErrorMessages.UnifiedLegacy.GroupService_GroupsByProjectRetrievedMessage);
            }
            catch (Exception ex)
            {
                return ServiceResult<IReadOnlyList<GroupResponseDTO>>.Fail(ex.Message, ErrorType.Unexpected, ErrorCodes.Common.UnexpectedError);
            }
        }

        // ======= External users (unificado aquí) =======

        public async Task<ServiceResult<List<ResolvedUserProfileDTO>>> GetExternalUsersByGroupAsync(int groupId, CancellationToken ct = default)
        {
            try
            {
                var exists = await _uow.Groups.ExistsAsync(g => g.GroupId == groupId, ct);
                if (!exists)
                    return ServiceResult<List<ResolvedUserProfileDTO>>.Fail(ErrorMessages.UnifiedLegacy.GroupService_GroupNotFoundMessage, ErrorType.NotFound, ErrorCodes.Common.NotFound);

                // 1) Miembros del grupo (tabla puente)
                var members = await _uow.GroupMembers.GetMembersByGroupAsync(groupId, ct: ct);
                if (members is null || members.Count == 0)
                    return ServiceResult<List<ResolvedUserProfileDTO>>.Fail(ErrorMessages.UnifiedLegacy.GroupService_NoGroupMembersFoundMessage, ErrorType.NotFound, ErrorCodes.Common.NotFound);

                // Nota: en tu caso member.UserId = AppUser.IdUser (PK de APP_USER)
                var appUserIds = members.Select(m => m.UserId).Distinct().ToList();
                if (appUserIds.Count == 0)
                    return ServiceResult<List<ResolvedUserProfileDTO>>.Fail(ErrorMessages.UnifiedLegacy.GroupService_NoAssociatedAppUsersFoundMessage, ErrorType.Validation, ErrorCodes.Common.InvalidRequest);

                // 2) Resolver IdLocal (IdentityUser.Id) desde AppUser para consultar emails institucionales
                // Mapa: AppUser.IdUser -> AppUser.IdLocal
                var identityIdByAppUserId = new Dictionary<int, int>();

                foreach (var appUserId in appUserIds)
                {
                    var appUser = await _uow.AppUsers.GetByIdUserAsync(appUserId, ct);
                    if (appUser?.IdLocal is int identityUserId && identityUserId > 0)
                        identityIdByAppUserId[appUserId] = identityUserId;
                }

                if (identityIdByAppUserId.Count == 0)
                    return ServiceResult<List<ResolvedUserProfileDTO>>.Fail(ErrorMessages.UnifiedLegacy.GroupService_NoLocalIdentityIdsForGroupMembersMessage, ErrorType.Validation, ErrorCodes.Common.InvalidRequest);

                var identityUserIds = identityIdByAppUserId.Values.Distinct().ToList();

                // 3) Emails institucionales (por IdentityUser.Id / IdLocal)
                var emailByIdentityId = await _identityQuery.GetEmailsByUserIdsAsync(identityUserIds, ct);

                var allEmails = emailByIdentityId.Values
                    .Where(e => !string.IsNullOrWhiteSpace(e))
                    .Select(e => e!.Trim().ToLowerInvariant())
                    .Distinct()
                    .ToList();

                if (allEmails.Count == 0)
                    return ServiceResult<List<ResolvedUserProfileDTO>>.Fail(ErrorMessages.UnifiedLegacy.GroupService_NoValidEmailsFoundForUsersMessage, ErrorType.Validation, ErrorCodes.Common.InvalidRequest);

                // 4) Rehidratar roles si faltan (evitar N+1) - misma lógica
                await EnsureMemberRolesLoadedAsync(members, ct);

                // 5) Directorio externo en batch
                var dirRes = await _directory.GetByEmailsAsync(allEmails, ct);
                if (!dirRes.Success || dirRes.Data is null || dirRes.Data.Count == 0)
                    return ServiceResult<List<ResolvedUserProfileDTO>>.Fail(ErrorMessages.UnifiedLegacy.GroupService_NoExternalUsersFoundMessage);

                var byEmail = dirRes.Data
                    .Where(p => !string.IsNullOrWhiteSpace(p.Email))
                    .GroupBy(p => p.Email!.Trim().ToLowerInvariant())
                    .ToDictionary(g => g.Key, g => g.First());

                // 6) Merge (local + externo) → ResolvedUserProfileDTO
                var result = new List<ResolvedUserProfileDTO>();

                // Cargar UNA sola vez periodos + distributivos para TODOS los correos del grupo
                var extLoad = await LoadExternalPeriodsAndDistributivosAsync(allEmails, ct);
                if (!extLoad.Success || extLoad.Data is null)
                    return ServiceResult<List<ResolvedUserProfileDTO>>.Fail(
                        extLoad.Message ?? ErrorMessages.UnifiedLegacy.GroupService_FailedLoadingExternalDataMessage,
                        extLoad.Error);

                var periods = extLoad.Data.Periods;
                var distributivos = extLoad.Data.Distributivos;

                foreach (var member in members)
                {
                    if (!identityIdByAppUserId.TryGetValue(member.UserId, out var identityUserId))
                        continue;

                    if (!emailByIdentityId.TryGetValue(identityUserId, out var em) || string.IsNullOrWhiteSpace(em))
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
                    return ServiceResult<List<ResolvedUserProfileDTO>>.Fail(ErrorMessages.UnifiedLegacy.GroupService_NoExternalUsersMatchedGroupMembersMessage, ErrorType.NotFound, ErrorCodes.Common.NotFound);

                return ServiceResult<List<ResolvedUserProfileDTO>>.Ok(result, ErrorMessages.UnifiedLegacy.GroupService_ExternalUsersByGroupRetrievedMessage);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error retrieving external users for group {GroupId}", groupId);
                return ServiceResult<List<ResolvedUserProfileDTO>>.Fail(ErrorMessages.UnifiedLegacy.GroupService_UnexpectedErrorMessage, ErrorType.Unexpected, ErrorCodes.Common.UnexpectedError);
            }
        }

        public async Task<ServiceResult<ResolvedUserProfileDTO>> GetExternalUserByAspNetIdAsync(int userId, CancellationToken ct = default)
        {
            try
            {
                var local = await _identityQuery.GetEmailByUserIdAsync(userId, ct);
                if (local is null)
                    return ServiceResult<ResolvedUserProfileDTO>.Fail(ErrorMessages.UnifiedLegacy.GroupService_AspNetUserNotFoundMessage, ErrorType.NotFound, ErrorCodes.Common.NotFound);

                var email = (local.Value.Email ?? string.Empty).Trim().ToLowerInvariant();
                if (string.IsNullOrWhiteSpace(email))
                    return ServiceResult<ResolvedUserProfileDTO>.Fail(ErrorMessages.UnifiedLegacy.GroupService_UserHasNoInstitutionalEmailMessage, ErrorType.Validation, ErrorCodes.Common.InvalidRequest);

                var dirRes = await _directory.GetByEmailsAsync(new[] { email }, ct);
                var profile = dirRes.Data?.FirstOrDefault();
                if (profile is null)
                    return ServiceResult<ResolvedUserProfileDTO>.Fail(ErrorMessages.UnifiedLegacy.GroupService_ExternalUserNotFoundForGivenEmailMessage, ErrorType.NotFound, ErrorCodes.Common.NotFound);

                var extLoad = await LoadExternalPeriodsAndDistributivosAsync(new[] { email }, ct);
                if (!extLoad.Success || extLoad.Data is null)
                    return ServiceResult<ResolvedUserProfileDTO>.Fail(
                        extLoad.Message ?? ErrorMessages.UnifiedLegacy.GroupService_FailedLoadingExternalPeriodsDistributivosMessage,
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

                return ServiceResult<ResolvedUserProfileDTO>.Ok(dto, ErrorMessages.UnifiedLegacy.GroupService_ExternalUserResolvedByAspNetUserIdMessage);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error resolving external user for ASP.NET user {UserId}", userId);
                return ServiceResult<ResolvedUserProfileDTO>.Fail(ErrorMessages.UnifiedLegacy.GroupService_UnexpectedErrorMessage, ErrorType.Unexpected, ErrorCodes.Common.UnexpectedError);
            }
        }

        public async Task<ServiceResult<ResolvedUserProfileDTO>> GetExternalUserByEmailAsync(string institutionalEmail, CancellationToken ct = default)
        {
            var email = (institutionalEmail ?? string.Empty).Trim().ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(email))
                return ServiceResult<ResolvedUserProfileDTO>.Fail(ErrorMessages.UnifiedLegacy.GroupService_EmailIsRequiredMessage, ErrorType.Validation, ErrorCodes.Common.InvalidRequest);

            try
            {
                var dirRes = await _directory.GetByEmailsAsync(new[] { email }, ct);
                var profile = dirRes.Data?.FirstOrDefault();
                if (profile is null)
                    return ServiceResult<ResolvedUserProfileDTO>.Fail(ErrorMessages.UnifiedLegacy.GroupService_ExternalUserNotFoundMessage, ErrorType.NotFound, ErrorCodes.Common.NotFound);

                var extLoad = await LoadExternalPeriodsAndDistributivosAsync(new[] { email }, ct);
                if (!extLoad.Success || extLoad.Data is null)
                    return ServiceResult<ResolvedUserProfileDTO>.Fail(
                        extLoad.Message ?? ErrorMessages.UnifiedLegacy.GroupService_FailedLoadingExternalPeriodsDistributivosMessage,
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

                return ServiceResult<ResolvedUserProfileDTO>.Ok(dto, ErrorMessages.UnifiedLegacy.GroupService_ExternalUserRetrievedByEmailMessage);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error retrieving external user by email {Email}", email);
                return ServiceResult<ResolvedUserProfileDTO>.Fail(ErrorMessages.UnifiedLegacy.GroupService_UnexpectedErrorMessage, ErrorType.Unexpected, ErrorCodes.Common.UnexpectedError);
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
                        ErrorMessages.UnifiedLegacy.GroupService_NoExternalUsersFoundMessage,
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
                        extLoad.Message ?? ErrorMessages.UnifiedLegacy.GroupService_FailedLoadingExternalPeriodsDistributivosMessage,
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

                return ServiceResult<List<ResolvedUserProfileDTO>>.Ok(list, ErrorMessages.UnifiedLegacy.GroupService_ExternalUsersRetrievedMessage);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error retrieving all external users");
                return ServiceResult<List<ResolvedUserProfileDTO>>.Fail(ErrorMessages.UnifiedLegacy.GroupService_UnexpectedErrorMessage, ErrorType.Unexpected, ErrorCodes.Common.UnexpectedError);
            }
        }

        // ================= WRITES =================

        public async Task<ServiceResult<GroupResponseDTO>> CreateAsync(AddGroupRequestDTO request, CancellationToken ct = default)
        {
            try
            {
                var name = (request.Name ?? string.Empty).Trim();
                if (string.IsNullOrWhiteSpace(name))
                    return ServiceResult<GroupResponseDTO>.Fail(ErrorMessages.UnifiedLegacy.GroupService_GroupNameRequiredMessage, ErrorType.Validation, ErrorCodes.Common.InvalidRequest);

                var exists = await _uow.Groups.NameExistsAsync(name, ct);
                if (exists)
                    return ServiceResult<GroupResponseDTO>.Fail(ErrorMessages.UnifiedLegacy.GroupService_GroupNameAlreadyExistsMessage, ErrorType.Conflict, ErrorCodes.Common.PersistenceConflict);

                var entity = new Group { GroupTypeId = request.GroupTypeId, Name = name };
                await _uow.Groups.AddAsync(entity, ct);
                await _uow.SaveChangesAsync(ct);

                var dto = ToGroupResponseDTO(entity);
                return ServiceResult<GroupResponseDTO>.Ok(dto, ErrorMessages.UnifiedLegacy.GroupService_GroupCreatedMessage);
            }
            catch (DbUpdateException dbex)
            {
                return ServiceResult<GroupResponseDTO>.Fail(dbex.InnerException?.Message ?? dbex.Message, ErrorType.Conflict, ErrorCodes.Common.PersistenceConflict);
            }
            catch (Exception ex)
            {
                return ServiceResult<GroupResponseDTO>.Fail(ex.Message, ErrorType.Unexpected, ErrorCodes.Common.UnexpectedError);
            }
        }

        // Preparation for the coordinator branch of AddMemberAsync; no provisioning or commit.
        internal async Task<ServiceResult<tesisproject.backend.Data.UnifiedEntities.Articles.Faculty>> PrepareCoordinatorFacultyAsync(
            AddGroupMemberRequestDTO request, CancellationToken ct = default)
        {
            if (request.FacultyId is null or <= 0)
                return UnifiedAcademicReferencePreparation.Invalid<tesisproject.backend.Data.UnifiedEntities.Articles.Faculty>(nameof(request.FacultyId));
            // ProjectDetail sends FacultyCareerId in this legacy field. Resolve its parent
            // from the directory before looking up the synchronized local faculty.
            var profiles = await _directory.GetByEmailsAsync(new[] { request.Email.Trim() }, ct);
            if (!profiles.Success)
                return UnifiedAcademicReferencePreparation.Relay<tesisproject.backend.Data.UnifiedEntities.Articles.Faculty, IReadOnlyList<ExternalUserProfileModel>>(profiles);
            var careers = profiles.Data?.SelectMany(p => p.Careers ?? []).ToList() ?? [];
            var selected = careers.FirstOrDefault(c => c.FacultyCareerId == request.FacultyId);
            var externalFacultyId = selected?.FacultyId ?? selected?.FacultyCareerId;
            externalFacultyId ??= careers.Any(c => c.FacultyId == request.FacultyId) ? request.FacultyId : null;
            if (!externalFacultyId.HasValue)
                return ServiceResult<tesisproject.backend.Data.UnifiedEntities.Articles.Faculty>.Fail(
                    ErrorMessages.Project.FacultyCareerResolutionFailed, ErrorType.Validation, ErrorCodes.Project.FacultyCareerResolutionFailed);
            return await UnifiedAcademicReferencePreparation.FacultyAsync(_uow, externalFacultyId.Value, ct);
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
                    return ServiceResult<GroupMemberResponseDTO>.Fail(ErrorMessages.UnifiedLegacy.GroupService_GroupNotFoundMessage, ErrorType.NotFound, ErrorCodes.Common.NotFound);

                if (request.MemberRole == 0)
                    return ServiceResult<GroupMemberResponseDTO>.Fail(ErrorMessages.UnifiedLegacy.GroupService_MemberRoleRequiredMessage, ErrorType.Validation, ErrorCodes.Common.InvalidRequest);

                var email = (request.Email ?? string.Empty).Trim().ToLowerInvariant();
                if (string.IsNullOrWhiteSpace(email))
                    return ServiceResult<GroupMemberResponseDTO>.Fail(ErrorMessages.UnifiedLegacy.GroupService_InstitutionalEmailRequiredMessage, ErrorType.Validation, ErrorCodes.Common.InvalidRequest);

                var document = (request.Document ?? string.Empty).Trim();
                if (string.IsNullOrWhiteSpace(document))
                    return ServiceResult<GroupMemberResponseDTO>.Fail(ErrorMessages.UnifiedLegacy.GroupService_DocumentRequiredMessage, ErrorType.Validation, ErrorCodes.Common.InvalidRequest);



                // 2) Construir RegisterRequest directo desde el DTO
                var appRole = request.MemberRole switch
                {
                    MemberRoleTypeIds.Coordinador => AppRoles.Coordinador,
                    MemberRoleTypeIds.Subrogante => AppRoles.Coordinador,
                    _ => AppRoles.User
                };

                var registerDto = new RegisterRequest
                {
                    Email = email,
                    Username = document,
                    Password = TemporaryPassword,
                    AspUserId = request.AspUserId,
                    Role = appRole
                };

                var known = await _identity.ResolveAsync(registerDto, ct);
                if (!known.Success) return UnifiedAcademicReferencePreparation.Relay<GroupMemberResponseDTO, int?>(known);
                if (known.Data.HasValue && await _uow.GroupMembers.ExistsAsync(request.GroupId, known.Data.Value, ct))
                    return ServiceResult<GroupMemberResponseDTO>.Fail(ErrorMessages.UnifiedLegacy.GroupService_UserAlreadyMemberOfGroupMessage, ErrorType.Conflict, ErrorCodes.Common.PersistenceConflict);
                Project? selectedProject = null;
                int? selectedFacultyId = null;
                if (request.MemberRole == MemberRoleTypeIds.Coordinador)
                {
                    selectedProject = await _uow.Projects.Query(asNoTracking: false).FirstOrDefaultAsync(p => p.ProjectGroupId == request.GroupId, ct);
                    if (selectedProject is null)
                        return ServiceResult<GroupMemberResponseDTO>.Fail(ErrorMessages.UnifiedLegacy.GroupService_NoProjectAssociatedToGroupMessage, ErrorType.NotFound, ErrorCodes.Common.NotFound);
                    var faculty = await PrepareCoordinatorFacultyAsync(request, ct);
                    if (!faculty.Success) return UnifiedAcademicReferencePreparation.Relay<GroupMemberResponseDTO, tesisproject.backend.Data.UnifiedEntities.Articles.Faculty>(faculty);
                    selectedFacultyId = faculty.Data!.FacultyId;
                }
                if (!await _uow.MemberRoleTypes.ExistsAsync(r => r.Id == request.MemberRole, ct))
                    return ServiceResult<GroupMemberResponseDTO>.Fail(ErrorMessages.UnifiedLegacy.GroupService_MemberRoleRequiredMessage, ErrorType.Validation, ErrorCodes.Common.InvalidRequest);
                var ensured = await _identity.EnsureAsync(registerDto, ct);
                if (!ensured.Success) return UnifiedAcademicReferencePreparation.Relay<GroupMemberResponseDTO, int>(ensured);
                var appUserIdUser = ensured.Data;

                // 5.1) Regla: si agrega Coordinador Principal (1), actualizar facultad del proyecto
                if (request.MemberRole == MemberRoleTypeIds.Coordinador)
                {
                    selectedProject!.FacultyId = selectedFacultyId!.Value;
                    // (D) Recomendado: cerrar cualquier Coordinador Principal activo previo (histórico)
                    var actives = await _uow.GroupMembers
                        .QueryByGroup(request.GroupId, asNoTracking: false)
                        .Where(m => m.LeftAt == null && m.MemberRoleId == MemberRoleTypeIds.Coordinador)
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
                    UserId = appUserIdUser,
                    MemberRoleId = request.MemberRole,
                    JoinedAt = DateTime.UtcNow
                };

                await _uow.GroupMembers.AddAsync(member, ct);
                await _uow.SaveChangesAsync(ct);

                // 7) Resolver nombre del rol (igual que antes)
                var roleName = await ResolveMemberRoleNameAsync(member.MemberRoleId, ct);

                var dto = new GroupMemberResponseDTO
                {
                    GroupMemberId = member.GroupMemberId,
                    ExternalUserId = member.UserId,
                    MemberRole = roleName,
                    JoinedAt = member.JoinedAt
                };

                return ServiceResult<GroupMemberResponseDTO>.Ok(dto, ErrorMessages.UnifiedLegacy.GroupService_MemberAddedToGroupMessage);
            }
            catch (DbUpdateException dbex)
            {
                return ServiceResult<GroupMemberResponseDTO>.Fail(
                    dbex.InnerException?.Message ?? dbex.Message,
                    ErrorType.Conflict, ErrorCodes.Common.PersistenceConflict);
            }
            catch (Exception ex)
            {
                return ServiceResult<GroupMemberResponseDTO>.Fail(ex.Message, ErrorType.Unexpected, ErrorCodes.Common.UnexpectedError);
            }
        }

        public async Task<ServiceResult<GroupResponseDTO>> UpdateAsync(UpdateGroupRequestDTO request, CancellationToken ct = default)
        {
            try
            {
                var group = await _uow.Groups.GetByIdAsync(request.GroupId, includeMembers: false, ct);
                if (group is null)
                    return ServiceResult<GroupResponseDTO>.Fail(ErrorMessages.UnifiedLegacy.GroupService_GroupNotFoundMessage, ErrorType.NotFound, ErrorCodes.Common.NotFound);

                var name = (request.Name ?? string.Empty).Trim();
                if (string.IsNullOrWhiteSpace(name))
                    return ServiceResult<GroupResponseDTO>.Fail(ErrorMessages.UnifiedLegacy.GroupService_GroupNameRequiredMessage, ErrorType.Validation, ErrorCodes.Common.InvalidRequest);

                group.Name = name;
                group.GroupTypeId = request.GroupTypeId;

                _uow.Groups.Update(group);
                await _uow.SaveChangesAsync(ct);

                var dto = ToGroupResponseDTO(group);
                return ServiceResult<GroupResponseDTO>.Ok(dto, ErrorMessages.UnifiedLegacy.GroupService_GroupUpdatedMessage);
            }
            catch (DbUpdateException dbex)
            {
                return ServiceResult<GroupResponseDTO>.Fail(dbex.InnerException?.Message ?? dbex.Message, ErrorType.Conflict, ErrorCodes.Common.PersistenceConflict);
            }
            catch (Exception ex)
            {
                return ServiceResult<GroupResponseDTO>.Fail(ex.Message, ErrorType.Unexpected, ErrorCodes.Common.UnexpectedError);
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
                    return ServiceResult<NoContent>.Fail(ErrorMessages.UnifiedLegacy.GroupService_GroupMemberNotFoundMessage, ErrorType.NotFound, ErrorCodes.Common.NotFound);

                if (member.LeftAt is not null)
                    return ServiceResult<NoContent>.Ok(new NoContent(), ErrorMessages.UnifiedLegacy.GroupService_MemberAlreadyDisabledMessage);

                // Soft delete lógico
                member.LeftAt = DateTime.UtcNow; // o DateTime.Now si trabajas con hora local

                _uow.GroupMembers.Update(member);
                await _uow.SaveChangesAsync(ct);

                return ServiceResult<NoContent>.Ok(new NoContent(), ErrorMessages.UnifiedLegacy.GroupService_MemberDisabledMessage);
            }
            catch (DbUpdateException dbex)
            {
                return ServiceResult<NoContent>.Fail(dbex.InnerException?.Message ?? dbex.Message, ErrorType.Conflict, ErrorCodes.Common.PersistenceConflict);
            }
            catch (Exception ex)
            {
                return ServiceResult<NoContent>.Fail(ex.Message, ErrorType.Unexpected, ErrorCodes.Common.UnexpectedError);
            }
        }

        // ================= Helpers =================

        private static GroupResponseDTO ToGroupResponseDTO(Group group)
        {
            return new GroupResponseDTO
            {
                GroupId = group.GroupId,
                GroupTypeId = group.GroupTypeId,
                Name = group.Name
            };
        }

        private async Task EnsureMemberRolesLoadedAsync(List<GroupMember> members, CancellationToken ct)
        {
            if (!members.Any(m => m.MemberRole == null && m.MemberRoleId != 0))
                return;

            var roleIds = members
                .Where(m => m.MemberRoleId != 0)
                .Select(m => m.MemberRoleId)
                .Distinct()
                .ToList();

            var rolesById = await _uow.MemberRoleTypes.GetByIdsAsync(
                roleIds,
                include: null,
                ct: ct
            );

            foreach (var m in members)
            {
                if (m.MemberRole == null && m.MemberRoleId != 0 &&
                    rolesById.TryGetValue(m.MemberRoleId, out var role))
                {
                    m.MemberRole = role;
                }
            }
        }

        private async Task<string> ResolveMemberRoleNameAsync(int memberRoleId, CancellationToken ct)
        {
            string roleName = string.Empty;

            if (memberRoleId != 0)
            {
                var rolesById = await _uow.MemberRoleTypes.GetByIdsAsync(
                    new[] { memberRoleId },
                    include: null,
                    ct: ct
                );

                if (rolesById.TryGetValue(memberRoleId, out var role))
                    roleName = role.Name;
            }

            return roleName;
        }

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
                    periodsRes.Message ?? ErrorMessages.UnifiedLegacy.GroupService_NoAcademicPeriodsFoundMessage,
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
                .Select(e => e!)
                .Distinct()
                .ToList();

            // ===== 3) Distributivos (1 llamada batch)
            var distributivos = new List<ExternalTeacherDistributivoModel>();

            if (emailList.Count > 0)
            {
                var distRes = await _distributivos.GetDistributivosByCorreosAsync(emailList, ct);

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
                ErrorMessages.UnifiedLegacy.GroupService_ExternalPeriodsAndDistributivosLoadedMessage
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
                Role = role!, // Preserve optional external role returned by legacy transport.
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
                    return ServiceResult<ProjectMembersReportDTO>.Fail(ErrorMessages.UnifiedLegacy.GroupService_ProjectNotFoundMessage, ErrorType.NotFound, ErrorCodes.Common.NotFound);

                if (projectInfo.GroupId <= 0)
                    return ServiceResult<ProjectMembersReportDTO>.Fail(
                        ErrorMessages.UnifiedLegacy.GroupService_ProjectNotFoundOrNoAssociatedGroupMessage,
                        ErrorType.NotFound, ErrorCodes.Common.NotFound);

                if (projectInfo.StartDate is null)
                    return ServiceResult<ProjectMembersReportDTO>.Fail(
                        ErrorMessages.UnifiedLegacy.GroupService_ProjectStartDateRequiredForReportMessage,
                        ErrorType.Validation, ErrorCodes.Common.InvalidRequest);

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
                        ErrorMessages.UnifiedLegacy.GroupService_NoGroupMembersFoundForProjectMessage,
                        ErrorType.NotFound, ErrorCodes.Common.NotFound);

                // =========================
                // 3) Prórrogas (cada una equivale a 6 meses)
                // =========================
                var extensions = await _uow.ProjectExtensions.GetByProjectAsync(projectId, ct);
                extensions = extensions
                    .Where(e => e.ProjectExtensionTypeId is ProjectExtensionTypeIds.Prorroga or ProjectExtensionTypeIds.AmpliacionPlazo)
                    .OrderBy(e => e.ProjectExtensionId)
                    .ToList();

                var extensionCount = extensions.Count;

                // Fin del bloque BASE = realEnd - (6 * #prórrogas)
                var baseEnd = realEnd.AddMonths(-ExtensionDurationMonths * extensionCount);
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
                    var rolesById = await _uow.MemberRoleTypes.GetByIdsAsync(roleIds, include: null, ct: ct);
                    roleNameById = rolesById.ToDictionary(k => k.Key, v => v.Value.Name);
                }

                // =========================
                // 5) Resolver AppUserId -> IdentityUser.Id (IdLocal) (solo para consulta; dedup)
                // Nota: aquí GroupMember.UserId = AppUser.IdUser
                // =========================
                var distinctAppUserIds = members
                    .Select(m => m.UserId) // AppUser.IdUser
                    .Where(id => id > 0)
                    .Distinct()
                    .ToList();

                if (distinctAppUserIds.Count == 0)
                    return ServiceResult<ProjectMembersReportDTO>.Fail(
                        ErrorMessages.UnifiedLegacy.GroupService_NoAssociatedAppUsersForProjectMembersMessage,
                        ErrorType.Validation, ErrorCodes.Common.InvalidRequest);

                var identityIdByAppUserId = new Dictionary<int, int>();
                var identityIds = new HashSet<int>();

                for (int i = 0; i < distinctAppUserIds.Count; i++)
                {
                    var appUserId = distinctAppUserIds[i];
                    var appUser = await _uow.AppUsers.GetByIdAsync(new object[] { appUserId }, ct);

                    if (appUser?.IdLocal is int idLocal && idLocal > 0)
                    {
                        identityIdByAppUserId[appUserId] = idLocal;
                        identityIds.Add(idLocal);
                    }
                }

                if (identityIds.Count == 0)
                    return ServiceResult<ProjectMembersReportDTO>.Fail(
                        ErrorMessages.UnifiedLegacy.GroupService_NoLocalIdentityIdsForProjectMembersMessage,
                        ErrorType.Validation, ErrorCodes.Common.InvalidRequest);

                // =========================
                // 6) IdentityUser.Id -> Email (batch)
                // =========================
                var emailByIdentityId = await _identityQuery.GetEmailsByUserIdsAsync(identityIds.ToList(), ct);

                var emailsDistinct = emailByIdentityId.Values
                    .Where(e => !string.IsNullOrWhiteSpace(e))
                    .Select(e => e!.Trim().ToLowerInvariant())
                    .Distinct()
                    .ToList();

                if (emailsDistinct.Count == 0)
                    return ServiceResult<ProjectMembersReportDTO>.Fail(
                        ErrorMessages.UnifiedLegacy.GroupService_NoInstitutionalEmailsForProjectMembersMessage,
                        ErrorType.Validation, ErrorCodes.Common.InvalidRequest);

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
                        extLoad.Message ?? ErrorMessages.UnifiedLegacy.GroupService_FailedLoadingExternalPeriodsDistributivosMessage,
                        extLoad.Error
                    );
                }

                var periods = extLoad.Data.Periods.ToList();
                var distributivos = extLoad.Data.Distributivos.ToList();

                // =========================
                // 9) Precomputar "períodos con horas" por cada registro histórico (incluye duplicados)
                // =========================
                var memberInfos = new List<MemberResolvedInfo>(members.Count);
                var ranges = new List<ExternalCareerSelector.EmailDateRange>(members.Count);

                foreach (var m in members)
                {
                    if (!identityIdByAppUserId.TryGetValue(m.UserId, out var identityUserId))
                        continue;

                    if (!emailByIdentityId.TryGetValue(identityUserId, out var em) || string.IsNullOrWhiteSpace(em))
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
                        Left = m.LeftAt?.Date
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

                    var extStart = baseEnd.AddMonths(ExtensionDurationMonths * i);
                    var extEnd = baseEnd.AddMonths(ExtensionDurationMonths * (i + 1));

                    if (extStart < projectStart) extStart = projectStart;
                    if (extEnd > realEnd) extEnd = realEnd;
                    if (extEnd < extStart) extEnd = extStart;

                    var onlyCoordinator = ext.ProjectExtensionTypeId == ProjectExtensionTypeIds.AmpliacionPlazo; // ampliación de plazo

                    var title = ext.ProjectExtensionTypeId == ProjectExtensionTypeIds.AmpliacionPlazo
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

                return ServiceResult<ProjectMembersReportDTO>.Ok(dto, ErrorMessages.UnifiedLegacy.GroupService_ProjectMembersReportGeneratedMessage);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error generating project members report for project {ProjectId}", projectId);
                return ServiceResult<ProjectMembersReportDTO>.Fail(ErrorMessages.UnifiedLegacy.GroupService_UnexpectedErrorMessage, ErrorType.Unexpected, ErrorCodes.Common.UnexpectedError);
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
                        .Where(x => x.Member.MemberRoleId == MemberRoleTypeIds.Coordinador)
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
                try { return CultureInfo.GetCultureInfo(CultureEsEc); }
                catch
                {
                    try { return CultureInfo.GetCultureInfo(CultureEsEs); }
                    catch { return CultureInfo.InvariantCulture; }
                }
            }
        }

        // DTO interno para no recalcular mappings
        private sealed class MemberResolvedInfo
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
