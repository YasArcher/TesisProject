# Referencia legacy — ProjectService

## TODO UNIFIED: CreateAsync

Resolver ExternalFacultyId/ExternalPeriodId a FacultyId/AcademicTermId locales y separar EnsureAppUsersAsync de Identity runtime. No asumir igualdad de IDs; evitar commits internos previos al commit del proyecto.

Referencia exacta: `tesisproject.backend/Services/Implementations/ProjectService.cs:217-279`. Firma omitida temporalmente del contrato Unified.

```csharp
        public async Task<ServiceResult<ProjectListResponseDTO>> CreateAsync(
            AddProjectRequestDTO dto,
            CancellationToken ct = default)
        {
            try
            {
                var duplicate = await _uow.Projects.Query()
                    .AnyAsync(p => p.ProjectName == dto.ProjectName && p.ProjectGroupId == dto.ProjectGroupId, ct);

                if (duplicate)
                {
                    return ServiceResult<ProjectListResponseDTO>.Fail(
                        ErrorMessages.Project.NameAlreadyExistsInGroup,
                        ErrorType.Conflict,
                        ErrorCodes.Project.NameAlreadyExistsInGroup);
                }

                var actorUserId = await GetExistingActorUserIdAsync(ct);
                if (!actorUserId.HasValue)
                {
                    return FailActorUserNotFound<ProjectListResponseDTO>();
                }

                var createdByUserId = actorUserId.Value;

                var entity = MapToEntity(dto, createdByUserId);
                await _uow.Projects.AddAsync(entity, ct);
                await _uow.SaveChangesAsync(ct);

                var created = await _uow.Projects
                    .Query()
                    .Where(p => p.ProjectId == entity.ProjectId)
                    .Select(p => new ProjectListResponseDTO
                    {
                        ProjectId = p.ProjectId,
                        ProjectCode = p.ProjectCode,
                        ProjectName = p.ProjectName,
                        ProjectStateName = p.ProjectState.Name,
                        ProjectTypeName = p.ProjectType.Name,
                        ProjectGroupName = p.ProjectGroup.Name,
                        StartDate = p.StartDate,
                        TentativeEndDate = p.TentativeEndDate,
                        ExecutionPercentage = p.ExecutionPercentage,
                        PrincipalCoordinatorFacultyId = p.FacultyId,
                        ConvocationId = p.ConvocationId ?? 0
                    })
                    .FirstAsync(ct);

                return ServiceResult<ProjectListResponseDTO>.Ok(created, ProjectCreatedMessage);
            }
            catch (UnauthorizedAccessException)
            {
                return FailUnauthorized<ProjectListResponseDTO>();
            }
            catch (DbUpdateException dbex)
            {
                return FailConflict<ProjectListResponseDTO>(dbex.InnerException?.Message ?? dbex.Message);
            }
            catch (Exception ex)
            {
                return FailUnexpected<ProjectListResponseDTO>(ex.Message);
            }
        }
```

## TODO UNIFIED: CreateFullAsync

Resolver ExternalFacultyId/ExternalPeriodId a FacultyId/AcademicTermId locales y separar EnsureAppUsersAsync de Identity runtime. No asumir igualdad de IDs; evitar commits internos previos al commit del proyecto.

Referencia exacta: `tesisproject.backend/Services/Implementations/ProjectService.cs:414-898`. Firma omitida temporalmente del contrato Unified.

