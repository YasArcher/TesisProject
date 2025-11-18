using Microsoft.EntityFrameworkCore;
using tesisproject.backend.Services.Interfaces;
using tesisproject.backend.UnitOfWork.Interfaces;
using tesisproject.shared.DTOs.Product.Request;
using tesisproject.shared.DTOs.Product.Response;
using tesisproject.shared.Entities.Core.Products;
using tesisproject.shared.Enums;
using tesisproject.shared.Responses;

namespace tesisproject.backend.Services.Implementations
{
    public class ProductService : IProductService
    {
        private readonly IUnitOfWork _uow;

        public ProductService(IUnitOfWork uow)
        {
            _uow = uow;
        }

        // ===================== CREATE =====================

        public async Task<ServiceResult<ProductDetailResponseDTO>> CreateAsync(ProductCreateRequestDTO request, CancellationToken ct = default)
        {
            try
            {
                // Basic validations
                if (request.ProjectId <= 0)
                    return ServiceResult<ProductDetailResponseDTO>.Fail("ProjectId is required.", ErrorType.Validation);
                if (string.IsNullOrWhiteSpace(request.Title))
                    return ServiceResult<ProductDetailResponseDTO>.Fail("Title is required.", ErrorType.Validation);
                if (request.ProductTypeId <= 0)
                    return ServiceResult<ProductDetailResponseDTO>.Fail("ProductTypeId is required.", ErrorType.Validation);

                // Type & definitions
                var type = await _uow.ProductTypes.GetByIdAsync(new object[] { request.ProductTypeId }, ct);
                if (type is null)
                    return ServiceResult<ProductDetailResponseDTO>.Fail("ProductType not found.", ErrorType.NotFound);

                var defs = await _uow.ProductAttributeDefinitions.GetByTypeAsync(request.ProductTypeId, ct);
                var defsById = defs.ToDictionary(d => d.Id, d => d);

                // Validate attribute values presence/ownership
                var providedValues = request.AttributeValues ?? new List<ProductAttributeValueUpsertDTO>();
                var providedById = providedValues.ToDictionary(v => v.AttributeDefinitionId, v => v);

                // Required attributes must be present
                foreach (var def in defs.Where(d => d.IsRequired))
                {
                    if (!providedById.ContainsKey(def.Id))
                        return ServiceResult<ProductDetailResponseDTO>.Fail($"Required attribute '{def.AttributeName}' is missing.", ErrorType.Validation);
                }

                // Each provided value must belong to the same ProductType
                foreach (var av in providedValues)
                {
                    if (!defsById.TryGetValue(av.AttributeDefinitionId, out var def))
                        return ServiceResult<ProductDetailResponseDTO>.Fail($"AttributeDefinitionId {av.AttributeDefinitionId} does not belong to ProductType {request.ProductTypeId}.", ErrorType.Validation);

                    var valCheck = ValidateAttributeValue(def.DataType, av.Value);
                    if (!valCheck.IsValid)
                        return ServiceResult<ProductDetailResponseDTO>.Fail($"Invalid value for '{def.AttributeName}': {valCheck.Error}.", ErrorType.Validation);
                }

                var entity = new Product
                {
                    ProjectId = request.ProjectId,
                    VisitId = request.VisitId,
                    Title = request.Title.Trim(),
                    Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
                    ProductTypeId = request.ProductTypeId,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                await _uow.Products.AddAsync(entity, ct);
                await _uow.SaveChangesAsync(ct); // need Id

                // Authors (avoid duplicates)
                var uniqueAuthorIds = (request.AuthorUserIds ?? new List<int>()).Where(id => id > 0).Distinct().ToList();
                foreach (var userId in uniqueAuthorIds)
                {
                    var exists = await _uow.ProductAuthors.ExistsForUserAsync(entity.Id, userId, ct);
                    if (!exists)
                    {
                        await _uow.ProductAuthors.AddAsync(new ProductAuthor
                        {
                            ProductId = entity.Id,
                            UserId = userId
                        }, ct);
                    }
                }

                // Values
                foreach (var av in providedValues)
                {
                    await _uow.ProductValues.AddAsync(new ProductValue
                    {
                        ProductId = entity.Id,
                        AttributeDefinitionId = av.AttributeDefinitionId,
                        Value = NormalizeValue(defsById[av.AttributeDefinitionId].DataType, av.Value)
                    }, ct);
                }

                await _uow.SaveChangesAsync(ct);

                var withRefs = await _uow.Products.GetByIdWithRefsAsync(entity.Id, ct);
                if (withRefs is null)
                    return ServiceResult<ProductDetailResponseDTO>.Fail("Product could not be loaded after creation.", ErrorType.Unexpected);

                return ServiceResult<ProductDetailResponseDTO>.Ok(MapToDetailDTO(withRefs), "Product created");
            }
            catch (DbUpdateException dbex)
            {
                return ServiceResult<ProductDetailResponseDTO>.Fail(dbex.InnerException?.Message ?? dbex.Message, ErrorType.Conflict);
            }
            catch (Exception ex)
            {
                return ServiceResult<ProductDetailResponseDTO>.Fail(ex.Message, ErrorType.Unexpected);
            }
        }

        // ===================== READ ONE =====================

        public async Task<ServiceResult<ProductDetailResponseDTO>> GetByIdAsync(int id, CancellationToken ct = default)
        {
            try
            {
                var prod = await _uow.Products.GetByIdWithRefsAsync(id, ct);
                if (prod is null)
                    return ServiceResult<ProductDetailResponseDTO>.Fail("Product not found.", ErrorType.NotFound);

                return ServiceResult<ProductDetailResponseDTO>.Ok(MapToDetailDTO(prod), "Product retrieved");
            }
            catch (Exception ex)
            {
                return ServiceResult<ProductDetailResponseDTO>.Fail(ex.Message, ErrorType.Unexpected);
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
                    .Select(p => new ProductListItemResponseDTO
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
                    })
                    .ToListAsync(ct);

                if (items.Count == 0)
                    return ServiceResult<IReadOnlyList<ProductListItemResponseDTO>>.Fail("No products found.", ErrorType.NotFound);

                return ServiceResult<IReadOnlyList<ProductListItemResponseDTO>>.Ok(items, "Products retrieved");
            }
            catch (Exception ex)
            {
                return ServiceResult<IReadOnlyList<ProductListItemResponseDTO>>.Fail(ex.Message, ErrorType.Unexpected);
            }
        }

