using tesisproject.backend.Repositories.Interfaces;
using tesisproject.backend.Data.UnifiedEntities.Core;

namespace tesisproject.backend.Repositories.Unified.Interfaces
{
    public interface IUnifiedGroupRepository : IGenericRepository<Group>
    {
        /// <summary>
        /// Obtiene un grupo por Id, con opción de incluir sus miembros.
        /// </summary>
        Task<Group?> GetByIdAsync(int id, bool includeMembers, CancellationToken ct = default);

        /// <summary>
        /// Verifica existencia de nombre (case-insensitive). Útil para crear/editar.
        /// </summary>
        Task<bool> NameExistsAsync(string name, CancellationToken ct = default);
        /// <summary>
        /// Trae los grupos asociados a un proyecto específico.
        /// </summary>

        Task<List<Group>> GetByProjectIdAsync(int projectId, CancellationToken ct = default);

    }
}