```csharp
        public async Task<ServiceResult<ProjectDetailResponseDTO>> CreateFullAsync(
            AddProjectFullRequestDTO request,
            CancellationToken ct = default)
        {
            void PhaseLog(string phase, string message)
                => Console.WriteLine($"[DEBUG] [{phase}] {message}");

            try
            {
                if (request.Project is null)
                {
                    PhaseLog("Fase 0 - Request", "Project payload is null");
                    return FailValidation<ProjectDetailResponseDTO>(
                        ErrorMessages.Project.InvalidProjectData,
                        ErrorCodes.Project.InvalidProjectData);
                }

                var p = request.Project;

                if (request.GroupMembers is null || request.GroupMembers.Count == 0)
                {
                    PhaseLog("Fase 0 - Request", "GroupMembers is null or empty");
                    return FailValidation<ProjectDetailResponseDTO>(
                        ErrorMessages.Project.AtLeastOneGroupMemberRequired,
                        ErrorCodes.Project.AtLeastOneGroupMemberRequired);
                }

                var principalCoordinatorEmail = request.GroupMembers
                    .FirstOrDefault(m => m.MemberRole == MemberRoleTypeIds.Coordinador)
                    ?.Email;

                if (string.IsNullOrWhiteSpace(principalCoordinatorEmail))
                {
                    PhaseLog("Fase 0 - Request", "Principal coordinator email not found");
                    return FailValidation<ProjectDetailResponseDTO>(
                        ErrorMessages.Project.PrincipalCoordinatorRequired,
                        ErrorCodes.Project.PrincipalCoordinatorRequired);
                }

                var d = request.ProjectDocumentData;
                var projectStartDate = p.StartDate;

                var profRes = await _externalDirectory.GetByEmailsAsync(new[] { principalCoordinatorEmail.Trim() }, ct);
                if (!profRes.Success || profRes.Data is null || profRes.Data.Count == 0)
                {
                    PhaseLog("Fase 1.2 - External Faculty Resolve",
                        $"External profile not found for email={principalCoordinatorEmail}. Error={profRes.Error}, Msg={profRes.Message}");

                    return FailNotFound<ProjectDetailResponseDTO>(
                        ErrorMessages.Project.PrincipalCoordinatorExternalProfileNotFound,
                        ErrorCodes.Project.PrincipalCoordinatorExternalProfileNotFound);
                }

                var profile = profRes.Data[0];

                var periodsRes = await _externalPeriods.GetAllAsync(ct);
                if (!periodsRes.Success || periodsRes.Data is null || periodsRes.Data.Count == 0)
                {
                    PhaseLog("Fase 1.2 - External Faculty Resolve",
                        $"External periods not available. Error={periodsRes.Error}, Msg={periodsRes.Message}");

                    return FailUnexpected<ProjectDetailResponseDTO>(
                        ErrorMessages.Project.ExternalAcademicPeriodsNotAvailable,
                        ErrorCodes.Project.ExternalAcademicPeriodsNotAvailable);
                }

                var periods = periodsRes.Data;

                var distRawRes = await _externalDistributivosRaw.GetDistributivosByCorreosAsync(
                    new[] { principalCoordinatorEmail.Trim() },
                    ct);

                if (!distRawRes.Success || distRawRes.Data is null || distRawRes.Data.Count == 0)
                {
                    PhaseLog("Fase 1.2 - External Faculty Resolve",
                        $"No distributivos found for email={principalCoordinatorEmail}. Error={distRawRes.Error}, Msg={distRawRes.Message}");

                    return FailNotFound<ProjectDetailResponseDTO>(
                        ErrorMessages.Project.NoDistributivoForPrincipalCoordinator,
                        ErrorCodes.Project.NoDistributivoForPrincipalCoordinator);
                }

                var distributivos = distRawRes.Data;

                if (projectStartDate is null)
                {
                    PhaseLog("Fase 1.2 - External Faculty Resolve", "Project StartDate is null");
                    return FailValidation<ProjectDetailResponseDTO>(
                        ErrorMessages.Project.StartDateRequired,
                        ErrorCodes.Project.StartDateRequired);
                }

                var selected = ExternalCareerSelector.SelectProjectCareer(
                    profile,
                    distributivos,
                    projectStartDate.Value,
                    periods,
                    onlyActivePreferred: true);

                if (selected is null)
                {
                    PhaseLog("Fase 1.2 - External Faculty Resolve",
                        $"SelectProjectCareer returned null for email={principalCoordinatorEmail}, start={projectStartDate:O}");

                    return FailValidation<ProjectDetailResponseDTO>(
                        ErrorMessages.Project.FacultyCareerResolutionFailed,
                        ErrorCodes.Project.FacultyCareerResolutionFailed);
                }

                var resolvedFacultyId = selected.FacultyId ?? selected.FacultyCareerId;

                PhaseLog("Fase 1.2 - External Faculty Resolve",
                    $"Resolved FacultyId={resolvedFacultyId} from selected FacultyCareerId={selected.FacultyCareerId}, FacultyId(parent)={selected.FacultyId}");

                p.FacultyId = resolvedFacultyId;

                if (p.FacultyId <= 0)
                {
                    PhaseLog("Fase 1.3 - Validación", "Resolved FacultyId <= 0");
                    return FailValidation<ProjectDetailResponseDTO>(
                        ErrorMessages.Project.InvalidFacultyId,
                        ErrorCodes.Project.InvalidFacultyId);
                }

                if (p.ProjectTypeId <= 0)
                {
                    PhaseLog("Fase 1 - Validación", "ProjectTypeId <= 0");
                    return FailValidation<ProjectDetailResponseDTO>(
                        ErrorMessages.Project.InvalidProjectTypeId,
                        ErrorCodes.Project.InvalidProjectTypeId);
                }

                if (p.ProjectStateId <= 0)
                {
                    PhaseLog("Fase 1 - Validación", "ProjectStateId <= 0");
                    return FailValidation<ProjectDetailResponseDTO>(
                        ErrorMessages.Project.InvalidProjectStateId,
                        ErrorCodes.Project.InvalidProjectStateId);
                }

                var actorUserId = await GetExistingActorUserIdAsync(ct);
                if (!actorUserId.HasValue)
                {
                    PhaseLog("Fase 1 - Validación", "CreatedByUserId unresolved from current authenticated user.");
                    return FailActorUserNotFound<ProjectDetailResponseDTO>();
                }

                var createdByUserId = actorUserId.Value;

                if (p.FacultyId <= 0)
                {
                    PhaseLog("Fase 1 - Validación", "FacultyId <= 0");
                    return FailValidation<ProjectDetailResponseDTO>(
                        ErrorMessages.Project.InvalidFacultyId,
                        ErrorCodes.Project.InvalidFacultyId);
                }

                PhaseLog("Fase 1.5 - Código", "Generando código de proyecto...");

                var facultyCode = (p.ProjectCode ?? string.Empty).Trim();

                if (string.IsNullOrWhiteSpace(facultyCode))
                {
                    PhaseLog("Fase 1.5 - Código", "facultyCode vacío");
                    return FailValidation<ProjectDetailResponseDTO>(
                        ErrorMessages.Project.FacultyProjectCodeRequired,
                        ErrorCodes.Project.FacultyProjectCodeRequired);
                }

                var lastNumber = await _uow.Projects
                    .Query(asNoTracking: true)
                    .Where(x => x.FacultyId == p.FacultyId &&
                                x.ProjectCode.StartsWith(facultyCode))
                    .OrderByDescending(x => x.ProjectNumber)
                    .Select(x => (int?)x.ProjectNumber)
                    .FirstOrDefaultAsync(ct) ?? 0;

                var nextNumber = lastNumber + 1;
                var generatedCode = $"{facultyCode}{nextNumber}-A";

                PhaseLog("Fase 1.5 - Código",
                    $"facultyCode={facultyCode}, last={lastNumber}, next={nextNumber}, final={generatedCode}");

                if (generatedCode.Length > 20)
                {
                    PhaseLog("Fase 1.5 - Código", $"generatedCode demasiado largo: {generatedCode.Length}");
                    return FailValidation<ProjectDetailResponseDTO>(
                        ErrorMessages.Project.GeneratedProjectCodeTooLong,
                        ErrorCodes.Project.GeneratedProjectCodeTooLong);
                }

                PhaseLog("Fase 2 - Group", "Creando grupo...");

                var groupEntity = new Group
                {
                    GroupTypeId = GroupTypeIds.Integrantes,
                    Name = generatedCode
                };

                await _uow.Groups.AddAsync(groupEntity, ct);

                PhaseLog("Fase 3 - Validación nombre", "Revisando duplicados...");

                var duplicate = await _uow.Projects
                    .Query(asNoTracking: true)
                    .AnyAsync(x => x.ProjectName == p.ProjectName, ct);

                if (duplicate)
                {
                    PhaseLog("Fase 3 - Validación nombre", "Duplicado detectado");
                    return ServiceResult<ProjectDetailResponseDTO>.Fail(
                        ErrorMessages.Project.NameAlreadyExists,
                        ErrorType.Conflict,
                        ErrorCodes.Project.NameAlreadyExists);
                }

                PhaseLog("Fase 4 - Project",
                    $"Creando entidad proyecto con: generatedCode={generatedCode}, nextNumber={nextNumber}");

                var projectEntity = new Project
                {
                    ProjectCode = generatedCode,
                    CreatedByUserId = createdByUserId,
                    ProjectTypeId = p.ProjectTypeId,
                    ProjectNumber = nextNumber,
                    ProjectStateId = ProjectStateIds.EnEjecucion,
                    ProjectName = p.ProjectName ?? string.Empty,
                    ApprovalDate = p.ApprovalDate,
                    StartDate = p.StartDate,
                    DurationInMonths = p.DurationInMonths,
                    TentativeEndDate = p.StartDate?.AddMonths(p.DurationInMonths),
                    RealEndDate = null,
                    ProjectOriginTypeId = p.ProjectOriginTypeId,
                    ExecutionPercentage = 0,
                    FacultyId = p.FacultyId,
                    ConvocationId = p.ConvocationId
                };

                projectEntity.ProjectGroup = groupEntity;

                await _uow.Projects.AddAsync(projectEntity, ct);

                if (d is not null && d.DocumentId > 0)
                {
                    PhaseLog("Fase 4.5 - ProjectDocument",
                        $"Vinculando DocumentId={d.DocumentId} al proyecto {generatedCode}...");

                    var projectDocument = new ProjectDocument
                    {
                        Project = projectEntity,
                        DocumentId = d.DocumentId,
                    };

                    await _uow.ProjectDocuments.AddAsync(projectDocument, ct);
                }
                else
                {
                    PhaseLog("Fase 4.5 - ProjectDocument", "No se recibió DocumentId para vincular.");
                }

                PhaseLog("Fase 5 - Miembros", "Asegurando AppUsers e insertando miembros...");

                if (request.GroupMembers is not null && request.GroupMembers.Count > 0)
                {
                    PhaseLog("Fase 5 - Miembros", $"Total GroupMembers en request: {request.GroupMembers.Count}");

                    for (int i = 0; i < request.GroupMembers.Count; i++)
                    {
                        var m = request.GroupMembers[i];
                        PhaseLog("Fase 5 - Miembros",
                            $"Member[{i}]: Email={m.Email}, AspUserId={m.AspUserId}, MemberRole={m.MemberRole}");
                    }

                    var registerDtos = request.GroupMembers
                        .Select(m => new RegisterRequest
                        {
                            Email = m.Email,
                            Username = m.Document,
                            Password = DefaultHardcodedPassword,
                            AspUserId = m.AspUserId,
                            Role = ResolveAppRoleFromMemberRole(m.MemberRole)
                        })
                        .ToList();

                    PhaseLog("Fase 5 - Miembros",
                        $"RegisterRequest count: {registerDtos.Count}, first email: {registerDtos.First().Email}");

                    var ensureResult = await _appUsers.EnsureAppUsersAsync(registerDtos, ct);

                    PhaseLog("Fase 5 - Miembros",
                        $"EnsureAppUsersAsync => Success={ensureResult.Success}, " +
                        $"Error={ensureResult.Error}, DataCount={(ensureResult.Data?.Count ?? 0)}");

                    if (!ensureResult.Success)
                    {
                        PhaseLog("Fase 5 - Miembros", $"Error asegurando AppUsers: {ensureResult.Error}");

                        return RelayFailure<ProjectDetailResponseDTO>(
                            ensureResult.Message,
                            ensureResult.Error,
                            ensureResult.ErrorCode,
                            ensureResult.ValidationErrors);
                    }

                    if (ensureResult.Data is null)
                    {
                        PhaseLog("Fase 5 - Miembros", "EnsureAppUsersAsync returned Success=true but Data=null.");

                        return FailUnexpected<ProjectDetailResponseDTO>(
                            ErrorMessages.Project.ErrorEnsuringAppUsers,
                            ErrorCodes.Project.ErrorEnsuringAppUsers);
                    }

                    var appUserIds = ensureResult.Data;

                    if (appUserIds.Count != request.GroupMembers.Count)
                    {
                        PhaseLog("Fase 5 - Miembros",
                            $"Cantidad de AppUserIds ({appUserIds.Count}) != GroupMembers ({request.GroupMembers.Count})");

                        return FailUnexpected<ProjectDetailResponseDTO>(
                            ErrorMessages.Project.InconsistentAppUserMapping,
                            ErrorCodes.Project.InconsistentAppUserMapping);
                    }

                    var groupMembers = new List<GroupMember>();

                    for (int i = 0; i < request.GroupMembers.Count; i++)
                    {
                        var memberDto = request.GroupMembers[i];
                        var appUserId = appUserIds[i];

                        PhaseLog("Fase 5 - Miembros",
                            $"Creando GroupMember[{i}]: AppUserId={appUserId}, MemberRoleId={memberDto.MemberRole}");

                        groupMembers.Add(new GroupMember
                        {
                            Group = groupEntity,
                            UserId = appUserId,
                            MemberRoleId = memberDto.MemberRole,
                            JoinedAt = DateTime.UtcNow
                        });
                    }

                    PhaseLog("Fase 5 - Miembros", $"Insertando {groupMembers.Count} GroupMembers...");
                    await _uow.GroupMembers.AddRangeAsync(groupMembers, ct);
                }
                else
                {
                    PhaseLog("Fase 5 - Miembros", "No hay GroupMembers en el request.");
                }

                PhaseLog("Fase 6 - Categorías", "Insertando categorías...");

                if (p.ResearchCategoryIds is not null && p.ResearchCategoryIds.Count > 0)
                {
                    var distinctIds = p.ResearchCategoryIds.Distinct().ToList();

                    var categoryLinks = distinctIds
                        .Select(catId => new ProjectResearchCategory
                        {
                            Project = projectEntity,
                            ResearchCategoryId = catId
                        })
                        .ToList();

                    await _uow.ProjectResearchCategories.AddRangeAsync(categoryLinks, ct);
                }

                PhaseLog("Fase 7 - Presupuesto", "Insertando budgets...");

                if (request.Budgets is not null)
                {
                    var budgetEntities = request.Budgets
                        .Where(b => b.FundingTypeId > 0 && b.InitialAmount > 0)
                        .Select(b => new Budget
                        {
                            Project = projectEntity,
                            ApprovedByUserId = createdByUserId,
                            InitialAmount = b.InitialAmount,
                            CertifiedAmount = 0,
                            ExecutedAmount = 0,
                            ApprovedAt = DateTime.Now,
                            FundingTypeId = b.FundingTypeId
                        })
                        .ToList();

                    await _uow.Budgets.AddRangeAsync(budgetEntities, ct);
                }

                PhaseLog("Fase 8 - Objetivos", "Insertando objetivos...");

                if (request.Objectives is not null)
                {
                    foreach (var objDto in request.Objectives)
                    {
                        var objectiveEntity = new ProjectObjective
                        {
                            Project = projectEntity,
                            ObjectiveTypeId = objDto.ObjectiveTypeId,
                            Objective = objDto.Objective,
                            Result = objDto.Result,
                            WeightedPercentage = objDto.WeightedPercentage,
                        };

                        await _uow.ProjectObjectives.AddAsync(objectiveEntity, ct);

                        if (objDto.Activities is not null)
                        {
                            foreach (var actDto in objDto.Activities)
                            {
                                var activityEntity = new ObjectiveActivity
                                {
                                    Objective = objectiveEntity,
                                    ActivityResult = actDto.ActivityResult ?? string.Empty,
                                    ActionText = actDto.ActionText ?? string.Empty,
                                    CreatedAt = DateTime.UtcNow
                                };

                                await _uow.ObjectiveActivities.AddAsync(activityEntity, ct);
                            }
                        }
                    }
                }

                PhaseLog("Fase 8.5 - ExternalResearchers", "Insertando investigadores externos...");

                if (request.ExternalResearcherIds is not null && request.ExternalResearcherIds.Count > 0)
                {
                    var distinctExternalIds = request.ExternalResearcherIds
                        .Where(id => id > 0)
                        .Distinct()
                        .ToList();

                    if (distinctExternalIds.Count > 0)
                    {
                        var externalLinks = new List<ExternalResearcherProject>();

                        foreach (var externalId in distinctExternalIds)
                        {
                            externalLinks.Add(new ExternalResearcherProject
                            {
                                ExternalResearcherId = externalId,
                                Project = projectEntity,
                                Role = ExternalResearcherRole,
                                CreatedAtUtc = DateTime.UtcNow,
                                CreatedByUserId = createdByUserId,
                                ExitDate = null
                            });
                        }

                        PhaseLog("Fase 8.5 - ExternalResearchers",
                            $"Insertando {externalLinks.Count} vínculos ExternalResearcherProject...");

                        await _uow.ExternalResearcherProjects.AddRangeAsync(externalLinks, ct);
                    }
                }
                else
                {
                    PhaseLog("Fase 8.5 - ExternalResearchers", "No hay investigadores externos en el request.");
                }

                PhaseLog("Fase 10 - Save", "Guardando UoW...");
                await _uow.SaveChangesAsync(ct);

                PhaseLog("Fase 11 - Detalle", "Consultando detalle...");

                var detail = await GetProjectDetailAsync(projectEntity.ProjectId, ct);
                if (!detail.Success)
                {
                    return detail;
                }

                return ServiceResult<ProjectDetailResponseDTO>.Ok(detail.Data!);
            }
            catch (UnauthorizedAccessException)
            {
                return FailUnauthorized<ProjectDetailResponseDTO>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DEBUG] EXCEPCIÓN → {ex}");
                return FailUnexpected<ProjectDetailResponseDTO>(ErrorMessages.Common.UnexpectedError);
            }
        }
```

