using tesisproject.backend.Data.UnifiedEntities.Authors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using tesisproject.backend.Services.Unified.Interfaces;
using tesisproject.backend.UnitOfWork.Unified.Interfaces;
using tesisproject.shared.DTOs.Products.Product.Request;
using tesisproject.shared.DTOs.Products.Product.Response;
using tesisproject.backend.Data.UnifiedEntities.Core.Products;
using tesisproject.shared.Enums;
using tesisproject.shared.Responses;
using tesisproject.shared.Errors;

namespace tesisproject.backend.Services.Unified.Implementations
{
    public class UnifiedProductService : IUnifiedProductService
    {
        private const string ProductCreatedMessage = "Product created";
        private const string ProductRetrievedMessage = "Product retrieved";
        private const string ProductsRetrievedMessage = "Products retrieved";
        private const string ProjectProductsRetrievedMessage = "Project products retrieved";
        private const string ProductUpdatedMessage = "Product updated";
        private const string ProductDeletedMessage = "Product deleted";

        private readonly IUnifiedUnitOfWork _uow;

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

        public UnifiedProductService(IUnifiedUnitOfWork uow)
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
                if (request is null)
                {
                    return ValidationFailure<ProductDetailResponseDTO>(
                        ErrorMessages.Common.RequestRequired,
                        ErrorCodes.Common.RequestRequired);
                }

                if (request.ProjectId is <= 0)
                {
                    return ValidationFailure<ProductDetailResponseDTO>(
                        ErrorMessages.Product.ProjectIdRequired,
                        ErrorCodes.Product.ProjectIdRequired);
                }

                if (string.IsNullOrWhiteSpace(request.Title))
                {
                    return ValidationFailure<ProductDetailResponseDTO>(
                        ErrorMessages.Product.TitleRequired,
                        ErrorCodes.Product.TitleRequired);
                }

                if (request.ProductTypeId <= 0)
                {
                    return ValidationFailure<ProductDetailResponseDTO>(
                        ErrorMessages.Product.ProductTypeIdRequired,
                        ErrorCodes.Product.ProductTypeIdRequired);
                }

                if (request.ProjectId is int projectId &&
                    await _uow.Projects.GetByIdAsync(Key(projectId), ct) is null)
                    return NotFoundFailure<ProductDetailResponseDTO>(ErrorMessages.Project.NotFound, ErrorCodes.Project.NotFound);

                var type = await _uow.ProductTypes.GetByIdAsync(Key(request.ProductTypeId), ct);
                if (type is null)
                {
                    return NotFoundFailure<ProductDetailResponseDTO>(
                        ErrorMessages.Product.ProductTypeNotFound,
                        ErrorCodes.Product.ProductTypeNotFound);
                }

                var defs = await _uow.ProductAttributeDefinitions
                    .QueryByType(request.ProductTypeId, asNoTracking: true)
                    .Include(d => d.ProductAttribute)
                    .OrderBy(d => d.DisplayOrder)
                    .ToListAsync(ct);

                var normalizedValuesResult = ValidateAndNormalizeValues(defs, request.Values);
                if (!normalizedValuesResult.Success)
                    return RelayFailure<ProductDetailResponseDTO, Dictionary<int, string?>>(normalizedValuesResult);

                var normalizedValues = normalizedValuesResult.Data!;

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

                var authors = await ResolveAuthorsAsync(request.AuthorUserIds, ct);
                if (!authors.Success)
                    return RelayFailure<ProductDetailResponseDTO, List<Author>>(authors);

                entity.Values = normalizedValues.Select(kv => new ProductValue
                {
                    Product = entity,
                    AttributeDefinitionId = kv.Key,
                    Value = kv.Value,
                    CreatedAt = DateTime.UtcNow
                }).ToList();
                await SyncAuthorsAsync(entity, authors.Data!, ct);
                await _uow.Products.AddAsync(entity, ct);
                await _uow.SaveChangesAsync(ct);

                var withRefs = await _uow.Products.GetByIdWithRefsAsync(entity.Id, ct);
                if (withRefs is null)
                {
                    return UnexpectedFailure<ProductDetailResponseDTO>(
                        ErrorMessages.Product.LoadAfterCreationFailed,
                        ErrorCodes.Product.LoadAfterCreationFailed);
                }

