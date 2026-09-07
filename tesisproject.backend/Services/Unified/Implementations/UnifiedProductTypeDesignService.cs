using Microsoft.EntityFrameworkCore;
using System.Linq;
using tesisproject.backend.Services.Unified.Interfaces;
using tesisproject.backend.UnitOfWork.Unified.Interfaces;
using tesisproject.shared.DTOs.Catalog.Common.Response;
using tesisproject.shared.DTOs.Catalog.ProductAttribute.Request;
using tesisproject.shared.DTOs.Catalog.ProductAttribute.Response;
using tesisproject.shared.DTOs.Products.ProductAttributeDefinition.Request;
using tesisproject.shared.DTOs.Products.ProductAttributeDefinition.Response;
using tesisproject.shared.DTOs.Products.ProductTypeDesign.Request;
using tesisproject.shared.DTOs.Products.ProductTypeDesign.Response;
using tesisproject.backend.Data.UnifiedEntities.Catalogs;
using tesisproject.backend.Data.UnifiedEntities.Core.Products;
using tesisproject.shared.Errors;
using tesisproject.shared.Responses;

namespace tesisproject.backend.Services.Unified.Implementations
{
    public sealed class UnifiedProductTypeDesignService : IUnifiedProductTypeDesignService
    {
        private const string NewTemplateMessage = "New product type design template.";
        private const string DesignLoadedMessage = "Product type design loaded.";

        private readonly IUnifiedUnitOfWork _uow;

        public UnifiedProductTypeDesignService(
            IUnifiedUnitOfWork uow)
        {
            _uow = uow;
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
                        .Fail(
                            ErrorMessages.ProductTypeDesign.ProductTypeNotFound,
                            ErrorType.NotFound,
                            ErrorCodes.ProductTypeDesign.ProductTypeNotFound);
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
            catch (Exception)
            {
                return FailUnexpected<ProductTypeDesignDetailDTO>();
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
                    return ValidationFailure<ProductTypeDesignDetailDTO>(
                        ErrorMessages.ProductTypeDesign.RequestOrProductTypeRequired,
                        ErrorCodes.ProductTypeDesign.RequestOrProductTypeRequired,
                        nameof(SaveProductTypeDesignRequestDTO.ProductType));

                var typeResult = await PrepareProductTypeAsync(request.ProductType, ct);
                if (!typeResult.Success || typeResult.Data is null)
                    return ServiceResult<ProductTypeDesignDetailDTO>.Fail(
                        typeResult.Message ?? ErrorMessages.ProductTypeDesign.ErrorSavingProductType,
                        typeResult.Error == ErrorType.None ? ErrorType.Validation : typeResult.Error,
                        typeResult.ErrorCode ?? ErrorCodes.ProductTypeDesign.ErrorSavingProductType,
                        typeResult.ValidationErrors);
                var type = typeResult.Data;
                var attributes = new UnifiedProductAttributePreparation(_uow);
                var attributeMap = new Dictionary<int, ProductAttribute>();
                foreach (var dto in request.Attributes ?? Enumerable.Empty<ProductAttributeUpsertDTO>())
                {
                    var result = dto.Id == 0
                        ? await attributes.PrepareCreateAsync(new AddProductAttributeRequestDTO
                        {
                            Name = dto.Name, IsActive = dto.IsActive, DataType = dto.DataType, Unit = dto.Unit
                        }, ct)
                        : await attributes.PrepareUpdateAsync(new UpdateProductAttributeRequestDTO
                        {
                            Id = dto.Id, Name = dto.Name, IsActive = dto.IsActive,
                            DataType = dto.DataType, Unit = dto.Unit
                        }, ct);
                    if (!result.Success || result.Data is null)
                        return ValidationFailure<ProductTypeDesignDetailDTO>(
                            string.Format(ErrorMessages.ProductTypeDesign.ErrorSavingProductAttribute, dto.Name),
                            ErrorCodes.ProductTypeDesign.ErrorSavingProductAttribute,
                            nameof(SaveProductTypeDesignRequestDTO.Attributes));
                    // Preserve the original last-mapping-wins behavior, including Id == 0.
                    attributeMap[dto.Id] = result.Data.Entity;
                }

                var requested = request.Definitions ?? new();
                var existing = request.ProductType.Id == 0
                    ? new List<ProductAttributeDefinition>()
                    : (await _uow.ProductAttributeDefinitions.GetByTypeAsync(type.Id, ct)).ToList();
                var existingById = existing.ToDictionary(x => x.Id);
                var requestedIds = requested.Where(x => x.Id > 0).Select(x => x.Id).ToHashSet();
                var removals = new List<ProductAttributeDefinition>();
                foreach (var definition in existing.Where(x => !requestedIds.Contains(x.Id)))
                    if (!await _uow.ProductAttributeDefinitions.AnyValuesUsingDefinitionAsync(definition.Id, ct))
                        removals.Add(definition);

                var definitions = new List<(ProductAttributeDefinition Entity, ProductAttributeDefinition Values, bool IsNew)>();
                foreach (var dto in requested)
                {
                    var isNew = dto.Id == 0;
                    if (!isNew && !existingById.ContainsKey(dto.Id))
                        continue;
                    attributeMap.TryGetValue(dto.ProductAttributeId, out var attribute);
                    var values = new ProductAttributeDefinition
                    {
                        ProductType = type, ProductTypeId = type.Id,
                        ProductAttribute = attribute!, ProductAttributeId = attribute?.Id ?? dto.ProductAttributeId,
                        IsRequired = dto.IsRequired, DisplayOrder = dto.DisplayOrder
                    };
                    definitions.Add((isNew ? values : existingById[dto.Id], values, isNew));
                }

                // All validation and preparation have completed. A failed apply/save requires
                // discarding this operation scope; never reuse its pending tracker state.
                type.Name = NormalizeName(request.ProductType.Name);
                type.IsActive = request.ProductType.IsActive;
                if (request.ProductType.Id == 0)
                    await _uow.ProductTypes.AddAsync(type, ct);
                else
                    _uow.ProductTypes.Update(type);
                await attributes.ApplyAsync(ct);
                foreach (var definition in removals)
                    _uow.ProductAttributeDefinitions.Remove(definition);
                foreach (var (entity, values, isNew) in definitions)
                {
                    if (isNew)
                        await _uow.ProductAttributeDefinitions.AddAsync(entity, ct);
                    else
                    {
                        if (values.ProductAttribute is not null || entity.ProductAttributeId != values.ProductAttributeId)
                            entity.ProductAttribute = values.ProductAttribute!;
                        entity.ProductAttributeId = values.ProductAttributeId;
                        entity.IsRequired = values.IsRequired;
                        entity.DisplayOrder = values.DisplayOrder;
                        _uow.ProductAttributeDefinitions.Update(entity);
                    }
                }
                await _uow.SaveChangesAsync(ct);
                return await GetDesignAsync(type.Id, ct);
            }
            catch (Exception)
            {
                return FailUnexpected<ProductTypeDesignDetailDTO>();
            }
        }