## TODO UNIFIED: ImportFromMatrixAsync

Resolver ExternalFacultyId/ExternalPeriodId a FacultyId/AcademicTermId locales y separar EnsureAppUsersAsync de Identity runtime. No asumir igualdad de IDs; evitar commits internos previos al commit del proyecto.

Referencia exacta: `tesisproject.backend/Services/Implementations/ProjectService.cs:1150-1755`. Firma omitida temporalmente del contrato Unified.

```csharp
        public async Task<ServiceResult<int>> ImportFromMatrixAsync(
            ProjectMatrixUploadSummaryDTO summary,
            CancellationToken ct = default)
        {
            void PhaseLog(string phase, string message)
            {
                var formatted = $"[IMPORT] [{phase}] {message}";
                Console.WriteLine(formatted);
                _logger.LogInformation(formatted);
            }

            try
            {
                PhaseLog("Init", "Starting ImportFromMatrixAsync...");

                var periodsResult = await _externalPeriods.GetAllAsync(ct);
                if (!periodsResult.Success || periodsResult.Data is null || periodsResult.Data.Count == 0)
                {
                    PhaseLog("Init-Periods", "Cannot retrieve academic periods from external API.");
                }

                var academicPeriodsCache = periodsResult.Data?.ToList() ?? new List<ExternalAcademicPeriodModel>();
                PhaseLog("Init", $"AcademicPeriods loaded (external): {academicPeriodsCache.Count}");

                var categoriesResult = await _researchCategoryService.ListAsync(
                    onlyActives: true,
                    ct: ct);

                if (!categoriesResult.Success || categoriesResult.Data is null)
                {
                    PhaseLog("Init-Categories",
                        $"No se pudieron cargar categorías de investigación: {categoriesResult.Error}");
                }

                var allCategories = categoriesResult.Data;

                var documentTypes = await _uow.DocumentTypes
                    .Query(asNoTracking: true)
                    .ToListAsync(ct);
                PhaseLog("Init", $"DocumentTypes loaded: {documentTypes.Count}");

                var convocationsCache = await _uow.Convocations
                    .Query(asNoTracking: false)
                    .ToListAsync(ct);
                PhaseLog("Init", $"Convocations loaded: {convocationsCache.Count}");

                var facultiesResult = await _externalAcademics.GetFacultiesAsync(ct);
                if (!facultiesResult.Success || facultiesResult.Data is null || facultiesResult.Data.Count == 0)
                {
                    PhaseLog("Init-Faculties", "Cannot retrieve faculties from external API.");
                    return FailUnexpected<int>(
                        ErrorMessages.Project.CannotRetrieveFaculties,
                        ErrorCodes.Project.CannotRetrieveFaculties);
                }

                var externalFacultiesCache = facultiesResult.Data;
                PhaseLog("Init-Faculties", $"External faculties loaded: {externalFacultiesCache.Count}");

                var projectStatesCache = await _uow.ProjectStates
                    .Query(asNoTracking: true)
                    .ToListAsync(ct);
                PhaseLog("Init", $"ProjectStates loaded: {projectStatesCache.Count}");

                if (summary.ImportedProjects is null || summary.ImportedProjects.Count == 0)
                {
                    PhaseLog("Init", "Summary.ImportedProjects is null or empty.");
                    return FailValidation<int>(
                        ErrorMessages.Project.NoImportedProjectsFound,
                        ErrorCodes.Project.NoImportedProjectsFound);
                }

                PhaseLog("Init", $"ImportedProjects in summary: {summary.ImportedProjects.Count}");

                var actorUserId = await GetExistingActorUserIdAsync(ct);
                if (!actorUserId.HasValue)
                {
                    PhaseLog("Init", "AppUser resolve failed for current authenticated user.");
                    return FailActorUserNotFound<int>();
                }

                var createdByUserId = actorUserId.Value;

                PhaseLog("Init", $"Import executed by AppUserId={createdByUserId}");

                var directoryResult = await _externalDirectory.GetAllAsync(ct);
                if (!directoryResult.Success || directoryResult.Data is null || directoryResult.Data.Count == 0)
                {
                    return FailUnexpected<int>(
                        ErrorMessages.Project.CannotRetrieveExternalDirectory,
                        ErrorCodes.Project.CannotRetrieveExternalDirectory);
                }

                var directoryCache = directoryResult.Data;

                int createdCount = 0;
                int skippedCount = 0;

                foreach (var dto in summary.ImportedProjects)
                {
                    ct.ThrowIfCancellationRequested();

                    if (dto is null)
                    {
                        skippedCount++;
                        continue;
                    }

                    if (string.IsNullOrWhiteSpace(dto.ProjectCode))
                    {
                        PhaseLog("Row", "Skipping project: ProjectCode is null/empty.");
                        skippedCount++;
                        continue;
                    }

                    if (!dto.Number.HasValue || dto.Number.Value <= 0)
                    {
                        PhaseLog("Row", $"Skipping project {dto.ProjectCode}: invalid Number.");
                        skippedCount++;
                        continue;
                    }

                    int? facultyId = null;
                    if (!string.IsNullOrWhiteSpace(dto.Faculty))
                    {
                        facultyId = ResolveFacultyExternalId(externalFacultiesCache, dto.Faculty!);
                        PhaseLog("Row", $"Resolved Faculty for {dto.ProjectCode}: {dto.Faculty} → {facultyId}");
                    }

                    int convocationId;
                    Convocation? convocationEntity = null;
                    var rawCallCode = dto.CallCode;

                    if (string.IsNullOrWhiteSpace(rawCallCode))
                    {
                        convocationId = DEFAULT_CONVOCATION_ID;

                        PhaseLog("Row",
                            $"CallCode empty → Assigning fixed ConvocationId={convocationId} to project {dto.ProjectCode}");
                    }
                    else
                    {
                        convocationEntity = await ResolveOrCreateConvocationAsync(
                            convocationsCache,
                            rawCallCode,
                            ct);

                        if (convocationEntity is null)
                        {
                            convocationId = DEFAULT_CONVOCATION_ID;

                            PhaseLog("Row",
                                $"CallCode '{rawCallCode}' could not be matched → Using fixed ConvocationId={convocationId} for project {dto.ProjectCode}");
                        }
                        else
                        {
                            convocationId = convocationEntity.Id;

                            PhaseLog("Row",
                                $"Resolved Convocation for {dto.ProjectCode}: CallCode='{rawCallCode}' → ConvocationId={convocationId}");
                        }
                    }

                    int? projectStateId = null;
                    if (!string.IsNullOrWhiteSpace(dto.State))
                    {
                        projectStateId = ResolveProjectStateId(projectStatesCache, dto.State!);
                        PhaseLog("Row",
                            $"Resolved ProjectState for {dto.ProjectCode}: State={dto.State} → ProjectStateId={projectStateId}");
                    }

                    if (dto.StartDate.HasValue && dto.StartDate.Value.Date > DateTime.UtcNow.Date)
                    {
                        projectStateId = ProjectStateIds.EnEjecucion;
                        PhaseLog("Row",
                            $"StartDate is in the future → Forcing ProjectStateId=6 for {dto.ProjectCode} (StartDate={dto.StartDate:yyyy-MM-dd}).");
                    }

                    var exists = await _uow.Projects
                        .Query(asNoTracking: true)
                        .AnyAsync(x =>
                            x.ProjectCode == dto.ProjectCode &&
                            x.ProjectNumber == dto.Number.Value,
                            ct);

                    if (exists)
                    {
                        PhaseLog("Row",
                            $"Project {dto.ProjectCode} (#{dto.Number}) already exists. Generating unique code...");

                        dto.ProjectCode = await GenerateUniqueProjectCodeAsync(dto.ProjectCode!, ct);

                        PhaseLog("Row", $" → New code: {dto.ProjectCode}");
                    }

                    var groupEntity = new Group
                    {
                        GroupTypeId = GroupTypeIds.Integrantes,
                        Name = dto.ProjectCode!
                    };

                    await _uow.Groups.AddAsync(groupEntity, ct);
                    PhaseLog("Row",
                        $"Group queued for insert: Name={groupEntity.Name}");

                    await InsertGroupMembersFromDirectoryAsync(groupEntity, dto, directoryCache, ct);

                    PhaseLog("Row",
                        $"Creating project: Code={dto.ProjectCode}, Number={dto.Number}, Name={dto.ProjectName}");

                    var durationMonths = dto.TermMonths ?? 0;

                    var approvalDoc = dto.Documents
                        .FirstOrDefault(d => d.DocumentType == "APROBACION HCU/CONIN");

                    DateTime? approvalDate = approvalDoc?.Date;

                    var finalResolutionDoc = dto.Documents
                        .FirstOrDefault(d => d.DocumentType == "RESOLUCION INFORME FINAL HCU");

                    DateTime? realEndDate = finalResolutionDoc?.Date;

                    DateTime? tentativeEndDate = dto.EstimatedEndDate;

                    if (!tentativeEndDate.HasValue && dto.StartDate.HasValue && durationMonths > 0)
                    {
                        tentativeEndDate = dto.StartDate.Value.AddMonths(durationMonths);
                    }

                    var originTypeId = ((dto.ProjectCode ?? string.Empty).Trim()
                            .StartsWith("PE", StringComparison.OrdinalIgnoreCase))
                        ? ProjectOriginTypeIds.Externo
                        : ProjectOriginTypeIds.Interno;

                    if (!facultyId.HasValue || facultyId.Value <= 0)
                    {
                        PhaseLog("Row", $"Skipping {dto.ProjectCode}: FacultyId unresolved.");
                        skippedCount++;
                        continue;
                    }

                    if (!projectStateId.HasValue || projectStateId.Value <= 0)
                    {
                        PhaseLog("Row", $"Skipping {dto.ProjectCode}: ProjectStateId unresolved.");
                        skippedCount++;
                        continue;
                    }

                    var executionPct = (dto.ExecutionProgress ?? 0m) * 100m;

                    var projectEntity = new Project
                    {
                        ProjectCode = $"{dto.ProjectCode}-{dto.Number.Value}",
                        ProjectNumber = dto.Number.Value,
                        ProjectName = dto.ProjectName ?? string.Empty,
                        CreatedByUserId = createdByUserId,
                        ProjectTypeId = ProjectTypeIds.Aplicada,
                        FacultyId = facultyId.Value,
                        ConvocationId = convocationId,
                        ProjectStateId = projectStateId.Value,
                        ProjectOriginTypeId = originTypeId,
                        ApprovalDate = approvalDate,
                        StartDate = dto.StartDate,
                        DurationInMonths = durationMonths,
                        TentativeEndDate = tentativeEndDate,
                        RealEndDate = realEndDate,
                        ExecutionPercentage = executionPct,
                    };

                    projectEntity.ProjectGroup = groupEntity;

                    await _uow.Projects.AddAsync(projectEntity, ct);

                    if (dto.HasExternalParticipants)
                    {
                        var projectExternalResearchers = new ExternalResearcherProject
                        {
                            ExternalResearcherId = 1,
                            Project = projectEntity,
                            Role = ExternalResearcherRole,
                            CreatedAtUtc = DateTime.UtcNow,
                            CreatedByUserId = createdByUserId,
                            ExitDate = null
                        };

                        await _uow.ExternalResearcherProjects.AddAsync(projectExternalResearchers, ct);
                    }

                    if (finalResolutionDoc is not null)
                    {
                        int? finalDocTypeId = ResolveDocumentTypeId(documentTypes, finalResolutionDoc.DocumentType);

                        if (!finalDocTypeId.HasValue)
                        {
                            finalDocTypeId = DocumentTypeIds.ResolucionInformeFinal;
                        }

                        var finalDocument = new Document
                        {
                            DocumentTypeId = finalDocTypeId.Value,
                            DocumentPath = LegacyMatrixDocumentPath,
                            ResolutionCode = finalResolutionDoc.Code,
                            ResolutionDate = finalResolutionDoc.Date,
                            CreatedAt = DateTime.UtcNow,
                            CreatedByUserId = createdByUserId
                        };

                        await _uow.Documents.AddAsync(finalDocument, ct);

                        await _uow.ProjectDocuments.AddAsync(new ProjectDocument
                        {
                            Project = projectEntity,
                            Document = finalDocument
                        }, ct);

                        PhaseLog("Row",
                            $"Final resolution document created for project {dto.ProjectCode} (DocTypeId={finalDocTypeId}).");
                    }

                    if (dto.AssignedValue.HasValue && dto.AssignedValue.Value > 0)
                    {
                        var budgetEntity = new Budget
                        {
                            Project = projectEntity,
                            ApprovedByUserId = createdByUserId,
                            FundingTypeId = FundingTypeIds.Interno,
                            InitialAmount = dto.AssignedValue.Value,
                            CertifiedAmount = 0,
                            ExecutedAmount = dto.ExecutedValue ?? 0,
                            ApprovedAt = approvalDate
                        };

                        await _uow.Budgets.AddAsync(budgetEntity, ct);
                    }
                    else
                    {
                        PhaseLog("Budget", $"No AssignedValue for project {dto.ProjectCode}");
                    }

                    if (allCategories is not null && allCategories.Count > 0)
                    {
                        var candidateIds = new List<int>();

                        var lineId = ResolveResearchCategoryId(
                            allCategories,
                            dto.ResearchLine,
                            ResearchCategoryTypeIds.LineaInvestigacion);

                        if (lineId.HasValue)
                            candidateIds.Add(lineId.Value);

                        var broadFieldId = ResolveResearchCategoryId(
                            allCategories,
                            dto.BroadField,
                            ResearchCategoryTypeIds.CampoAmplio);

                        if (broadFieldId.HasValue)
                            candidateIds.Add(broadFieldId.Value);

                        var specificId = ResolveResearchCategoryId(
                            allCategories,
                            dto.SpecificField,
                            ResearchCategoryTypeIds.CampoEspecifico);

                        if (specificId.HasValue)
                            candidateIds.Add(specificId.Value);

                        var detailedId = ResolveResearchCategoryId(
                            allCategories,
                            dto.DetailedField,
                            ResearchCategoryTypeIds.CampoDetallado);

                        if (detailedId.HasValue)
                            candidateIds.Add(detailedId.Value);

                        var scopeId = ResolveResearchCategoryId(
                            allCategories,
                            dto.TerritorialScope,
                            ResearchCategoryTypeIds.AlcanceTerritorial);

                        if (scopeId.HasValue)
                            candidateIds.Add(scopeId.Value);

                        var impactId = ResolveResearchCategoryId(
                            allCategories,
                            dto.ExpectedImpact,
                            ResearchCategoryTypeIds.ImpactoEsperado);

                        if (impactId.HasValue)
                            candidateIds.Add(impactId.Value);

                        var domainId = ResolveResearchCategoryId(
                            allCategories,
                            dto.Domain,
                            ResearchCategoryTypeIds.Dominio);

                        if (domainId.HasValue)
                            candidateIds.Add(domainId.Value);

                        candidateIds = candidateIds.Distinct().ToList();

                        if (candidateIds.Count > 0)
                        {
                            var leafIds = FilterToLeafCategories(candidateIds, allCategories);

                            var researchCategoryLinks = leafIds
                                .Select(id => new ProjectResearchCategory
                                {
                                    Project = projectEntity,
                                    ResearchCategoryId = id
                                })
                                .ToList();

                            if (researchCategoryLinks.Count > 0)
                                await _uow.ProjectResearchCategories.AddRangeAsync(researchCategoryLinks, ct);
                        }
                    }
                    else
                    {
                        PhaseLog("Categories", "No categories loaded; skipping research category mapping.");
                    }

                    if (dto.Documents is not null && dto.Documents.Count > 0 && documentTypes.Count > 0)
                    {
                        foreach (var docDto in dto.Documents)
                        {
                            if (string.IsNullOrWhiteSpace(docDto.DocumentType))
                                continue;

                            if (docDto.DocumentType.StartsWith("FECHA ", StringComparison.OrdinalIgnoreCase))
                            {
                                continue;
                            }

                            if (docDto.DocumentType == "RESOLUCION INFORME FINAL HCU")
                            {
                                continue;
                            }

                            var docTypeId = ResolveDocumentTypeId(documentTypes, docDto.DocumentType);
                            if (!docTypeId.HasValue)
                                continue;

                            var document = new Document
                            {
                                DocumentTypeId = docTypeId.Value,
                                DocumentPath = LegacyMatrixDocumentPath,
                                ResolutionCode = docDto.Code,
                                ResolutionDate = docDto.Date,
                                CreatedAt = DateTime.UtcNow,
                                CreatedByUserId = createdByUserId
                            };

                            await _uow.Documents.AddAsync(document, ct);

                            await _uow.ProjectDocuments.AddAsync(new ProjectDocument
                            {
                                Project = projectEntity,
                                Document = document
                            }, ct);
                        }
                    }

                    var nowUtc = DateTime.UtcNow;

                    var defaultAcademicPeriodId = academicPeriodsCache
                        .OrderByDescending(p => p.PeriodId)
                        .Select(p => p.PeriodId)
                        .FirstOrDefault();

                    if (dto.Extensions is not null && dto.Extensions.Count > 0)
                    {
                        foreach (var ext in dto.Extensions)
                        {
                            if (string.IsNullOrWhiteSpace(ext.ResolutionCode) &&
                                !ext.NewEndDate.HasValue)
                            {
                                continue;
                            }

                            var extensionDocument = new Document
                            {
                                DocumentTypeId = DocumentTypeIds.ResolucionProrroga,
                                DocumentPath = LegacyMatrixDocumentPath,
                                ResolutionCode = ext.ResolutionCode,
                                ResolutionDate = ext.NewEndDate,
                                CreatedAt = nowUtc,
                                CreatedByUserId = createdByUserId
                            };

                            await _uow.Documents.AddAsync(extensionDocument, ct);

                            var extensionEntity = new ProjectExtension
                            {
                                Project = projectEntity,
                                Document = extensionDocument,
                                ProjectExtensionTypeId = null,
                                ExtensionDate = ext.NewEndDate
                                                ?? projectEntity.TentativeEndDate
                                                ?? nowUtc,
                                RequestedAt = null,
                                ApprovedAt = ext.NewEndDate
                            };

                            await _uow.ProjectExtensions.AddAsync(extensionEntity, ct);
                        }
                    }

                    if (defaultAcademicPeriodId <= 0)
                    {
                        PhaseLog("Visits", $"No AcademicPeriods available. Skipping visits for project {dto.ProjectCode}.");
                    }
                    else if (dto.VisitPeriods is null || dto.VisitPeriods.Count == 0)
                    {
                        PhaseLog("Visits", $"No VisitPeriods provided. No visits created for project {dto.ProjectCode}.");
                    }
                    else
                    {
                        var executedPeriods = dto.VisitPeriods
                            .Select(vp => new
                            {
                                vp,
                                raw = (vp.RawValue ?? string.Empty).Trim()
                            })
                            .Where(x => x.vp.HasReport && !string.IsNullOrWhiteSpace(x.raw))
                            .ToList();

                        if (executedPeriods.Count == 0)
                        {
                            PhaseLog("Visits", $"No executed visits found. No visits created for project {dto.ProjectCode}.");
                        }
                        else
                        {
                            var visitsToInsert = new List<Visit>(executedPeriods.Count);

                            foreach (var x in executedPeriods)
                            {
                                var academicPeriodId = ResolveAcademicPeriodId(academicPeriodsCache, x.vp.PeriodLabel)
                                                       ?? defaultAcademicPeriodId;

                                var visitDocument = new Document
                                {
                                    DocumentTypeId = DocumentTypeIds.ResolucionVisita,
                                    DocumentPath = LegacyMatrixDocumentPath,
                                    ResolutionCode = x.raw,
                                    ResolutionDate = null,
                                    CreatedAt = nowUtc,
                                    CreatedByUserId = createdByUserId
                                };

                                await _uow.Documents.AddAsync(visitDocument, ct);

                                visitsToInsert.Add(new Visit
                                {
                                    Project = projectEntity,
                                    VisitStateId = VisitStateIds.Realized,
                                    AcademicPeriodId = academicPeriodId,
                                    Document = visitDocument,
                                    FundingDocument = null,
                                    ProgressDocument = null,
                                    PerformedByUserId = null,
                                    ScheduledDate = null,
                                    PerformedDate = null,
                                    CreatedAt = nowUtc
                                });
                            }

                            await _uow.Visits.AddRangeAsync(visitsToInsert, ct);

                            PhaseLog("Visits", $"Inserted executed visits: {visitsToInsert.Count} for project {dto.ProjectCode}.");
                        }
                    }

                    if (!string.IsNullOrWhiteSpace(dto.GeneralObjective))
                    {
                        var objetive = new ProjectObjective
                        {
                            Project = projectEntity,
                            ObjectiveTypeId = ObjectiveTypeIds.General,
                            Objective = dto.GeneralObjective ?? string.Empty,
                            Result = string.Empty
                        };

                        await _uow.ProjectObjectives.AddAsync(objetive, ct);
                    }

                    createdCount++;
                }

                await _uow.SaveChangesAsync(ct);

                PhaseLog("Result",
                    $"Created={createdCount}, Skipped={skippedCount}");

                return ServiceResult<int>.Ok(createdCount);
            }
            catch (UnauthorizedAccessException)
            {
                return FailUnauthorized<int>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[IMPORT] EXCEPTION → {ex}");
                _logger.LogError(ex, "[IMPORT] Exception in ImportFromMatrixAsync");
                return FailUnexpected<int>(ErrorMessages.Common.UnexpectedError);
            }
        }
```

