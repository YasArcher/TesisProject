using Microsoft.EntityFrameworkCore;
using tesisproject.backend.Services.Interfaces;
using tesisproject.backend.UnitOfWork.Interfaces;
using tesisproject.shared.DTOs.Visit.Request;
using tesisproject.shared.DTOs.Visit.Response;
using tesisproject.shared.Entities.Core;
using tesisproject.shared.Responses;
using tesisproject.shared.Enums; // VisitStateIds

namespace tesisproject.backend.Services.Implementations
{
    public class VisitService : IVisitService
    {
        private readonly IUnitOfWork _uow;

        public VisitService(IUnitOfWork uow)
        {
            _uow = uow;
        }

        // =============== CREATE ===============

        public async Task<ServiceResult<VisitListResponseDTO>> CreateAsync(AddVisitRequestDTO request, CancellationToken ct = default)
        {
            try
            {
                if (request.ProjectId <= 0)
                    return ServiceResult<VisitListResponseDTO>.Fail("ProjectId is required.", ErrorType.Validation);

                var entity = new Visit
                {
                    ProjectId = request.ProjectId,
                    VisitStateId = VisitStateIds.Planned,
                    ScheduledDate = request.ScheduledDate,
                };

                await _uow.Visits.AddAsync(entity, ct);
                await _uow.SaveChangesAsync(ct);

                var withRefs = await _uow.Visits.GetByIdWithRefsAsync(entity.VisitId, ct);
                if (withRefs is null)
                    return ServiceResult<VisitListResponseDTO>.Fail("Visit could not be loaded after creation.", ErrorType.Unexpected);

                return ServiceResult<VisitListResponseDTO>.Ok(MapToListDTO(withRefs), "Visit created");
            }
            catch (DbUpdateException dbex)
            {
                return ServiceResult<VisitListResponseDTO>.Fail(dbex.InnerException?.Message ?? dbex.Message, ErrorType.Conflict);
            }
            catch (Exception ex)
            {
                return ServiceResult<VisitListResponseDTO>.Fail(ex.Message, ErrorType.Unexpected);
            }
        }

        // =============== READ ONE ===============

        public async Task<ServiceResult<VisitListResponseDTO>> GetByIdAsync(int visitId, CancellationToken ct = default)
        {
            try
            {
                var visit = await _uow.Visits.GetByIdWithRefsAsync(visitId, ct);
                if (visit is null)
                    return ServiceResult<VisitListResponseDTO>.Fail("Visit not found.", ErrorType.NotFound);

                return ServiceResult<VisitListResponseDTO>.Ok(MapToListDTO(visit), "Visit retrieved");
            }
            catch (Exception ex)
            {
                return ServiceResult<VisitListResponseDTO>.Fail(ex.Message, ErrorType.Unexpected);
            }
        }

        // =============== LIST ===============

        public async Task<ServiceResult<IReadOnlyList<VisitListResponseDTO>>> ListAsync(CancellationToken ct = default)
        {
            try
            {
                var items = await _uow.Visits.QueryWithRefs()
                    .OrderBy(v => v.ScheduledDate ?? DateTime.MaxValue)
                    .Select(v => new VisitListResponseDTO
                    {
                        VisitId = v.VisitId,
                        ProjectId = v.ProjectId,
                        ProjectName = v.Project.ProjectName,
                        VisitStateId = v.VisitStateId,
                        VisitStateName = v.VisitState.Name,
                        DocumentId = v.DocumentId,
                        VisitDate = v.ScheduledDate,
                    })
                    .ToListAsync(ct);

                if (items.Count == 0)
                    return ServiceResult<IReadOnlyList<VisitListResponseDTO>>.Fail("No visits found.", ErrorType.NotFound);

                return ServiceResult<IReadOnlyList<VisitListResponseDTO>>.Ok(items, "Visits retrieved");
            }
            catch (Exception ex)
            {
                return ServiceResult<IReadOnlyList<VisitListResponseDTO>>.Fail(ex.Message, ErrorType.Unexpected);
            }
        }

        public async Task<ServiceResult<IReadOnlyList<VisitListResponseDTO>>> ListByProjectAsync(int projectId, CancellationToken ct = default)
        {
            try
            {
                if (projectId <= 0)
                    return ServiceResult<IReadOnlyList<VisitListResponseDTO>>.Fail("projectId is required.", ErrorType.Validation);

                var items = await _uow.Visits.GetByProjectAsync(projectId, ct);
                if (items.Count == 0)
                    return ServiceResult<IReadOnlyList<VisitListResponseDTO>>.Fail("No visits found for this project.", ErrorType.NotFound);

                var dtos = items.Select(MapToListDTO).ToList();
                return ServiceResult<IReadOnlyList<VisitListResponseDTO>>.Ok(dtos, "Project visits retrieved");
            }
            catch (Exception ex)
            {
                return ServiceResult<IReadOnlyList<VisitListResponseDTO>>.Fail(ex.Message, ErrorType.Unexpected);
            }
        }

