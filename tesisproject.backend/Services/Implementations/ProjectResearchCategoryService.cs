using tesisproject.backend.Services.Interfaces;
using tesisproject.backend.UnitOfWork.Interfaces;
using tesisproject.shared.DTOs.ProjectResearchCategory.Request;
using tesisproject.shared.DTOs.ProjectResearchCategory.Response;
using tesisproject.shared.Entities.Core;
using tesisproject.shared.Responses;

namespace tesisproject.backend.Services.Implementations
{
    public class ProjectResearchCategoryService : IProjectResearchCategoryService
    {
        private readonly IUnitOfWork _uow;

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

            var dto = items.Select(x => new ProjectResearchCategoryListItemDTO
            {
                Id = x.ProjectResearchCategoryId,
                ProjectId = x.ProjectId,
                ResearchCategoryId = x.ResearchCategoryId,
            }).ToList();

            return ServiceResult<List<ProjectResearchCategoryListItemDTO>>.Ok(dto);
        }

        public async Task<ServiceResult<ProjectResearchCategoryDetailDTO>> GetByIdAsync(
            int id,
            CancellationToken ct = default)
        {
            if (id <= 0)
                return ServiceResult<ProjectResearchCategoryDetailDTO>.Fail("Invalid id.", ErrorType.Validation);

            var entity = await _uow.ProjectResearchCategories
                .GetByIdAsync(new object[] { id }, ct);

            if (entity is null)
                return ServiceResult<ProjectResearchCategoryDetailDTO>.Fail("Not found.", ErrorType.NotFound);

            var dto = new ProjectResearchCategoryDetailDTO
            {
                Id = entity.ProjectResearchCategoryId,
                ProjectId = entity.ProjectId,
                ResearchCategoryId = entity.ResearchCategoryId
            };

            return ServiceResult<ProjectResearchCategoryDetailDTO>.Ok(dto);
        }

        // ================= WRITES =================

        public async Task<ServiceResult<ProjectResearchCategoryDetailDTO>> CreateAsync(
            AddProjectResearchCategoryRequestDTO request,
            CancellationToken ct = default)
        {
            if (request == null || request.ProjectId <= 0 || request.ResearchCategoryId <= 0)
                return ServiceResult<ProjectResearchCategoryDetailDTO>.Fail("Invalid data.", ErrorType.Validation);

            var entity = new ProjectResearchCategory
            {
                ProjectId = request.ProjectId,
                ResearchCategoryId = request.ResearchCategoryId
            };

            await _uow.ProjectResearchCategories.AddAsync(entity, ct);
            await _uow.SaveChangesAsync(ct);

            var dto = new ProjectResearchCategoryDetailDTO
            {
                Id = entity.ProjectResearchCategoryId,
                ProjectId = entity.ProjectId,
                ResearchCategoryId = entity.ResearchCategoryId
            };

            return ServiceResult<ProjectResearchCategoryDetailDTO>.Ok(dto);
        }

        public async Task<ServiceResult<ProjectResearchCategoryDetailDTO>> UpdateAsync(
            UpdateProjectResearchCategoryRequestDTO request,
            CancellationToken ct = default)
        {
            if (request == null || request.Id <= 0)
                return ServiceResult<ProjectResearchCategoryDetailDTO>.Fail("Invalid id.", ErrorType.Validation);

            var entity = await _uow.ProjectResearchCategories.GetByIdAsync(
                new object[] { request.Id }, ct);

            if (entity is null)
                return ServiceResult<ProjectResearchCategoryDetailDTO>.Fail("Not found.", ErrorType.NotFound);

            entity.ProjectId = request.ProjectId;
            entity.ResearchCategoryId = request.ResearchCategoryId;

            _uow.ProjectResearchCategories.Update(entity);
            await _uow.SaveChangesAsync(ct);

            var dto = new ProjectResearchCategoryDetailDTO
            {
                Id = entity.ProjectResearchCategoryId,
                ProjectId = entity.ProjectId,
                ResearchCategoryId = entity.ResearchCategoryId
            };

            return ServiceResult<ProjectResearchCategoryDetailDTO>.Ok(dto);
        }

        public async Task<ServiceResult<NoContent>> DeleteAsync(
    int id,
    CancellationToken ct = default)
        {
            if (id <= 0)
                return ServiceResult<NoContent>.Fail("Invalid id.", ErrorType.Validation);

            var entity = await _uow.ProjectResearchCategories
                .GetByIdAsync(new object[] { id }, ct);

            if (entity == null)
                return ServiceResult<NoContent>.Fail("Not found.", ErrorType.NotFound);

            _uow.ProjectResearchCategories.Remove(entity);
            await _uow.SaveChangesAsync(ct);

            return ServiceResult<NoContent>.Ok(new NoContent());
        }

    }
}