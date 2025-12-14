using Microsoft.EntityFrameworkCore;
using tesisproject.backend.Repositories.Interfaces;
using tesisproject.backend.Services.Interfaces;
using tesisproject.backend.UnitOfWork.Interfaces;
using tesisproject.shared.DTOs.Catalog.Common.Request;
using tesisproject.shared.DTOs.Catalog.Common.Response;
using tesisproject.shared.Entities.Base;
using tesisproject.shared.Responses;

namespace tesisproject.backend.Services.Implementations
{
    /// <summary>
    /// Generic CRUD service for simple catalog entities (Id, Name, IsActive, IsLocked).
    /// </summary>
    public class CatalogCrudService<TCatalog> : ICatalogCrudService<TCatalog>
        where TCatalog : CatalogEntityBase, new()
    {
        private readonly IUnitOfWork _uow;
        private readonly ICatalogRepository<TCatalog> _repo;

        public CatalogCrudService(
            IUnitOfWork uow,
            ICatalogRepository<TCatalog> repo)
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
                // Si quieres solo activos, puedes pasar onlyActives: true
                var items = await _repo.ListAsync(
                    onlyActives: false,
                    ct: ct);

                var dto = items
                    .OrderBy(x => x.Name)
                    .Select(MapToListItem)
                    .ToList()
                    .AsReadOnly();

                var message = dto.Count == 0
                    ? "No items found for this catalog."
                    : "Catalog items retrieved.";

                return ServiceResult<IReadOnlyList<CatalogListItemDTO>>
                    .Ok(dto, message);
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
                        .Fail("Id is required.", ErrorType.Validation);

                var entity = await _repo.GetByIdAsync(new object[] { id }, ct);
                if (entity is null)
                    return ServiceResult<CatalogDetailDTO>
                        .Fail("Item not found.", ErrorType.NotFound);

                return ServiceResult<CatalogDetailDTO>.Ok(
                    MapToDetail(entity),
                    "Catalog item retrieved.");
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
                        .Fail("Name is required.", ErrorType.Validation);

                var exists = await _repo.NameExistsAsync(name, excludeId: null, ct);
                if (exists)
                    return ServiceResult<CatalogDetailDTO>
                        .Fail("Name already exists.", ErrorType.Validation);

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
                    "Catalog item created.");
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
                        .Fail("Id is required.", ErrorType.Validation);

                var entity = await _repo.GetByIdAsync(new object[] { request.Id }, ct);
                if (entity is null)
                    return ServiceResult<CatalogDetailDTO>
                        .Fail("Item not found.", ErrorType.NotFound);

                if (entity.IsLocked)
                    return ServiceResult<CatalogDetailDTO>
                        .Fail("Catalog item is locked and cannot be modified.", ErrorType.Conflict);

                var name = (request.Name ?? string.Empty).Trim();
                if (string.IsNullOrWhiteSpace(name))
                    return ServiceResult<CatalogDetailDTO>
                        .Fail("Name is required.", ErrorType.Validation);

                var duplicated = await _repo.NameExistsAsync(name, excludeId: request.Id, ct);
                if (duplicated)
                    return ServiceResult<CatalogDetailDTO>
                        .Fail("Name already exists.", ErrorType.Validation);

                entity.Name = name;
                entity.IsActive = request.IsActive;

                _repo.Update(entity);
                await _uow.SaveChangesAsync(ct);

                return ServiceResult<CatalogDetailDTO>.Ok(
                    MapToDetail(entity),
                    "Catalog item updated.");
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
                        .Fail("Id is required.", ErrorType.Validation);

                var entity = await _repo.GetByIdAsync(new object[] { id }, ct);
                if (entity is null)
                    return ServiceResult<NoContent>
                        .Fail("Item not found.", ErrorType.NotFound);

                if (entity.IsLocked)
                    return ServiceResult<NoContent>
                        .Fail("Catalog item is locked and cannot be deleted.", ErrorType.Conflict);

                _repo.Remove(entity);
                await _uow.SaveChangesAsync(ct);

                return ServiceResult<NoContent>
                    .Ok(new NoContent(), "Catalog item deleted.");
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
