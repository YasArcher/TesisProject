using tesisproject.backend.Repositories.Unified.Interfaces;
using Microsoft.EntityFrameworkCore;
using tesisproject.backend.Data;
using tesisproject.backend.Repositories.Implementations;
using tesisproject.backend.Data.UnifiedEntities.Core;

namespace tesisproject.backend.Repositories.Unified.Implementations
{
    public class UnifiedAppConfigurationRepository
        : GenericRepository<AppConfiguration>, IUnifiedAppConfigurationRepository
    {
        public UnifiedAppConfigurationRepository(UnifiedDideDbContext ctx) : base(ctx) { }

        public async Task<AppConfiguration?> GetByModuleAndKeyAsync(
            string module,
            string settingKey,
            bool onlyActive = true,
            CancellationToken ct = default)
        {
            var normalizedModule = (module ?? string.Empty).Trim();
            var normalizedKey = (settingKey ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(normalizedModule) || string.IsNullOrWhiteSpace(normalizedKey))
                return null;

            IQueryable<AppConfiguration> q = _db;

            if (onlyActive)
                q = q.Where(x => x.IsActive);

            return await q.AsNoTracking()
                .FirstOrDefaultAsync(
                    x => x.Module == normalizedModule && x.SettingKey == normalizedKey,
                    ct);
        }

        public async Task<IReadOnlyList<AppConfiguration>> ListByModuleAsync(
            string module,
            bool onlyActive = true,
            CancellationToken ct = default)
        {
            var normalizedModule = (module ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(normalizedModule))
                return [];

            IQueryable<AppConfiguration> q = _db
                .Where(x => x.Module == normalizedModule);

            if (onlyActive)
                q = q.Where(x => x.IsActive);

            return await q.AsNoTracking()
                .OrderBy(x => x.DisplayOrder)
                .ThenBy(x => x.SettingKey)
                .ToListAsync(ct);
        }

        public async Task<bool> ExistsAsync(
            string module,
            string settingKey,
            int? excludeId = null,
            CancellationToken ct = default)
        {
            var normalizedModule = (module ?? string.Empty).Trim();
            var normalizedKey = (settingKey ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(normalizedModule) || string.IsNullOrWhiteSpace(normalizedKey))
                return false;

            IQueryable<AppConfiguration> q = _db.Where(x =>
                x.Module == normalizedModule &&
                x.SettingKey == normalizedKey);

            if (excludeId.HasValue)
                q = q.Where(x => x.AppConfigurationId != excludeId.Value);

            return await q.AsNoTracking().AnyAsync(ct);
        }
    }
}
