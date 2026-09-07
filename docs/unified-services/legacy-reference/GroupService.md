# Referencia legacy — GroupService

## TODO UNIFIED: AddMemberAsync

EnsureAppUserAsync confirma Identity/AppUser antes de validar/guardar grupo. Resolver FacultyId externo a local para coordinador.

Referencia exacta: `tesisproject.backend/Services/Implementations/GroupService.cs:464-603`. Firma omitida temporalmente del contrato Unified.

```csharp
        public async Task<ServiceResult<GroupMemberResponseDTO>> AddMemberAsync(
            AddGroupMemberRequestDTO request,
            CancellationToken ct = default)
        {
            try
            {
                // 1) Validar grupo
                var group = await _uow.Groups.GetByIdAsync(request.GroupId, includeMembers: false, ct);
                if (group is null)
                    return ServiceResult<GroupMemberResponseDTO>.Fail(GroupNotFoundMessage, ErrorType.NotFound, ErrorCodes.Common.NotFound);

                if (request.MemberRole == 0)
                    return ServiceResult<GroupMemberResponseDTO>.Fail(MemberRoleRequiredMessage, ErrorType.Validation, ErrorCodes.Common.InvalidRequest);

                var email = (request.Email ?? string.Empty).Trim().ToLowerInvariant();
                if (string.IsNullOrWhiteSpace(email))
                    return ServiceResult<GroupMemberResponseDTO>.Fail(InstitutionalEmailRequiredMessage, ErrorType.Validation, ErrorCodes.Common.InvalidRequest);

                var document = (request.Document ?? string.Empty).Trim();
                if (string.IsNullOrWhiteSpace(document))
                    return ServiceResult<GroupMemberResponseDTO>.Fail(DocumentRequiredMessage, ErrorType.Validation, ErrorCodes.Common.InvalidRequest);



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

                // 3) Asegurar AppUser (Identity + AppUser)
                var ensureResult = await _appUsers.EnsureAppUserAsync(registerDto, ct);
                if (!ensureResult.Success)
                {
                    return ServiceResult<GroupMemberResponseDTO>.Fail(
                        ensureResult.Message ?? FailedToEnsureAppUserMessage,
                        ensureResult.Error);
                }

                var appUserPk = ensureResult.Data; // IdUser (PK de APP_USER)

                // 4) Obtener AppUser para resolver el IdUser a persistir en GroupMember.UserId (tabla puente)
                var appUser = await _uow.AppUsers.GetByIdAsync(new object[] { appUserPk }, ct);
                if (appUser is null)
                    return ServiceResult<GroupMemberResponseDTO>.Fail(UnableToResolveAspNetUserFromAppUserMessage, ErrorType.Unexpected, ErrorCodes.Common.UnexpectedError);

                var appUserIdUser = appUser.IdUser; // Este es el valor que se guarda en GroupMember.UserId

                // 5) Validar duplicado (GroupId + AppUser.IdUser)
                var duplicated = await _uow.GroupMembers.ExistsAsync(request.GroupId, appUserIdUser, ct);
                if (duplicated)
                    return ServiceResult<GroupMemberResponseDTO>.Fail(UserAlreadyMemberOfGroupMessage, ErrorType.Conflict, ErrorCodes.Common.PersistenceConflict);

                // 5.1) Regla: si agrega Coordinador Principal (1), actualizar facultad del proyecto
                if (request.MemberRole == MemberRoleTypeIds.Coordinador)
                {
                    // (A) Resolver FacultyId (prioridad: FacultyId directo)
                    int? facultyId = request.FacultyId;

                    // Si solo te mandan FacultyCareerId, aquí deberías traducir a FacultyId.
                    // Si aún no tienes esa tabla/repositorio, NO inventes: obliga FacultyId.
                    if (facultyId is null || facultyId <= 0)
                        return ServiceResult<GroupMemberResponseDTO>.Fail(
                            FacultyIdRequiredForCoordinatorPrincipalMessage,
                            ErrorType.Validation, ErrorCodes.Common.InvalidRequest);

                    // (B) Traer el proyecto asociado a este grupo (1 grupo -> 1 proyecto)
                    var project = await _uow.Projects
                        .Query(asNoTracking: false)
                        .FirstOrDefaultAsync(p => p.ProjectGroupId == request.GroupId, ct);

                    if (project is null)
                        return ServiceResult<GroupMemberResponseDTO>.Fail(
                            NoProjectAssociatedToGroupMessage,
                            ErrorType.NotFound, ErrorCodes.Common.NotFound);

                    // (C) Actualizar facultad del proyecto
                    project.FacultyId = facultyId.Value;
                    _uow.Projects.Update(project);

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

                return ServiceResult<GroupMemberResponseDTO>.Ok(dto, MemberAddedToGroupMessage);
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
```

