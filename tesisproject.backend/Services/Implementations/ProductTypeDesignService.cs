using Microsoft.EntityFrameworkCore;
using System.Linq;
using tesisproject.backend.Services.Interfaces;
using tesisproject.backend.UnitOfWork.Interfaces;
using tesisproject.shared.DTOs.Catalog.Common.Response;
using tesisproject.shared.DTOs.Catalog.ProductAttribute.Request;
using tesisproject.shared.DTOs.Catalog.ProductAttribute.Response;
using tesisproject.shared.DTOs.Products.ProductAttributeDefinition.Request;
using tesisproject.shared.DTOs.Products.ProductAttributeDefinition.Response;
using tesisproject.shared.DTOs.Products.ProductTypeDesign.Request;
using tesisproject.shared.DTOs.Products.ProductTypeDesign.Response;
using tesisproject.shared.Entities.Catalogs;
using tesisproject.shared.Entities.Core.Products;
using tesisproject.shared.Responses;

namespace tesisproject.backend.Services.Implementations
{
    public sealed class ProductTypeDesignService : IProductTypeDesignService
    {
        private const string NewTemplateMessage = "New product type design template.";
        private const string ProductTypeNotFoundMessage = "ProductType not found.";
        private const string DesignLoadedMessage = "Product type design loaded.";
        private const string RequestOrProductTypeNullMessage = "Request or ProductType is null.";
        private const string ErrorSavingProductTypeFallbackMessage = "Error saving ProductType.";
        private const string ErrorSavingProductAttributeMessageTemplate = "Error saving ProductAttribute '{0}'.";
        private const string ProductTypeNameRequiredMessage = "ProductType name is required.";
        private const string ProductTypeNameAlreadyExistsMessage = "ProductType name already exists.";
        private const string ProductTypeLockedMessage = "ProductType is locked and cannot be modified.";

        private readonly IUnitOfWork _uow;
        private readonly IProductAttributeService _productAttributeService;

        public ProductTypeDesignService(
            IUnitOfWork uow,
            IProductAttributeService productAttributeService)
        {
            _uow = uow;
            _productAttributeService = productAttributeService;
        }

        // ==================== GET DESIGN ====================

        public async Task<ServiceResult<ProductTypeDesignDetailDTO>> GetDesignAsync(
            int? productTypeId,
            CancellationToken ct = default)
        {
            try
            {
                // 1) cargar atributos disponibles (por simplicidad: todos activos)
                var attributes = await _uow.ProductAttributes.ListAsync(
                    onlyActives: true,
                    ct: ct);

                var attributeDtos = attributes
                    .Select(MapAttributeToDetail)
                    .ToList()
                    .AsReadOnly();

                // 2) si no hay productTypeId => template para creación
                if (productTypeId is null || productTypeId <= 0)
                {
                    var dtoNew = new ProductTypeDesignDetailDTO
                    {
                        ProductType = CreateNewProductTypeCatalogTemplate(),
                        Attributes = attributeDtos,
                        Definitions = Array.Empty<ProductAttributeDefinitionDetailDTO>()
                    };

                    return ServiceResult<ProductTypeDesignDetailDTO>
                        .Ok(dtoNew, NewTemplateMessage);
                }

                // 3) edición: cargar ProductType
                var typeEntity = await _uow.ProductTypes
                    .GetByIdAsync(Key(productTypeId.Value), ct);

                if (typeEntity is null)
                {
                    return ServiceResult<ProductTypeDesignDetailDTO>
                        .Fail(ProductTypeNotFoundMessage, ErrorType.NotFound);
                }

                var typeDto = MapProductTypeToCatalogDetail(typeEntity);

                // 4) cargar definiciones + nombre de atributo
                var defsQuery = _uow.ProductAttributeDefinitions
                    .QueryByType(productTypeId.Value, asNoTracking: true)
                    .Include(d => d.ProductAttribute);

                var defs = await defsQuery
                    .OrderBy(d => d.DisplayOrder)
                    .ToListAsync(ct);

                var defDtos = defs
                    .Select(MapDefinitionToDetail)
                    .ToList()
                    .AsReadOnly();

                var dto = new ProductTypeDesignDetailDTO
                {
                    ProductType = typeDto,
                    Attributes = attributeDtos,
                    Definitions = defDtos
                };

                return ServiceResult<ProductTypeDesignDetailDTO>
                    .Ok(dto, DesignLoadedMessage);
            }
            catch (Exception ex)
            {
                return ServiceResult<ProductTypeDesignDetailDTO>
                    .Fail(ex.Message, ErrorType.Unexpected);
            }
        }

