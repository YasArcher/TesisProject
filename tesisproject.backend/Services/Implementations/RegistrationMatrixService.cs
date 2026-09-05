using Microsoft.EntityFrameworkCore;
using tesisproject.backend.Data.Articles;
using tesisproject.backend.Data.Articles.Entities;
using tesisproject.backend.Services.Interfaces;
using tesisproject.shared.DTOs.MassRegistration;
using tesisproject.shared.Responses;

namespace tesisproject.backend.Services.Implementations;

public sealed class RegistrationMatrixService : IRegistrationMatrixService
{
    private readonly ArticlesDbContext _db;

    public RegistrationMatrixService(ArticlesDbContext db) => _db = db;

    public async Task<ServiceResult<List<RegistrationMatrixSummaryDto>>> GetMatricesAsync(int take, string? ownerUserId, bool includeAll, CancellationToken ct = default)
    {
        var query = ApplyOwnership(_db.RegistrationMatrices.AsNoTracking(), ownerUserId, includeAll);
        if (query is null) return ServiceResult<List<RegistrationMatrixSummaryDto>>.Ok([]);

        var result = await query
            .OrderByDescending(x => x.UpdatedAt ?? x.CreatedAt)
            .ThenByDescending(x => x.RegistrationMatrixId)
            .Take(Math.Clamp(take, 1, 100))
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

        return ServiceResult<List<RegistrationMatrixSummaryDto>>.Ok(result);
    }

    public async Task<ServiceResult<RegistrationMatrixDetailDto>> GetMatrixAsync(int matrixId, string? ownerUserId, bool includeAll, CancellationToken ct = default)
    {
        var matrix = await FindMatrixAsync(matrixId, ownerUserId, includeAll, asTracking: false, ct);
        return matrix is null ? NotFound() : ServiceResult<RegistrationMatrixDetailDto>.Ok(MapDetail(matrix));
    }

    public async Task<ServiceResult<RegistrationMatrixDetailDto>> CreateMatrixAsync(CreateRegistrationMatrixRequest request, string? ownerUserId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return ServiceResult<RegistrationMatrixDetailDto>.Fail("Asigna un nombre a la matriz antes de continuar.", ErrorType.Validation, "ARTICLES_MATRIX_NAME_REQUIRED");

        var fields = await LoadEligibleFieldsAsync(request.FieldIds.Distinct().ToList(), ct);
        if (fields.Count == 0)
            return ServiceResult<RegistrationMatrixDetailDto>.Fail("Selecciona al menos un campo visible y activo para crear la matriz.", ErrorType.Validation, "ARTICLES_MATRIX_FIELDS_REQUIRED");

        var matrix = new RegistrationMatrix
        {
            Name = request.Name.Trim(),
            EntityName = string.IsNullOrWhiteSpace(request.EntityName) ? "Article" : request.EntityName.Trim(),
            Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim(),
            CreatedByUserId = Normalize(ownerUserId),
            Status = "Draft",
            CreatedAt = DateTime.UtcNow
        };

        var order = 1;
        foreach (var field in fields)
        {
            matrix.Columns.Add(new RegistrationMatrixColumn
            {
                FieldId = field.FieldId,
                DisplayOrder = order++,
                WidthUnits = field.FieldKey is "Title" or "JournalName" or "PublicationUrl" or "Affiliation" ? 2 : 1,
                CreatedAt = DateTime.UtcNow
            });
        }

        _db.RegistrationMatrices.Add(matrix);
        await _db.SaveChangesAsync(ct);
        return await GetMatrixAsync(matrix.RegistrationMatrixId, ownerUserId, includeAll: false, ct);
    }

    public async Task<ServiceResult<RegistrationMatrixDetailDto>> AddRowAsync(int matrixId, string? ownerUserId, bool includeAll, CancellationToken ct = default)
    {
        var matrix = await FindMatrixAsync(matrixId, ownerUserId, includeAll, asTracking: true, ct);
        if (matrix is null) return NotFound();
        if (!IsDraft(matrix)) return Locked<RegistrationMatrixDetailDto>();

        matrix.Rows.Add(new RegistrationMatrixRow
        {
            RowNumber = matrix.Rows.Count == 0 ? 1 : matrix.Rows.Max(x => x.RowNumber) + 1,
            Status = "Draft",
            CreatedAt = DateTime.UtcNow
        });
        matrix.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return await GetMatrixAsync(matrixId, ownerUserId, includeAll, ct);
    }