        public async Task<ServiceResult<IReadOnlyList<ProductListItemResponseDTO>>> ListByProjectAsync(int projectId, CancellationToken ct = default)
        {
            try
            {
                if (projectId <= 0)
                    return ServiceResult<IReadOnlyList<ProductListItemResponseDTO>>.Fail("projectId is required.", ErrorType.Validation);

                var entities = await _uow.Products.GetByProjectAsync(projectId, ct); // includes ProductType
                if (entities.Count == 0)
                    return ServiceResult<IReadOnlyList<ProductListItemResponseDTO>>.Fail("No products found for this project.", ErrorType.NotFound);

                var dtos = entities
                    .OrderByDescending(p => p.CreatedAt)
                    .Select(p => new ProductListItemResponseDTO
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
                    })
                    .ToList();

                return ServiceResult<IReadOnlyList<ProductListItemResponseDTO>>.Ok(dtos, "Project products retrieved");
            }
            catch (Exception ex)
            {
                return ServiceResult<IReadOnlyList<ProductListItemResponseDTO>>.Fail(ex.Message, ErrorType.Unexpected);
            }
        }

        // ===================== UPDATE =====================

        public async Task<ServiceResult<ProductDetailResponseDTO>> UpdateAsync(ProductUpdateRequestDTO request, CancellationToken ct = default)
        {
            try
            {
                var entity = await _uow.Products.GetByIdAsync(new object[] { request.Id }, ct);
                if (entity is null)
                    return ServiceResult<ProductDetailResponseDTO>.Fail("Product not found.", ErrorType.NotFound);

                if (string.IsNullOrWhiteSpace(request.Title))
                    return ServiceResult<ProductDetailResponseDTO>.Fail("Title is required.", ErrorType.Validation);

                // Update core
                entity.Title = request.Title.Trim();
                entity.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
                if (request.IsActive.HasValue) entity.IsActive = request.IsActive.Value;
                entity.UpdatedAt = DateTime.UtcNow;
                _uow.Products.Update(entity);

                // Sync authors (if provided)
                if (request.AuthorUserIds is not null)
                {
                    var existing = await _uow.ProductAuthors.GetByProductAsync(entity.Id, ct);
                    var existingIds = existing.Select(a => a.UserId).ToHashSet();
                    var newIds = request.AuthorUserIds.Where(id => id > 0).Distinct().ToHashSet();

                    // remove
                    foreach (var toRemove in existing.Where(a => !newIds.Contains(a.UserId)))
                        _uow.ProductAuthors.Remove(toRemove);

                    // add
                    foreach (var uid in newIds.Where(id => !existingIds.Contains(id)))
                    {
                        await _uow.ProductAuthors.AddAsync(new ProductAuthor
                        {
                            ProductId = entity.Id,
                            UserId = uid
                        }, ct);
                    }
                }

                // Upsert attribute values (if provided)
                if (request.AttributeValues is not null && request.AttributeValues.Count > 0)
                {
                    // Load definitions for product's type
                    var defs = await _uow.ProductAttributeDefinitions.GetByTypeAsync(entity.ProductTypeId, ct);
                    var defsById = defs.ToDictionary(d => d.Id, d => d);

                    foreach (var av in request.AttributeValues)
                    {
                        if (!defsById.TryGetValue(av.AttributeDefinitionId, out var def))
                            return ServiceResult<ProductDetailResponseDTO>.Fail($"AttributeDefinitionId {av.AttributeDefinitionId} does not belong to ProductType {entity.ProductTypeId}.", ErrorType.Validation);

                        var valCheck = ValidateAttributeValue(def.DataType, av.Value);
                        if (!valCheck.IsValid)
                            return ServiceResult<ProductDetailResponseDTO>.Fail($"Invalid value for '{def.AttributeName}': {valCheck.Error}.", ErrorType.Validation);

                        var existing = await _uow.ProductValues.GetByPairAsync(entity.Id, av.AttributeDefinitionId, ct);
                        if (existing is null)
                        {
                            await _uow.ProductValues.AddAsync(new ProductValue
                            {
                                ProductId = entity.Id,
                                AttributeDefinitionId = av.AttributeDefinitionId,
                                Value = NormalizeValue(def.DataType, av.Value)
                            }, ct);
                        }
                        else
                        {
                            existing.Value = NormalizeValue(def.DataType, av.Value);
                            existing.UpdatedAt = DateTime.UtcNow;
                            _uow.ProductValues.Update(existing);
                        }
                    }
                }

                await _uow.SaveChangesAsync(ct);

                var withRefs = await _uow.Products.GetByIdWithRefsAsync(entity.Id, ct);
                if (withRefs is null)
                    return ServiceResult<ProductDetailResponseDTO>.Fail("Product could not be loaded after update.", ErrorType.Unexpected);

                return ServiceResult<ProductDetailResponseDTO>.Ok(MapToDetailDTO(withRefs), "Product updated");
            }
            catch (DbUpdateException dbex)
            {
                return ServiceResult<ProductDetailResponseDTO>.Fail(dbex.InnerException?.Message ?? dbex.Message, ErrorType.Conflict);
            }
            catch (Exception ex)
            {
                return ServiceResult<ProductDetailResponseDTO>.Fail(ex.Message, ErrorType.Unexpected);
            }
        }

