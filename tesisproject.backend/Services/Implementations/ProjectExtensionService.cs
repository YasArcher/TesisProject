using Microsoft.EntityFrameworkCore;
using tesisproject.backend.Services.Interfaces;
using tesisproject.backend.UnitOfWork.Interfaces;
using tesisproject.shared.DTOs.ProjectExtensions.Request;
using tesisproject.shared.DTOs.ProjectExtensions.Response;
using tesisproject.shared.Entities.Core;
using tesisproject.shared.Responses;

namespace tesisproject.backend.Services.Implementations
{
    public class ProjectExtensionService : IProjectExtensionService
    {
        private readonly IUnitOfWork _uow;

        public ProjectExtensionService(IUnitOfWork uow)
        {
            _uow = uow;
        }

        // ===== CREATE =====
        public async Task<ServiceResult<ProjectExtensionListResponseDTO>> CreateAsync(
            AddProjectExtensionRequestDTO request,
            CancellationToken ct = default)
        {
            try
            {
                if (request.ProjectId <= 0)
                    return ServiceResult<ProjectExtensionListResponseDTO>.Fail("ProjectId is required.", ErrorType.Validation);

                if (request.ProjectExtensionTypeId <= 0)
                    return ServiceResult<ProjectExtensionListResponseDTO>.Fail("ProjectExtensionTypeId is required.", ErrorType.Validation);

                // 1) Load project (needed to update TentativeEndDate)
                var project = await _uow.Projects.GetByIdAsync(new object[] { request.ProjectId }, ct);
                if (project is null)
                    return ServiceResult<ProjectExtensionListResponseDTO>.Fail("Project not found.", ErrorType.NotFound);

                // 2) Validate ProjectExtensionType exists (optional but recommended)
                var extType = await _uow.ProjectExtensionTypes.GetByIdAsync(new object[] { request.ProjectExtensionTypeId }, ct);
                if (extType is null)
                    return ServiceResult<ProjectExtensionListResponseDTO>.Fail("ProjectExtensionType not found.", ErrorType.NotFound);

                // 3) Update project tentative end date (+ 6 months)
                if (!project.TentativeEndDate.HasValue)
                    return ServiceResult<ProjectExtensionListResponseDTO>.Fail(
                        "Project TentativeEndDate is null. It must be set before applying an extension.",
                        ErrorType.Validation);

                project.TentativeEndDate = project.TentativeEndDate.Value.AddMonths(6);
                _uow.Projects.Update(project);

                // 4) Create ProjectExtension
                var extension = new ProjectExtension
                {
                    ProjectId = request.ProjectId,
                    ProjectExtensionTypeId = request.ProjectExtensionTypeId, // ✅ nuevo FK
                    DocumentId = request.DocumentId,

                    ExtensionDate = DateTime.UtcNow, // ✅ requerido en tu Entity (no-null)

                    RequestedAt = request.RequestedAt,
                    ApprovedAt = request.ApprovedAt
                };

                await _uow.ProjectExtensions.AddAsync(extension, ct);

                // 5) Create Visit (planned, no date)
                var visit = new Visit
                {
                    ProjectId = request.ProjectId,
                    VisitStateId = 1,
                    AcademicPeriodId = null,

                    ScheduledDate = null,
                    PerformedDate = null,

                    FundingDocumentId = null,
                    DocumentId = null,
                    ProgressDocumentId = null,
                    PerformedByUserId = null
                };

                await _uow.Visits.AddAsync(visit, ct);

                // 6) Single commit
                await _uow.SaveChangesAsync(ct);

                // 7) Reload with refs for response
                var withRefs = await _uow.ProjectExtensions.GetByIdWithRefsAsync(extension.ProjectExtensionId, ct);
                if (withRefs is null)
                    return ServiceResult<ProjectExtensionListResponseDTO>.Fail(
                        "ProjectExtension could not be loaded after creation.",
                        ErrorType.Unexpected);

                return ServiceResult<ProjectExtensionListResponseDTO>.Ok(MapToListDTO(withRefs), "ProjectExtension created");
            }
            catch (DbUpdateException dbex)
            {
                return ServiceResult<ProjectExtensionListResponseDTO>.Fail(dbex.InnerException?.Message ?? dbex.Message, ErrorType.Conflict);
            }
            catch (Exception ex)
            {
                return ServiceResult<ProjectExtensionListResponseDTO>.Fail(ex.Message, ErrorType.Unexpected);
            }
        }


        // ===== READ ONE =====
        public async Task<ServiceResult<ProjectExtensionListResponseDTO>> GetByIdAsync(int projectExtensionId, CancellationToken ct = default)
        {
            try
            {
                var entity = await _uow.ProjectExtensions.GetByIdWithRefsAsync(projectExtensionId, ct);
                if (entity is null)
                    return ServiceResult<ProjectExtensionListResponseDTO>.Fail("ProjectExtension not found.", ErrorType.NotFound);

                return ServiceResult<ProjectExtensionListResponseDTO>.Ok(MapToListDTO(entity), "ProjectExtension retrieved");
            }
            catch (Exception ex)
            {
                return ServiceResult<ProjectExtensionListResponseDTO>.Fail(ex.Message, ErrorType.Unexpected);
            }
        }

