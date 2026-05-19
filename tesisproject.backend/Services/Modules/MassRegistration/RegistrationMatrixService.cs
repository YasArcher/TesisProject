using Microsoft.EntityFrameworkCore;
using tesisproject.backend.Data;
using tesisproject.backend.Data.Entities;
using tesisproject.backend.Services.Interfaces;
using tesisproject.shared.DTOs.MassRegistration;

namespace tesisproject.backend.Services.Implementations
{
    public class RegistrationMatrixService : IRegistrationMatrixService
    {
        private readonly AppDbContext _db;
        private readonly IBulkImportService _bulkImportService;

        public RegistrationMatrixService(AppDbContext db, IBulkImportService bulkImportService)
        {
            _db = db;
            _bulkImportService = bulkImportService;
        }

        public async Task<List<RegistrationMatrixSummaryDto>> GetMatricesAsync(int take = 50, CancellationToken ct = default)
            => await GetMatricesAsync(take, null, true, ct);

        public async Task<List<RegistrationMatrixSummaryDto>> GetMatricesAsync(int take, string? ownerUserId, bool includeAll, CancellationToken ct = default)
        {
            var query = _db.RegistrationMatrices
                .AsNoTracking()
                .AsQueryable();

            if (!includeAll)
            {
                var normalizedOwner = NormalizeReference(ownerUserId);
                if (string.IsNullOrWhiteSpace(normalizedOwner))
                {
                    return [];
                }

                query = query.Where(x => x.CreatedByUserId == normalizedOwner);
            }

            return await query
                .OrderByDescending(x => x.UpdatedAt ?? x.CreatedAt)
                .ThenByDescending(x => x.RegistrationMatrixId)
                .Take(Math.Max(1, take))
                .Select(x => new RegistrationMatrixSummaryDto
                {
                    RegistrationMatrixId = x.RegistrationMatrixId,
                    Name = x.Name,
                    EntityName = x.EntityName,
                    Status = x.Status,
                    ColumnCount = x.Columns.Count,
                    RowCount = x.Rows.Count,
                    LastImportBatchId = x.LastImportBatchId,
                    CreatedAt = x.CreatedAt,
                    UpdatedAt = x.UpdatedAt
                })
                .ToListAsync(ct);
        }

        public async Task<RegistrationMatrixDetailDto?> GetMatrixAsync(int matrixId, CancellationToken ct = default)
            => await GetMatrixAsync(matrixId, null, true, ct);

        public async Task<RegistrationMatrixDetailDto?> GetMatrixAsync(int matrixId, string? ownerUserId, bool includeAll, CancellationToken ct = default)
        {
            var query = _db.RegistrationMatrices
                .AsNoTracking()
                .Include(x => x.Columns)
                    .ThenInclude(x => x.Field)
                .Include(x => x.Rows.OrderBy(r => r.RowNumber))
                    .ThenInclude(x => x.Cells)
                .AsQueryable();

            if (!includeAll)
            {
                var normalizedOwner = NormalizeReference(ownerUserId);
                if (string.IsNullOrWhiteSpace(normalizedOwner))
                {
                    return null;
                }

                query = query.Where(x => x.CreatedByUserId == normalizedOwner);
            }

            var matrix = await query.FirstOrDefaultAsync(x => x.RegistrationMatrixId == matrixId, ct);

            return matrix is null ? null : MapDetail(matrix);
        }

        public async Task<RegistrationMatrixDetailDto> CreateMatrixAsync(CreateRegistrationMatrixRequest request, CancellationToken ct = default)
            => await CreateMatrixAsync(request, null, ct);

        public async Task<RegistrationMatrixDetailDto> CreateMatrixAsync(CreateRegistrationMatrixRequest request, string? ownerUserId, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                throw new InvalidOperationException("Debes asignar un nombre a la matriz.");
            }

            var matrix = new RegistrationMatrix
            {
                Name = request.Name.Trim(),
                EntityName = string.IsNullOrWhiteSpace(request.EntityName) ? "Article" : request.EntityName.Trim(),
                Status = "Draft",
                Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim(),
                CreatedByUserId = NormalizeReference(ownerUserId),
                CreatedAt = DateTime.UtcNow
            };

