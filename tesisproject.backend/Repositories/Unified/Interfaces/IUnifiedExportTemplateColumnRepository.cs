using tesisproject.backend.Data.UnifiedEntities.Export;

namespace tesisproject.backend.Repositories.Unified.Interfaces
{
    public interface IUnifiedExportTemplateColumnRepository
    {
        Task<IReadOnlyList<ExportTemplateColumn>> ListByTemplateIdAsync(
            int templateId,
            CancellationToken ct = default);

        Task AddRangeAsync(
            IEnumerable<ExportTemplateColumn> entities,
            CancellationToken ct = default);

        void RemoveRange(IEnumerable<ExportTemplateColumn> entities);

    }
}