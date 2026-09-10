using tesisproject.backend.Repositories.Interfaces;
using tesisproject.backend.Data.UnifiedEntities.Auth;

namespace tesisproject.backend.Repositories.Unified.Interfaces
{
    public interface IUnifiedAppUserRepository : IGenericRepository<AppUser>
    {
        Task<AppUser?> GetByIdUserAsync(int idUser, CancellationToken ct = default);
        Task<AppUser?> GetByLocalIdAsync(int idLocal, CancellationToken ct = default);
        Task<AppUser?> GetByAspIdAsync(int idAsp, CancellationToken ct = default);
    }
}