## TODO UNIFIED: InsertGroupMembersFromDirectoryAsync

Resolver ExternalFacultyId/ExternalPeriodId a FacultyId/AcademicTermId locales y separar EnsureAppUsersAsync de Identity runtime. No asumir igualdad de IDs; evitar commits internos previos al commit del proyecto.

Referencia exacta: `tesisproject.backend/Services/Implementations/ProjectService.cs:992-1148`. Firma omitida temporalmente del contrato Unified.

```csharp
        private async Task InsertGroupMembersFromDirectoryAsync(
            Group groupEntity,
            ImportedProjectDTO dto,
            IReadOnlyList<ExternalUserProfileModel> directoryCache,
            CancellationToken ct)
        {
            if (groupEntity is null) throw new ArgumentNullException(nameof(groupEntity));
            if (dto is null) throw new ArgumentNullException(nameof(dto));
            if (directoryCache is null) throw new ArgumentNullException(nameof(directoryCache));

            const double MIN_NAME_SIMILARITY = 80.0;

            List<ExternalUserProfileModel> ResolvePeople(IEnumerable<string> names, List<string> discarded)
            {
                var resolved = new List<ExternalUserProfileModel>();

                foreach (var rawName in names ?? Enumerable.Empty<string>())
                {
                    ct.ThrowIfCancellationRequested();

                    var name = (rawName ?? string.Empty).Trim();
                    if (string.IsNullOrWhiteSpace(name))
                        continue;

                    ExternalUserProfileModel? best = null;
                    double bestScore = 0.0;

                    for (int i = 0; i < directoryCache.Count; i++)
                    {
                        var p = directoryCache[i];
                        if (p is null) continue;

                        if (!p.AspId.HasValue || p.AspId.Value <= 0) continue;

                        var score = NameSimilarity.FlexibleFullNameSimilarityPercentage(
                            name,
                            p.FullName,
                            normalize: true);

                        if (score > bestScore)
                        {
                            bestScore = score;
                            best = p;
                        }
                    }

                    if (best is null || bestScore < MIN_NAME_SIMILARITY)
                    {
                        discarded.Add(name);
                        continue;
                    }

                    if (resolved.Any(x => x.AspId == best.AspId))
                        continue;

                    resolved.Add(best);
                }

                return resolved;
            }

            var discardedCoordinators = new List<string>();
            var discardedAlternates = new List<string>();

            var matchedCoordinators = ResolvePeople(dto.Coordinators, discardedCoordinators);
            var matchedAlternates = ResolvePeople(dto.AlternateCoordinators, discardedAlternates);

            dto.CoordinatorDiscardedTokens.AddRange(discardedCoordinators);
            dto.AlternateCoordinatorDiscardedTokens.AddRange(discardedAlternates);

            var allProfiles = matchedCoordinators
                .Concat(matchedAlternates)
                .GroupBy(x => x.AspId!.Value)
                .Select(g => g.First())
                .ToList();

            if (allProfiles.Count == 0)
                return;

            var coordinatorAspIds = matchedCoordinators
                .Concat(matchedAlternates)
                .Select(x => x.AspId!.Value)
                .ToHashSet();

            var registerDtos = allProfiles
                .Select(p =>
                {
                    var aspId = p.AspId!.Value;
                    var roleForAsp = coordinatorAspIds.Contains(aspId)
                        ? AppRoles.Coordinador
                        : AppRoles.User;

                    return new RegisterRequest
                    {
                        Email = p.Email,
                        Username = p.Document,
                        Password = DefaultHardcodedPassword,
                        AspUserId = aspId,
                        Role = roleForAsp
                    };
                })
                .ToList();

            var ensure = await _appUsers.EnsureAppUsersAsync(registerDtos, ct);

            if (!ensure.Success || ensure.Data is null || ensure.Data.Count != registerDtos.Count)
            {
                throw new InvalidOperationException(
                    ensure.Message ?? ErrorMessages.Project.ErrorEnsuringAppUsers);
            }

            var appUserIdByAspId = new Dictionary<int, int>();

            for (int i = 0; i < registerDtos.Count; i++)
            {
                var aspId = registerDtos[i].AspUserId;
                var localAppUserId = ensure.Data[i];
                if (aspId.HasValue)
                {
                    appUserIdByAspId[aspId.Value] = localAppUserId;
                }
            }

            var now = DateTime.UtcNow;

            async Task AddRoleMembersAsync(List<ExternalUserProfileModel> matched, int roleId)
            {
                if (matched is null || matched.Count == 0) return;

                var take = matched.Count >= 2 ? matched.Take(2).ToList() : matched.Take(1).ToList();

                for (int i = 0; i < take.Count; i++)
                {
                    ct.ThrowIfCancellationRequested();

                    var profile = take[i];
                    var aspId = profile.AspId!.Value;

                    if (!appUserIdByAspId.TryGetValue(aspId, out var localUserId))
                        continue;

                    var gm = new GroupMember
                    {
                        Group = groupEntity,
                        UserId = localUserId,
                        MemberRoleId = roleId,
                        JoinedAt = now,
                        LeftAt = (take.Count >= 2 && i == 0) ? now : null
                    };

                    await _uow.GroupMembers.AddAsync(gm, ct);
                }
            }

            await AddRoleMembersAsync(matchedCoordinators, MemberRoleTypeIds.Coordinador);
            await AddRoleMembersAsync(matchedAlternates, MemberRoleTypeIds.Subrogante);
        }
```

## TODO UNIFIED: MapToEntity

Resolver ExternalFacultyId/ExternalPeriodId a FacultyId/AcademicTermId locales y separar EnsureAppUsersAsync de Identity runtime. No asumir igualdad de IDs; evitar commits internos previos al commit del proyecto.

Referencia exacta: `tesisproject.backend/Services/Implementations/ProjectService.cs:900-914`. Firma omitida temporalmente del contrato Unified.

```csharp
        private static Project MapToEntity(AddProjectRequestDTO dto, int userId) => new()
        {
            ProjectCode = dto.ProjectCode ?? string.Empty,
            CreatedByUserId = userId,
            ProjectTypeId = dto.ProjectTypeId,
            ProjectStateId = dto.ProjectStateId,
            ProjectGroupId = dto.ProjectGroupId,
            ProjectName = dto.ProjectName,
            StartDate = dto.StartDate,
            TentativeEndDate = dto.StartDate?.AddMonths(dto.DurationInMonths),
            ExecutionPercentage = 0,
            DurationInMonths = dto.DurationInMonths,
            FacultyId = dto.FacultyId,
            ConvocationId = dto.ConvocationId
        };
```
