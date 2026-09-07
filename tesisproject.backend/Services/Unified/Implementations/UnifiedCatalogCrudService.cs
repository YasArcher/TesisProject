using Microsoft.EntityFrameworkCore;
using tesisproject.backend.Repositories.Unified.Interfaces;
using tesisproject.backend.Services.Interfaces;
using tesisproject.backend.Services.Unified.Interfaces;
using tesisproject.backend.UnitOfWork.Unified.Interfaces;
using tesisproject.shared.Common.Utils;
using tesisproject.shared.DTOs.Catalog.Common.Request;
using tesisproject.shared.DTOs.Catalog.Common.Response;
using tesisproject.backend.Data.UnifiedEntities.Base;
using tesisproject.shared.Enums;
using tesisproject.shared.Errors;
using tesisproject.shared.Responses;

namespace tesisproject.backend.Services.Unified.Implementations
{
    /// <summary>
    /// Generic CRUD service for simple catalog entities (Id, Name, IsActive, IsLocked).
    /// </summary>
    public class UnifiedCatalogCrudService<TCatalog> : IUnifiedCatalogCrudService<TCatalog>
        where TCatalog : CatalogEntityBase, new()
    {

        private const string CatalogItemsRetrievedMessage = "Catalog items retrieved.";
        private const string CatalogItemRetrievedMessage = "Catalog item retrieved.";
        private const string CatalogItemCreatedMessage = "Catalog item created.";
        private const string CatalogItemUpdatedMessage = "Catalog item updated.";
        private const string CatalogItemRenamedByCloneMessage = "Catalog item renamed by creating a new item and deactivating the previous one.";
        private const string CatalogItemDeletedMessage = "Catalog item deleted.";

        private readonly IUnifiedUnitOfWork _uow;
        private readonly IUnifiedCatalogRepository<TCatalog> _repo;

        private static double SimilarityThresholdPercent =>
            (double)(int)CatalogLevenshteinConfig.SimilarityThresholdPercent;

        private static int SimilarityMaxCandidates =>
            (int)CatalogLevenshteinConfig.MaxCandidates;

        public UnifiedCatalogCrudService(IUnifiedUnitOfWork uow)
        {
            _uow = uow;
            _repo = UnifiedCatalogAccess.Get<TCatalog>(uow);
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
                    ? ErrorMessages.UnifiedLegacy.CatalogCrudService_NoItemsFoundMessage
                    : CatalogItemsRetrievedMessage;

                return ServiceResult<IReadOnlyList<CatalogListItemDTO>>.Ok(dto, message);
            }
            catch (Exception)
            {
                return FailUnexpected<IReadOnlyList<CatalogListItemDTO>>();
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
                {
                    return ValidationFailure<CatalogDetailDTO>(
                        ErrorMessages.Common.InvalidId,
                        ErrorCodes.Common.InvalidId,
                        nameof(CatalogDetailDTO.Id));
                }

                var entity = await _repo.GetByIdAsync([id], ct);
                if (entity is null)
                {
                    return ServiceResult<CatalogDetailDTO>.Fail(
                        ErrorMessages.Catalog.ItemNotFound,
                        ErrorType.NotFound,
                        ErrorCodes.Catalog.ItemNotFound);
                }

                return ServiceResult<CatalogDetailDTO>.Ok(
                    MapToDetail(entity),
                    CatalogItemRetrievedMessage);
            }
            catch (Exception)
            {
                return FailUnexpected<CatalogDetailDTO>();
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
                {
                    return ValidationFailure<CatalogDetailDTO>(
                        ErrorMessages.Common.NameRequired,
                        ErrorCodes.Common.NameRequired,
                        nameof(AddCatalogRequestDTO.Name));
                }

                var exists = await _repo.NameExistsAsync(name, excludeId: null, ct);
                if (exists)
                {
                    return ValidationFailure<CatalogDetailDTO>(
                        ErrorMessages.Common.NameAlreadyExists,
                        ErrorCodes.Common.NameAlreadyExists,
                        nameof(AddCatalogRequestDTO.Name));
                }

                var entity = new TCatalog
                {
                    Name = name,
                    IsActive = true
                };

                await _repo.AddAsync(entity, ct);
                await _uow.SaveChangesAsync(ct);

                return ServiceResult<CatalogDetailDTO>.Ok(
                    MapToDetail(entity),
                    CatalogItemCreatedMessage);
            }
            catch (DbUpdateException)
            {
                return FailConflict<CatalogDetailDTO>();
            }
            catch (Exception)
            {
                return FailUnexpected<CatalogDetailDTO>();
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
                {
                    return ValidationFailure<CatalogDetailDTO>(
                        ErrorMessages.Common.InvalidId,
                        ErrorCodes.Common.InvalidId,
                        nameof(UpdateCatalogRequestDTO.Id));
                }

                var entity = await _repo.GetByIdAsync([request.Id], ct);
                if (entity is null)
                {
                    return ServiceResult<CatalogDetailDTO>.Fail(
                        ErrorMessages.Catalog.ItemNotFound,
                        ErrorType.NotFound,
                        ErrorCodes.Catalog.ItemNotFound);
                }

                if (entity.IsLocked)
                {
                    return ServiceResult<CatalogDetailDTO>.Fail(
                        ErrorMessages.Catalog.ItemLockedForModify,
                        ErrorType.Conflict,
                        ErrorCodes.Catalog.ItemLockedForModify);
                }

                var newName = (request.Name ?? string.Empty).Trim();
                if (string.IsNullOrWhiteSpace(newName))
                {
                    return ValidationFailure<CatalogDetailDTO>(
                        ErrorMessages.Common.NameRequired,
                        ErrorCodes.Common.NameRequired,
                        nameof(UpdateCatalogRequestDTO.Name));
                }

                var nameChanged = !string.Equals(
                    entity.Name?.Trim(),
                    newName,
                    StringComparison.OrdinalIgnoreCase);

                if (!nameChanged)
                {
                    entity.IsActive = request.IsActive;
                    return await SaveAndOkAsync(entity, CatalogItemUpdatedMessage, ct);
                }

                var duplicated = await _repo.NameExistsAsync(newName, excludeId: request.Id, ct);
                if (duplicated)
                {
                    return ValidationFailure<CatalogDetailDTO>(
                        ErrorMessages.Catalog.NameAlreadyExistsDetailed,
                        ErrorCodes.Common.NameAlreadyExists,
                        nameof(UpdateCatalogRequestDTO.Name));
                }

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

                    return ValidationFailure<CatalogDetailDTO>(
                        string.Format(ErrorMessages.Catalog.SimilarNameCandidatesFound, hint),
                        ErrorCodes.Catalog.SimilarNameCandidatesFound,
                        nameof(UpdateCatalogRequestDTO.Name));
                }

                var hasReferences = await _repo.HasReferencesAsync(entity.Id, ct);

                if (hasReferences)
                {
                    var newEntity = new TCatalog
                    {
                        Name = newName,
                        IsActive = request.IsActive,
                    };

                    await _repo.AddAsync(newEntity, ct);

                    entity.IsActive = false;
                    _repo.Update(entity);

                    await _uow.SaveChangesAsync(ct);

                    return ServiceResult<CatalogDetailDTO>.Ok(
                        MapToDetail(newEntity),
                        CatalogItemRenamedByCloneMessage);
                }

                entity.Name = newName;
                entity.IsActive = request.IsActive;

                return await SaveAndOkAsync(entity, CatalogItemUpdatedMessage, ct);
            }
            catch (DbUpdateException)
            {
                return FailConflict<CatalogDetailDTO>();
            }
            catch (Exception)
            {
                return FailUnexpected<CatalogDetailDTO>();
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
                {
                    return ValidationFailure<NoContent>(
                        ErrorMessages.Common.InvalidId,
                        ErrorCodes.Common.InvalidId,
                        "Id");
                }

                var entity = await _repo.GetByIdAsync([id], ct);
                if (entity is null)
                {
                    return ServiceResult<NoContent>.Fail(
                        ErrorMessages.Catalog.ItemNotFound,
                        ErrorType.NotFound,
                        ErrorCodes.Catalog.ItemNotFound);
                }

                if (entity.IsLocked)
                {
                    return ServiceResult<NoContent>.Fail(
                        ErrorMessages.Catalog.ItemLockedForDelete,
                        ErrorType.Conflict,
                        ErrorCodes.Catalog.ItemLockedForDelete);
                }

                _repo.Remove(entity);
                await _uow.SaveChangesAsync(ct);

                return ServiceResult<NoContent>
                    .Ok(new NoContent(), CatalogItemDeletedMessage);
            }
            catch (DbUpdateException)
            {
                return FailConflict<NoContent>();
            }
            catch (Exception)
            {
                return FailUnexpected<NoContent>();
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

        private static ServiceResult<T> FailConflict<T>()
            => ServiceResult<T>.Fail(
                ErrorMessages.Common.PersistenceConflict,
                ErrorType.Conflict,
                ErrorCodes.Common.PersistenceConflict);

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