using tesisproject.backend.Repositories.Unified.Interfaces;
using tesisproject.backend.Services.Interfaces;
using tesisproject.backend.Services.Unified.Interfaces;
using tesisproject.backend.UnitOfWork.Unified.Interfaces;
using tesisproject.shared.DTOs.VisitIssues.Request;
using tesisproject.shared.DTOs.VisitIssues.Response;
using tesisproject.backend.Data.UnifiedEntities.Core;
using tesisproject.shared.Errors;
using tesisproject.shared.Responses;

namespace tesisproject.backend.Services.Unified.Implementations
{
    public sealed class UnifiedVisitIssueService : IUnifiedVisitIssueService
    {
        private readonly IUnifiedVisitIssueRepository _issueRepo;
        private readonly IUnifiedVisitRepository _visitRepo;
        private readonly IUnifiedUnitOfWork _uow;
        private readonly ICurrentUserService _currentUser;

        public UnifiedVisitIssueService(
            IUnifiedUnitOfWork uow,
            ICurrentUserService currentUser)
        {
            _issueRepo = uow.VisitIssues;
            _visitRepo = uow.Visits;
            _uow = uow;
            _currentUser = currentUser;
        }

        // =========================
        //        CREATE
        // =========================
        public async Task<ServiceResult<VisitIssueResponseDTO>> CreateAsync(
            VisitIssueCreateRequestDTO request,
            CancellationToken ct = default)
        {
            try
            {
                var visitExists = await VisitExistsAsync(request.VisitId, ct);
                if (!visitExists)
                    return FailVisitNotFound<VisitIssueResponseDTO>(request.VisitId);

                var reporterUserId = await GetExistingActorUserIdAsync(ct);
                if (!reporterUserId.HasValue)
                    return FailUserNotFound<VisitIssueResponseDTO>();

                var entity = new VisitIssue
                {
                    VisitId = request.VisitId,
                    Description = request.Description,
                    CreatedAtUtc = DateTime.UtcNow,
                    ReportedByUserId = reporterUserId.Value
                };

                await _issueRepo.AddAsync(entity, ct);
                await _uow.SaveChangesAsync(ct);

                return ServiceResult<VisitIssueResponseDTO>.Ok(ToResponse(entity));
            }
            catch (UnauthorizedAccessException)
            {
                return ServiceResult<VisitIssueResponseDTO>.Fail(
                    ErrorMessages.Auth.UserNotAuthenticated,
                    ErrorType.Unauthorized,
                    ErrorCodes.Auth.UserNotAuthenticated);
            }
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
                return FailVisitIssueNotFound<VisitIssueResponseDTO>(id);

            return ServiceResult<VisitIssueResponseDTO>.Ok(ToResponse(entity));
        }

        // =========================
        //     LIST BY VISIT
        // =========================
        public async Task<ServiceResult<IReadOnlyList<VisitIssueResponseDTO>>> ListByVisitAsync(
            int visitId,
            CancellationToken ct = default)
        {
            var visitExists = await VisitExistsAsync(visitId, ct);
            if (!visitExists)
                return FailVisitNotFound<IReadOnlyList<VisitIssueResponseDTO>>(visitId);

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
            CancellationToken ct = default)
        {
            try
            {
                var entity = await _issueRepo.FirstOrDefaultAsync(x => x.VisitIssueId == id, ct);
                if (entity is null)
                    return FailVisitIssueNotFound<VisitIssueResponseDTO>(id);

                var reporterUserId = await GetExistingActorUserIdAsync(ct);
                if (!reporterUserId.HasValue)
                    return FailUserNotFound<VisitIssueResponseDTO>();

                if (request.Description is not null)
                    entity.Description = request.Description;

                entity.ReportedByUserId = reporterUserId.Value;
                entity.UpdatedAtUtc = DateTime.UtcNow;

                _issueRepo.Update(entity);
                await _uow.SaveChangesAsync(ct);

                return ServiceResult<VisitIssueResponseDTO>.Ok(ToResponse(entity));
            }
            catch (UnauthorizedAccessException)
            {
                return ServiceResult<VisitIssueResponseDTO>.Fail(
                    ErrorMessages.Auth.UserNotAuthenticated,
                    ErrorType.Unauthorized,
                    ErrorCodes.Auth.UserNotAuthenticated);
            }
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
                return FailVisitIssueNotFound<NoContent>(id);

            _issueRepo.Remove(entity);
            await _uow.SaveChangesAsync(ct);

            return ServiceResult<NoContent>.Ok(new NoContent());
        }

        private Task<bool> VisitExistsAsync(int visitId, CancellationToken ct)
            => _visitRepo.ExistsAsync(v => v.VisitId == visitId, ct);

        private async Task<int?> GetExistingActorUserIdAsync(CancellationToken ct)
        {
            var currentUserId = _currentUser.GetRequiredUserId();
            var user = await _uow.AppUsers.GetByIdUserAsync(currentUserId, ct);
            return user?.IdUser;
        }

        private static ServiceResult<T> FailVisitNotFound<T>(int visitId)
            => ServiceResult<T>.Fail(
                string.Format(ErrorMessages.Visit.NotFoundById, visitId),
                ErrorType.NotFound,
                ErrorCodes.Visit.NotFound);

        private static ServiceResult<T> FailVisitIssueNotFound<T>(int id)
            => ServiceResult<T>.Fail(
                string.Format(ErrorMessages.VisitIssue.NotFoundById, id),
                ErrorType.NotFound,
                ErrorCodes.VisitIssue.NotFound);

        private static ServiceResult<T> FailUserNotFound<T>()
            => ServiceResult<T>.Fail(
                ErrorMessages.VisitIssue.ReporterUserNotFound,
                ErrorType.NotFound,
                ErrorCodes.VisitIssue.ReporterUserNotFound);

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
