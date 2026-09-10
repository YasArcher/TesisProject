using Microsoft.EntityFrameworkCore;
using tesisproject.backend.Data;
using tesisproject.backend.Repositories.Implementations;
using tesisproject.backend.Repositories.Unified.Interfaces;
using tesisproject.backend.Data.UnifiedEntities.Auth;

namespace tesisproject.backend.Repositories.Unified.Implementations
{
    public class UnifiedAppUserRepository : GenericRepository<AppUser>, IUnifiedAppUserRepository
    {
        public UnifiedAppUserRepository(UnifiedDideDbContext ctx) : base(ctx)
        {
        }

        public async Task<AppUser?> GetByIdUserAsync(int idUser, CancellationToken ct = default)
        {
            // IdUser es la PK interna usada por las tablas de negocio
            return await Query(asNoTracking: true)
                .FirstOrDefaultAsync(u => u.IdUser == idUser, ct);
        }

        public async Task<AppUser?> GetByLocalIdAsync(int idLocal, CancellationToken ct = default)
        {
            // IdLocal apunta al IdentityUser<int>.Id del ASP local
            return await Query(asNoTracking: true)
                .FirstOrDefaultAsync(u => u.IdLocal == idLocal, ct);
        }

        public async Task<AppUser?> GetByAspIdAsync(int idAsp, CancellationToken ct = default)
        {
            // IdAsp será el Id del ASP de la universidad cuando lo tengas
            return await Query(asNoTracking: true)
                .FirstOrDefaultAsync(u => u.IdAsp == idAsp, ct);
        }
    }
}