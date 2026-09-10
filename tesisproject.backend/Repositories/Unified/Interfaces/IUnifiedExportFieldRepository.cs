using tesisproject.backend.Data.UnifiedEntities.Export;

namespace tesisproject.backend.Repositories.Unified.Interfaces
{
    public interface IUnifiedExportFieldRepository
    {
        Task<IReadOnlyList<ExportField>> ListAsync(CancellationToken ct = default);
        Task<ExportField?> GetByIdAsync(int id, CancellationToken ct = default);
        Task<ExportField?> GetByKeyAsync(string key, CancellationToken ct = default);

        Task AddAsync(ExportField entity, CancellationToken ct = default);
        void Update(ExportField entity);
        void Remove(ExportField entity);
    }
}