    public async Task<ServiceResult<RegistrationMatrixDetailDto>> UpdateCellAsync(int matrixId, int rowId, UpdateRegistrationMatrixCellRequest request, string? ownerUserId, bool includeAll, CancellationToken ct = default)
    {
        var matrix = await FindMatrixAsync(matrixId, ownerUserId, includeAll, asTracking: true, ct);
        if (matrix is null) return NotFound();
        if (!IsDraft(matrix)) return Locked<RegistrationMatrixDetailDto>();
        if (!matrix.Columns.Any(x => x.FieldId == request.FieldId))
            return ServiceResult<RegistrationMatrixDetailDto>.Fail("El campo seleccionado no pertenece a la matriz.", ErrorType.Validation, "ARTICLES_MATRIX_FIELD_NOT_ALLOWED");

        var row = matrix.Rows.FirstOrDefault(x => x.RegistrationMatrixRowId == rowId);
        if (row is null) return NotFound();

        var cleaned = string.IsNullOrWhiteSpace(request.RawValue) ? null : request.RawValue.Trim();
        var cell = row.Cells.FirstOrDefault(x => x.FieldId == request.FieldId);
        if (cell is null && cleaned is not null)
        {
            row.Cells.Add(new RegistrationMatrixCell { FieldId = request.FieldId, RawValue = cleaned, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
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
        matrix.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return await GetMatrixAsync(matrixId, ownerUserId, includeAll, ct);
    }

    public async Task<ServiceResult<RegistrationMatrixDeleteResultDto>> DeleteMatrixAsync(int matrixId, string? ownerUserId, bool includeAll, CancellationToken ct = default)
    {
        var matrix = await FindMatrixAsync(matrixId, ownerUserId, includeAll, asTracking: true, ct);
        if (matrix is null)
            return ServiceResult<RegistrationMatrixDeleteResultDto>.Fail("No se encontro la matriz solicitada.", ErrorType.NotFound, "ARTICLES_MATRIX_NOT_FOUND");
        if (!IsDraft(matrix))
            return ServiceResult<RegistrationMatrixDeleteResultDto>.Fail("Solo se pueden eliminar matrices en borrador.", ErrorType.Validation, "ARTICLES_MATRIX_DELETE_LOCKED");

        _db.RegistrationMatrices.Remove(matrix);
        await _db.SaveChangesAsync(ct);
        return ServiceResult<RegistrationMatrixDeleteResultDto>.Ok(new RegistrationMatrixDeleteResultDto { Deleted = true, Message = "La matriz en borrador fue eliminada correctamente." });
    }

    public async Task<ServiceResult<RegistrationMatrixSubmissionResultDto>> SubmitToStagingAsync(int matrixId, SubmitRegistrationMatrixRequest request, string? ownerUserId, bool includeAll, CancellationToken ct = default)
    {
        var matrix = await FindMatrixAsync(matrixId, ownerUserId, includeAll, asTracking: false, ct);
        if (matrix is null)
            return ServiceResult<RegistrationMatrixSubmissionResultDto>.Fail("No se encontro la matriz solicitada.", ErrorType.NotFound, "ARTICLES_MATRIX_NOT_FOUND");

        return ServiceResult<RegistrationMatrixSubmissionResultDto>.Fail(
            "La matriz ya puede prepararse como borrador. El envio a staging se activara cuando integremos BulkImport y Workflow en la fusion.",
            ErrorType.Validation,
            "ARTICLES_MATRIX_STAGING_PENDING");
    }

    private async Task<List<FieldCatalogEntry>> LoadEligibleFieldsAsync(IReadOnlyCollection<int> fieldIds, CancellationToken ct)
    {
        if (fieldIds.Count == 0) return [];
        var order = fieldIds.Select((fieldId, index) => new { fieldId, index }).ToDictionary(x => x.fieldId, x => x.index);
        var fields = await _db.FieldCatalogEntries.AsNoTracking()
            .Where(x => fieldIds.Contains(x.FieldId))
            .Where(x => x.IsActive && x.IsVisible)
            .Where(x => x.EntityName == "Article" || x.EntityName == "ArticleParticipant")
            .ToListAsync(ct);
        return fields.OrderBy(x => order[x.FieldId]).ToList();
    }

    private async Task<RegistrationMatrix?> FindMatrixAsync(int matrixId, string? ownerUserId, bool includeAll, bool asTracking, CancellationToken ct)
    {
        var query = _db.RegistrationMatrices
            .Include(x => x.Columns).ThenInclude(x => x.Field)
            .Include(x => x.Rows.OrderBy(r => r.RowNumber)).ThenInclude(x => x.Cells)
            .AsQueryable();
        if (!asTracking) query = query.AsNoTracking();
        var ownedQuery = ApplyOwnership(query, ownerUserId, includeAll);
        return ownedQuery is null ? null : await ownedQuery.FirstOrDefaultAsync(x => x.RegistrationMatrixId == matrixId, ct);
    }

    private static IQueryable<RegistrationMatrix>? ApplyOwnership(IQueryable<RegistrationMatrix> query, string? ownerUserId, bool includeAll)
    {
        if (includeAll) return query;
        var owner = Normalize(ownerUserId);
        return string.IsNullOrWhiteSpace(owner) ? null : query.Where(x => x.CreatedByUserId == owner);
    }

    private static RegistrationMatrixDetailDto MapDetail(RegistrationMatrix matrix) => new()
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
        Columns = matrix.Columns.OrderBy(x => x.DisplayOrder).ThenBy(x => x.RegistrationMatrixColumnId).Select(x => new RegistrationMatrixColumnDto
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
        }).ToList(),
        Rows = matrix.Rows.OrderBy(x => x.RowNumber).Select(x => new RegistrationMatrixRowDto
        {
            RegistrationMatrixRowId = x.RegistrationMatrixRowId,
            RowNumber = x.RowNumber,
            Status = x.Status,
            Cells = x.Cells.Select(c => new RegistrationMatrixCellDto { FieldId = c.FieldId, RawValue = c.RawValue }).ToList()
        }).ToList()
    };

    private static bool IsDraft(RegistrationMatrix matrix) => matrix.Status.Equals("Draft", StringComparison.OrdinalIgnoreCase) && !matrix.LastImportBatchId.HasValue;
    private static ServiceResult<RegistrationMatrixDetailDto> NotFound() => ServiceResult<RegistrationMatrixDetailDto>.Fail("No se encontro la matriz solicitada.", ErrorType.NotFound, "ARTICLES_MATRIX_NOT_FOUND");
    private static ServiceResult<T> Locked<T>() => ServiceResult<T>.Fail("La matriz ya no esta en borrador y no puede modificarse desde esta vista.", ErrorType.Validation, "ARTICLES_MATRIX_LOCKED");
    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
