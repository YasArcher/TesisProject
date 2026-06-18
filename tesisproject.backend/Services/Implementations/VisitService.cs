using Microsoft.EntityFrameworkCore;
using tesisproject.backend.Services.Interfaces;
using tesisproject.backend.UnitOfWork.Interfaces;
using tesisproject.shared.DTOs.Visit.Request;
using tesisproject.shared.DTOs.Visit.Response;
using tesisproject.shared.Entities.Core;
using tesisproject.shared.Responses;
using tesisproject.shared.Errors;
using tesisproject.shared.Enums; // VisitStateIds

namespace tesisproject.backend.Services.Implementations
{
    public class VisitService : IVisitService
    {
        private const int PlanningMinMonths = 1;

        private const string MsgVisitCreated = "Visit created";
        private const string MsgVisitUpdated = "Visit updated";
        private const string MsgVisitDeleted = "Visit deleted";
        private const string MsgVisitRetrieved = "Visit retrieved";
        private const string MsgVisitsRetrieved = "Visits retrieved";
        private const string MsgProjectVisitsRetrieved = "Project visits retrieved";
        private const string MsgVisitDetailRetrieved = "Visit detail retrieved";
        private const string MsgVisitFinalized = "Visit finalized";

        private const string MsgNoPlannedVisitsFound = "No planned visits found.";
        private const string MsgPlannedVisitsRetrieved = "Planned visits retrieved";

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
                {
                    return ServiceResult<VisitListResponseDTO>.Fail(
                        ErrorMessages.Visit.ProjectIdRequired,
                        ErrorType.Validation,
                        ErrorCodes.Visit.ProjectIdRequired);
                }

                var entity = new Visit
                {
                    ProjectId = request.ProjectId,
                    VisitStateId = VisitStateIds.Planned,
                    ScheduledDate = request.ScheduledDate,
                };

                await _uow.Visits.AddAsync(entity, ct);
                await _uow.SaveChangesAsync(ct);

