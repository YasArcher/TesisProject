using Microsoft.EntityFrameworkCore;
using tesisproject.backend.Repositories.Interfaces;
using tesisproject.backend.Services.Interfaces;
using tesisproject.backend.UnitOfWork.Interfaces;
using tesisproject.shared.Common.Utils;
using tesisproject.shared.DTOs.Auth;
using tesisproject.shared.DTOs.Catalog.ResearchCategory.Response;
using tesisproject.shared.DTOs.External;
using tesisproject.shared.DTOs.Matrices.Import;
using tesisproject.shared.DTOs.Matrices.Response;
using tesisproject.shared.DTOs.Project.Request;
using tesisproject.shared.DTOs.Project.Response;
using tesisproject.shared.DTOs.ProjectObjective.Response;
using tesisproject.shared.Entities.Catalogs;
using tesisproject.shared.Entities.Core;
using tesisproject.shared.Entities.External;
using tesisproject.shared.Responses;

namespace tesisproject.backend.Services.Implementations
{
    public class ProjectService : IProjectService
    {
        private readonly IUnitOfWork _uow;
        private readonly IExternalPeriodsClient _externalPeriods;
        private readonly IAppUserService _appUsers;
        private readonly IExternalAcademicsService _externalAcademics;
        private readonly IResearchCategoryService _researchCategoryService;
        private readonly ILogger<ProjectService> _logger;
        private readonly IExternalDirectoryClient _externalDirectory;

        // 🔹 ResearchCategoryType fijos
        private const int CAT_TYPE_DOMINIO = 1;
        private const int CAT_TYPE_LINEA = 2;
        private const int CAT_TYPE_SUBLINEA = 3;
        private const int CAT_TYPE_CAMPO_AMPLO = 4;
        private const int CAT_TYPE_CAMPO_ESPECIFICO = 5;
        private const int CAT_TYPE_CAMPO_DETALLADO = 6;
        private const int CAT_TYPE_ALCANCE_TERRITORIAL = 7;
        private const int CAT_TYPE_IMPACTO_ESPERADO = 8;

        // 🔹 VisitStates: Realizada
        private const int PLANNED_VISIT_STATE_ID = 1;   // Planificada
        private const int PENDING_VISIT_STATE_ID = 2;   // Pendiente
        private const int REALIZED_VISIT_STATE_ID = 3;  // Realizada
        private const int WAITING_VISIT_STATE_ID = 4;   // En Espera

        // 🔹 DocumentTypes (catálogo fijo)
        // 1 = Memorando inicial
        // 2 = Memorando Informe Final
        // 3 = Resolucion Prorroga
        // 4 = Resolucion Informe Final
        // 5 = Contrato Auspicio
        private const int DOCUMENT_TYPE_RESOLUCION_PRORROGA = 3;
        private const int DOCUMENT_TYPE_VISIT_RESOLUTION = 6; // para visitas históricas con resolución
        private const int DOCUMENT_TYPE_FINAL_PROJECT_RESOLUTION = 4; // para visitas históricas con resolución

        // 🔹 Convocatoria por defecto (para registros sin CallCode o sin match)
        private const int DEFAULT_CONVOCATION_ID = 1;


        public ProjectService(
            IUnitOfWork uow,
            IAppUserService appUsers,
            IExternalAcademicsService externalAcademics,
            IResearchCategoryService researchCategoryService,
            ILogger<ProjectService> logger,
            IExternalDirectoryClient externalDirectory,
            IExternalPeriodsClient externalPeriods)
        {
            _uow = uow;
            _appUsers = appUsers;
            _externalAcademics = externalAcademics;
            _researchCategoryService = researchCategoryService;
            _logger = logger;
            _externalDirectory = externalDirectory;
            _externalPeriods = externalPeriods;
        }

        // ================= READS =================

        public async Task<ServiceResult<List<ProjectListResponseDTO>>> GetAllAsync(CancellationToken ct = default)
        {
            try
            {
                var data = await _uow.Projects
                    .Query()
                    .Include(p => p.Budgets)
                    .Include(p => p.ProjectResearchCategories)
                    .OrderByDescending(p => p.StartDate)
                    .ThenByDescending(p => p.ProjectId)
                    .Select(p => new ProjectListResponseDTO
                    {
                        ProjectId = p.ProjectId,
                        ProjectCode = p.ProjectCode,
                        ProjectName = p.ProjectName,
                        ProjectStateName = p.ProjectState.Name,
                        ProjectTypeName = p.ProjectType.Name,
                        ProjectGroupName = p.ProjectGroup.Name,
                        StartDate = p.StartDate,
                        HasExternalParticipation = p.ExternalResearcherProjects.Any(),
                        TentativeEndDate = p.TentativeEndDate,
                        ExecutionPercentage = p.ExecutionPercentage,
                        PrincipalCoordinatorFacultyId = p.FacultyId,
                        ApprovalDate = p.ApprovalDate,
                        RealEndtDate = p.RealEndDate,
                        FundingTypeId = p.Budgets
                            .Select(b => b.FundingTypeId)
                            .Distinct()
                            .ToList(),

                        ResearchCategoryIds = p.ProjectResearchCategories
                            .Select(prc => prc.ResearchCategoryId)
                            .Distinct()
                            .ToList(),

                        ConvocationId = p.ConvocationId ?? 0
                    })
                    .ToListAsync(ct);

                if (data.Count == 0)
                    return ServiceResult<List<ProjectListResponseDTO>>.Fail("No projects found.", ErrorType.NotFound);

                return ServiceResult<List<ProjectListResponseDTO>>.Ok(data, "Projects retrieved");
            }
            catch (Exception ex)
            {
                return ServiceResult<List<ProjectListResponseDTO>>.Fail(ex.Message, ErrorType.Unexpected);
            }
        }

        public async Task<ServiceResult<ProjectListResponseDTO>> GetByIdAsync(int id, CancellationToken ct = default)
        {
            try
            {
                var dto = await _uow.Projects
                    .Query()
                    .Where(p => p.ProjectId == id)
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

                        FundingTypeId = p.Budgets
                            .Select(b => b.FundingTypeId)
                            .Distinct()
                            .ToList(),

                        ResearchCategoryIds = p.ProjectResearchCategories
                            .Select(prc => prc.ResearchCategoryId)
                            .Distinct()
                            .ToList(),
                        ConvocationId = p.ConvocationId ?? 0
                    })
                    .FirstOrDefaultAsync(ct);

