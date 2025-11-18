using Microsoft.EntityFrameworkCore;
using tesisproject.backend.Repositories.Interfaces;
using tesisproject.backend.Services.Interfaces;
using tesisproject.backend.UnitOfWork.Interfaces;
using tesisproject.shared.Entities.Base;
using tesisproject.shared.Responses;

namespace tesisproject.backend.Services.Implementations
{
    /// <summary>
    /// Generic CRUD service for catalog entities that inherit from CatalogEntityBase.
    /// Uses IGenericRepository<TCatalog> plus IUnitOfWork for SaveChanges.
    /// </summary>
    public class CatalogCrudService<TCatalog> : ICatalogCrudService<TCatalog>
        where TCatalog : CatalogEntityBase
    {
        private readonly IUnitOfWork _uow;
        private readonly IGenericRepository<TCatalog> _repo;

        public CatalogCrudService(IUnitOfWork uow, IGenericRepository<TCatalog> repo)
        {
            _uow = uow;
            _repo = repo;
        }

        // =============== LIST ===============

        public async Task<ServiceResult<IReadOnlyList<TCatalog>>> ListAsync(CancellationToken ct = default)
        {
            try
            {
                var items = await _repo.GetAllAsync(ct: ct);

                if (items.Count == 0)
                    return ServiceResult<IReadOnlyList<TCatalog>>
                        .Fail("No items found for this catalog.", ErrorType.NotFound);

                return ServiceResult<IReadOnlyList<TCatalog>>
                    .Ok(items, "Catalog items retrieved.");
            }
            catch (Exception ex)
            {
                return ServiceResult<IReadOnlyList<TCatalog>>
                    .Fail(ex.Message, ErrorType.Unexpected);
            }
        }

        // =============== READ ONE ===============

        public async Task<ServiceResult<TCatalog>> GetByIdAsync(int id, CancellationToken ct = default)
        {
            try
            {
                if (id <= 0)
                    return ServiceResult<TCatalog>
                        .Fail("Id is required.", ErrorType.Validation);

                var entity = await _repo.GetByIdAsync(new object[] { id }, ct);
                if (entity is null)
                    return ServiceResult<TCatalog>
                        .Fail("Item not found.", ErrorType.NotFound);

                return ServiceResult<TCatalog>.Ok(entity, "Catalog item retrieved.");
            }
            catch (Exception ex)
            {
                return ServiceResult<TCatalog>
                    .Fail(ex.Message, ErrorType.Unexpected);
            }
        }

        // =============== CREATE ===============

        public async Task<ServiceResult<TCatalog>> CreateAsync(TCatalog entity, CancellationToken ct = default)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(entity.Name))
                    return ServiceResult<TCatalog>
                        .Fail("Name is required.", ErrorType.Validation);

                await _repo.AddAsync(entity, ct);
                await _uow.SaveChangesAsync(ct);

                return ServiceResult<TCatalog>.Ok(entity, "Catalog item created.");
            }
            catch (DbUpdateException dbex)
            {
                return ServiceResult<TCatalog>
                    .Fail(dbex.InnerException?.Message ?? dbex.Message, ErrorType.Conflict);
            }
            catch (Exception ex)
            {
                return ServiceResult<TCatalog>
                    .Fail(ex.Message, ErrorType.Unexpected);
            }
        }

        // =============== UPDATE ===============

        public async Task<ServiceResult<TCatalog>> UpdateAsync(int id, TCatalog input, CancellationToken ct = default)
        {
            try
            {
                if (id <= 0)
                    return ServiceResult<TCatalog>
                        .Fail("Id is required.", ErrorType.Validation);

                var entity = await _repo.GetByIdAsync(new object[] { id }, ct);
                if (entity is null)
                    return ServiceResult<TCatalog>
                        .Fail("Item not found.", ErrorType.NotFound);

                if (string.IsNullOrWhiteSpace(input.Name))
                    return ServiceResult<TCatalog>
                        .Fail("Name is required.", ErrorType.Validation);

                // Solo mapeamos lo que sabemos que existe en CatalogEntityBase.
                // Si tu catálogo tiene más propiedades (como IndexingSource),
                // puedes extender este servicio en una subclase específica.
                entity.Name = input.Name;

                _repo.Update(entity);
                await _uow.SaveChangesAsync(ct);

                return ServiceResult<TCatalog>.Ok(entity, "Catalog item updated.");
            }
            catch (DbUpdateException dbex)
            {
                return ServiceResult<TCatalog>
                    .Fail(dbex.InnerException?.Message ?? dbex.Message, ErrorType.Conflict);
            }
            catch (Exception ex)
            {
                return ServiceResult<TCatalog>
                    .Fail(ex.Message, ErrorType.Unexpected);
            }
        }

        // =============== DELETE ===============

        public async Task<ServiceResult<NoContent>> DeleteAsync(int id, CancellationToken ct = default)
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
    }
}