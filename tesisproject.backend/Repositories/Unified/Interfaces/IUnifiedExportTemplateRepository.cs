using tesisproject.backend.Data.UnifiedEntities.Export;

namespace tesisproject.backend.Repositories.Unified.Interfaces
{
    public interface IUnifiedExportTemplateRepository
    {
        Task<IReadOnlyList<ExportTemplate>> ListAsync(CancellationToken ct = default);

        /// <summary>
        /// Incluye Columns y Field de cada columna.
        /// </summary>
        Task<ExportTemplate?> GetDetailByIdAsync(int id, CancellationToken ct = default);

        Task<ExportTemplate?> GetByKeyAsync(string key, CancellationToken ct = default);

        Task AddAsync(ExportTemplate entity, CancellationToken ct = default);
        void Update(ExportTemplate entity);
        void Remove(ExportTemplate entity);
    }
}