            _db.RegistrationMatrices.Add(matrix);
            await _db.SaveChangesAsync(ct);

            if (request.FieldIds.Count > 0)
            {
                await AddColumnsInternalAsync(matrix.RegistrationMatrixId, request.FieldIds, ct);
            }

            return await GetMatrixAsync(matrix.RegistrationMatrixId, ct) ?? new RegistrationMatrixDetailDto();
        }

        public async Task<RegistrationMatrixDetailDto?> UpdateMatrixAsync(int matrixId, UpdateRegistrationMatrixRequest request, CancellationToken ct = default)
        {
            var matrix = await _db.RegistrationMatrices.FirstOrDefaultAsync(x => x.RegistrationMatrixId == matrixId, ct);
            if (matrix is null)
            {
                return null;
            }

            matrix.Name = string.IsNullOrWhiteSpace(request.Name) ? matrix.Name : request.Name.Trim();
            matrix.Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim();
            matrix.Status = string.IsNullOrWhiteSpace(request.Status) ? matrix.Status : request.Status.Trim();
            matrix.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);

            return await GetMatrixAsync(matrixId, ct);
        }

        public async Task<RegistrationMatrixDetailDto?> AddColumnsAsync(int matrixId, AddRegistrationMatrixColumnsRequest request, CancellationToken ct = default)
        {
            var exists = await _db.RegistrationMatrices.AnyAsync(x => x.RegistrationMatrixId == matrixId, ct);
            if (!exists)
            {
                return null;
            }

            await AddColumnsInternalAsync(matrixId, request.FieldIds, ct);
            return await GetMatrixAsync(matrixId, ct);
        }

        public async Task<RegistrationMatrixDetailDto?> UpdateColumnOrderAsync(int matrixId, int columnId, UpdateRegistrationMatrixColumnOrderRequest request, CancellationToken ct = default)
        {
            var columns = await _db.RegistrationMatrixColumns
                .Where(x => x.RegistrationMatrixId == matrixId)
                .OrderBy(x => x.DisplayOrder)
                .ThenBy(x => x.RegistrationMatrixColumnId)
                .ToListAsync(ct);

            if (columns.Count == 0)
            {
                return null;
            }

            var target = columns.FirstOrDefault(x => x.RegistrationMatrixColumnId == columnId);
            if (target is null)
            {
                return null;
            }

            var requestedOrder = Math.Max(1, request.DisplayOrder);
            requestedOrder = Math.Min(requestedOrder, columns.Count);

            columns.Remove(target);
            columns.Insert(requestedOrder - 1, target);

            for (var index = 0; index < columns.Count; index++)
            {
                columns[index].DisplayOrder = index + 1;
                if (columns[index].RegistrationMatrixColumnId == columnId)
                {
                    columns[index].WidthUnits = Math.Max(1, request.WidthUnits);
                }
            }

            var matrix = await _db.RegistrationMatrices.FirstOrDefaultAsync(x => x.RegistrationMatrixId == matrixId, ct);
            if (matrix is not null)
            {
                matrix.UpdatedAt = DateTime.UtcNow;
            }

            await _db.SaveChangesAsync(ct);
            return await GetMatrixAsync(matrixId, ct);
        }

        public async Task<bool> RemoveColumnAsync(int matrixId, int columnId, CancellationToken ct = default)
        {
            var column = await _db.RegistrationMatrixColumns
                .FirstOrDefaultAsync(x => x.RegistrationMatrixId == matrixId && x.RegistrationMatrixColumnId == columnId, ct);

            if (column is null)
            {
                return false;
            }

            var relatedCells = await _db.RegistrationMatrixCells
                .Include(x => x.Row)
                .Where(x => x.Row != null && x.Row.RegistrationMatrixId == matrixId && x.FieldId == column.FieldId)
                .ToListAsync(ct);

            if (relatedCells.Count > 0)
            {
                _db.RegistrationMatrixCells.RemoveRange(relatedCells);
            }

            _db.RegistrationMatrixColumns.Remove(column);
            await _db.SaveChangesAsync(ct);
            return true;
        }

        public async Task<RegistrationMatrixDetailDto?> AddRowAsync(int matrixId, CancellationToken ct = default)
        {
            var matrix = await _db.RegistrationMatrices
                .Include(x => x.Rows)
                .FirstOrDefaultAsync(x => x.RegistrationMatrixId == matrixId, ct);

            if (matrix is null)
            {
                return null;
            }

            var nextRowNumber = matrix.Rows.Count == 0 ? 1 : matrix.Rows.Max(x => x.RowNumber) + 1;
            _db.RegistrationMatrixRows.Add(new RegistrationMatrixRow
            {
                RegistrationMatrixId = matrixId,
                RowNumber = nextRowNumber,
                Status = "Draft",
                CreatedAt = DateTime.UtcNow
            });

            matrix.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);
            return await GetMatrixAsync(matrixId, ct);
        }

        public async Task<RegistrationMatrixDetailDto?> UpdateCellAsync(int matrixId, int rowId, UpdateRegistrationMatrixCellRequest request, CancellationToken ct = default)
        {
            var row = await _db.RegistrationMatrixRows
                .Include(x => x.Matrix)
                .FirstOrDefaultAsync(x => x.RegistrationMatrixId == matrixId && x.RegistrationMatrixRowId == rowId, ct);

            if (row is null)
            {
                return null;
            }

            var columnExists = await _db.RegistrationMatrixColumns
                .AnyAsync(x => x.RegistrationMatrixId == matrixId && x.FieldId == request.FieldId, ct);

            if (!columnExists)
            {
                throw new InvalidOperationException("La columna no pertenece a la matriz seleccionada.");
            }

            var cell = await _db.RegistrationMatrixCells
                .FirstOrDefaultAsync(x => x.RegistrationMatrixRowId == rowId && x.FieldId == request.FieldId, ct);

            var cleaned = string.IsNullOrWhiteSpace(request.RawValue) ? null : request.RawValue.Trim();

            if (cell is null && cleaned is not null)
            {
                _db.RegistrationMatrixCells.Add(new RegistrationMatrixCell
                {
                    RegistrationMatrixRowId = rowId,
                    FieldId = request.FieldId,
                    RawValue = cleaned,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
            }
            else if (cell is not null && cleaned is null)
            {
                _db.RegistrationMatrixCells.Remove(cell);
            }
            else if (cell is not null)
            {
                cell.RawValue = cleaned;
                cell.UpdatedAt = DateTime.UtcNow;
            }

            row.UpdatedAt = DateTime.UtcNow;
            if (row.Matrix is not null)
            {
                row.Matrix.UpdatedAt = DateTime.UtcNow;
            }

            await _db.SaveChangesAsync(ct);
            return await GetMatrixAsync(matrixId, ct);
        }

        public async Task<bool> DeleteRowAsync(int matrixId, int rowId, CancellationToken ct = default)
        {
            var row = await _db.RegistrationMatrixRows
                .FirstOrDefaultAsync(x => x.RegistrationMatrixId == matrixId && x.RegistrationMatrixRowId == rowId, ct);

            if (row is null)
            {
                return false;
            }

            _db.RegistrationMatrixRows.Remove(row);
            await _db.SaveChangesAsync(ct);
            return true;
        }

        public async Task<RegistrationMatrixSubmissionResultDto> SubmitToStagingAsync(int matrixId, SubmitRegistrationMatrixRequest request, string? userId, CancellationToken ct = default)
        {
            var matrix = await _db.RegistrationMatrices
                .Include(x => x.Columns)
                    .ThenInclude(x => x.Field)
                .Include(x => x.Rows.OrderBy(r => r.RowNumber))
                    .ThenInclude(x => x.Cells)
                .FirstOrDefaultAsync(x => x.RegistrationMatrixId == matrixId, ct);

            if (matrix is null)
            {
                throw new InvalidOperationException("La matriz seleccionada no existe.");
            }

            var detail = MapDetail(matrix);
            var batchResult = await _bulkImportService.CreateBatchFromMatrixAsync(detail, request.ValidateAfterCreate, request.UseAuthorWorkflow, userId, request.RowParticipants, ct);

            matrix.Status = "SentToStaging";
            matrix.LastImportBatchId = batchResult.Batch.Summary.ImportBatchId;
            matrix.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);

            return new RegistrationMatrixSubmissionResultDto
            {
                Message = $"La matriz {matrix.Name} se envió a staging.",
                Matrix = await GetMatrixAsync(matrixId, ct) ?? detail,
                BatchResult = batchResult
            };
        }

        private async Task AddColumnsInternalAsync(int matrixId, IEnumerable<int> fieldIds, CancellationToken ct)
        {
            var normalizedIds = fieldIds.Distinct().ToList();
            if (normalizedIds.Count == 0)
            {
                return;
            }

            var existingColumns = await _db.RegistrationMatrixColumns
                .Where(x => x.RegistrationMatrixId == matrixId)
                .ToListAsync(ct);

            var existingFieldIds = existingColumns.Select(x => x.FieldId).ToHashSet();
            var orderMap = normalizedIds
                .Select((fieldId, index) => new { fieldId, index })
                .ToDictionary(x => x.fieldId, x => x.index);

            var fields = await _db.FieldCatalogEntries
                .AsNoTracking()
                .Where(x => normalizedIds.Contains(x.FieldId))
                .Where(x => x.EntityName == "Article" || x.EntityName == "ArticleParticipant")
                .ToListAsync(ct);

            var nextOrder = existingColumns.Count == 0 ? 1 : existingColumns.Max(x => x.DisplayOrder) + 1;
            foreach (var field in fields.OrderBy(x => orderMap[x.FieldId]))
            {
                if (existingFieldIds.Contains(field.FieldId))
                {
                    continue;
                }

                _db.RegistrationMatrixColumns.Add(new RegistrationMatrixColumn
                {
                    RegistrationMatrixId = matrixId,
                    FieldId = field.FieldId,
                    DisplayOrder = nextOrder++,
                    WidthUnits = field.FieldKey is "Title" or "JournalName" or "PublicationUrl" or "Affiliation" ? 2 : 1,
                    CreatedAt = DateTime.UtcNow
                });
            }

            var matrix = await _db.RegistrationMatrices.FirstAsync(x => x.RegistrationMatrixId == matrixId, ct);
            matrix.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);
        }

        private static RegistrationMatrixDetailDto MapDetail(RegistrationMatrix matrix)
        {
            return new RegistrationMatrixDetailDto
            {
                Summary = new RegistrationMatrixSummaryDto
                {
                    RegistrationMatrixId = matrix.RegistrationMatrixId,
                    Name = matrix.Name,
                    EntityName = matrix.EntityName,
                    Status = matrix.Status,
                    ColumnCount = matrix.Columns.Count,
                    RowCount = matrix.Rows.Count,
                    LastImportBatchId = matrix.LastImportBatchId,
                    CreatedAt = matrix.CreatedAt,
                    UpdatedAt = matrix.UpdatedAt
                },
                Notes = matrix.Notes,
                Columns = matrix.Columns
                    .OrderBy(x => x.DisplayOrder)
                    .ThenBy(x => x.RegistrationMatrixColumnId)
                    .Select(x => new RegistrationMatrixColumnDto
                    {
                        RegistrationMatrixColumnId = x.RegistrationMatrixColumnId,
                        FieldId = x.FieldId,
                        EntityName = x.Field?.EntityName ?? string.Empty,
                        FieldKey = x.Field?.FieldKey ?? string.Empty,
                        FieldLabel = x.Field?.FieldLabel ?? string.Empty,
                        DataType = x.Field?.DataType ?? string.Empty,
                        IsRequired = x.Field?.IsRequired ?? false,
                        IsDynamic = x.Field?.IsDynamic ?? false,
                        DisplayOrder = x.DisplayOrder,
                        WidthUnits = x.WidthUnits
                    })
                    .ToList(),
                Rows = matrix.Rows
                    .OrderBy(x => x.RowNumber)
                    .Select(x => new RegistrationMatrixRowDto
                    {
                        RegistrationMatrixRowId = x.RegistrationMatrixRowId,
                        RowNumber = x.RowNumber,
                        Status = x.Status,
                        Cells = x.Cells
                            .OrderBy(c => c.FieldId)
                            .Select(c => new RegistrationMatrixCellDto
                            {
                                FieldId = c.FieldId,
                                RawValue = c.RawValue
                            })
                            .ToList()
                    })
                    .ToList()
            };
        }

        private static string? NormalizeReference(string? value)
            => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
