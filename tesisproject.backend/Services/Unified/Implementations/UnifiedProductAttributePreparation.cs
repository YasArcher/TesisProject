using Microsoft.EntityFrameworkCore;
using tesisproject.backend.Data.UnifiedEntities.Catalogs;
using tesisproject.backend.UnitOfWork.Unified.Interfaces;
using tesisproject.shared.DTOs.Catalog.ProductAttribute.Request;
using tesisproject.shared.Errors;
using tesisproject.shared.Responses;

namespace tesisproject.backend.Services.Unified.Implementations;

// Operation-local preparation shared by autonomous CRUD and product type design.
// Validation changes only detached Values; ApplyAsync never commits the UoW.
internal sealed class UnifiedProductAttributePreparation(IUnifiedUnitOfWork uow)
{
    private readonly List<PreparedAttribute> _changes = [];

    internal sealed class PreparedAttribute(ProductAttribute entity, ProductAttribute values, bool isNew)
    {
        internal ProductAttribute Entity { get; } = entity;
        internal ProductAttribute Values { get; set; } = values;
        internal bool IsNew { get; } = isNew;
    }

    internal async Task<ServiceResult<PreparedAttribute>> PrepareCreateAsync(
        AddProductAttributeRequestDTO? request, CancellationToken ct)
    {
        var name = (request?.Name ?? string.Empty).Trim();
        if (request is null || string.IsNullOrWhiteSpace(name))
            return Invalid(ErrorMessages.UnifiedLegacy.FacultyScopeService_NameRequiredMessage);
        if (await NameExistsAsync(name, null, ct))
            return Invalid(ErrorMessages.UnifiedLegacy.IndexingSourceService_NameAlreadyExistsMessage);

        var entity = new ProductAttribute
        {
            Name = name,
            IsActive = request.IsActive,
            DataType = request.DataType,
            Unit = request.Unit,
            IsLocked = false
        };
        var prepared = new PreparedAttribute(entity, entity, true);
        _changes.Add(prepared);
        return ServiceResult<PreparedAttribute>.Ok(prepared);
    }

    internal async Task<ServiceResult<PreparedAttribute>> PrepareUpdateAsync(
        UpdateProductAttributeRequestDTO? request, CancellationToken ct)
    {
        if (request is null || request.Id <= 0)
            return Invalid(ErrorMessages.UnifiedLegacy.IndexingSourceService_InvalidIdMessage);
        var name = (request.Name ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(name))
            return Invalid(ErrorMessages.UnifiedLegacy.FacultyScopeService_NameRequiredMessage);

        var prepared = _changes.FirstOrDefault(x => !x.IsNew && x.Entity.Id == request.Id);
        var entity = prepared?.Entity ?? await uow.ProductAttributes.GetByIdAsync(new object[] { request.Id }, ct);
        if (entity is null)
            return ServiceResult<PreparedAttribute>.Fail(ErrorMessages.UnifiedLegacy.ProductAttributeService_NotFoundMessage,
                ErrorType.NotFound, ErrorCodes.Common.NotFound);
        if ((prepared?.Values ?? entity).IsLocked)
            return Invalid(ErrorMessages.UnifiedLegacy.ProductAttributeService_LockedCannotModifyMessage);
        if (await NameExistsAsync(name, request.Id, ct))
            return Invalid(ErrorMessages.UnifiedLegacy.IndexingSourceService_NameAlreadyExistsMessage);

        var values = new ProductAttribute
        {
            Id = entity.Id,
            Name = name,
            IsActive = request.IsActive,
            DataType = request.DataType,
            Unit = request.Unit,
            IsLocked = request.IsLocked
        };
        if (prepared is null)
        {
            prepared = new PreparedAttribute(entity, values, false);
            _changes.Add(prepared);
        }
        else
            prepared.Values = values;
        return ServiceResult<PreparedAttribute>.Ok(prepared);
    }

    private async Task<bool> NameExistsAsync(string name, int? excludeId, CancellationToken ct)
    {
        if (_changes.Count == 0)
            return await uow.ProductAttributes.NameExistsAsync(name, excludeId, ct);

        // The former intermediate saves made preceding renames/inserts visible to the
        // next validation. Overlay that prepared state without writing it to the DB.
        var changedIds = _changes.Where(x => !x.IsNew).Select(x => x.Entity.Id).ToArray();
        if (await uow.ProductAttributes.Query()
            .AnyAsync(x => x.Name == name && (!excludeId.HasValue || x.Id != excludeId.Value)
                && !changedIds.Contains(x.Id), ct))
            return true;

        foreach (var change in _changes.Where(x => x.IsNew || x.Entity.Id != excludeId))
        {
            var preparedName = change.Values.Name;
            // Compare through the provider rather than invent an in-memory case/accent
            // rule. DefaultIfEmpty supplies one scalar row even for an empty catalog.
            var preparedNames = uow.ProductAttributes.Query().Select(x => x.Id)
                .Take(0).DefaultIfEmpty().Select(_ => preparedName);
            if (await preparedNames.AnyAsync(candidate => candidate == name, ct))
                return true;
        }
        return false;
    }

    internal async Task ApplyAsync(CancellationToken ct)
    {
        foreach (var change in _changes)
        {
            if (change.IsNew)
                await uow.ProductAttributes.AddAsync(change.Entity, ct);
            else
            {
                change.Entity.Name = change.Values.Name;
                change.Entity.IsActive = change.Values.IsActive;
                change.Entity.DataType = change.Values.DataType;
                change.Entity.Unit = change.Values.Unit;
                change.Entity.IsLocked = change.Values.IsLocked;
                uow.ProductAttributes.Update(change.Entity);
            }
        }
    }

    private static ServiceResult<PreparedAttribute> Invalid(string message) =>
        ServiceResult<PreparedAttribute>.Fail(message, ErrorType.Validation, ErrorCodes.Common.InvalidRequest);
}
