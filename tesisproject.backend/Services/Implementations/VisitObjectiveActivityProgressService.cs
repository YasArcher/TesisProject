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
                    return ServiceResult<VisitObjectiveActivityProgressSingleResponseDTO>.Fail("Request is required.", ErrorType.Validation);

                if (request.VisitId <= 0)
                    return ServiceResult<VisitObjectiveActivityProgressSingleResponseDTO>.Fail("VisitId is required.", ErrorType.Validation);

                if (request.ObjectiveActivityId <= 0)
                    return ServiceResult<VisitObjectiveActivityProgressSingleResponseDTO>.Fail("ObjectiveActivityId is required.", ErrorType.Validation);

                if (request.ProgressPercentage < 0 || request.ProgressPercentage > 100)
                    return ServiceResult<VisitObjectiveActivityProgressSingleResponseDTO>.Fail("ProgressPercentage must be between 0 and 100.", ErrorType.Validation);

                // 1) Validar visita
                var visit = await _uow.Visits.GetByIdAsync(new object[] { request.VisitId }, ct);
                if (visit is null)
                    return ServiceResult<VisitObjectiveActivityProgressSingleResponseDTO>.Fail("Visit not found.", ErrorType.NotFound);

                // 2) Validar que la actividad exista Y pertenezca al proyecto de la visita
                // ObjectiveActivity.ObjectiveId -> ProjectObjective.Id -> ProjectObjective.ProjectId == visit.ProjectId
                var activityBelongsToProject = await (
                    from a in _uow.ObjectiveActivities.Query(asNoTracking: true)
                    join o in _uow.ProjectObjectives.Query(asNoTracking: true) on a.ObjectiveId equals o.Id
                    where a.ObjectiveActivityId == request.ObjectiveActivityId
                          && o.ProjectId == visit.ProjectId
                    select a.ObjectiveActivityId
                ).AnyAsync(ct);

                if (!activityBelongsToProject)
                {
                    return ServiceResult<VisitObjectiveActivityProgressSingleResponseDTO>.Fail(
                        "ObjectiveActivityId is invalid for this project/visit.",
                        ErrorType.Validation);
                }
                // 3) Validación adicional:
                // La suma de avances (por visita) de esta actividad dentro del proyecto no debe pasar 100%.
                //
                // sumOther = suma en el proyecto para ObjectiveActivityId, excluyendo la visita actual.
                // (Usamos join para no depender de navegación p.Visit.ProjectId)
                var sumOther = await (
                    from p in _uow.VisitObjectiveActivityProgresses.Query(asNoTracking: true)
                    join v in _uow.Visits.Query(asNoTracking: true) on p.VisitId equals v.VisitId
                    where v.ProjectId == visit.ProjectId
                          && p.ObjectiveActivityId == request.ObjectiveActivityId
                          && p.VisitId != request.VisitId
                    select (int?)p.ProgressPercentage
                ).SumAsync(ct) ?? 0;

                if (sumOther + request.ProgressPercentage > 100)
                {
                    var remaining = 100 - sumOther;
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

                var dto = new VisitObjectiveActivityProgressSingleResponseDTO
                {
                    Id = entity.Id,
                    VisitId = entity.VisitId,
                    ProjectId = visit.ProjectId,
                    ObjectiveActivityId = entity.ObjectiveActivityId,
                    ProgressPercentage = entity.ProgressPercentage,
                    CreatedAt = entity.CreatedAt,
                    ProjectProgressInVisit = projectProgressInVisit,
                    CurrentProjectProgress = currentProjectProgress
                };

                return ServiceResult<VisitObjectiveActivityProgressSingleResponseDTO>.Ok(dto, "Activity progress saved");
            }
            catch (DbUpdateException dbex)
            {
                return ServiceResult<VisitObjectiveActivityProgressSingleResponseDTO>.Fail(
                    dbex.InnerException?.Message ?? dbex.Message,
                    ErrorType.Conflict);
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
                    return ServiceResult<NoContent>.Fail("id is required.", ErrorType.Validation);

                var entity = await _uow.VisitObjectiveActivityProgresses.GetByIdAsync(new object[] { id }, ct);
                if (entity is null)
                    return ServiceResult<NoContent>.Fail("Progress record not found.", ErrorType.NotFound);

                _uow.VisitObjectiveActivityProgresses.Remove(entity);
                await _uow.SaveChangesAsync(ct);

                return ServiceResult<NoContent>.Ok(new NoContent(), "Activity progress deleted");
            }
            catch (DbUpdateException dbex)
            {
                return ServiceResult<NoContent>.Fail(
                    dbex.InnerException?.Message ?? dbex.Message,
                    ErrorType.Conflict);
            }
            catch (Exception ex)
            {
                return ServiceResult<NoContent>.Fail(ex.Message, ErrorType.Unexpected);
            }
        }

    }
}