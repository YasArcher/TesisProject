using tesisproject.shared.Entities.Export;

namespace tesisproject.backend.Repositories.Interfaces
{
    public interface IExportTemplateColumnRepository
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