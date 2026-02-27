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
        private const string RequestRequiredMessage = "Request is required.";
        private const string ObjectiveActivityIdRequiredMessage = "ObjectiveActivityId is required.";
        private const string UserIdRequiredMessage = "UserId is required.";
        private const string VisitIdRequiredMessage = "VisitId is required.";

        private const string InvalidIdMessage = "Invalid id.";
        private const string ObjectiveActivityNotFoundMessage = "ObjectiveActivity not found.";
        private const string VisitNotFoundMessage = "Visit not found.";
        private const string AssignmentNotFoundMessage = "Assignment not found.";
        private const string AssignmentAlreadyExistsMessage = "Assignment already exists for this activity, user and visit.";

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
                    .Fail(ObjectiveActivityIdRequiredMessage, ErrorType.Validation);

            var assignments = await _uow.ObjectiveActivityUsers.GetByActivityAsync(objectiveActivityId, ct);

            var dto = assignments
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
                    .Fail(RequestRequiredMessage, ErrorType.Validation);

            if (request.ObjectiveActivityId <= 0)
                return ServiceResult<ObjectiveActivityUserDTO>
                    .Fail(ObjectiveActivityIdRequiredMessage, ErrorType.Validation);

            if (request.UserId <= 0)
                return ServiceResult<ObjectiveActivityUserDTO>
                    .Fail(UserIdRequiredMessage, ErrorType.Validation);

            if (request.VisitId <= 0)
                return ServiceResult<ObjectiveActivityUserDTO>
                    .Fail(VisitIdRequiredMessage, ErrorType.Validation);

            // Validar que la actividad exista
            var activity = await _uow.ObjectiveActivities.GetByIdAsync(
                Key(request.ObjectiveActivityId), ct);

            if (activity is null)
                return ServiceResult<ObjectiveActivityUserDTO>
                    .Fail(ObjectiveActivityNotFoundMessage, ErrorType.Validation);

            // Validar que la visita exista
            var visit = await _uow.Visits.GetByIdAsync(
                Key(request.VisitId), ct);

            if (visit is null)
                return ServiceResult<ObjectiveActivityUserDTO>
                    .Fail(VisitNotFoundMessage, ErrorType.Validation);

            // Evitar duplicados actividad-usuario-visita
            var exists = await _uow.ObjectiveActivityUsers.ExistsAssignmentAsync(
                request.ObjectiveActivityId,
                request.UserId,
                request.VisitId,
                ct);

            if (exists)
                return ServiceResult<ObjectiveActivityUserDTO>
                    .Fail(AssignmentAlreadyExistsMessage, ErrorType.Validation);

            var entity = new ObjectiveActivityUser
            {
                ObjectiveActivityId = request.ObjectiveActivityId,
                UserId = request.UserId,
                VisitId = request.VisitId,
                WeeklyHours = request.WeeklyHours,
                RoleDescription = NormalizeText(request.RoleDescription),
                ReportNotes = NormalizeText(request.ReportNotes)
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
                    .Fail(InvalidIdMessage, ErrorType.Validation);

            var entity = await _uow.ObjectiveActivityUsers.GetByIdAsync(
                Key(request.Id), ct);

            if (entity is null)
                return ServiceResult<ObjectiveActivityUserDTO>
                    .Fail(AssignmentNotFoundMessage, ErrorType.NotFound);

            entity.WeeklyHours = request.WeeklyHours;
            entity.RoleDescription = NormalizeText(request.RoleDescription);
            entity.ReportNotes = NormalizeText(request.ReportNotes);

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
                    .Fail(InvalidIdMessage, ErrorType.Validation);

            var entity = await _uow.ObjectiveActivityUsers.GetByIdAsync(
                Key(id), ct);

            if (entity is null)
                return ServiceResult<bool>
                    .Fail(AssignmentNotFoundMessage, ErrorType.NotFound);

            _uow.ObjectiveActivityUsers.Remove(entity);
            await _uow.SaveChangesAsync(ct);

            return ServiceResult<bool>.Ok(true);
        }

        // =============== Helpers ===============

        private static object[] Key(int id) => new object[] { id };

        private static string NormalizeText(string? value)
            => (value ?? string.Empty).Trim();

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