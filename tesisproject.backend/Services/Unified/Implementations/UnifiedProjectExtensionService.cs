using Microsoft.EntityFrameworkCore;
using System;
using System.Linq.Expressions;
using tesisproject.backend.Services.Interfaces;
using tesisproject.backend.Services.Unified.Interfaces;
using tesisproject.backend.UnitOfWork.Unified.Interfaces;
using tesisproject.shared.DTOs.ProjectExtensions.Request;
using tesisproject.shared.DTOs.ProjectExtensions.Response;
using tesisproject.backend.Data.UnifiedEntities.Core;
using tesisproject.shared.Enums;
using tesisproject.shared.Responses;
using tesisproject.shared.Errors;

namespace tesisproject.backend.Services.Unified.Implementations
{
    public class UnifiedProjectExtensionService : IUnifiedProjectExtensionService
    {





        private const string TentativeEndDateNullMessage =
            "Project TentativeEndDate is null. It must be set before applying an extension.";
        private const string ProjectExtensionCouldNotLoadAfterCreationMessage =
            "ProjectExtension could not be loaded after creation.";
        private const string ProjectExtensionCouldNotLoadAfterUpdateMessage =
            "ProjectExtension could not be loaded after update.";

        private const string NoProjectExtensionsFoundMessage = "No project extensions found.";

        private const string NoProjectExtensionsFoundForProjectMessage = "No project extensions found for this project.";

        private const string ProjectExtensionCreatedMessage = "ProjectExtension created";
        private const string ProjectExtensionRetrievedMessage = "ProjectExtension retrieved";
        private const string ProjectExtensionsRetrievedMessage = "Project extensions retrieved";
        private const string ProjectExtensionsByProjectRetrievedMessage = "Project extensions by project retrieved";
        private const string ProjectExtensionUpdatedMessage = "ProjectExtension updated";
        private const string ProjectExtensionDeletedMessage = "ProjectExtension deleted";

        private const int ExtensionMonths = 6;

        private static readonly Expression<Func<ProjectExtension, ProjectExtensionListResponseDTO>> MapToListExpression = pe =>
            new ProjectExtensionListResponseDTO
            {
                ProjectExtensionId = pe.ProjectExtensionId,
                ProjectId = pe.ProjectId,
                ProjectName = pe.Project.ProjectName,
                DocumentId = pe.DocumentId,
                // DocumentName = pe.Document != null ? pe.Document.Name : null,
                RequestedAt = pe.RequestedAt,
                ApprovedAt = pe.ApprovedAt
            };

        private readonly IUnifiedUnitOfWork _uow;

        public UnifiedProjectExtensionService(IUnifiedUnitOfWork uow)
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
                if (request is null)
                    return ServiceResult<ProjectExtensionListResponseDTO>.Fail(ErrorMessages.UnifiedLegacy.FacultyScopeService_RequestRequiredMessage, ErrorType.Validation, ErrorCodes.Common.InvalidRequest);

                if (request.ProjectId <= 0)
                    return ServiceResult<ProjectExtensionListResponseDTO>.Fail(ErrorMessages.UnifiedLegacy.ProjectExtensionService_ProjectIdRequiredMessage, ErrorType.Validation, ErrorCodes.Common.InvalidRequest);

                if (request.ProjectExtensionTypeId <= 0)
                    return ServiceResult<ProjectExtensionListResponseDTO>.Fail(ErrorMessages.UnifiedLegacy.ProjectExtensionService_ProjectExtensionTypeIdRequiredMessage, ErrorType.Validation, ErrorCodes.Common.InvalidRequest);

                // 1) Load project (needed to update TentativeEndDate)
                var project = await _uow.Projects.GetByIdAsync(Key(request.ProjectId), ct);
                if (project is null)
                    return ServiceResult<ProjectExtensionListResponseDTO>.Fail(ErrorMessages.UnifiedLegacy.ProjectExtensionService_ProjectNotFoundMessage, ErrorType.NotFound, ErrorCodes.Common.NotFound);

                // 2) Validate ProjectExtensionType exists (optional but recommended)
                var extType = await _uow.ProjectExtensionTypes.GetByIdAsync(Key(request.ProjectExtensionTypeId), ct);
                if (extType is null)
                    return ServiceResult<ProjectExtensionListResponseDTO>.Fail(ErrorMessages.UnifiedLegacy.ProjectExtensionService_ProjectExtensionTypeNotFoundMessage, ErrorType.NotFound, ErrorCodes.Common.NotFound);

                // 3) Update project tentative end date (+ 6 months)
                if (!project.TentativeEndDate.HasValue)
                    return ServiceResult<ProjectExtensionListResponseDTO>.Fail(
                        TentativeEndDateNullMessage,
                        ErrorType.Validation, ErrorCodes.Common.InvalidRequest);

