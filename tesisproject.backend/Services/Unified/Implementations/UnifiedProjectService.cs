using Microsoft.EntityFrameworkCore;
using tesisproject.backend.Repositories.Unified.Interfaces;
using tesisproject.backend.Services.Interfaces;
using tesisproject.backend.Services.Unified.Interfaces;
using tesisproject.backend.UnitOfWork.Unified.Interfaces;
using tesisproject.shared.Auth;
using tesisproject.shared.Common.External;
using tesisproject.shared.Common.Utils;
using tesisproject.shared.DTOs.Auth;
using tesisproject.shared.DTOs.Catalog.ResearchCategory.Response;
using tesisproject.shared.DTOs.External;
using tesisproject.shared.DTOs.Matrices.Import;
using tesisproject.shared.DTOs.Matrices.Response;
using tesisproject.shared.DTOs.Project.Request;
using tesisproject.shared.DTOs.Project.Response;
using tesisproject.shared.DTOs.ProjectObjective.Response;
using tesisproject.backend.Data.UnifiedEntities.Catalogs;
using tesisproject.backend.Data.UnifiedEntities.Core;
using tesisproject.shared.Entities.External;
using tesisproject.shared.Enums;
using tesisproject.shared.Responses;
using tesisproject.shared.Errors;

namespace tesisproject.backend.Services.Unified.Implementations
{
    public class UnifiedProjectService : IUnifiedProjectService
    {
        private readonly IUnifiedUnitOfWork _uow;
        private readonly IUnifiedIdentityProvisioningService _identity;
        private readonly ICurrentUserService _currentUser;
        private readonly IUnifiedResearchCategoryService _researchCategoryService;
        private readonly ILogger<UnifiedProjectService> _logger;
        private readonly IExternalDirectoryClient _externalDirectory;
        private readonly IExternalDistributivosService _externalDistributivosRaw;

        private const int DEFAULT_CONVOCATION_ID = 1;

        private const string ProjectsRetrievedMessage = "Projects retrieved";
        private const string ProjectRetrievedMessage = "Project retrieved";
        private const string ProjectsByTypeRetrievedMessage = "Projects by type retrieved";

        private const string ProjectUpdatedMessage = "Project updated";
        private const string ProjectDeletedMessage = "Project deleted";
        private const string ProjectDetailRetrievedMessage = "Project detail retrieved";
        private const string ProjectResearchCategoriesUpdatedMessage = "Project research categories updated.";




        private static readonly Dictionary<string, string> ProjectStateManualMap = new()
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

        public UnifiedProjectService(
            IUnifiedUnitOfWork uow,
            ICurrentUserService currentUser,
            IUnifiedResearchCategoryService researchCategoryService,
            ILogger<UnifiedProjectService> logger,
            IExternalDirectoryClient externalDirectory,
            IExternalDistributivosService externalDistributivosRaw,
            IUnifiedIdentityProvisioningService identity)
        {
            _uow = uow;
            _identity = identity;
            _currentUser = currentUser;
            _researchCategoryService = researchCategoryService;
            _logger = logger;
            _externalDirectory = externalDirectory;
            _externalDistributivosRaw = externalDistributivosRaw;
        }

        private const string ExternalResearcherRole = "ExternalResearcher";

        private const string DefaultHardcodedPassword = "aaaaaqqq1231231";

        private const string LegacyMatrixDocumentPath = "legacy-matrix";

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
                {
                    return FailNotFound<List<ProjectListResponseDTO>>(
                        ErrorMessages.Project.NoneFound,
                        ErrorCodes.Project.NoneFound);
                }

