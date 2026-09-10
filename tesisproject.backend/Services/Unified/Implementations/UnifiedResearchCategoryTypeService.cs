using Microsoft.EntityFrameworkCore;
using tesisproject.backend.Services.Interfaces;
using tesisproject.backend.Services.Unified.Interfaces;
using tesisproject.backend.UnitOfWork.Unified.Interfaces;
using tesisproject.shared.DTOs.Catalog.ResearchCategoryType.Request;
using tesisproject.shared.DTOs.Catalog.ResearchCategoryType.Response;
using tesisproject.backend.Data.UnifiedEntities.Catalogs;
using tesisproject.shared.Responses;
using tesisproject.shared.Errors;

namespace tesisproject.backend.Services.Unified.Implementations
{
    public class UnifiedResearchCategoryTypeService : IUnifiedResearchCategoryTypeService
    {



        private readonly IUnifiedUnitOfWork _uow;

        public UnifiedResearchCategoryTypeService(IUnifiedUnitOfWork uow)
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

            var dto = items
                .Select(ToListItemDto)
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
                    ErrorMessages.UnifiedLegacy.ResearchCategoryTypeService_MsgItemNotFound,
                    ErrorType.NotFound
                );

            var dto = ToDetailDto(entity);

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
            var nameExists = await _uow.ResearchCategoryTypes.NameExistsAsync(dto.Name, null, ct);
            if (nameExists)
                return ServiceResult<int>.Fail(
                    ErrorMessages.UnifiedLegacy.ResearchCategoryTypeService_MsgNameAlreadyExists,
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
                return ServiceResult<bool>.Fail(ErrorMessages.UnifiedLegacy.ResearchCategoryTypeService_MsgItemNotFound, ErrorType.NotFound, ErrorCodes.Common.NotFound);

            // Validate name
            var nameExists = await _uow.ResearchCategoryTypes.NameExistsAsync(dto.Name, id, ct);
            if (nameExists)
                return ServiceResult<bool>.Fail(ErrorMessages.UnifiedLegacy.ResearchCategoryTypeService_MsgNameAlreadyExists, ErrorType.Conflict, ErrorCodes.Common.PersistenceConflict);

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
                return ServiceResult<bool>.Fail(ErrorMessages.UnifiedLegacy.ResearchCategoryTypeService_MsgItemNotFound, ErrorType.NotFound, ErrorCodes.Common.NotFound);

            _uow.ResearchCategoryTypes.Remove(entity);
            await _uow.SaveChangesAsync(ct);

            return ServiceResult<bool>.Ok(true);
        }

        private static ResearchCategoryTypeListItemDTO ToListItemDto(ResearchCategoryType x)
        {
            return new ResearchCategoryTypeListItemDTO
            {
                Id = x.Id,
                Name = x.Name,
                IsActive = x.IsActive,
                ResearchCategoryGroupId = x.ResearchCategoryGroupId,
                CategoriesCount = x.ResearchCategories.Count
            };
        }

        private static ResearchCategoryTypeDetailDTO ToDetailDto(ResearchCategoryType entity)
        {
            return new ResearchCategoryTypeDetailDTO
            {
                Id = entity.Id,
                Name = entity.Name,
                IsActive = entity.IsActive,
                ResearchCategoryGroupId = entity.ResearchCategoryGroupId,
                Categories = entity.ResearchCategories
                    .Select(ToDetailItemDto)
                    .ToList()
            };
        }

        private static ResearchCategoryTypeDetailDTO.ResearchCategoryItem ToDetailItemDto(ResearchCategory c)
        {
            return new ResearchCategoryTypeDetailDTO.ResearchCategoryItem
            {
                Id = c.Id,
                Name = c.Name
            };
        }
    }
}