using Microsoft.EntityFrameworkCore;
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
        private const string InvalidIdMessage = "Invalid id.";
        private const string ObjectiveIdRequiredMessage = "ObjectiveId is required.";
        private const string RequestRequiredMessage = "Request is required.";
        private const string ProjectObjectiveNotFoundMessage = "ProjectObjective not found.";
        private const string ObjectiveActivityNotFoundMessage = "ObjectiveActivity not found.";
        private const string InvalidVisitIdMessage = "Invalid visitId.";
        private const string InvalidActivityIdMessage = "Invalid activityId.";
        private const string ProgressRangeMessage = "Progress must be between 0 and 100.";
        private const string VisitNotFoundMessage = "Visit not found.";
        private const string ActivityResultMaxLengthMessage = "ActivityResult cannot exceed 1000 characters.";
        private const string ActionTextMaxLengthMessage = "ActionText cannot exceed 1000 characters.";

        private const int CompletedThreshold = 100;
        private const int MaxTextLength = 1000;

        private const int GeneralObjectiveTypeId = 1;

        private const int ProjectStateCompletedId = 2;
        private const int ProjectStateInProgressId = 3;

        private const decimal PercentMin = 0m;
        private const decimal PercentMax = 100m;

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
                    .Fail(ObjectiveIdRequiredMessage, ErrorType.Validation);

            var activities = await _uow.ObjectiveActivities.GetByObjectiveAsync(objectiveId, ct);

            // 1) IDs de actividades para consultar últimos snapshots
            var activityIds = activities.Select(a => a.ObjectiveActivityId).ToList();

            // 2) Mapa: ObjectiveActivityId -> ProgressPercentage (último snapshot)
            var latestProgressMap = await _uow.VisitObjectiveActivityProgresses
                .GetTotalProgressByActivityIdsAsync(activityIds, ct);

            // 3) Construir DTO sin depender de campos eliminados en ObjectiveActivity
            var dto = activities
                .OrderBy(a => a.ObjectiveActivityId)
                .Select(a =>
                {
                    var progress = GetProgressOrDefault(latestProgressMap, a.ObjectiveActivityId);

                    return new ObjectiveActivityListItemDTO
                    {
                        ObjectiveActivityId = a.ObjectiveActivityId,
                        ObjectiveId = a.ObjectiveId,
                        ActivityResult = a.ActivityResult ?? string.Empty,
                        ActionText = a.ActionText ?? string.Empty,
                        CreatedAt = a.CreatedAt,

                        ProgressPercentage = progress,
                        IsCompleted = progress >= CompletedThreshold
                    };
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
                    .Fail(InvalidIdMessage, ErrorType.Validation);

            var entity = await _uow.ObjectiveActivities.GetByIdWithRefsAsync(activityId, ct);
            if (entity is null)
                return ServiceResult<ObjectiveActivityDetailDTO>
                    .Fail(ObjectiveActivityNotFoundMessage, ErrorType.NotFound);

            var map = await _uow.VisitObjectiveActivityProgresses
                .GetTotalProgressByActivityIdsAsync(SingleIdArray(entity.ObjectiveActivityId), ct);

            var progress = GetProgressOrDefault(map, entity.ObjectiveActivityId);

            var dto = MapToDetailDto(entity, progress);
            return ServiceResult<ObjectiveActivityDetailDTO>.Ok(dto);
        }

        // ==================== WRITES ====================

        public async Task<ServiceResult<ObjectiveActivityDetailDTO>> CreateAsync(
            AddObjectiveActivityRequestDTO request,
            CancellationToken ct = default)
        {
            if (request is null)
                return ServiceResult<ObjectiveActivityDetailDTO>
                    .Fail(RequestRequiredMessage, ErrorType.Validation);

            if (request.ObjectiveId <= 0)
                return ServiceResult<ObjectiveActivityDetailDTO>
                    .Fail(ObjectiveIdRequiredMessage, ErrorType.Validation);

            // Validar que el ProjectObjective exista
            var objective = await _uow.ProjectObjectives.GetByIdAsync(
                new object[] { request.ObjectiveId }, ct);

            if (objective is null)
                return ServiceResult<ObjectiveActivityDetailDTO>
                    .Fail(ProjectObjectiveNotFoundMessage, ErrorType.Validation);

            var entity = new ObjectiveActivity
            {
                ObjectiveId = (int)request.ObjectiveId,
                ActivityResult = (request.ActivityResult ?? string.Empty).Trim(),
                ActionText = (request.ActionText ?? string.Empty).Trim(),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = null,
            };

            await _uow.ObjectiveActivities.AddAsync(entity, ct);
            await _uow.SaveChangesAsync(ct);

            var created = await _uow.ObjectiveActivities.GetByIdWithRefsAsync(entity.ObjectiveActivityId, ct)
                          ?? entity;

            var dto = MapToDetailDto(created, progressPercentage: 0);
            return ServiceResult<ObjectiveActivityDetailDTO>.Ok(dto);
        }

        public async Task<ServiceResult<ObjectiveActivityDetailDTO>> UpdateAsync(
            UpdateObjectiveActivityRequestDTO request,
            CancellationToken ct = default)
        {
            if (request is null || request.ObjectiveActivityId <= 0)
                return ServiceResult<ObjectiveActivityDetailDTO>
                    .Fail(InvalidIdMessage, ErrorType.Validation);

            if (request.ObjectiveId <= 0)
                return ServiceResult<ObjectiveActivityDetailDTO>
                    .Fail(ObjectiveIdRequiredMessage, ErrorType.Validation);

            // Validaciones StringLength
            var activityResult = (request.ActivityResult ?? string.Empty).Trim();
            if (activityResult.Length > MaxTextLength)
                return ServiceResult<ObjectiveActivityDetailDTO>
                    .Fail(ActivityResultMaxLengthMessage, ErrorType.Validation);

            var actionText = (request.ActionText ?? string.Empty).Trim();
            if (actionText.Length > MaxTextLength)
                return ServiceResult<ObjectiveActivityDetailDTO>
                    .Fail(ActionTextMaxLengthMessage, ErrorType.Validation);

            // 1) Cargar actividad
            var entity = await _uow.ObjectiveActivities.GetByIdAsync(
                new object[] { request.ObjectiveActivityId }, ct);

            if (entity is null)
                return ServiceResult<ObjectiveActivityDetailDTO>
                    .Fail(ObjectiveActivityNotFoundMessage, ErrorType.NotFound);

            // 2) Validar objetivo
            var objective = await _uow.ProjectObjectives.GetByIdAsync(
                new object[] { request.ObjectiveId }, ct);

            if (objective is null)
                return ServiceResult<ObjectiveActivityDetailDTO>
                    .Fail(ProjectObjectiveNotFoundMessage, ErrorType.Validation);

            // 3) Update actividad (solo metadata/textos)
            entity.ObjectiveId = request.ObjectiveId;
            entity.ActivityResult = activityResult;
            entity.ActionText = actionText;
            entity.UpdatedAt = DateTime.UtcNow;

            _uow.ObjectiveActivities.Update(entity);

            // 4) Guardar cambios
            await _uow.SaveChangesAsync(ct);

            // 5) Recargar con refs si aplica
            var updated = await _uow.ObjectiveActivities.GetByIdWithRefsAsync(entity.ObjectiveActivityId, ct)
                          ?? entity;

            // 6) Progreso actual desde snapshots (último)
            var map = await _uow.VisitObjectiveActivityProgresses
                .GetLatestProgressByActivityIdsAsync(SingleIdArray(updated.ObjectiveActivityId), ct);

            var progress = GetProgressOrDefault(map, updated.ObjectiveActivityId);

            return ServiceResult<ObjectiveActivityDetailDTO>.Ok(MapToDetailDto(updated, progress));
        }

        public async Task<ServiceResult<bool>> DeleteAsync(
            int activityId,
            CancellationToken ct = default)
        {
            if (activityId <= 0)
                return ServiceResult<bool>.Fail(InvalidIdMessage, ErrorType.Validation);

            var entity = await _uow.ObjectiveActivities.GetByIdAsync(
                new object[] { activityId }, ct);

            if (entity is null)
                return ServiceResult<bool>.Fail(ObjectiveActivityNotFoundMessage, ErrorType.NotFound);

            _uow.ObjectiveActivities.Remove(entity);
            await _uow.SaveChangesAsync(ct);

            return ServiceResult<bool>.Ok(true);
        }

        public async Task<ServiceResult<bool>> SetProgressAsync(
            int visitId,
            int activityId,
            int progressPercentage, // TOTAL ACUMULADO deseado (0..100)
            CancellationToken ct = default)
        {
            if (visitId <= 0)
                return ServiceResult<bool>.Fail(InvalidVisitIdMessage, ErrorType.Validation);

            if (activityId <= 0)
                return ServiceResult<bool>.Fail(InvalidActivityIdMessage, ErrorType.Validation);

            if (progressPercentage < 0 || progressPercentage > 100)
                return ServiceResult<bool>.Fail(ProgressRangeMessage, ErrorType.Validation);

            // 1) Validar Visit
            var visit = await _uow.Visits.GetByIdAsync(new object[] { visitId }, ct);
            if (visit is null)
                return ServiceResult<bool>.Fail(VisitNotFoundMessage, ErrorType.NotFound);

            // 2) Validar Activity
            var activity = await _uow.ObjectiveActivities.GetByIdAsync(new object[] { activityId }, ct);
            if (activity is null)
                return ServiceResult<bool>.Fail(ObjectiveActivityNotFoundMessage, ErrorType.NotFound);

            // 3) Calcular progreso acumulado ANTES de esta visita (VisitId < visitId)
            //    Nota: aquí asumimos que VisitId es incremental y define el orden temporal.
            //    Además: por cada (VisitId, ActivityId) tomamos el ÚLTIMO registro (por CreatedAt).
            var totalBeforeDict = await _uow.VisitObjectiveActivityProgresses
                .GetCumulativeProgressByProjectUpToVisitAndActivityIdsAsync(
                    projectId: visit.ProjectId,
                    visitId: visitId - 1,               // <- "hasta la anterior" por ID
                    activityIds: SingleIdArray(activityId),
                    ct: ct);

            var totalBeforeThisVisit = totalBeforeDict.TryGetValue(activityId, out var before) ? before : 0; // 0..100 clamped

            // 4) REGLA: permitir disminuir, pero NO por debajo de lo ya avanzado antes de esta visita
            if (progressPercentage < totalBeforeThisVisit)
            {
                return ServiceResult<bool>.Fail(
                    $"You cannot set progress below {totalBeforeThisVisit}% because that was achieved before this visit.",
                    ErrorType.Validation);
            }

            // 5) Nuevo delta de ESTA visita (lo que aporta esta visita para llegar al total deseado)
            var newDelta = progressPercentage - totalBeforeThisVisit; // 0..(100-totalBefore)

            // 6) Buscar el registro existente "más reciente" en esta visita (si existe)
            //    OJO: tu GenericRepository.FirstOrDefaultAsync no permite ordenar.
            //    Solución: usa Query() y ordena.
            var existing = await GetLatestProgressEntityForVisitAsync(visitId, activityId, ct);

            // 7) Upsert del delta
            if (existing is null)
            {
                // Si no hay cambio real, omite insertar (opcional)
                if (newDelta == 0)
                    return ServiceResult<bool>.Ok(true);

                var entity = new VisitObjectiveActivityProgress
                {
                    VisitId = visitId,
                    ObjectiveActivityId = activityId,
                    ProgressPercentage = newDelta,
                    CreatedAt = DateTime.UtcNow
                };

                await _uow.VisitObjectiveActivityProgresses.AddAsync(entity, ct);
            }
            else
            {
                existing.ProgressPercentage = newDelta;
                existing.CreatedAt = DateTime.UtcNow; // ideal: UpdatedAt
                _uow.VisitObjectiveActivityProgresses.Update(existing);
            }

            await _uow.SaveChangesAsync(ct);

            // 8) Recalcular ExecutionPercentage del proyecto
            await RecalculateProjectExecutionPercentageAsync(visit.ProjectId, ct);

            return ServiceResult<bool>.Ok(true);
        }

        // ==================== Helpers ====================

        private static ObjectiveActivityDetailDTO MapToDetailDto(ObjectiveActivity entity, int progressPercentage)
            => new ObjectiveActivityDetailDTO
            {
                ObjectiveActivityId = entity.ObjectiveActivityId,
                ObjectiveId = entity.ObjectiveId,
                ActivityResult = entity.ActivityResult ?? string.Empty,
                ImprovementAction = entity.ActionText ?? string.Empty,
                CreatedAt = entity.CreatedAt,
                UpdatedAt = entity.UpdatedAt,
                ProgressPercentage = progressPercentage,
                IsCompleted = progressPercentage >= CompletedThreshold
            };

        private static int[] SingleIdArray(int id) => new[] { id };

        private static int GetProgressOrDefault(IReadOnlyDictionary<int, int> map, int activityId)
            => map.TryGetValue(activityId, out var value) ? value : 0;

        private async Task<VisitObjectiveActivityProgress?> GetLatestProgressEntityForVisitAsync(
            int visitId,
            int activityId,
            CancellationToken ct)
        {
            return await _uow.VisitObjectiveActivityProgresses
                .Query(asNoTracking: false)
                .Where(x => x.VisitId == visitId && x.ObjectiveActivityId == activityId)
                .OrderByDescending(x => x.CreatedAt)
                .FirstOrDefaultAsync(ct);
        }

        private async Task RecalculateProjectExecutionPercentageAsync(int projectId, CancellationToken ct)
        {
            // 1) Traer objetivos con actividades (ya incluye Activities)
            var objectives = await _uow.ProjectObjectives.GetByProjectAsync(projectId, ct);

            // 2) Excluir objetivo general
            var scopedObjectives = objectives
                .Where(o => o.ObjectiveTypeId != GeneralObjectiveTypeId)
                .ToList();

            if (scopedObjectives.Count == 0)
            {
                await SetProjectExecutionAsync(projectId, 0m, ct);
                return;
            }

            // 3) Sacar todas las actividades de esos objetivos
            var activities = scopedObjectives
                .SelectMany(o => o.Activities)
                .ToList();

            if (activities.Count == 0)
            {
                // Por tu regla no debería pasar, pero dejamos seguro
                await SetProjectExecutionAsync(projectId, 0m, ct);
                return;
            }

            var activityIds = activities.Select(a => a.ObjectiveActivityId).Distinct().ToList();

            // 4) Total acumulado por actividad (SUM de deltas)
            var totalsByActivity = await _uow.VisitObjectiveActivityProgresses
                .GetTotalProgressByActivityIdsAsync(activityIds, ct);

            // 5) Calcular ponderado por objetivo
            decimal weightedSum = 0m;

            foreach (var obj in scopedObjectives)
            {
                var objActs = obj.Activities;
                if (objActs is null || objActs.Count == 0)
                    continue;

                decimal objAvg = (decimal)objActs
                    .Select(a => totalsByActivity.TryGetValue(a.ObjectiveActivityId, out var p) ? p : 0)
                    .Average(); // 0..100

                weightedSum += objAvg * obj.WeightedPercentage;
            }

            // Pesos (sin objetivo general) deben sumar 100 según tu regla
            var execution = weightedSum / 100m;

            // clamp y persistir
            execution = ClampPercentage(execution);
            await SetProjectExecutionAsync(projectId, Math.Round(execution, 2), ct);
        }

        private async Task SetProjectExecutionAsync(int projectId, decimal execution, CancellationToken ct)
        {
            var project = await _uow.Projects.GetByIdAsync(new object[] { projectId }, ct);
            if (project is null) return;

            var clamped = ClampPercentage(execution);

            project.ExecutionPercentage = clamped;
            project.ProjectStateId = (project.ExecutionPercentage >= PercentMax) ? ProjectStateCompletedId : ProjectStateInProgressId;

            _uow.Projects.Update(project);
            await _uow.SaveChangesAsync(ct);
        }

        private static decimal ClampPercentage(decimal value)
            => Math.Min(PercentMax, Math.Max(PercentMin, value));
    }
}