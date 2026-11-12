using System.Linq.Expressions;
using System.Text;
using Microsoft.EntityFrameworkCore;
using tesisproject.backend.UnitOfWork.Unified.Interfaces;
using tesisproject.backend.Data.UnifiedEntities.Articles;
using tesisproject.backend.Services.Unified.Interfaces;
using tesisproject.shared.DTOs.Imports;
using tesisproject.shared.DTOs.MassRegistration;
using tesisproject.shared.Responses;

namespace tesisproject.backend.Services.Unified.Implementations;

public sealed class UnifiedRegistrationMatrixService : IUnifiedRegistrationMatrixService
{
    public Task<ServiceResult<RegistrationMatrixDetailDto>> CreateMatrixAsync(CreateRegistrationMatrixRequest request, string? ownerUserId, CancellationToken ct = default)
        => _uow.ExecuteInTransactionAsync(_ => CreateMatrixAsyncCore(request, ownerUserId, ct), ct);

    public Task<ServiceResult<RegistrationMatrixDetailDto>> AddRowAsync(int matrixId, string? ownerUserId, bool includeAll, CancellationToken ct = default)
        => _uow.ExecuteInTransactionAsync(_ => AddRowAsyncCore(matrixId, ownerUserId, includeAll, ct), ct);

    public Task<ServiceResult<RegistrationMatrixDetailDto>> UpdateCellAsync(int matrixId, int rowId, UpdateRegistrationMatrixCellRequest request, string? ownerUserId, bool includeAll, CancellationToken ct = default)
        => _uow.ExecuteInTransactionAsync(_ => UpdateCellAsyncCore(matrixId, rowId, request, ownerUserId, includeAll, ct), ct);

    public Task<ServiceResult<RegistrationMatrixDeleteResultDto>> DeleteMatrixAsync(int matrixId, string? ownerUserId, bool includeAll, CancellationToken ct = default)
        => _uow.ExecuteInTransactionAsync(_ => DeleteMatrixAsyncCore(matrixId, ownerUserId, includeAll, ct), ct);

    private readonly IUnifiedUnitOfWork _uow;
    private readonly IUnifiedArticleUserContext _user;

    public UnifiedRegistrationMatrixService(IUnifiedUnitOfWork uow, IUnifiedArticleUserContext user)
    { _uow = uow; _user = user; }

    public async Task<ServiceResult<List<RegistrationMatrixSummaryDto>>> GetMatricesAsync(int take, string? ownerUserId, bool includeAll, CancellationToken ct = default)
    {
        var predicate = Ownership(ownerUserId, includeAll);
        if (predicate is null) return ServiceResult<List<RegistrationMatrixSummaryDto>>.Ok([]);
        var result = await _uow.ArticleRegistrationMatrices.GetSummariesAsync(predicate, Math.Clamp(take, 1, 100), ct);

        return ServiceResult<List<RegistrationMatrixSummaryDto>>.Ok(result);
    }

    public async Task<ServiceResult<RegistrationMatrixDetailDto>> GetMatrixAsync(int matrixId, string? ownerUserId, bool includeAll, CancellationToken ct = default)
    {
        var matrix = await FindMatrixAsync(matrixId, ownerUserId, includeAll, asTracking: false, ct);
        return matrix is null ? NotFound() : ServiceResult<RegistrationMatrixDetailDto>.Ok(MapDetail(matrix));
    }

    private async Task<ServiceResult<RegistrationMatrixDetailDto>> CreateMatrixAsyncCore(CreateRegistrationMatrixRequest request, string? ownerUserId, CancellationToken ct = default)
    {
        if (Ownership(ownerUserId, false) is null)
            return ServiceResult<RegistrationMatrixDetailDto>.Fail("Se requiere un propietario autenticado válido.", ErrorType.Forbidden, "ARTICLES_MATRIX_OWNER_REQUIRED");
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

        await _uow.ArticleRegistrationMatrices.AddAsync(matrix, ct);
        await _uow.SaveChangesAsync(ct);
        return await GetMatrixAsync(matrix.RegistrationMatrixId, ownerUserId, includeAll: false, ct);
    }

