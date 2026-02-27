using tesisproject.backend.Services.Interfaces;
using tesisproject.backend.UnitOfWork.Interfaces;
using tesisproject.shared.DTOs.ExternalResearcherProject.Request;
using tesisproject.shared.DTOs.ExternalResearcherProject.Response;
using tesisproject.shared.Entities.Core;
using tesisproject.shared.Responses;

namespace tesisproject.backend.Services.Implementations
{
    public class ExternalResearcherProjectService : IExternalResearcherProjectService
    {
        private readonly IUnitOfWork _uow;

        // ===== Messages (mismo texto exacto) =====
        private const string MsgInvalidProjectId = "Invalid project id.";
        private const string MsgInvalidId = "Invalid id.";
        private const string MsgRecordNotFound = "Record not found.";
        private const string MsgUserNotFound = "User not found.";
        private const string MsgInvalidRequest = "Invalid request.";
        private const string MsgExternalResearcherIdRequired = "ExternalResearcherId is required.";
        private const string MsgProjectIdRequired = "ProjectId is required.";
        private const string MsgRoleRequired = "Role is required.";
        private const string MsgExternalResearcherNotFound = "External researcher not found.";
        private const string MsgProjectNotFound = "Project not found.";
        private const string MsgApiError = "API error.";
        private const string MsgExternalResearcherAlreadyAssigned ="External researcher is already assigned to this project.";

        public ExternalResearcherProjectService(IUnitOfWork uow)
        {
            _uow = uow;
        }

        // ================= READS =================

        public async Task<ServiceResult<IReadOnlyList<ExternalResearcherProjectListItemDTO>>> ListByProjectAsync(
            int projectId,
            CancellationToken ct = default)
        {
            if (projectId <= 0)
                return ServiceResult<IReadOnlyList<ExternalResearcherProjectListItemDTO>>
                    .Fail(MsgInvalidProjectId, ErrorType.Validation);

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
                return ServiceResult<ExternalResearcherProjectDetailDTO>.Fail(MsgInvalidId, ErrorType.Validation);

            var entity = await _uow.ExternalResearcherProjects.GetByIdWithRefsAsync(id, ct);
            if (entity is null)
                return ServiceResult<ExternalResearcherProjectDetailDTO>.Fail(MsgRecordNotFound, ErrorType.NotFound);

            var dto = ToDetailDTO(entity);
            return ServiceResult<ExternalResearcherProjectDetailDTO>.Ok(dto);
        }

        // ================ WRITES ================

