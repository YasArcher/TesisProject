using tesisproject.backend.Repositories.Interfaces;
using tesisproject.backend.Services.Interfaces;
using tesisproject.backend.UnitOfWork.Interfaces;
using tesisproject.shared.DTOs.VisitIssues.Request;
using tesisproject.shared.DTOs.VisitIssues.Response;
using tesisproject.shared.Entities.Core;
using tesisproject.shared.Errors;
using tesisproject.shared.Responses;

namespace tesisproject.backend.Services.Implementations
{
    public sealed class VisitIssueService : IVisitIssueService
    {
        private readonly IVisitIssueRepository _issueRepo;
        private readonly IVisitRepository _visitRepo;
        private readonly IUnitOfWork _uow;
        private readonly ICurrentUserService _currentUser;

        public VisitIssueService(
            IVisitIssueRepository issueRepo,
            IVisitRepository visitRepo,
            IUnitOfWork uow,
            ICurrentUserService currentUser)
        {
            _issueRepo = issueRepo;
            _visitRepo = visitRepo;
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
