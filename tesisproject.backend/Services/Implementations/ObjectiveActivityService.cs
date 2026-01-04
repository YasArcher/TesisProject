using tesisproject.backend.Services.Interfaces;
using tesisproject.backend.UnitOfWork.Interfaces;
using tesisproject.shared.DTOs.ObjectiveActivity.Request;
using tesisproject.shared.DTOs.ObjectiveActivity.Response;
using tesisproject.shared.Entities.Core;
using tesisproject.shared.Responses;

namespace tesisproject.backend.Services.Implementations
{
    public class ObjectiveActivityService : IObjectiveActivityService
    {
        private readonly IUnitOfWork _uow;

        public ObjectiveActivityService(IUnitOfWork uow)
        {
            _uow = uow;
        }

        // ==================== READS ====================

        public async Task<ServiceResult<IReadOnlyList<ObjectiveActivityListItemDTO>>> ListByObjectiveAsync(
            int objectiveId,
            CancellationToken ct = default)
        {
            if (objectiveId <= 0)
                return ServiceResult<IReadOnlyList<ObjectiveActivityListItemDTO>>
                    .Fail("ObjectiveId is required.", ErrorType.Validation);

            var entities = await _uow.ObjectiveActivities.GetByObjectiveAsync(objectiveId, ct);

            var dto = entities
                .OrderBy(a => a.ObjectiveActivityId)
                .Select(a => new ObjectiveActivityListItemDTO
                {
                    ObjectiveActivityId = a.ObjectiveActivityId,
                    ObjectiveId = a.ObjectiveId,
                    ActivityResult = a.ActivityResult ?? string.Empty,
                    ActionText = a.ActionText ?? string.Empty,
                    IsCompleted = a.IsCompleted,
                    CreatedAt = a.CreatedAt,
                    ProgressPercentage = a.ProgressPercentage
                })
                .ToList();

            return ServiceResult<IReadOnlyList<ObjectiveActivityListItemDTO>>.Ok(dto);
        }

        public async Task<ServiceResult<ObjectiveActivityDetailDTO>> GetByIdAsync(
            int activityId,
            CancellationToken ct = default)
        {
            if (activityId <= 0)
                return ServiceResult<ObjectiveActivityDetailDTO>
                    .Fail("Invalid id.", ErrorType.Validation);

            var entity = await _uow.ObjectiveActivities.GetByIdWithRefsAsync(activityId, ct);
            if (entity is null)
                return ServiceResult<ObjectiveActivityDetailDTO>
                    .Fail("ObjectiveActivity not found.", ErrorType.NotFound);

            var dto = MapToDetailDto(entity);
            return ServiceResult<ObjectiveActivityDetailDTO>.Ok(dto);
        }

        // ==================== WRITES ====================

        public async Task<ServiceResult<ObjectiveActivityDetailDTO>> CreateAsync(
            AddObjectiveActivityRequestDTO request,
            CancellationToken ct = default)
        {
            if (request is null)
                return ServiceResult<ObjectiveActivityDetailDTO>
                    .Fail("Request is required.", ErrorType.Validation);

            if (request.ObjectiveId <= 0)
                return ServiceResult<ObjectiveActivityDetailDTO>
                    .Fail("ObjectiveId is required.", ErrorType.Validation);

            // Validar que el ProjectObjective exista
            var objective = await _uow.ProjectObjectives.GetByIdAsync(
                new object[] { request.ObjectiveId }, ct);

            if (objective is null)
                return ServiceResult<ObjectiveActivityDetailDTO>
                    .Fail("ProjectObjective not found.", ErrorType.Validation);

            var entity = new ObjectiveActivity
            {
                ObjectiveId = (int)request.ObjectiveId,
                ActivityResult = (request.ActivityResult ?? string.Empty).Trim(),
                ActionText = (request.ActionText ?? string.Empty).Trim(),
                ProgressPercentage = Math.Clamp(request.ProgressPercentage, 0, 100),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = null,
            };


            await _uow.ObjectiveActivities.AddAsync(entity, ct);
            await _uow.SaveChangesAsync(ct);

            var created = await _uow.ObjectiveActivities.GetByIdWithRefsAsync(entity.ObjectiveActivityId, ct)
                          ?? entity;

            var dto = MapToDetailDto(created);
            return ServiceResult<ObjectiveActivityDetailDTO>.Ok(dto);
        }