        public async Task<ServiceResult<ExternalResearcherProjectDetailDTO>> CreateAsync(
            ExternalResearcherProjectCreateRequestDTO request,
            int currentUserId,
            CancellationToken ct = default)
        {
            // EFECTIVO: validar request/IDs/Role antes de ir a BD por el usuario
            if (request is null)
                return ServiceResult<ExternalResearcherProjectDetailDTO>.Fail(MsgInvalidRequest, ErrorType.Validation);

            if (request.ExternalResearcherId <= 0)
                return ServiceResult<ExternalResearcherProjectDetailDTO>.Fail(MsgExternalResearcherIdRequired, ErrorType.Validation);

            if (request.ProjectId <= 0)
                return ServiceResult<ExternalResearcherProjectDetailDTO>.Fail(MsgProjectIdRequired, ErrorType.Validation);

            var role = (request.Role ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(role))
                return ServiceResult<ExternalResearcherProjectDetailDTO>.Fail(MsgRoleRequired, ErrorType.Validation);

            var user = await _uow.AppUsers.GetByIdUserAsync(currentUserId, ct);
            if (user is null)
            {
                return ServiceResult<ExternalResearcherProjectDetailDTO>.Fail(
                    MsgUserNotFound,
                    ErrorType.NotFound
                );
            }

            // EFECTIVO: ahora es NotFound (antes Validation)
            var extRes = await _uow.ExternalResearchers.GetByIdAsync(
                new object[] { request.ExternalResearcherId }, ct);
            if (extRes is null)
                return ServiceResult<ExternalResearcherProjectDetailDTO>.Fail(MsgExternalResearcherNotFound, ErrorType.NotFound);

            var project = await _uow.Projects.GetByIdAsync(
                new object[] { request.ProjectId }, ct);
            if (project is null)
                return ServiceResult<ExternalResearcherProjectDetailDTO>.Fail(MsgProjectNotFound, ErrorType.NotFound);

            // Antes de crear entity (luego de validar IDs y antes de AddAsync)
            var exists = await _uow.ExternalResearcherProjects.ExistsAsync(
                x => x.ExternalResearcherId == request.ExternalResearcherId
                  && x.ProjectId == request.ProjectId,
                ct);

            if (exists)
                return ServiceResult<ExternalResearcherProjectDetailDTO>.Fail(
                    MsgExternalResearcherAlreadyAssigned,
                    ErrorType.Conflict);

            var entity = new ExternalResearcherProject
            {
                ExternalResearcherId = request.ExternalResearcherId,
                ProjectId = request.ProjectId,
                Role = role,
                CreatedByUserId = user.IdUser,
                ExitDate = request.ExitDate
            };

            await _uow.ExternalResearcherProjects.AddAsync(entity, ct);
            await _uow.SaveChangesAsync(ct);

            // EFECTIVO: eliminamos reload; seteamos navegación para mantener FullName/Email en el DTO
            entity.ExternalResearcher = extRes;

            var dto = ToDetailDTO(entity);
            return ServiceResult<ExternalResearcherProjectDetailDTO>.Ok(dto);
        }

        public async Task<ServiceResult<ExternalResearcherProjectDetailDTO>> UpdateAsync(
            ExternalResearcherProjectUpdateRequestDTO request,
            CancellationToken ct = default)
        {
            if (request is null || request.Id <= 0)
                return ServiceResult<ExternalResearcherProjectDetailDTO>.Fail(MsgInvalidId, ErrorType.Validation);

            if (request.ExternalResearcherId <= 0)
                return ServiceResult<ExternalResearcherProjectDetailDTO>.Fail(MsgExternalResearcherIdRequired, ErrorType.Validation);

            if (request.ProjectId <= 0)
                return ServiceResult<ExternalResearcherProjectDetailDTO>.Fail(MsgProjectIdRequired, ErrorType.Validation);

            var role = (request.Role ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(role))
                return ServiceResult<ExternalResearcherProjectDetailDTO>.Fail(MsgRoleRequired, ErrorType.Validation);

            var entity = await _uow.ExternalResearcherProjects.GetByIdAsync(
                new object[] { request.Id }, ct);
            if (entity is null)
                return ServiceResult<ExternalResearcherProjectDetailDTO>.Fail(MsgRecordNotFound, ErrorType.NotFound);

            // EFECTIVO: ahora es NotFound (antes Validation)
            var extRes = await _uow.ExternalResearchers.GetByIdAsync(
                new object[] { request.ExternalResearcherId }, ct);
            if (extRes is null)
                return ServiceResult<ExternalResearcherProjectDetailDTO>.Fail(MsgExternalResearcherNotFound, ErrorType.NotFound);

            // EFECTIVO: ahora es NotFound (antes Validation)
            var project = await _uow.Projects.GetByIdAsync(
                new object[] { request.ProjectId }, ct);
            if (project is null)
                return ServiceResult<ExternalResearcherProjectDetailDTO>.Fail(MsgProjectNotFound, ErrorType.NotFound);

            var exists = await _uow.ExternalResearcherProjects.ExistsAsync(
                x => x.ExternalResearcherId == request.ExternalResearcherId
                  && x.ProjectId == request.ProjectId
                  && x.ExternalResearcherProjectId != request.Id,
                ct);

            if (exists)
                return ServiceResult<ExternalResearcherProjectDetailDTO>.Fail(
                    MsgExternalResearcherAlreadyAssigned,
                    ErrorType.Conflict);

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