        private async Task<ServiceResult<ProductType>> PrepareProductTypeAsync(
            ProductTypeUpsertDTO dto, CancellationToken ct)
        {
            var name = NormalizeName(dto.Name);
            if (string.IsNullOrWhiteSpace(name))
                return ValidationFailure<ProductType>(ErrorMessages.ProductTypeDesign.ProductTypeNameRequired,
                    ErrorCodes.ProductTypeDesign.ProductTypeNameRequired, nameof(ProductTypeUpsertDTO.Name));
            ProductType entity;
            if (dto.Id == 0)
                entity = new ProductType { Name = name, IsActive = dto.IsActive, IsLocked = false };
            else
            {
                var existing = await _uow.ProductTypes.GetByIdAsync(Key(dto.Id), ct);
                if (existing is null)
                    return ServiceResult<ProductType>.Fail(ErrorMessages.ProductTypeDesign.ProductTypeNotFound,
                        ErrorType.NotFound, ErrorCodes.ProductTypeDesign.ProductTypeNotFound);
                if (existing.IsLocked)
                    return ServiceResult<ProductType>.Fail(ErrorMessages.ProductTypeDesign.ProductTypeLocked,
                        ErrorType.Conflict, ErrorCodes.ProductTypeDesign.ProductTypeLocked);
                entity = existing;
            }
            if (await _uow.ProductTypes.NameExistsAsync(name, dto.Id == 0 ? null : dto.Id, ct))
                return ValidationFailure<ProductType>(ErrorMessages.ProductTypeDesign.ProductTypeNameAlreadyExists,
                    ErrorCodes.ProductTypeDesign.ProductTypeNameAlreadyExists, nameof(ProductTypeUpsertDTO.Name));
            return ServiceResult<ProductType>.Ok(entity);
        }

        private static ServiceResult<T> FailUnexpected<T>()
            => ServiceResult<T>.Fail(
                ErrorMessages.Common.UnexpectedError,
                ErrorType.Unexpected,
                ErrorCodes.Common.UnexpectedError);

        private static ServiceResult<T> ValidationFailure<T>(
            string message,
            string errorCode,
            params string[] fields)
        {
            Dictionary<string, string[]>? validation = null;

            if (fields is { Length: > 0 })
            {
                validation = fields
                    .Distinct(StringComparer.Ordinal)
                    .ToDictionary(
                        field => field,
                        _ => new[] { message },
                        StringComparer.Ordinal);
            }

            return ServiceResult<T>.Fail(
                message,
                ErrorType.Validation,
                errorCode,
                validation);
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