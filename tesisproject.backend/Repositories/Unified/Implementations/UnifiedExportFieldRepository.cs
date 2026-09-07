using Microsoft.EntityFrameworkCore;
using tesisproject.backend.Data;
using tesisproject.backend.Repositories.Implementations;
using tesisproject.backend.Repositories.Unified.Interfaces;
using tesisproject.backend.Data.UnifiedEntities.Export;

namespace tesisproject.backend.Repositories.Unified.Implementations
{
    public class UnifiedExportFieldRepository : IUnifiedExportFieldRepository
    {
        private readonly UnifiedDideDbContext _db;

        public UnifiedExportFieldRepository(UnifiedDideDbContext db)
        {
            _db = db;
        }

        public async Task<IReadOnlyList<ExportField>> ListAsync(CancellationToken ct = default)
        {
            return await _db.ExportFields
                .AsNoTracking()
                .OrderBy(f => f.DisplayName)
                .ToListAsync(ct);
        }

        public async Task<ExportField?> GetByIdAsync(int id, CancellationToken ct = default)
        {
            return await _db.ExportFields
                .FirstOrDefaultAsync(f => f.Id == id, ct);
        }

        public async Task<ExportField?> GetByKeyAsync(string key, CancellationToken ct = default)
        {
            return await _db.ExportFields
                .FirstOrDefaultAsync(f => f.Key == key, ct);
        }

        public async Task AddAsync(ExportField entity, CancellationToken ct = default)
        {
            await _db.ExportFields.AddAsync(entity, ct);
        }

        public void Update(ExportField entity)
        {
            _db.ExportFields.Update(entity);
        }

        public void Remove(ExportField entity)
        {
            _db.ExportFields.Remove(entity);
        }
    }
}