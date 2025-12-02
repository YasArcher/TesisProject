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
                    CreatedAt = a.CreatedAt
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
                IsCompleted = request.IsCompleted,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = null
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

            var entity = await _uow.ObjectiveActivities.GetByIdAsync(
                new object[] { request.ObjectiveActivityId }, ct);

            if (entity is null)
                return ServiceResult<ObjectiveActivityDetailDTO>
                    .Fail("ObjectiveActivity not found.", ErrorType.NotFound);

            // Validar que el Objective aún exista
            var objective = await _uow.ProjectObjectives.GetByIdAsync(
                new object[] { request.ObjectiveId }, ct);

            if (objective is null)
                return ServiceResult<ObjectiveActivityDetailDTO>
                    .Fail("ProjectObjective not found.", ErrorType.Validation);

            entity.ObjectiveId = request.ObjectiveId;
            entity.ActivityResult = (request.ActivityResult ?? string.Empty).Trim();
            entity.ActionText = (request.ActionText ?? string.Empty).Trim();
            entity.IsCompleted = request.IsCompleted;
            entity.UpdatedAt = DateTime.UtcNow;

            _uow.ObjectiveActivities.Update(entity);
            await _uow.SaveChangesAsync(ct);

            var updated = await _uow.ObjectiveActivities.GetByIdWithRefsAsync(entity.ObjectiveActivityId, ct)
                          ?? entity;

            var dto = MapToDetailDto(updated);
            return ServiceResult<ObjectiveActivityDetailDTO>.Ok(dto);
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

        public async Task<ServiceResult<bool>> SetCompletedAsync(
            int activityId,
            bool isCompleted,
            CancellationToken ct = default)
        {
            if (activityId <= 0)
                return ServiceResult<bool>.Fail("Invalid id.", ErrorType.Validation);

            var entity = await _uow.ObjectiveActivities.GetByIdAsync(
                new object[] { activityId }, ct);

            if (entity is null)
                return ServiceResult<bool>.Fail("ObjectiveActivity not found.", ErrorType.NotFound);

            entity.IsCompleted = isCompleted;
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
                UpdatedAt = entity.UpdatedAt
            };
    }
}
