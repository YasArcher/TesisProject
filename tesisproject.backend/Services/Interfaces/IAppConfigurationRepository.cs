using tesisproject.backend.Repositories.Interfaces;
using tesisproject.shared.Entities.Core;

namespace tesisproject.backend.Services.Interfaces
{
    public interface IAppConfigurationRepository : IGenericRepository<AppConfiguration>
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