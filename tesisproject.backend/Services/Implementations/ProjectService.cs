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

                        FundingTypeId = p.Budgets
                            .Select(b => b.FundingTypeId)
                            .Distinct()
                            .ToList()
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
                   .Include(p => p.ProjectResearchCategories)
                       .ThenInclude(prc => prc.ResearchCategory)
                           .ThenInclude(rc => rc.ParentCategory)   // para subir a dominio
                   .Include(p => p.ProjectResearchCategories)
                       .ThenInclude(prc => prc.ResearchCategory)
                           .ThenInclude(rc => rc.SubCategories)    // para obtener las líneas de ese dominio
                   .Where(p => p.ProjectId == projectId)
                   .Select(p => new ProjectDetailResponseDTO
                   {
                       ProjectId = p.ProjectId,
                       ProjectCode = p.ProjectCode ?? string.Empty,
                       ProjectName = p.ProjectName ?? string.Empty,
                       ProjectObjectives = MapToDTO(p.ProjectObjectives),

                       ResearchDomains = p.ProjectResearchCategories
                           .Select(prc => prc.ResearchCategory)
                           .Where(rc => rc.ParentCategoryId != null)    // tomamos las líneas
                           .Select(rc => rc.ParentCategory)             // subimos al dominio
                           .Where(domain => domain != null)
                           .Distinct()                                  // dominios únicos
                           .Select(domain => new ProjectResearchDomainDTO
                           {
                               ResearchDomainTypeId = domain!.Id,
                               ResearchDomainTypeName = domain.Name,
                               ResearchLines = domain.SubCategories
                                   .Where(line => p.ProjectResearchCategories
                                       .Any(prc => prc.ResearchCategoryId == line.Id))
                                   .Select(line => new ProjectResearchLineDTO
                                   {
                                       ResearchLineTypeId = line.Id,
                                       ResearchLineTypeName = line.Name
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

                       ProjectGroupId = p.ProjectGroupId,
                       ProjectGroupName = p.ProjectGroup.Name ?? string.Empty,

                       // 🔹 Todos los presupuestos del proyecto
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
            CancellationToken ct = default)
        {
            // Helper local para log interno
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

                if (p.CreatedByUserId <= 0)
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

                // El front manda aquí solo el prefijo de facultad, por ej. "PFCHE"
                var facultyCode = (p.ProjectCode ?? string.Empty).Trim();

                if (string.IsNullOrWhiteSpace(facultyCode))
                {
                    PhaseLog("Fase 1.5 - Código", "facultyCode vacío");
                    return ServiceResult<ProjectDetailResponseDTO>.Fail("Faculty project code (prefix) is required.");
                }

                // Buscar último número usado para esa facultad
                var lastNumber = await _uow.Projects
                    .Query(asNoTracking: true)
                    .Where(x => x.FacultyId == p.FacultyId &&
                                x.ProjectCode.StartsWith(facultyCode))  // PFCHE...
                    .OrderByDescending(x => x.ProjectNumber)
                    .Select(x => (int?)x.ProjectNumber)
                    .FirstOrDefaultAsync(ct) ?? 0;

                var nextNumber = lastNumber + 1;

                // Construir código final: PFCHE17-A
                var generatedCode = $"{facultyCode}{nextNumber}-A";

                PhaseLog("Fase 1.5 - Código",
                    $"facultyCode={facultyCode}, last={lastNumber}, next={nextNumber}, final={generatedCode}");

                // Por seguridad con [StringLength(20)]
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


                PhaseLog("Fase 4 - Project", "Creando entidad proyecto...");

                var projectEntity = new Project
                {
                    ProjectCode = generatedCode,
                    CreatedByUserId = p.CreatedByUserId,
                    ProjectTypeId = p.ProjectTypeId,
                    ProjectNumber = nextNumber,
                    ProjectStateId = p.ProjectStateId,
                    ProjectName = p.ProjectName ?? string.Empty,
                    ApprovalDate = p.ApprovalDate,
                    StartDate = p.StartDate,
                    DurationInMonths = p.DurationInMonths,
                    TentativeEndDate = p.StartDate?.AddMonths(p.DurationInMonths),
                    RealEndDate = null,
                    ExecutionPercentage = 0,
                    FacultyId = p.FacultyId,
                    InitialDocumentId = d?.DocumentId
                };

                projectEntity.ProjectGroup = groupEntity;

                await _uow.Projects.AddAsync(projectEntity, ct);

                // ============================================
                // 5) Miembros del grupo
                // ============================================

                PhaseLog("Fase 5 - Miembros", "Insertando miembros...");

                if (request.GroupMembers is not null && request.GroupMembers.Count > 0)
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
                            ApprovedByUserId = b.ApprovedByUserId,
                            InitialAmount = b.InitialAmount,
                            CertifiedAmount = 0,
                            ExecutedAmount = 0,
                            ApprovedAt = null,
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
                            Result = objDto.Result
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
                                    ImprovementAction = actDto.ImprovementAction ?? string.Empty,
                                    IsCompleted = actDto.IsCompleted,
                                    CreatedAt = DateTime.UtcNow
                                };

                                await _uow.ObjectiveActivities.AddAsync(activityEntity, ct);
                            }
                        }
                    }
                }

                // ============================================
                // 9) Visitas
                // ============================================

                PhaseLog("Fase 9 - Visitas", "Generando visitas...");

                var latest = await _periods
                    .Query(asNoTracking: true)
                    .OrderByDescending(p => p.Id)
                    .FirstOrDefaultAsync(ct);

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
                            AcademicPeriodId = latest.Id,
                            ScheduledDate = request.ScheduledDate,
                            PerformedDate = null,
                            CreatedAt = DateTime.UtcNow
                        })
                        .ToList();

                    await _uow.Visits.AddRangeAsync(visits, ct);
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

        private static Project MapToEntity(AddProjectRequestDTO dto) => new()
        {
            ProjectCode = dto.ProjectCode ?? string.Empty,
            CreatedByUserId = dto.CreatedByUserId,
            ProjectTypeId = dto.ProjectTypeId,
            ProjectStateId = dto.ProjectStateId,
            ProjectGroupId = dto.ProjectGroupId,
            ProjectName = dto.ProjectName,
            StartDate = dto.StartDate,
            TentativeEndDate = dto.StartDate?.AddMonths(dto.DurationInMonths),
            ExecutionPercentage = 0,
            DurationInMonths = dto.DurationInMonths,
            FacultyId = dto.FacultyId
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

                // Quitar relaciones sobrantes
                var linksToRemove = currentLinks
                    .Where(x => toRemoveIds.Contains(x.ResearchCategoryId))
                    .ToList();

                if (linksToRemove.Count > 0)
                {
                    _uow.ProjectResearchCategories.RemoveRange(linksToRemove);
                }

                // Agregar las nuevas
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
        }
    }
}
