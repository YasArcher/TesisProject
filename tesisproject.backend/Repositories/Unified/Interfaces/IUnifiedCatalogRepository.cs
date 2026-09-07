using tesisproject.backend.Repositories.Interfaces;
using System.Linq.Expressions;
using tesisproject.shared.DTOs.Filters;
using tesisproject.backend.Data.UnifiedEntities.Base;

namespace tesisproject.backend.Repositories.Unified.Interfaces
{
    public interface IUnifiedCatalogRepository<T> : IGenericRepository<T> where T : CatalogEntityBase
    {
        /// <summary>
        /// Lista elementos del catálogo con filtros opcionales.
        /// </summary>
        Task<IReadOnlyList<T>> ListAsync(
            bool onlyActives = true,
            Expression<Func<T, bool>>? where = null,
            Func<IQueryable<T>, IQueryable<T>>? include = null,
            CancellationToken ct = default);

        /// <summary>
        /// Devuelve un diccionario por Id con filtros opcionales (útil para "rehidratar" por lote).
        /// </summary>
        Task<Dictionary<int, T>> GetByIdsAsync(
            IEnumerable<int> ids,
            Func<IQueryable<T>, IQueryable<T>>? include = null,
            CancellationToken ct = default);

        /// <summary>
        /// Obtiene un registro por nombre (match exacto), con opcional "solo activos".
        /// </summary>
        Task<T?> GetByNameAsync(
            string name,
            bool onlyActives = true,
            Func<IQueryable<T>, IQueryable<T>>? include = null,
            CancellationToken ct = default);

        /// <summary>
        /// Verifica si existe un nombre (con exclusión de Id para Update).
        /// </summary>
        Task<bool> NameExistsAsync(
            string name,
            int? excludeId = null,
            CancellationToken ct = default);

        /// <summary>
        /// Key/Value para combos, con búsqueda opcional (term), límite (take),
        /// y posibilidad de filtrar con expresión (p.ej. por CountryId, Flag, etc.).
        /// </summary>
        Task<List<KeyValueItemDTO>> GetKeyValuesAsync(string? term = null, int? take = null, CancellationToken ct = default);

        Task<bool> HasReferencesAsync(int id, CancellationToken ct = default);

    }
}
