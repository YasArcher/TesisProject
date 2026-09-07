using tesisproject.backend.Services.Interfaces;
using tesisproject.backend.Services.Unified.Interfaces;
using tesisproject.backend.UnitOfWork.Unified.Interfaces;
using tesisproject.shared.DTOs.ObjectiveActivityUser.Request;
using tesisproject.shared.DTOs.ObjectiveActivityUser.Response;
using tesisproject.backend.Data.UnifiedEntities.Core;
using tesisproject.shared.Errors;
using tesisproject.shared.Responses;

namespace tesisproject.backend.Services.Unified.Implementations
{
    public class UnifiedObjectiveActivityUserService : IUnifiedObjectiveActivityUserService
    {
        private readonly IUnifiedUnitOfWork _uow;

        public UnifiedObjectiveActivityUserService(IUnifiedUnitOfWork uow)
        {
            _uow = uow;
        }

        // =============== READS ===============

        public async Task<ServiceResult<IReadOnlyList<ObjectiveActivityUserDTO>>> ListByActivityAsync(
            int objectiveActivityId,
            CancellationToken ct = default)
        {
            if (objectiveActivityId <= 0)
            {
                return ServiceResult<IReadOnlyList<ObjectiveActivityUserDTO>>.Fail(
                    ErrorMessages.ObjectiveActivityUser.ObjectiveActivityIdRequired,
                    ErrorType.Validation,
                    ErrorCodes.ObjectiveActivityUser.ObjectiveActivityIdRequired);
            }

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
            {
                return ServiceResult<ObjectiveActivityUserDTO>.Fail(
                    ErrorMessages.Common.RequestRequired,
                    ErrorType.Validation,
                    ErrorCodes.Common.RequestRequired);
            }

            if (request.ObjectiveActivityId <= 0)
            {
                return ServiceResult<ObjectiveActivityUserDTO>.Fail(
                    ErrorMessages.ObjectiveActivityUser.ObjectiveActivityIdRequired,
                    ErrorType.Validation,
                    ErrorCodes.ObjectiveActivityUser.ObjectiveActivityIdRequired);
            }

            if (request.UserId <= 0)
            {
                return ServiceResult<ObjectiveActivityUserDTO>.Fail(
                    ErrorMessages.ObjectiveActivityUser.UserIdRequired,
                    ErrorType.Validation,
                    ErrorCodes.ObjectiveActivityUser.UserIdRequired);
            }

            if (request.VisitId <= 0)
            {
                return ServiceResult<ObjectiveActivityUserDTO>.Fail(
                    ErrorMessages.ObjectiveActivityUser.VisitIdRequired,
                    ErrorType.Validation,
                    ErrorCodes.ObjectiveActivityUser.VisitIdRequired);
            }

            // Validar que la actividad exista
            var activity = await _uow.ObjectiveActivities.GetByIdAsync(
                Key(request.ObjectiveActivityId), ct);

            if (activity is null)
            {
                return ServiceResult<ObjectiveActivityUserDTO>.Fail(
                    ErrorMessages.ObjectiveActivityUser.ObjectiveActivityNotFound,
                    ErrorType.Validation,
                    ErrorCodes.ObjectiveActivityUser.ObjectiveActivityNotFound);
            }

            // Validar que la visita exista
            var visit = await _uow.Visits.GetByIdAsync(
                Key(request.VisitId), ct);

            if (visit is null)
            {
                return ServiceResult<ObjectiveActivityUserDTO>.Fail(
                    ErrorMessages.Visit.NotFound,
                    ErrorType.Validation,
                    ErrorCodes.Visit.NotFound);
            }

            // Evitar duplicados actividad-usuario-visita
            var exists = await _uow.ObjectiveActivityUsers.ExistsAssignmentAsync(
                request.ObjectiveActivityId,
                request.UserId,
                request.VisitId,
                ct);

            if (exists)
            {
                return ServiceResult<ObjectiveActivityUserDTO>.Fail(
                    ErrorMessages.ObjectiveActivityUser.AlreadyAssigned,
                    ErrorType.Validation,
                    ErrorCodes.ObjectiveActivityUser.AlreadyAssigned);
            }

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
            {
                return ServiceResult<ObjectiveActivityUserDTO>.Fail(
                    ErrorMessages.Common.InvalidId,
                    ErrorType.Validation,
                    ErrorCodes.Common.InvalidId);
            }

            var entity = await _uow.ObjectiveActivityUsers.GetByIdAsync(
                Key(request.Id), ct);

            if (entity is null)
            {
                return ServiceResult<ObjectiveActivityUserDTO>.Fail(
                    ErrorMessages.ObjectiveActivityUser.AssignmentNotFound,
                    ErrorType.NotFound,
                    ErrorCodes.ObjectiveActivityUser.AssignmentNotFound);
            }

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
            {
                return ServiceResult<bool>.Fail(
                    ErrorMessages.Common.InvalidId,
                    ErrorType.Validation,
                    ErrorCodes.Common.InvalidId);
            }

            var entity = await _uow.ObjectiveActivityUsers.GetByIdAsync(
                Key(id), ct);

            if (entity is null)
            {
                return ServiceResult<bool>.Fail(
                    ErrorMessages.ObjectiveActivityUser.AssignmentNotFound,
                    ErrorType.NotFound,
                    ErrorCodes.ObjectiveActivityUser.AssignmentNotFound);
            }

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