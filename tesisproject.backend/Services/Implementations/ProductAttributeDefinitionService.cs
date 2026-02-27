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
        private const string InvalidProductTypeIdMessage = "Invalid product type id.";
        private const string InvalidIdMessage = "Invalid id.";
        private const string DefinitionNotFoundMessage = "Definition not found.";
        private const string ProductTypeAndAttributeRequiredMessage = "ProductTypeId and ProductAttributeId are required.";
        private const string AttributeAlreadyAssignedMessage = "This attribute is already assigned to the selected product type.";

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
                    .Fail(InvalidProductTypeIdMessage, ErrorType.Validation);

            var query = QueryWithRefs()
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
                return ServiceResult<ProductAttributeDefinitionDetailDTO>.Fail(InvalidIdMessage, ErrorType.Validation);

            var entity = await QueryWithRefs()
                .FirstOrDefaultAsync(x => x.Id == id, ct);

            if (entity is null)
                return ServiceResult<ProductAttributeDefinitionDetailDTO>.Fail(DefinitionNotFoundMessage, ErrorType.NotFound);

            var dto = MapToDetailDto(entity);
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
                    ProductTypeAndAttributeRequiredMessage,
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
                    AttributeAlreadyAssignedMessage,
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
            var created = await QueryWithRefs()
                .FirstAsync(x => x.Id == entity.Id, ct);

            var dto = MapToDetailDto(created);
            return ServiceResult<ProductAttributeDefinitionDetailDTO>.Ok(dto);
        }

        public async Task<ServiceResult<ProductAttributeDefinitionDetailDTO>> UpdateAsync(
            UpdateProductAttributeDefinitionRequestDTO request,
            CancellationToken ct = default)
        {
            if (request is null || request.Id <= 0)
                return ServiceResult<ProductAttributeDefinitionDetailDTO>.Fail(InvalidIdMessage, ErrorType.Validation);

            var entity = await _uow.ProductAttributeDefinitions
                .GetByIdAsync(Key(request.Id), ct);

            if (entity is null)
                return ServiceResult<ProductAttributeDefinitionDetailDTO>.Fail(DefinitionNotFoundMessage, ErrorType.NotFound);

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
                    AttributeAlreadyAssignedMessage,
                    ErrorType.Validation);
            }

            entity.ProductTypeId = request.ProductTypeId;
            entity.ProductAttributeId = request.ProductAttributeId;
            entity.IsRequired = request.IsRequired;
            entity.DisplayOrder = request.DisplayOrder;

            _uow.ProductAttributeDefinitions.Update(entity);
            await _uow.SaveChangesAsync(ct);

            // Recargar detalle con includes
            var updated = await QueryWithRefs()
                .FirstAsync(x => x.Id == entity.Id, ct);

            var dto = MapToDetailDto(updated);
            return ServiceResult<ProductAttributeDefinitionDetailDTO>.Ok(dto);
        }

        public async Task<ServiceResult<bool>> DeleteAsync(
            int id,
            CancellationToken ct = default)
        {
            if (id <= 0)
                return ServiceResult<bool>.Fail(InvalidIdMessage, ErrorType.Validation);

            var entity = await _uow.ProductAttributeDefinitions
                .GetByIdAsync(Key(id), ct);

            if (entity is null)
                return ServiceResult<bool>.Fail(DefinitionNotFoundMessage, ErrorType.NotFound);

            _uow.ProductAttributeDefinitions.Remove(entity);
            await _uow.SaveChangesAsync(ct);

            return ServiceResult<bool>.Ok(true);
        }

        // ============== Helpers ==============

        private IQueryable<ProductAttributeDefinition> QueryWithRefs()
            => _uow.ProductAttributeDefinitions
                .Query()
                .Include(x => x.ProductType)
                .Include(x => x.ProductAttribute);

        private static object[] Key(int id) => new object[] { id };

        private static ProductAttributeDefinitionDetailDTO MapToDetailDto(ProductAttributeDefinition entity)
            => new ProductAttributeDefinitionDetailDTO
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
    }
}