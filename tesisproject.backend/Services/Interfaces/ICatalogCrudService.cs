using tesisproject.shared.Entities.Base;
using tesisproject.shared.Responses;

namespace tesisproject.backend.Services.Interfaces
{
    /// <summary>
    /// Generic CRUD service for catalog entities (CatalogEntityBase).
    /// Works directly over the entity type; mapping to DTOs can be done
    /// in specialized services if needed.
    /// </summary>
    public interface ICatalogCrudService<TCatalog>
        where TCatalog : CatalogEntityBase
    {
        Task<ServiceResult<IReadOnlyList<TCatalog>>> ListAsync(CancellationToken ct = default);
        Task<ServiceResult<TCatalog>> GetByIdAsync(int id, CancellationToken ct = default);
        Task<ServiceResult<TCatalog>> CreateAsync(TCatalog entity, CancellationToken ct = default);
        Task<ServiceResult<TCatalog>> UpdateAsync(int id, TCatalog input, CancellationToken ct = default);
        Task<ServiceResult<NoContent>> DeleteAsync(int id, CancellationToken ct = default);
    }
}
