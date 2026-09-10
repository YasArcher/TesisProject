using tesisproject.backend.Services.Unified.Interfaces;
using tesisproject.backend.UnitOfWork.Unified.Interfaces;
using tesisproject.shared.DTOs.Catalog.ProductAttribute.Request;
using tesisproject.shared.DTOs.Catalog.ProductAttribute.Response;
using tesisproject.backend.Data.UnifiedEntities.Catalogs;
using tesisproject.shared.Responses;
using tesisproject.shared.Errors;

namespace tesisproject.backend.Services.Unified.Implementations
{
    public sealed class UnifiedProductAttributeService : IUnifiedProductAttributeService
    {







        private readonly IUnifiedUnitOfWork _uow;

        public UnifiedProductAttributeService(IUnifiedUnitOfWork uow)
        {
            _uow = uow;
        }

        // ================= READS =================

        public async Task<ServiceResult<IReadOnlyList<ProductAttributeListItemDTO>>> ListAsync(
            bool onlyActives = true,
            CancellationToken ct = default)
        {
            var items = await _uow.ProductAttributes.ListAsync(
                onlyActives: onlyActives,
                ct: ct);

            var dto = items
                .Select(ToListItemDto)
                .ToList();

            return ServiceResult<IReadOnlyList<ProductAttributeListItemDTO>>.Ok(dto);
        }

        public async Task<ServiceResult<ProductAttributeDetailDTO>> GetByIdAsync(
            int id,
            CancellationToken ct = default)
        {
            if (id <= 0)
                return ServiceResult<ProductAttributeDetailDTO>.Fail(ErrorMessages.UnifiedLegacy.IndexingSourceService_InvalidIdMessage, ErrorType.Validation, ErrorCodes.Common.InvalidRequest);

            var entity = await _uow.ProductAttributes.GetByIdAsync(Key(id), ct);
            if (entity is null)
                return ServiceResult<ProductAttributeDetailDTO>.Fail(ErrorMessages.UnifiedLegacy.ProductAttributeService_NotFoundMessage, ErrorType.NotFound, ErrorCodes.Common.NotFound);

            var dto = ToDetailDto(entity);
            return ServiceResult<ProductAttributeDetailDTO>.Ok(dto);
        }

        // ================ WRITES ================

        public async Task<ServiceResult<ProductAttributeDetailDTO>> CreateAsync(
            AddProductAttributeRequestDTO request,
            CancellationToken ct = default)
        {
            var preparation = new UnifiedProductAttributePreparation(_uow);
            var result = await preparation.PrepareCreateAsync(request, ct);
            if (!result.Success || result.Data is null)
                return ServiceResult<ProductAttributeDetailDTO>.Fail(result.Message!, result.Error,
                    result.ErrorCode, result.ValidationErrors);
            await preparation.ApplyAsync(ct);
            await _uow.SaveChangesAsync(ct);
            return ServiceResult<ProductAttributeDetailDTO>.Ok(ToDetailDto(result.Data.Entity));
        }

        public async Task<ServiceResult<ProductAttributeDetailDTO>> UpdateAsync(
            UpdateProductAttributeRequestDTO request,
            CancellationToken ct = default)
        {
            var preparation = new UnifiedProductAttributePreparation(_uow);
            var result = await preparation.PrepareUpdateAsync(request, ct);
            if (!result.Success || result.Data is null)
                return ServiceResult<ProductAttributeDetailDTO>.Fail(result.Message!, result.Error,
                    result.ErrorCode, result.ValidationErrors);
            await preparation.ApplyAsync(ct);
            await _uow.SaveChangesAsync(ct);
            return ServiceResult<ProductAttributeDetailDTO>.Ok(ToDetailDto(result.Data.Entity));
        }

        public async Task<ServiceResult<NoContent>> DeleteAsync(
            int id,
            CancellationToken ct = default)
        {
            if (id <= 0)
                return ServiceResult<NoContent>.Fail(ErrorMessages.UnifiedLegacy.IndexingSourceService_InvalidIdMessage, ErrorType.Validation, ErrorCodes.Common.InvalidRequest);

            var entity = await _uow.ProductAttributes.GetByIdAsync(Key(id), ct);
            if (entity is null)
                return ServiceResult<NoContent>.Fail(ErrorMessages.UnifiedLegacy.ProductAttributeService_NotFoundMessage, ErrorType.NotFound, ErrorCodes.Common.NotFound);

            if (entity.IsLocked)
                return ServiceResult<NoContent>.Fail(
                    ErrorMessages.UnifiedLegacy.ProductAttributeService_LockedCannotDeleteMessage,
                    ErrorType.Validation, ErrorCodes.Common.InvalidRequest);

            _uow.ProductAttributes.Remove(entity);
            await _uow.SaveChangesAsync(ct);

            return ServiceResult<NoContent>.Ok(new NoContent());
        }

        // ================= Helpers =================

        private static object[] Key(int id) => new object[] { id };

        private static string NormalizeName(string? value)
            => (value ?? string.Empty).Trim();

        private static ProductAttributeListItemDTO ToListItemDto(ProductAttribute entity)
            => new ProductAttributeListItemDTO
            {
                Id = entity.Id,
                Name = entity.Name,
                IsActive = entity.IsActive,
                IsLocked = entity.IsLocked,
                DataType = entity.DataType,
                Unit = entity.Unit
            };

        private static ProductAttributeDetailDTO ToDetailDto(ProductAttribute entity)
            => new ProductAttributeDetailDTO
            {
                Id = entity.Id,
                Name = entity.Name,
                IsActive = entity.IsActive,
                IsLocked = entity.IsLocked,
                DataType = entity.DataType,
                Unit = entity.Unit
            };
    }
}