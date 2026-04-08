using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using tesisproject.backend.Services.Interfaces;
using tesisproject.backend.UnitOfWork.Interfaces;
using tesisproject.shared.DTOs.Products.Product.Request;
using tesisproject.shared.DTOs.Products.Product.Response;
using tesisproject.shared.Entities.Core.Products;
using tesisproject.shared.Enums;
using tesisproject.shared.Responses;
using tesisproject.shared.Errors;

namespace tesisproject.backend.Services.Implementations
{
    public class ProductService : IProductService
    {
        private const string RequestRequiredMessage = "Request is required.";
        private const string ProjectIdRequiredMessage = "ProjectId is required.";
        private const string TitleRequiredMessage = "Title is required.";
        private const string ProductTypeIdRequiredMessage = "ProductTypeId is required.";
        private const string ProductTypeNotFoundMessage = "ProductType not found.";

        private const string ProductNotFoundMessage = "Product not found.";
        private const string NoProductsFoundMessage = "No products found.";
        private const string ProjectIdRequiredLowercaseMessage = "projectId is required.";
        private const string NoProductsFoundForProjectMessage = "No products found for this project.";

        private const string ProductCouldNotLoadAfterCreationMessage = "Product could not be loaded after creation.";
        private const string ProductCouldNotLoadAfterUpdateMessage = "Product could not be loaded after update.";

        private const string ProductCreatedMessage = "Product created";
        private const string ProductRetrievedMessage = "Product retrieved";
        private const string ProductsRetrievedMessage = "Products retrieved";
        private const string ProjectProductsRetrievedMessage = "Project products retrieved";
        private const string ProductUpdatedMessage = "Product updated";
        private const string ProductDeletedMessage = "Product deleted";

        private readonly IUnitOfWork _uow;

        private static readonly Expression<Func<Product, ProductListItemResponseDTO>> MapToListItemExpression = p => new ProductListItemResponseDTO
        {
            Id = p.Id,
            ProjectId = p.ProjectId,
            VisitId = p.VisitId,
            Title = p.Title,
            Description = p.Description,
            ProductTypeId = p.ProductTypeId,
            ProductTypeName = p.ProductType != null ? p.ProductType.Name : string.Empty,
            IsActive = p.IsActive,
            CreatedAt = p.CreatedAt
        };

        public ProductService(IUnitOfWork uow)
        {
            _uow = uow;
        }

        // ===================== CREATE =====================

