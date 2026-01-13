using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using tesisproject.backend.Services.Interfaces;
using tesisproject.backend.UnitOfWork.Interfaces;
using tesisproject.shared.DTOs.Visit.Request;
using tesisproject.shared.DTOs.Visit.Response;
using tesisproject.shared.Entities.Core;
using tesisproject.shared.Responses;

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
                // Validaciones mínimas (alineado a tu patrón)
                if (request.ProjectId <= 0)
                    return ServiceResult<VisitListResponseDTO>.Fail("ProjectId is required.", ErrorType.Validation);

                var entity = new Visit
                {
                    ProjectId = request.ProjectId,
                    VisitStateId = 1,
                    ScheduledDate = request.ScheduledDate,
                };

                await _uow.Visits.AddAsync(entity, ct);
                await _uow.SaveChangesAsync(ct);

                // Traemos con refs para mapear textos
                var withRefs = await _uow.Visits.GetByIdWithRefsAsync(entity.VisitId, ct);
                if (withRefs is null)
                    return ServiceResult<VisitListResponseDTO>.Fail("Visit could not be loaded after creation.", ErrorType.Unexpected);

                var dto = MapToListDTO(withRefs);
                return ServiceResult<VisitListResponseDTO>.Ok(dto, "Visit created");
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
                // Usamos QueryWithRefs para preparar proyección directa
                var q = _uow.Visits.QueryWithRefs();
                var items = await q
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

                var dto = MapToDetailDTO(visit);
                return ServiceResult<VisitDetailResponseDTO>.Ok(dto, "Visit detail retrieved");
            }
            catch (Exception ex)
            {
                return ServiceResult<VisitDetailResponseDTO>.Fail(ex.Message, ErrorType.Unexpected);
            }
        }

        public async Task<ServiceResult<VisitListResponseDTO>> FinalizeAsync(
    FinalizeVisitRequestDTO request,
    CancellationToken ct = default)
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

                // (Opcional recomendado) validar que el estado exista
                var stateExists = await _uow.VisitStates.ExistsAsync(x => x.Id == request.FinalVisitStateId, ct);
                if (!stateExists)
                    return ServiceResult<VisitListResponseDTO>.Fail("VisitStateId is invalid.", ErrorType.Validation);

                // setear estado final + fecha realizada
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

        public async Task<ServiceResult<IReadOnlyList<VisitPlannedForExecutionListDTO>>> ListPlannedForExecutionAsync(
    bool isFirstVisit,
    CancellationToken ct = default)
        {
            try
            {
                var cutoff = CutoffByMonthsBack(DateTime.UtcNow, minMonths: 4);

                var baseQuery = _uow.Visits
                    .QueryWithRefs()
                    .AsNoTracking()
                    .Where(ProjectHasMinAge(cutoff))
                    .Where(v => v.VisitStateId != 2);

                IQueryable<int> selectedVisitIds;

                if (isFirstVisit)
                {
                    baseQuery = baseQuery.Where(EligibleForFirstVisitSelector());
                    selectedVisitIds = FirstVisitIdsPerProject(baseQuery);
                }
                else
                {
                    selectedVisitIds = NextVisitIdsAfterLastRealizedPerProject(baseQuery);
                }

                // 1) Traer la lista base (sin VisitNumber)
                var list = await _uow.Visits
                    .QueryWithRefs()
                    .AsNoTracking()
                    .Where(v => selectedVisitIds.Contains(v.VisitId))
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

                        VisitNumber = 0 // se llena abajo
                    })
                    .OrderBy(v => v.VisitDate ?? DateTime.MaxValue)
                    .ThenBy(v => v.VisitId)
                    .ToListAsync(ct);

                // 2) Llenar VisitNumber
                if (isFirstVisit)
                {
                    foreach (var item in list)
                        item.VisitNumber = 1;
                }
                else
                {
                    const int realizedStateId = 3; // <-- AJUSTA según tu seed final

                    var projectIds = list.Select(x => x.ProjectId).Distinct().ToList();

                    var realizedCounts = await _uow.Visits
                        .Query() // OJO: aquí ya NO estás en expression tree del Select, así que no hay CS0854
                        .AsNoTracking()
                        .Where(x => projectIds.Contains(x.ProjectId) && x.VisitStateId == realizedStateId)
                        .GroupBy(x => x.ProjectId)
                        .Select(g => new { ProjectId = g.Key, Count = g.Count() })
                        .ToDictionaryAsync(x => x.ProjectId, x => x.Count, ct);

                    foreach (var item in list)
                    {
                        var realized = realizedCounts.TryGetValue(item.ProjectId, out var c) ? c : 0;
                        item.VisitNumber = realized + 1;
                    }
                }

                return ServiceResult<IReadOnlyList<VisitPlannedForExecutionListDTO>>.Ok(list, "Planned visits retrieved");
            }
            catch (Exception ex)
            {
                return ServiceResult<IReadOnlyList<VisitPlannedForExecutionListDTO>>.Fail(ex.Message, ErrorType.Unexpected);
            }
        }
        public async Task<ServiceResult<IReadOnlyList<VisitPlannedForExecutionListDTO>>> ListByStateAsync(
    int visitStateId,
    CancellationToken ct = default)
        {
            try
            {
                if (visitStateId <= 0)
                    return ServiceResult<IReadOnlyList<VisitPlannedForExecutionListDTO>>
                        .Fail("visitStateId is required.", ErrorType.Validation);

                const int realizedStateId = 3; // AJUSTA según tu seed (VISITA REALIZADA)

                // 1) Traer lista (mismo DTO)
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

                // 2) Llenar VisitNumber (misma lógica que ya aplicabas)
                var projectIds = list.Select(x => x.ProjectId).Distinct().ToList();

                var realizedCounts = await _uow.Visits
                    .Query()
                    .AsNoTracking()
                    .Where(x => projectIds.Contains(x.ProjectId) && x.VisitStateId == realizedStateId)
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



        public async Task<ServiceResult<NoContent>> BulkScheduleAsync(BulkScheduleVisitsRequestDTO request, CancellationToken ct = default)
        {
            if (request is null)
                return ServiceResult<NoContent>.Fail("Request inválido.");

            var ids = request.VisitIds?
                .Where(x => x > 0)
                .Distinct()
                .ToList() ?? new List<int>();

            if (ids.Count == 0)
                return ServiceResult<NoContent>.Fail("Debes enviar al menos un ID de visita válido.");

            if (request.ScheduledDate == default)
                return ServiceResult<NoContent>.Fail("ScheduledDate es requerido.");

            // Si tú trabajas con fechas sin hora, opcional:
            // var scheduled = request.ScheduledDate.Date;
            var scheduled = request.ScheduledDate;

            // Repo: validar existencia y actualizar
            var repoResult = await _uow.Visits.BulkScheduleAsync(ids, scheduled, visitStateId: 2, ct);

            if (!repoResult.Success)
                return ServiceResult<NoContent>.Fail(repoResult.Error ?? "No se pudo actualizar las visitas.");

            await _uow.SaveChangesAsync(ct);

            return ServiceResult<NoContent>.Ok( new NoContent());
        }

        // =============== HELPER METHODS ===============

        public static DateTime CutoffByMonthsBack(DateTime today, int minMonths)
            => today.Date.AddMonths(-minMonths);

        // Proyectos con StartDate y que ya cumplieron mínimo X meses
        public static Expression<Func<Visit, bool>> ProjectHasMinAge(DateTime cutoff)
            => v => v.Project.StartDate != null && v.Project.StartDate.Value <= cutoff;

        // Filtra por estado del proyecto (para “primera visita”, usar Planificado)
        public static Expression<Func<Visit, bool>> ProjectInState(int projectStateId)
            => v => v.Project.ProjectStateId == projectStateId;

        // Devuelve 1 visita por proyecto: la “primera” (por VisitDate y desempate por VisitId)
        public static IQueryable<int> FirstVisitIdsPerProject(IQueryable<Visit> q)
            => q.GroupBy(v => v.ProjectId)
                .Select(g => g
                    .OrderBy(v => v.ScheduledDate ?? DateTime.MaxValue)
                    .ThenBy(v => v.VisitId)
                    .Select(v => v.VisitId)
                    .First());

        public static Expression<Func<Visit, bool>> EligibleForFirstVisitSelector()
            => v =>
                v.Project.ProjectStateId == 6 // PLANIFICADO
                || (v.Project.ProjectStateId == 3 // EN EJECUCION
                    && !v.Project.Visits.Any(x => x.VisitStateId == 3)); // VISITA REALIZADA

        // Devuelve 1 visita por proyecto (EN EJECUCIÓN), la siguiente a la última REALIZADA.
        // Selección por VisitId para evitar problemas cuando ScheduledDate es null.
        public static IQueryable<int> NextVisitIdsAfterLastRealizedPerProject(IQueryable<Visit> q)
        {
            // Última visita realizada por proyecto
            var lastRealized = q
                .Where(v => v.Project.ProjectStateId == 3 && v.VisitStateId == 3) // Proyecto en ejecución + visita realizada
                .GroupBy(v => v.ProjectId)
                .Select(g => new
                {
                    ProjectId = g.Key,
                    LastRealizedVisitId = g.Max(x => x.VisitId)
                });

            // Candidatas: visitas NO realizadas que vengan después de la última realizada
            var nextIds = q
                .Where(v => v.Project.ProjectStateId == 3 && v.VisitStateId != 3) // aún no realizada
                .Join(
                    lastRealized,
                    v => v.ProjectId,
                    lr => lr.ProjectId,
                    (v, lr) => new { v, lr.LastRealizedVisitId }
                )
                .Where(x => x.v.VisitId > x.LastRealizedVisitId)
                .GroupBy(x => x.v.ProjectId)
                .Select(g => g
                    .OrderBy(x => x.v.VisitId)
                    .Select(x => x.v.VisitId)
                    .First()
                );

            return nextIds;
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