using tesisproject.backend.Services.Interfaces;
using tesisproject.backend.UnitOfWork.Interfaces;
using tesisproject.shared.DTOs.Catalog.FundingType.Request;
using tesisproject.shared.DTOs.Catalog.ResearchCategoryGroup.Request;
using tesisproject.shared.DTOs.Catalog.ResearchCategoryGroup.Response;
using tesisproject.shared.DTOs.Filters;
using tesisproject.shared.Entities.Catalogs;
using tesisproject.shared.Responses;

namespace tesisproject.backend.Services.Implementations
{
    public class ResearchCategoryGroupService : IResearchCategoryGroupService
    {
        private readonly IUnitOfWork _uow;

        public ResearchCategoryGroupService(IUnitOfWork uow)
        {
            _uow = uow;
        }

        // ================= READS =================

        public async Task<ServiceResult<IReadOnlyList<ResearchCategoryGroupListItemDTO>>> ListAsync(
            bool onlyActives = true,
            CancellationToken ct = default)
        {
            var items = await _uow.ResearchCategoryGroups.ListAsync(
                onlyActives: onlyActives,
                ct: ct);

            var dto = items.Select(x => new ResearchCategoryGroupListItemDTO
            {
                Id = x.Id,
                Name = x.Name,
                IsActive = x.IsActive
            }).ToList();

            return ServiceResult<IReadOnlyList<ResearchCategoryGroupListItemDTO>>.Ok(dto);
        }

        public async Task<ServiceResult<ResearchCategoryGroupListItemDTO>> GetByIdAsync(
            int id,
            CancellationToken ct = default)
        {
            if (id <= 0)
                return ServiceResult<ResearchCategoryGroupListItemDTO>.Fail(
                    "Invalid id.",
                    ErrorType.Validation);

            var entity = await _uow.ResearchCategoryGroups.GetByIdAsync(new object[] { id }, ct);
            if (entity is null)
                return ServiceResult<ResearchCategoryGroupListItemDTO>.Fail(
                    "ResearchCategoryGroup not found.",
                    ErrorType.NotFound);

            var dto = new ResearchCategoryGroupListItemDTO
            {
                Id = entity.Id,
                Name = entity.Name,
                IsActive = entity.IsActive
            };

            return ServiceResult<ResearchCategoryGroupListItemDTO>.Ok(dto);
        }

        public async Task<ServiceResult<List<KeyValueItemDTO>>> GetKeyValuesAsync(
            string? term = null,
            int? take = null,
            CancellationToken ct = default)
        {
            var list = await _uow.ResearchCategoryGroups.GetKeyValuesAsync(term, take, ct);
            return ServiceResult<List<KeyValueItemDTO>>.Ok(list);
        }

        // ================ WRITES ================

        public async Task<ServiceResult<ResearchCategoryGroupListItemDTO>> CreateAsync(
            AddResearchCategoryGroupDTO request,
            CancellationToken ct = default)
        {
            var name = (request?.Name ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(name))
                return ServiceResult<ResearchCategoryGroupListItemDTO>.Fail(
                    "Name is required.",
                    ErrorType.Validation);

            // Evitar duplicado por Name
            var exists = await _uow.ResearchCategoryGroups.NameExistsAsync(
                name,
                excludeId: null,
                ct);

            if (exists)
                return ServiceResult<ResearchCategoryGroupListItemDTO>.Fail(
                    "Name already exists.",
                    ErrorType.Validation);

            var entity = new ResearchCategoryGroup
            {
                Name = name,
                IsActive = true
            };

            await _uow.ResearchCategoryGroups.AddAsync(entity, ct);
            await _uow.SaveChangesAsync(ct);

            var dto = new ResearchCategoryGroupListItemDTO
            {
                Id = entity.Id,
                Name = entity.Name,
                IsActive = entity.IsActive
            };

            return ServiceResult<ResearchCategoryGroupListItemDTO>.Ok(dto);
        }

        public async Task<ServiceResult<ResearchCategoryGroupListItemDTO>> UpdateAsync(
            UpdateResearchCategoryGroupDTO request,
            CancellationToken ct = default)
        {
            if (request is null || request.Id <= 0)
                return ServiceResult<ResearchCategoryGroupListItemDTO>.Fail(
                    "Invalid id.",
                    ErrorType.Validation);

            var name = (request.Name ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(name))
                return ServiceResult<ResearchCategoryGroupListItemDTO>.Fail(
                    "Name is required.",
                    ErrorType.Validation);

            var entity = await _uow.ResearchCategoryGroups.GetByIdAsync(new object[] { request.Id }, ct);
            if (entity is null)
                return ServiceResult<ResearchCategoryGroupListItemDTO>.Fail(
                    "ResearchCategoryGroup not found.",
                    ErrorType.NotFound);

            // Validar duplicado por Name excluyendo el propio Id
            var duplicated = await _uow.ResearchCategoryGroups.NameExistsAsync(
                name,
                excludeId: request.Id,
                ct);

            if (duplicated)
                return ServiceResult<ResearchCategoryGroupListItemDTO>.Fail(
                    "Name already exists.",
                    ErrorType.Validation);

            entity.Name = name;
            entity.IsActive = request.IsActive;

            _uow.ResearchCategoryGroups.Update(entity);
            await _uow.SaveChangesAsync(ct);

            var dto = new ResearchCategoryGroupListItemDTO
            {
                Id = entity.Id,
                Name = entity.Name,
                IsActive = entity.IsActive
            };

            return ServiceResult<ResearchCategoryGroupListItemDTO>.Ok(dto);
        }
    }
}