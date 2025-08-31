using tesisproject.shared.Entities.Core;

namespace tesisproject.backend.Repositories.Interfaces
{
    public interface IGroupRepository
    {
        Task AddAsync(Group entity, CancellationToken ct = default);
        Task<Group?> GetByIdAsync(Guid id, bool includeMembers, CancellationToken ct = default);
        Task<bool> NameExistsAsync(string name, CancellationToken ct = default);
        IQueryable<Group> Query(); // para listados/paginación
    }
}
