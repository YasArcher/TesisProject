using tesisproject.backend.Repositories.Interfaces;
using tesisproject.backend.Data.UnifiedEntities.Core;

namespace tesisproject.backend.Repositories.Unified.Interfaces
{
    public interface IUnifiedAppConfigurationRepository : IGenericRepository<AppConfiguration>
    {
        Task<AppConfiguration?> GetByModuleAndKeyAsync(
            string module,
            string settingKey,
            bool onlyActive = true,
            CancellationToken ct = default);

        Task<IReadOnlyList<AppConfiguration>> ListByModuleAsync(
            string module,
            bool onlyActive = true,
            CancellationToken ct = default);

        Task<bool> ExistsAsync(
            string module,
            string settingKey,
            int? excludeId = null,
            CancellationToken ct = default);
    }
}