using tesisproject.backend.Services.Interfaces;
using tesisproject.backend.Services.Unified.Interfaces;
using tesisproject.backend.UnitOfWork.Unified.Interfaces;
using tesisproject.shared.DTOs.ObjectiveActivity.Response;
using tesisproject.shared.DTOs.ProjectObjective.Request;
using tesisproject.shared.DTOs.ProjectObjective.Response;
using tesisproject.backend.Data.UnifiedEntities.Core;
using tesisproject.shared.Responses;
using tesisproject.shared.Errors;

namespace tesisproject.backend.Services.Unified.Implementations
{
    public class UnifiedProjectObjectiveService : IUnifiedProjectObjectiveService
    {
        private readonly IUnifiedUnitOfWork _uow;









        public UnifiedProjectObjectiveService(IUnifiedUnitOfWork uow)
        {
            _uow = uow;
        }

        // ==================== READS ====================

        public async Task<ServiceResult<IReadOnlyList<ProjectObjectiveListItemDTO>>> ListByProjectAsync(
            int projectId,
            CancellationToken ct = default)
        {
            if (projectId <= 0)
                return FailValidation<IReadOnlyList<ProjectObjectiveListItemDTO>>(ErrorMessages.UnifiedLegacy.ProjectExtensionService_ProjectIdRequiredMessage);

            var entities = await _uow.ProjectObjectives.GetByProjectAsync(projectId, ct);

            var dto = entities
                .OrderBy(o => o.Id)
                .Select(o => new ProjectObjectiveListItemDTO
                {
                    Id = o.Id,
                    ProjectId = o.ProjectId,
                    ObjectiveTypeId = o.ObjectiveTypeId,
                    ObjectiveTypeName = o.ObjectiveType?.Name ?? string.Empty,
                    Objective = o.Objective,          // propiedad de la entidad
                    Result = o.Result,
                    ActivitiesCount = o.Activities?.Count ?? 0
                })
                .ToList();

            return ServiceResult<IReadOnlyList<ProjectObjectiveListItemDTO>>.Ok(dto);
        }

        public async Task<ServiceResult<IReadOnlyList<ProjectObjectiveWithActivitiesDTO>>> GetByProjectWithActivitiesAsync(
            int projectId,
            CancellationToken ct = default)
        {
            if (projectId <= 0)
                return FailValidation<IReadOnlyList<ProjectObjectiveWithActivitiesDTO>>(ErrorMessages.UnifiedLegacy.ProjectExtensionService_ProjectIdRequiredMessage);

            // 1) Objetivos + Activities
            var objectives = await _uow.ProjectObjectives.GetByProjectWithActivitiesAsync(projectId, ct);

            if (objectives is null || objectives.Count == 0)
                return ServiceResult<IReadOnlyList<ProjectObjectiveWithActivitiesDTO>>.Ok(new List<ProjectObjectiveWithActivitiesDTO>());

            // 2) Todas las actividades del proyecto (para calcular progreso una sola vez)
            var activityIds = CollectObjectiveActivityIds(objectives);

            // 3) Total acumulado por actividad (lo correcto para tu modelo actual)
            var totalsMap = activityIds.Count == 0
                ? new Dictionary<int, int>()
                : await _uow.VisitObjectiveActivityProgresses.GetTotalProgressByActivityIdsAsync(activityIds, ct);

            // 4) Map a DTO
            var dto = objectives
                .Select(o => new ProjectObjectiveWithActivitiesDTO
                {
                    // ======= CAMPOS DE ProjectObjectiveDetailDTO =======
                    Id = o.Id,                 // Ajusta si tu PK se llama distinto
                    ProjectId = o.ProjectId,
                    ObjectiveTypeId = o.ObjectiveTypeId,
                    ObjectiveTypeName = o.ObjectiveType?.Name ?? string.Empty,  // requiere Include ObjectiveType
                    Objective = o.Objective ?? string.Empty,   // o el campo real en entidad (ej: ObjectiveText/Title)
                    Result = o.Result ?? string.Empty,         // o el campo real en entidad (ej: ExpectedResult)
                    WeightedPercentage = o.WeightedPercentage,

                    // ======= Activities =======
                    Activities = (o.Activities ?? Enumerable.Empty<ObjectiveActivity>())
                        .OrderBy(a => a.ObjectiveActivityId)
                        .Select(a =>
                        {
                            var progress = totalsMap.TryGetValue(a.ObjectiveActivityId, out var p) ? p : 0;

                            return new ObjectiveActivityListItemDTO
                            {
                                ObjectiveActivityId = a.ObjectiveActivityId,
                                ObjectiveId = a.ObjectiveId,
                                ActivityResult = a.ActivityResult ?? string.Empty,
                                ActionText = a.ActionText ?? string.Empty,
                                CreatedAt = a.CreatedAt,

                                // derivado desde snapshots
                                ProgressPercentage = progress,
                                IsCompleted = progress >= 100
                            };
                        })
                        .ToList()
                })
                .ToList();

            return ServiceResult<IReadOnlyList<ProjectObjectiveWithActivitiesDTO>>.Ok(dto);
        }