                return ServiceResult<ProductDetailResponseDTO>.Ok(MapToDetailDTO(withRefs), ProductCreatedMessage);
            }
            catch (DbUpdateException dbex)
            {
                return FailConflict<ProductDetailResponseDTO>(dbex);
            }
            catch (Exception ex)
            {
                return FailUnexpected<ProductDetailResponseDTO>(ex);
            }
        }

        // ===================== READ ONE =====================

        public async Task<ServiceResult<ProductDetailResponseDTO>> GetByIdAsync(int id, CancellationToken ct = default)
        {
            try
            {
                var prod = await _uow.Products.GetByIdWithRefsAsync(id, ct);
                if (prod is null)
                {
                    return NotFoundFailure<ProductDetailResponseDTO>(
                        ErrorMessages.Product.NotFound,
                        ErrorCodes.Product.NotFound);
                }

                return ServiceResult<ProductDetailResponseDTO>.Ok(MapToDetailDTO(prod), ProductRetrievedMessage);
            }
            catch (Exception ex)
            {
                return FailUnexpected<ProductDetailResponseDTO>(ex);
            }
        }

        // ===================== LIST =====================

        public async Task<ServiceResult<IReadOnlyList<ProductListItemResponseDTO>>> ListAsync(CancellationToken ct = default)
        {
            try
            {
                var q = _uow.Products.QueryWithRefs();

                var items = await q
                    .OrderByDescending(p => p.CreatedAt)
                    .Select(MapToListItemExpression)
                    .ToListAsync(ct);

                if (items.Count == 0)
                {
                    return NotFoundFailure<IReadOnlyList<ProductListItemResponseDTO>>(
                        ErrorMessages.Product.NoneFound,
                        ErrorCodes.Product.NoneFound);
                }

                return ServiceResult<IReadOnlyList<ProductListItemResponseDTO>>.Ok(items, ProductsRetrievedMessage);
            }
            catch (Exception ex)
            {
                return FailUnexpected<IReadOnlyList<ProductListItemResponseDTO>>(ex);
            }
        }

        public async Task<ServiceResult<IReadOnlyList<ProductListItemResponseDTO>>> ListByProjectAsync(
            int projectId,
            CancellationToken ct = default)
        {
            try
            {
                if (projectId <= 0)
                {
                    return ValidationFailure<IReadOnlyList<ProductListItemResponseDTO>>(
                        ErrorMessages.Product.ProjectIdRequired,
                        ErrorCodes.Product.ProjectIdRequired);
                }

                var entities = await _uow.Products.GetByProjectAsync(projectId, ct);
                if (entities.Count == 0)
                {
                    return NotFoundFailure<IReadOnlyList<ProductListItemResponseDTO>>(
                        ErrorMessages.Product.NoneFoundForProject,
                        ErrorCodes.Product.NoneFoundForProject);
                }

                var dtos = entities
                    .OrderByDescending(p => p.CreatedAt)
                    .Select(MapToListItemDto)
                    .ToList();

                return ServiceResult<IReadOnlyList<ProductListItemResponseDTO>>.Ok(dtos, ProjectProductsRetrievedMessage);
            }
            catch (Exception ex)
            {
                return FailUnexpected<IReadOnlyList<ProductListItemResponseDTO>>(ex);
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
                {
                    return ValidationFailure<ProductDetailResponseDTO>(
                        ErrorMessages.Common.RequestRequired,
                        ErrorCodes.Common.RequestRequired);
                }

                var entity = await _uow.Products.GetByIdWithRefsAsync(request.Id, ct);
                if (entity is null)
                {
                    return NotFoundFailure<ProductDetailResponseDTO>(
                        ErrorMessages.Product.NotFound,
                        ErrorCodes.Product.NotFound);
                }

                if (string.IsNullOrWhiteSpace(request.Title))
                {
                    return ValidationFailure<ProductDetailResponseDTO>(
                        ErrorMessages.Product.TitleRequired,
                        ErrorCodes.Product.TitleRequired);
                }

                Dictionary<int, string?>? normalizedValues = null;
                if (request.Values is not null)
                {
                    var defs = await _uow.ProductAttributeDefinitions
                        .QueryByType(entity.ProductTypeId, asNoTracking: true)
                        .Include(d => d.ProductAttribute)
                        .OrderBy(d => d.DisplayOrder)
                        .ToListAsync(ct);
                    var values = ValidateAndNormalizeValues(defs, request.Values);
                    if (!values.Success)
                        return RelayFailure<ProductDetailResponseDTO, Dictionary<int, string?>>(values);
                    normalizedValues = values.Data!;
                }

                var authors = await ResolveAuthorsAsync(request.AuthorUserIds, ct);
                if (!authors.Success)
                    return RelayFailure<ProductDetailResponseDTO, List<Author>>(authors);

                if (request.AuthorUserIds is not null)
                    await SyncAuthorsAsync(entity, authors.Data!, ct);
                if (normalizedValues is not null)
                    await SyncValuesAsync(entity, normalizedValues, ct);

                entity.Title = NormalizeRequiredText(request.Title);
                entity.Description = NormalizeOptionalText(request.Description);
                if (request.IsActive.HasValue)
                    entity.IsActive = request.IsActive.Value;
                entity.UpdatedAt = DateTime.UtcNow;
                // GetByIdWithRefsAsync returns a tracked aggregate; do not Update its whole graph.

                await _uow.SaveChangesAsync(ct);

                var withRefs = await _uow.Products.GetByIdWithRefsAsync(entity.Id, ct);
                if (withRefs is null)
                {
                    return UnexpectedFailure<ProductDetailResponseDTO>(
                        ErrorMessages.Product.LoadAfterUpdateFailed,
                        ErrorCodes.Product.LoadAfterUpdateFailed);
                }

                return ServiceResult<ProductDetailResponseDTO>.Ok(MapToDetailDTO(withRefs), ProductUpdatedMessage);
            }
            catch (DbUpdateException dbex)
            {
                return FailConflict<ProductDetailResponseDTO>(dbex);
            }
            catch (Exception ex)
            {
                return FailUnexpected<ProductDetailResponseDTO>(ex);
            }
        }

        // ===================== DELETE =====================

        public async Task<ServiceResult<NoContent>> DeleteAsync(int id, CancellationToken ct = default)
        {
            try
            {
                var entity = await _uow.Products.GetByIdWithRefsAsync(id, ct);
                if (entity is null)
                {
                    return NotFoundFailure<NoContent>(
                        ErrorMessages.Product.NotFound,
                        ErrorCodes.Product.NotFound);
                }

                var authors = entity.Authors?.ToList() ?? [];
                if (authors.Count > 0)
                    _uow.ProductAuthors.RemoveRange(authors);

                var values = entity.Values?.ToList() ?? [];
                if (values.Count > 0)
                    _uow.ProductValues.RemoveRange(values);

                _uow.Products.Remove(entity);
                await _uow.SaveChangesAsync(ct);

                return ServiceResult<NoContent>.Ok(new NoContent(), ProductDeletedMessage);
            }
            catch (DbUpdateException dbex)
            {
                return FailConflict<NoContent>(dbex);
            }
            catch (Exception ex)
            {
                return FailUnexpected<NoContent>(ex);
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

        private async Task<ServiceResult<List<Author>>> ResolveAuthorsAsync(
            IEnumerable<int>? authorUserIds, CancellationToken ct)
        {
            // Legacy ignores nonpositive IDs, collapses duplicates, and treats null as no change.
            var userIds = (authorUserIds ?? []).Where(id => id > 0).Distinct().ToList();
            foreach (var userId in userIds)
                if (await _uow.AppUsers.GetByIdAsync(Key(userId), ct) is null)
                    return NotFoundFailure<List<Author>>(ErrorMessages.AppUser.NotFound, ErrorCodes.AppUser.NotFound);

            var authors = new List<Author>();
            foreach (var userId in userIds)
            {
                var author = await _uow.Authors.GetByAppUserIdAsync(userId, ct);
                // Recheck after validating the source, immediately before preparing a new identity.
                author ??= await _uow.Authors.GetByAppUserIdAsync(userId, ct);
                if (author is not null && (author.AppUserId != userId || author.ExternalResearcherId.HasValue))
                    return ServiceResult<List<Author>>.Fail(ErrorMessages.Author.ExactlyOneSource,
                        ErrorType.Validation, ErrorCodes.Author.ExactlyOneSource,
                        new Dictionary<string, string[]> { [nameof(ProductCreateRequestDTO.AuthorUserIds)] = [ErrorMessages.Author.ExactlyOneSource] });
                authors.Add(author ?? new Author { AppUserId = userId, ExternalResearcherId = null });
            }
            return ServiceResult<List<Author>>.Ok(authors);
        }

        private async Task SyncAuthorsAsync(Product product, List<Author> desired, CancellationToken ct)
        {
            product.Authors ??= new List<ProductAuthor>();
            var existingIds = product.Authors.Select(link => link.AuthorId).ToHashSet();
            var desiredIds = desired.Where(author => author.AuthorId > 0).Select(author => author.AuthorId).ToHashSet();
            // A supplied list replaces the authorship set; null is handled by the owner.
            foreach (var removed in product.Authors.Where(link => !desiredIds.Contains(link.AuthorId)).ToList())
            {
                _uow.ProductAuthors.Remove(removed);
                product.Authors.Remove(removed);
            }
            foreach (var author in desired)
            {
                var isNew = author.AuthorId == 0;
                if (!isNew && existingIds.Contains(author.AuthorId)) continue;
                if (isNew) await _uow.Authors.AddAsync(author, ct);
                var link = new ProductAuthor { Product = product, AuthorId = author.AuthorId };
                // Only new Authors use a navigation. Existing source lookups are detached;
                // passing those instances to Add would attempt to insert existing people again.
                if (isNew) link.Author = author;
                product.Authors.Add(link);
                if (product.Id != 0) await _uow.ProductAuthors.AddAsync(link, ct);
            }
        }

        private async Task SyncValuesAsync(
            Product product,
            Dictionary<int, string?> normalizedValuesByDefinitionId,
            CancellationToken ct)
        {
            product.Values ??= new List<ProductValue>();
            var existing = product.Values.ToList();
            var existingByDefId = existing.ToDictionary(v => v.AttributeDefinitionId);

            foreach (var kv in normalizedValuesByDefinitionId)
            {
                var defId = kv.Key;
                var value = kv.Value;

                if (!existingByDefId.TryGetValue(defId, out var current))
                {
                    var added = new ProductValue
                    {
                        Product = product,
                        AttributeDefinitionId = defId,
                        Value = value,
                        CreatedAt = DateTime.UtcNow
                    };
                    product.Values.Add(added);
                    await _uow.ProductValues.AddAsync(added, ct);
                }
                else
                {
                    current.Value = value;
                    current.UpdatedAt = DateTime.UtcNow;
                    // Existing values are tracked with the aggregate.
                }
            }

            var requestedDefIds = normalizedValuesByDefinitionId.Keys.ToHashSet();
            foreach (var old in existing.Where(v => !requestedDefIds.Contains(v.AttributeDefinitionId)))
            {
                _uow.ProductValues.Remove(old);
                product.Values.Remove(old);
            }
        }

        private static ServiceResult<Dictionary<int, string?>> ValidateAndNormalizeValues(
            List<ProductAttributeDefinition> definitions,
            List<ProductValueUpsertDTO>? provided)
        {
            provided ??= new();

            var defsById = definitions.ToDictionary(d => d.Id);

            var providedByDefId = provided
                .Where(x => x.AttributeDefinitionId > 0)
                .GroupBy(x => x.AttributeDefinitionId)
                .ToDictionary(g => g.Key, g => g.Last());

            foreach (var def in definitions.Where(d => d.IsRequired))
            {
                if (!providedByDefId.TryGetValue(def.Id, out var pv) || string.IsNullOrWhiteSpace(pv.Value))
                {
                    return ValidationFailure<Dictionary<int, string?>>(
                        string.Format(ErrorMessages.Product.RequiredAttributeValueMissing, def.Id),
                        ErrorCodes.Product.RequiredAttributeValueMissing);
                }
            }

            var normalized = new Dictionary<int, string?>();

            foreach (var kv in providedByDefId)
            {
                var defId = kv.Key;
                var dto = kv.Value;

                if (!defsById.TryGetValue(defId, out var def))
                {
                    return ValidationFailure<Dictionary<int, string?>>(
                        string.Format(ErrorMessages.Product.AttributeDefinitionDoesNotBelongToProductType, defId),
                        ErrorCodes.Product.AttributeDefinitionMismatch);
                }

                var dt = def.ProductAttribute?.DataType ?? ProductAttributeDataType.Text;

                var check = ValidateAttributeValue(dt, dto.Value);
                if (!check.IsValid)
                {
                    return ValidationFailure<Dictionary<int, string?>>(
                        string.Format(
                            ErrorMessages.Product.InvalidAttributeValue,
                            defId,
                            check.Error ?? ErrorMessages.Common.InvalidRequest),
                        ErrorCodes.Product.InvalidAttributeValue);
                }

                normalized[defId] = NormalizeValue(dt, dto.Value);
            }

            return ServiceResult<Dictionary<int, string?>>.Ok(normalized);
        }

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
                    .OrderBy(a => a.AuthorOrder ?? int.MaxValue)
                    .ThenBy(a => a.Id)
                    .Select(a => new ProductAuthorResponseDTO
                    {
                        AuthorId = a.AuthorId,
                        AuthorType = a.Author.AppUserId.HasValue ? ProductAuthorType.Institutional : ProductAuthorType.External,
                        AppUserId = a.Author.AppUserId,
                        ExternalResearcherId = a.Author.ExternalResearcherId,
                        AuthorOrder = a.AuthorOrder,
                        Participation = a.Participation,
                        IsPrimaryAuthor = a.IsPrimaryAuthor,
                        ParticipantTypeSnapshot = a.ParticipantTypeSnapshot,
                        AffiliationSnapshot = a.AffiliationSnapshot,
                        NameSnapshot = a.NameSnapshot,
                        IdentificationSnapshot = a.IdentificationSnapshot,
                        EmailSnapshot = a.EmailSnapshot,
                        CreatedAt = a.CreatedAt,
                        UpdatedAt = a.UpdatedAt
                    })
                    .ToList(),

                Values = (p.Values ?? new List<ProductValue>())
                    .OrderBy(v => v.AttributeDefinition?.DisplayOrder ?? 0)
                    .Select(v => new ProductValueResponseDTO
                    {
                        AttributeDefinitionId = v.AttributeDefinitionId,
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

            return trimmed;
        }

        private static (bool IsValid, string? Error) ValidateAttributeValue(ProductAttributeDataType dt, string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return (true, null);

            switch (dt)
            {
                case ProductAttributeDataType.Number:
                    return (decimal.TryParse(value, out _), ErrorMessages.Product.ExpectedNumericValue);

                case ProductAttributeDataType.Date:
                    return (DateTime.TryParse(value, out _), ErrorMessages.Product.ExpectedValidDate);

                case ProductAttributeDataType.Url:
                    return (Uri.TryCreate(value, UriKind.Absolute, out _), ErrorMessages.Product.ExpectedAbsoluteUrl);

                case ProductAttributeDataType.Text:
                default:
                    return (value.Length <= 4000, ErrorMessages.Product.TextTooLong);
            }
        }

        private static ServiceResult<T> ValidationFailure<T>(string message, string errorCode)
            => ServiceResult<T>.Fail(message, ErrorType.Validation, errorCode);

        private static ServiceResult<T> NotFoundFailure<T>(string message, string errorCode)
            => ServiceResult<T>.Fail(message, ErrorType.NotFound, errorCode);

        private static ServiceResult<T> UnexpectedFailure<T>(string message, string errorCode)
            => ServiceResult<T>.Fail(message, ErrorType.Unexpected, errorCode);

        private static ServiceResult<TTarget> RelayFailure<TTarget, TSource>(ServiceResult<TSource> source)
        {
            var error = source.Error == ErrorType.None
                ? ErrorType.Unexpected
                : source.Error;

            return ServiceResult<TTarget>.Fail(
                source.Message ?? ErrorMessages.Common.UnexpectedError,
                error,
                source.ErrorCode ?? (error == ErrorType.Unexpected ? ErrorCodes.Common.UnexpectedError : null),
                source.ValidationErrors);
        }

        private static ServiceResult<T> FailConflict<T>(DbUpdateException dbex)
            => ServiceResult<T>.Fail(
                dbex.InnerException?.Message ?? dbex.Message,
                ErrorType.Conflict,
                ErrorCodes.Common.PersistenceConflict);

        private static ServiceResult<T> FailUnexpected<T>(Exception ex)
            => ServiceResult<T>.Fail(
                ex.Message,
                ErrorType.Unexpected,
                ErrorCodes.Common.UnexpectedError);
    }
}