                project.TentativeEndDate = project.TentativeEndDate.Value.AddMonths(ExtensionMonths);
                _uow.Projects.Update(project);

                // 4) Create ProjectExtension
                var extension = new ProjectExtension
                {
                    ProjectId = request.ProjectId,
                    ProjectExtensionTypeId = request.ProjectExtensionTypeId,
                    DocumentId = request.DocumentId,

                    ExtensionDate = DateTime.UtcNow,

                    RequestedAt = request.RequestedAt,
                    ApprovedAt = request.ApprovedAt
                };

                await _uow.ProjectExtensions.AddAsync(extension, ct);

                // 5) Create Visit (planned, no date)
                var visit = new Visit
                {
                    ProjectId = request.ProjectId,
                    VisitStateId = VisitStateIds.Planned,
                    AcademicTermId = null,

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
                        ProjectExtensionCouldNotLoadAfterCreationMessage,
                        ErrorType.Unexpected, ErrorCodes.Common.UnexpectedError);

                return ServiceResult<ProjectExtensionListResponseDTO>.Ok(MapToListDTO(withRefs), ProjectExtensionCreatedMessage);
            }
            catch (DbUpdateException dbex)
            {
                return ServiceResult<ProjectExtensionListResponseDTO>.Fail(
                    dbex.InnerException?.Message ?? dbex.Message,
                    ErrorType.Conflict, ErrorCodes.Common.PersistenceConflict);
            }
            catch (Exception ex)
            {
                return ServiceResult<ProjectExtensionListResponseDTO>.Fail(ex.Message, ErrorType.Unexpected, ErrorCodes.Common.UnexpectedError);
            }
        }

        // ===== READ ONE =====
        public async Task<ServiceResult<ProjectExtensionListResponseDTO>> GetByIdAsync(
            int projectExtensionId,
            CancellationToken ct = default)
        {
            try
            {
                var entity = await _uow.ProjectExtensions.GetByIdWithRefsAsync(projectExtensionId, ct);
                if (entity is null)
                    return ServiceResult<ProjectExtensionListResponseDTO>.Fail(ErrorMessages.UnifiedLegacy.ProjectExtensionService_ProjectExtensionNotFoundMessage, ErrorType.NotFound, ErrorCodes.Common.NotFound);

                return ServiceResult<ProjectExtensionListResponseDTO>.Ok(MapToListDTO(entity), ProjectExtensionRetrievedMessage);
            }
            catch (Exception ex)
            {
                return ServiceResult<ProjectExtensionListResponseDTO>.Fail(ex.Message, ErrorType.Unexpected, ErrorCodes.Common.UnexpectedError);
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
                    .Select(MapToListExpression)
                    .ToListAsync(ct);

                if (items.Count == 0)
                    return ServiceResult<IReadOnlyList<ProjectExtensionListResponseDTO>>.Fail(NoProjectExtensionsFoundMessage, ErrorType.NotFound, ErrorCodes.Common.NotFound);

                return ServiceResult<IReadOnlyList<ProjectExtensionListResponseDTO>>.Ok(items, ProjectExtensionsRetrievedMessage);
            }
            catch (Exception ex)
            {
                return ServiceResult<IReadOnlyList<ProjectExtensionListResponseDTO>>.Fail(ex.Message, ErrorType.Unexpected, ErrorCodes.Common.UnexpectedError);
            }
        }

        // ===== LIST BY PROJECT =====
        public async Task<ServiceResult<IReadOnlyList<ProjectExtensionListResponseDTO>>> ListByProjectAsync(
            int projectId,
            CancellationToken ct = default)
        {
            try
            {
                if (projectId <= 0)
                    return ServiceResult<IReadOnlyList<ProjectExtensionListResponseDTO>>.Fail(ErrorMessages.UnifiedLegacy.ProjectExtensionService_ProjectIdRequiredLowercaseMessage, ErrorType.Validation, ErrorCodes.Common.InvalidRequest);

                var items = await _uow.ProjectExtensions.GetByProjectAsync(projectId, ct);
                if (items.Count == 0)
                    return ServiceResult<IReadOnlyList<ProjectExtensionListResponseDTO>>.Fail(NoProjectExtensionsFoundForProjectMessage, ErrorType.NotFound, ErrorCodes.Common.NotFound);

                var dtos = items.Select(MapToListDTO).ToList();
                return ServiceResult<IReadOnlyList<ProjectExtensionListResponseDTO>>.Ok(dtos, ProjectExtensionsByProjectRetrievedMessage);
            }
            catch (Exception ex)
            {
                return ServiceResult<IReadOnlyList<ProjectExtensionListResponseDTO>>.Fail(ex.Message, ErrorType.Unexpected, ErrorCodes.Common.UnexpectedError);
            }
        }

        // ===== UPDATE =====
        public async Task<ServiceResult<ProjectExtensionListResponseDTO>> UpdateAsync(
            UpdateProjectExtensionRequestDTO request,
            CancellationToken ct = default)
        {
            try
            {
                if (request is null)
                    return ServiceResult<ProjectExtensionListResponseDTO>.Fail(ErrorMessages.UnifiedLegacy.FacultyScopeService_RequestRequiredMessage, ErrorType.Validation, ErrorCodes.Common.InvalidRequest);

                var entity = await _uow.ProjectExtensions.GetByIdAsync(Key(request.ProjectExtensionId), ct);
                if (entity is null)
                    return ServiceResult<ProjectExtensionListResponseDTO>.Fail(ErrorMessages.UnifiedLegacy.ProjectExtensionService_ProjectExtensionNotFoundMessage, ErrorType.NotFound, ErrorCodes.Common.NotFound);

                if (request.ProjectId <= 0)
                    return ServiceResult<ProjectExtensionListResponseDTO>.Fail(ErrorMessages.UnifiedLegacy.ProjectExtensionService_ProjectIdRequiredMessage, ErrorType.Validation, ErrorCodes.Common.InvalidRequest);

                if (request.ProjectExtensionTypeId <= 0)
                    return ServiceResult<ProjectExtensionListResponseDTO>.Fail(ErrorMessages.UnifiedLegacy.ProjectExtensionService_ProjectExtensionTypeIdRequiredMessage, ErrorType.Validation, ErrorCodes.Common.InvalidRequest);

                entity.ProjectId = request.ProjectId;
                entity.ProjectExtensionTypeId = request.ProjectExtensionTypeId; // ✅ B: antes se validaba pero NO se asignaba
                entity.DocumentId = request.DocumentId;
                entity.RequestedAt = request.RequestedAt;
                entity.ApprovedAt = request.ApprovedAt;

                _uow.ProjectExtensions.Update(entity);
                await _uow.SaveChangesAsync(ct);

                var withRefs = await _uow.ProjectExtensions.GetByIdWithRefsAsync(entity.ProjectExtensionId, ct);
                if (withRefs is null)
                    return ServiceResult<ProjectExtensionListResponseDTO>.Fail(ProjectExtensionCouldNotLoadAfterUpdateMessage, ErrorType.Unexpected, ErrorCodes.Common.UnexpectedError);

                return ServiceResult<ProjectExtensionListResponseDTO>.Ok(MapToListDTO(withRefs), ProjectExtensionUpdatedMessage);
            }
            catch (DbUpdateException dbex)
            {
                return ServiceResult<ProjectExtensionListResponseDTO>.Fail(
                    dbex.InnerException?.Message ?? dbex.Message,
                    ErrorType.Conflict, ErrorCodes.Common.PersistenceConflict);
            }
            catch (Exception ex)
            {
                return ServiceResult<ProjectExtensionListResponseDTO>.Fail(ex.Message, ErrorType.Unexpected, ErrorCodes.Common.UnexpectedError);
            }
        }

        // ===== DELETE =====
        public async Task<ServiceResult<NoContent>> DeleteAsync(
            int projectExtensionId,
            CancellationToken ct = default)
        {
            try
            {
                var entity = await _uow.ProjectExtensions.GetByIdAsync(Key(projectExtensionId), ct);
                if (entity is null)
                    return ServiceResult<NoContent>.Fail(ErrorMessages.UnifiedLegacy.ProjectExtensionService_ProjectExtensionNotFoundMessage, ErrorType.NotFound, ErrorCodes.Common.NotFound);

                _uow.ProjectExtensions.Remove(entity);
                await _uow.SaveChangesAsync(ct);

                return ServiceResult<NoContent>.Ok(new NoContent(), ProjectExtensionDeletedMessage);
            }
            catch (DbUpdateException dbex)
            {
                return ServiceResult<NoContent>.Fail(
                    dbex.InnerException?.Message ?? dbex.Message,
                    ErrorType.Conflict, ErrorCodes.Common.PersistenceConflict);
            }
            catch (Exception ex)
            {
                return ServiceResult<NoContent>.Fail(ex.Message, ErrorType.Unexpected, ErrorCodes.Common.UnexpectedError);
            }
        }

        // ===== HELPERS =====
        private static object[] Key(int id) => new object[] { id };

        // ===== MAPPER =====
        private static ProjectExtensionListResponseDTO MapToListDTO(ProjectExtension pe) => new()
        {
            ProjectExtensionId = pe.ProjectExtensionId,
            ProjectId = pe.ProjectId,
            ProjectName = pe.Project?.ProjectName ?? string.Empty,
            DocumentId = pe.DocumentId,
            // DocumentName = pe.Document?.Name,
            RequestedAt = pe.RequestedAt,
            ApprovedAt = pe.ApprovedAt
        };
    }
}