                return ServiceResult<List<ProjectListResponseDTO>>.Ok(data, ProjectsRetrievedMessage);
            }
            catch (Exception ex)
            {
                return FailUnexpected<List<ProjectListResponseDTO>>(ex.Message);
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
                    ? FailNotFound<ProjectListResponseDTO>(
                        ErrorMessages.Project.NotFound,
                        ErrorCodes.Project.NotFound)
                    : ServiceResult<ProjectListResponseDTO>.Ok(dto, ProjectRetrievedMessage);
            }
            catch (Exception ex)
            {
                return FailUnexpected<ProjectListResponseDTO>(ex.Message);
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

                return ServiceResult<List<ProjectListResponseDTO>>.Ok(data, ProjectsByTypeRetrievedMessage);
            }
            catch (Exception ex)
            {
                return FailUnexpected<List<ProjectListResponseDTO>>(ex.Message);
            }
        }

        // ================= WRITES =================

        public async Task<ServiceResult<ProjectListResponseDTO>> CreateAsync(
            UnifiedAddProjectRequestDTO dto,
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

                var faculty = await UnifiedAcademicReferencePreparation.FacultyAsync(_uow, dto.ExternalFacultyId, ct);
                if (!faculty.Success) return UnifiedAcademicReferencePreparation.Relay<ProjectListResponseDTO, tesisproject.backend.Data.UnifiedEntities.Articles.Faculty>(faculty);
                var entity = MapToEntity(dto, createdByUserId, faculty.Data!.FacultyId);
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

        public async Task<ServiceResult<NoContent>> UpdateAsync(int id, UpdateProjectRequestDTO dto, CancellationToken ct = default)
        {
            try
            {
                var current = await _uow.Projects.GetByIdAsync(new object[] { id }, ct);
                if (current is null)
                {
                    return FailNotFound<NoContent>(
                        ErrorMessages.Project.NotFound,
                        ErrorCodes.Project.NotFound);
                }

                ApplyUpdate(current, dto);
                _uow.Projects.Update(current);
                await _uow.SaveChangesAsync(ct);

                return ServiceResult<NoContent>.Ok(new NoContent(), ProjectUpdatedMessage);
            }
            catch (DbUpdateConcurrencyException)
            {
                return FailConflict<NoContent>(ErrorMessages.Common.PersistenceConflict);
            }
            catch (DbUpdateException dbex)
            {
                return FailConflict<NoContent>(dbex.InnerException?.Message ?? dbex.Message);
            }
            catch (Exception ex)
            {
                return FailUnexpected<NoContent>(ex.Message);
            }
        }

        public async Task<ServiceResult<NoContent>> DeleteAsync(int id, CancellationToken ct = default)
        {
            try
            {
                var current = await _uow.Projects.GetByIdAsync(new object[] { id }, ct);
                if (current is null)
                {
                    return FailNotFound<NoContent>(
                        ErrorMessages.Project.NotFound,
                        ErrorCodes.Project.NotFound);
                }

                _uow.Projects.Remove(current);
                await _uow.SaveChangesAsync(ct);

                return ServiceResult<NoContent>.Ok(new NoContent(), ProjectDeletedMessage);
            }
            catch (DbUpdateException dbex)
            {
                return FailConflict<NoContent>(dbex.InnerException?.Message ?? dbex.Message);
            }
            catch (Exception ex)
            {
                return FailUnexpected<NoContent>(ex.Message);
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
                        ProjectOriginTypeId = p.ProjectOriginTypeId,
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

                if (dto is null)
                {
                    return FailNotFound<ProjectDetailResponseDTO>(
                        ErrorMessages.Project.NotFound,
                        ErrorCodes.Project.NotFound);
                }

                dto.Documents = await _uow.ProjectDocuments
                    .Query(asNoTracking: true)
                    .Where(pd => pd.ProjectId == projectId)
                    .Select(pd => new ProjectDocumentRefDTO
                    {
                        DocumentId = pd.DocumentId,
                        DocumentTypeId = pd.Document.DocumentTypeId
                    })
                    .ToListAsync(ct);

                return ServiceResult<ProjectDetailResponseDTO>.Ok(dto, ProjectDetailRetrievedMessage);
            }
            catch (Exception ex)
            {
                return FailUnexpected<ProjectDetailResponseDTO>(ex.Message);
            }
        }

        public async Task<ServiceResult<ProjectDetailResponseDTO>> CreateFullAsync(
            AddProjectFullRequestDTO request,
            CancellationToken ct = default)
        {
            void PhaseLog(string phase, string message)
                => _logger.LogDebug("Unified project phase {Phase}", phase);

            try
            {
                if (request is null) return FailValidation<ProjectDetailResponseDTO>(ErrorMessages.Common.RequestRequired, ErrorCodes.Common.RequestRequired);
                var academic = await PrepareFullProjectFacultyAsync(request, ct);
                if (!academic.Success) return UnifiedAcademicReferencePreparation.Relay<ProjectDetailResponseDTO, tesisproject.backend.Data.UnifiedEntities.Articles.Faculty>(academic);
                var localFacultyId = academic.Data!.FacultyId;
                var p = request.Project!;
                var d = request.ProjectDocumentData;
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

                if (localFacultyId <= 0)
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
                    .Where(x => x.FacultyId == localFacultyId &&
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

                if (string.IsNullOrWhiteSpace(p.ProjectName) ||
                    !await _uow.ProjectTypes.ExistsAsync(x => x.Id == p.ProjectTypeId, ct) ||
                    !await _uow.ProjectStates.ExistsAsync(x => x.Id == ProjectStateIds.EnEjecucion, ct) ||
                    !await _uow.Convocations.ExistsAsync(x => x.Id == p.ConvocationId, ct))
                    return FailValidation<ProjectDetailResponseDTO>(ErrorMessages.Common.InvalidRequest, ErrorCodes.Common.InvalidRequest);
                foreach (var member in request.GroupMembers!)
                    if (!await _uow.MemberRoleTypes.ExistsAsync(x => x.Id == member.MemberRole, ct))
                        return FailValidation<ProjectDetailResponseDTO>(ErrorMessages.Common.InvalidRequest, ErrorCodes.Common.InvalidRequest);
                foreach (var id in p.ResearchCategoryIds ?? [])
                    if (!await _uow.ResearchCategories.ExistsAsync(x => x.Id == id, ct))
                        return FailValidation<ProjectDetailResponseDTO>(ErrorMessages.Common.InvalidRequest, ErrorCodes.Common.InvalidRequest);
                foreach (var id in request.ExternalResearcherIds ?? [])
                    if (id > 0 && !await _uow.ExternalResearchers.ExistsAsync(x => x.ExternalResearcherId == id, ct))
                        return FailValidation<ProjectDetailResponseDTO>(ErrorMessages.Common.InvalidRequest, ErrorCodes.Common.InvalidRequest);
                if (d is not null && d.DocumentId > 0 && !await _uow.Documents.ExistsAsync(x => x.DocumentId == d.DocumentId, ct))
                    return FailValidation<ProjectDetailResponseDTO>(ErrorMessages.Common.InvalidRequest, ErrorCodes.Common.InvalidRequest);
                if (!await _uow.ProjectOriginTypes.ExistsAsync(x => x.Id == p.ProjectOriginTypeId, ct))
                    return FailValidation<ProjectDetailResponseDTO>(ErrorMessages.Common.InvalidRequest, ErrorCodes.Common.InvalidRequest);
                foreach (var budget in (request.Budgets ?? []).Where(b => b.FundingTypeId > 0 && b.InitialAmount > 0))
                    if (!await _uow.FundingTypes.ExistsAsync(x => x.Id == budget.FundingTypeId, ct))
                        return FailValidation<ProjectDetailResponseDTO>(ErrorMessages.Common.InvalidRequest, ErrorCodes.Common.InvalidRequest);
                foreach (var objective in request.Objectives ?? [])
                    if (!await _uow.ObjectiveTypes.ExistsAsync(x => x.Id == objective.ObjectiveTypeId, ct))
                        return FailValidation<ProjectDetailResponseDTO>(ErrorMessages.Common.InvalidRequest, ErrorCodes.Common.InvalidRequest);
                    var registerDtos = request.GroupMembers!
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

                    var ensureResult = await _identity.EnsureSelectedAsync(registerDtos, ct);

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

                PhaseLog("Fase 2 - Group", "Creando grupo...");

                var groupEntity = new Group
                {
                    GroupTypeId = GroupTypeIds.Integrantes,
                    Name = generatedCode
                };

                await _uow.Groups.AddAsync(groupEntity, ct);

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
                    FacultyId = localFacultyId,
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

        private static Project MapToEntity(UnifiedAddProjectRequestDTO dto, int userId, int localFacultyId) => new()
        {
            ProjectCode = dto.ProjectCode ?? string.Empty,
            CreatedByUserId = userId,
            ProjectTypeId = dto.ProjectTypeId,
            ProjectStateId = dto.ProjectStateId,
            ProjectGroupId = dto.ProjectGroupId,
            ProjectName = dto.ProjectName,
            ApprovalDate = dto.ApprovalDate,
            StartDate = dto.StartDate,
            TentativeEndDate = dto.StartDate?.AddMonths(dto.DurationInMonths),
            ExecutionPercentage = 0,
            DurationInMonths = dto.DurationInMonths,
            FacultyId = localFacultyId,
            ConvocationId = dto.ConvocationId,
            ProjectOriginTypeId = dto.ProjectOriginTypeId
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
                    Objective = e.Objective,
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
                    return FailNotFound<NoContent>(
                        ErrorMessages.Project.NotFound,
                        ErrorCodes.Project.NotFound);
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

                return ServiceResult<NoContent>.Ok(new NoContent(), ProjectResearchCategoriesUpdatedMessage);
            }
            catch (Exception ex)
            {
                return FailUnexpected<NoContent>(ex.Message);
            }
        }

        internal sealed record PreparedInstitutionalMember(RegisterRequest Request, int MemberRoleId, bool Historical)
        {
            internal int IdUser { get; set; }
        }

        internal static List<PreparedInstitutionalMember> SelectImportedMembers(
            ImportedProjectDTO dto, IReadOnlyList<ExternalUserProfileModel> directoryCache, CancellationToken ct)
        {
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

            var matchedCoordinators = ResolvePeople(dto.Coordinators, discardedCoordinators).Take(2).ToList();
            var matchedAlternates = ResolvePeople(dto.AlternateCoordinators, discardedAlternates).Take(2).ToList();

            dto.CoordinatorDiscardedTokens.AddRange(discardedCoordinators);
            dto.AlternateCoordinatorDiscardedTokens.AddRange(discardedAlternates);


            var selected = new List<PreparedInstitutionalMember>();
            void Append(List<ExternalUserProfileModel> profiles, int role)
            {
                for (var i = 0; i < profiles.Count; i++)
                {
                    var p = profiles[i];
                    selected.Add(new(new RegisterRequest { Email = p.Email, Username = p.Document,
                        Password = DefaultHardcodedPassword, AspUserId = p.AspId, Role = AppRoles.Coordinador },
                        role, profiles.Count >= 2 && i == 0));
                }
            }
            Append(matchedCoordinators, MemberRoleTypeIds.Coordinador);
            Append(matchedAlternates, MemberRoleTypeIds.Subrogante);
            return selected;
        }

        private async Task InsertGroupMembersFromDirectoryAsync(Group groupEntity,
            IReadOnlyList<PreparedInstitutionalMember> selected, CancellationToken ct)
        {
            // Provisioning has completed for the entire batch before any aggregate is tracked.
            var now = DateTime.UtcNow;
            foreach (var member in selected)
                await _uow.GroupMembers.AddAsync(new GroupMember { Group = groupEntity, UserId = member.IdUser,
                    MemberRoleId = member.MemberRoleId, JoinedAt = now, LeftAt = member.Historical ? now : null }, ct);
        }

        public async Task<ServiceResult<int>> ImportFromMatrixAsync(
            ProjectMatrixUploadSummaryDTO summary,
            CancellationToken ct = default)
        {
            void PhaseLog(string phase, string message)
            {
                _logger.LogDebug("Unified import phase {Phase}", phase);
            }

            try
            {
                PhaseLog("Init", "Starting ImportFromMatrixAsync...");

                var localTerms = await UnifiedAcademicCatalogReads.TermsAsync(_uow, ct);
                if (!localTerms.Success)
                    return UnifiedAcademicReferencePreparation.Relay<int, List<tesisproject.backend.Data.UnifiedEntities.Articles.AcademicTerm>>(localTerms);
                var academicPeriodsCache = UnifiedAcademicCatalogReads.Periods(localTerms.Data!);
                var localPeriodIds = localTerms.Data!.ToDictionary(x => x.ExternalPeriodId!.Value, x => x.AcademicTermId);

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

                var localFaculties = await UnifiedAcademicCatalogReads.FacultiesAsync(_uow, ct);
                if (!localFaculties.Success)
                    return UnifiedAcademicReferencePreparation.Relay<int, List<tesisproject.backend.Data.UnifiedEntities.Articles.Faculty>>(localFaculties);
                var externalFacultiesCache = UnifiedAcademicCatalogReads.FacultyRoots(localFaculties.Data!);
                var localFacultyIds = localFaculties.Data!.ToDictionary(x => x.ExternalFacultyId!.Value, x => x.FacultyId);

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

                var prepared = new List<(ImportedProjectDTO Dto, PreparedImportAcademicReferences Academic,
                    int ConvocationId, Convocation? Convocation, int StateId, List<PreparedInstitutionalMember> Members)>();
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

                    var academic = await PrepareImportAcademicReferencesAsync(dto, externalFacultiesCache, academicPeriodsCache, ct, localFacultyIds, localPeriodIds);
                    if (!academic.Success) return UnifiedAcademicReferencePreparation.Relay<int, PreparedImportAcademicReferences>(academic);
                    int? facultyId = academic.Data!.FacultyId;
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

                    if (!projectStateId.HasValue || projectStateId.Value <= 0) { skippedCount++; continue; }
                    if (!await _uow.Convocations.ExistsAsync(c => c.Id == convocationId, ct))
                        return FailValidation<int>(ErrorMessages.Common.InvalidRequest, ErrorCodes.Common.InvalidRequest);
                    var origin = (dto.ProjectCode ?? string.Empty).Trim().StartsWith("PE", StringComparison.OrdinalIgnoreCase)
                        ? ProjectOriginTypeIds.Externo : ProjectOriginTypeIds.Interno;
                    if (!await _uow.ProjectOriginTypes.ExistsAsync(x => x.Id == origin, ct) ||
                        !await _uow.ProjectTypes.ExistsAsync(x => x.Id == ProjectTypeIds.Aplicada, ct) ||
                        !await _uow.ProjectStates.ExistsAsync(x => x.Id == projectStateId.Value, ct) ||
                        (dto.HasExternalParticipants && !await _uow.ExternalResearchers.ExistsAsync(x => x.ExternalResearcherId == 1, ct)) ||
                        (dto.AssignedValue > 0 && !await _uow.FundingTypes.ExistsAsync(x => x.Id == FundingTypeIds.Interno, ct)) ||
                        (!string.IsNullOrWhiteSpace(dto.GeneralObjective) && !await _uow.ObjectiveTypes.ExistsAsync(x => x.Id == ObjectiveTypeIds.General, ct)))
                        return FailValidation<int>(ErrorMessages.Common.InvalidRequest, ErrorCodes.Common.InvalidRequest);
                    var requiredDocumentTypes = new List<int>();
                    if (academic.Data!.Visits.Count > 0) requiredDocumentTypes.Add(DocumentTypeIds.ResolucionVisita);
                    if (dto.Extensions?.Count > 0) requiredDocumentTypes.Add(DocumentTypeIds.ResolucionProrroga);
                    var finalDocumentInput = dto.Documents?.FirstOrDefault(d => d.DocumentType == "RESOLUCION INFORME FINAL HCU");
                    if (finalDocumentInput is not null)
                        requiredDocumentTypes.Add(ResolveDocumentTypeId(documentTypes, finalDocumentInput.DocumentType) ?? DocumentTypeIds.ResolucionInformeFinal);
                    if (requiredDocumentTypes.Any(id => !documentTypes.Any(d => d.Id == id)))
                        return FailValidation<int>(ErrorMessages.Common.InvalidRequest, ErrorCodes.Common.InvalidRequest);
                    prepared.Add((dto, academic.Data!, convocationId, convocationEntity, projectStateId.Value,
                        SelectImportedMembers(dto, directoryCache, ct)));
                }

                var selectedMembers = prepared.SelectMany(r => r.Members).ToList();
                var provisioned = await _identity.EnsureSelectedAsync(selectedMembers.Select(m => m.Request), ct);
                if (!provisioned.Success) return UnifiedAcademicReferencePreparation.Relay<int, List<int>>(provisioned);
                for (var i = 0; i < selectedMembers.Count; i++) selectedMembers[i].IdUser = provisioned.Data![i];

                // Preserve one domain commit for the batch. No provisioning below this point.
                foreach (var row in prepared)
                {
                    var dto = row.Dto;
                    int? facultyId = row.Academic.FacultyId;
                    int? projectStateId = row.StateId;
                    var convocationId = row.ConvocationId;
                    var convocationEntity = row.Convocation;
                    var groupEntity = new Group
                    {
                        GroupTypeId = GroupTypeIds.Integrantes,
                        Name = dto.ProjectCode!
                    };

                    await _uow.Groups.AddAsync(groupEntity, ct);
                    PhaseLog("Row",
                        $"Group queued for insert: Name={groupEntity.Name}");

                    await InsertGroupMembersFromDirectoryAsync(groupEntity, row.Members, ct);

                    PhaseLog("Row",
                        $"Creating project: Code={dto.ProjectCode}, Number={dto.Number}, Name={dto.ProjectName}");

                    var durationMonths = dto.TermMonths ?? 0;

                    var approvalDoc = (dto.Documents ?? [])
                        .FirstOrDefault(d => d.DocumentType == "APROBACION HCU/CONIN");

                    DateTime? approvalDate = approvalDoc?.Date;

                    var finalResolutionDoc = (dto.Documents ?? [])
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
                        ProjectCode = $"{dto.ProjectCode}-{dto.Number!.Value}",
                        ProjectNumber = dto.Number!.Value,
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

                    var localVisits = row.Academic.ApplyTo(projectEntity, createdByUserId);
                    await _uow.Visits.AddRangeAsync(localVisits, ct);

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

        // ============================
        //   HELPERS
        // ============================

        private static string ResolveAppRoleFromMemberRole(int memberRoleId)
        {
            return memberRoleId switch
            {
                MemberRoleTypeIds.Coordinador or MemberRoleTypeIds.Subrogante => AppRoles.Coordinador,
                _ => AppRoles.User
            };
        }

        private async Task<int?> GetExistingActorUserIdAsync(CancellationToken ct)
        {
            var currentUserId = _currentUser.GetRequiredUserId();
            var user = await _uow.AppUsers.GetByLocalIdAsync(currentUserId, ct);
            return user?.IdUser;
        }

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
            var byId = allCategories.ToDictionary(c => c.Id);

            bool IsAncestor(int ancestorId, int childId)
            {
                var currentId = childId;

                while (byId.TryGetValue(currentId, out var cat) && cat.ParentCategoryId.HasValue)
                {
                    if (cat.ParentCategoryId.Value == ancestorId)
                        return true;

                    currentId = cat.ParentCategoryId.Value;
                }

                return false;
            }

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

        // Read-only stage for CreateFullAsync. The entry point still requires Identity work.
        internal async Task<ServiceResult<tesisproject.backend.Data.UnifiedEntities.Articles.Faculty>> PrepareFullProjectFacultyAsync(
            AddProjectFullRequestDTO request, CancellationToken ct = default)
        {
            void PhaseLog(string phase, string message) => _logger.LogDebug("{Phase}: {Message}", phase, message);
                if (request.Project is null)
                {
                    PhaseLog("Fase 0 - Request", "Project payload is null");
                    return FailValidation<tesisproject.backend.Data.UnifiedEntities.Articles.Faculty>(
                        ErrorMessages.Project.InvalidProjectData,
                        ErrorCodes.Project.InvalidProjectData);
                }

                var p = request.Project;

                if (request.GroupMembers is null || request.GroupMembers.Count == 0)
                {
                    PhaseLog("Fase 0 - Request", "GroupMembers is null or empty");
                    return FailValidation<tesisproject.backend.Data.UnifiedEntities.Articles.Faculty>(
                        ErrorMessages.Project.AtLeastOneGroupMemberRequired,
                        ErrorCodes.Project.AtLeastOneGroupMemberRequired);
                }

                var principalCoordinatorEmail = request.GroupMembers
                    .FirstOrDefault(m => m.MemberRole == MemberRoleTypeIds.Coordinador)
                    ?.Email;

                if (string.IsNullOrWhiteSpace(principalCoordinatorEmail))
                {
                    PhaseLog("Fase 0 - Request", "Principal coordinator email not found");
                    return FailValidation<tesisproject.backend.Data.UnifiedEntities.Articles.Faculty>(
                        ErrorMessages.Project.PrincipalCoordinatorRequired,
                        ErrorCodes.Project.PrincipalCoordinatorRequired);
                }

                var projectStartDate = p.StartDate;

                var profRes = await _externalDirectory.GetByEmailsAsync(new[] { principalCoordinatorEmail.Trim() }, ct);
                if (!profRes.Success || profRes.Data is null || profRes.Data.Count == 0)
                {
                    PhaseLog("Fase 1.2 - External Faculty Resolve",
                        $"External profile not found for email={principalCoordinatorEmail}. Error={profRes.Error}, Msg={profRes.Message}");

                    return FailNotFound<tesisproject.backend.Data.UnifiedEntities.Articles.Faculty>(
                        ErrorMessages.Project.PrincipalCoordinatorExternalProfileNotFound,
                        ErrorCodes.Project.PrincipalCoordinatorExternalProfileNotFound);
                }

                var profile = profRes.Data[0];

                var periodsRes = await UnifiedAcademicCatalogReads.PeriodsAsync(_uow, ct);
                if (!periodsRes.Success)
                    return UnifiedAcademicReferencePreparation.Relay<tesisproject.backend.Data.UnifiedEntities.Articles.Faculty, List<ExternalAcademicPeriodModel>>(periodsRes);
                var periods = periodsRes.Data!;

                var distRawRes = await _externalDistributivosRaw.GetDistributivosByCorreosAsync(
                    new[] { principalCoordinatorEmail.Trim() },
                    ct);

                if (!distRawRes.Success || distRawRes.Data is null || distRawRes.Data.Count == 0)
                {
                    PhaseLog("Fase 1.2 - External Faculty Resolve",
                        $"No distributivos found for email={principalCoordinatorEmail}. Error={distRawRes.Error}, Msg={distRawRes.Message}");

                    return FailNotFound<tesisproject.backend.Data.UnifiedEntities.Articles.Faculty>(
                        ErrorMessages.Project.NoDistributivoForPrincipalCoordinator,
                        ErrorCodes.Project.NoDistributivoForPrincipalCoordinator);
                }

                var distributivos = distRawRes.Data;

                if (projectStartDate is null)
                {
                    PhaseLog("Fase 1.2 - External Faculty Resolve", "Project StartDate is null");
                    return FailValidation<tesisproject.backend.Data.UnifiedEntities.Articles.Faculty>(
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

                    return FailValidation<tesisproject.backend.Data.UnifiedEntities.Articles.Faculty>(
                        ErrorMessages.Project.FacultyCareerResolutionFailed,
                        ErrorCodes.Project.FacultyCareerResolutionFailed);
                }


            return await UnifiedAcademicReferencePreparation.RootForCareerAsync(_uow, selected.FacultyCareerId, ct);
        }

        // Does not create Project/Visit/Document or invoke Identity. Resolve the complete row first.
        internal async Task<ServiceResult<PreparedImportAcademicReferences>> PrepareImportAcademicReferencesAsync(
            ImportedProjectDTO dto, List<ExternalFacultyDTO> faculties,
            List<ExternalAcademicPeriodModel> periods, CancellationToken ct = default,
            IReadOnlyDictionary<int, int>? localFacultyIds = null, IReadOnlyDictionary<int, int>? localPeriodIds = null)
        {
            ct.ThrowIfCancellationRequested();
            var externalFacultyId = ResolveFacultyExternalId(faculties, dto.Faculty ?? string.Empty);
            if (!externalFacultyId.HasValue)
                return UnifiedAcademicReferencePreparation.Invalid<PreparedImportAcademicReferences>(nameof(dto.Faculty));
            int facultyId;
            if (localFacultyIds is not null)
            {
                if (!localFacultyIds.TryGetValue(externalFacultyId.Value, out facultyId))
                    return ServiceResult<PreparedImportAcademicReferences>.Fail(ErrorMessages.AcademicReferences.FacultyNotSynchronized,
                        ErrorType.NotFound, ErrorCodes.AcademicReferences.FacultyNotSynchronized);
            }
            else
            {
                var faculty = await UnifiedAcademicReferencePreparation.FacultyAsync(_uow, externalFacultyId.Value, ct);
                if (!faculty.Success) return UnifiedAcademicReferencePreparation.Relay<PreparedImportAcademicReferences, tesisproject.backend.Data.UnifiedEntities.Articles.Faculty>(faculty);
                facultyId = faculty.Data!.FacultyId;
            }

            var visits = new List<PreparedImportedVisit>();
            var executed = (dto.VisitPeriods ?? []).Where(v => v.HasReport && !string.IsNullOrWhiteSpace(v.RawValue)).ToList();
            if (executed.Count > 0)
            {
                var defaultExternalPeriodId = periods.OrderByDescending(p => p.PeriodId).Select(p => p.PeriodId).FirstOrDefault();
                if (defaultExternalPeriodId <= 0)
                    return ServiceResult<PreparedImportAcademicReferences>.Fail(ErrorMessages.AcademicReferences.AcademicTermNotSynchronized,
                        ErrorType.NotFound, ErrorCodes.AcademicReferences.AcademicTermNotSynchronized);
                // Resolve all visits as a batch; import callers reuse the initial catalog maps.
                var resolved = executed.Select(visit => new { Visit = visit,
                    ExternalId = ResolveExternalAcademicPeriodId(periods, visit.PeriodLabel) ?? defaultExternalPeriodId }).ToList();
                if (localPeriodIds is null)
                {
                    var ids = resolved.Select(x => x.ExternalId).Distinct().ToList();
                    localPeriodIds = await _uow.AcademicTerms.Query()
                        .Where(x => x.ExternalPeriodId.HasValue && ids.Contains(x.ExternalPeriodId.Value))
                        .ToDictionaryAsync(x => x.ExternalPeriodId!.Value, x => x.AcademicTermId, ct);
                }
                foreach (var item in resolved)
                {
                    if (!localPeriodIds.TryGetValue(item.ExternalId, out var termId))
                        return ServiceResult<PreparedImportAcademicReferences>.Fail(ErrorMessages.AcademicReferences.AcademicTermNotSynchronized,
                            ErrorType.NotFound, ErrorCodes.AcademicReferences.AcademicTermNotSynchronized);
                    visits.Add(new PreparedImportedVisit(termId, item.Visit.RawValue!.Trim()));
                }
            }
            return ServiceResult<PreparedImportAcademicReferences>.Ok(new(facultyId, visits));
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
                var candidate = c.Code;
                if (string.IsNullOrWhiteSpace(candidate))
                    candidate = c.Name;

                if (string.IsNullOrWhiteSpace(candidate))
                    continue;

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

            if (ProjectStateManualMap.TryGetValue(norm, out var canonicalName))
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

        private static int? ResolveExternalAcademicPeriodId(
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

        private static ServiceResult<T> FailUnauthorized<T>()
            => ServiceResult<T>.Fail(
                ErrorMessages.Auth.UserNotAuthenticated,
                ErrorType.Unauthorized,
                ErrorCodes.Auth.UserNotAuthenticated);

        private static ServiceResult<T> FailActorUserNotFound<T>()
            => ServiceResult<T>.Fail(
                ErrorMessages.Auth.ActorUserNotFound,
                ErrorType.NotFound,
                ErrorCodes.Auth.ActorUserNotFound);

        private static ServiceResult<T> FailValidation<T>(
            string message,
            string errorCode,
            Dictionary<string, string[]>? validation = null)
            => ServiceResult<T>.Fail(
                message,
                ErrorType.Validation,
                errorCode,
                validation);

        private static ServiceResult<T> FailNotFound<T>(string message, string errorCode)
            => ServiceResult<T>.Fail(
                message,
                ErrorType.NotFound,
                errorCode);

        private static ServiceResult<T> FailUnexpected<T>(string message, string? errorCode = null)
            => ServiceResult<T>.Fail(
                message,
                ErrorType.Unexpected,
                errorCode ?? ErrorCodes.Common.UnexpectedError);

        private static ServiceResult<T> FailConflict<T>(string message)
            => ServiceResult<T>.Fail(
                message,
                ErrorType.Conflict,
                ErrorCodes.Common.PersistenceConflict);

        private static ServiceResult<TTarget> RelayFailure<TTarget>(
            string? message,
            ErrorType error,
            string? errorCode,
            Dictionary<string, string[]>? validation)
        {
            var normalizedError = error == ErrorType.None
                ? ErrorType.Unexpected
                : error;

            return ServiceResult<TTarget>.Fail(
                message ?? ErrorMessages.Common.UnexpectedError,
                normalizedError,
                errorCode ?? (normalizedError == ErrorType.Unexpected ? ErrorCodes.Common.UnexpectedError : null),
                validation);
        }
    }
}
