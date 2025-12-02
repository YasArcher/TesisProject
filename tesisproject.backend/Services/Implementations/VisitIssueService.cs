using tesisproject.backend.Repositories.Interfaces;
using tesisproject.backend.Services.Interfaces;
using tesisproject.backend.UnitOfWork.Interfaces;
using tesisproject.shared.DTOs.Budgets.Request;
using tesisproject.shared.DTOs.VisitIssues.Request;
using tesisproject.shared.DTOs.VisitIssues.Response;
using tesisproject.shared.Entities.Core;
using tesisproject.shared.Enums;
using tesisproject.shared.Responses;

namespace tesisproject.backend.Services.Implementations
{
    public sealed class VisitIssueService : IVisitIssueService
    {
        private readonly IVisitIssueRepository _issueRepo;
        private readonly IVisitRepository _visitRepo;
        private readonly IUnitOfWork _uow;

        public VisitIssueService(
            IVisitIssueRepository issueRepo,
            IVisitRepository visitRepo,
            IUnitOfWork uow)
        {
            _issueRepo = issueRepo;
            _visitRepo = visitRepo;
            _uow = uow;
        }

        // =========================
        //        CREATE
        // =========================
        public async Task<ServiceResult<VisitIssueResponseDTO>> CreateAsync(
            VisitIssueCreateRequestDTO request,
            int currentUserId,
            CancellationToken ct = default)
        {
            // Validar existencia de Visit
            var visitExists = await _visitRepo.ExistsAsync(v => v.VisitId == request.VisitId, ct);
            if (!visitExists)
                return ServiceResult<VisitIssueResponseDTO>.Fail(
                    $"Visit {request.VisitId} was not found.",
                    ErrorType.NotFound);
            var user = await _uow.AppUsers.GetByIdUserAsync(currentUserId, ct);
            if (user is null)
            {
                return ServiceResult<VisitIssueResponseDTO>.Fail(
                    "User not found.",
                    ErrorType.NotFound
                );
            }
            var entity = new VisitIssue
            {
                VisitId = request.VisitId,
                Description = request.Description,
                CreatedAtUtc = DateTime.UtcNow,
                ReportedByUserId = user.IdUser
            };

            await _issueRepo.AddAsync(entity, ct);
            await _uow.SaveChangesAsync(ct);

            return ServiceResult<VisitIssueResponseDTO>.Ok(ToResponse(entity));
        }

        // =========================
        //        READ (ID)
        // =========================
        public async Task<ServiceResult<VisitIssueResponseDTO>> GetByIdAsync(
            int id,
            CancellationToken ct = default)
        {
            var entity = await _issueRepo.GetByIdWithRefsAsync(id, ct);
            if (entity is null)
                return ServiceResult<VisitIssueResponseDTO>.Fail(
                    $"VisitIssue {id} was not found.",
                    ErrorType.NotFound);

            return ServiceResult<VisitIssueResponseDTO>.Ok(ToResponse(entity));
        }

        // =========================
        //     LIST BY VISIT
        // =========================
        public async Task<ServiceResult<IReadOnlyList<VisitIssueResponseDTO>>> ListByVisitAsync(
            int visitId,
            CancellationToken ct = default)
        {
            var visitExists = await _visitRepo.ExistsAsync(v => v.VisitId == visitId, ct);
            if (!visitExists)
                return ServiceResult<IReadOnlyList<VisitIssueResponseDTO>>.Fail(
                    $"Visit {visitId} was not found.",
                    ErrorType.NotFound);

            var items = await _issueRepo.GetByVisitAsync(visitId, ct);
            var result = items.Select(ToResponse).ToList().AsReadOnly();

            return ServiceResult<IReadOnlyList<VisitIssueResponseDTO>>.Ok(result);
        }

        // =========================
        //        UPDATE
        // =========================
        public async Task<ServiceResult<VisitIssueResponseDTO>> UpdateAsync(
            int id,
            VisitIssueUpdateRequestDTO request,
            int currentUserId,
            CancellationToken ct = default)
        {
            var entity = await _issueRepo.FirstOrDefaultAsync(x => x.VisitIssueId == id, ct);
            if (entity is null)
                return ServiceResult<VisitIssueResponseDTO>.Fail(
                    $"VisitIssue {id} was not found.",
                    ErrorType.NotFound);
            var user = await _uow.AppUsers.GetByIdUserAsync(currentUserId, ct);
            if (user is null)
            {
                return ServiceResult<VisitIssueResponseDTO>.Fail(
                    "User not found.",
                    ErrorType.NotFound
                );
            }
            // Aplicar solo campos no nulos (PATCH-like)
            if (request.Description is not null) entity.Description = request.Description;
            entity.ReportedByUserId = user.IdUser;
            entity.UpdatedAtUtc = DateTime.UtcNow;

            _issueRepo.Update(entity);
            await _uow.SaveChangesAsync(ct);

            return ServiceResult<VisitIssueResponseDTO>.Ok(ToResponse(entity));
        }

        // =========================
        //        DELETE
        // =========================
        public async Task<ServiceResult<NoContent>> DeleteAsync(
            int id,
            CancellationToken ct = default)
        {
            var entity = await _issueRepo.FirstOrDefaultAsync(x => x.VisitIssueId == id, ct);
            if (entity is null)
                return ServiceResult<NoContent>.Fail(
                    $"VisitIssue {id} was not found.",
                    ErrorType.NotFound);

            _issueRepo.Remove(entity);
            await _uow.SaveChangesAsync(ct);

            return ServiceResult<NoContent>.Ok(new NoContent());
        }

        // =========================
        //     MAPEO A RESPONSE
        // =========================
        private static VisitIssueResponseDTO ToResponse(VisitIssue e) => new VisitIssueResponseDTO
        {
            VisitIssueId = e.VisitIssueId,
            VisitId = e.VisitId,
            Description = e.Description,
            ReportedByUserId = e.ReportedByUserId,
            CreatedAtUtc = e.CreatedAtUtc,
            UpdatedAtUtc = e.UpdatedAtUtc
        };
    }
}