        // =============== UPDATE ===============

        public async Task<ServiceResult<VisitListResponseDTO>> UpdateAsync(UpdateVisitRequestDTO request, CancellationToken ct = default)
        {
            try
            {
                var entity = await _uow.Visits.GetByIdAsync(new object[] { request.VisitId }, ct);
                if (entity is null)
                    return ServiceResult<VisitListResponseDTO>.Fail("Visit not found.", ErrorType.NotFound);

                if (request.ProjectId <= 0)
                    return ServiceResult<VisitListResponseDTO>.Fail("ProjectId is required.", ErrorType.Validation);

                if (request.VisitStateId <= 0)
                    return ServiceResult<VisitListResponseDTO>.Fail("VisitStateId is required.", ErrorType.Validation);

                entity.ProjectId = request.ProjectId;
                entity.VisitStateId = request.VisitStateId;
                entity.DocumentId = request.DocumentId;
                entity.ScheduledDate = request.VisitDate;

                _uow.Visits.Update(entity);
                await _uow.SaveChangesAsync(ct);

                var withRefs = await _uow.Visits.GetByIdWithRefsAsync(entity.VisitId, ct);
                if (withRefs is null)
                    return ServiceResult<VisitListResponseDTO>.Fail("Visit could not be loaded after update.", ErrorType.Unexpected);

                return ServiceResult<VisitListResponseDTO>.Ok(MapToListDTO(withRefs), "Visit updated");
            }
            catch (DbUpdateException dbex)
            {
                return ServiceResult<VisitListResponseDTO>.Fail(dbex.InnerException?.Message ?? dbex.Message, ErrorType.Conflict);
            }
            catch (Exception ex)
            {
                return ServiceResult<VisitListResponseDTO>.Fail(ex.Message, ErrorType.Unexpected);
            }
        }

        // =============== DELETE ===============

        public async Task<ServiceResult<NoContent>> DeleteAsync(int visitId, CancellationToken ct = default)
        {
            try
            {
                var entity = await _uow.Visits.GetByIdAsync(new object[] { visitId }, ct);
                if (entity is null)
                    return ServiceResult<NoContent>.Fail("Visit not found.", ErrorType.NotFound);

                _uow.Visits.Remove(entity);
                await _uow.SaveChangesAsync(ct);

                return ServiceResult<NoContent>.Ok(new NoContent(), "Visit deleted");
            }
            catch (DbUpdateException dbex)
            {
                return ServiceResult<NoContent>.Fail(dbex.InnerException?.Message ?? dbex.Message, ErrorType.Conflict);
            }
            catch (Exception ex)
            {
                return ServiceResult<NoContent>.Fail(ex.Message, ErrorType.Unexpected);
            }
        }

        // =============== DETAIL ===============

        public async Task<ServiceResult<VisitDetailResponseDTO>> GetVisitDetailAsync(int visitId, CancellationToken ct = default)
        {
            try
            {
                var visit = await _uow.Visits.GetByIdWithRefsAsync(visitId, ct);
                if (visit is null)
                    return ServiceResult<VisitDetailResponseDTO>.Fail("Visit not found.", ErrorType.NotFound);

                return ServiceResult<VisitDetailResponseDTO>.Ok(MapToDetailDTO(visit), "Visit detail retrieved");
            }
            catch (Exception ex)
            {
                return ServiceResult<VisitDetailResponseDTO>.Fail(ex.Message, ErrorType.Unexpected);
            }
        }

