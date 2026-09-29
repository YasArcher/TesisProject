using System.Linq.Expressions;
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
        var matrix = await FindMatrixAsync(matrixId, ownerUserId, includeAll, asTracking: true, ct);
        if (matrix is null)
            return ServiceResult<RegistrationMatrixSubmissionResultDto>.Fail("No se encontro la matriz solicitada.", ErrorType.NotFound, "ARTICLES_MATRIX_NOT_FOUND");

        if (!IsDraft(matrix))
            return ServiceResult<RegistrationMatrixSubmissionResultDto>.Fail("La matriz ya fue enviada y no puede reenviarse desde esta vista.", ErrorType.Validation, "ARTICLES_MATRIX_ALREADY_SUBMITTED");

        if (matrix.Rows.Count == 0)
            return ServiceResult<RegistrationMatrixSubmissionResultDto>.Fail("Agrega al menos una fila antes de enviar la matriz a revision.", ErrorType.Validation, "ARTICLES_MATRIX_ROWS_REQUIRED");

        var missingRequired = matrix.Rows.Sum(row => matrix.Columns.Count(column =>
            column.Field?.IsRequired == true &&
            string.IsNullOrWhiteSpace(row.Cells.FirstOrDefault(cell => cell.FieldId == column.FieldId)?.RawValue)));
        if (missingRequired > 0)
            return ServiceResult<RegistrationMatrixSubmissionResultDto>.Fail($"Completa {missingRequired} campo(s) obligatorio(s) antes de enviar la matriz.", ErrorType.Validation, "ARTICLES_MATRIX_REQUIRED_VALUES_MISSING");

        var now = DateTime.UtcNow;
        matrix.Status = request.UseAuthorWorkflow ? "SubmittedToWorkflow" : "Submitted";
        matrix.LastImportBatchId = matrix.RegistrationMatrixId;
        matrix.UpdatedAt = now;
        foreach (var row in matrix.Rows)
        {
            row.Status = "Submitted";
            row.UpdatedAt = now;
        }

        await _uow.SaveChangesAsync(ct);
        var detail = MapDetail(matrix);
        var result = new RegistrationMatrixSubmissionResultDto
        {
            Message = request.UseAuthorWorkflow
                ? "La matriz fue enviada a la bandeja de revision para continuar el flujo institucional."
                : "La matriz fue marcada como enviada para revision.",
            Matrix = detail,
            BatchResult = BuildSubmissionBatchResult(detail, now)
        };

        return ServiceResult<RegistrationMatrixSubmissionResultDto>.Ok(result);
    }

    private static BulkImportActionResultDto BuildSubmissionBatchResult(RegistrationMatrixDetailDto matrix, DateTime submittedAt)
    {
        var rows = matrix.Rows.Select(row => new BulkImportRowPreviewDto
        {
            ImportBatchRowId = row.RegistrationMatrixRowId,
            RowNumber = row.RowNumber,
            RowStatus = "Submitted",
            Cells = row.Cells.Select(cell =>
            {
                var column = matrix.Columns.FirstOrDefault(x => x.FieldId == cell.FieldId);
                return new BulkImportRowCellDto
                {
                    FieldId = cell.FieldId,
                    EntityName = column?.EntityName ?? string.Empty,
                    FieldKey = column?.FieldKey ?? string.Empty,
                    FieldLabel = column?.FieldLabel ?? string.Empty,
                    RawValue = cell.RawValue,
                    NormalizedValue = cell.RawValue,
                    IsValid = true,
                    ValueType = column?.DataType ?? string.Empty
                };
            }).ToList()
        }).ToList();

        return new BulkImportActionResultDto
        {
            Message = "Borrador enviado a revision.",
            InsertedRows = matrix.Rows.Count,
            Batch = new BulkImportBatchDetailDto
            {
                Summary = new BulkImportBatchSummaryDto
                {
                    ImportBatchId = matrix.Summary.LastImportBatchId ?? matrix.Summary.RegistrationMatrixId,
                    BatchCode = $"MATRIX-{matrix.Summary.RegistrationMatrixId:D6}",
                    SourceType = "RegistrationMatrix",
                    EntityName = matrix.Summary.EntityName,
                    TotalRows = matrix.Rows.Count,
                    SuccessfulRows = matrix.Rows.Count,
                    ValidRows = matrix.Rows.Count,
                    Status = matrix.Summary.Status,
                    StartedAt = submittedAt,
                    FinishedAt = submittedAt,
                    Notes = matrix.Notes
                },
                Rows = rows
            }
        };
    }

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
