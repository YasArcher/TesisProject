using Microsoft.EntityFrameworkCore;
using tesisproject.backend.Services.Interfaces;
using tesisproject.backend.UnitOfWork.Interfaces;
using tesisproject.shared.DTOs.VisitObjectiveActivityProgress.Request;
using tesisproject.shared.DTOs.VisitObjectiveActivityProgress.Response;
using tesisproject.shared.Entities.Core;
using tesisproject.shared.Responses;

namespace tesisproject.backend.Services.Implementations
{
    public class VisitObjectiveActivityProgressService : IVisitObjectiveActivityProgressService
    {
        private const int MaxProgressPercentage = 100;

        private const string MsgRequestRequired = "Request is required.";
        private const string MsgVisitIdRequired = "VisitId is required.";
        private const string MsgObjectiveActivityIdRequired = "ObjectiveActivityId is required.";
        private const string MsgProgressPercentageRange = "ProgressPercentage must be between 0 and 100.";
        private const string MsgVisitNotFound = "Visit not found.";
        private const string MsgObjectiveActivityInvalidForProjectVisit = "ObjectiveActivityId is invalid for this project/visit.";
        private const string MsgActivityProgressSaved = "Activity progress saved";

        private const string MsgIdRequired = "id is required.";
        private const string MsgProgressRecordNotFound = "Progress record not found.";
        private const string MsgActivityProgressDeleted = "Activity progress deleted";

        private readonly IUnitOfWork _uow;

        public VisitObjectiveActivityProgressService(IUnitOfWork uow)
        {
            _uow = uow;
        }

        public async Task<ServiceResult<VisitObjectiveActivityProgressSingleResponseDTO>> UpsertSingleAsync(
            UpsertSingleVisitObjectiveActivityProgressRequestDTO request,
            CancellationToken ct = default)
        {
            try
            {
                if (request is null)
                    return ServiceResult<VisitObjectiveActivityProgressSingleResponseDTO>.Fail(MsgRequestRequired, ErrorType.Validation);

                if (request.VisitId <= 0)
                    return ServiceResult<VisitObjectiveActivityProgressSingleResponseDTO>.Fail(MsgVisitIdRequired, ErrorType.Validation);

                if (request.ObjectiveActivityId <= 0)
                    return ServiceResult<VisitObjectiveActivityProgressSingleResponseDTO>.Fail(MsgObjectiveActivityIdRequired, ErrorType.Validation);

                if (IsProgressOutOfRange(request.ProgressPercentage))
                    return ServiceResult<VisitObjectiveActivityProgressSingleResponseDTO>.Fail(MsgProgressPercentageRange, ErrorType.Validation);

                // 1) Validar visita
                var visit = await _uow.Visits.GetByIdAsync(new object[] { request.VisitId }, ct);
                if (visit is null)
                    return ServiceResult<VisitObjectiveActivityProgressSingleResponseDTO>.Fail(MsgVisitNotFound, ErrorType.NotFound);

                // 2) Validar que la actividad exista Y pertenezca al proyecto de la visita
                // ObjectiveActivity.ObjectiveId -> ProjectObjective.Id -> ProjectObjective.ProjectId == visit.ProjectId
                var activityBelongsToProject = await ActivityBelongsToProjectAsync(
                    request.ObjectiveActivityId,
                    visit.ProjectId,
                    ct);

                if (!activityBelongsToProject)
                {
                    return ServiceResult<VisitObjectiveActivityProgressSingleResponseDTO>.Fail(
                        MsgObjectiveActivityInvalidForProjectVisit,
                        ErrorType.Validation);
                }

                // 3) Validación adicional:
                // La suma de avances (por visita) de esta actividad dentro del proyecto no debe pasar 100%.
                //
                // sumOther = suma en el proyecto para ObjectiveActivityId, excluyendo la visita actual.
                // (Usamos join para no depender de navegación p.Visit.ProjectId)
                var sumOther = await SumOtherProgressAsync(
                    visit.ProjectId,
                    request.ObjectiveActivityId,
                    request.VisitId,
                    ct);

                if (sumOther + request.ProgressPercentage > MaxProgressPercentage)
                {
                    var remaining = MaxProgressPercentage - sumOther;
                    return ServiceResult<VisitObjectiveActivityProgressSingleResponseDTO>.Fail(
                        $"Progress exceeds 100%. Remaining allowed for this activity in this project is {remaining}%.",
                        ErrorType.Validation);
                }

                // 3) UPSERT por (VisitId + ObjectiveActivityId)
                var existing = await _uow.VisitObjectiveActivityProgresses
                    .Query(asNoTracking: false) // tracking para Update
                    .FirstOrDefaultAsync(x =>
                        x.VisitId == request.VisitId &&
                        x.ObjectiveActivityId == request.ObjectiveActivityId, ct);

                VisitObjectiveActivityProgress entity;

                if (existing is null)
                {
                    entity = new VisitObjectiveActivityProgress
                    {
                        VisitId = request.VisitId,
                        ObjectiveActivityId = request.ObjectiveActivityId,
                        ProgressPercentage = request.ProgressPercentage,
                        CreatedAt = DateTime.UtcNow
                    };

                    await _uow.VisitObjectiveActivityProgresses.AddAsync(entity, ct);
                }
                else
                {
                    existing.ProgressPercentage = request.ProgressPercentage;

                    // Si "CreatedAt" representa el timestamp del snapshot, esto es correcto.
                    // Si prefieres que CreatedAt sea "cuando se creó por primera vez", elimina esta línea.
                    existing.CreatedAt = DateTime.UtcNow;

                    _uow.VisitObjectiveActivityProgresses.Update(existing);
                    entity = existing;
                }

                await _uow.SaveChangesAsync(ct);

                // 4) Agregados (para refrescar UI sin 2 llamadas extra)
                var projectProgressInVisit =
                    await _uow.VisitObjectiveActivityProgresses.GetProjectProgressByVisitAsync(visit.ProjectId, request.VisitId, ct);

                var currentProjectProgress =
                    await _uow.VisitObjectiveActivityProgresses.GetCurrentProjectProgressAsync(visit.ProjectId, ct);

                var dto = BuildSingleResponseDto(
                    entity,
                    visit.ProjectId,
                    projectProgressInVisit,
                    currentProjectProgress);

                return ServiceResult<VisitObjectiveActivityProgressSingleResponseDTO>.Ok(dto, MsgActivityProgressSaved);
            }
            catch (DbUpdateException dbex)
            {
                return FailConflict<VisitObjectiveActivityProgressSingleResponseDTO>(dbex);
            }
            catch (Exception ex)
            {
                return ServiceResult<VisitObjectiveActivityProgressSingleResponseDTO>.Fail(ex.Message, ErrorType.Unexpected);
            }
        }

