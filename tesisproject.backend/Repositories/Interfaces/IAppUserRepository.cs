using tesisproject.shared.Entities.Auth;

namespace tesisproject.backend.Repositories.Interfaces
{
    public interface IAppUserRepository : IGenericRepository<AppUser>
    {
        Task<AppUser?> GetByIdUserAsync(int idUser, CancellationToken ct = default);
        Task<AppUser?> GetByLocalIdAsync(int idLocal, CancellationToken ct = default);
        Task<AppUser?> GetByAspIdAsync(int idAsp, CancellationToken ct = default);
    }
}
