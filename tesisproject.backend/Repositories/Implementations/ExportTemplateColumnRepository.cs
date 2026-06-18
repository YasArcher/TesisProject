using Microsoft.EntityFrameworkCore;
using tesisproject.backend.Data;
using tesisproject.backend.Repositories.Interfaces;
using tesisproject.shared.Entities.Export;

namespace tesisproject.backend.Repositories.Implementations
{
    public class ExportTemplateColumnRepository : IExportTemplateColumnRepository
    {
        private readonly AppDbContext _db;

        public ExportTemplateColumnRepository(AppDbContext db)
        {
            _db = db;
        }

        public async Task<IReadOnlyList<ExportTemplateColumn>> ListByTemplateIdAsync(
            int templateId,
            CancellationToken ct = default)
        {
            return await _db.ExportTemplateColumns
                .Where(c => c.TemplateId == templateId)
                .OrderBy(c => c.OrderIndex)
                .ToListAsync(ct);
        }

        public async Task AddRangeAsync(
            IEnumerable<ExportTemplateColumn> entities,
            CancellationToken ct = default)
        {
            await _db.ExportTemplateColumns.AddRangeAsync(entities, ct);
        }

        public void RemoveRange(IEnumerable<ExportTemplateColumn> entities)
        {
            _db.ExportTemplateColumns.RemoveRange(entities);
        }
    }
}