        public async Task<ServiceResult<ProjectObjectiveDetailDTO>> GetByIdAsync(
            int id,
            CancellationToken ct = default)
        {
            if (id <= 0)
                return FailValidation<ProjectObjectiveDetailDTO>(ErrorMessages.UnifiedLegacy.IndexingSourceService_InvalidIdMessage);

            var entity = await _uow.ProjectObjectives.GetByIdWithRefsAsync(id, ct);
            if (entity is null)
                return FailNotFound<ProjectObjectiveDetailDTO>(ErrorMessages.UnifiedLegacy.ObjectiveActivityService_ProjectObjectiveNotFoundMessage);

            var dto = new ProjectObjectiveDetailDTO
            {
                Id = entity.Id,
                ProjectId = entity.ProjectId,
                ObjectiveTypeId = entity.ObjectiveTypeId,
                ObjectiveTypeName = entity.ObjectiveType?.Name ?? string.Empty,
                Objective = entity.Objective,
                Result = entity.Result
            };

            return ServiceResult<ProjectObjectiveDetailDTO>.Ok(dto);
        }

        // ==================== WRITES ====================

        public async Task<ServiceResult<ProjectObjectiveDetailDTO>> CreateAsync(
            AddProjectObjectiveRequestDTO request,
            CancellationToken ct = default)
        {
            if (request is null)
                return FailValidation<ProjectObjectiveDetailDTO>(ErrorMessages.UnifiedLegacy.FacultyScopeService_RequestRequiredMessage);

            if (request.ProjectId <= 0)
                return FailValidation<ProjectObjectiveDetailDTO>(ErrorMessages.UnifiedLegacy.ProjectExtensionService_ProjectIdRequiredMessage);

            if (request.ObjectiveTypeId <= 0)
                return FailValidation<ProjectObjectiveDetailDTO>(ErrorMessages.UnifiedLegacy.ProjectObjectiveService_ObjectiveTypeIdRequiredMessage);

            var objectiveText = (request.Objective ?? string.Empty).Trim();
            var resultText = (request.Result ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(objectiveText))
                return FailValidation<ProjectObjectiveDetailDTO>(ErrorMessages.UnifiedLegacy.ProjectObjectiveService_ObjectiveRequiredMessage);

            if (string.IsNullOrWhiteSpace(resultText))
                return FailValidation<ProjectObjectiveDetailDTO>(ErrorMessages.UnifiedLegacy.ProjectObjectiveService_ResultRequiredMessage);

            // Validar que el ObjectiveType exista y esté activo
            var objectiveType = await _uow.ObjectiveTypes.GetByIdAsync(
                new object[] { request.ObjectiveTypeId }, ct);

            if (objectiveType is null || !objectiveType.IsActive)
                return FailValidation<ProjectObjectiveDetailDTO>(ErrorMessages.UnifiedLegacy.ProjectObjectiveService_ObjectiveTypeInvalidOrInactiveMessage);

            var entity = new ProjectObjective
            {
                ProjectId = request.ProjectId,
                ObjectiveTypeId = request.ObjectiveTypeId,
                Objective = objectiveText,
                WeightedPercentage = request.WeightedPercentage,
                Result = resultText
            };

            await _uow.ProjectObjectives.AddAsync(entity, ct);
            await _uow.SaveChangesAsync(ct);

            // Volver a cargar con referencias para el DTO (por si necesitas ObjectiveTypeName)
            var created = await _uow.ProjectObjectives.GetByIdWithRefsAsync(entity.Id, ct) ?? entity;

            var dto = new ProjectObjectiveDetailDTO
            {
                Id = created.Id,
                ProjectId = created.ProjectId,
                ObjectiveTypeId = created.ObjectiveTypeId,
                ObjectiveTypeName = created.ObjectiveType?.Name ?? objectiveType.Name,
                Objective = created.Objective,
                Result = created.Result
            };

            return ServiceResult<ProjectObjectiveDetailDTO>.Ok(dto);
        }

