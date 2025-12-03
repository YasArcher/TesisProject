using tesisproject.backend.Services.Interfaces;
using tesisproject.backend.UnitOfWork.Interfaces;
using tesisproject.shared.DTOs.Document.Response;
using tesisproject.shared.DTOs.ExternalResearcherProject.Request;
using tesisproject.shared.DTOs.ExternalResearcherProject.Response;
using tesisproject.shared.Entities.Core;
using tesisproject.shared.Responses;

namespace tesisproject.backend.Services.Implementations
{
    public class ExternalResearcherProjectService : IExternalResearcherProjectService
    {
        private readonly IUnitOfWork _uow;

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
                    .Fail("Invalid project id.", ErrorType.Validation);

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
                return ServiceResult<ExternalResearcherProjectDetailDTO>.Fail("Invalid id.", ErrorType.Validation);

            var entity = await _uow.ExternalResearcherProjects.GetByIdWithRefsAsync(id, ct);
            if (entity is null)
                return ServiceResult<ExternalResearcherProjectDetailDTO>.Fail("Record not found.", ErrorType.NotFound);

            var dto = ToDetailDTO(entity);
            return ServiceResult<ExternalResearcherProjectDetailDTO>.Ok(dto);
        }

        // ================ WRITES ================

        public async Task<ServiceResult<ExternalResearcherProjectDetailDTO>> CreateAsync(
            ExternalResearcherProjectCreateRequestDTO request,
            int currentUserId,
            CancellationToken ct = default)
        {

            var user = await _uow.AppUsers.GetByIdUserAsync(currentUserId, ct);
            if (user is null)
            {
                return ServiceResult<ExternalResearcherProjectDetailDTO>.Fail(
                    "User not found.",
                    ErrorType.NotFound
                );
            }
            if (request is null)
                return ServiceResult<ExternalResearcherProjectDetailDTO>.Fail("Invalid request.", ErrorType.Validation);

            if (request.ExternalResearcherId <= 0)
                return ServiceResult<ExternalResearcherProjectDetailDTO>.Fail("ExternalResearcherId is required.", ErrorType.Validation);

            if (request.ProjectId <= 0)
                return ServiceResult<ExternalResearcherProjectDetailDTO>.Fail("ProjectId is required.", ErrorType.Validation);

            var role = (request.Role ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(role))
                return ServiceResult<ExternalResearcherProjectDetailDTO>.Fail("Role is required.", ErrorType.Validation);

            // Validar existencia de ExternalResearcher y Project
            var extRes = await _uow.ExternalResearchers.GetByIdAsync(
                new object[] { request.ExternalResearcherId }, ct);
            if (extRes is null)
                return ServiceResult<ExternalResearcherProjectDetailDTO>.Fail("External researcher not found.", ErrorType.Validation);

            var project = await _uow.Projects.GetByIdAsync(
                new object[] { request.ProjectId }, ct);
            if (project is null)
                return ServiceResult<ExternalResearcherProjectDetailDTO>.Fail("Project not found.", ErrorType.Validation);

            var entity = new ExternalResearcherProject
            {
                ExternalResearcherId = request.ExternalResearcherId,
                ProjectId = request.ProjectId,
                Role = role,
                CreatedByUserId = user.IdUser,
                ExitDate = request.ExitDate
                // CreatedAtUtc se setea por default en la entidad
            };

            await _uow.ExternalResearcherProjects.AddAsync(entity, ct);
            await _uow.SaveChangesAsync(ct);

            // Recargar con refs si quieres datos completos
            var reloaded = await _uow.ExternalResearcherProjects.GetByIdWithRefsAsync(
                entity.ExternalResearcherProjectId, ct);

            var dto = ToDetailDTO(reloaded ?? entity);
            return ServiceResult<ExternalResearcherProjectDetailDTO>.Ok(dto);
        }

        public async Task<ServiceResult<ExternalResearcherProjectDetailDTO>> UpdateAsync(
            ExternalResearcherProjectUpdateRequestDTO request,
            CancellationToken ct = default)
        {
            if (request is null || request.Id <= 0)
                return ServiceResult<ExternalResearcherProjectDetailDTO>.Fail("Invalid id.", ErrorType.Validation);

            if (request.ExternalResearcherId <= 0)
                return ServiceResult<ExternalResearcherProjectDetailDTO>.Fail("ExternalResearcherId is required.", ErrorType.Validation);

            if (request.ProjectId <= 0)
                return ServiceResult<ExternalResearcherProjectDetailDTO>.Fail("ProjectId is required.", ErrorType.Validation);

            var role = (request.Role ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(role))
                return ServiceResult<ExternalResearcherProjectDetailDTO>.Fail("Role is required.", ErrorType.Validation);

            var entity = await _uow.ExternalResearcherProjects.GetByIdAsync(
                new object[] { request.Id }, ct);
            if (entity is null)
                return ServiceResult<ExternalResearcherProjectDetailDTO>.Fail("Record not found.", ErrorType.NotFound);

            // Validar existencia de claves FK
            var extRes = await _uow.ExternalResearchers.GetByIdAsync(
                new object[] { request.ExternalResearcherId }, ct);
            if (extRes is null)
                return ServiceResult<ExternalResearcherProjectDetailDTO>.Fail("External researcher not found.", ErrorType.Validation);

            var project = await _uow.Projects.GetByIdAsync(
                new object[] { request.ProjectId }, ct);
            if (project is null)
                return ServiceResult<ExternalResearcherProjectDetailDTO>.Fail("Project not found.", ErrorType.Validation);

            entity.ExternalResearcherId = request.ExternalResearcherId;
            entity.ProjectId = request.ProjectId;
            entity.Role = role;
            entity.ExitDate = request.ExitDate;

            _uow.ExternalResearcherProjects.Update(entity);
            await _uow.SaveChangesAsync(ct);

            var reloaded = await _uow.ExternalResearcherProjects.GetByIdWithRefsAsync(
                entity.ExternalResearcherProjectId, ct);

            var dto = ToDetailDTO(reloaded ?? entity);
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