        // ===== LIST =====
        public async Task<ServiceResult<IReadOnlyList<ProjectExtensionListResponseDTO>>> ListAsync(CancellationToken ct = default)
        {
            try
            {
                var q = _uow.ProjectExtensions.QueryWithRefs();
                var items = await q
                    .OrderBy(pe => pe.RequestedAt ?? DateTime.MaxValue)
                    .Select(pe => new ProjectExtensionListResponseDTO
                    {
                        ProjectExtensionId = pe.ProjectExtensionId,
                        ProjectId = pe.ProjectId,
                        ProjectName = pe.Project.ProjectName,
                        DocumentId = pe.DocumentId,
 //                       DocumentName = pe.Document != null ? pe.Document.Name : null,
                        RequestedAt = pe.RequestedAt,
                        ApprovedAt = pe.ApprovedAt
                    })
                    .ToListAsync(ct);

                if (items.Count == 0)
                    return ServiceResult<IReadOnlyList<ProjectExtensionListResponseDTO>>.Fail("No project extensions found.", ErrorType.NotFound);

                return ServiceResult<IReadOnlyList<ProjectExtensionListResponseDTO>>.Ok(items, "Project extensions retrieved");
            }
            catch (Exception ex)
            {
                return ServiceResult<IReadOnlyList<ProjectExtensionListResponseDTO>>.Fail(ex.Message, ErrorType.Unexpected);
            }
        }

        // ===== LIST BY PROJECT =====
        public async Task<ServiceResult<IReadOnlyList<ProjectExtensionListResponseDTO>>> ListByProjectAsync(int projectId, CancellationToken ct = default)
        {
            try
            {
                if (projectId <= 0)
                    return ServiceResult<IReadOnlyList<ProjectExtensionListResponseDTO>>.Fail("projectId is required.", ErrorType.Validation);

                var items = await _uow.ProjectExtensions.GetByProjectAsync(projectId, ct);
                if (items.Count == 0)
                    return ServiceResult<IReadOnlyList<ProjectExtensionListResponseDTO>>.Fail("No project extensions found for this project.", ErrorType.NotFound);

                var dtos = items.Select(MapToListDTO).ToList();
                return ServiceResult<IReadOnlyList<ProjectExtensionListResponseDTO>>.Ok(dtos, "Project extensions by project retrieved");
            }
            catch (Exception ex)
            {
                return ServiceResult<IReadOnlyList<ProjectExtensionListResponseDTO>>.Fail(ex.Message, ErrorType.Unexpected);
            }
        }

        // ===== UPDATE =====
        public async Task<ServiceResult<ProjectExtensionListResponseDTO>> UpdateAsync(UpdateProjectExtensionRequestDTO request, CancellationToken ct = default)
        {
            try
            {
                var entity = await _uow.ProjectExtensions.GetByIdAsync(new object[] { request.ProjectExtensionId }, ct);
                if (entity is null)
                    return ServiceResult<ProjectExtensionListResponseDTO>.Fail("ProjectExtension not found.", ErrorType.NotFound);

                if (request.ProjectId <= 0)
                    return ServiceResult<ProjectExtensionListResponseDTO>.Fail("ProjectId is required.", ErrorType.Validation);

                if (request.ProjectExtensionTypeId <= 0)
                    return ServiceResult<ProjectExtensionListResponseDTO>.Fail("ProjectExtensionTypeId is required.", ErrorType.Validation);

                entity.ProjectId = request.ProjectId;
                entity.DocumentId = request.DocumentId;
                entity.RequestedAt = request.RequestedAt;
                entity.ApprovedAt = request.ApprovedAt;

                _uow.ProjectExtensions.Update(entity);
                await _uow.SaveChangesAsync(ct);

                var withRefs = await _uow.ProjectExtensions.GetByIdWithRefsAsync(entity.ProjectExtensionId, ct);
                if (withRefs is null)
                    return ServiceResult<ProjectExtensionListResponseDTO>.Fail("ProjectExtension could not be loaded after update.", ErrorType.Unexpected);

                return ServiceResult<ProjectExtensionListResponseDTO>.Ok(MapToListDTO(withRefs), "ProjectExtension updated");
            }
            catch (DbUpdateException dbex)
            {
                return ServiceResult<ProjectExtensionListResponseDTO>.Fail(dbex.InnerException?.Message ?? dbex.Message, ErrorType.Conflict);
            }
            catch (Exception ex)
            {
                return ServiceResult<ProjectExtensionListResponseDTO>.Fail(ex.Message, ErrorType.Unexpected);
            }
        }

        // ===== DELETE =====
        public async Task<ServiceResult<NoContent>> DeleteAsync(int projectExtensionId, CancellationToken ct = default)
        {
            try
            {
                var entity = await _uow.ProjectExtensions.GetByIdAsync(new object[] { projectExtensionId }, ct);
                if (entity is null)
                    return ServiceResult<NoContent>.Fail("ProjectExtension not found.", ErrorType.NotFound);

                _uow.ProjectExtensions.Remove(entity);
                await _uow.SaveChangesAsync(ct);

                return ServiceResult<NoContent>.Ok(new NoContent(), "ProjectExtension deleted");
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

        // ===== MAPPER =====
        private static ProjectExtensionListResponseDTO MapToListDTO(ProjectExtension pe) => new()
        {
            ProjectExtensionId = pe.ProjectExtensionId,
            ProjectId = pe.ProjectId,
            ProjectName = pe.Project?.ProjectName ?? string.Empty,
            DocumentId = pe.DocumentId,
//            DocumentName = pe.Document?.Name,
            RequestedAt = pe.RequestedAt,
            ApprovedAt = pe.ApprovedAt
        };
    }
}