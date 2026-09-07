using Microsoft.EntityFrameworkCore;
using tesisproject.backend.Data;
using tesisproject.backend.Repositories.Implementations;
using tesisproject.backend.Repositories.Unified.Interfaces;
using tesisproject.backend.Data.UnifiedEntities.Export;

namespace tesisproject.backend.Repositories.Unified.Implementations
{
    public class UnifiedExportTemplateColumnRepository : IUnifiedExportTemplateColumnRepository
    {
        private readonly UnifiedDideDbContext _db;

        public UnifiedExportTemplateColumnRepository(UnifiedDideDbContext db)
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