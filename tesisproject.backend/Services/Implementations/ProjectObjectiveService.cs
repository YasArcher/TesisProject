using tesisproject.backend.Services.Interfaces;
using tesisproject.backend.UnitOfWork.Interfaces;
using tesisproject.shared.DTOs.ObjectiveActivity.Response;
using tesisproject.shared.DTOs.ProjectObjective.Request;
using tesisproject.shared.DTOs.ProjectObjective.Response;
using tesisproject.shared.Entities.Core;
using tesisproject.shared.Responses;

namespace tesisproject.backend.Services.Implementations
{
    public class ProjectObjectiveService : IProjectObjectiveService
    {
        private readonly IUnitOfWork _uow;

        public ProjectObjectiveService(IUnitOfWork uow)
        {
            _uow = uow;
        }

        // ==================== READS ====================

        public async Task<ServiceResult<IReadOnlyList<ProjectObjectiveListItemDTO>>> ListByProjectAsync(
            int projectId,
            CancellationToken ct = default)
        {
            if (projectId <= 0)
                return ServiceResult<IReadOnlyList<ProjectObjectiveListItemDTO>>
                    .Fail("ProjectId is required.", ErrorType.Validation);

            var entities = await _uow.ProjectObjectives.GetByProjectAsync(projectId, ct);

            var dto = entities
                .OrderBy(o => o.Id)
                .Select(o => new ProjectObjectiveListItemDTO
                {
                    Id = o.Id,
                    ProjectId = o.ProjectId,
                    ObjectiveTypeId = o.ObjectiveTypeId,
                    ObjectiveTypeName = o.ObjectiveType?.Name ?? string.Empty,
                    Objective = o.Objetive,          // propiedad de la entidad
                    Result = o.Result,
                    ActivitiesCount = o.Activities?.Count ?? 0
                })
                .ToList();

            return ServiceResult<IReadOnlyList<ProjectObjectiveListItemDTO>>.Ok(dto);
        }

        public async Task<ServiceResult<ProjectObjectiveDetailDTO>> GetByIdAsync(
            int id,
            CancellationToken ct = default)
        {
            if (id <= 0)
                return ServiceResult<ProjectObjectiveDetailDTO>
                    .Fail("Invalid id.", ErrorType.Validation);

            var entity = await _uow.ProjectObjectives.GetByIdWithRefsAsync(id, ct);
            if (entity is null)
                return ServiceResult<ProjectObjectiveDetailDTO>
                    .Fail("ProjectObjective not found.", ErrorType.NotFound);

            var dto = new ProjectObjectiveDetailDTO
            {
                Id = entity.Id,
                ProjectId = entity.ProjectId,
                ObjectiveTypeId = entity.ObjectiveTypeId,
                ObjectiveTypeName = entity.ObjectiveType?.Name ?? string.Empty,
                Objective = entity.Objetive,
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
                return ServiceResult<ProjectObjectiveDetailDTO>
                    .Fail("Request is required.", ErrorType.Validation);

            if (request.ProjectId <= 0)
                return ServiceResult<ProjectObjectiveDetailDTO>
                    .Fail("ProjectId is required.", ErrorType.Validation);

            if (request.ObjectiveTypeId <= 0)
                return ServiceResult<ProjectObjectiveDetailDTO>
                    .Fail("ObjectiveTypeId is required.", ErrorType.Validation);

            var objectiveText = (request.Objective ?? string.Empty).Trim();
            var resultText = (request.Result ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(objectiveText))
                return ServiceResult<ProjectObjectiveDetailDTO>
                    .Fail("Objective is required.", ErrorType.Validation);

            if (string.IsNullOrWhiteSpace(resultText))
                return ServiceResult<ProjectObjectiveDetailDTO>
                    .Fail("Result is required.", ErrorType.Validation);

            // Validar que el ObjectiveType exista y esté activo
            var objectiveType = await _uow.ObjectiveTypes.GetByIdAsync(
                new object[] { request.ObjectiveTypeId }, ct);

            if (objectiveType is null || !objectiveType.IsActive)
                return ServiceResult<ProjectObjectiveDetailDTO>
                    .Fail("ObjectiveType is invalid or inactive.", ErrorType.Validation);

            var entity = new ProjectObjective
            {
                ProjectId = request.ProjectId,
                ObjectiveTypeId = request.ObjectiveTypeId,
                Objetive = objectiveText,
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
                Objective = created.Objetive,
                Result = created.Result
            };

            return ServiceResult<ProjectObjectiveDetailDTO>.Ok(dto);
        }

        public async Task<ServiceResult<ProjectObjectiveDetailDTO>> UpdateAsync(
            UpdateProjectObjectiveRequestDTO request,
            CancellationToken ct = default)
        {
            if (request is null || request.Id <= 0)
                return ServiceResult<ProjectObjectiveDetailDTO>
                    .Fail("Invalid id.", ErrorType.Validation);

            if (request.ProjectId <= 0)
                return ServiceResult<ProjectObjectiveDetailDTO>
                    .Fail("ProjectId is required.", ErrorType.Validation);

            if (request.ObjectiveTypeId <= 0)
                return ServiceResult<ProjectObjectiveDetailDTO>
                    .Fail("ObjectiveTypeId is required.", ErrorType.Validation);

            var objectiveText = (request.Objective ?? string.Empty).Trim();
            var resultText = (request.Result ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(objectiveText))
                return ServiceResult<ProjectObjectiveDetailDTO>
                    .Fail("Objective is required.", ErrorType.Validation);

            if (string.IsNullOrWhiteSpace(resultText))
                return ServiceResult<ProjectObjectiveDetailDTO>
                    .Fail("Result is required.", ErrorType.Validation);

            var entity = await _uow.ProjectObjectives.GetByIdAsync(
                new object[] { request.Id }, ct);

            if (entity is null)
                return ServiceResult<ProjectObjectiveDetailDTO>
                    .Fail("ProjectObjective not found.", ErrorType.NotFound);

            // Validar ObjectiveType
            var objectiveType = await _uow.ObjectiveTypes.GetByIdAsync(
                new object[] { request.ObjectiveTypeId }, ct);

            if (objectiveType is null || !objectiveType.IsActive)
                return ServiceResult<ProjectObjectiveDetailDTO>
                    .Fail("ObjectiveType is invalid or inactive.", ErrorType.Validation);

            entity.ProjectId = request.ProjectId;
            entity.ObjectiveTypeId = request.ObjectiveTypeId;
            entity.Objetive = objectiveText;
            entity.Result = resultText;

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
                Objective = updated.Objetive,
                Result = updated.Result
            };

            return ServiceResult<ProjectObjectiveDetailDTO>.Ok(dto);
        }

