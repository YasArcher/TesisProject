using tesisproject.backend.Services.Interfaces;
using tesisproject.backend.Services.Unified.Interfaces;
using tesisproject.backend.UnitOfWork.Unified.Interfaces;
using tesisproject.shared.DTOs.ExternalResearcherProject.Request;
using tesisproject.shared.DTOs.ExternalResearcherProject.Response;
using tesisproject.backend.Data.UnifiedEntities.Core;
using tesisproject.shared.Errors;
using tesisproject.shared.Responses;

namespace tesisproject.backend.Services.Unified.Implementations
{
    public class UnifiedExternalResearcherProjectService : IUnifiedExternalResearcherProjectService
    {
        private readonly IUnifiedUnitOfWork _uow;
        private readonly ICurrentUserService _currentUser;

        public UnifiedExternalResearcherProjectService(
            IUnifiedUnitOfWork uow,
            ICurrentUserService currentUser)
        {
            _uow = uow;
            _currentUser = currentUser;
        }

        // ================= READS =================

        public async Task<ServiceResult<IReadOnlyList<ExternalResearcherProjectListItemDTO>>> ListByProjectAsync(
            int projectId,
            CancellationToken ct = default)
        {
            if (projectId <= 0)
            {
                return ServiceResult<IReadOnlyList<ExternalResearcherProjectListItemDTO>>.Fail(
                    ErrorMessages.ExternalResearcherProject.InvalidProjectId,
                    ErrorType.Validation,
                    ErrorCodes.ExternalResearcherProject.InvalidProjectId);
            }

            var items = await _uow.ExternalResearcherProjects
                .ListByProjectAsync(projectId, ct);

            var dto = items.Select(ToListItemDTO).ToList();

            return ServiceResult<IReadOnlyList<ExternalResearcherProjectListItemDTO>>.Ok(dto);
        }

        public async Task<ServiceResult<ExternalResearcherProjectDetailDTO>> GetByIdAsync(
            int id,
            CancellationToken ct = default)
        {
            if (id <= 0)
            {
                return ServiceResult<ExternalResearcherProjectDetailDTO>.Fail(
                    ErrorMessages.ExternalResearcherProject.InvalidId,
                    ErrorType.Validation,
                    ErrorCodes.ExternalResearcherProject.InvalidId);
            }

            var entity = await _uow.ExternalResearcherProjects.GetByIdWithRefsAsync(id, ct);
            if (entity is null)
            {
                return ServiceResult<ExternalResearcherProjectDetailDTO>.Fail(
                    ErrorMessages.ExternalResearcherProject.NotFound,
                    ErrorType.NotFound,
                    ErrorCodes.ExternalResearcherProject.NotFound);
            }

            var dto = ToDetailDTO(entity);
            return ServiceResult<ExternalResearcherProjectDetailDTO>.Ok(dto);
        }

        // ================ WRITES ================

