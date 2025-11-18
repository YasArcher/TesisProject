using tesisproject.backend.Services.Interfaces;
using tesisproject.backend.UnitOfWork.Interfaces;
using tesisproject.shared.DTOs.ObjectiveActivityUser.Request;
using tesisproject.shared.DTOs.ObjectiveActivityUser.Response;
using tesisproject.shared.Entities.Core;
using tesisproject.shared.Responses;

namespace tesisproject.backend.Services.Implementations
{
    public class ObjectiveActivityUserService : IObjectiveActivityUserService
    {
        private readonly IUnitOfWork _uow;

        public ObjectiveActivityUserService(IUnitOfWork uow)
        {
            _uow = uow;
        }

        // =============== READS ===============

        public async Task<ServiceResult<IReadOnlyList<ObjectiveActivityUserDTO>>> ListByActivityAsync(
            int objectiveActivityId,
            CancellationToken ct = default)
        {
            if (objectiveActivityId <= 0)
                return ServiceResult<IReadOnlyList<ObjectiveActivityUserDTO>>
                    .Fail("ObjectiveActivityId is required.", ErrorType.Validation);

            var entities = await _uow.ObjectiveActivityUsers.GetByActivityAsync(objectiveActivityId, ct);

            var dto = entities
                .OrderBy(x => x.Id)
                .Select(MapToDto)
                .ToList();

            return ServiceResult<IReadOnlyList<ObjectiveActivityUserDTO>>.Ok(dto);
        }

        // =============== WRITES ===============

        public async Task<ServiceResult<ObjectiveActivityUserDTO>> AssignAsync(
            AssignObjectiveActivityUserRequestDTO request,
            CancellationToken ct = default)
        {
            if (request is null)
                return ServiceResult<ObjectiveActivityUserDTO>
                    .Fail("Request is required.", ErrorType.Validation);

            if (request.ObjectiveActivityId <= 0)
                return ServiceResult<ObjectiveActivityUserDTO>
                    .Fail("ObjectiveActivityId is required.", ErrorType.Validation);

            if (request.UserId <= 0)
                return ServiceResult<ObjectiveActivityUserDTO>
                    .Fail("UserId is required.", ErrorType.Validation);

            if (request.VisitId <= 0)
                return ServiceResult<ObjectiveActivityUserDTO>
                    .Fail("VisitId is required.", ErrorType.Validation);

            // Validar que la actividad exista
            var activity = await _uow.ObjectiveActivities.GetByIdAsync(
                new object[] { request.ObjectiveActivityId }, ct);

            if (activity is null)
                return ServiceResult<ObjectiveActivityUserDTO>
                    .Fail("ObjectiveActivity not found.", ErrorType.Validation);

            // Validar que la visita exista
            var visit = await _uow.Visits.GetByIdAsync(
                new object[] { request.VisitId }, ct);

            if (visit is null)
                return ServiceResult<ObjectiveActivityUserDTO>
                    .Fail("Visit not found.", ErrorType.Validation);

            // Evitar duplicados actividad-usuario-visita
            var exists = await _uow.ObjectiveActivityUsers.ExistsAssignmentAsync(
                request.ObjectiveActivityId,
                request.UserId,
                request.VisitId,
                ct);

            if (exists)
                return ServiceResult<ObjectiveActivityUserDTO>
                    .Fail("Assignment already exists for this activity, user and visit.", ErrorType.Validation);

            var entity = new ObjectiveActivityUser
            {
                ObjectiveActivityId = request.ObjectiveActivityId,
                UserId = request.UserId,
                VisitId = request.VisitId,
                WeeklyHours = request.WeeklyHours,
                RoleDescription = (request.RoleDescription ?? string.Empty).Trim(),
                ReportNotes = (request.ReportNotes ?? string.Empty).Trim()
            };

            await _uow.ObjectiveActivityUsers.AddAsync(entity, ct);
            await _uow.SaveChangesAsync(ct);

            var dto = MapToDto(entity);
            return ServiceResult<ObjectiveActivityUserDTO>.Ok(dto);
        }

        public async Task<ServiceResult<ObjectiveActivityUserDTO>> UpdateAsync(
            UpdateObjectiveActivityUserRequestDTO request,
            CancellationToken ct = default)
        {
            if (request is null || request.Id <= 0)
                return ServiceResult<ObjectiveActivityUserDTO>
                    .Fail("Invalid id.", ErrorType.Validation);

            var entity = await _uow.ObjectiveActivityUsers.GetByIdAsync(
                new object[] { request.Id }, ct);

            if (entity is null)
                return ServiceResult<ObjectiveActivityUserDTO>
                    .Fail("Assignment not found.", ErrorType.NotFound);

            entity.WeeklyHours = request.WeeklyHours;
            entity.RoleDescription = (request.RoleDescription ?? string.Empty).Trim();
            entity.ReportNotes = (request.ReportNotes ?? string.Empty).Trim();

            _uow.ObjectiveActivityUsers.Update(entity);
            await _uow.SaveChangesAsync(ct);

            var dto = MapToDto(entity);
            return ServiceResult<ObjectiveActivityUserDTO>.Ok(dto);
        }

        public async Task<ServiceResult<bool>> UnassignAsync(
            int id,
            CancellationToken ct = default)
        {
            if (id <= 0)
                return ServiceResult<bool>
                    .Fail("Invalid id.", ErrorType.Validation);

            var entity = await _uow.ObjectiveActivityUsers.GetByIdAsync(
                new object[] { id }, ct);

            if (entity is null)
                return ServiceResult<bool>
                    .Fail("Assignment not found.", ErrorType.NotFound);

            _uow.ObjectiveActivityUsers.Remove(entity);
            await _uow.SaveChangesAsync(ct);

            return ServiceResult<bool>.Ok(true);
        }

        // =============== Helpers ===============

        private static ObjectiveActivityUserDTO MapToDto(ObjectiveActivityUser entity)
            => new ObjectiveActivityUserDTO
            {
                Id = entity.Id,
                ObjectiveActivityId = entity.ObjectiveActivityId,
                UserId = entity.UserId,
                VisitId = entity.VisitId,
                WeeklyHours = entity.WeeklyHours,
                RoleDescription = entity.RoleDescription,
                ReportNotes = entity.ReportNotes
            };
    }
}