using Microsoft.EntityFrameworkCore;
using tesisproject.backend.Data;
using tesisproject.backend.Repositories.Implementations;
using tesisproject.backend.Repositories.Unified.Interfaces;
using tesisproject.backend.Data.UnifiedEntities.Export;

namespace tesisproject.backend.Repositories.Unified.Implementations
{
    public class UnifiedExportTemplateRepository : IUnifiedExportTemplateRepository
    {
        private readonly UnifiedDideDbContext _db;

        public UnifiedExportTemplateRepository(UnifiedDideDbContext db)
        {
            _db = db;
        }

        public async Task<IReadOnlyList<ExportTemplate>> ListAsync(CancellationToken ct = default)
        {
            return await _db.ExportTemplates
                .AsNoTracking()
                .OrderBy(t => t.Name)
                .ToListAsync(ct);
        }

        public async Task<ExportTemplate?> GetDetailByIdAsync(int id, CancellationToken ct = default)
        {
            return await _db.ExportTemplates
                .Include(t => t.Columns)
                    .ThenInclude(c => c.ExportField)   // ← aquí estaba el problema (antes .Field)
                .FirstOrDefaultAsync(t => t.Id == id, ct);
        }

        public async Task<ExportTemplate?> GetByKeyAsync(string key, CancellationToken ct = default)
        {
            return await _db.ExportTemplates
                .FirstOrDefaultAsync(t => t.Key == key, ct);
        }

        public async Task AddAsync(ExportTemplate entity, CancellationToken ct = default)
        {
            await _db.ExportTemplates.AddAsync(entity, ct);
        }

        public void Update(ExportTemplate entity)
        {
            _db.ExportTemplates.Update(entity);
        }

        public void Remove(ExportTemplate entity)
        {
            _db.ExportTemplates.Remove(entity);
        }
    }
}