        public async Task<ServiceResult<ObjectiveActivityDetailDTO>> UpdateAsync(
            UpdateObjectiveActivityRequestDTO request,
            CancellationToken ct = default)
        {
            if (request is null || request.ObjectiveActivityId <= 0)
                return ServiceResult<ObjectiveActivityDetailDTO>
                    .Fail("Invalid id.", ErrorType.Validation);

            if (request.ObjectiveId <= 0)
                return ServiceResult<ObjectiveActivityDetailDTO>
                    .Fail("ObjectiveId is required.", ErrorType.Validation);

            // Validaciones alineadas a tu entidad (StringLength + Range)
            var activityResult = (request.ActivityResult ?? string.Empty).Trim();
            if (activityResult.Length > 1000)
                return ServiceResult<ObjectiveActivityDetailDTO>
                    .Fail("ActivityResult cannot exceed 1000 characters.", ErrorType.Validation);

            var actionText = (request.ActionText ?? string.Empty).Trim();
            if (actionText.Length > 1000)
                return ServiceResult<ObjectiveActivityDetailDTO>
                    .Fail("ActionText cannot exceed 1000 characters.", ErrorType.Validation);

            if (request.ProgressPercentage < 0 || request.ProgressPercentage > 100)
                return ServiceResult<ObjectiveActivityDetailDTO>
                    .Fail("Progress must be between 0 and 100.", ErrorType.Validation);

            // 1) Cargar actividad
            var entity = await _uow.ObjectiveActivities.GetByIdAsync(
                new object[] { request.ObjectiveActivityId }, ct);

            if (entity is null)
                return ServiceResult<ObjectiveActivityDetailDTO>
                    .Fail("ObjectiveActivity not found.", ErrorType.NotFound);

            // 2) Validar objetivo
            var objective = await _uow.ProjectObjectives.GetByIdAsync(
                new object[] { request.ObjectiveId }, ct);

            if (objective is null)
                return ServiceResult<ObjectiveActivityDetailDTO>
                    .Fail("ProjectObjective not found.", ErrorType.Validation);

            // Regla: no permitir disminuir progreso
            if (request.ProgressPercentage < entity.ProgressPercentage)
                return ServiceResult<ObjectiveActivityDetailDTO>
                    .Fail("Progress cannot be decreased.", ErrorType.Validation);

            // 3) Update actividad
            entity.ObjectiveId = request.ObjectiveId;
            entity.ActivityResult = activityResult;
            entity.ActionText = actionText;
            entity.ProgressPercentage = request.ProgressPercentage;
            entity.UpdatedAt = DateTime.UtcNow;

            _uow.ObjectiveActivities.Update(entity);

            // 4) Recalcular % del proyecto (fuente de verdad: BD)
            //    Nota: este cálculo NO depende del front.
            var projectId = objective.ProjectId;

            var objectives = await _uow.ProjectObjectives
                .ListByProjectWithActivitiesAsync(projectId, ct);

            // Si no hay objetivos, dejamos 0
            decimal totalWeight = 0m;
            decimal weightedSum = 0m;

            foreach (var o in objectives)
            {
                var weight = (decimal)o.WeightedPercentage;

                decimal objectiveProgress = 0m;

                if (o.Activities is not null && o.Activities.Count > 0)
                    objectiveProgress = (decimal)o.Activities.Average(a => a.ProgressPercentage);

                totalWeight += weight;
                weightedSum += weight * objectiveProgress;
            }

            decimal projectExecution = 0m;

            if (totalWeight > 0m)
                projectExecution = weightedSum / totalWeight; // normalizado 0..100

            // Clamp defensivo y redondeo
            if (projectExecution < 0m) projectExecution = 0m;
            if (projectExecution > 100m) projectExecution = 100m;

            projectExecution = Math.Round(projectExecution, 2);

            // 5) Persistir ExecutionPercentage en Project
            var project = await _uow.Projects.GetByIdAsync(new object[] { projectId }, ct);

            if (project is null)
                return ServiceResult<ObjectiveActivityDetailDTO>
                    .Fail("Project not found.", ErrorType.NotFound);

            project.ExecutionPercentage = projectExecution;

            _uow.Projects.Update(project);

            // 6) Guardar todo en una sola transacción
            await _uow.SaveChangesAsync(ct);

            // 7) Respuesta (con refs si tu repo ya lo soporta)
            var updated = await _uow.ObjectiveActivities.GetByIdWithRefsAsync(entity.ObjectiveActivityId, ct)
                          ?? entity;

            return ServiceResult<ObjectiveActivityDetailDTO>.Ok(MapToDetailDto(updated));

        }

        public async Task<ServiceResult<bool>> DeleteAsync(
            int activityId,
            CancellationToken ct = default)
        {
            if (activityId <= 0)
                return ServiceResult<bool>.Fail("Invalid id.", ErrorType.Validation);

            var entity = await _uow.ObjectiveActivities.GetByIdAsync(
                new object[] { activityId }, ct);

            if (entity is null)
                return ServiceResult<bool>.Fail("ObjectiveActivity not found.", ErrorType.NotFound);

            _uow.ObjectiveActivities.Remove(entity);
            await _uow.SaveChangesAsync(ct);

            return ServiceResult<bool>.Ok(true);
        }

        public async Task<ServiceResult<bool>> SetProgressAsync(
    int activityId,
    int progressPercentage,
    CancellationToken ct = default)
        {
            if (activityId <= 0)
                return ServiceResult<bool>.Fail("Invalid id.", ErrorType.Validation);

            if (progressPercentage < 0 || progressPercentage > 100)
                return ServiceResult<bool>.Fail("Progress must be between 0 and 100.", ErrorType.Validation);

            var entity = await _uow.ObjectiveActivities.GetByIdAsync(new object[] { activityId }, ct);

            if (entity is null)
                return ServiceResult<bool>.Fail("ObjectiveActivity not found.", ErrorType.NotFound);

            entity.ProgressPercentage = progressPercentage;
            entity.UpdatedAt = DateTime.UtcNow;

            _uow.ObjectiveActivities.Update(entity);
            await _uow.SaveChangesAsync(ct);

            return ServiceResult<bool>.Ok(true);
        }


        // ==================== Helpers ====================

        private static ObjectiveActivityDetailDTO MapToDetailDto(ObjectiveActivity entity)
            => new ObjectiveActivityDetailDTO
            {
                ObjectiveActivityId = entity.ObjectiveActivityId,
                ObjectiveId = entity.ObjectiveId,
                ActivityResult = entity.ActivityResult ?? string.Empty,
                ImprovementAction = entity.ActionText ?? string.Empty,
                IsCompleted = entity.IsCompleted,
                CreatedAt = entity.CreatedAt,
                UpdatedAt = entity.UpdatedAt,
                ProgressPercentage = entity.ProgressPercentage
            };
    }
}
