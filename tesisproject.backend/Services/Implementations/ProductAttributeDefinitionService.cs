using Microsoft.EntityFrameworkCore;
using tesisproject.backend.Services.Interfaces;
using tesisproject.backend.UnitOfWork.Interfaces;
using tesisproject.shared.DTOs.Products.ProductAttributeDefinition.Request;
using tesisproject.shared.DTOs.Products.ProductAttributeDefinition.Response;
using tesisproject.shared.Entities.Core.Products;
using tesisproject.shared.Responses;

namespace tesisproject.backend.Services.Implementations
{
    public sealed class ProductAttributeDefinitionService : IProductAttributeDefinitionService
    {
        private readonly IUnitOfWork _uow;

        public ProductAttributeDefinitionService(IUnitOfWork uow)
        {
            _uow = uow;
        }

        // ============== READS ==============

        public async Task<ServiceResult<IReadOnlyList<ProductAttributeDefinitionListItemDTO>>> ListByProductTypeAsync(
            int productTypeId,
            CancellationToken ct = default)
        {
            if (productTypeId <= 0)
                return ServiceResult<IReadOnlyList<ProductAttributeDefinitionListItemDTO>>
                    .Fail("Invalid product type id.", ErrorType.Validation);

            var query = _uow.ProductAttributeDefinitions
                .Query()
                .Include(x => x.ProductType)
                .Include(x => x.ProductAttribute)
                .Where(x => x.ProductTypeId == productTypeId)
                .OrderBy(x => x.DisplayOrder)
                .ThenBy(x => x.Id);

            var list = await query
                .Select(x => new ProductAttributeDefinitionListItemDTO
                {
                    Id = x.Id,
                    ProductTypeId = x.ProductTypeId,
                    ProductTypeName = x.ProductType!.Name,
                    ProductAttributeId = x.ProductAttributeId,
                    ProductAttributeName = x.ProductAttribute!.Name,
                    DataType = x.ProductAttribute!.DataType,
                    Unit = x.ProductAttribute!.Unit,
                    IsRequired = x.IsRequired,
                    DisplayOrder = x.DisplayOrder
                })
                .ToListAsync(ct);

            return ServiceResult<IReadOnlyList<ProductAttributeDefinitionListItemDTO>>.Ok(list);
        }

        public async Task<ServiceResult<ProductAttributeDefinitionDetailDTO>> GetByIdAsync(
            int id,
            CancellationToken ct = default)
        {
            if (id <= 0)
                return ServiceResult<ProductAttributeDefinitionDetailDTO>.Fail("Invalid id.", ErrorType.Validation);

            var entity = await _uow.ProductAttributeDefinitions
                .Query()
                .Include(x => x.ProductType)
                .Include(x => x.ProductAttribute)
                .FirstOrDefaultAsync(x => x.Id == id, ct);

            if (entity is null)
                return ServiceResult<ProductAttributeDefinitionDetailDTO>.Fail("Definition not found.", ErrorType.NotFound);

            var dto = new ProductAttributeDefinitionDetailDTO
            {
                Id = entity.Id,
                ProductTypeId = entity.ProductTypeId,
                ProductTypeName = entity.ProductType!.Name,
                ProductAttributeId = entity.ProductAttributeId,
                ProductAttributeName = entity.ProductAttribute!.Name,
                DataType = entity.ProductAttribute!.DataType,
                Unit = entity.ProductAttribute!.Unit,
                IsRequired = entity.IsRequired,
                DisplayOrder = entity.DisplayOrder
            };

            return ServiceResult<ProductAttributeDefinitionDetailDTO>.Ok(dto);
        }

        // ============== WRITES ==============

