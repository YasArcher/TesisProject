using tesisproject.shared.Entities.Core;

namespace tesisproject.backend.Repositories.Interfaces
{
    public interface IGroupRepository : IGenericRepository<Group>
    {
        /// <summary>
        /// Obtiene un grupo por Id, con opción de incluir sus miembros.
        /// </summary>
        Task<Group?> GetByIdAsync(int id, bool includeMembers, CancellationToken ct = default);

        /// <summary>
        /// Verifica existencia de nombre (case-insensitive). Útil para crear/editar.
        /// </summary>
        Task<bool> NameExistsAsync(string name, CancellationToken ct = default);
    }
}