                return await LoadWithRefsOrFailAsync(
                    entity.VisitId,
                    ErrorMessages.Visit.LoadAfterCreationFailed,
                    ErrorCodes.Visit.LoadAfterCreationFailed,
                    MsgVisitCreated,
                    ct);
            }
            catch (DbUpdateException dbex)
            {
                return FailConflict<VisitListResponseDTO>(dbex);
            }
            catch (Exception ex)
            {
                return FailUnexpected<VisitListResponseDTO>(ex);
            }
        }

        // =============== READ ONE ===============

        public async Task<ServiceResult<VisitListResponseDTO>> GetByIdAsync(int visitId, CancellationToken ct = default)
        {
            try
            {
                var visit = await _uow.Visits.GetByIdWithRefsAsync(visitId, ct);
                if (visit is null)
                {
                    return ServiceResult<VisitListResponseDTO>.Fail(
                        ErrorMessages.Visit.NotFound,
                        ErrorType.NotFound,
                        ErrorCodes.Visit.NotFound);
                }

                return ServiceResult<VisitListResponseDTO>.Ok(MapToListDTO(visit), MsgVisitRetrieved);
            }
            catch (Exception ex)
            {
                return FailUnexpected<VisitListResponseDTO>(ex);
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
                {
                    return ServiceResult<IReadOnlyList<VisitListResponseDTO>>.Fail(
                        ErrorMessages.Visit.NoneFound,
                        ErrorType.NotFound,
                        ErrorCodes.Visit.NoneFound);
                }

                return ServiceResult<IReadOnlyList<VisitListResponseDTO>>.Ok(items, MsgVisitsRetrieved);
            }
            catch (Exception ex)
            {
                return FailUnexpected<IReadOnlyList<VisitListResponseDTO>>(ex);
            }
        }

        public async Task<ServiceResult<IReadOnlyList<VisitListResponseDTO>>> ListByProjectAsync(int projectId, CancellationToken ct = default)
        {
            try
            {
                if (projectId <= 0)
                {
                    return ServiceResult<IReadOnlyList<VisitListResponseDTO>>.Fail(
                        ErrorMessages.Visit.ProjectIdRequired,
                        ErrorType.Validation,
                        ErrorCodes.Visit.ProjectIdRequired);
                }

                var items = await _uow.Visits.GetByProjectAsync(projectId, ct);
                if (items.Count == 0)
                {
                    return ServiceResult<IReadOnlyList<VisitListResponseDTO>>.Fail(
                        ErrorMessages.Visit.NoneFoundForProject,
                        ErrorType.NotFound,
                        ErrorCodes.Visit.NoneFoundForProject);
                }

                var dtos = items.Select(MapToListDTO).ToList();
                return ServiceResult<IReadOnlyList<VisitListResponseDTO>>.Ok(dtos, MsgProjectVisitsRetrieved);
            }
            catch (Exception ex)
            {
                return FailUnexpected<IReadOnlyList<VisitListResponseDTO>>(ex);
            }
        }

        // =============== UPDATE ===============

        public async Task<ServiceResult<VisitListResponseDTO>> UpdateAsync(UpdateVisitRequestDTO request, CancellationToken ct = default)
        {
            try
            {
                var entity = await _uow.Visits.GetByIdAsync(new object[] { request.VisitId }, ct);
                if (entity is null)
                {
                    return ServiceResult<VisitListResponseDTO>.Fail(
                        ErrorMessages.Visit.NotFound,
                        ErrorType.NotFound,
                        ErrorCodes.Visit.NotFound);
                }

                if (request.ProjectId <= 0)
                {
                    return ServiceResult<VisitListResponseDTO>.Fail(
                        ErrorMessages.Visit.ProjectIdRequired,
                        ErrorType.Validation,
                        ErrorCodes.Visit.ProjectIdRequired);
                }

                if (request.VisitStateId <= 0)
                {
                    return ServiceResult<VisitListResponseDTO>.Fail(
                        ErrorMessages.Visit.VisitStateIdRequired,
                        ErrorType.Validation,
                        ErrorCodes.Visit.VisitStateIdRequired);
                }

                entity.ProjectId = request.ProjectId;
                entity.VisitStateId = request.VisitStateId;
                entity.DocumentId = request.DocumentId;
                entity.ScheduledDate = request.VisitDate;

                _uow.Visits.Update(entity);
                await _uow.SaveChangesAsync(ct);

                return await LoadWithRefsOrFailAsync(
                    entity.VisitId,
                    ErrorMessages.Visit.LoadAfterUpdateFailed,
                    ErrorCodes.Visit.LoadAfterUpdateFailed,
                    MsgVisitUpdated,
                    ct);
            }
            catch (DbUpdateException dbex)
            {
                return FailConflict<VisitListResponseDTO>(dbex);
            }
            catch (Exception ex)
            {
                return FailUnexpected<VisitListResponseDTO>(ex);
            }
        }

        // =============== DELETE ===============

        public async Task<ServiceResult<NoContent>> DeleteAsync(int visitId, CancellationToken ct = default)
        {
            try
            {
                var entity = await _uow.Visits.GetByIdAsync(new object[] { visitId }, ct);
                if (entity is null)
                {
                    return ServiceResult<NoContent>.Fail(
                        ErrorMessages.Visit.NotFound,
                        ErrorType.NotFound,
                        ErrorCodes.Visit.NotFound);
                }

                _uow.Visits.Remove(entity);
                await _uow.SaveChangesAsync(ct);

                return ServiceResult<NoContent>.Ok(new NoContent(), MsgVisitDeleted);
            }
            catch (DbUpdateException dbex)
            {
                return FailConflict<NoContent>(dbex);
            }
            catch (Exception ex)
            {
                return FailUnexpected<NoContent>(ex);
            }
        }

        // =============== DETAIL ===============

        public async Task<ServiceResult<VisitDetailResponseDTO>> GetVisitDetailAsync(int visitId, CancellationToken ct = default)
        {
            try
            {
                var visit = await _uow.Visits.GetByIdWithRefsAsync(visitId, ct);
                if (visit is null)
                {
                    return ServiceResult<VisitDetailResponseDTO>.Fail(
                        ErrorMessages.Visit.NotFound,
                        ErrorType.NotFound,
                        ErrorCodes.Visit.NotFound);
                }

                return ServiceResult<VisitDetailResponseDTO>.Ok(MapToDetailDTO(visit), MsgVisitDetailRetrieved);
            }
            catch (Exception ex)
            {
                return FailUnexpected<VisitDetailResponseDTO>(ex);
            }
        }

        public async Task<ServiceResult<VisitListResponseDTO>> FinalizeAsync(FinalizeVisitRequestDTO request, CancellationToken ct = default)
        {
            try
            {
                if (request is null || request.VisitId <= 0)
                {
                    return ServiceResult<VisitListResponseDTO>.Fail(
                        ErrorMessages.Visit.VisitIdRequired,
                        ErrorType.Validation,
                        ErrorCodes.Visit.VisitIdRequired);
                }

                if (request.FinalVisitStateId <= 0)
                {
                    return ServiceResult<VisitListResponseDTO>.Fail(
                        ErrorMessages.Visit.FinalVisitStateIdRequired,
                        ErrorType.Validation,
                        ErrorCodes.Visit.FinalVisitStateIdRequired);
                }

                var entity = await _uow.Visits.GetByIdAsync(new object[] { request.VisitId }, ct);
                if (entity is null)
                {
                    return ServiceResult<VisitListResponseDTO>.Fail(
                        ErrorMessages.Visit.NotFound,
                        ErrorType.NotFound,
                        ErrorCodes.Visit.NotFound);
                }

                var stateExists = await _uow.VisitStates.ExistsAsync(x => x.Id == request.FinalVisitStateId, ct);
                if (!stateExists)
                {
                    return ServiceResult<VisitListResponseDTO>.Fail(
                        ErrorMessages.Visit.VisitStateIdInvalid,
                        ErrorType.Validation,
                        ErrorCodes.Visit.VisitStateIdInvalid);
                }

                entity.VisitStateId = request.FinalVisitStateId;
                entity.PerformedDate ??= DateTime.UtcNow;

                _uow.Visits.Update(entity);
                await _uow.SaveChangesAsync(ct);

                return await LoadWithRefsOrFailAsync(
                    entity.VisitId,
                    ErrorMessages.Visit.LoadAfterFinalizeFailed,
                    ErrorCodes.Visit.LoadAfterFinalizeFailed,
                    MsgVisitFinalized,
                    ct);
            }
            catch (DbUpdateException dbex)
            {
                return FailConflict<VisitListResponseDTO>(dbex);
            }
            catch (Exception ex)
            {
                return FailUnexpected<VisitListResponseDTO>(ex);
            }
        }

        // =============== PLANNING LIST (CANDIDATES TO CREATE) ===============

        public async Task<ServiceResult<IReadOnlyList<VisitPlannedForExecutionListDTO>>> ListPlannedForExecutionAsync(
            DateOnly? executionDate = null,
            CancellationToken ct = default)
        {
            try
            {
                var today = (executionDate?.ToDateTime(TimeOnly.MinValue) ?? DateTime.UtcNow.Date).Date;
                var cutoff = today.AddMonths(-PlanningMinMonths);

                var rows = await _uow.Projects
                    .Query(asNoTracking: true)
                    .Where(p => p.StartDate != null)
                    .Where(p => p.ProjectStateId == ProjectStateIds.EnEjecucion || p.ProjectStateId == ProjectStateIds.Planificado)
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
                        HasOpenVisit = p.Visits.Any(v => VisitStateIds.OpenStates.Contains(v.VisitStateId))
                    })
                    .Where(x => !x.HasOpenVisit)
                    .Where(x => (x.LastRealizedDate ?? x.StartDate) <= cutoff)
                    .OrderBy(x => (x.LastRealizedDate ?? x.StartDate))
                    .ToListAsync(ct);

                if (rows.Count == 0)
                {
                    return ServiceResult<IReadOnlyList<VisitPlannedForExecutionListDTO>>
                        .Ok(Array.Empty<VisitPlannedForExecutionListDTO>(), MsgNoPlannedVisitsFound);
                }

                var list = rows.Select(x =>
                {
                    var baseDate = (x.LastRealizedDate ?? x.StartDate)!.Value.Date;
                    var dueDate = baseDate.AddMonths(PlanningMinMonths);

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
                    .Ok(list, MsgPlannedVisitsRetrieved);
            }
            catch (Exception ex)
            {
                return FailUnexpected<IReadOnlyList<VisitPlannedForExecutionListDTO>>(ex);
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
                {
                    return ServiceResult<IReadOnlyList<VisitPlannedForExecutionListDTO>>.Fail(
                        ErrorMessages.Visit.VisitStateIdRequired,
                        ErrorType.Validation,
                        ErrorCodes.Visit.VisitStateIdRequired);
                }

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
                {
                    return ServiceResult<IReadOnlyList<VisitPlannedForExecutionListDTO>>.Fail(
                        ErrorMessages.Visit.NoneFoundForState,
                        ErrorType.NotFound,
                        ErrorCodes.Visit.NoneFoundForState);
                }

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
                    .Ok(list, MsgVisitsRetrieved);
            }
            catch (Exception ex)
            {
                return FailUnexpected<IReadOnlyList<VisitPlannedForExecutionListDTO>>(ex);
            }
        }

        // =============== BULK SCHEDULE ===============

        public async Task<ServiceResult<NoContent>> BulkScheduleAsync(BulkScheduleVisitsRequestDTO request, CancellationToken ct = default)
        {
            if (request is null)
            {
                return ServiceResult<NoContent>.Fail(
                    ErrorMessages.Visit.BulkInvalidRequest,
                    ErrorType.Validation,
                    ErrorCodes.Visit.BulkInvalidRequest);
            }

            var projectIds = request.ProjectIds?
                .Where(x => x > 0)
                .Distinct()
                .ToList() ?? new List<int>();

            if (projectIds.Count == 0)
            {
                return ServiceResult<NoContent>.Fail(
                    ErrorMessages.Visit.BulkNoValidProjectIds,
                    ErrorType.Validation,
                    ErrorCodes.Visit.BulkNoValidProjectIds);
            }

            if (request.ScheduledDate == default)
            {
                return ServiceResult<NoContent>.Fail(
                    ErrorMessages.Visit.BulkScheduledDateRequired,
                    ErrorType.Validation,
                    ErrorCodes.Visit.BulkScheduledDateRequired);
            }

            var scheduled = request.ScheduledDate;

            var repoResult = await _uow.Visits.BulkScheduleAsync(projectIds, scheduled, visitStateId: VisitStateIds.Pending, ct);

            if (!repoResult.Success)
            {
                return ServiceResult<NoContent>.Fail(
                    repoResult.Error ?? ErrorMessages.Visit.BulkScheduleFailed,
                    ErrorType.Unexpected,
                    ErrorCodes.Visit.BulkScheduleFailed);
            }

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

        private async Task<ServiceResult<VisitListResponseDTO>> LoadWithRefsOrFailAsync(
            int visitId,
            string failMessage,
            string failCode,
            string okMessage,
            CancellationToken ct)
        {
            var withRefs = await _uow.Visits.GetByIdWithRefsAsync(visitId, ct);
            if (withRefs is null)
            {
                return ServiceResult<VisitListResponseDTO>.Fail(
                    failMessage,
                    ErrorType.Unexpected,
                    failCode);
            }

            return ServiceResult<VisitListResponseDTO>.Ok(MapToListDTO(withRefs), okMessage);
        }

        private static ServiceResult<T> FailConflict<T>(DbUpdateException dbex)
        {
            return ServiceResult<T>.Fail(
                dbex.InnerException?.Message ?? dbex.Message,
                ErrorType.Conflict,
                ErrorCodes.Common.PersistenceConflict);
        }

        private static ServiceResult<T> FailUnexpected<T>(Exception ex)
        {
            return ServiceResult<T>.Fail(
                ex.Message,
                ErrorType.Unexpected,
                ErrorCodes.Common.UnexpectedError);
        }
    }
}