        public async Task<ServiceResult<VisitListResponseDTO>> FinalizeAsync(FinalizeVisitRequestDTO request, CancellationToken ct = default)
        {
            try
            {
                if (request is null || request.VisitId <= 0)
                    return ServiceResult<VisitListResponseDTO>.Fail("VisitId is required.", ErrorType.Validation);

                if (request.FinalVisitStateId <= 0)
                    return ServiceResult<VisitListResponseDTO>.Fail("FinalVisitStateId is required.", ErrorType.Validation);

                var entity = await _uow.Visits.GetByIdAsync(new object[] { request.VisitId }, ct);
                if (entity is null)
                    return ServiceResult<VisitListResponseDTO>.Fail("Visit not found.", ErrorType.NotFound);

                var stateExists = await _uow.VisitStates.ExistsAsync(x => x.Id == request.FinalVisitStateId, ct);
                if (!stateExists)
                    return ServiceResult<VisitListResponseDTO>.Fail("VisitStateId is invalid.", ErrorType.Validation);

                entity.VisitStateId = request.FinalVisitStateId;
                entity.PerformedDate ??= DateTime.UtcNow;

                _uow.Visits.Update(entity);
                await _uow.SaveChangesAsync(ct);

                var withRefs = await _uow.Visits.GetByIdWithRefsAsync(entity.VisitId, ct);
                if (withRefs is null)
                    return ServiceResult<VisitListResponseDTO>.Fail("Visit could not be loaded after finalize.", ErrorType.Unexpected);

                return ServiceResult<VisitListResponseDTO>.Ok(MapToListDTO(withRefs), "Visit finalized");
            }
            catch (DbUpdateException dbex)
            {
                return ServiceResult<VisitListResponseDTO>.Fail(dbex.InnerException?.Message ?? dbex.Message, ErrorType.Conflict);
            }
            catch (Exception ex)
            {
                return ServiceResult<VisitListResponseDTO>.Fail(ex.Message, ErrorType.Unexpected);
            }
        }

        // =============== PLANNING LIST (CANDIDATES TO CREATE) ===============

        public async Task<ServiceResult<IReadOnlyList<VisitPlannedForExecutionListDTO>>> ListPlannedForExecutionAsync(
    DateOnly? executionDate = null,
    CancellationToken ct = default)
        {
            try
            {
                const int minMonths = 1;

                // Fecha de referencia: si el cliente manda una fecha, se usa; si no, se usa hoy (UTC).
                var today = (executionDate?.ToDateTime(TimeOnly.MinValue) ?? DateTime.UtcNow.Date).Date;

                // Regla: "cumple el periodo configurado" hasta la fecha => baseDate + minMonths <= today
                // Equivalente en query: baseDate <= today - minMonths
                var cutoff = today.AddMonths(-minMonths);

                var rows = await _uow.Projects
                    .Query(asNoTracking: true)
                    .Where(p => p.StartDate != null)
                    .Where(p => p.ProjectStateId == 6 || p.ProjectStateId == 3)
                    .Select(p => new
                    {
                        p.ProjectId,
                        p.ProjectName,
                        p.ProjectCode,
                        p.FacultyId,
                        p.StartDate,

                        LastRealizedDate = p.Visits
                            .Where(v => v.VisitStateId == VisitStateIds.Realized)
                            .Select(v => (DateTime?)(v.PerformedDate ?? v.ScheduledDate ?? v.CreatedAt))
                            .Max(),

                        RealizedCount = p.Visits.Count(v => v.VisitStateId == VisitStateIds.Realized),

                        // Bloquea si existe una visita abierta (Planned/Pending/OnHold)
                        HasOpenVisit = p.Visits.Any(v => VisitStateIds.OpenStates.Contains(v.VisitStateId))
                    })
                    .Where(x => !x.HasOpenVisit)
                    // "hasta la fecha": si baseDate <= cutoff entonces dueDate (=baseDate+1mes) <= today
                    .Where(x => (x.LastRealizedDate ?? x.StartDate) <= cutoff)
                    .OrderBy(x => (x.LastRealizedDate ?? x.StartDate))
                    .ToListAsync(ct);

                if (rows.Count == 0)
                {
                    return ServiceResult<IReadOnlyList<VisitPlannedForExecutionListDTO>>
                        .Ok(Array.Empty<VisitPlannedForExecutionListDTO>(), "No planned visits found.");
                }

                var list = rows.Select(x =>
                {
                    var baseDate = (x.LastRealizedDate ?? x.StartDate)!.Value.Date;
                    var dueDate = baseDate.AddMonths(minMonths);

                    return new VisitPlannedForExecutionListDTO
                    {
                        VisitId = 0,
                        VisitDate = dueDate,

                        VisitStateId = VisitStateIds.Planned,
                        VisitStateName = "Planificable",

                        ProjectId = x.ProjectId,
                        ProjectName = x.ProjectName,
                        ProjectCode = x.ProjectCode,
                        FacultyId = x.FacultyId,

                        VisitNumber = x.RealizedCount + 1
                    };
                }).ToList();

                list = list.Where(x => x.VisitDate.HasValue && x.VisitDate.Value.Date <= today).ToList();

                return ServiceResult<IReadOnlyList<VisitPlannedForExecutionListDTO>>
                    .Ok(list, "Planned visits retrieved");
            }
            catch (Exception ex)
            {
                return ServiceResult<IReadOnlyList<VisitPlannedForExecutionListDTO>>
                    .Fail(ex.Message, ErrorType.Unexpected);
            }
        }

        // =============== LIST BY STATE ===============