        // ==================== SAVE DESIGN (CREATE/UPDATE) ====================

        public async Task<ServiceResult<ProductTypeDesignDetailDTO>> SaveDesignAsync(
            SaveProductTypeDesignRequestDTO request,
            CancellationToken ct = default)
        {
            try
            {
                if (request is null || request.ProductType is null)
                {
                    return ServiceResult<ProductTypeDesignDetailDTO>
                        .Fail(RequestOrProductTypeNullMessage, ErrorType.Validation);
                }

                // 1) Upsert ProductType
                var productTypeIdResult = await UpsertProductTypeAsync(request.ProductType, ct);
                if (!productTypeIdResult.Success || productTypeIdResult.Data <= 0)
                {
                    return ServiceResult<ProductTypeDesignDetailDTO>
                        .Fail(productTypeIdResult.Message ?? ErrorSavingProductTypeFallbackMessage, ErrorType.Validation);
                }

                var productTypeId = productTypeIdResult.Data;

                // 2) Upsert atributos (si se envían)
                var attributeIdMap = new Dictionary<int, int>(); // oldId -> newId (para nuevos)

                foreach (var attrDto in request.Attributes ?? Enumerable.Empty<ProductAttributeUpsertDTO>())
                {
                    var mappedId = await UpsertAttributeAsync(attrDto, ct);
                    if (mappedId <= 0)
                    {
                        return ServiceResult<ProductTypeDesignDetailDTO>
                            .Fail(string.Format(ErrorSavingProductAttributeMessageTemplate, attrDto.Name), ErrorType.Validation);
                    }

                    // Nota: se mantiene el comportamiento original (incluye el caso Id == 0)
                    attributeIdMap[attrDto.Id] = mappedId;
                }

                // 3) Sincronizar definiciones
                await SyncDefinitionsAsync(productTypeId, request.Definitions ?? new(), attributeIdMap, ct);

                // 4) Guardar cambios finales
                await _uow.SaveChangesAsync(ct);

                // 5) Devolver el diseño actualizado
                return await GetDesignAsync(productTypeId, ct);
            }
            catch (Exception ex)
            {
                return ServiceResult<ProductTypeDesignDetailDTO>
                    .Fail(ex.Message, ErrorType.Unexpected);
            }
        }

        // ==================== HELPERS ====================

        private async Task<ServiceResult<int>> UpsertProductTypeAsync(
            ProductTypeUpsertDTO dto,
            CancellationToken ct)
        {
            var name = NormalizeName(dto.Name);
            if (string.IsNullOrWhiteSpace(name))
            {
                return ServiceResult<int>
                    .Fail(ProductTypeNameRequiredMessage, ErrorType.Validation);
            }

            if (dto.Id == 0)
            {
                // Crear nuevo ProductType
                var exists = await _uow.ProductTypes.NameExistsAsync(name, excludeId: null, ct);
                if (exists)
                {
                    return ServiceResult<int>
                        .Fail(ProductTypeNameAlreadyExistsMessage, ErrorType.Validation);
                }

                var entity = new ProductType
                {
                    Name = name,
                    IsActive = dto.IsActive,
                    IsLocked = false
                };

                await _uow.ProductTypes.AddAsync(entity, ct);
                await _uow.SaveChangesAsync(ct);

                return ServiceResult<int>.Ok(entity.Id);
            }
            else
            {
                // Actualizar ProductType existente
                var entity = await _uow.ProductTypes.GetByIdAsync(Key(dto.Id), ct);
                if (entity is null)
                {
                    return ServiceResult<int>
                        .Fail(ProductTypeNotFoundMessage, ErrorType.NotFound);
                }

                if (entity.IsLocked)
                {
                    return ServiceResult<int>
                        .Fail(ProductTypeLockedMessage, ErrorType.Conflict);
                }

                var duplicated = await _uow.ProductTypes.NameExistsAsync(name, excludeId: dto.Id, ct);
                if (duplicated)
                {
                    return ServiceResult<int>
                        .Fail(ProductTypeNameAlreadyExistsMessage, ErrorType.Validation);
                }

                entity.Name = name;
                entity.IsActive = dto.IsActive;

                _uow.ProductTypes.Update(entity);
                await _uow.SaveChangesAsync(ct);

                return ServiceResult<int>.Ok(entity.Id);
            }
        }