## TODO UNIFIED: GetExternalUserByAspNetIdAsync

Consulta AspNetUsers/Identity no disponible en IUnifiedUnitOfWork; acordar frontera de lectura Identity.

Referencia exacta: `tesisproject.backend/Services/Implementations/GroupService.cs:295-339`. Firma omitida temporalmente del contrato Unified.

```csharp
        public async Task<ServiceResult<ResolvedUserProfileDTO>> GetExternalUserByAspNetIdAsync(int userId, CancellationToken ct = default)
        {
            try
            {
                var local = await _uow.AspNetUsers.GetEmailByUserIdAsync(userId, ct);
                if (local is null)
                    return ServiceResult<ResolvedUserProfileDTO>.Fail(AspNetUserNotFoundMessage, ErrorType.NotFound, ErrorCodes.Common.NotFound);

                var email = (local.Value.Email ?? string.Empty).Trim().ToLowerInvariant();
                if (string.IsNullOrWhiteSpace(email))
                    return ServiceResult<ResolvedUserProfileDTO>.Fail(UserHasNoInstitutionalEmailMessage, ErrorType.Validation, ErrorCodes.Common.InvalidRequest);

                var dirRes = await _directory.GetByEmailsAsync(new[] { email }, ct);
                var profile = dirRes.Data?.FirstOrDefault();
                if (profile is null)
                    return ServiceResult<ResolvedUserProfileDTO>.Fail(ExternalUserNotFoundForGivenEmailMessage, ErrorType.NotFound, ErrorCodes.Common.NotFound);

                var extLoad = await LoadExternalPeriodsAndDistributivosAsync(new[] { email }, ct);
                if (!extLoad.Success || extLoad.Data is null)
                    return ServiceResult<ResolvedUserProfileDTO>.Fail(
                        extLoad.Message ?? FailedLoadingExternalPeriodsDistributivosMessage,
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

                return ServiceResult<ResolvedUserProfileDTO>.Ok(dto, ExternalUserResolvedByAspNetUserIdMessage);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error resolving external user for ASP.NET user {UserId}", userId);
                return ServiceResult<ResolvedUserProfileDTO>.Fail(UnexpectedErrorMessage, ErrorType.Unexpected, ErrorCodes.Common.UnexpectedError);
            }
        }
```

## TODO UNIFIED: GetExternalUsersByGroupAsync

Consulta AspNetUsers/Identity no disponible en IUnifiedUnitOfWork; acordar frontera de lectura Identity.

Referencia exacta: `tesisproject.backend/Services/Implementations/GroupService.cs:187-293`. Firma omitida temporalmente del contrato Unified.