        public async Task<ServiceResult<IReadOnlyList<VisitPlannedForExecutionListDTO>>> ListByStateAsync(
            int visitStateId,
            CancellationToken ct = default)
        {
            try
            {
                if (visitStateId <= 0)
                    return ServiceResult<IReadOnlyList<VisitPlannedForExecutionListDTO>>
                        .Fail("visitStateId is required.", ErrorType.Validation);

                var list = await _uow.Visits
                    .QueryWithRefs()
                    .AsNoTracking()
                    .Where(v => v.VisitStateId == visitStateId)
                    .Select(v => new VisitPlannedForExecutionListDTO
                    {
                        VisitId = v.VisitId,
                        VisitDate = v.ScheduledDate,

                        VisitStateId = v.VisitStateId,
                        VisitStateName = v.VisitState != null ? v.VisitState.Name : string.Empty,

                        ProjectId = v.ProjectId,
                        ProjectName = v.Project.ProjectName,
                        ProjectCode = v.Project.ProjectCode,
                        FacultyId = v.Project.FacultyId,

                        VisitNumber = 0
                    })
                    .OrderBy(v => v.VisitDate ?? DateTime.MaxValue)
                    .ThenBy(v => v.VisitId)
                    .ToListAsync(ct);

                if (list.Count == 0)
                    return ServiceResult<IReadOnlyList<VisitPlannedForExecutionListDTO>>
                        .Fail("No visits found for this state.", ErrorType.NotFound);

                var projectIds = list.Select(x => x.ProjectId).Distinct().ToList();

                var realizedCounts = await _uow.Visits
                    .Query()
                    .AsNoTracking()
                    .Where(x => projectIds.Contains(x.ProjectId) && x.VisitStateId == VisitStateIds.Realized)
                    .GroupBy(x => x.ProjectId)
                    .Select(g => new { ProjectId = g.Key, Count = g.Count() })
                    .ToDictionaryAsync(x => x.ProjectId, x => x.Count, ct);

                foreach (var item in list)
                {
                    var realized = realizedCounts.TryGetValue(item.ProjectId, out var c) ? c : 0;
                    item.VisitNumber = realized + 1;
                }

                return ServiceResult<IReadOnlyList<VisitPlannedForExecutionListDTO>>
                    .Ok(list, "Visits retrieved");
            }
            catch (Exception ex)
            {
                return ServiceResult<IReadOnlyList<VisitPlannedForExecutionListDTO>>
                    .Fail(ex.Message, ErrorType.Unexpected);
            }
        }

        // =============== BULK SCHEDULE ===============

        public async Task<ServiceResult<NoContent>> BulkScheduleAsync(BulkScheduleVisitsRequestDTO request, CancellationToken ct = default)
        {
            if (request is null)
                return ServiceResult<NoContent>.Fail("Request inválido.");

            var projectIds = request.ProjectIds?
                .Where(x => x > 0)
                .Distinct()
                .ToList() ?? new List<int>();

            if (projectIds.Count == 0)
                return ServiceResult<NoContent>.Fail("Debes enviar al menos un ProjectId válido.");

            if (request.ScheduledDate == default)
                return ServiceResult<NoContent>.Fail("ScheduledDate es requerido.");

            var scheduled = request.ScheduledDate;

            // Reutilizas el mismo método repo, pero ahora su primer parámetro serán ProjectIds
            var repoResult = await _uow.Visits.BulkScheduleAsync(projectIds, scheduled, visitStateId: VisitStateIds.Pending, ct);

            if (!repoResult.Success)
                return ServiceResult<NoContent>.Fail(repoResult.Error ?? "No se pudo planificar las visitas.");

            await _uow.SaveChangesAsync(ct);

            return ServiceResult<NoContent>.Ok(new NoContent());
        }

        // =============== MAPPING ===============

        private static VisitListResponseDTO MapToListDTO(Visit v) => new()
        {
            VisitId = v.VisitId,
            ProjectId = v.ProjectId,
            ProjectName = v.Project?.ProjectName ?? string.Empty,
            VisitStateId = v.VisitStateId,
            VisitStateName = v.VisitState?.Name ?? string.Empty,
            DocumentId = v.DocumentId,
            VisitDate = v.ScheduledDate,
        };
        private static VisitDetailResponseDTO MapToDetailDTO(Visit v) => new()
        {
            VisitId = v.VisitId,
            FundingDocumentId = v.FundingDocumentId,
            DocumentId = v.DocumentId,
            ProgressDocumentId = v.ProgressDocumentId,
            ScheduledDate = v.ScheduledDate,
            PerformedDate = v.PerformedDate,
            VisitState = v.VisitState?.Name ?? string.Empty,
            VisitStateId = v.VisitStateId
        };
    }
}