        public async Task<ServiceResult<bool>> DeleteAsync(
            int id,
            CancellationToken ct = default)
        {
            if (id <= 0)
                return ServiceResult<bool>.Fail("Invalid id.", ErrorType.Validation);

            var entity = await _uow.ProjectObjectives.GetByIdAsync(new object[] { id }, ct);
            if (entity is null)
                return ServiceResult<bool>.Fail("ProjectObjective not found.", ErrorType.NotFound);

            _uow.ProjectObjectives.Remove(entity);
            await _uow.SaveChangesAsync(ct);

            return ServiceResult<bool>.Ok(true);
        }

        public async Task<ServiceResult<IReadOnlyList<ProjectObjectiveWithActivitiesDTO>>>
        ListByProjectWithActivitiesAsync(int projectId, CancellationToken ct = default)
        {
            var objectives = await _uow.ProjectObjectives
                .ListByProjectWithActivitiesAsync(projectId, ct);

            // 1) Reunir todos los IDs de actividades del proyecto
            var activityIds = objectives
                .SelectMany(o => o.Activities ?? new List<ObjectiveActivity>())
                .Select(a => a.ObjectiveActivityId)
                .Distinct()
                .ToList();

            // 2) Mapa: ObjectiveActivityId -> último progreso
            var progressMap = await _uow.VisitObjectiveActivityProgresses
                .GetLatestProgressByActivityIdsAsync(activityIds, ct);

            // 3) Mapear a DTO usando el progreso desde snapshots
            var dtoList = objectives
                .Select(o => new ProjectObjectiveWithActivitiesDTO
                {
                    Id = o.Id,
                    ProjectId = o.ProjectId,
                    ObjectiveTypeId = o.ObjectiveTypeId,
                    WeightedPercentage = o.WeightedPercentage,
                    ObjectiveTypeName = o.ObjectiveType.Name,
                    Objective = o.Objetive,
                    Result = o.Result,
                    Activities = (o.Activities ?? new List<ObjectiveActivity>())
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

    }
}