                return dto is null
                    ? ServiceResult<ProjectListResponseDTO>.Fail("Project not found.", ErrorType.NotFound)
                    : ServiceResult<ProjectListResponseDTO>.Ok(dto, "Project retrieved");
            }
            catch (Exception ex)
            {
                return ServiceResult<ProjectListResponseDTO>.Fail(ex.Message, ErrorType.Unexpected);
            }
        }

        public async Task<ServiceResult<List<ProjectListResponseDTO>>> GetByTypeAsync(int projectTypeId, CancellationToken ct = default)
        {
            try
            {
                var data = await _uow.Projects
                    .Query()
                    .Where(p => p.ProjectTypeId == projectTypeId)
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
                    .ToListAsync(ct);

                return ServiceResult<List<ProjectListResponseDTO>>.Ok(data, "Projects by type retrieved");
            }
            catch (Exception ex)
            {
                return ServiceResult<List<ProjectListResponseDTO>>.Fail(ex.Message, ErrorType.Unexpected);
            }
        }

        // ================= WRITES =================

        public async Task<ServiceResult<ProjectListResponseDTO>> CreateAsync(AddProjectRequestDTO dto, int currentUserId, CancellationToken ct = default)
        {
            try
            {
                var duplicate = await _uow.Projects.Query()
                    .AnyAsync(p => p.ProjectName == dto.ProjectName && p.ProjectGroupId == dto.ProjectGroupId, ct);

                if (duplicate)
                    return ServiceResult<ProjectListResponseDTO>.Fail(
                        "A project with the same name already exists in this group.",
                        ErrorType.Conflict
                    );

                var user = await _uow.AppUsers.GetByIdUserAsync(currentUserId, ct);
                if (user is null)
                {
                    return ServiceResult<ProjectListResponseDTO>.Fail(
                        "User not found.",
                        ErrorType.NotFound
                    );
                }

                var entity = MapToEntity(dto, user.IdUser);
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

                return ServiceResult<ProjectListResponseDTO>.Ok(created, "Project created");
            }
            catch (DbUpdateException dbex)
            {
                return ServiceResult<ProjectListResponseDTO>.Fail(dbex.InnerException?.Message ?? dbex.Message, ErrorType.Conflict);
            }
            catch (Exception ex)
            {
                return ServiceResult<ProjectListResponseDTO>.Fail(ex.Message, ErrorType.Unexpected);
            }
        }

        public async Task<ServiceResult<NoContent>> UpdateAsync(int id, UpdateProjectRequestDTO dto, CancellationToken ct = default)
        {
            try
            {
                var current = await _uow.Projects.GetByIdAsync(new object[] { id }, ct);
                if (current is null)
                    return ServiceResult<NoContent>.Fail("Project not found.", ErrorType.NotFound);

                ApplyUpdate(current, dto);
                _uow.Projects.Update(current);
                await _uow.SaveChangesAsync(ct);

                return ServiceResult<NoContent>.Ok(new NoContent(), "Project updated");
            }
            catch (DbUpdateConcurrencyException)
            {
                return ServiceResult<NoContent>.Fail("Concurrency conflict.", ErrorType.Conflict);
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

        public async Task<ServiceResult<NoContent>> DeleteAsync(int id, CancellationToken ct = default)
        {
            try
            {
                var current = await _uow.Projects.GetByIdAsync(new object[] { id }, ct);
                if (current is null)
                    return ServiceResult<NoContent>.Fail("Project not found.", ErrorType.NotFound);

                _uow.Projects.Remove(current);
                await _uow.SaveChangesAsync(ct);

                return ServiceResult<NoContent>.Ok(new NoContent(), "Project deleted");
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

        public async Task<ServiceResult<ProjectDetailResponseDTO>> GetProjectDetailAsync(
           int projectId,
           CancellationToken ct = default)
        {
            try
            {
                var dto = await _uow.Projects
                   .Query()
                   .Include(p => p.ProjectResearchCategories)
                   .Where(p => p.ProjectId == projectId)
                   .Select(p => new ProjectDetailResponseDTO
                   {
                       ProjectId = p.ProjectId,
                       ProjectCode = p.ProjectCode ?? string.Empty,
                       ProjectName = p.ProjectName ?? string.Empty,

                       ProjectObjectives = MapToDTO(p.ProjectObjectives),

                       ResearchCategoryIds = p.ProjectResearchCategories
                           .Select(prc => prc.ResearchCategoryId)
                           .Distinct()
                           .ToList(),

                       ProjectTypeId = p.ProjectTypeId,
                       ProjectTypeName = p.ProjectType.Name ?? string.Empty,
                       ConvocationId = p.ConvocationId ?? 0,
                       DurationInMonths = p.DurationInMonths,
                       ProjectStateId = p.ProjectStateId,
                       ProjectStateName = p.ProjectState.Name ?? string.Empty,

                       StartDate = p.StartDate,
                       TentativeEndDate = p.TentativeEndDate,
                       RealEndDate = p.RealEndDate,
                       ExecutionPercentage = p.ExecutionPercentage ?? 0,

                       ProjectGroupId = p.ProjectGroupId,
                       ProjectGroupName = p.ProjectGroup.Name ?? string.Empty,

                       Budgets = p.Budgets
                           .OrderBy(b => b.BudgetId)
                           .Select(b => new ProjectBudgetDetailDTO
                           {
                               BudgetId = b.BudgetId,
                               InitialAmount = b.InitialAmount,
                               CertifiedAmount = b.CertifiedAmount,
                               ExecutedAmount = b.ExecutedAmount,
                               ApprovedAt = b.ApprovedAt,
                               FundingTypeId = b.FundingTypeId,
                               FundingTypeName = b.FundingType.Name
                           })
                           .ToList()
                   })
                   .FirstOrDefaultAsync(ct);

                return dto is null
                    ? ServiceResult<ProjectDetailResponseDTO>.Fail("Project not found.", ErrorType.NotFound)
                    : ServiceResult<ProjectDetailResponseDTO>.Ok(dto, "Project detail retrieved");
            }
            catch (Exception ex)
            {
                return ServiceResult<ProjectDetailResponseDTO>.Fail(ex.Message, ErrorType.Unexpected);
            }
        }

        public async Task<ServiceResult<ProjectDetailResponseDTO>> CreateFullAsync(
            AddProjectFullRequestDTO request,
            int currentUserId,
            CancellationToken ct = default)
        {
            void PhaseLog(string phase, string message)
                => Console.WriteLine($"[DEBUG] [{phase}] {message}");

            try
            {
                if (request.Project is null)
                {
                    PhaseLog("Fase 0 - Request", "Project payload is null");
                    return ServiceResult<ProjectDetailResponseDTO>.Fail("Invalid project data.");
                }

                var p = request.Project;
                var d = request.ProjectDocumentData;

                // ============================================
                // 1) Validación mínima
                // ============================================

                if (p.ProjectTypeId <= 0)
                {
                    PhaseLog("Fase 1 - Validación", "ProjectTypeId <= 0");
                    return ServiceResult<ProjectDetailResponseDTO>.Fail("Invalid ProjectTypeId.");
                }

                if (p.ProjectStateId <= 0)
                {
                    PhaseLog("Fase 1 - Validación", "ProjectStateId <= 0");
                    return ServiceResult<ProjectDetailResponseDTO>.Fail("Invalid ProjectStateId.");
                }

                var user = await _uow.AppUsers.GetByIdUserAsync(currentUserId, ct);
                if (user is null)
                {
                    return ServiceResult<ProjectDetailResponseDTO>.Fail(
                        "User not found.",
                        ErrorType.NotFound
                    );
                }

                if (user.IdUser <= 0)
                {
                    PhaseLog("Fase 1 - Validación", "CreatedByUserId <= 0");
                    return ServiceResult<ProjectDetailResponseDTO>.Fail("Invalid CreatedByUserId.");
                }

                if (p.FacultyId <= 0)
                {
                    PhaseLog("Fase 1 - Validación", "FacultyId <= 0");
                    return ServiceResult<ProjectDetailResponseDTO>.Fail("Invalid FacultyId.");
                }

                if (request.ScheduledDate == default)
                {
                    PhaseLog("Fase 1 - Validación", "ScheduledDate is default");
                    return ServiceResult<ProjectDetailResponseDTO>.Fail("Invalid scheduled date.");
                }

                // ============================================
                // 1.5) Generar ProjectNumber + ProjectCode real
                // ============================================
                PhaseLog("Fase 1.5 - Código", "Generando código de proyecto...");

                var facultyCode = (p.ProjectCode ?? string.Empty).Trim();

                if (string.IsNullOrWhiteSpace(facultyCode))
                {
                    PhaseLog("Fase 1.5 - Código", "facultyCode vacío");
                    return ServiceResult<ProjectDetailResponseDTO>.Fail("Faculty project code (prefix) is required.");
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
                    return ServiceResult<ProjectDetailResponseDTO>.Fail("Generated project code is too long.");
                }

                // ============================================
                // 2) Crear Group
                // ============================================

                PhaseLog("Fase 2 - Group", "Creando grupo...");

                var groupEntity = new Group
                {
                    GroupTypeId = 1,
                    Name = generatedCode
                };

                await _uow.Groups.AddAsync(groupEntity, ct);

                // ============================================
                // 3) Validar nombre duplicado
                // ============================================

                PhaseLog("Fase 3 - Validación nombre", "Revisando duplicados...");

                var duplicate = await _uow.Projects
                    .Query(asNoTracking: true)
                    .AnyAsync(x => x.ProjectName == p.ProjectName, ct);

                if (duplicate)
                {
                    PhaseLog("Fase 3 - Validación nombre", "Duplicado detectado");
                    return ServiceResult<ProjectDetailResponseDTO>.Fail("A project with the same name already exists.");
                }

                // ============================================
                // 4) Crear Project
                // ============================================

                PhaseLog("Fase 4 - Project",
                    $"Creando entidad proyecto con: generatedCode={generatedCode}, nextNumber={nextNumber}");

                var projectEntity = new Project
                {
                    ProjectCode = generatedCode,
                    CreatedByUserId = user.IdUser,
                    ProjectTypeId = p.ProjectTypeId,
                    ProjectNumber = nextNumber,
                    ProjectStateId = p.ProjectStateId,
                    ProjectName = p.ProjectName ?? string.Empty,
                    ApprovalDate = p.ApprovalDate,
                    StartDate = p.StartDate,
                    DurationInMonths = p.DurationInMonths,
                    TentativeEndDate = p.StartDate?.AddMonths(p.DurationInMonths),
                    RealEndDate = null,
                    ProjectOriginTypeId = 1,
                    ExecutionPercentage = 0,
                    FacultyId = p.FacultyId,
                    ConvocationId = p.ConvocationId
                };

                projectEntity.ProjectGroup = groupEntity;

                await _uow.Projects.AddAsync(projectEntity, ct);

                // ============================================
                // 4.5) Vincular documento inicial (si existe)
                // ============================================

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

                // ============================================
                // 5) Miembros del grupo
                // ============================================

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
                            Password = "aaaaaqqq1231231",
                            AspUserId = m.AspUserId
                        })
                        .ToList();

                    PhaseLog("Fase 5 - Miembros",
                        $"RegisterRequest count: {registerDtos.Count}, first email: {registerDtos.First().Email}");

                    var ensureResult = await _appUsers.EnsureAppUsersAsync(registerDtos, ct);

                    PhaseLog("Fase 5 - Miembros",
                        $"EnsureAppUsersAsync => Success={ensureResult.Success}, " +
                        $"Error={ensureResult.Error}, DataCount={(ensureResult.Data?.Count ?? 0)}");

                    if (!ensureResult.Success || ensureResult.Data is null)
                    {
                        PhaseLog("Fase 5 - Miembros", $"Error asegurando AppUsers: {ensureResult.Error}");
                        return ServiceResult<ProjectDetailResponseDTO>.Fail(
                            ensureResult.Message ?? "Error ensuring app users.",
                            ErrorType.Unexpected);
                    }

                    var appUserIds = ensureResult.Data;

                    if (appUserIds.Count != request.GroupMembers.Count)
                    {
                        PhaseLog("Fase 5 - Miembros",
                            $"Cantidad de AppUserIds ({appUserIds.Count}) != GroupMembers ({request.GroupMembers.Count})");
                        return ServiceResult<ProjectDetailResponseDTO>.Fail("Inconsistent app user mapping.");
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

                // ============================================
                // 6) Categorías
                // ============================================

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

                // ============================================
                // 7) Presupuestos
                // ============================================

                PhaseLog("Fase 7 - Presupuesto", "Insertando budgets...");

                if (request.Budgets is not null)
                {
                    var budgetEntities = request.Budgets
                        .Where(b => b.FundingTypeId > 0 && b.InitialAmount > 0)
                        .Select(b => new Budget
                        {
                            Project = projectEntity,
                            ApprovedByUserId = user.IdUser,
                            InitialAmount = b.InitialAmount,
                            CertifiedAmount = 0,
                            ExecutedAmount = 0,
                            ApprovedAt = DateTime.Now,
                            FundingTypeId = b.FundingTypeId
                        })
                        .ToList();

                    await _uow.Budgets.AddRangeAsync(budgetEntities, ct);
                }

                // ============================================
                // 8) Objetivos y Actividades
                // ============================================

                PhaseLog("Fase 8 - Objetivos", "Insertando objetivos...");

                if (request.Objectives is not null)
                {
                    foreach (var objDto in request.Objectives)
                    {
                        var objectiveEntity = new ProjectObjective
                        {
                            Project = projectEntity,
                            ObjectiveTypeId = objDto.ObjectiveTypeId,
                            Objetive = objDto.Objective,
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
                                    ProgressPercentage = actDto.ProgressPercentage,
                                    CreatedAt = DateTime.UtcNow
                                };

                                await _uow.ObjectiveActivities.AddAsync(activityEntity, ct);
                            }
                        }
                    }
                }

                // ============================================
                // 8.5) Investigadores externos
                // ============================================

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
                                Role = "ExternalResearcher",
                                CreatedAtUtc = DateTime.UtcNow,
                                CreatedByUserId = user.IdUser,
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

                // ============================================
                // 9) Visitas (nuevos proyectos)
                // ============================================

                PhaseLog("Fase 9 - Visitas", "Generando visitas...");

                var periodsResult = await _externalPeriods.GetAllAsync(ct);
                if (!periodsResult.Success || periodsResult.Data is null || periodsResult.Data.Count == 0)
                {
                    PhaseLog("Fase 9 - Visitas", "No external academic periods available. Skipping visits creation.");
                }
                else
                {
                    var latest = periodsResult.Data
                        .OrderByDescending(p => p.PeriodId)   // o EndDate si prefieres
                        .FirstOrDefault();

                    if (latest is not null &&
                        projectEntity.StartDate is not null &&
                        projectEntity.DurationInMonths > 0)
                    {
                        var totalVisits = projectEntity.DurationInMonths / 6;

                        var visits = Enumerable.Range(0, totalVisits)
                            .Select(_ => new Visit
                            {
                                Project = projectEntity,
                                VisitStateId = 1,
                                AcademicPeriodId = latest.PeriodId, // ✅ ahora viene de API
                                ScheduledDate = request.ScheduledDate,
                                PerformedDate = null,
                                CreatedAt = DateTime.UtcNow
                            })
                            .ToList();

                        await _uow.Visits.AddRangeAsync(visits, ct);
                    }
                }


                // ============================================
                // 10) Guardar
                // ============================================

                PhaseLog("Fase 10 - Save", "Guardando UoW...");

                await _uow.SaveChangesAsync(ct);

                // ============================================
                // 11) Obtener detalle final
                // ============================================

                PhaseLog("Fase 11 - Detalle", "Consultando detalle...");

                var detail = await GetProjectDetailAsync(projectEntity.ProjectId, ct);
                if (!detail.Success)
                    return ServiceResult<ProjectDetailResponseDTO>.Fail("Error retrieving project detail.");

                return ServiceResult<ProjectDetailResponseDTO>.Ok(detail.Data!);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DEBUG] EXCEPCIÓN → {ex}");
                return ServiceResult<ProjectDetailResponseDTO>.Fail("Unexpected server error.");
            }
        }

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

        private static ICollection<ProjectObjectiveListItemDTO> MapToDTO(
            ICollection<ProjectObjective> entities)
        {
            return entities
                .Select(e => new ProjectObjectiveListItemDTO
                {
                    Id = e.Id,
                    ProjectId = e.ProjectId,
                    ObjectiveTypeId = e.ObjectiveTypeId,
                    ObjectiveTypeName = e.ObjectiveType?.Name ?? string.Empty,
                    Objective = e.Objetive,
                    Result = e.Result,
                    ActivitiesCount = e.Activities?.Count ?? 0
                })
                .ToList();
        }

        public async Task<ServiceResult<NoContent>> UpdateResearchCategoriesAsync(
            int projectId,
            List<int> researchCategoryIds,
            CancellationToken ct = default)
        {
            try
            {
                var project = await _uow.Projects
                    .Query()
                    .Include(p => p.ProjectResearchCategories)
                    .FirstOrDefaultAsync(p => p.ProjectId == projectId, ct);

                if (project is null)
                {
                    return ServiceResult<NoContent>.Fail(
                        "Project not found.",
                        ErrorType.NotFound);
                }

                var newIds = (researchCategoryIds ?? new List<int>())
                    .Distinct()
                    .ToList();

                var currentLinks = project.ProjectResearchCategories.ToList();
                var currentIds = currentLinks
                    .Select(x => x.ResearchCategoryId)
                    .ToList();

                var toRemoveIds = currentIds.Except(newIds).ToList();
                var toAddIds = newIds.Except(currentIds).ToList();

                var linksToRemove = currentLinks
                    .Where(x => toRemoveIds.Contains(x.ResearchCategoryId))
                    .ToList();

                if (linksToRemove.Count > 0)
                {
                    _uow.ProjectResearchCategories.RemoveRange(linksToRemove);
                }

                foreach (var catId in toAddIds)
                {
                    project.ProjectResearchCategories.Add(new ProjectResearchCategory
                    {
                        ProjectId = project.ProjectId,
                        ResearchCategoryId = catId
                    });
                }

                await _uow.SaveChangesAsync(ct);

                return ServiceResult<NoContent>.Ok(new NoContent(), "Project research categories updated.");
            }
            catch (Exception ex)
            {
                return ServiceResult<NoContent>.Fail(
                    ex.Message,
                    ErrorType.Unexpected);
            }
        }

        private async Task InsertGroupMembersFromDirectoryAsync(
    Group groupEntity,
    ImportedProjectDTO dto,
    IReadOnlyList<ExternalUserProfileModel> directoryCache,
    CancellationToken ct)
        {
            if (groupEntity is null) throw new ArgumentNullException(nameof(groupEntity));
            if (dto is null) throw new ArgumentNullException(nameof(dto));
            if (directoryCache is null) throw new ArgumentNullException(nameof(directoryCache));

            // Roles (ajusta a tus IDs reales de MemberRoleType)
            // Ejemplo: 1=Coordinador, 2=Subrogante, 3=Investigador
            const int ROLE_COORDINATOR = 1;
            const int ROLE_ALTERNATE_COORDINATOR = 2;
            const int ROLE_INVESTIGATOR = 3; // si luego lo agregas al DTO

            // Threshold recomendado para evitar matches basura
            const double MIN_NAME_SIMILARITY = 80.0;

            // ===== Helper local: resuelve personas por similitud contra directorio =====
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

                        // ASP_ID no puede faltar según tú, igual validamos por seguridad
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

                    // Evitar duplicados (mismo usuario externo repetido)
                    if (resolved.Any(x => x.AspId == best.AspId))
                        continue;

                    resolved.Add(best);
                }

                return resolved;
            }

            // ===== 1) Resolver por rol =====
            // DTO ya trae listas: Coordinators / AlternateCoordinators
            var discardedCoordinators = new List<string>();
            var discardedAlternates = new List<string>();

            var matchedCoordinators = ResolvePeople(dto.Coordinators, discardedCoordinators);
            var matchedAlternates = ResolvePeople(dto.AlternateCoordinators, discardedAlternates);

            // Si luego agregas Investigators al DTO:
            // var discardedInvestigators = new List<string>();
            // var matchedInvestigators = ResolvePeople(dto.Investigators, discardedInvestigators);

            // (Opcional) guardar auditoría si tú quieres:
            dto.CoordinatorDiscardedTokens.AddRange(discardedCoordinators);
            dto.AlternateCoordinatorDiscardedTokens.AddRange(discardedAlternates);

            // ===== 2) Construir lista total de perfiles a asegurar en ASP local (una sola llamada) =====
            var allProfiles = matchedCoordinators
                .Concat(matchedAlternates)
                //.Concat(matchedInvestigators)
                .GroupBy(x => x.AspId!.Value)
                .Select(g => g.First())
                .ToList();

            if (allProfiles.Count == 0)
                return;

            var registerDtos = allProfiles
                .Select(p => new RegisterRequest
                {
                    Email = p.Email,
                    Username = p.Document,                // tu CreateFull usa Document aquí
                    Password = "aaaaaqqq1231231",         // igual que tu flujo actual
                    AspUserId = p.AspId!.Value
                })
                .ToList();

            // Misma lógica de CreateFull: EnsureAppUsersAsync
            var ensure = await _appUsers.EnsureAppUsersAsync(registerDtos, ct);

            if (!ensure.Success || ensure.Data is null || ensure.Data.Count != registerDtos.Count)
            {
                // No invento comportamiento: fallo duro porque se rompe la consistencia de mapeo
                throw new InvalidOperationException(
                    ensure.Message ?? "Error ensuring app users for group members.");
            }

            // ===== 3) Crear mapa ASP_ID -> Local AppUserId (el orden importa por tu contrato actual) =====
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

            // ===== 4) Insertar GroupMembers por rol con tu regla de "2+ personas" =====
            var now = DateTime.UtcNow;

            async Task AddRoleMembersAsync(List<ExternalUserProfileModel> matched, int roleId)
            {
                if (matched is null || matched.Count == 0) return;

                // Regla: si hay 2+, solo consideramos los 2 primeros (1ro inactivo, 2do activo)
                // Si quieres meter todos y solo “apagar” el primero, dímelo y lo ajusto.
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
                        // 👇 regla:
                        LeftAt = (take.Count >= 2 && i == 0) ? now : null
                    };

                    await _uow.GroupMembers.AddAsync(gm, ct);
                }
            }

            await AddRoleMembersAsync(matchedCoordinators, ROLE_COORDINATOR);
            await AddRoleMembersAsync(matchedAlternates, ROLE_ALTERNATE_COORDINATOR);
            // await AddRoleMembersAsync(matchedInvestigators, ROLE_INVESTIGATOR);
        }


        public async Task<ServiceResult<int>> ImportFromMatrixAsync(
    ProjectMatrixUploadSummaryDTO summary,
    int currentUserId,
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

                // 🔹 AcademicPeriods (para mapear visitas históricas)
                var periodsResult = await _externalPeriods.GetAllAsync(ct);
                if (!periodsResult.Success || periodsResult.Data is null || periodsResult.Data.Count == 0)
                {
                    PhaseLog("Init-Periods", "Cannot retrieve academic periods from external API.");
                    // Aquí decides: o fallas, o sigues sin visitas.
                    // Yo lo dejo como "seguir" para no romper import completo:
                }
                var academicPeriodsCache = periodsResult.Data?.ToList() ?? new List<ExternalAcademicPeriodModel>();
                PhaseLog("Init", $"AcademicPeriods loaded (external): {academicPeriodsCache.Count}");

                PhaseLog("Init", $"AcademicPeriods loaded: {academicPeriodsCache.Count}");

                // 🔹 Categorías de investigación (una sola vez)
                var categoriesResult = await _researchCategoryService.ListAsync(
                    onlyActives: true,
                    ct: ct);

                if (!categoriesResult.Success || categoriesResult.Data is null)
                {
                    PhaseLog("Init-Categories",
                        $"No se pudieron cargar categorías de investigación: {categoriesResult.Error}");
                }

                var allCategories = categoriesResult.Data; // IReadOnlyList<ResearchCategoryListItemDTO>?

                // 🔹 Tipos de documento (para dto.Documents → DocumentType)
                var documentTypes = await _uow.DocumentTypes
                    .Query(asNoTracking: true)
                    .ToListAsync(ct);
                PhaseLog("Init", $"DocumentTypes loaded: {documentTypes.Count}");

                // 🔹 Convocatorias en caché
                var convocationsCache = await _uow.Convocations
                    .Query(asNoTracking: false)
                    .ToListAsync(ct);
                PhaseLog("Init", $"Convocations loaded: {convocationsCache.Count}");

                // 🔹 Facultades externas
                var facultiesResult = await _externalAcademics.GetFacultiesAsync(ct);
                if (!facultiesResult.Success || facultiesResult.Data is null || facultiesResult.Data.Count == 0)
                {
                    PhaseLog("Init-Faculties", "Cannot retrieve faculties from external API.");
                    return ServiceResult<int>.Fail("Cannot retrieve faculties from external API.", ErrorType.Unexpected);
                }
                var externalFacultiesCache = facultiesResult.Data;
                PhaseLog("Init-Faculties", $"External faculties loaded: {externalFacultiesCache.Count}");

                // 🔹 Estados de proyecto (FINALIZADO, EN CIERRE, etc.)
                var projectStatesCache = await _uow.ProjectStates
                    .Query(asNoTracking: true)
                    .ToListAsync(ct);
                PhaseLog("Init", $"ProjectStates loaded: {projectStatesCache.Count}");

                if (summary.ImportedProjects is null || summary.ImportedProjects.Count == 0)
                {
                    PhaseLog("Init", "Summary.ImportedProjects is null or empty.");
                    return ServiceResult<int>.Fail("No imported projects found in summary.");
                }

                PhaseLog("Init", $"ImportedProjects in summary: {summary.ImportedProjects.Count}");

                // Usuario que ejecuta el import
                var user = await _uow.AppUsers.GetByIdUserAsync(currentUserId, ct);
                if (user is null || user.IdUser <= 0)
                {
                    PhaseLog("Init", $"User not found or invalid. currentUserId={currentUserId}");
                    return ServiceResult<int>.Fail("User not found or invalid.", ErrorType.NotFound);
                }

                PhaseLog("Init", $"Import executed by UserId={user.IdUser}");

                var directoryResult = await _externalDirectory.GetAllAsync(ct);
                if (!directoryResult.Success || directoryResult.Data is null || directoryResult.Data.Count == 0)
                    return ServiceResult<int>.Fail("Cannot retrieve external directory.", ErrorType.Unexpected);

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

                    // 2.2 Facultades (match más cercano del API externo)
                    int? facultyId = null;
                    if (!string.IsNullOrWhiteSpace(dto.Faculty))
                    {
                        facultyId = ResolveFacultyExternalId(externalFacultiesCache, dto.Faculty!);
                        PhaseLog("Row", $"Resolved Faculty for {dto.ProjectCode}: {dto.Faculty} → {facultyId}");
                    }

                    // 2.3 Convocatoria
                    int convocationId;
                    Convocation? convocationEntity = null;
                    var rawCallCode = dto.CallCode;

                    // 🟦 Caso 1: CallCode vacío → usar ID fijo
                    if (string.IsNullOrWhiteSpace(rawCallCode))
                    {
                        convocationId = DEFAULT_CONVOCATION_ID;

                        PhaseLog("Row",
                            $"CallCode empty → Assigning fixed ConvocationId={convocationId} to project {dto.ProjectCode}");
                    }
                    else
                    {
                        // 🟩 Caso 2: CallCode con texto → usar mejor coincidencia (Levenshtein)
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

                    // 2.4 Estado del proyecto
                    int? projectStateId = null;
                    if (!string.IsNullOrWhiteSpace(dto.State))
                    {
                        projectStateId = ResolveProjectStateId(projectStatesCache, dto.State!);
                        PhaseLog("Row",
                            $"Resolved ProjectState for {dto.ProjectCode}: State={dto.State} → ProjectStateId={projectStateId}");
                    }

                    if (dto.StartDate.HasValue && dto.StartDate.Value.Date > DateTime.UtcNow.Date)
                    {
                        projectStateId = 6;
                        PhaseLog("Row",
                            $"StartDate is in the future → Forcing ProjectStateId=6 for {dto.ProjectCode} (StartDate={dto.StartDate:yyyy-MM-dd}).");
                    }

                    // 2.5 Evitar duplicados: si ya existe mismo código y número, regenerar código
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

                    // ============================================
                    // 3) Crear Group
                    // ============================================

                    var groupEntity = new Group
                    {
                        GroupTypeId = 1,
                        Name = dto.ProjectCode!
                    };

                    await _uow.Groups.AddAsync(groupEntity, ct);
                    PhaseLog("Row",
                        $"Group queued for insert: Name={groupEntity.Name}");

                    await InsertGroupMembersFromDirectoryAsync(groupEntity, dto, directoryCache, ct);


                    // ============================================
                    // 4) Crear Project
                    // ============================================

                    PhaseLog("Row",
                        $"Creating project: Code={dto.ProjectCode}, Number={dto.Number}, Name={dto.ProjectName}");

                    var durationMonths = dto.TermMonths ?? 0;

                    // Fechas desde documentos
                    var approvalDoc = dto.Documents
                        .FirstOrDefault(d => d.DocumentType == "APROBACION HCU/CONIN");

                    DateTime? approvalDate = approvalDoc?.Date;

                    var finalResolutionDoc = dto.Documents
                        .FirstOrDefault(d => d.DocumentType == "RESOLUCION INFORME FINAL HCU");

                    DateTime? realEndDate = finalResolutionDoc?.Date;

                    // Si en la matriz vino FECHA DE FINALIZACIÓN ESTIMADA la usamos,
                    // si no, la calculamos como StartDate + Plazo
                    DateTime? tentativeEndDate = dto.EstimatedEndDate;

                    if (!tentativeEndDate.HasValue && dto.StartDate.HasValue && durationMonths > 0)
                    {
                        tentativeEndDate = dto.StartDate.Value.AddMonths(durationMonths);
                    }
                    // Regla: si el código empieza con "PE" => External (2); caso contrario Internal (1)
                    var originTypeId = (dto.ProjectCode ?? string.Empty)
                        .Trim()
                        .StartsWith("PE", StringComparison.OrdinalIgnoreCase)
                            ? 2
                            : 1;


                    var projectEntity = new Project
                    {
                        ProjectCode = $"{dto.ProjectCode}-{dto.Number!.Value}",
                        ProjectNumber = dto.Number!.Value,
                        ProjectName = dto.ProjectName ?? string.Empty,
                        CreatedByUserId = user.IdUser,
                        ProjectTypeId = 1,
                        FacultyId = facultyId ?? 0,
                        ConvocationId = convocationId,
                        ProjectStateId = projectStateId ?? 0,
                        ProjectOriginTypeId = originTypeId,
                        ApprovalDate = approvalDate,
                        StartDate = dto.StartDate,
                        DurationInMonths = durationMonths,
                        TentativeEndDate = tentativeEndDate,
                        RealEndDate = realEndDate,
                        ExecutionPercentage = dto.ExecutionProgress * 100 ?? 0m,
                    };

                    projectEntity.ProjectGroup = groupEntity;

                    await _uow.Projects.AddAsync(projectEntity, ct);

                    if (dto.HasExternalParticipants)
                    {
                        var projectExternalResearchers = new ExternalResearcherProject
                        {
                            ExternalResearcherId = 1,
                            Project = projectEntity,
                            Role = "ExternalResearcher",
                            CreatedAtUtc = DateTime.UtcNow,
                            CreatedByUserId = user.IdUser,
                            ExitDate = null
                        };

                        await _uow.ExternalResearcherProjects.AddAsync(projectExternalResearchers, ct);
                    }

                        // ============================================
                        // 4.1) Documento de RESOLUCION INFORME FINAL HCU (si existe)
                        //      → debe usar el tipo de doc "final de proyecto" (p.ej. Id = 4)
                        // ============================================

                        if (finalResolutionDoc is not null)
                    {
                        int? finalDocTypeId = ResolveDocumentTypeId(documentTypes, finalResolutionDoc.DocumentType);

                        if (!finalDocTypeId.HasValue)
                        {
                            // Fallback al tipo fijo de "resolución final de proyecto"
                            finalDocTypeId = DOCUMENT_TYPE_FINAL_PROJECT_RESOLUTION;
                        }

                        var finalDocument = new Document
                        {
                            DocumentTypeId = finalDocTypeId.Value,
                            DocumentPath = "legacy-matrix",
                            ResolutionCode = finalResolutionDoc.Code,
                            ResolutionDate = finalResolutionDoc.Date,
                            CreatedAt = DateTime.UtcNow,
                            CreatedByUserId = user.IdUser
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

                    // ============================================
                    // 5) Presupuesto histórico  
                    // ============================================

                    if (dto.AssignedValue.HasValue && dto.AssignedValue.Value > 0)
                    {
                        var budgetEntity = new Budget
                        {
                            Project = projectEntity,
                            ApprovedByUserId = user.IdUser,
                            FundingTypeId = 2, // fijo según tu catálogo
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

                    // ============================================
                    // 6) Categorías de investigación 
                    // ============================================

                    if (allCategories is not null && allCategories.Count > 0)
                    {
                        // Primero solo IDs, sin crear entidades todavía
                        var candidateIds = new List<int>();

                        var lineId = ResolveResearchCategoryId(
                            allCategories,
                            dto.ResearchLine,
                            CAT_TYPE_LINEA);

                        if (lineId.HasValue)
                            candidateIds.Add(lineId.Value);

                        var broadFieldId = ResolveResearchCategoryId(
                            allCategories,
                            dto.BroadField,
                            CAT_TYPE_CAMPO_AMPLO);

                        if (broadFieldId.HasValue)
                            candidateIds.Add(broadFieldId.Value);

                        var specificId = ResolveResearchCategoryId(
                            allCategories,
                            dto.SpecificField,
                            CAT_TYPE_CAMPO_ESPECIFICO);

                        if (specificId.HasValue)
                            candidateIds.Add(specificId.Value);

                        var detailedId = ResolveResearchCategoryId(
                            allCategories,
                            dto.DetailedField,
                            CAT_TYPE_CAMPO_DETALLADO);

                        if (detailedId.HasValue)
                            candidateIds.Add(detailedId.Value);

                        var scopeId = ResolveResearchCategoryId(
                            allCategories,
                            dto.TerritorialScope,
                            CAT_TYPE_ALCANCE_TERRITORIAL);

                        if (scopeId.HasValue)
                            candidateIds.Add(scopeId.Value);

                        var impactId = ResolveResearchCategoryId(
                            allCategories,
                            dto.ExpectedImpact,
                            CAT_TYPE_IMPACTO_ESPERADO);

                        if (impactId.HasValue)
                            candidateIds.Add(impactId.Value);

                        // 🔹 Dominio (NUEVO)
                        var domainId = ResolveResearchCategoryId(
                            allCategories,
                            dto.Domain,
                            CAT_TYPE_DOMINIO);

                        if (domainId.HasValue)
                            candidateIds.Add(domainId.Value);

                        // Quitamos duplicados por si acaso
                        candidateIds = candidateIds
                            .Distinct()
                            .ToList();

                        if (candidateIds.Count > 0)
                        {
                            // 🔍 Aquí aplicamos la lógica jerárquica:
                            // si una categoría es ancestro de otra, se elimina el ancestro.
                            var leafIds = FilterToLeafCategories(candidateIds, allCategories);

                            var researchCategoryLinks = leafIds
                                .Select(id => new ProjectResearchCategory
                                {
                                    Project = projectEntity,
                                    ResearchCategoryId = id
                                })
                                .ToList();

                            if (researchCategoryLinks.Count > 0)
                            {
                                await _uow.ProjectResearchCategories
                                    .AddRangeAsync(researchCategoryLinks, ct);
                            }
                        }
                    }
                    else
                    {
                        PhaseLog("Categories", "No categories loaded; skipping research category mapping.");
                    }

                    // ============================================
                    // 7) Documentos (GENÉRICO desde dto.Documents)
                    //    (excepto FECHA* y RESOLUCION INFORME FINAL HCU, que ya tratamos arriba)
                    // ============================================

                    if (dto.Documents is not null && dto.Documents.Count > 0 && documentTypes.Count > 0)
                    {
                        foreach (var docDto in dto.Documents)
                        {
                            if (string.IsNullOrWhiteSpace(docDto.DocumentType))
                                continue;

                            // Ya usado para ApprovalDate / RealEndDate → no crear Document para FECHA*
                            if (docDto.DocumentType.StartsWith("FECHA ", StringComparison.OrdinalIgnoreCase))
                            {
                                continue;
                            }

                            // Ya creamos el documento de resolución final arriba → no duplicar
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
                                DocumentPath = "legacy-matrix",
                                ResolutionCode = docDto.Code,
                                ResolutionDate = docDto.Date,
                                CreatedAt = DateTime.UtcNow,
                                CreatedByUserId = user.IdUser
                            };

                            await _uow.Documents.AddAsync(document, ct);

                            await _uow.ProjectDocuments.AddAsync(new ProjectDocument
                            {
                                Project = projectEntity,
                                Document = document
                            }, ct);
                        }
                    }

                    // ============================================
                    // 8.A) PRE-GENERAR VISITAS (por meses + prórrogas)
                    // ============================================

                    var nowUtc = DateTime.UtcNow;

                    var isFinalized = IsFinalizedProjectState(projectStatesCache, projectEntity.ProjectStateId);
                    var defaultVisitStateId = isFinalized ? REALIZED_VISIT_STATE_ID : PLANNED_VISIT_STATE_ID;
                    // base: por meses
                    var baseVisits = CalculateBaseVisitCount(durationMonths);

                    // extra: 1 por cada prórroga válida (misma regla de validación que abajo)
                    var extensionVisits = CountValidItems(dto.Extensions, ext =>
                        !(string.IsNullOrWhiteSpace(ext.ResolutionCode) && !ext.NewEndDate.HasValue));

                    var plannedVisits = BuildPlannedVisits(projectEntity, baseVisits + extensionVisits, nowUtc);

                    foreach (var v in plannedVisits)
                    {
                        v.VisitStateId = defaultVisitStateId;
                    }

                    // Si no hay periodos académicos, no podemos persistir visitas (AcademicPeriodId es obligatorio)
                    var defaultAcademicPeriodId = academicPeriodsCache
                        .OrderByDescending(p => p.PeriodId)
                        .Select(p => p.PeriodId)
                        .FirstOrDefault();

                    // ============================================
                    // 8) Prórrogas (ProjectExtension + Document)
                    // ============================================

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
                                DocumentTypeId = DOCUMENT_TYPE_RESOLUCION_PRORROGA,
                                DocumentPath = "legacy-matrix",
                                ResolutionCode = ext.ResolutionCode,
                                ResolutionDate = ext.NewEndDate,
                                CreatedAt = nowUtc,
                                CreatedByUserId = user.IdUser
                            };

                            await _uow.Documents.AddAsync(extensionDocument, ct);

                            var extensionEntity = new ProjectExtension
                            {
                                Project = projectEntity,
                                Document = extensionDocument,
                                ExtensionDate = ext.NewEndDate
                                                ?? projectEntity.TentativeEndDate
                                                ?? nowUtc,
                                RequestedAt = null,
                                ApprovedAt = ext.NewEndDate
                            };

                            await _uow.ProjectExtensions.AddAsync(extensionEntity, ct);
                        }
                    }

                    // ============================================
                    // 9) VISITAS (primero generadas, ahora se asignan periodos + docs)
                    //    Reglas:
                    //    - El Excel/JSON trae MUCHOS VisitPeriods históricos (placeholders).
                    //    - Una visita "ejecutada" se identifica por: HasReport == true && RawValue no vacío.
                    //    - Si el proyecto está FINALIZADO: queremos registrar visitas aunque falte resolución.
                    //    - NO inflar cantidad: no crear visitas extra por periodos vacíos.
                    //    - Si falta slot pero llega una ejecutada, reutilizar una visita "vacía" antes de crear una nueva.
                    // ============================================

                    if (plannedVisits.Count > 0)
                    {
                        var slotIndex = 0;

                        if (dto.VisitPeriods is not null && dto.VisitPeriods.Count > 0)
                        {
                            foreach (var vp in dto.VisitPeriods)
                            {
                                // "Ejecutada" solo si hay evidencia (reporte + código)
                                var rawValue = (vp.RawValue ?? string.Empty).Trim();
                                var executed = vp.HasReport && !string.IsNullOrWhiteSpace(rawValue);

                                // ✅ Si NO está finalizado y NO está ejecutada, no debe consumir slots (evita desplazar los reales)
                                if (!isFinalized && !executed)
                                    continue;

                                Visit? visit = null;

                                // ✅ Caso A: hay slots disponibles -> usar slot y consumirlo
                                if (slotIndex < plannedVisits.Count)
                                {
                                    visit = plannedVisits[slotIndex];
                                    slotIndex++;
                                }
                                else
                                {
                                    // ✅ Caso B: NO hay slots -> NO crear visita extra salvo que sea necesario
                                    // Si no está ejecutada (solo puede pasar si isFinalized==true), no hacemos nada
                                    // porque la regla base ya generó slots suficientes; no queremos inflar por labels extra.
                                    if (!executed)
                                        continue;

                                    // ✅ Reutiliza una visita lo más "vacía" posible: sin Document y sin AcademicPeriod asignado
                                    visit = plannedVisits.FirstOrDefault(v => v.Document == null && v.AcademicPeriodId <= 0)
                                         ?? plannedVisits.FirstOrDefault(v => v.Document == null);

                                    if (visit is null)
                                    {
                                        // Caso raro: más reportes ejecutados que slots realmente disponibles
                                        plannedVisits.Add(new Visit
                                        {
                                            Project = projectEntity,
                                            VisitStateId = PLANNED_VISIT_STATE_ID,
                                            AcademicPeriodId = 0,
                                            FundingDocument = null,
                                            Document = null,
                                            ProgressDocument = null,
                                            PerformedByUserId = null,
                                            ScheduledDate = null,
                                            PerformedDate = null,
                                            CreatedAt = nowUtc
                                        });

                                        visit = plannedVisits[^1];
                                    }
                                }

                                // --- Asignar periodo académico (si se puede resolver) ---
                                var academicPeriodId = ResolveAcademicPeriodId(academicPeriodsCache, vp.PeriodLabel);
                                if (academicPeriodId.HasValue)
                                    visit.AcademicPeriodId = academicPeriodId.Value;

                                // --- Documento SOLO si está ejecutada ---
                                if (executed)
                                {
                                    var visitDocument = new Document
                                    {
                                        DocumentTypeId = DOCUMENT_TYPE_VISIT_RESOLUTION,
                                        DocumentPath = "legacy-matrix",
                                        ResolutionCode = rawValue,
                                        ResolutionDate = null,
                                        CreatedAt = nowUtc,
                                        CreatedByUserId = user.IdUser
                                    };

                                    visit.Document = visitDocument;
                                    visit.VisitStateId = REALIZED_VISIT_STATE_ID;

                                    await _uow.Documents.AddAsync(visitDocument, ct);
                                }
                                else
                                {
                                    // Si no hay evidencia, queda planificada aquí;
                                    // si el proyecto es finalizado, abajo se forzará REALIZED (tu regla de negocio).
                                    visit.VisitStateId = PLANNED_VISIT_STATE_ID;
                                }
                            }
                        }

                        // 3) Asegurar AcademicPeriodId para TODAS las visitas antes de persistir
                        // (porque es obligatorio en BD)
                        if (defaultAcademicPeriodId <= 0)
                        {
                            PhaseLog("Visits", $"No AcademicPeriods available. Skipping planned visits for project {dto.ProjectCode}.");
                        }
                        else
                        {
                            foreach (var v in plannedVisits)
                            {
                                if (v.AcademicPeriodId <= 0)
                                    v.AcademicPeriodId = defaultAcademicPeriodId;

                                // ✅ Tu regla de negocio: si el proyecto está FINALIZADO,
                                // todas las visitas se consideran REALIZED aunque falte resolución en Excel.
                                if (isFinalized)
                                    v.VisitStateId = REALIZED_VISIT_STATE_ID;
                            }

                            await _uow.Visits.AddRangeAsync(plannedVisits, ct);
                        }
                    }

                    // 10 objetivos

                    if (!string.IsNullOrWhiteSpace(dto.GeneralObjective))
                    {
                        var objetive = new ProjectObjective
                        {
                            Project = projectEntity,
                            ObjectiveTypeId = 1,
                            Objetive = dto.GeneralObjective ?? string.Empty,
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
            catch (Exception ex)
            {
                Console.WriteLine($"[IMPORT] EXCEPTION → {ex}");
                _logger.LogError(ex, "[IMPORT] Exception in ImportFromMatrixAsync");
                return ServiceResult<int>.Fail("Unexpected server error.");
            }
        }


        // ============================
        //   HELPERS
        // ============================

        private static int? ResolveDocumentTypeId(
            IReadOnlyList<DocumentType> documentTypes,
            string? documentTypeKey)
        {
            if (documentTypes is null || documentTypes.Count == 0)
                return null;

            if (string.IsNullOrWhiteSpace(documentTypeKey))
                return null;

            var keyNorm = NormalizeHeader(documentTypeKey);

            DocumentType? best = null;
            double bestScore = 0.0;

            foreach (var dt in documentTypes)
            {
                var dtNorm = NormalizeHeader(dt.Name);

                var score = Levenshtein.SimilarityPercentage(
                    keyNorm,
                    dtNorm,
                    normalize: false);

                if (score > bestScore)
                {
                    bestScore = score;
                    best = dt;
                }
            }

            if (best is null || bestScore < 70.0)
                return null;

            return best.Id;
        }

        private static string NormalizeHeader(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            var s = value.Trim().ToUpperInvariant();

            s = s
                .Replace("Á", "A")
                .Replace("É", "E")
                .Replace("Í", "I")
                .Replace("Ó", "O")
                .Replace("Ú", "U")
                .Replace("Ñ", "N");

            while (s.Contains("  "))
                s = s.Replace("  ", " ");

            return s;
        }

        private async Task<string> GenerateUniqueProjectCodeAsync(
            string baseCode,
            CancellationToken ct)
        {
            var exists = await _uow.Projects
                .Query(asNoTracking: true)
                .AnyAsync(x => x.ProjectCode == baseCode, ct);

            if (!exists)
                return baseCode;

            int counter = 1;
            string newCode;

            do
            {
                newCode = $"{baseCode}-DUP{counter}";
                counter++;

            } while (await _uow.Projects
                .Query(asNoTracking: true)
                .AnyAsync(x => x.ProjectCode == newCode, ct));

            return newCode;
        }

        private static List<int> FilterToLeafCategories(
            List<int> candidateIds,
            IReadOnlyList<ResearchCategoryListItemDTO> allCategories)
        {
            // Mapa rápido id -> categoría
            var byId = allCategories.ToDictionary(c => c.Id);

            bool IsAncestor(int ancestorId, int childId)
            {
                var currentId = childId;

                // Subimos por la cadena de padres hasta llegar a la raíz
                while (byId.TryGetValue(currentId, out var cat) && cat.ParentCategoryId.HasValue)
                {
                    if (cat.ParentCategoryId.Value == ancestorId)
                        return true;

                    currentId = cat.ParentCategoryId.Value;
                }

                return false;
            }

            // Nos quedamos solo con los IDs que NO son ancestros de otro
            var leafIds = candidateIds
                .Where(id =>
                    !candidateIds.Any(otherId =>
                        otherId != id && IsAncestor(id, otherId)))
                .ToList();

            return leafIds;
        }


        private static int? ResolveResearchCategoryId(
            IEnumerable<ResearchCategoryListItemDTO> allCategories,
            string? rawName,
            int expectedTypeId,
            double minSimilarity = 0)
        {
            if (allCategories is null)
                return null;

            if (string.IsNullOrWhiteSpace(rawName))
                return null;

            var targetNorm = Levenshtein.NormalizeForComparison(rawName);

            ResearchCategoryListItemDTO? best = null;
            double bestScore = 0.0;

            var candidates = allCategories
                .Where(c => c.ResearchCategoryTypeId == expectedTypeId);

            foreach (var cat in candidates)
            {
                var nameNorm = Levenshtein.NormalizeForComparison(cat.Name);
                var score = Levenshtein.SimilarityPercentage(
                    targetNorm,
                    nameNorm,
                    normalize: false);

                if (score > bestScore)
                {
                    bestScore = score;
                    best = cat;
                }
            }

            if (best is null || bestScore < minSimilarity)
                return null;

            return best.Id;
        }

        private static int? ResolveFacultyExternalId(
            List<ExternalFacultyDTO> faculties,
            string facultyName)
        {
            if (faculties is null || faculties.Count == 0)
                return null;

            if (string.IsNullOrWhiteSpace(facultyName))
                return null;

            var targetNorm = Levenshtein.NormalizeForComparison(facultyName);

            ExternalFacultyDTO? best = null;
            double bestSimilarity = 0.0;

            foreach (var fac in faculties)
            {
                var nameNorm = Levenshtein.NormalizeForComparison(fac.Name);
                var sim = Levenshtein.SimilarityPercentage(nameNorm, targetNorm, normalize: false);

                if (sim > bestSimilarity)
                {
                    bestSimilarity = sim;
                    best = fac;
                }
            }

            if (best is null)
                return null;

            Console.WriteLine($"[IMPORT][Faculty] '{facultyName}' -> '{best.Name}' ({bestSimilarity:F2}%)");

            return best.FacultyId;
        }

        private async Task<Convocation?> ResolveOrCreateConvocationAsync(
            List<Convocation> convocationsCache,
            string callCode,
            CancellationToken ct)
        {
            if (convocationsCache is null || convocationsCache.Count == 0)
            {
                Console.WriteLine("[IMPORT][Convocation] No convocations in cache.");
                return null;
            }

            if (string.IsNullOrWhiteSpace(callCode))
            {
                Console.WriteLine("[IMPORT][Convocation] Empty CallCode, cannot compare.");
                return null;
            }

            Convocation? bestMatch = null;
            double bestSimilarity = double.MinValue;

            foreach (var c in convocationsCache)
            {
                // ✅ Usa Code si existe; si no, Name (que es donde tienes el texto)
                var candidate = c.Code;
                if (string.IsNullOrWhiteSpace(candidate))
                    candidate = c.Name;

                if (string.IsNullOrWhiteSpace(candidate))
                    continue;

                // ✅ Normalización solo una vez (dentro del SimilarityPercentage)
                var sim = Levenshtein.SimilarityPercentage(candidate, callCode, normalize: true);

                if (sim > bestSimilarity)
                {
                    bestSimilarity = sim;
                    bestMatch = c;
                }
            }

            if (bestMatch is null)
            {
                Console.WriteLine($"[IMPORT][Convocation] No match found for '{callCode}'.");
                return null;
            }

            // (Opcional) umbral mínimo para evitar matches “forzados”
            // if (bestSimilarity < 90.0) return null;

            Console.WriteLine(
                $"[IMPORT][Convocation] Using best match '{callCode}' -> '{(bestMatch.Code ?? bestMatch.Name)}' ({bestSimilarity:F2}%)");

            return bestMatch;
        }

        private static int? ResolveProjectStateId(
            List<ProjectState> states,
            string stateName)
        {
            if (states is null || states.Count == 0)
                return null;

            if (string.IsNullOrWhiteSpace(stateName))
                return null;

            var norm = Levenshtein.NormalizeForComparison(stateName);

            var manualMap = new Dictionary<string, string>
            {
                ["archivado"] = "CANCELADO",
                ["ejecucion"] = "EN EJECUCION",
                ["ejecución"] = "EN EJECUCION",
                ["en ejecucion"] = "EN EJECUCION",
                ["en ejecución"] = "EN EJECUCION",
                ["en proceso de finalizacion"] = "EN CIERRE",
                ["en proceso de finalización"] = "EN CIERRE",
                ["finalizado"] = "FINALIZADO",
                ["criterio final"] = "FINALIZADO"
            };

            if (manualMap.TryGetValue(norm, out var canonicalName))
            {
                var canonicalNorm = Levenshtein.NormalizeForComparison(canonicalName);

                var exact = states.FirstOrDefault(s =>
                    Levenshtein.NormalizeForComparison(s.Name) == canonicalNorm);

                if (exact is not null)
                    return exact.Id;
            }

            ProjectState? best = null;
            double bestSim = 0.0;

            foreach (var s in states)
            {
                var nameNorm = Levenshtein.NormalizeForComparison(s.Name);
                var sim = Levenshtein.SimilarityPercentage(nameNorm, norm, normalize: false);

                if (sim > bestSim)
                {
                    bestSim = sim;
                    best = s;
                }
            }

            if (best is not null)
            {
                Console.WriteLine($"[IMPORT][State] '{stateName}' -> '{best.Name}' ({bestSim:F2}%)");
                return best.Id;
            }

            return null;
        }

        private static int? ResolveAcademicPeriodId(
            List<ExternalAcademicPeriodModel> periods,
            string? periodLabel)
        {
            if (periods is null || periods.Count == 0)
                return null;

            if (string.IsNullOrWhiteSpace(periodLabel))
                return null;

            var target = Levenshtein.NormalizeForComparison(periodLabel);

            ExternalAcademicPeriodModel? best = null;
            double bestScore = 0.0;

            foreach (var p in periods)
            {
                var nameNorm = Levenshtein.NormalizeForComparison(p.Name);
                var score = Levenshtein.SimilarityPercentage(nameNorm, target, normalize: false);

                if (score > bestScore)
                {
                    bestScore = score;
                    best = p;
                }
            }

            if (best is null || bestScore < 70.0)
                return null;

            Console.WriteLine($"[IMPORT][Period] '{periodLabel}' -> '{best.Name}' ({bestScore:F2}%)");
            return best.PeriodId;
        }


        private static bool IsFinalizedProjectState(List<ProjectState> states, int? projectStateId)
        {
            if (!projectStateId.HasValue || projectStateId.Value <= 0) return false;
            if (states is null || states.Count == 0) return false;

            var state = states.FirstOrDefault(s => s.Id == projectStateId.Value);
            if (state is null || string.IsNullOrWhiteSpace(state.Name)) return false;

            var norm = Levenshtein.NormalizeForComparison(state.Name);
            return norm == "finalizado";
        }

        private static int CalculateBaseVisitCount(int durationInMonths)
        {
            if (durationInMonths <= 0) return 0;

            // MISMA REGLA QUE CreateFullAsync: división entera
            // (12 meses => 2 visitas, 11 meses => 1 visita)
            return durationInMonths / 6;
        }

        private static int CountValidItems<T>(IEnumerable<T>? items, Func<T, bool> isValid)
        {
            if (items is null) return 0;

            var count = 0;
            foreach (var item in items)
            {
                if (isValid(item)) count++;
            }
            return count;
        }

        private static List<Visit> BuildPlannedVisits(Project projectEntity, int totalVisits, DateTime createdAtUtc)
        {
            var list = new List<Visit>(Math.Max(0, totalVisits));

            for (int i = 0; i < totalVisits; i++)
            {
                list.Add(new Visit
                {
                    Project = projectEntity,
                    VisitStateId = 1,           // mismo estado que CreateFullAsync para "planificada"
                    AcademicPeriodId = 0,       // se asigna DESPUÉS (obligatorio antes de AddRange)
                    FundingDocument = null,
                    Document = null,
                    ProgressDocument = null,
                    PerformedByUserId = null,
                    ScheduledDate = null,
                    PerformedDate = null,
                    CreatedAt = createdAtUtc
                });
            }

            return list;
        }

        private static void ApplyUpdate(Project target, UpdateProjectRequestDTO dto)
        {
            target.ProjectName = dto.ProjectName;
            target.ProjectTypeId = dto.ProjectTypeId;
            target.ProjectStateId = dto.ProjectStateId;
            target.StartDate = dto.StartDate;
            target.DurationInMonths = dto.DurationInMonths;
            target.TentativeEndDate = dto.TentativeEndDate;
            target.RealEndDate = dto.RealEndDate;
            target.ConvocationId = dto.ConvocationId;
        }
    }
}