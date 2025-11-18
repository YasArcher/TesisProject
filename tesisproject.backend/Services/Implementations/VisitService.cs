using Microsoft.EntityFrameworkCore;
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

                if (request.VisitStateId <= 0)
                    return ServiceResult<VisitListResponseDTO>.Fail("VisitStateId is required.", ErrorType.Validation);

                var entity = new Visit
                {
                    ProjectId = request.ProjectId,
                    VisitStateId = request.VisitStateId,
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
            VisitState = v.VisitState?.Name ?? string.Empty
        };

    }
}
