using Microsoft.EntityFrameworkCore;
using tesisproject.backend.Repositories.Interfaces;
using tesisproject.backend.Services.Interfaces;
using tesisproject.backend.UnitOfWork.Interfaces;
using tesisproject.shared.DTOs.Project.Request;
using tesisproject.shared.DTOs.Project.Response;
using tesisproject.shared.DTOs.ProjectObjective.Response;
using tesisproject.shared.Entities.Catalogs;
using tesisproject.shared.Entities.Core;
using tesisproject.shared.Responses;

namespace tesisproject.backend.Services.Implementations
{
    public class ProjectService : IProjectService
    {
        private readonly IUnitOfWork _uow;
        private readonly ICatalogRepository<AcademicPeriod> _periods;

        public ProjectService(IUnitOfWork uow, ICatalogRepository<AcademicPeriod> periods)
        {
            _uow = uow;
            _periods = periods;
        }


        // ================= READS =================

        public async Task<ServiceResult<List<ProjectListResponseDTO>>> GetAllAsync(CancellationToken ct = default)
        {
            try
            {
                var data = await _uow.Projects
                    .Query()
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
                        FundingTypeId = p.FundingTypeId
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
                        PrincipalCoordinatorFacultyId = p.FacultyId
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
                        PrincipalCoordinatorFacultyId = p.FacultyId
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

        public async Task<ServiceResult<ProjectListResponseDTO>> CreateAsync(AddProjectRequestDTO dto, CancellationToken ct = default)
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

                var entity = MapToEntity(dto);
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
                        PrincipalCoordinatorFacultyId = p.FacultyId
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

                if (dto.ExecutionPercentage.HasValue && current.ExecutionPercentage.HasValue &&
                    dto.ExecutionPercentage.Value < current.ExecutionPercentage.Value)
                {
                    return ServiceResult<NoContent>.Fail("Execution percentage cannot decrease.", ErrorType.Validation);
                }

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
                    .Where(p => p.ProjectId == projectId)
                    .Select(p => new ProjectDetailResponseDTO
                    {
                        // --- General ---
                        ProjectId = p.ProjectId,
                        ProjectCode = p.ProjectCode ?? string.Empty,
                        ProjectName = p.ProjectName ?? string.Empty,
                        ProjectObjectives = MapToDTO(p.ProjectObjectives),

                        // 🔁 Ahora dominios con sus líneas (solo las del proyecto)
                        ResearchDomains = p.ProjectResearchLines
                            .GroupBy(prl => new
                            {
                                prl.ResearchLineType.ResearchDomainTypeId,
                                DomainName = prl.ResearchLineType.ResearchDomainType.Name
                            })
                            .Select(g => new ProjectResearchDomainDTO
                            {
                                ResearchDomainTypeId = g.Key.ResearchDomainTypeId,
                                ResearchDomainTypeName = g.Key.DomainName,
                                ResearchLines = g
                                    .Select(prl => new ProjectResearchLineDTO
                                    {
                                        ResearchLineTypeId = prl.ResearchLineTypeId,
                                        ResearchLineTypeName = prl.ResearchLineType.Name
                                    })
                                    .ToList()
                            })
                            .ToList(),

                        ProjectTypeId = p.ProjectTypeId,
                        ProjectTypeName = p.ProjectType.Name ?? string.Empty,

                        ProjectStateId = p.ProjectStateId,
                        ProjectStateName = p.ProjectState.Name ?? string.Empty,

                        StartDate = p.StartDate,
                        TentativeEndDate = p.TentativeEndDate,
                        RealEndDate = p.RealEndDate,
                        ExecutionPercentage = p.ExecutionPercentage ?? 0,

                        // --- Groups ---
                        ProjectGroupId = p.ProjectGroupId,
                        ProjectGroupName = p.ProjectGroup.Name ?? string.Empty,

                        // --- Budget ---
                        BudgetId = p.Budget != null ? p.Budget.BudgetId : 0,
                        BudgetAmount = p.Budget != null ? p.Budget.InitialAmount : 0,
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
            CancellationToken ct = default)
        {
            try
            {
                if (request.Project is null)
                    return ServiceResult<ProjectDetailResponseDTO>.Fail(
                        "Project payload is required.",
                        ErrorType.Validation);

                var p = request.Project;
                var d = request.ProjectDocumentData;

                // ============================================
                // 1) Validación básica mínima
                // ============================================

                if (p.ProjectTypeId <= 0)
                    return ServiceResult<ProjectDetailResponseDTO>.Fail(
                        "ProjectTypeId is required.",
                        ErrorType.Validation);

                if (p.ProjectStateId <= 0)
                    return ServiceResult<ProjectDetailResponseDTO>.Fail(
                        "ProjectStateId is required.",
                        ErrorType.Validation);

                if (p.CreatedByUserId <= 0)
                    return ServiceResult<ProjectDetailResponseDTO>.Fail(
                        "CreatedByUserId is required.",
                        ErrorType.Validation);

                if (p.FacultyId <= 0)
                    return ServiceResult<ProjectDetailResponseDTO>.Fail(
                        "FacultyId is required.",
                        ErrorType.Validation);

                // ============================================
                // 2) Crear el Group del proyecto (siempre)
                //    - GroupTypeId = 1 (Integrantes de Proyecto)
                //    - Name = ProjectCode
                // ============================================

                var groupEntity = new Group
                {
                    GroupTypeId = 1, // Integrantes de Proyecto
                    Name = p.ProjectCode ?? string.Empty
                };

                await _uow.Groups.AddAsync(groupEntity, ct);

                // ============================================
                // 3) Validación de nombre duplicado (global)
                // ============================================

                var projectName = p.ProjectName ?? string.Empty;

                var duplicate = await _uow.Projects
                    .Query(asNoTracking: true)
                    .AnyAsync(x => x.ProjectName == projectName, ct);

                if (duplicate)
                {
                    return ServiceResult<ProjectDetailResponseDTO>.Fail(
                        "A project with the same name already exists.",
                        ErrorType.Conflict);
                }

                // ============================================
                // 4) Crear entidad Project
                // ============================================

                var projectEntity = new Project
                {
                    ProjectCode = p.ProjectCode ?? string.Empty,
                    CreatedByUserId = p.CreatedByUserId,
                    ProjectTypeId = p.ProjectTypeId,
                    ProjectStateId = p.ProjectStateId,
                    ProjectName = p.ProjectName ?? string.Empty,
                    ResearchLineTypeId = p.ResearchLine,
                    ApprovalDate = p.ApprovalDate,
                    StartDate = p.StartDate,
                    DurationInMonths = p.DurationInMonths,
                    TentativeEndDate = p.StartDate?.AddMonths(p.DurationInMonths),
                    RealEndDate = null,
                    ExecutionPercentage = 0, // nuevo proyecto siempre arranca en 0
                    FacultyId = p.FacultyId,
                    FundingTypeId = p.FundingTypeId,
                    InitialDocumentId = d?.DocumentId // por si viene null
                };

                projectEntity.ProjectGroup = groupEntity;

                // ============================================
                // 5) Asignar miembros al Group
                // ============================================

                if (request.GroupMembers is not null &&
                    request.GroupMembers.Count > 0)
                {
                    var groupMembers = request.GroupMembers
                        .Select(m => new GroupMember
                        {
                            Group = groupEntity,
                            UserId = m.ExternalUserId,
                            MemberRoleId = m.MemberRole,
                            JoinedAt = DateTime.UtcNow
                        })
                        .ToList();

                    await _uow.GroupMembers.AddRangeAsync(groupMembers, ct);
                }

                await _uow.Projects.AddAsync(projectEntity, ct);

                // ============================================
                // 6) Crear Budget (opcional)
                // ============================================

                if (request.Budget is not null)
                {
                    var b = request.Budget;

                    var budgetEntity = new Budget
                    {
                        Project = projectEntity,
                        ApprovedByUserId = b.ApprovedByUserId,
                        InitialAmount = b.InitialAmount,
                        CertifiedAmount = b.CertifiedAmount,
                        ExecutedAmount = b.ExecutedAmount,
                        ApprovedAt = b.ApprovedAt
                    };

                    await _uow.Budgets.AddAsync(budgetEntity, ct);
                }

                // ============================================
                // 7) Crear Objectives y Activities
                // ============================================

                if (request.Objectives is not null && request.Objectives.Count > 0)
                {
                    foreach (var objDto in request.Objectives)
                    {
                        var objectiveEntity = new ProjectObjective
                        {
                            Project = projectEntity,
                            ObjectiveTypeId = objDto.ObjectiveTypeId,
                            Objetive = objDto.Objective,
                            Result = objDto.Result
                        };

                        await _uow.ProjectObjectives.AddAsync(objectiveEntity, ct);

                        if (objDto.Activities is null || objDto.Activities.Count == 0)
                            continue;

                        foreach (var actDto in objDto.Activities)
                        {
                            var activityEntity = new ObjectiveActivity
                            {
                                Objective = objectiveEntity,
                                ActivityResult = actDto.ActivityResult ?? string.Empty,
                                ImprovementAction = actDto.ImprovementAction ?? string.Empty,
                                IsCompleted = actDto.IsCompleted,
                                CreatedAt = DateTime.UtcNow
                            };

                            await _uow.ObjectiveActivities.AddAsync(activityEntity, ct);
                        }
                    }
                }

                // ============================================
                // 8) Generar Visits automáticamente (semestrales)
                // ============================================

                var latest = await _periods
                    .Query(asNoTracking: true)
                    .OrderByDescending(p => p.Id)
                    .FirstOrDefaultAsync(ct);

                if (latest is not null &&
                    projectEntity.StartDate is not null &&
                    projectEntity.DurationInMonths > 0)
                {
                    var academicPeriodId = latest.Id;
                    var visitStateId = 1; // Siempre "Programada"

                    var totalVisits = projectEntity.DurationInMonths / 6;

                    var visits = new List<Visit>();

                    for (int i = 0; i < totalVisits; i++)
                    {
                        var visit = new Visit
                        {
                            Project = projectEntity,
                            VisitStateId = visitStateId,
                            AcademicPeriodId = academicPeriodId,
                            // Si quieres que todas arranquen sin fecha programada:
                            // ScheduledDate = null,
                            ScheduledDate = request.ScheduledDate,
                            PerformedDate = null,
                            CreatedAt = DateTime.UtcNow
                        };

                        visits.Add(visit);
                    }

                    await _uow.Visits.AddRangeAsync(visits, ct);
                }

                // ============================================
                // 9) Guardar todo
                // ============================================

                await _uow.SaveChangesAsync(ct);

                // ============================================
                // 10) Volver a leer el detalle y devolver DTO
                // ============================================

                var detailResult = await GetProjectDetailAsync(projectEntity.ProjectId, ct);
                if (!detailResult.Success)
                    return detailResult;

                return ServiceResult<ProjectDetailResponseDTO>.Ok(
                    detailResult.Data!,
                    "Project with related data created");
            }
            catch (DbUpdateException dbex)
            {
                return ServiceResult<ProjectDetailResponseDTO>.Fail(
                    dbex.InnerException?.Message ?? dbex.Message,
                    ErrorType.Conflict);
            }
            catch (Exception ex)
            {
                return ServiceResult<ProjectDetailResponseDTO>.Fail(
                    ex.Message,
                    ErrorType.Unexpected);
            }
        }



        private static Project MapToEntity(AddProjectRequestDTO dto) => new()
        {
            ProjectCode = dto.ProjectCode ?? string.Empty,
            CreatedByUserId = dto.CreatedByUserId,
            ProjectTypeId = dto.ProjectTypeId,
            ProjectStateId = dto.ProjectStateId,
            ProjectGroupId = dto.ProjectGroupId,
            ProjectName = dto.ProjectName,
            ResearchLineTypeId = dto.ResearchLine,
            StartDate = dto.StartDate,
            TentativeEndDate = dto.StartDate?.AddMonths(dto.DurationInMonths),
            ExecutionPercentage = 0,
            DurationInMonths = dto.DurationInMonths,
            FacultyId = dto.FacultyId,
            FundingTypeId = dto.FundingTypeId
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


        private static void ApplyUpdate(Project target, UpdateProjectRequestDTO dto)
        {
            target.ProjectName = dto.ProjectName;
            target.ProjectTypeId = dto.ProjectTypeId;
            target.ProjectStateId = dto.ProjectStateId;
            target.ProjectGroupId = dto.ProjectGroupId;
            target.InitialDocumentId = dto.InitialDocumentId;
            target.StartDate = dto.StartDate;
            target.TentativeEndDate = dto.TentativeEndDate;
            target.RealEndDate = dto.RealEndDate;
            target.ExecutionPercentage = dto.ExecutionPercentage;

            // ==============================
            //   Sincronizar líneas de investigación
            // ==============================
            if (dto.ResearchLineTypeIds != null)
            {
                var dtoIds = dto.ResearchLineTypeIds
                    .Distinct()
                    .ToList();

                var currentIds = target.ProjectResearchLines
                    .Select(prl => prl.ResearchLineTypeId)
                    .ToList();

                // IDs que hay que agregar
                var toAdd = dtoIds.Except(currentIds).ToList();
                // IDs que hay que quitar
                var toRemove = currentIds.Except(dtoIds).ToList();

                // Quitar las relaciones que ya no están en el DTO
                target.ProjectResearchLines = target.ProjectResearchLines
                    .Where(prl => !toRemove.Contains(prl.ResearchLineTypeId))
                    .ToList();

                // Agregar nuevas relaciones
                foreach (var id in toAdd)
                {
                    target.ProjectResearchLines.Add(new ProjectResearchLine
                    {
                        ProjectId = target.ProjectId,
                        ResearchLineTypeId = id
                    });
                }
            }
        }

    }
}
