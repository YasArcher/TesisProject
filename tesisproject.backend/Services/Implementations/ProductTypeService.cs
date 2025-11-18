using Microsoft.EntityFrameworkCore;
using tesisproject.backend.Services.Interfaces;
using tesisproject.backend.UnitOfWork.Interfaces;
using tesisproject.shared.DTOs.Catalog.ProductType.Request;
using tesisproject.shared.DTOs.Catalog.ProductType.Response;
using tesisproject.shared.DTOs.ProductAttributeDefinition.Response;
using tesisproject.shared.Entities.Catalogs;
using tesisproject.shared.Entities.Core.Products;
using tesisproject.shared.Responses;

namespace tesisproject.backend.Services.Implementations
{
    public class ProductTypeService : IProductTypeService
    {
        private readonly IUnitOfWork _uow;

        public ProductTypeService(IUnitOfWork uow)
        {
            _uow = uow;
        }

        // ===================== CREATE (Type only) =====================

        public async Task<ServiceResult<ProductTypeListItemResponseDTO>> CreateAsync(ProductTypeCreateRequestDTO request, CancellationToken ct = default)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Name))
                    return ServiceResult<ProductTypeListItemResponseDTO>.Fail("Name is required.", ErrorType.Validation);

                var existsName = await _uow.ProductTypes.ExistsNameAsync(request.Name, null, ct);
                if (existsName)
                    return ServiceResult<ProductTypeListItemResponseDTO>.Fail("A ProductType with the same Name already exists.", ErrorType.Conflict);

                var entity = new ProductType
                {
                    Name = request.Name.Trim(),
                    IsActive = request.IsActive
                };

                await _uow.ProductTypes.AddAsync(entity, ct);
                await _uow.SaveChangesAsync(ct);

                var dto = new ProductTypeListItemResponseDTO
                {
                    Id = entity.Id,
                    Name = entity.Name,
                    IsActive = entity.IsActive
                };

                return ServiceResult<ProductTypeListItemResponseDTO>.Ok(dto, "ProductType created");
            }
            catch (DbUpdateException dbex)
            {
                return ServiceResult<ProductTypeListItemResponseDTO>.Fail(dbex.InnerException?.Message ?? dbex.Message, ErrorType.Conflict);
            }
            catch (Exception ex)
            {
                return ServiceResult<ProductTypeListItemResponseDTO>.Fail(ex.Message, ErrorType.Unexpected);
            }
        }

        // ===================== CREATE (Type + Definitions) =====================

        public async Task<ServiceResult<ProductTypeWithDefinitionsResponseDTO>> CreateWithDefinitionsAsync(ProductTypeWithDefinitionsCreateRequestDTO request, CancellationToken ct = default)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Name))
                    return ServiceResult<ProductTypeWithDefinitionsResponseDTO>.Fail("Name is required.", ErrorType.Validation);

                var existsName = await _uow.ProductTypes.ExistsNameAsync(request.Name, null, ct);
                if (existsName)
                    return ServiceResult<ProductTypeWithDefinitionsResponseDTO>.Fail("A ProductType with the same Name already exists.", ErrorType.Conflict);

                var type = new ProductType
                {
                    Name = request.Name.Trim(),
                    IsActive = request.IsActive
                };

                await _uow.ProductTypes.AddAsync(type, ct);
                // no commit aún; insertaremos definiciones y luego commit
                foreach (var d in request.AttributeDefinitions.OrderBy(x => x.DisplayOrder))
                {
                    var def = new ProductAttributeDefinition
                    {
                        ProductTypeId = 0, // se setea tras guardar type (Id), o usamos el mismo 'type' tracking
                        AttributeName = d.AttributeName.Trim(),
                        DataType = d.DataType,
                        IsRequired = d.IsRequired,
                        DisplayOrder = d.DisplayOrder,
                        Unit = string.IsNullOrWhiteSpace(d.Unit) ? null : d.Unit!.Trim()
                    };
                    // Hack simple: deferimos ProductTypeId hasta que type tenga Id.
                    // Alternativa: SaveChanges antes y luego insertar. Aquí haremos SaveChanges parcial primero.
                }

                // Guardamos el tipo para obtener Id
                await _uow.SaveChangesAsync(ct);

                // Insert definiciones ahora sí con ProductTypeId
                foreach (var d in request.AttributeDefinitions.OrderBy(x => x.DisplayOrder))
                {
                    var def = new ProductAttributeDefinition
                    {
                        ProductTypeId = type.Id,
                        AttributeName = d.AttributeName.Trim(),
                        DataType = d.DataType,
                        IsRequired = d.IsRequired,
                        DisplayOrder = d.DisplayOrder,
                        Unit = string.IsNullOrWhiteSpace(d.Unit) ? null : d.Unit!.Trim()
                    };
                    await _uow.ProductAttributeDefinitions.AddAsync(def, ct);
                }

                await _uow.SaveChangesAsync(ct);

                // Build response
                var defs = await _uow.ProductAttributeDefinitions.GetByTypeAsync(type.Id, ct);
                var dto = new ProductTypeWithDefinitionsResponseDTO
                {
                    Id = type.Id,
                    Name = type.Name,
                    IsActive = type.IsActive,
                    AttributeDefinitions = defs
                        .OrderBy(x => x.DisplayOrder)
                        .Select(MapDefinitionToItemDTO)
                        .ToList()
                };

                return ServiceResult<ProductTypeWithDefinitionsResponseDTO>.Ok(dto, "ProductType with definitions created");
            }
            catch (DbUpdateException dbex)
            {
                return ServiceResult<ProductTypeWithDefinitionsResponseDTO>.Fail(dbex.InnerException?.Message ?? dbex.Message, ErrorType.Conflict);
            }
            catch (Exception ex)
            {
                return ServiceResult<ProductTypeWithDefinitionsResponseDTO>.Fail(ex.Message, ErrorType.Unexpected);
            }
        }

        // ===================== READ =====================

        public async Task<ServiceResult<ProductTypeListItemResponseDTO>> GetByIdAsync(int id, CancellationToken ct = default)
        {
            try
            {
                var type = await _uow.ProductTypes.GetByIdAsync(new object[] { id }, ct);
                if (type is null)
                    return ServiceResult<ProductTypeListItemResponseDTO>.Fail("ProductType not found.", ErrorType.NotFound);

                var dto = new ProductTypeListItemResponseDTO
                {
                    Id = type.Id,
                    Name = type.Name,
                    IsActive = type.IsActive
                };
                return ServiceResult<ProductTypeListItemResponseDTO>.Ok(dto, "ProductType retrieved");
            }
            catch (Exception ex)
            {
                return ServiceResult<ProductTypeListItemResponseDTO>.Fail(ex.Message, ErrorType.Unexpected);
            }
        }

        public async Task<ServiceResult<ProductTypeWithDefinitionsResponseDTO>> GetByIdWithDefinitionsAsync(int id, CancellationToken ct = default)
        {
            try
            {
                var type = await _uow.ProductTypes.GetByIdAsync(new object[] { id }, ct);
                if (type is null)
                    return ServiceResult<ProductTypeWithDefinitionsResponseDTO>.Fail("ProductType not found.", ErrorType.NotFound);

                var defs = await _uow.ProductAttributeDefinitions.GetByTypeAsync(id, ct);

                var dto = new ProductTypeWithDefinitionsResponseDTO
                {
                    Id = type.Id,
                    Name = type.Name,
                    IsActive = type.IsActive,
                    AttributeDefinitions = defs
                        .OrderBy(x => x.DisplayOrder)
                        .Select(MapDefinitionToItemDTO)
                        .ToList()
                };

                return ServiceResult<ProductTypeWithDefinitionsResponseDTO>.Ok(dto, "ProductType retrieved");
            }
            catch (Exception ex)
            {
                return ServiceResult<ProductTypeWithDefinitionsResponseDTO>.Fail(ex.Message, ErrorType.Unexpected);
            }
        }

        public async Task<ServiceResult<IReadOnlyList<ProductTypeListItemResponseDTO>>> ListAsync(CancellationToken ct = default)
        {
            try
            {
                var q = _uow.ProductTypes.Query();
                var items = await q
                    .OrderBy(t => t.Name)
                    .Select(t => new ProductTypeListItemResponseDTO
                    {
                        Id = t.Id,
                        Name = t.Name,
                        IsActive = t.IsActive
                    })
                    .ToListAsync(ct);

                if (items.Count == 0)
                    return ServiceResult<IReadOnlyList<ProductTypeListItemResponseDTO>>.Fail("No product types found.", ErrorType.NotFound);

                return ServiceResult<IReadOnlyList<ProductTypeListItemResponseDTO>>.Ok(items, "Product types retrieved");
            }
            catch (Exception ex)
            {
                return ServiceResult<IReadOnlyList<ProductTypeListItemResponseDTO>>.Fail(ex.Message, ErrorType.Unexpected);
            }
        }

        // ===================== UPDATE =====================

        public async Task<ServiceResult<ProductTypeListItemResponseDTO>> UpdateAsync(ProductTypeUpdateRequestDTO request, CancellationToken ct = default)
        {
            try
            {
                var entity = await _uow.ProductTypes.GetByIdAsync(new object[] { request.Id }, ct);
                if (entity is null)
                    return ServiceResult<ProductTypeListItemResponseDTO>.Fail("ProductType not found.", ErrorType.NotFound);

                if (string.IsNullOrWhiteSpace(request.Name))
                    return ServiceResult<ProductTypeListItemResponseDTO>.Fail("Name is required.", ErrorType.Validation);

                var existsName = await _uow.ProductTypes.ExistsNameAsync(request.Name, request.Id, ct);
                if (existsName)
                    return ServiceResult<ProductTypeListItemResponseDTO>.Fail("A ProductType with the same Name already exists.", ErrorType.Conflict);

                entity.Name = request.Name.Trim();
                entity.IsActive = request.IsActive;

                _uow.ProductTypes.Update(entity);
                await _uow.SaveChangesAsync(ct);

                var dto = new ProductTypeListItemResponseDTO
                {
                    Id = entity.Id,
                    Name = entity.Name,
                    IsActive = entity.IsActive
                };
                return ServiceResult<ProductTypeListItemResponseDTO>.Ok(dto, "ProductType updated");
            }
            catch (DbUpdateException dbex)
            {
                return ServiceResult<ProductTypeListItemResponseDTO>.Fail(dbex.InnerException?.Message ?? dbex.Message, ErrorType.Conflict);
            }
            catch (Exception ex)
            {
                return ServiceResult<ProductTypeListItemResponseDTO>.Fail(ex.Message, ErrorType.Unexpected);
            }
        }

        public async Task<ServiceResult<ProductTypeWithDefinitionsResponseDTO>> UpdateWithDefinitionsAsync(ProductTypeWithDefinitionsUpdateRequestDTO request, CancellationToken ct = default)
        {
            try
            {
                var entity = await _uow.ProductTypes.GetByIdAsync(new object[] { request.Id }, ct);
                if (entity is null)
                    return ServiceResult<ProductTypeWithDefinitionsResponseDTO>.Fail("ProductType not found.", ErrorType.NotFound);

                if (string.IsNullOrWhiteSpace(request.Name))
                    return ServiceResult<ProductTypeWithDefinitionsResponseDTO>.Fail("Name is required.", ErrorType.Validation);

                var existsName = await _uow.ProductTypes.ExistsNameAsync(request.Name, request.Id, ct);
                if (existsName)
                    return ServiceResult<ProductTypeWithDefinitionsResponseDTO>.Fail("A ProductType with the same Name already exists.", ErrorType.Conflict);

                entity.Name = request.Name.Trim();
                entity.IsActive = request.IsActive;
                _uow.ProductTypes.Update(entity);

                // Procesar upserts de definiciones
                // 1) Cargar definiciones actuales
                var currentDefs = await _uow.ProductAttributeDefinitions.GetByTypeAsync(entity.Id, ct);
                var currentById = currentDefs.ToDictionary(x => x.Id, x => x);

                // 2) Operaciones
                foreach (var item in request.AttributeDefinitions)
                {
                    // DELETE
                    if (item.Id.HasValue && item.IsDeleted)
                    {
                        if (!currentById.TryGetValue(item.Id.Value, out var toDelete))
                            return ServiceResult<ProductTypeWithDefinitionsResponseDTO>.Fail($"Definition {item.Id.Value} not found.", ErrorType.NotFound);

                        // Validar uso
                        var used = await _uow.ProductAttributeDefinitions.AnyValuesUsingDefinitionAsync(toDelete.Id, ct);
                        if (used)
                            return ServiceResult<ProductTypeWithDefinitionsResponseDTO>.Fail($"Definition '{toDelete.AttributeName}' is in use by existing Product values and cannot be deleted.", ErrorType.Conflict);

                        _uow.ProductAttributeDefinitions.Remove(toDelete);
                        continue;
                    }

                    // ADD
                    if (!item.Id.HasValue)
                    {
                        var def = new ProductAttributeDefinition
                        {
                            ProductTypeId = entity.Id,
                            AttributeName = item.AttributeName.Trim(),
                            DataType = item.DataType,
                            IsRequired = item.IsRequired,
                            DisplayOrder = item.DisplayOrder,
                            Unit = string.IsNullOrWhiteSpace(item.Unit) ? null : item.Unit!.Trim()
                        };
                        await _uow.ProductAttributeDefinitions.AddAsync(def, ct);
                        continue;
                    }

                    // UPDATE
                    if (item.Id.HasValue && !item.IsDeleted)
                    {
                        if (!currentById.TryGetValue(item.Id.Value, out var toUpdate))
                            return ServiceResult<ProductTypeWithDefinitionsResponseDTO>.Fail($"Definition {item.Id.Value} not found.", ErrorType.NotFound);

                        toUpdate.AttributeName = item.AttributeName.Trim();
                        toUpdate.DataType = item.DataType;
                        toUpdate.IsRequired = item.IsRequired;
                        toUpdate.DisplayOrder = item.DisplayOrder;
                        toUpdate.Unit = string.IsNullOrWhiteSpace(item.Unit) ? null : item.Unit!.Trim();

                        _uow.ProductAttributeDefinitions.Update(toUpdate);
                    }
                }

                await _uow.SaveChangesAsync(ct);

                // Response final
                var finalDefs = await _uow.ProductAttributeDefinitions.GetByTypeAsync(entity.Id, ct);
                var dto = new ProductTypeWithDefinitionsResponseDTO
                {
                    Id = entity.Id,
                    Name = entity.Name,
                    IsActive = entity.IsActive,
                    AttributeDefinitions = finalDefs
                        .OrderBy(x => x.DisplayOrder)
                        .Select(MapDefinitionToItemDTO)
                        .ToList()
                };

                return ServiceResult<ProductTypeWithDefinitionsResponseDTO>.Ok(dto, "ProductType + definitions updated");
            }
            catch (DbUpdateException dbex)
            {
                return ServiceResult<ProductTypeWithDefinitionsResponseDTO>.Fail(dbex.InnerException?.Message ?? dbex.Message, ErrorType.Conflict);
            }
            catch (Exception ex)
            {
                return ServiceResult<ProductTypeWithDefinitionsResponseDTO>.Fail(ex.Message, ErrorType.Unexpected);
            }
        }

        // ===================== DELETE =====================

        public async Task<ServiceResult<NoContent>> DeleteAsync(int id, CancellationToken ct = default)
        {
            try
            {
                var entity = await _uow.ProductTypes.GetByIdAsync(new object[] { id }, ct);
                if (entity is null)
                    return ServiceResult<NoContent>.Fail("ProductType not found.", ErrorType.NotFound);

                // 1) Bloquear si existen productos que usan este tipo
                var anyProducts = await _uow.Products.Query()
                    .AnyAsync(p => p.ProductTypeId == id, ct);
                if (anyProducts)
                    return ServiceResult<NoContent>.Fail("Cannot delete ProductType: there are products using this type.", ErrorType.Conflict);

                // 2) Bloquear si existen definiciones en uso por valores
                var defs = await _uow.ProductAttributeDefinitions.GetByTypeAsync(id, ct);
                foreach (var d in defs)
                {
                    var used = await _uow.ProductAttributeDefinitions.AnyValuesUsingDefinitionAsync(d.Id, ct);
                    if (used)
                        return ServiceResult<NoContent>.Fail($"Cannot delete ProductType: definition '{d.AttributeName}' has values in use.", ErrorType.Conflict);
                }

                // 3) Si no hay uso: borrar definiciones y luego el tipo
                foreach (var d in defs)
                    _uow.ProductAttributeDefinitions.Remove(d);

                _uow.ProductTypes.Remove(entity);
                await _uow.SaveChangesAsync(ct);

                return ServiceResult<NoContent>.Ok(new NoContent(), "ProductType deleted");
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

        private static ProductAttributeDefinitionItemDTO MapDefinitionToItemDTO(ProductAttributeDefinition d) => new()
        {
            Id = d.Id,
            ProductTypeId = d.ProductTypeId,
            AttributeName = d.AttributeName,
            DataType = d.DataType,
            IsRequired = d.IsRequired,
            DisplayOrder = d.DisplayOrder,
            Unit = d.Unit
        };
    }
}