    private async Task<ServiceResult<RegistrationMatrixDetailDto>> AddRowAsyncCore(int matrixId, string? ownerUserId, bool includeAll, CancellationToken ct = default)
    {
        var matrix = await FindMatrixAsync(matrixId, ownerUserId, includeAll, asTracking: true, ct);
        if (matrix is null) return NotFound();
        if (!IsDraft(matrix)) return Locked<RegistrationMatrixDetailDto>();
        if (matrix.Rows.Count >= 250)
            return ServiceResult<RegistrationMatrixDetailDto>.Fail("Cada matriz admite hasta 250 filas.", ErrorType.Validation, "ARTICLES_MATRIX_TOO_LARGE");

        matrix.Rows.Add(new RegistrationMatrixRow
        {
            RowNumber = matrix.Rows.Count == 0 ? 1 : matrix.Rows.Max(x => x.RowNumber) + 1,
            Status = "Draft",
            CreatedAt = DateTime.UtcNow
        });
        matrix.UpdatedAt = DateTime.UtcNow;
        await _uow.SaveChangesAsync(ct);
        return await GetMatrixAsync(matrixId, ownerUserId, includeAll, ct);
    }

    private async Task<ServiceResult<RegistrationMatrixDetailDto>> UpdateCellAsyncCore(int matrixId, int rowId, UpdateRegistrationMatrixCellRequest request, string? ownerUserId, bool includeAll, CancellationToken ct = default)
    {
        var matrix = await FindMatrixAsync(matrixId, ownerUserId, includeAll, asTracking: true, ct);
        if (matrix is null) return NotFound();
        if (!IsDraft(matrix)) return Locked<RegistrationMatrixDetailDto>();
        var column = matrix.Columns.FirstOrDefault(x => x.FieldId == request.FieldId);
        if (column is null)
            return ServiceResult<RegistrationMatrixDetailDto>.Fail("El campo seleccionado no pertenece a la matriz.", ErrorType.Validation, "ARTICLES_MATRIX_FIELD_NOT_ALLOWED");

        var row = matrix.Rows.FirstOrDefault(x => x.RegistrationMatrixRowId == rowId);
        if (row is null) return NotFound();

        var cleaned = string.IsNullOrWhiteSpace(request.RawValue) ? null : request.RawValue.Trim();
        var maxLength = column.Field?.FieldKey switch
        {
            "Doi" => 20,
            "Email" => 30,
            _ => Math.Clamp(column.Field?.MaxLength ?? 4000, 1, 4000)
        };
        if (cleaned?.Length > maxLength)
            return ServiceResult<RegistrationMatrixDetailDto>.Fail($"{column.Field?.FieldLabel ?? "El campo"} admite hasta {maxLength} caracteres.", ErrorType.Validation, "ARTICLES_MATRIX_VALUE_TOO_LONG");
        var cell = row.Cells.FirstOrDefault(x => x.FieldId == request.FieldId);
        if (cell is null && cleaned is not null)
        {
            row.Cells.Add(new RegistrationMatrixCell { FieldId = request.FieldId, RawValue = cleaned, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
        }
        else if (cell is not null && cleaned is null)
        {
            _uow.RegistrationMatrixCells.Remove(cell);
        }
        else if (cell is not null)
        {
            cell.RawValue = cleaned;
            cell.UpdatedAt = DateTime.UtcNow;
        }

        row.UpdatedAt = DateTime.UtcNow;
        matrix.UpdatedAt = DateTime.UtcNow;
        await _uow.SaveChangesAsync(ct);
        return await GetMatrixAsync(matrixId, ownerUserId, includeAll, ct);
    }

    private async Task<ServiceResult<RegistrationMatrixDeleteResultDto>> DeleteMatrixAsyncCore(int matrixId, string? ownerUserId, bool includeAll, CancellationToken ct = default)
    {
        var matrix = await FindMatrixAsync(matrixId, ownerUserId, includeAll, asTracking: true, ct);
        if (matrix is null)
            return ServiceResult<RegistrationMatrixDeleteResultDto>.Fail("No se encontro la matriz solicitada.", ErrorType.NotFound, "ARTICLES_MATRIX_NOT_FOUND");
        if (!IsDraft(matrix))
            return ServiceResult<RegistrationMatrixDeleteResultDto>.Fail("Solo se pueden eliminar matrices en borrador.", ErrorType.Validation, "ARTICLES_MATRIX_DELETE_LOCKED");

        _uow.ArticleRegistrationMatrices.RemoveGraph(matrix);
        await _uow.SaveChangesAsync(ct);
        return ServiceResult<RegistrationMatrixDeleteResultDto>.Ok(new RegistrationMatrixDeleteResultDto { Deleted = true, Message = "La matriz en borrador fue eliminada correctamente." });
    }

    public async Task<ServiceResult<RegistrationMatrixSubmissionResultDto>> SubmitToStagingAsync(int matrixId, SubmitRegistrationMatrixRequest request, string? ownerUserId, bool includeAll, CancellationToken ct = default)
    {
        try
        {
            return await _uow.ExecuteInTransactionAsync(_ => SubmitToStagingCoreAsync(matrixId, request, ownerUserId, includeAll, ct), ct);
        }
        catch (DbUpdateException) when (!ct.IsCancellationRequested)
        {
            var matrix = await FindMatrixAsync(matrixId, ownerUserId, includeAll, asTracking: false, ct);
            var batch = matrix is null ? null : await _uow.ArticleStagingBatches.Query()
                .FirstOrDefaultAsync(x => x.RegistrationMatrixId == matrixId, ct);
            if (matrix is not null && batch is not null)
                return ServiceResult<RegistrationMatrixSubmissionResultDto>.Ok(MapSubmission(matrix, batch, "La matriz ya se encontraba en staging."));
            throw;
        }
    }

    private async Task<ServiceResult<RegistrationMatrixSubmissionResultDto>> SubmitToStagingCoreAsync(int matrixId, SubmitRegistrationMatrixRequest request, string? ownerUserId, bool includeAll, CancellationToken ct)
    {
        var matrix = await FindMatrixAsync(matrixId, ownerUserId, includeAll, asTracking: true, ct);
        if (matrix is null)
            return ServiceResult<RegistrationMatrixSubmissionResultDto>.Fail("No se encontro la matriz solicitada.", ErrorType.NotFound, "ARTICLES_MATRIX_NOT_FOUND");
        if (matrix.Status.Equals("Staged", StringComparison.OrdinalIgnoreCase))
        {
            var existing = await _uow.ArticleStagingBatches.Query().FirstOrDefaultAsync(x =>
                x.RegistrationMatrixId == matrixId, ct);
            return existing is null
                ? ServiceResult<RegistrationMatrixSubmissionResultDto>.Fail("La matriz ya fue enviada pero no se encontro el lote.", ErrorType.Conflict, "ARTICLES_MATRIX_STAGING_INCONSISTENT")
                : ServiceResult<RegistrationMatrixSubmissionResultDto>.Ok(MapSubmission(matrix, existing, "La matriz ya se encontraba en staging."));
        }
        if (!IsDraft(matrix))
            return ServiceResult<RegistrationMatrixSubmissionResultDto>.Fail("Solo se pueden enviar matrices en borrador.", ErrorType.Validation, "ARTICLES_MATRIX_LOCKED");
        if (matrix.Rows.Count == 0 || matrix.Columns.Count == 0)
            return ServiceResult<RegistrationMatrixSubmissionResultDto>.Fail("Agrega al menos una fila y un campo antes de enviar.", ErrorType.Validation, "ARTICLES_MATRIX_EMPTY");
        if (matrix.Rows.Count > 250)
            return ServiceResult<RegistrationMatrixSubmissionResultDto>.Fail("Cada lote admite hasta 250 filas. Divide la captura en varias matrices.", ErrorType.Validation, "ARTICLES_MATRIX_TOO_LARGE");
        if (string.IsNullOrWhiteSpace(matrix.CreatedByUserId))
            return ServiceResult<RegistrationMatrixSubmissionResultDto>.Fail("La matriz requiere un propietario valido.", ErrorType.Forbidden, "ARTICLES_MATRIX_OWNER_REQUIRED");
        if (request is null || !request.ValidateAfterCreate || request.UseAuthorWorkflow)
            return ServiceResult<RegistrationMatrixSubmissionResultDto>.Fail(
                "Este envío solo prepara un lote validado. El envío a revisión todavía no está disponible desde la matriz.",
                ErrorType.Validation, "ARTICLES_MATRIX_WORKFLOW_NOT_AVAILABLE");

        var groups = request.RowParticipants ?? [];
        var rowIds = matrix.Rows.Select(x => x.RegistrationMatrixRowId).ToHashSet();
        if (groups.Count > matrix.Rows.Count || groups.Select(x => x.RegistrationMatrixRowId).Distinct().Count() != groups.Count ||
            groups.Any(g => !rowIds.Contains(g.RegistrationMatrixRowId) || g.Participants is null || g.Participants.Count > 20 ||
                g.Participants.Any(p => p.Cells is null || p.Cells.Count > 30 || p.Cells.Any(c => c.FieldId <= 0 || c.RawValue?.Length > 4000))))
            return ServiceResult<RegistrationMatrixSubmissionResultDto>.Fail("Los participantes enviados no corresponden a las filas de esta matriz.", ErrorType.Validation, "ARTICLES_MATRIX_PARTICIPANTS_INVALID");

        var doiFieldId = matrix.Columns.FirstOrDefault(c => c.Field?.EntityName == "Article" &&
            string.Equals(c.Field.FieldKey, "Doi", StringComparison.OrdinalIgnoreCase))?.FieldId;
        var candidateDois = doiFieldId is null ? [] : matrix.Rows.SelectMany(r => r.Cells)
            .Where(c => c.FieldId == doiFieldId).Select(c => ArticleMatrixStagingPlanner.NormalizeDoi(c.RawValue))
            .Where(x => x is { Length: <= 20 }).Cast<string>().Distinct().ToArray();
        var doiVariants = candidateDois.SelectMany(doi => new[] { doi, $"doi:{doi}", $"https://doi.org/{doi}", $"http://doi.org/{doi}" }).ToArray();
        var registeredDois = candidateDois.Length == 0 ? [] : (await _uow.ArticleReads.Query()
            .Where(a => a.Doi != null && doiVariants.Contains(a.Doi.Trim().ToLower()))
            .Select(a => a.Doi!).ToListAsync(ct))
            .Select(ArticleMatrixStagingPlanner.NormalizeDoi).Where(x => x is not null).Cast<string>().ToArray();
        var rows = ArticleMatrixStagingPlanner.BuildRows(matrix, registeredDois.ToHashSet(StringComparer.OrdinalIgnoreCase), groups);
        if (rows.Any(x => Encoding.UTF8.GetByteCount(x.SnapshotJson) > 16_384) ||
            rows.Sum(x => Encoding.UTF8.GetByteCount(x.SnapshotJson)) > 2_097_152)
            return ServiceResult<RegistrationMatrixSubmissionResultDto>.Fail(
                "El lote contiene demasiado texto. Reduce el contenido de las filas antes de enviarlo.",
                ErrorType.Validation, "ARTICLES_MATRIX_SNAPSHOT_TOO_LARGE");
        var invalidRows = rows.Where(x => x.Status == "Invalid").ToList();
        if (invalidRows.Count > 0)
            return ServiceResult<RegistrationMatrixSubmissionResultDto>.Fail(
                $"Corrige {invalidRows.Count} fila(s) antes de enviar. Primera observacion: fila {invalidRows[0].RowNumber}: {invalidRows[0].ValidationMessage}",
                ErrorType.Validation, "ARTICLES_MATRIX_STAGING_INVALID");

        var batch = new ArticleStagingBatch
        {
            RegistrationMatrixId = matrixId,
            CreatedByUserId = matrix.CreatedByUserId,
            Status = "Staged",
            TotalRows = rows.Count,
            ReadyRows = rows.Count(x => x.Status == "Ready"),
            DuplicateRows = rows.Count(x => x.Status == "Duplicate"),
            InvalidRows = 0,
            Rows = rows
        };
        await _uow.ArticleStagingBatches.AddAsync(batch, ct);
        await _uow.SaveChangesAsync(ct);
        matrix.Status = "Staged";
        matrix.UpdatedAt = DateTime.UtcNow;
        var statuses = rows.ToDictionary(x => x.RegistrationMatrixRowId, x => x.Status);
        foreach (var row in matrix.Rows)
        {
            row.Status = statuses[row.RegistrationMatrixRowId];
            row.UpdatedAt = matrix.UpdatedAt;
        }
        await _uow.SaveChangesAsync(ct);
        return ServiceResult<RegistrationMatrixSubmissionResultDto>.Ok(MapSubmission(matrix, batch,
            $"Lote preparado: {batch.ReadyRows} fila(s) listas y {batch.DuplicateRows} DOI duplicado(s). Ningun articulo se registro aun."));
    }

    public async Task<ServiceResult<BulkImportBatchSummaryDto>> GetStagingSummaryAsync(int matrixId, string? ownerUserId, bool includeAll, CancellationToken ct = default)
    {
        var matrix = await FindMatrixAsync(matrixId, ownerUserId, includeAll, asTracking: false, ct);
        if (matrix is null || !matrix.Status.Equals("Staged", StringComparison.OrdinalIgnoreCase))
            return ServiceResult<BulkImportBatchSummaryDto>.Fail("No se encontro un lote para esta matriz.", ErrorType.NotFound, "ARTICLES_STAGING_NOT_FOUND");
        var batch = await _uow.ArticleStagingBatches.Query().FirstOrDefaultAsync(x =>
            x.RegistrationMatrixId == matrixId, ct);
        return batch is null
            ? ServiceResult<BulkImportBatchSummaryDto>.Fail("No se encontro un lote para esta matriz.", ErrorType.NotFound, "ARTICLES_STAGING_NOT_FOUND")
            : ServiceResult<BulkImportBatchSummaryDto>.Ok(MapBatchSummary(batch));
    }

    private static RegistrationMatrixSubmissionResultDto MapSubmission(RegistrationMatrix matrix, ArticleStagingBatch batch, string message) => new()
    {
        Message = message,
        Matrix = MapDetail(matrix),
        BatchResult = new BulkImportActionResultDto
        {
            Message = message,
            Batch = new BulkImportBatchDetailDto { Summary = MapBatchSummary(batch) },
            InsertedRows = 0,
            DuplicateRows = batch.DuplicateRows,
            FailedRows = batch.InvalidRows
        }
    };

    private static BulkImportBatchSummaryDto MapBatchSummary(ArticleStagingBatch batch) => new()
    {
        ImportBatchId = batch.ArticleStagingBatchId,
        BatchCode = $"MATRIX-{batch.ArticleStagingBatchId:D6}",
        SourceType = "Matrix",
        EntityName = "Article",
        SourceReference = batch.RegistrationMatrixId.ToString(),
        TotalRows = batch.TotalRows,
        ValidRows = batch.ReadyRows,
        DuplicateRows = batch.DuplicateRows,
        ErrorRows = batch.InvalidRows,
        Status = batch.Status,
        StartedAt = batch.CreatedAt,
        CreatedBy = batch.CreatedByUserId
    };

    private async Task<List<FieldCatalogEntry>> LoadEligibleFieldsAsync(IReadOnlyCollection<int> fieldIds, CancellationToken ct)
    {
        if (fieldIds.Count == 0) return [];
        var order = fieldIds.Select((fieldId, index) => new { fieldId, index }).ToDictionary(x => x.fieldId, x => x.index);
        var fields = await _uow.ArticleFields.GetAllAsync(x => fieldIds.Contains(x.FieldId) && x.IsActive && x.IsVisible
            && (x.EntityName == "Article" || x.EntityName == "ArticleParticipant"), ct);
        return fields.OrderBy(x => order[x.FieldId]).ToList();
    }

    private async Task<RegistrationMatrix?> FindMatrixAsync(int matrixId, string? ownerUserId, bool includeAll, bool asTracking, CancellationToken ct)
    {
        var ownership = Ownership(ownerUserId, includeAll);
        if (ownership is null) return null;
        // Choose an explicit data predicate here; repositories make no access decision.
        var owner = Normalize(ownerUserId);
        return await _uow.ArticleRegistrationMatrices.FindWithDetailsAsync(
            includeAll && _user.CanManageAll ? m => m.RegistrationMatrixId == matrixId
                : m => m.RegistrationMatrixId == matrixId && m.CreatedByUserId == owner,
            !asTracking, ct);
    }

    private Expression<Func<RegistrationMatrix, bool>>? Ownership(string? ownerUserId, bool includeAll)
    {
        if (!_user.IsAuthenticated) return null;
        if (includeAll && _user.CanManageAll) return m => true;
        var owner = Normalize(ownerUserId);
        if (owner is null || owner != Normalize(_user.OwnerReference)) return null;
        return m => m.CreatedByUserId == owner;
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
            SourceType = x.Field?.SourceType ?? string.Empty,
            ReferenceTableName = x.Field?.ReferenceTableName,
            IsRequired = x.Field?.IsRequired ?? false,
            IsDynamic = x.Field?.IsDynamic ?? false,
            IsVisible = x.Field?.IsVisible ?? false,
            IsEditable = x.Field?.IsEditable ?? false,
            MaxLength = x.Field?.MaxLength,
            ValidationRule = x.Field?.ValidationRule,
            DisplayOrder = x.DisplayOrder,
            WidthUnits = x.WidthUnits,
            Options = x.Field?.Options
                .OrderBy(option => option.DisplayOrder)
                .ThenBy(option => option.OptionLabel)
                .Select(option => new tesisproject.shared.DTOs.Configuration.DynamicFieldOptionDto
                {
                    DynamicFieldOptionId = option.DynamicFieldOptionId,
                    FieldId = option.FieldId,
                    OptionValue = option.OptionValue,
                    OptionLabel = option.OptionLabel,
                    DisplayOrder = option.DisplayOrder,
                    IsActive = option.IsActive
                })
                .ToList() ?? []
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