```csharp
        public async Task<ServiceResult<List<ResolvedUserProfileDTO>>> GetExternalUsersByGroupAsync(int groupId, CancellationToken ct = default)
        {
            try
            {
                var exists = await _uow.Groups.ExistsAsync(g => g.GroupId == groupId, ct);
                if (!exists)
                    return ServiceResult<List<ResolvedUserProfileDTO>>.Fail(GroupNotFoundMessage, ErrorType.NotFound, ErrorCodes.Common.NotFound);

                // 1) Miembros del grupo (tabla puente)
                var members = await _uow.GroupMembers.GetMembersByGroupAsync(groupId, ct: ct);
                if (members is null || members.Count == 0)
                    return ServiceResult<List<ResolvedUserProfileDTO>>.Fail(NoGroupMembersFoundMessage, ErrorType.NotFound, ErrorCodes.Common.NotFound);

                // Nota: en tu caso member.UserId = AppUser.IdUser (PK de APP_USER)
                var appUserIds = members.Select(m => m.UserId).Distinct().ToList();
                if (appUserIds.Count == 0)
                    return ServiceResult<List<ResolvedUserProfileDTO>>.Fail(NoAssociatedAppUsersFoundMessage, ErrorType.Validation, ErrorCodes.Common.InvalidRequest);

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
                    return ServiceResult<List<ResolvedUserProfileDTO>>.Fail(NoLocalIdentityIdsForGroupMembersMessage, ErrorType.Validation, ErrorCodes.Common.InvalidRequest);

                var identityUserIds = identityIdByAppUserId.Values.Distinct().ToList();

                // 3) Emails institucionales (por IdentityUser.Id / IdLocal)
                var emailByIdentityId = await _uow.AspNetUsers.GetEmailsByUserIdsAsync(identityUserIds, ct);

                var allEmails = emailByIdentityId.Values
                    .Where(e => !string.IsNullOrWhiteSpace(e))
                    .Select(e => e!.Trim().ToLowerInvariant())
                    .Distinct()
                    .ToList();

                if (allEmails.Count == 0)
                    return ServiceResult<List<ResolvedUserProfileDTO>>.Fail(NoValidEmailsFoundForUsersMessage, ErrorType.Validation, ErrorCodes.Common.InvalidRequest);

                // 4) Rehidratar roles si faltan (evitar N+1) - misma lógica
                await EnsureMemberRolesLoadedAsync(members, ct);

                // 5) Directorio externo en batch
                var dirRes = await _directory.GetByEmailsAsync(allEmails, ct);
                if (!dirRes.Success || dirRes.Data is null || dirRes.Data.Count == 0)
                    return ServiceResult<List<ResolvedUserProfileDTO>>.Fail(NoExternalUsersFoundMessage);

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
                        extLoad.Message ?? FailedLoadingExternalDataMessage,
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
                    return ServiceResult<List<ResolvedUserProfileDTO>>.Fail(NoExternalUsersMatchedGroupMembersMessage, ErrorType.NotFound, ErrorCodes.Common.NotFound);

                return ServiceResult<List<ResolvedUserProfileDTO>>.Ok(result, ExternalUsersByGroupRetrievedMessage);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error retrieving external users for group {GroupId}", groupId);
                return ServiceResult<List<ResolvedUserProfileDTO>>.Fail(UnexpectedErrorMessage, ErrorType.Unexpected, ErrorCodes.Common.UnexpectedError);
            }
        }
```

## TODO UNIFIED: GetProjectMembersReportAsync

Consulta AspNetUsers/Identity no disponible en IUnifiedUnitOfWork; acordar frontera de lectura Identity.

Referencia exacta: `tesisproject.backend/Services/Implementations/GroupService.cs:850-1241`. Firma omitida temporalmente del contrato Unified.

```csharp
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
                    return ServiceResult<ProjectMembersReportDTO>.Fail(ProjectNotFoundMessage, ErrorType.NotFound, ErrorCodes.Common.NotFound);

                if (projectInfo.GroupId <= 0)
                    return ServiceResult<ProjectMembersReportDTO>.Fail(
                        ProjectNotFoundOrNoAssociatedGroupMessage,
                        ErrorType.NotFound, ErrorCodes.Common.NotFound);

                if (projectInfo.StartDate is null)
                    return ServiceResult<ProjectMembersReportDTO>.Fail(
                        ProjectStartDateRequiredForReportMessage,
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
                        NoGroupMembersFoundForProjectMessage,
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
                    var rolesById = await _uow.MemberRoleTypeRepository.GetByIdsAsync(roleIds, include: null, ct: ct);
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
                        NoAssociatedAppUsersForProjectMembersMessage,
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
                        NoLocalIdentityIdsForProjectMembersMessage,
                        ErrorType.Validation, ErrorCodes.Common.InvalidRequest);

                // =========================
                // 6) IdentityUser.Id -> Email (batch)
                // =========================
                var emailByIdentityId = await _uow.AspNetUsers.GetEmailsByUserIdsAsync(identityIds.ToList(), ct);

                var emailsDistinct = emailByIdentityId.Values
                    .Where(e => !string.IsNullOrWhiteSpace(e))
                    .Select(e => e!.Trim().ToLowerInvariant())
                    .Distinct()
                    .ToList();

                if (emailsDistinct.Count == 0)
                    return ServiceResult<ProjectMembersReportDTO>.Fail(
                        NoInstitutionalEmailsForProjectMembersMessage,
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
                        extLoad.Message ?? FailedLoadingExternalPeriodsDistributivosMessage,
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

                return ServiceResult<ProjectMembersReportDTO>.Ok(dto, ProjectMembersReportGeneratedMessage);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error generating project members report for project {ProjectId}", projectId);
                return ServiceResult<ProjectMembersReportDTO>.Fail(UnexpectedErrorMessage, ErrorType.Unexpected, ErrorCodes.Common.UnexpectedError);
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
```