        public async Task<ServiceResult<ProjectObjectiveDetailDTO>> UpdateAsync(
            UpdateProjectObjectiveRequestDTO request,
            CancellationToken ct = default)
        {
            if (request is null || request.Id <= 0)
                return FailValidation<ProjectObjectiveDetailDTO>(ErrorMessages.UnifiedLegacy.IndexingSourceService_InvalidIdMessage);

            if (request.ProjectId <= 0)
                return FailValidation<ProjectObjectiveDetailDTO>(ErrorMessages.UnifiedLegacy.ProjectExtensionService_ProjectIdRequiredMessage);

            if (request.ObjectiveTypeId <= 0)
                return FailValidation<ProjectObjectiveDetailDTO>(ErrorMessages.UnifiedLegacy.ProjectObjectiveService_ObjectiveTypeIdRequiredMessage);

            var objectiveText = (request.Objective ?? string.Empty).Trim();
            var resultText = (request.Result ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(objectiveText))
                return FailValidation<ProjectObjectiveDetailDTO>(ErrorMessages.UnifiedLegacy.ProjectObjectiveService_ObjectiveRequiredMessage);

            if (string.IsNullOrWhiteSpace(resultText))
                return FailValidation<ProjectObjectiveDetailDTO>(ErrorMessages.UnifiedLegacy.ProjectObjectiveService_ResultRequiredMessage);

            var entity = await _uow.ProjectObjectives.GetByIdAsync(
                new object[] { request.Id }, ct);

            if (entity is null)
                return FailNotFound<ProjectObjectiveDetailDTO>(ErrorMessages.UnifiedLegacy.ObjectiveActivityService_ProjectObjectiveNotFoundMessage);

            // Validar ObjectiveType
            var objectiveType = await _uow.ObjectiveTypes.GetByIdAsync(
                new object[] { request.ObjectiveTypeId }, ct);

            if (objectiveType is null || !objectiveType.IsActive)
                return FailValidation<ProjectObjectiveDetailDTO>(ErrorMessages.UnifiedLegacy.ProjectObjectiveService_ObjectiveTypeInvalidOrInactiveMessage);

            entity.ProjectId = request.ProjectId;
            entity.ObjectiveTypeId = request.ObjectiveTypeId;
            entity.Objective = objectiveText;
            entity.Result = resultText;
            entity.WeightedPercentage = request.WeightedPercentage;

            _uow.ProjectObjectives.Update(entity);
            await _uow.SaveChangesAsync(ct);

            // Releer con referencias
            var updated = await _uow.ProjectObjectives.GetByIdWithRefsAsync(entity.Id, ct) ?? entity;

            var dto = new ProjectObjectiveDetailDTO
            {
                Id = updated.Id,
                ProjectId = updated.ProjectId,
                ObjectiveTypeId = updated.ObjectiveTypeId,
                ObjectiveTypeName = updated.ObjectiveType?.Name ?? objectiveType.Name,
                Objective = updated.Objective,
                Result = updated.Result
            };

            return ServiceResult<ProjectObjectiveDetailDTO>.Ok(dto);
        }