        // ===================== DELETE =====================

        public async Task<ServiceResult<NoContent>> DeleteAsync(int id, CancellationToken ct = default)
        {
            try
            {
                var entity = await _uow.Products.GetByIdAsync(new object[] { id }, ct);
                if (entity is null)
                    return ServiceResult<NoContent>.Fail("Product not found.", ErrorType.NotFound);

                // Remove children first if no cascade
                var authors = await _uow.ProductAuthors.GetByProductAsync(id, ct);
                if (authors.Count > 0) _uow.ProductAuthors.RemoveRange(authors);

                var values = await _uow.ProductValues.GetByProductAsync(id, ct);
                if (values.Count > 0) _uow.ProductValues.RemoveRange(values);

                _uow.Products.Remove(entity);
                await _uow.SaveChangesAsync(ct);

                return ServiceResult<NoContent>.Ok(new NoContent(), "Product deleted");
            }
            catch (DbUpdateException dbex)
            {
                return ServiceResult<NoContent>.Fail(dbex.InnerException?.Message ?? dbex.Message, ErrorType.Conflict);
            }
            catch (Exception ex)
            {
                return ServiceResult<NoContent>.Fail(ex.Message, ErrorType.Unexpected);
            }
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
                    .OrderBy(v => v.AttributeDefinition!.DisplayOrder)
                    .Select(v => new ProductValueResponseDTO
                    {
                        AttributeDefinitionId = v.AttributeDefinitionId,
                        AttributeName = v.AttributeDefinition?.AttributeName ?? string.Empty,
                        DataType = MapDataTypeToString(v.AttributeDefinition?.DataType ?? ProductAttributeDataType.Text),
                        IsRequired = v.AttributeDefinition?.IsRequired ?? false,
                        DisplayOrder = v.AttributeDefinition?.DisplayOrder ?? 0,
                        Unit = v.AttributeDefinition?.Unit,
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

            // Keep normalization simple: just trim; conversions/format can be added if needed
            return trimmed;
        }

        private static (bool IsValid, string? Error) ValidateAttributeValue(ProductAttributeDataType dt, string? value)
        {
            // Null allowed unless the attribute is required; the required-ness is validated separately
            if (value is null) return (true, null);

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