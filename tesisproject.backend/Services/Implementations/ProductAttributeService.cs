using tesisproject.backend.Services.Interfaces;
using tesisproject.backend.UnitOfWork.Interfaces;
using tesisproject.shared.DTOs.Catalog.ProductAttribute.Request;
using tesisproject.shared.DTOs.Catalog.ProductAttribute.Response;
using tesisproject.shared.Entities.Catalogs;
using tesisproject.shared.Responses;
using tesisproject.shared.Errors;

namespace tesisproject.backend.Services.Implementations
{
    public sealed class ProductAttributeService : IProductAttributeService
    {
        private const string InvalidIdMessage = "Invalid id.";
        private const string NotFoundMessage = "ProductAttribute not found.";
        private const string NameRequiredMessage = "Name is required.";
        private const string NameAlreadyExistsMessage = "Name already exists.";
        private const string LockedCannotModifyMessage = "This attribute is locked and cannot be modified.";
        private const string LockedCannotDeleteMessage = "This attribute is locked and cannot be deleted.";

        private readonly IUnitOfWork _uow;

        public ProductAttributeService(IUnitOfWork uow)
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
                return ServiceResult<ProductAttributeDetailDTO>.Fail(InvalidIdMessage, ErrorType.Validation, ErrorCodes.Common.InvalidRequest);

            var entity = await _uow.ProductAttributes.GetByIdAsync(Key(id), ct);
            if (entity is null)
                return ServiceResult<ProductAttributeDetailDTO>.Fail(NotFoundMessage, ErrorType.NotFound, ErrorCodes.Common.NotFound);

            var dto = ToDetailDto(entity);
            return ServiceResult<ProductAttributeDetailDTO>.Ok(dto);
        }

        // ================ WRITES ================

        public async Task<ServiceResult<ProductAttributeDetailDTO>> CreateAsync(
            AddProductAttributeRequestDTO request,
            CancellationToken ct = default)
        {
            var name = NormalizeName(request?.Name);

            if (string.IsNullOrWhiteSpace(name))
                return ServiceResult<ProductAttributeDetailDTO>.Fail(NameRequiredMessage, ErrorType.Validation, ErrorCodes.Common.InvalidRequest);

            // Evitar duplicados por Name
            var exists = await _uow.ProductAttributes.NameExistsAsync(name, excludeId: null, ct);
            if (exists)
                return ServiceResult<ProductAttributeDetailDTO>.Fail(NameAlreadyExistsMessage, ErrorType.Validation, ErrorCodes.Common.InvalidRequest);

            var entity = new ProductAttribute
            {
                Name = name,
                IsActive = request.IsActive,
                DataType = request.DataType,
                Unit = request.Unit,
                IsLocked = false
            };

            await _uow.ProductAttributes.AddAsync(entity, ct);
            await _uow.SaveChangesAsync(ct);

            var dto = ToDetailDto(entity);
            return ServiceResult<ProductAttributeDetailDTO>.Ok(dto);
        }

        public async Task<ServiceResult<ProductAttributeDetailDTO>> UpdateAsync(
            UpdateProductAttributeRequestDTO request,
            CancellationToken ct = default)
        {
            if (request is null || request.Id <= 0)
                return ServiceResult<ProductAttributeDetailDTO>.Fail(InvalidIdMessage, ErrorType.Validation, ErrorCodes.Common.InvalidRequest);

            var name = NormalizeName(request.Name);
            if (string.IsNullOrWhiteSpace(name))
                return ServiceResult<ProductAttributeDetailDTO>.Fail(NameRequiredMessage, ErrorType.Validation, ErrorCodes.Common.InvalidRequest);

            var entity = await _uow.ProductAttributes.GetByIdAsync(Key(request.Id), ct);
            if (entity is null)
                return ServiceResult<ProductAttributeDetailDTO>.Fail(NotFoundMessage, ErrorType.NotFound, ErrorCodes.Common.NotFound);

            if (entity.IsLocked)
                return ServiceResult<ProductAttributeDetailDTO>.Fail(LockedCannotModifyMessage, ErrorType.Validation, ErrorCodes.Common.InvalidRequest);

            // Validar duplicado por Name, excluyendo el propio Id
            var duplicated = await _uow.ProductAttributes.NameExistsAsync(name, excludeId: request.Id, ct);
            if (duplicated)
                return ServiceResult<ProductAttributeDetailDTO>.Fail(NameAlreadyExistsMessage, ErrorType.Validation, ErrorCodes.Common.InvalidRequest);

            entity.Name = name;
            entity.IsActive = request.IsActive;
            entity.DataType = request.DataType;
            entity.Unit = request.Unit;
            entity.IsLocked = request.IsLocked;

            _uow.ProductAttributes.Update(entity);
            await _uow.SaveChangesAsync(ct);

            var dto = ToDetailDto(entity);
            return ServiceResult<ProductAttributeDetailDTO>.Ok(dto);
        }

        public async Task<ServiceResult<NoContent>> DeleteAsync(
            int id,
            CancellationToken ct = default)
        {
            if (id <= 0)
                return ServiceResult<NoContent>.Fail(InvalidIdMessage, ErrorType.Validation, ErrorCodes.Common.InvalidRequest);

            var entity = await _uow.ProductAttributes.GetByIdAsync(Key(id), ct);
            if (entity is null)
                return ServiceResult<NoContent>.Fail(NotFoundMessage, ErrorType.NotFound, ErrorCodes.Common.NotFound);

            if (entity.IsLocked)
                return ServiceResult<NoContent>.Fail(
                    LockedCannotDeleteMessage,
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