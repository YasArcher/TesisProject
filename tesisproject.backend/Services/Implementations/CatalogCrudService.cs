using Microsoft.EntityFrameworkCore;
using tesisproject.backend.Repositories.Interfaces;
using tesisproject.backend.Services.Interfaces;
using tesisproject.backend.UnitOfWork.Interfaces;
using tesisproject.shared.Common.Utils;
using tesisproject.shared.DTOs.Catalog.Common.Request;
using tesisproject.shared.DTOs.Catalog.Common.Response;
using tesisproject.shared.Entities.Base;
using tesisproject.shared.Enums;
using tesisproject.shared.Responses;

namespace tesisproject.backend.Services.Implementations
{
    /// <summary>
    /// Generic CRUD service for simple catalog entities (Id, Name, IsActive, IsLocked).
    /// </summary>
    public class CatalogCrudService<TCatalog> : ICatalogCrudService<TCatalog>
        where TCatalog : CatalogEntityBase, new()
    {
        private const string NoItemsFoundMessage = "No items found for this catalog.";
        private const string CatalogItemsRetrievedMessage = "Catalog items retrieved.";
        private const string IdRequiredMessage = "Id is required.";
        private const string ItemNotFoundMessage = "Item not found.";
        private const string CatalogItemRetrievedMessage = "Catalog item retrieved.";
        private const string NameRequiredMessage = "Name is required.";
        private const string NameAlreadyExistsMessage = "Name already exists.";
        private const string CatalogItemCreatedMessage = "Catalog item created.";
        private const string CatalogItemLockedForModifyMessage = "Catalog item is locked and cannot be modified.";
        private const string CatalogItemUpdatedMessage = "Catalog item updated.";
        private const string NameAlreadyExistsDetailedMessage = "Name already exists. Please review the catalog to avoid duplicates.";
        private const string SimilarNameCandidatesTemplate = "This name looks very similar to existing items. Please review before saving. Candidates: {0}";
        private const string CatalogItemRenamedByCloneMessage = "Catalog item renamed by creating a new item and deactivating the previous one.";
        private const string CatalogItemLockedForDeleteMessage = "Catalog item is locked and cannot be deleted.";
        private const string CatalogItemDeletedMessage = "Catalog item deleted.";

        private readonly IUnitOfWork _uow;
        private readonly ICatalogRepository<TCatalog> _repo;

        // Levenshtein config from enum
        private static double SimilarityThresholdPercent =>
            (double)(int)CatalogLevenshteinConfig.SimilarityThresholdPercent;

        private static int SimilarityMaxCandidates =>
            (int)CatalogLevenshteinConfig.MaxCandidates;

        public CatalogCrudService(IUnitOfWork uow, ICatalogRepository<TCatalog> repo)
        {
            _uow = uow;
            _repo = repo;
        }

        // =============== LIST ===============

        public async Task<ServiceResult<IReadOnlyList<CatalogListItemDTO>>> ListAsync(
            CancellationToken ct = default)
        {
            try
            {
                var items = await _repo.ListAsync(
                    onlyActives: false,
                    ct: ct);

                var dto = items
                    .OrderBy(x => x.Name)
                    .Select(MapToListItem)
                    .ToList()
                    .AsReadOnly();

                var message = dto.Count == 0
                    ? NoItemsFoundMessage
                    : CatalogItemsRetrievedMessage;

                return ServiceResult<IReadOnlyList<CatalogListItemDTO>>.Ok(dto, message);
            }
            catch (Exception ex)
            {
                return ServiceResult<IReadOnlyList<CatalogListItemDTO>>
                    .Fail(ex.Message, ErrorType.Unexpected);
            }
        }

        // =============== READ ONE ===============

        public async Task<ServiceResult<CatalogDetailDTO>> GetByIdAsync(
            int id,
            CancellationToken ct = default)
        {
            try
            {
                if (id <= 0)
                    return ServiceResult<CatalogDetailDTO>
                        .Fail(IdRequiredMessage, ErrorType.Validation);

                var entity = await _repo.GetByIdAsync(new object[] { id }, ct);
                if (entity is null)
                    return ServiceResult<CatalogDetailDTO>
                        .Fail(ItemNotFoundMessage, ErrorType.NotFound);

                return ServiceResult<CatalogDetailDTO>.Ok(
                    MapToDetail(entity),
                    CatalogItemRetrievedMessage);
            }
            catch (Exception ex)
            {
                return ServiceResult<CatalogDetailDTO>
                    .Fail(ex.Message, ErrorType.Unexpected);
            }
        }

        // =============== CREATE ===============

        public async Task<ServiceResult<CatalogDetailDTO>> CreateAsync(
            AddCatalogRequestDTO request,
            CancellationToken ct = default)
        {
            try
            {
                var name = (request?.Name ?? string.Empty).Trim();
                if (string.IsNullOrWhiteSpace(name))
                    return ServiceResult<CatalogDetailDTO>
                        .Fail(NameRequiredMessage, ErrorType.Validation);

                var exists = await _repo.NameExistsAsync(name, excludeId: null, ct);
                if (exists)
                    return ServiceResult<CatalogDetailDTO>
                        .Fail(NameAlreadyExistsMessage, ErrorType.Validation);

                var entity = new TCatalog
                {
                    Name = name,
                    IsActive = true,
                    // IsLocked viene de CatalogEntityBase, por defecto false
                };

                await _repo.AddAsync(entity, ct);
                await _uow.SaveChangesAsync(ct);

                return ServiceResult<CatalogDetailDTO>.Ok(
                    MapToDetail(entity),
                    CatalogItemCreatedMessage);
            }
            catch (DbUpdateException dbex)
            {
                return ServiceResult<CatalogDetailDTO>
                    .Fail(dbex.InnerException?.Message ?? dbex.Message, ErrorType.Conflict);
            }
            catch (Exception ex)
            {
                return ServiceResult<CatalogDetailDTO>
                    .Fail(ex.Message, ErrorType.Unexpected);
            }
        }

        // =============== UPDATE ===============

        public async Task<ServiceResult<CatalogDetailDTO>> UpdateAsync(
            UpdateCatalogRequestDTO request,
            CancellationToken ct = default)
        {
            try
            {
                if (request is null || request.Id <= 0)
                    return ServiceResult<CatalogDetailDTO>
                        .Fail(IdRequiredMessage, ErrorType.Validation);

                var entity = await _repo.GetByIdAsync(new object[] { request.Id }, ct);
                if (entity is null)
                    return ServiceResult<CatalogDetailDTO>
                        .Fail(ItemNotFoundMessage, ErrorType.NotFound);

                if (entity.IsLocked)
                    return ServiceResult<CatalogDetailDTO>
                        .Fail(CatalogItemLockedForModifyMessage, ErrorType.Conflict);

                var newName = (request.Name ?? string.Empty).Trim();
                if (string.IsNullOrWhiteSpace(newName))
                    return ServiceResult<CatalogDetailDTO>
                        .Fail(NameRequiredMessage, ErrorType.Validation);

                var nameChanged = !string.Equals(
                    entity.Name?.Trim(),
                    newName,
                    StringComparison.OrdinalIgnoreCase);

                // ====== Si NO cambió el nombre -> solo IsActive ======
                if (!nameChanged)
                {
                    entity.IsActive = request.IsActive;
                    return await SaveAndOkAsync(entity, CatalogItemUpdatedMessage, ct);
                }

                // ====== Si cambió el nombre ======

                // 1) Duplicado exacto (bloquear)
                var duplicated = await _repo.NameExistsAsync(newName, excludeId: request.Id, ct);
                if (duplicated)
                {
                    return ServiceResult<CatalogDetailDTO>
                        .Fail(NameAlreadyExistsDetailedMessage, ErrorType.Validation);
                }

                // 2) Posible duplicado (Levenshtein) -> bloquear y sugerir
                var allItems = await _repo.ListAsync(onlyActives: false, ct: ct);

                var suggestions = allItems
                    .Where(x => x.Id != entity.Id)
                    .Select(x => new
                    {
                        x.Id,
                        x.Name,
                        x.IsActive,
                        Similarity = Levenshtein.SimilarityPercentage(newName, x.Name)
                    })
                    .Where(x => x.Similarity >= SimilarityThresholdPercent)
                    .OrderByDescending(x => x.Similarity)
                    .Take(SimilarityMaxCandidates)
                    .ToList();

                if (suggestions.Count > 0)
                {
                    var hint = string.Join(" | ", suggestions.Select(s =>
                        $"{s.Name} (Id: {s.Id}, Similarity: {s.Similarity:0.0}%, Active: {s.IsActive})"));

                    return ServiceResult<CatalogDetailDTO>.Fail(
                        string.Format(SimilarNameCandidatesTemplate, hint),
                        ErrorType.Validation);
                }

                // 3) Regla: si tiene referencias -> crear nuevo + desactivar actual
                var hasReferences = await _repo.HasReferencesAsync(entity.Id, ct);

                if (hasReferences)
                {
                    var newEntity = new TCatalog
                    {
                        Name = newName,
                        IsActive = request.IsActive, // ✅ respeta el toggle del request
                    };

                    await _repo.AddAsync(newEntity, ct);

                    entity.IsActive = false;
                    _repo.Update(entity);

                    await _uow.SaveChangesAsync(ct);

                    return ServiceResult<CatalogDetailDTO>.Ok(
                        MapToDetail(newEntity),
                        CatalogItemRenamedByCloneMessage);
                }

                // 4) Si NO tiene referencias -> update in-place
                entity.Name = newName;
                entity.IsActive = request.IsActive;

                return await SaveAndOkAsync(entity, CatalogItemUpdatedMessage, ct);
            }
            catch (DbUpdateException dbex)
            {
                return ServiceResult<CatalogDetailDTO>
                    .Fail(dbex.InnerException?.Message ?? dbex.Message, ErrorType.Conflict);
            }
            catch (Exception ex)
            {
                return ServiceResult<CatalogDetailDTO>
                    .Fail(ex.Message, ErrorType.Unexpected);
            }
        }

        // =============== DELETE ===============

        public async Task<ServiceResult<NoContent>> DeleteAsync(
            int id,
            CancellationToken ct = default)
        {
            try
            {
                if (id <= 0)
                    return ServiceResult<NoContent>
                        .Fail(IdRequiredMessage, ErrorType.Validation);

                var entity = await _repo.GetByIdAsync(new object[] { id }, ct);
                if (entity is null)
                    return ServiceResult<NoContent>
                        .Fail(ItemNotFoundMessage, ErrorType.NotFound);

                if (entity.IsLocked)
                    return ServiceResult<NoContent>
                        .Fail(CatalogItemLockedForDeleteMessage, ErrorType.Conflict);

                _repo.Remove(entity);
                await _uow.SaveChangesAsync(ct);

                return ServiceResult<NoContent>
                    .Ok(new NoContent(), CatalogItemDeletedMessage);
            }
            catch (DbUpdateException dbex)
            {
                return ServiceResult<NoContent>
                    .Fail(dbex.InnerException?.Message ?? dbex.Message, ErrorType.Conflict);
            }
            catch (Exception ex)
            {
                return ServiceResult<NoContent>
                    .Fail(ex.Message, ErrorType.Unexpected);
            }
        }

        // =============== PRIVATE HELPERS ===============

        private async Task<ServiceResult<CatalogDetailDTO>> SaveAndOkAsync(
            TCatalog entity,
            string message,
            CancellationToken ct)
        {
            _repo.Update(entity);
            await _uow.SaveChangesAsync(ct);

            return ServiceResult<CatalogDetailDTO>.Ok(
                MapToDetail(entity),
                message);
        }

        // =============== MAPPING HELPERS ===============

        private static CatalogListItemDTO MapToListItem(TCatalog x) => new()
        {
            Id = x.Id,
            Name = x.Name,
            IsActive = x.IsActive,
            IsLocked = x.IsLocked
        };

        private static CatalogDetailDTO MapToDetail(TCatalog x) => new()
        {
            Id = x.Id,
            Name = x.Name,
            IsActive = x.IsActive,
            IsLocked = x.IsLocked
        };
    }
}
