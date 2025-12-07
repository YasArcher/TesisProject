using tesisproject.backend.Services.Interfaces;
using tesisproject.backend.UnitOfWork.Interfaces;
using tesisproject.shared.DTOs.Catalog.ProductAttribute.Request;
using tesisproject.shared.DTOs.Catalog.ProductAttribute.Response;
using tesisproject.shared.Entities.Catalogs;
using tesisproject.shared.Responses;

namespace tesisproject.backend.Services.Implementations
{
    public sealed class ProductAttributeService : IProductAttributeService
    {
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
                .Select(x => new ProductAttributeListItemDTO
                {
                    Id = x.Id,
                    Name = x.Name,
                    IsActive = x.IsActive,
                    IsLocked = x.IsLocked,
                    DataType = x.DataType,
                    Unit = x.Unit
                })
                .ToList();

            return ServiceResult<IReadOnlyList<ProductAttributeListItemDTO>>.Ok(dto);
        }

        public async Task<ServiceResult<ProductAttributeDetailDTO>> GetByIdAsync(
            int id,
            CancellationToken ct = default)
        {
            if (id <= 0)
                return ServiceResult<ProductAttributeDetailDTO>.Fail("Invalid id.", ErrorType.Validation);

            var entity = await _uow.ProductAttributes.GetByIdAsync(new object[] { id }, ct);
            if (entity is null)
                return ServiceResult<ProductAttributeDetailDTO>.Fail("ProductAttribute not found.", ErrorType.NotFound);

            var dto = new ProductAttributeDetailDTO
            {
                Id = entity.Id,
                Name = entity.Name,
                IsActive = entity.IsActive,
                IsLocked = entity.IsLocked,
                DataType = entity.DataType,
                Unit = entity.Unit
            };

            return ServiceResult<ProductAttributeDetailDTO>.Ok(dto);
        }

        // ================ WRITES ================

        public async Task<ServiceResult<ProductAttributeDetailDTO>> CreateAsync(
            AddProductAttributeRequestDTO request,
            CancellationToken ct = default)
        {
            var name = (request?.Name ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(name))
                return ServiceResult<ProductAttributeDetailDTO>.Fail("Name is required.", ErrorType.Validation);

            // Evitar duplicados por Name
            var exists = await _uow.ProductAttributes.NameExistsAsync(name, excludeId: null, ct);
            if (exists)
                return ServiceResult<ProductAttributeDetailDTO>.Fail("Name already exists.", ErrorType.Validation);

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

            var dto = new ProductAttributeDetailDTO
            {
                Id = entity.Id,
                Name = entity.Name,
                IsActive = entity.IsActive,
                IsLocked = entity.IsLocked,
                DataType = entity.DataType,
                Unit = entity.Unit
            };

            return ServiceResult<ProductAttributeDetailDTO>.Ok(dto);
        }

        public async Task<ServiceResult<ProductAttributeDetailDTO>> UpdateAsync(
            UpdateProductAttributeRequestDTO request,
            CancellationToken ct = default)
        {
            if (request is null || request.Id <= 0)
                return ServiceResult<ProductAttributeDetailDTO>.Fail("Invalid id.", ErrorType.Validation);

            var name = (request.Name ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(name))
                return ServiceResult<ProductAttributeDetailDTO>.Fail("Name is required.", ErrorType.Validation);

            var entity = await _uow.ProductAttributes.GetByIdAsync(new object[] { request.Id }, ct);
            if (entity is null)
                return ServiceResult<ProductAttributeDetailDTO>.Fail("ProductAttribute not found.", ErrorType.NotFound);

            if (entity.IsLocked)
                return ServiceResult<ProductAttributeDetailDTO>.Fail("This attribute is locked and cannot be modified.", ErrorType.Validation);

            // Validar duplicado por Name, excluyendo el propio Id
            var duplicated = await _uow.ProductAttributes.NameExistsAsync(name, excludeId: request.Id, ct);
            if (duplicated)
                return ServiceResult<ProductAttributeDetailDTO>.Fail("Name already exists.", ErrorType.Validation);

            entity.Name = name;
            entity.IsActive = request.IsActive;
            entity.DataType = request.DataType;
            entity.Unit = request.Unit;
            entity.IsLocked = request.IsLocked;

            _uow.ProductAttributes.Update(entity);
            await _uow.SaveChangesAsync(ct);

            var dto = new ProductAttributeDetailDTO
            {
                Id = entity.Id,
                Name = entity.Name,
                IsActive = entity.IsActive,
                IsLocked = entity.IsLocked,
                DataType = entity.DataType,
                Unit = entity.Unit
            };

            return ServiceResult<ProductAttributeDetailDTO>.Ok(dto);
        }

        public async Task<ServiceResult<NoContent>> DeleteAsync(
            int id,
            CancellationToken ct = default)
        {
            if (id <= 0)
                return ServiceResult<NoContent>.Fail("Invalid id.", ErrorType.Validation);

            var entity = await _uow.ProductAttributes.GetByIdAsync(new object[] { id }, ct);
            if (entity is null)
                return ServiceResult<NoContent>.Fail("ProductAttribute not found.", ErrorType.NotFound);

            if (entity.IsLocked)
                return ServiceResult<NoContent>.Fail(
                    "This attribute is locked and cannot be deleted.",
                    ErrorType.Validation);

            _uow.ProductAttributes.Remove(entity);
            await _uow.SaveChangesAsync(ct);

            return ServiceResult<NoContent>.Ok(new NoContent());
        }


    }
}