        public async Task<ServiceResult<ProductDetailResponseDTO>> CreateAsync(
            ProductCreateRequestDTO request,
            CancellationToken ct = default)
        {
            try
            {
                // Basic validations
                if (request is null)
                    return ServiceResult<ProductDetailResponseDTO>.Fail(RequestRequiredMessage, ErrorType.Validation, ErrorCodes.Common.InvalidRequest);

                if (request.ProjectId <= 0)
                    return ServiceResult<ProductDetailResponseDTO>.Fail(ProjectIdRequiredMessage, ErrorType.Validation, ErrorCodes.Common.InvalidRequest);

                if (string.IsNullOrWhiteSpace(request.Title))
                    return ServiceResult<ProductDetailResponseDTO>.Fail(TitleRequiredMessage, ErrorType.Validation, ErrorCodes.Common.InvalidRequest);

                if (request.ProductTypeId <= 0)
                    return ServiceResult<ProductDetailResponseDTO>.Fail(ProductTypeIdRequiredMessage, ErrorType.Validation, ErrorCodes.Common.InvalidRequest);

                // ProductType exists
                var type = await _uow.ProductTypes.GetByIdAsync(Key(request.ProductTypeId), ct);
                if (type is null)
                    return ServiceResult<ProductDetailResponseDTO>.Fail(ProductTypeNotFoundMessage, ErrorType.NotFound, ErrorCodes.Common.NotFound);

                // Load definitions for this type (include ProductAttribute to validate DataType/Unit/Name)
                var defs = await _uow.ProductAttributeDefinitions
                    .QueryByType(request.ProductTypeId, asNoTracking: true)
                    .Include(d => d.ProductAttribute)
                    .OrderBy(d => d.DisplayOrder)
                    .ToListAsync(ct);

                // Validate + normalize values (by definition)
                var normalizedValuesResult = ValidateAndNormalizeValues(defs, request.Values);
                if (!normalizedValuesResult.Success)
                    return ServiceResult<ProductDetailResponseDTO>.Fail(normalizedValuesResult.Message!, ErrorType.Validation, ErrorCodes.Common.InvalidRequest);

                var normalizedValues = normalizedValuesResult.Data!; // Dictionary<int defId, string? value>

                // Create product header
                var entity = new Product
                {
                    ProjectId = request.ProjectId,
                    VisitId = request.VisitId,
                    Title = NormalizeRequiredText(request.Title),
                    Description = NormalizeOptionalText(request.Description),
                    ProductTypeId = request.ProductTypeId,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                await _uow.Products.AddAsync(entity, ct);
                await _uow.SaveChangesAsync(ct); // need ProductId

                // Sync authors
                await SyncAuthorsAsync(entity.Id, request.AuthorUserIds, ct);

                // Insert values
                foreach (var kv in normalizedValues)
                {
                    await _uow.ProductValues.AddAsync(new ProductValue
                    {
                        ProductId = entity.Id,
                        AttributeDefinitionId = kv.Key,
                        Value = kv.Value,
                        CreatedAt = DateTime.UtcNow
                    }, ct);
                }

                await _uow.SaveChangesAsync(ct);

                var withRefs = await _uow.Products.GetByIdWithRefsAsync(entity.Id, ct);
                if (withRefs is null)
                    return ServiceResult<ProductDetailResponseDTO>.Fail(
                        ProductCouldNotLoadAfterCreationMessage,
                        ErrorType.Unexpected, ErrorCodes.Common.UnexpectedError);

                return ServiceResult<ProductDetailResponseDTO>.Ok(MapToDetailDTO(withRefs), ProductCreatedMessage);
            }
            catch (DbUpdateException dbex)
            {
                return ServiceResult<ProductDetailResponseDTO>.Fail(
                    dbex.InnerException?.Message ?? dbex.Message,
                    ErrorType.Conflict, ErrorCodes.Common.PersistenceConflict);
            }
            catch (Exception ex)
            {
                return ServiceResult<ProductDetailResponseDTO>.Fail(ex.Message, ErrorType.Unexpected, ErrorCodes.Common.UnexpectedError);
            }
        }

        // ===================== READ ONE =====================

        public async Task<ServiceResult<ProductDetailResponseDTO>> GetByIdAsync(int id, CancellationToken ct = default)
        {
            try
            {
                var prod = await _uow.Products.GetByIdWithRefsAsync(id, ct);
                if (prod is null)
                    return ServiceResult<ProductDetailResponseDTO>.Fail(ProductNotFoundMessage, ErrorType.NotFound, ErrorCodes.Common.NotFound);

                return ServiceResult<ProductDetailResponseDTO>.Ok(MapToDetailDTO(prod), ProductRetrievedMessage);
            }
            catch (Exception ex)
            {
                return ServiceResult<ProductDetailResponseDTO>.Fail(ex.Message, ErrorType.Unexpected, ErrorCodes.Common.UnexpectedError);
            }
        }

        // ===================== LIST =====================

        public async Task<ServiceResult<IReadOnlyList<ProductListItemResponseDTO>>> ListAsync(CancellationToken ct = default)
        {
            try
            {
                var q = _uow.Products.QueryWithRefs(); // includes ProductType

                var items = await q
                    .OrderByDescending(p => p.CreatedAt)
                    .Select(MapToListItemExpression)
                    .ToListAsync(ct);

                if (items.Count == 0)
                    return ServiceResult<IReadOnlyList<ProductListItemResponseDTO>>.Fail(NoProductsFoundMessage, ErrorType.NotFound, ErrorCodes.Common.NotFound);

                return ServiceResult<IReadOnlyList<ProductListItemResponseDTO>>.Ok(items, ProductsRetrievedMessage);
            }
            catch (Exception ex)
            {
                return ServiceResult<IReadOnlyList<ProductListItemResponseDTO>>.Fail(ex.Message, ErrorType.Unexpected, ErrorCodes.Common.UnexpectedError);
            }
        }

        public async Task<ServiceResult<IReadOnlyList<ProductListItemResponseDTO>>> ListByProjectAsync(
            int projectId,
            CancellationToken ct = default)
        {
            try
            {
                if (projectId <= 0)
                    return ServiceResult<IReadOnlyList<ProductListItemResponseDTO>>.Fail(ProjectIdRequiredLowercaseMessage, ErrorType.Validation, ErrorCodes.Common.InvalidRequest);

                var entities = await _uow.Products.GetByProjectAsync(projectId, ct); // includes ProductType
                if (entities.Count == 0)
                    return ServiceResult<IReadOnlyList<ProductListItemResponseDTO>>.Fail(NoProductsFoundForProjectMessage, ErrorType.NotFound, ErrorCodes.Common.NotFound);

                var dtos = entities
                    .OrderByDescending(p => p.CreatedAt)
                    .Select(MapToListItemDto)
                    .ToList();

                return ServiceResult<IReadOnlyList<ProductListItemResponseDTO>>.Ok(dtos, ProjectProductsRetrievedMessage);
            }
            catch (Exception ex)
            {
                return ServiceResult<IReadOnlyList<ProductListItemResponseDTO>>.Fail(ex.Message, ErrorType.Unexpected, ErrorCodes.Common.UnexpectedError);
            }
        }

        // ===================== UPDATE =====================

        public async Task<ServiceResult<ProductDetailResponseDTO>> UpdateAsync(
            ProductUpdateRequestDTO request,
            CancellationToken ct = default)
        {
            try
            {
                if (request is null)
                    return ServiceResult<ProductDetailResponseDTO>.Fail(RequestRequiredMessage, ErrorType.Validation, ErrorCodes.Common.InvalidRequest);

                var entity = await _uow.Products.GetByIdAsync(Key(request.Id), ct);
                if (entity is null)
                    return ServiceResult<ProductDetailResponseDTO>.Fail(ProductNotFoundMessage, ErrorType.NotFound, ErrorCodes.Common.NotFound);

                if (string.IsNullOrWhiteSpace(request.Title))
                    return ServiceResult<ProductDetailResponseDTO>.Fail(TitleRequiredMessage, ErrorType.Validation, ErrorCodes.Common.InvalidRequest);

                // Update header
                entity.Title = NormalizeRequiredText(request.Title);
                entity.Description = NormalizeOptionalText(request.Description);
                if (request.IsActive.HasValue) entity.IsActive = request.IsActive.Value;
                entity.UpdatedAt = DateTime.UtcNow;
                _uow.Products.Update(entity);

                // Sync authors (if provided)
                if (request.AuthorUserIds is not null)
                    await SyncAuthorsAsync(entity.Id, request.AuthorUserIds, ct);

                // Sync values (if provided)
                if (request.Values is not null)
                {
                    // Load definitions for this product type (include ProductAttribute)
                    var defs = await _uow.ProductAttributeDefinitions
                        .QueryByType(entity.ProductTypeId, asNoTracking: true)
                        .Include(d => d.ProductAttribute)
                        .OrderBy(d => d.DisplayOrder)
                        .ToListAsync(ct);

                    var normalizedValuesResult = ValidateAndNormalizeValues(defs, request.Values);
                    if (!normalizedValuesResult.Success)
                        return ServiceResult<ProductDetailResponseDTO>.Fail(normalizedValuesResult.Message!, ErrorType.Validation, ErrorCodes.Common.InvalidRequest);

                    var normalizedValues = normalizedValuesResult.Data!; // Dictionary<int defId, string? value>

                    await SyncValuesAsync(entity.Id, normalizedValues, ct);
                }

                await _uow.SaveChangesAsync(ct);

                var withRefs = await _uow.Products.GetByIdWithRefsAsync(entity.Id, ct);
                if (withRefs is null)
                    return ServiceResult<ProductDetailResponseDTO>.Fail(ProductCouldNotLoadAfterUpdateMessage, ErrorType.Unexpected, ErrorCodes.Common.UnexpectedError);

                return ServiceResult<ProductDetailResponseDTO>.Ok(MapToDetailDTO(withRefs), ProductUpdatedMessage);
            }
            catch (DbUpdateException dbex)
            {
                return ServiceResult<ProductDetailResponseDTO>.Fail(
                    dbex.InnerException?.Message ?? dbex.Message,
                    ErrorType.Conflict, ErrorCodes.Common.PersistenceConflict);
            }
            catch (Exception ex)
            {
                return ServiceResult<ProductDetailResponseDTO>.Fail(ex.Message, ErrorType.Unexpected, ErrorCodes.Common.UnexpectedError);
            }
        }

        // ===================== DELETE =====================

        public async Task<ServiceResult<NoContent>> DeleteAsync(int id, CancellationToken ct = default)
        {
            try
            {
                var entity = await _uow.Products.GetByIdAsync(Key(id), ct);
                if (entity is null)
                    return ServiceResult<NoContent>.Fail(ProductNotFoundMessage, ErrorType.NotFound, ErrorCodes.Common.NotFound);

                // Remove children first if no cascade
                var authors = await _uow.ProductAuthors.GetByProductAsync(id, ct);
                if (authors.Count > 0) _uow.ProductAuthors.RemoveRange(authors);

                var values = await _uow.ProductValues.GetByProductAsync(id, ct);
                if (values.Count > 0) _uow.ProductValues.RemoveRange(values);

                _uow.Products.Remove(entity);
                await _uow.SaveChangesAsync(ct);

                return ServiceResult<NoContent>.Ok(new NoContent(), ProductDeletedMessage);
            }
            catch (DbUpdateException dbex)
            {
                return ServiceResult<NoContent>.Fail(
                    dbex.InnerException?.Message ?? dbex.Message,
                    ErrorType.Conflict, ErrorCodes.Common.PersistenceConflict);
            }
            catch (Exception ex)
            {
                return ServiceResult<NoContent>.Fail(ex.Message, ErrorType.Unexpected, ErrorCodes.Common.UnexpectedError);
            }
        }

        // ===================== HELPERS =====================

        private static object[] Key(int id) => new object[] { id };

        private static string NormalizeRequiredText(string value) => value.Trim();

        private static string? NormalizeOptionalText(string? value)
            => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

        private static ProductListItemResponseDTO MapToListItemDto(Product p)
            => new ProductListItemResponseDTO
            {
                Id = p.Id,
                ProjectId = p.ProjectId,
                VisitId = p.VisitId,
                Title = p.Title,
                Description = p.Description,
                ProductTypeId = p.ProductTypeId,
                ProductTypeName = p.ProductType != null ? p.ProductType.Name : string.Empty,
                IsActive = p.IsActive,
                CreatedAt = p.CreatedAt
            };

        private async Task SyncAuthorsAsync(int productId, IEnumerable<int>? authorUserIds, CancellationToken ct)
        {
            // If null => do nothing (caller decides). If empty => remove all.
            if (authorUserIds is null) return;

            var newIds = authorUserIds.Where(id => id > 0).Distinct().ToHashSet();

            var existing = await _uow.ProductAuthors.GetByProductAsync(productId, ct);
            var existingIds = existing.Select(a => a.UserId).ToHashSet();

            // remove
            foreach (var toRemove in existing.Where(a => !newIds.Contains(a.UserId)))
                _uow.ProductAuthors.Remove(toRemove);

            // add
            foreach (var uid in newIds.Where(id => !existingIds.Contains(id)))
            {
                await _uow.ProductAuthors.AddAsync(new ProductAuthor
                {
                    ProductId = productId,
                    UserId = uid,
                    CreatedAt = DateTime.UtcNow
                }, ct);
            }
        }

        private async Task SyncValuesAsync(
            int productId,
            Dictionary<int, string?> normalizedValuesByDefinitionId,
            CancellationToken ct)
        {
            // Existing values
            var existing = await _uow.ProductValues.GetByProductAsync(productId, ct);
            var existingByDefId = existing.ToDictionary(v => v.AttributeDefinitionId);

            // Upsert requested
            foreach (var kv in normalizedValuesByDefinitionId)
            {
                var defId = kv.Key;
                var value = kv.Value;

                if (!existingByDefId.TryGetValue(defId, out var current))
                {
                    await _uow.ProductValues.AddAsync(new ProductValue
                    {
                        ProductId = productId,
                        AttributeDefinitionId = defId,
                        Value = value,
                        CreatedAt = DateTime.UtcNow
                    }, ct);
                }
                else
                {
                    current.Value = value;
                    current.UpdatedAt = DateTime.UtcNow;
                    _uow.ProductValues.Update(current);
                }
            }

            // Remove values not present in request (because request is treated as "full form submission")
            var requestedDefIds = normalizedValuesByDefinitionId.Keys.ToHashSet();
            foreach (var old in existing.Where(v => !requestedDefIds.Contains(v.AttributeDefinitionId)))
                _uow.ProductValues.Remove(old);
        }

        private static ServiceResult<Dictionary<int, string?>> ValidateAndNormalizeValues(
            List<ProductAttributeDefinition> definitions,
            List<ProductValueUpsertDTO>? provided)
        {
            provided ??= new();

            // defs by id
            var defsById = definitions.ToDictionary(d => d.Id);

            // collapse duplicates (keep last)
            var providedByDefId = provided
                .Where(x => x.AttributeDefinitionId > 0)
                .GroupBy(x => x.AttributeDefinitionId)
                .ToDictionary(g => g.Key, g => g.Last());

            // required check
            foreach (var def in definitions.Where(d => d.IsRequired))
            {
                if (!providedByDefId.TryGetValue(def.Id, out var pv) || string.IsNullOrWhiteSpace(pv.Value))
                {
                    return ServiceResult<Dictionary<int, string?>>.Fail(
                        $"Required attribute value is missing (AttributeDefinitionId={def.Id}).",
                        ErrorType.Validation);
                }
            }

            // validate ownership + datatype + normalize
            var normalized = new Dictionary<int, string?>();

            foreach (var kv in providedByDefId)
            {
                var defId = kv.Key;
                var dto = kv.Value;

                if (!defsById.TryGetValue(defId, out var def))
                {
                    return ServiceResult<Dictionary<int, string?>>.Fail(
                        $"AttributeDefinitionId {defId} does not belong to the selected ProductType.",
                        ErrorType.Validation, ErrorCodes.Common.InvalidRequest);
                }

                var dt = def.ProductAttribute?.DataType ?? ProductAttributeDataType.Text;

                var check = ValidateAttributeValue(dt, dto.Value);
                if (!check.IsValid)
                {
                    return ServiceResult<Dictionary<int, string?>>.Fail(
                        $"Invalid value for AttributeDefinitionId={defId}: {check.Error}",
                        ErrorType.Validation, ErrorCodes.Common.InvalidRequest);
                }

                normalized[defId] = NormalizeValue(dt, dto.Value);
            }

            // If your UI expects ALL definitions to exist as rows (even if null),
            // you can fill missing optional definitions here. If not, keep it sparse.
            // foreach (var def in definitions)
            //     if (!normalized.ContainsKey(def.Id)) normalized[def.Id] = null;

            return ServiceResult<Dictionary<int, string?>>.Ok(normalized);
        }

        // ===================== MAPPING =====================

        private static ProductDetailResponseDTO MapToDetailDTO(Product p)
        {
            var dto = new ProductDetailResponseDTO
            {
                Id = p.Id,
                ProjectId = p.ProjectId,
                VisitId = p.VisitId,
                Title = p.Title,
                Description = p.Description,
                ProductTypeId = p.ProductTypeId,
                ProductTypeName = p.ProductType?.Name ?? string.Empty,
                IsActive = p.IsActive,
                CreatedAt = p.CreatedAt,
                UpdatedAt = p.UpdatedAt,

                Authors = (p.Authors ?? new List<ProductAuthor>())
                    .Select(a => new ProductAuthorResponseDTO
                    {
                        UserId = a.UserId,
                        CreatedAt = a.CreatedAt,
                        UpdatedAt = a.UpdatedAt
                    })
                    .ToList(),

                Values = (p.Values ?? new List<ProductValue>())
                    .OrderBy(v => v.AttributeDefinition?.DisplayOrder ?? 0)
                    .Select(v => new ProductValueResponseDTO
                    {
                        AttributeDefinitionId = v.AttributeDefinitionId,

                        // metadata from definition (and attribute if included)
                        ProductAttributeId = v.AttributeDefinition?.ProductAttributeId ?? 0,
                        ProductAttributeName = v.AttributeDefinition?.ProductAttribute?.Name ?? string.Empty,
                        DataType = MapDataTypeToString(v.AttributeDefinition?.ProductAttribute?.DataType ?? ProductAttributeDataType.Text),
                        Unit = v.AttributeDefinition?.ProductAttribute?.Unit,

                        IsRequired = v.AttributeDefinition?.IsRequired ?? false,
                        DisplayOrder = v.AttributeDefinition?.DisplayOrder ?? 0,

                        Value = v.Value,
                        CreatedAt = v.CreatedAt,
                        UpdatedAt = v.UpdatedAt
                    })
                    .ToList()
            };

            return dto;
        }

        private static string MapDataTypeToString(ProductAttributeDataType dt) => dt switch
        {
            ProductAttributeDataType.Number => "number",
            ProductAttributeDataType.Date => "date",
            ProductAttributeDataType.Url => "url",
            _ => "text"
        };

        private static string? NormalizeValue(ProductAttributeDataType dt, string? raw)
        {
            if (raw is null) return null;
            var trimmed = raw.Trim();
            if (trimmed.Length == 0) return null;

            // If later you want strict formats:
            // - Date => yyyy-MM-dd
            // - Number => invariant culture
            return trimmed;
        }

        private static (bool IsValid, string? Error) ValidateAttributeValue(ProductAttributeDataType dt, string? value)
        {
            // null/empty allowed for non-required fields; required is validated separately
            if (string.IsNullOrWhiteSpace(value)) return (true, null);

            switch (dt)
            {
                case ProductAttributeDataType.Number:
                    return (decimal.TryParse(value, out _), "Expected a numeric value.");
                case ProductAttributeDataType.Date:
                    return (DateTime.TryParse(value, out _), "Expected a valid date.");
                case ProductAttributeDataType.Url:
                    return (Uri.TryCreate(value, UriKind.Absolute, out _), "Expected a valid absolute URL.");
                case ProductAttributeDataType.Text:
                default:
                    return (value.Length <= 4000, "Text too long (max 4000).");
            }
        }
    }
}