        public async Task<ServiceResult<ProductAttributeDefinitionDetailDTO>> CreateAsync(
            AddProductAttributeDefinitionRequestDTO request,
            CancellationToken ct = default)
        {
            if (request is null ||
                request.ProductTypeId <= 0 ||
                request.ProductAttributeId <= 0)
            {
                return ServiceResult<ProductAttributeDefinitionDetailDTO>.Fail(
                    "ProductTypeId and ProductAttributeId are required.",
                    ErrorType.Validation);
            }

            // Validar que no exista ya la combinación (ProductType, ProductAttribute)
            var exists = await _uow.ProductAttributeDefinitions
                .ExistsAsync(x =>
                    x.ProductTypeId == request.ProductTypeId &&
                    x.ProductAttributeId == request.ProductAttributeId,
                    ct);

            if (exists)
            {
                return ServiceResult<ProductAttributeDefinitionDetailDTO>.Fail(
                    "This attribute is already assigned to the selected product type.",
                    ErrorType.Validation);
            }

            var entity = new ProductAttributeDefinition
            {
                ProductTypeId = request.ProductTypeId,
                ProductAttributeId = request.ProductAttributeId,
                IsRequired = request.IsRequired,
                DisplayOrder = request.DisplayOrder
            };

            await _uow.ProductAttributeDefinitions.AddAsync(entity, ct);
            await _uow.SaveChangesAsync(ct);

            // Recargar con includes para armar el DTO de detalle
            var created = await _uow.ProductAttributeDefinitions
                .Query()
                .Include(x => x.ProductType)
                .Include(x => x.ProductAttribute)
                .FirstAsync(x => x.Id == entity.Id, ct);

            var dto = new ProductAttributeDefinitionDetailDTO
            {
                Id = created.Id,
                ProductTypeId = created.ProductTypeId,
                ProductTypeName = created.ProductType!.Name,
                ProductAttributeId = created.ProductAttributeId,
                ProductAttributeName = created.ProductAttribute!.Name,
                DataType = created.ProductAttribute!.DataType,
                Unit = created.ProductAttribute!.Unit,
                IsRequired = created.IsRequired,
                DisplayOrder = created.DisplayOrder
            };

            return ServiceResult<ProductAttributeDefinitionDetailDTO>.Ok(dto);
        }

        public async Task<ServiceResult<ProductAttributeDefinitionDetailDTO>> UpdateAsync(
            UpdateProductAttributeDefinitionRequestDTO request,
            CancellationToken ct = default)
        {
            if (request is null || request.Id <= 0)
                return ServiceResult<ProductAttributeDefinitionDetailDTO>.Fail("Invalid id.", ErrorType.Validation);

            var entity = await _uow.ProductAttributeDefinitions
                .GetByIdAsync(new object[] { request.Id }, ct);

            if (entity is null)
                return ServiceResult<ProductAttributeDefinitionDetailDTO>.Fail("Definition not found.", ErrorType.NotFound);

            // Validar duplicado (ProductTypeId + ProductAttributeId) excluyendo el propio Id
            var duplicated = await _uow.ProductAttributeDefinitions
                .ExistsAsync(x =>
                    x.Id != request.Id &&
                    x.ProductTypeId == request.ProductTypeId &&
                    x.ProductAttributeId == request.ProductAttributeId,
                    ct);

            if (duplicated)
            {
                return ServiceResult<ProductAttributeDefinitionDetailDTO>.Fail(
                    "This attribute is already assigned to the selected product type.",
                    ErrorType.Validation);
            }

            entity.ProductTypeId = request.ProductTypeId;
            entity.ProductAttributeId = request.ProductAttributeId;
            entity.IsRequired = request.IsRequired;
            entity.DisplayOrder = request.DisplayOrder;

            _uow.ProductAttributeDefinitions.Update(entity);
            await _uow.SaveChangesAsync(ct);

            // Recargar detalle con includes
            var updated = await _uow.ProductAttributeDefinitions
                .Query()
                .Include(x => x.ProductType)
                .Include(x => x.ProductAttribute)
                .FirstAsync(x => x.Id == entity.Id, ct);

            var dto = new ProductAttributeDefinitionDetailDTO
            {
                Id = updated.Id,
                ProductTypeId = updated.ProductTypeId,
                ProductTypeName = updated.ProductType!.Name,
                ProductAttributeId = updated.ProductAttributeId,
                ProductAttributeName = updated.ProductAttribute!.Name,
                DataType = updated.ProductAttribute!.DataType,
                Unit = updated.ProductAttribute!.Unit,
                IsRequired = updated.IsRequired,
                DisplayOrder = updated.DisplayOrder
            };

            return ServiceResult<ProductAttributeDefinitionDetailDTO>.Ok(dto);
        }

        public async Task<ServiceResult<bool>> DeleteAsync(
            int id,
            CancellationToken ct = default)
        {
            if (id <= 0)
                return ServiceResult<bool>.Fail("Invalid id.", ErrorType.Validation);

            var entity = await _uow.ProductAttributeDefinitions
                .GetByIdAsync(new object[] { id }, ct);

            if (entity is null)
                return ServiceResult<bool>.Fail("Definition not found.", ErrorType.NotFound);

            _uow.ProductAttributeDefinitions.Remove(entity);
            await _uow.SaveChangesAsync(ct);

            return ServiceResult<bool>.Ok(true);
        }
    }
}