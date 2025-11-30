using Microsoft.EntityFrameworkCore;
using tesisproject.backend.Services.Interfaces;
using tesisproject.backend.UnitOfWork.Interfaces;
using tesisproject.shared.DTOs.Catalog.ResearchCategoryType.Request;
using tesisproject.shared.DTOs.Catalog.ResearchCategoryType.Response;
using tesisproject.shared.Entities.Catalogs;
using tesisproject.shared.Responses;

namespace tesisproject.backend.Services.Implementations
{
    public class ResearchCategoryTypeService : IResearchCategoryTypeService
    {
        private readonly IUnitOfWork _uow;

        public ResearchCategoryTypeService(IUnitOfWork uow)
        {
            _uow = uow;
        }

        // ============================
        //             LIST
        // ============================
        public async Task<ServiceResult<IReadOnlyList<ResearchCategoryTypeListItemDTO>>> ListAsync(
            bool onlyActives = true,
            CancellationToken ct = default)
        {
            var items = await _uow.ResearchCategoryTypes.ListAsync(
                onlyActives: onlyActives,
                where: null,
                include: q => q.Include(x => x.ResearchCategories),
                ct: ct
            );


            var dto = items.Select(x => new ResearchCategoryTypeListItemDTO
            {
                Id = x.Id,
                Name = x.Name,
                IsActive = x.IsActive,
                ResearchCategoryGroupId = x.ResearchCategoryGroupId,
                CategoriesCount = x.ResearchCategories.Count
            })
            .ToList();

            return ServiceResult<IReadOnlyList<ResearchCategoryTypeListItemDTO>>.Ok(dto);
        }

        // ============================
        //          GET BY ID
        // ============================
        public async Task<ServiceResult<ResearchCategoryTypeDetailDTO>> GetByIdAsync(
            int id,
            CancellationToken ct = default)
        {
            var entity = await _uow.ResearchCategoryTypes
                .Query()
                .Include(x => x.ResearchCategories)
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id, ct);

            if (entity == null)
                return ServiceResult<ResearchCategoryTypeDetailDTO>.Fail(
                    "Item not found",
                    ErrorType.NotFound
                );

            var dto = new ResearchCategoryTypeDetailDTO
            {
                Id = entity.Id,
                Name = entity.Name,
                IsActive = entity.IsActive,
                ResearchCategoryGroupId = entity.ResearchCategoryGroupId,
                Categories = entity.ResearchCategories
                    .Select(c => new ResearchCategoryTypeDetailDTO.ResearchCategoryItem
                    {
                        Id = c.Id,
                        Name = c.Name
                    })
                    .ToList()
            };

            return ServiceResult<ResearchCategoryTypeDetailDTO>.Ok(dto);
        }

        // ============================
        //             CREATE
        // ============================
        public async Task<ServiceResult<int>> CreateAsync(
            ResearchCategoryTypeCreateRequestDTO dto,
            CancellationToken ct = default)
        {
            // Validate name exists
            bool exists = await _uow.ResearchCategoryTypes.NameExistsAsync(dto.Name, null, ct);
            if (exists)
                return ServiceResult<int>.Fail(
                    "Name already exists",
                    ErrorType.Conflict
                );

            var entity = new ResearchCategoryType
            {
                Name = dto.Name,
                IsActive = true,
                ResearchCategoryGroupId = dto.ResearchCategoryGroupId
            };

            await _uow.ResearchCategoryTypes.AddAsync(entity, ct);
            await _uow.SaveChangesAsync(ct);

            // No tienes Created(), así que usamos Ok(id)
            return ServiceResult<int>.Ok(entity.Id);
        }

        // ============================
        //             UPDATE
        // ============================
        public async Task<ServiceResult<bool>> UpdateAsync(
            int id,
            ResearchCategoryTypeUpdateRequestDTO dto,
            CancellationToken ct = default)
        {
            var entity = await _uow.ResearchCategoryTypes.GetByIdAsync(new object[] { id }, ct);
            if (entity == null)
                return ServiceResult<bool>.Fail("Item not found", ErrorType.NotFound);

            // Validate name
            bool exists = await _uow.ResearchCategoryTypes.NameExistsAsync(dto.Name, id, ct);
            if (exists)
                return ServiceResult<bool>.Fail("Name already exists", ErrorType.Conflict);

            entity.Name = dto.Name;
            entity.IsActive = dto.IsActive;
            entity.ResearchCategoryGroupId = dto.ResearchCategoryGroupId;

            _uow.ResearchCategoryTypes.Update(entity);
            await _uow.SaveChangesAsync(ct);

            return ServiceResult<bool>.Ok(true);
        }

        // ============================
        //             DELETE
        // ============================
        public async Task<ServiceResult<bool>> DeleteAsync(
            int id,
            CancellationToken ct = default)
        {
            var entity = await _uow.ResearchCategoryTypes.GetByIdAsync(new object[] { id }, ct);

            if (entity == null)
                return ServiceResult<bool>.Fail("Item not found", ErrorType.NotFound);

            _uow.ResearchCategoryTypes.Remove(entity);
            await _uow.SaveChangesAsync(ct);

            return ServiceResult<bool>.Ok(true);
        }
    }
}
