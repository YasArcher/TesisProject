# Referencia legacy — ProductService

## TODO UNIFIED: CreateAsync

AuthorUserIds/UserId no representan AuthorId ni autores externos. Decidir creación automática de Author; Authors.GetByAppUserIdAsync/GetByExternalResearcherIdAsync y ProductAuthors.ExistsForAuthorAsync ya existen. El detalle requiere DTO con discriminador y fuente externa.

Referencia exacta: `tesisproject.backend/Services/Implementations/ProductService.cs:48-149`. Firma omitida temporalmente del contrato Unified.

```csharp
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

                if (request.ProjectId <= 0)
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

                await _uow.Products.AddAsync(entity, ct);
                await _uow.SaveChangesAsync(ct);

                await SyncAuthorsAsync(entity.Id, request.AuthorUserIds, ct);

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
```

## TODO UNIFIED: DeleteAsync

Depende de GetByIdAsync. Ver decisión en este informe.

Referencia exacta: `tesisproject.backend/Services/Implementations/ProductService.cs:317-350`. Firma omitida temporalmente del contrato Unified.

```csharp
        public async Task<ServiceResult<NoContent>> DeleteAsync(int id, CancellationToken ct = default)
        {
            try
            {
                var entity = await _uow.Products.GetByIdAsync(Key(id), ct);
                if (entity is null)
                {
                    return NotFoundFailure<NoContent>(
                        ErrorMessages.Product.NotFound,
                        ErrorCodes.Product.NotFound);
                }

                var authors = await _uow.ProductAuthors.GetByProductAsync(id, ct);
                if (authors.Count > 0)
                    _uow.ProductAuthors.RemoveRange(authors);

                var values = await _uow.ProductValues.GetByProductAsync(id, ct);
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
```

## TODO UNIFIED: GetByIdAsync

AuthorUserIds/UserId no representan AuthorId ni autores externos. Decidir creación automática de Author; Authors.GetByAppUserIdAsync/GetByExternalResearcherIdAsync y ProductAuthors.ExistsForAuthorAsync ya existen. El detalle requiere DTO con discriminador y fuente externa.

Referencia exacta: `tesisproject.backend/Services/Implementations/ProductService.cs:153-171`. Firma omitida temporalmente del contrato Unified.

```csharp
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
```

## TODO UNIFIED: ListAsync

ProductListItemResponseDTO.ProjectId es int; decidir contrato nullable para producción independiente.

Referencia exacta: `tesisproject.backend/Services/Implementations/ProductService.cs:175-199`. Firma omitida temporalmente del contrato Unified.

```csharp
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
```

## TODO UNIFIED: MapToDetailDTO

AuthorUserIds/UserId no representan AuthorId ni autores externos. Decidir creación automática de Author; Authors.GetByAppUserIdAsync/GetByExternalResearcherIdAsync y ProductAuthors.ExistsForAuthorAsync ya existen. El detalle requiere DTO con discriminador y fuente externa.

Referencia exacta: `tesisproject.backend/Services/Implementations/ProductService.cs:491-534`. Firma omitida temporalmente del contrato Unified.

```csharp
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
```

## TODO UNIFIED: SyncAuthorsAsync

AuthorUserIds/UserId no representan AuthorId ni autores externos. Decidir creación automática de Author; Authors.GetByAppUserIdAsync/GetByExternalResearcherIdAsync y ProductAuthors.ExistsForAuthorAsync ya existen. El detalle requiere DTO con discriminador y fuente externa.

Referencia exacta: `tesisproject.backend/Services/Implementations/ProductService.cs:375-397`. Firma omitida temporalmente del contrato Unified.

```csharp
        private async Task SyncAuthorsAsync(int productId, IEnumerable<int>? authorUserIds, CancellationToken ct)
        {
            if (authorUserIds is null)
                return;

            var newIds = authorUserIds.Where(id => id > 0).Distinct().ToHashSet();

            var existing = await _uow.ProductAuthors.GetByProductAsync(productId, ct);
            var existingIds = existing.Select(a => a.UserId).ToHashSet();

            foreach (var toRemove in existing.Where(a => !newIds.Contains(a.UserId)))
                _uow.ProductAuthors.Remove(toRemove);

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
```

## TODO UNIFIED: UpdateAsync

AuthorUserIds/UserId no representan AuthorId ni autores externos. Decidir creación automática de Author; Authors.GetByAppUserIdAsync/GetByExternalResearcherIdAsync y ProductAuthors.ExistsForAuthorAsync ya existen. El detalle requiere DTO con discriminador y fuente externa.

Referencia exacta: `tesisproject.backend/Services/Implementations/ProductService.cs:237-313`. Firma omitida temporalmente del contrato Unified.

```csharp
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

                var entity = await _uow.Products.GetByIdAsync(Key(request.Id), ct);
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

                entity.Title = NormalizeRequiredText(request.Title);
                entity.Description = NormalizeOptionalText(request.Description);

                if (request.IsActive.HasValue)
                    entity.IsActive = request.IsActive.Value;

                entity.UpdatedAt = DateTime.UtcNow;
                _uow.Products.Update(entity);

                if (request.AuthorUserIds is not null)
                    await SyncAuthorsAsync(entity.Id, request.AuthorUserIds, ct);

                if (request.Values is not null)
                {
                    var defs = await _uow.ProductAttributeDefinitions
                        .QueryByType(entity.ProductTypeId, asNoTracking: true)
                        .Include(d => d.ProductAttribute)
                        .OrderBy(d => d.DisplayOrder)
                        .ToListAsync(ct);

                    var normalizedValuesResult = ValidateAndNormalizeValues(defs, request.Values);
                    if (!normalizedValuesResult.Success)
                        return RelayFailure<ProductDetailResponseDTO, Dictionary<int, string?>>(normalizedValuesResult);

                    var normalizedValues = normalizedValuesResult.Data!;
                    await SyncValuesAsync(entity.Id, normalizedValues, ct);
                }

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
```