        public async Task<ServiceResult<ExternalResearcherProjectDetailDTO>> CreateAsync(
            ExternalResearcherProjectCreateRequestDTO request,
            CancellationToken ct = default)
        {
            try
            {
                if (request is null)
                {
                    return ServiceResult<ExternalResearcherProjectDetailDTO>.Fail(
                        ErrorMessages.Common.InvalidRequest,
                        ErrorType.Validation,
                        ErrorCodes.Common.InvalidRequest);
                }

                if (request.ExternalResearcherId <= 0)
                {
                    return ServiceResult<ExternalResearcherProjectDetailDTO>.Fail(
                        ErrorMessages.ExternalResearcherProject.ExternalResearcherIdRequired,
                        ErrorType.Validation,
                        ErrorCodes.ExternalResearcherProject.ExternalResearcherIdRequired);
                }

                if (request.ProjectId <= 0)
                {
                    return ServiceResult<ExternalResearcherProjectDetailDTO>.Fail(
                        ErrorMessages.ExternalResearcherProject.ProjectIdRequired,
                        ErrorType.Validation,
                        ErrorCodes.ExternalResearcherProject.ProjectIdRequired);
                }

                var role = (request.Role ?? string.Empty).Trim();
                if (string.IsNullOrWhiteSpace(role))
                {
                    return ServiceResult<ExternalResearcherProjectDetailDTO>.Fail(
                        ErrorMessages.ExternalResearcherProject.RoleRequired,
                        ErrorType.Validation,
                        ErrorCodes.ExternalResearcherProject.RoleRequired);
                }

                var actorUserId = await GetExistingActorUserIdAsync(ct);
                if (!actorUserId.HasValue)
                {
                    return ServiceResult<ExternalResearcherProjectDetailDTO>.Fail(
                        ErrorMessages.Auth.ActorUserNotFound,
                        ErrorType.NotFound,
                        ErrorCodes.Auth.ActorUserNotFound);
                }

                var extRes = await _uow.ExternalResearchers.GetByIdAsync(
                    new object[] { request.ExternalResearcherId }, ct);

                if (extRes is null)
                {
                    return ServiceResult<ExternalResearcherProjectDetailDTO>.Fail(
                        ErrorMessages.ExternalResearcherProject.ExternalResearcherNotFound,
                        ErrorType.NotFound,
                        ErrorCodes.ExternalResearcherProject.ExternalResearcherNotFound);
                }

                var project = await _uow.Projects.GetByIdAsync(
                    new object[] { request.ProjectId }, ct);

                if (project is null)
                {
                    return ServiceResult<ExternalResearcherProjectDetailDTO>.Fail(
                        ErrorMessages.Project.NotFound,
                        ErrorType.NotFound,
                        ErrorCodes.Project.NotFound);
                }

                var exists = await _uow.ExternalResearcherProjects.ExistsAsync(
                    x => x.ExternalResearcherId == request.ExternalResearcherId
                      && x.ProjectId == request.ProjectId,
                    ct);

                if (exists)
                {
                    return ServiceResult<ExternalResearcherProjectDetailDTO>.Fail(
                        ErrorMessages.ExternalResearcherProject.AlreadyAssigned,
                        ErrorType.Conflict,
                        ErrorCodes.ExternalResearcherProject.AlreadyAssigned);
                }

                var entity = new ExternalResearcherProject
                {
                    ExternalResearcherId = request.ExternalResearcherId,
                    ProjectId = request.ProjectId,
                    Role = role,
                    CreatedByUserId = actorUserId.Value,
                    ExitDate = request.ExitDate
                };

                await _uow.ExternalResearcherProjects.AddAsync(entity, ct);
                await _uow.SaveChangesAsync(ct);

                entity.ExternalResearcher = extRes;

                var dto = ToDetailDTO(entity);
                return ServiceResult<ExternalResearcherProjectDetailDTO>.Ok(dto);
            }
            catch (UnauthorizedAccessException)
            {
                return ServiceResult<ExternalResearcherProjectDetailDTO>.Fail(
                    ErrorMessages.Auth.UserNotAuthenticated,
                    ErrorType.Unauthorized,
                    ErrorCodes.Auth.UserNotAuthenticated);
            }
        }