        public async Task<ServiceResult<NoContent>> DeleteAsync(int id, CancellationToken ct = default)
        {
            try
            {
                if (id <= 0)
                    return ServiceResult<NoContent>.Fail(MsgIdRequired, ErrorType.Validation);

                var entity = await _uow.VisitObjectiveActivityProgresses.GetByIdAsync(new object[] { id }, ct);
                if (entity is null)
                    return ServiceResult<NoContent>.Fail(MsgProgressRecordNotFound, ErrorType.NotFound);

                _uow.VisitObjectiveActivityProgresses.Remove(entity);
                await _uow.SaveChangesAsync(ct);

                return ServiceResult<NoContent>.Ok(new NoContent(), MsgActivityProgressDeleted);
            }
            catch (DbUpdateException dbex)
            {
                return FailConflict<NoContent>(dbex);
            }
            catch (Exception ex)
            {
                return ServiceResult<NoContent>.Fail(ex.Message, ErrorType.Unexpected);
            }
        }

        private static bool IsProgressOutOfRange(int progressPercentage)
            => progressPercentage < 0 || progressPercentage > MaxProgressPercentage;

        private Task<bool> ActivityBelongsToProjectAsync(
            int objectiveActivityId,
            int projectId,
            CancellationToken ct)
        {
            return (
                from a in _uow.ObjectiveActivities.Query(asNoTracking: true)
                join o in _uow.ProjectObjectives.Query(asNoTracking: true) on a.ObjectiveId equals o.Id
                where a.ObjectiveActivityId == objectiveActivityId
                      && o.ProjectId == projectId
                select a.ObjectiveActivityId
            ).AnyAsync(ct);
        }

        private async Task<int> SumOtherProgressAsync(
            int projectId,
            int objectiveActivityId,
            int visitId,
            CancellationToken ct)
        {
            var sumOther = await (
                from p in _uow.VisitObjectiveActivityProgresses.Query(asNoTracking: true)
                join v in _uow.Visits.Query(asNoTracking: true) on p.VisitId equals v.VisitId
                where v.ProjectId == projectId
                      && p.ObjectiveActivityId == objectiveActivityId
                      && p.VisitId != visitId
                select (int?)p.ProgressPercentage
            ).SumAsync(ct) ?? 0;

            return sumOther;
        }

        private static VisitObjectiveActivityProgressSingleResponseDTO BuildSingleResponseDto(
            VisitObjectiveActivityProgress entity,
            int projectId,
            decimal? projectProgressInVisit,
            decimal? currentProjectProgress)
        {
            return new VisitObjectiveActivityProgressSingleResponseDTO
            {
                Id = entity.Id,
                VisitId = entity.VisitId,
                ProjectId = projectId,
                ObjectiveActivityId = entity.ObjectiveActivityId,
                ProgressPercentage = entity.ProgressPercentage,
                CreatedAt = entity.CreatedAt,
                ProjectProgressInVisit = projectProgressInVisit,
                CurrentProjectProgress = currentProjectProgress
            };
        }

        private static ServiceResult<T> FailConflict<T>(DbUpdateException dbex)
        {
            return ServiceResult<T>.Fail(
                dbex.InnerException?.Message ?? dbex.Message,
                ErrorType.Conflict);
        }
    }
}