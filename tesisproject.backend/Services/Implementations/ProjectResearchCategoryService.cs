using tesisproject.backend.Services.Interfaces;
using tesisproject.backend.UnitOfWork.Interfaces;
using tesisproject.shared.DTOs.ProjectResearchCategory.Request;
using tesisproject.shared.DTOs.ProjectResearchCategory.Response;
using tesisproject.shared.Entities.Core;
using tesisproject.shared.Responses;
using tesisproject.shared.Errors;

namespace tesisproject.backend.Services.Implementations
{
    public class ProjectResearchCategoryService : IProjectResearchCategoryService
    {
        private readonly IUnitOfWork _uow;

        private const string InvalidIdMessage = "Invalid id.";
        private const string NotFoundMessage = "Not found.";
        private const string InvalidDataMessage = "Invalid data.";

        public ProjectResearchCategoryService(IUnitOfWork uow)
        {
            _uow = uow;
        }

        // ================= READS =================

        public async Task<ServiceResult<List<ProjectResearchCategoryListItemDTO>>> ListAsync(
            int projectId,
            CancellationToken ct = default)
        {
            var items = await _uow.ProjectResearchCategories
                .GetAllAsync(x => x.ProjectId == projectId, ct);

            var dto = items
                .Select(MapToListItemDTO)
                .ToList();

            return ServiceResult<List<ProjectResearchCategoryListItemDTO>>.Ok(dto);
        }

        public async Task<ServiceResult<ProjectResearchCategoryDetailDTO>> GetByIdAsync(
            int id,
            CancellationToken ct = default)
        {
            if (id <= 0)
                return FailValidation<ProjectResearchCategoryDetailDTO>(InvalidIdMessage);

            var entity = await _uow.ProjectResearchCategories
                .GetByIdAsync(new object[] { id }, ct);

            if (entity is null)
                return FailNotFound<ProjectResearchCategoryDetailDTO>(NotFoundMessage);

            var dto = MapToDetailDTO(entity);

            return ServiceResult<ProjectResearchCategoryDetailDTO>.Ok(dto);
        }

        // ================= WRITES =================

        public async Task<ServiceResult<ProjectResearchCategoryDetailDTO>> CreateAsync(
            AddProjectResearchCategoryRequestDTO request,
            CancellationToken ct = default)
        {
            if (request == null || request.ProjectId <= 0 || request.ResearchCategoryId <= 0)
                return FailValidation<ProjectResearchCategoryDetailDTO>(InvalidDataMessage);

            var entity = new ProjectResearchCategory
            {
                ProjectId = request.ProjectId,
                ResearchCategoryId = request.ResearchCategoryId
            };

            await _uow.ProjectResearchCategories.AddAsync(entity, ct);
            await _uow.SaveChangesAsync(ct);

            var dto = MapToDetailDTO(entity);

            return ServiceResult<ProjectResearchCategoryDetailDTO>.Ok(dto);
        }

        public async Task<ServiceResult<ProjectResearchCategoryDetailDTO>> UpdateAsync(
            UpdateProjectResearchCategoryRequestDTO request,
            CancellationToken ct = default)
        {
            if (request == null || request.Id <= 0)
                return FailValidation<ProjectResearchCategoryDetailDTO>(InvalidIdMessage);

            var entity = await _uow.ProjectResearchCategories.GetByIdAsync(
                new object[] { request.Id }, ct);

            if (entity is null)
                return FailNotFound<ProjectResearchCategoryDetailDTO>(NotFoundMessage);

            entity.ProjectId = request.ProjectId;
            entity.ResearchCategoryId = request.ResearchCategoryId;

            _uow.ProjectResearchCategories.Update(entity);
            await _uow.SaveChangesAsync(ct);

            var dto = MapToDetailDTO(entity);

            return ServiceResult<ProjectResearchCategoryDetailDTO>.Ok(dto);
        }

        public async Task<ServiceResult<NoContent>> DeleteAsync(
            int id,
            CancellationToken ct = default)
        {
            if (id <= 0)
                return FailValidation<NoContent>(InvalidIdMessage);

            var entity = await _uow.ProjectResearchCategories
                .GetByIdAsync(new object[] { id }, ct);

            if (entity == null)
                return FailNotFound<NoContent>(NotFoundMessage);

            _uow.ProjectResearchCategories.Remove(entity);
            await _uow.SaveChangesAsync(ct);

            return ServiceResult<NoContent>.Ok(new NoContent());
        }

        // ================= PRIVATE HELPERS =================

        private static ServiceResult<T> FailValidation<T>(string message)
            => ServiceResult<T>.Fail(message, ErrorType.Validation, ErrorCodes.Common.InvalidRequest);

        private static ServiceResult<T> FailNotFound<T>(string message)
            => ServiceResult<T>.Fail(message, ErrorType.NotFound, ErrorCodes.Common.NotFound);

        private static ProjectResearchCategoryListItemDTO MapToListItemDTO(ProjectResearchCategory entity)
            => new ProjectResearchCategoryListItemDTO
            {
                Id = entity.ProjectResearchCategoryId,
                ProjectId = entity.ProjectId,
                ResearchCategoryId = entity.ResearchCategoryId,
            };

        private static ProjectResearchCategoryDetailDTO MapToDetailDTO(ProjectResearchCategory entity)
            => new ProjectResearchCategoryDetailDTO
            {
                Id = entity.ProjectResearchCategoryId,
                ProjectId = entity.ProjectId,
                ResearchCategoryId = entity.ResearchCategoryId
            };
    }
}