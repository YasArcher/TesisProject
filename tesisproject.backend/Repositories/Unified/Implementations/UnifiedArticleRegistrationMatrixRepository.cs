using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using tesisproject.shared.DTOs.MassRegistration;
using tesisproject.backend.Data;
using tesisproject.backend.Data.UnifiedEntities.Articles;
using tesisproject.backend.Repositories.Implementations;
using tesisproject.backend.Repositories.Unified.Interfaces;

namespace tesisproject.backend.Repositories.Unified.Implementations;

public sealed class UnifiedArticleRegistrationMatrixRepository(UnifiedDideDbContext context)
    : GenericRepository<RegistrationMatrix>(context), IUnifiedArticleRegistrationMatrixRepository
{
    public IQueryable<RegistrationMatrix> QueryWithDetails(bool asNoTracking = true) => Query(asNoTracking)
        .Include(m => m.Columns.OrderBy(c => c.DisplayOrder).ThenBy(c => c.RegistrationMatrixColumnId)).ThenInclude(c => c.Field)
        .Include(m => m.Rows.OrderBy(r => r.RowNumber)).ThenInclude(r => r.Cells)
        .AsSingleQuery();

    public Task<RegistrationMatrix?> FindWithDetailsAsync(Expression<Func<RegistrationMatrix, bool>> predicate, bool asNoTracking, CancellationToken ct)
        => QueryWithDetails(asNoTracking).FirstOrDefaultAsync(predicate, ct);

    public Task<List<RegistrationMatrixSummaryDto>> GetSummariesAsync(Expression<Func<RegistrationMatrix, bool>> predicate, int take, CancellationToken ct)
        => Query().Where(predicate).OrderByDescending(m => m.UpdatedAt ?? m.CreatedAt).ThenByDescending(m => m.RegistrationMatrixId)
            .Take(take).Select(m => new RegistrationMatrixSummaryDto
            {
                RegistrationMatrixId = m.RegistrationMatrixId, Name = m.Name, EntityName = m.EntityName, Status = m.Status,
                ColumnCount = m.Columns.Count, RowCount = m.Rows.Count, LastImportBatchId = m.LastImportBatchId,
                CreatedAt = m.CreatedAt, UpdatedAt = m.UpdatedAt
            }).ToListAsync(ct);

    public void RemoveGraph(RegistrationMatrix matrix)
    {
        _ctx.RemoveRange(matrix.Rows.SelectMany(r => r.Cells));
        _ctx.RemoveRange(matrix.Rows);
        _ctx.RemoveRange(matrix.Columns);
        Remove(matrix);
    }
}
