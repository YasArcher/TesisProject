using Microsoft.EntityFrameworkCore;
using tesisproject.backend.Services.Interfaces;
using tesisproject.backend.UnitOfWork.Interfaces;
using tesisproject.shared.DTOs.Project.Request;
using tesisproject.shared.DTOs.Project.Response;
using tesisproject.shared.Entities.Core;
using tesisproject.shared.Responses;

namespace tesisproject.backend.Services.Implementations
{
    public class ProjectService : IProjectService
    {
        private readonly IUnitOfWork _uow;
        public ProjectService(IUnitOfWork uow) => _uow = uow;

        // ================= READS =================

        public async Task<ServiceResult<List<ProjectListResponseDTO>>> GetAllAsync(CancellationToken ct = default)
        {
            try
            {
                var data = await _uow.Projects
                    .Query()
                    .Select(p => new ProjectListResponseDTO(
                        p.ProjectId,
                        p.ProjectCode,
                        p.ProjectName,
                        p.ProjectState.Name,
                        p.ProjectType.Name,
                        p.ProjectGroup.Name,
                        p.StartDate,
                        p.TentativeEndDate,
                        p.ExecutionPercentage
                    ))
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
                    .Select(p => new ProjectListResponseDTO(
                        p.ProjectId,
                        p.ProjectCode,
                        p.ProjectName,
                        p.ProjectState.Name,
                        p.ProjectType.Name,
                        p.ProjectGroup.Name,
                        p.StartDate,
                        p.TentativeEndDate,
                        p.ExecutionPercentage
                    ))
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
                    .Select(p => new ProjectListResponseDTO(
                        p.ProjectId,
                        p.ProjectCode,
                        p.ProjectName,
                        p.ProjectState.Name,
                        p.ProjectType.Name,
                        p.ProjectGroup.Name,
                        p.StartDate,
                        p.TentativeEndDate,
                        p.ExecutionPercentage
                    ))
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
                    .Select(p => new ProjectListResponseDTO(
                        p.ProjectId,
                        p.ProjectCode,
                        p.ProjectName,
                        p.ProjectState.Name,
                        p.ProjectType.Name,
                        p.ProjectGroup.Name,
                        p.StartDate,
                        p.TentativeEndDate,
                        p.ExecutionPercentage
                    ))
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

        public async Task<ServiceResult<ProjectDetailResponseDTO>> GetProjectDetailAsync(int projectId, CancellationToken ct = default)
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
                        ProjectObjective = p.ProjectObjective ?? string.Empty,
                        ResearchLine = p.ResearchLine ?? string.Empty,
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
                        SenesytGroupId = p.SenesytGroupId,
                        SenesytGroupName = p.SenesytGroup != null ? p.SenesytGroup.Name : null,

                        // --- Budget --- ✅ CORREGIDO
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

        private static Project MapToEntity(AddProjectRequestDTO dto) => new()
        {
            ProjectCode = dto.ProjectCode ?? string.Empty,
            CreatedByUserId = dto.CreatedByUserId,
            ProjectTypeId = dto.ProjectTypeId,
            ProjectStateId = dto.ProjectStateId,
            ProjectGroupId = dto.ProjectGroupId,
            SenesytGroupId = dto.SenesytGroupId,
            InitialDocumentId = dto.InitialDocumentId,
            ProjectName = dto.ProjectName,
            ProjectObjective = dto.ProjectObjective,
            ResearchLine = dto.ResearchLine,
            StartDate = dto.StartDate,
            TentativeEndDate = dto.TentativeEndDate,
            ExecutionPercentage = 0
        };

        private static void ApplyUpdate(Project target, UpdateProjectRequestDTO dto)
        {
            target.ProjectName = dto.ProjectName;
            target.ProjectObjective = dto.ProjectObjective;
            target.ResearchLine = dto.ResearchLine;
            target.ProjectTypeId = dto.ProjectTypeId;
            target.ProjectStateId = dto.ProjectStateId;
            target.ProjectGroupId = dto.ProjectGroupId;
            target.SenesytGroupId = dto.SenesytGroupId;
            target.InitialDocumentId = dto.InitialDocumentId;
            target.StartDate = dto.StartDate;
            target.TentativeEndDate = dto.TentativeEndDate;
            target.RealEndDate = dto.RealEndDate;
            target.ExecutionPercentage = dto.ExecutionPercentage;
        }
    }
}