        public async Task<ServiceResult<bool>> DeleteAsync(
            int id,
            CancellationToken ct = default)
        {
            if (id <= 0)
                return FailValidation<bool>(ErrorMessages.UnifiedLegacy.IndexingSourceService_InvalidIdMessage);

            var entity = await _uow.ProjectObjectives.GetByIdAsync(new object[] { id }, ct);
            if (entity is null)
                return FailNotFound<bool>(ErrorMessages.UnifiedLegacy.ObjectiveActivityService_ProjectObjectiveNotFoundMessage);

            _uow.ProjectObjectives.Remove(entity);
            await _uow.SaveChangesAsync(ct);

            return ServiceResult<bool>.Ok(true);
        }

        public async Task<ServiceResult<IReadOnlyList<ProjectObjectiveWithActivitiesDTO>>>
            ListByProjectWithActivitiesAsync(int projectId, int visitId, CancellationToken ct = default)
        {
            var objectives = await _uow.ProjectObjectives
                .ListByProjectWithActivitiesAsync(projectId, ct);

            var activityIds = CollectObjectiveActivityIds(objectives);

            var progressMap = await _uow.VisitObjectiveActivityProgresses
                .GetCumulativeProgressByProjectUpToVisitAndActivityIdsAsync(projectId, visitId, activityIds, ct);

            var dtoList = objectives
                .Select(o => new ProjectObjectiveWithActivitiesDTO
                {
                    Id = o.Id,
                    ProjectId = o.ProjectId,
                    ObjectiveTypeId = o.ObjectiveTypeId,
                    WeightedPercentage = o.WeightedPercentage,
                    ObjectiveTypeName = o.ObjectiveType.Name,
                    Objective = o.Objective,
                    Result = o.Result,
                    Activities = (o.Activities ?? Enumerable.Empty<ObjectiveActivity>())
                        .Select(a =>
                        {
                            var progress = progressMap.TryGetValue(a.ObjectiveActivityId, out var p) ? p : 0;
                            return new ObjectiveActivityListItemDTO
                            {
                                ObjectiveActivityId = a.ObjectiveActivityId,
                                ObjectiveId = a.ObjectiveId,
                                ActivityResult = a.ActivityResult,
                                ActionText = a.ActionText,
                                CreatedAt = a.CreatedAt,
                                ProgressPercentage = progress,
                                IsCompleted = progress >= 100
                            };
                        })
                        .ToList()
                })
                .ToList();

            return ServiceResult<IReadOnlyList<ProjectObjectiveWithActivitiesDTO>>.Ok(dtoList);
        }

        // ==================== PRIVATE HELPERS ====================

        private static ServiceResult<T> FailValidation<T>(string message)
            => ServiceResult<T>.Fail(message, ErrorType.Validation, ErrorCodes.Common.InvalidRequest);

        private static ServiceResult<T> FailNotFound<T>(string message)
            => ServiceResult<T>.Fail(message, ErrorType.NotFound, ErrorCodes.Common.NotFound);

        private static List<int> CollectObjectiveActivityIds(IEnumerable<ProjectObjective> objectives)
        {
            return objectives
                .SelectMany(o => o.Activities ?? Enumerable.Empty<ObjectiveActivity>())
                .Select(a => a.ObjectiveActivityId)
                .Distinct()
                .ToList();
        }
    }
}