        public async Task<ServiceResult<ExternalResearcherProjectDetailDTO>> UpdateAsync(
            ExternalResearcherProjectUpdateRequestDTO request,
            CancellationToken ct = default)
        {
            if (request is null)
            {
                return ServiceResult<ExternalResearcherProjectDetailDTO>.Fail(
                    ErrorMessages.Common.InvalidRequest,
                    ErrorType.Validation,
                    ErrorCodes.Common.InvalidRequest);
            }

            if (request.Id <= 0)
            {
                return ServiceResult<ExternalResearcherProjectDetailDTO>.Fail(
                    ErrorMessages.ExternalResearcherProject.InvalidId,
                    ErrorType.Validation,
                    ErrorCodes.ExternalResearcherProject.InvalidId);
            }

            if (request.ExternalResearcherId <= 0)
            {
                return ServiceResult<ExternalResearcherProjectDetailDTO>.Fail(
                    ErrorMessages.ExternalResearcherProject.ExternalResearcherIdRequired,
                    ErrorType.Validation,
                    ErrorCodes.ExternalResearcherProject.ExternalResearcherIdRequired);
            }

            if (request.ProjectId <= 0)
            {
                return ServiceResult<ExternalResearcherProjectDetailDTO>.Fail(
                    ErrorMessages.ExternalResearcherProject.ProjectIdRequired,
                    ErrorType.Validation,
                    ErrorCodes.ExternalResearcherProject.ProjectIdRequired);
            }

            var role = (request.Role ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(role))
            {
                return ServiceResult<ExternalResearcherProjectDetailDTO>.Fail(
                    ErrorMessages.ExternalResearcherProject.RoleRequired,
                    ErrorType.Validation,
                    ErrorCodes.ExternalResearcherProject.RoleRequired);
            }

            var entity = await _uow.ExternalResearcherProjects.GetByIdAsync(
                new object[] { request.Id }, ct);

            if (entity is null)
            {
                return ServiceResult<ExternalResearcherProjectDetailDTO>.Fail(
                    ErrorMessages.ExternalResearcherProject.NotFound,
                    ErrorType.NotFound,
                    ErrorCodes.ExternalResearcherProject.NotFound);
            }

            var extRes = await _uow.ExternalResearchers.GetByIdAsync(
                new object[] { request.ExternalResearcherId }, ct);

            if (extRes is null)
            {
                return ServiceResult<ExternalResearcherProjectDetailDTO>.Fail(
                    ErrorMessages.ExternalResearcherProject.ExternalResearcherNotFound,
                    ErrorType.NotFound,
                    ErrorCodes.ExternalResearcherProject.ExternalResearcherNotFound);
            }

            var project = await _uow.Projects.GetByIdAsync(
                new object[] { request.ProjectId }, ct);

            if (project is null)
            {
                return ServiceResult<ExternalResearcherProjectDetailDTO>.Fail(
                    ErrorMessages.Project.NotFound,
                    ErrorType.NotFound,
                    ErrorCodes.Project.NotFound);
            }

            var exists = await _uow.ExternalResearcherProjects.ExistsAsync(
                x => x.ExternalResearcherId == request.ExternalResearcherId
                  && x.ProjectId == request.ProjectId
                  && x.ExternalResearcherProjectId != request.Id,
                ct);

            if (exists)
            {
                return ServiceResult<ExternalResearcherProjectDetailDTO>.Fail(
                    ErrorMessages.ExternalResearcherProject.AlreadyAssigned,
                    ErrorType.Conflict,
                    ErrorCodes.ExternalResearcherProject.AlreadyAssigned);
            }

            entity.ExternalResearcherId = request.ExternalResearcherId;
            entity.ProjectId = request.ProjectId;
            entity.Role = role;
            entity.ExitDate = request.ExitDate;

            _uow.ExternalResearcherProjects.Update(entity);
            await _uow.SaveChangesAsync(ct);

            entity.ExternalResearcher = extRes;

            var dto = ToDetailDTO(entity);
            return ServiceResult<ExternalResearcherProjectDetailDTO>.Ok(dto);
        }

        // ================ HELPERS ================

        private async Task<int?> GetExistingActorUserIdAsync(CancellationToken ct)
        {
            var currentUserId = _currentUser.GetRequiredUserId();
            var user = await _uow.AppUsers.GetByIdUserAsync(currentUserId, ct);
            return user?.IdUser;
        }

        // ================ MAPPERS ================

        private static ExternalResearcherProjectListItemDTO ToListItemDTO(ExternalResearcherProject x)
        {
            return new ExternalResearcherProjectListItemDTO
            {
                Id = x.ExternalResearcherProjectId,
                ExternalResearcherId = x.ExternalResearcherId,
                ProjectId = x.ProjectId,
                Role = x.Role,
                CreatedAtUtc = x.CreatedAtUtc,
                CreatedByUserId = x.CreatedByUserId,
                ExitDate = x.ExitDate,
                ExternalResearcherFullName = x.ExternalResearcher?.FullName ?? string.Empty,
                ExternalResearcherEmail = x.ExternalResearcher?.Email ?? string.Empty
            };
        }

        private static ExternalResearcherProjectDetailDTO ToDetailDTO(ExternalResearcherProject x)
        {
            return new ExternalResearcherProjectDetailDTO
            {
                Id = x.ExternalResearcherProjectId,
                ExternalResearcherId = x.ExternalResearcherId,
                ProjectId = x.ProjectId,
                Role = x.Role,
                CreatedAtUtc = x.CreatedAtUtc,
                CreatedByUserId = x.CreatedByUserId,
                ExitDate = x.ExitDate,
                ExternalResearcherFullName = x.ExternalResearcher?.FullName ?? string.Empty,
                ExternalResearcherEmail = x.ExternalResearcher?.Email ?? string.Empty
            };
        }
    }
}