        private async Task<int> UpsertAttributeAsync(
            ProductAttributeUpsertDTO dto,
            CancellationToken ct)
        {
            var name = NormalizeName(dto.Name);
            if (string.IsNullOrWhiteSpace(name))
                return 0;

            if (dto.Id == 0)
            {
                // Crear nuevo atributo usando ProductAttributeService
                var addReq = new AddProductAttributeRequestDTO
                {
                    Name = name,
                    IsActive = dto.IsActive,
                    DataType = dto.DataType,
                    Unit = dto.Unit
                };

                var result = await _productAttributeService.CreateAsync(addReq, ct);
                if (!result.Success || result.Data is null)
                    return 0;

                return result.Data.Id;
            }
            else
            {
                // Actualizar atributo existente
                var updateReq = new UpdateProductAttributeRequestDTO
                {
                    Id = dto.Id,
                    Name = name,
                    IsActive = dto.IsActive,
                    DataType = dto.DataType,
                    Unit = dto.Unit
                };

                var result = await _productAttributeService.UpdateAsync(updateReq, ct);
                if (!result.Success || result.Data is null)
                    return 0;

                return result.Data.Id;
            }
        }

        private async Task SyncDefinitionsAsync(
            int productTypeId,
            List<ProductAttributeDefinitionUpsertDTO> requested,
            Dictionary<int, int> attributeIdMap,
            CancellationToken ct)
        {
            // 1) Definiciones actuales en BD
            var existing = await _uow.ProductAttributeDefinitions
                .GetByTypeAsync(productTypeId, ct);

            var existingById = existing.ToDictionary(x => x.Id);

            var requestedIds = requested
                .Where(x => x.Id > 0)
                .Select(x => x.Id)
                .ToHashSet();

            // 2) Eliminar definiciones que ya no existen en el request
            foreach (var def in existing)
            {
                if (!requestedIds.Contains(def.Id))
                {
                    var inUse = await _uow.ProductAttributeDefinitions
                        .AnyValuesUsingDefinitionAsync(def.Id, ct);

                    if (inUse)
                    {
                        // si ya tiene valores, puedes decidir:
                        // - lanzar excepción
                        // - simplemente no borrar
                        // aquí opto por no borrar:
                        continue;
                    }

                    _uow.ProductAttributeDefinitions.Remove(def);
                }
            }

            // 3) Crear / actualizar definiciones enviadas
            foreach (var dto in requested)
            {
                // Resolver ProductAttributeId real en caso de nuevos atributos
                var attrId = dto.ProductAttributeId;
                if (attributeIdMap.TryGetValue(dto.ProductAttributeId, out var mappedId))
                {
                    attrId = mappedId;
                }

                if (dto.Id == 0)
                {
                    // nueva definición
                    var def = new ProductAttributeDefinition
                    {
                        ProductTypeId = productTypeId,
                        ProductAttributeId = attrId,
                        IsRequired = dto.IsRequired,
                        DisplayOrder = dto.DisplayOrder
                    };

                    await _uow.ProductAttributeDefinitions.AddAsync(def, ct);
                }
                else
                {
                    // actualizar existente
                    if (!existingById.TryGetValue(dto.Id, out var def))
                        continue;

                    def.ProductAttributeId = attrId;
                    def.IsRequired = dto.IsRequired;
                    def.DisplayOrder = dto.DisplayOrder;

                    _uow.ProductAttributeDefinitions.Update(def);
                }
            }
        }

        // ==================== MAPPERS ====================

        private static object[] Key(int id) => new object[] { id };

        private static string NormalizeName(string? value)
            => (value ?? string.Empty).Trim();

        private static CatalogDetailDTO CreateNewProductTypeCatalogTemplate()
            => new CatalogDetailDTO
            {
                Id = 0,
                Name = string.Empty,
                IsActive = true,
                IsLocked = false
            };

        private static CatalogDetailDTO MapProductTypeToCatalogDetail(ProductType typeEntity)
            => new CatalogDetailDTO
            {
                Id = typeEntity.Id,
                Name = typeEntity.Name,
                IsActive = typeEntity.IsActive,
                IsLocked = typeEntity.IsLocked
            };

        private static ProductAttributeDetailDTO MapAttributeToDetail(ProductAttribute x)
            => new()
            {
                Id = x.Id,
                Name = x.Name,
                IsActive = x.IsActive,
                IsLocked = x.IsLocked,
                DataType = x.DataType,
                Unit = x.Unit
            };

        private static ProductAttributeDefinitionDetailDTO MapDefinitionToDetail(
            ProductAttributeDefinition d)
            => new()
            {
                Id = d.Id,
                ProductTypeId = d.ProductTypeId,
                ProductAttributeId = d.ProductAttributeId,
                ProductAttributeName = d.ProductAttribute?.Name ?? string.Empty,
                IsRequired = d.IsRequired,
                DisplayOrder = d.DisplayOrder
            };
    }
}