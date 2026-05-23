using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using tesisproject.backend.Data;
using tesisproject.backend.Data.Entities;
using tesisproject.backend.Services.Interfaces;
using tesisproject.backend.Services.Modules.Reporting;
using tesisproject.shared.DTOs.Articles;
using tesisproject.shared.DTOs.ExternalApis;
using tesisproject.shared.DTOs.Imports;
using tesisproject.shared.DTOs.MassRegistration;
using tesisproject.shared.Validation;

namespace tesisproject.backend.Services.Implementations
{
    public class BulkImportService : IBulkImportService
    {
        private readonly AppDbContext _db;
        private readonly IWorkflowService _workflowService;
        private readonly IArticleAggregatePersistenceService _articleAggregatePersistenceService;
        private readonly IReportingRefreshQueue _reportingRefreshQueue;
        private static readonly ExternalDynamicFieldSeed[] ScopusArticleDynamicFields =
        [
            new("ScopusCitationCount", "Scopus: citas", "int", true),
            new("ScopusOpenAccessStatus", "Scopus: estado open access", "string", true),
            new("ScopusLicenseUrl", "Scopus: licencia", "string", false),
            new("ScopusDocumentType", "Scopus: tipo documental", "string", true),
            new("ScopusAbstract", "Scopus: resumen", "string", false),
            new("ScopusLanguage", "Scopus: idioma", "string", true),
            new("ScopusPageRange", "Scopus: rango de paginas", "string", false),
            new("ScopusPublisher", "Scopus: editorial", "string", true),
            new("ScopusEIssn", "Scopus: E-ISSN", "string", true),
            new("ScopusKeywords", "Scopus: palabras clave", "json", true),
            new("ScopusSubjectAreas", "Scopus: areas tematicas", "json", true),
            new("ScopusAuthorAffiliations", "Scopus: afiliaciones de autores", "json", true),
            new("ScopusRawPayload", "Scopus: payload original", "json", false)
        ];

        public BulkImportService(
            AppDbContext db,
            IWorkflowService workflowService,
            IArticleAggregatePersistenceService articleAggregatePersistenceService,
            IReportingRefreshQueue reportingRefreshQueue)
        {
            _db = db;
            _workflowService = workflowService;
            _articleAggregatePersistenceService = articleAggregatePersistenceService;
            _reportingRefreshQueue = reportingRefreshQueue;
        }

        public async Task<List<BulkImportBatchSummaryDto>> GetBatchesAsync(string? entityName = null, int take = 20, CancellationToken ct = default)
        {
            var query = _db.ImportBatches.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(entityName))
            {
                var normalized = entityName.Trim();
                query = query.Where(x => x.EntityName == normalized);
            }

            var batches = await query
                .OrderByDescending(x => x.ImportBatchId)
                .Take(Math.Max(1, take))
                .Select(x => new
                {
                    Batch = x,
                    PendingRows = x.Rows.Count(r => r.RowStatus == "Pending"),
                    ErrorRows = x.Rows.Count(r => r.RowStatus == "Error"),
                    ValidRows = x.Rows.Count(r => r.RowStatus == "Valid"),
                    ProcessedRows = x.Rows.Count(r => r.RowStatus == "Processed")
                })
                .ToListAsync(ct);

            return batches.Select(MapSummary).ToList();
        }

        public async Task<BulkImportTemplateDescriptorDto> GetTemplateDescriptorAsync(BulkImportTemplateRequest request, CancellationToken ct = default)
        {
            await ArticleVenueModelHelper.EnsureVenueCompositeFieldsAsync(_db, ct);
            var articleFields = await ResolveTemplateFieldsAsync("Article", request.ArticleFieldIds, request.UseActiveFormsWhenEmpty, ct);
            var participantFields = await ResolveTemplateFieldsAsync("ArticleParticipant", request.ParticipantFieldIds, request.UseActiveFormsWhenEmpty, ct);

            return new BulkImportTemplateDescriptorDto
            {
                TemplateName = string.IsNullOrWhiteSpace(request.TemplateName) ? $"plantilla_{DateTime.UtcNow:yyyyMMdd_HHmmss}" : request.TemplateName.Trim(),
                EntityName = string.IsNullOrWhiteSpace(request.EntityName) ? "Article" : request.EntityName.Trim(),
                SourceType = string.IsNullOrWhiteSpace(request.SourceType) ? "Excel" : request.SourceType.Trim(),
                ArticleFields = articleFields.Select(MapTemplateField).ToList(),
                ParticipantFields = participantFields.Select(MapTemplateField).ToList()
            };
        }

        public async Task<(byte[] Content, string FileName)> GenerateTemplateAsync(BulkImportTemplateRequest request, CancellationToken ct = default)
        {
            var descriptor = await GetTemplateDescriptorAsync(request, ct);
            var allFields = descriptor.ArticleFields.Concat(descriptor.ParticipantFields).ToList();

            using var workbook = new XLWorkbook();
            var dataSheet = workbook.Worksheets.Add("CargaMasiva");
            var guideSheet = workbook.Worksheets.Add("Guia");

            for (var i = 0; i < allFields.Count; i++)
            {
                dataSheet.Cell(1, i + 1).Value = allFields[i].HeaderKey;
                dataSheet.Cell(1, i + 1).Style.Font.Bold = true;
                dataSheet.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.FromHtml(i < descriptor.ArticleFields.Count ? "#12343b" : "#c17c2f");
                dataSheet.Cell(1, i + 1).Style.Font.FontColor = XLColor.White;
                dataSheet.Cell(2, i + 1).Value = allFields[i].Placeholder ?? allFields[i].HelpText ?? string.Empty;
                dataSheet.Cell(2, i + 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#f7fafb");
                dataSheet.Column(i + 1).Width = Math.Max(18, allFields[i].FieldLabel.Length * 0.9);
            }

            dataSheet.SheetView.FreezeRows(2);

            var guideHeaders = new[] { "Encabezado", "Entidad", "Campo", "Etiqueta", "Tipo", "Requerido", "Dinamico", "Origen", "Ayuda", "Valores sugeridos", "Entrada aceptada", "Se normaliza como" };
            for (var i = 0; i < guideHeaders.Length; i++)
            {
                guideSheet.Cell(1, i + 1).Value = guideHeaders[i];
                guideSheet.Cell(1, i + 1).Style.Font.Bold = true;
                guideSheet.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#2f4858");
                guideSheet.Cell(1, i + 1).Style.Font.FontColor = XLColor.White;
            }

            var sourceFields = await _db.FieldCatalogEntries
                .AsNoTracking()
                .Where(x => allFields.Select(f => f.FieldId).Contains(x.FieldId))
                .Include(x => x.Options)
                .ToListAsync(ct);

            var row = 2;
            foreach (var field in sourceFields.OrderBy(x => x.EntityName).ThenBy(x => x.DisplayOrder).ThenBy(x => x.FieldId))
            {
                var dto = MapTemplateField(field);
                guideSheet.Cell(row, 1).Value = dto.HeaderKey;
                guideSheet.Cell(row, 2).Value = dto.EntityName;
                guideSheet.Cell(row, 3).Value = dto.FieldKey;
                guideSheet.Cell(row, 4).Value = dto.FieldLabel;
                guideSheet.Cell(row, 5).Value = dto.DataType;
                guideSheet.Cell(row, 6).Value = dto.IsRequired ? "Si" : "No";
                guideSheet.Cell(row, 7).Value = dto.IsDynamic ? "Si" : "No";
                guideSheet.Cell(row, 8).Value = dto.SourceType;
                guideSheet.Cell(row, 9).Value = dto.HelpText ?? dto.Placeholder ?? string.Empty;
                guideSheet.Cell(row, 10).Value = await BuildSuggestedValuesAsync(field, ct);
                guideSheet.Cell(row, 11).Value = BuildAcceptedInputHint(field);
                guideSheet.Cell(row, 12).Value = BuildNormalizationHint(field);
                row++;
            }

            guideSheet.Columns().AdjustToContents();

            using var ms = new MemoryStream();
            workbook.SaveAs(ms);
            var fileName = $"{descriptor.TemplateName}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.xlsx";
            return (ms.ToArray(), fileName);
        }

        public async Task<BulkImportBatchDetailDto> CreateBatchFromFileAsync(Stream fileStream, string fileName, string sourceType, string? notes, string? userId, CancellationToken ct = default)
        {
            await ArticleVenueModelHelper.EnsureVenueCompositeFieldsAsync(_db, ct);
            var parsed = await ParseFileAsync(fileStream, fileName, ct);
            var now = DateTime.UtcNow;
            var templateDescriptor = BuildTemplateFromHeaders(parsed.Headers);
            var requiredFieldContract = await BuildRequiredFieldContractAsync(ct);
            var batch = new ImportBatch
            {
                BatchCode = GenerateBatchCode("BATCH-UI"),
                SourceType = string.IsNullOrWhiteSpace(sourceType) ? "Excel" : sourceType.Trim(),
                EntityName = "Article",
                FileName = fileName,
                SourceReference = SerializeBatchMetadata(new BatchTemplateMetadata
                {
                    Origin = "ui-dynamic-import",
                    FileName = fileName,
                    SourceType = sourceType,
                    ArticleFieldIds = templateDescriptor.ArticleFields.Select(x => x.FieldId).ToList(),
                    ParticipantFieldIds = templateDescriptor.ParticipantFields.Select(x => x.FieldId).ToList(),
                    RequiredArticleFieldIds = requiredFieldContract.ArticleFieldIds,
                    RequiredParticipantFieldIds = requiredFieldContract.ParticipantFieldIds
                }, preserveRequiredContractOnlyWhenTrimmed: true),
                TotalRows = parsed.Rows.Count,
                SuccessfulRows = 0,
                ErrorRows = 0,
                Status = parsed.Rows.Count == 0 ? "Empty" : "Pending",
                StartedAt = now,
                CreatedBy = string.IsNullOrWhiteSpace(userId) ? "system" : userId,
                Notes = notes
            };

            _db.ImportBatches.Add(batch);
            await _db.SaveChangesAsync(ct);
            await TryCreateAuthorWorkflowAsync(batch, false, userId, ct);

            var rowsToInsert = new List<ImportBatchRow>(parsed.Rows.Count);
            foreach (var parsedRow in parsed.Rows)
            {
                rowsToInsert.Add(new ImportBatchRow
                {
                    ImportBatchId = batch.ImportBatchId,
                    RowNumber = parsedRow.RowNumber,
                    RowStatus = "Pending",
                    RawJson = JsonSerializer.Serialize(parsedRow.RawData),
                    CreatedAt = now
                });
            }

            _db.ImportBatchRows.AddRange(rowsToInsert);
            await _db.SaveChangesAsync(ct);

            var rowValuesToInsert = new List<ImportBatchRowValue>();
            for (var index = 0; index < parsed.Rows.Count; index++)
            {
                var parsedRow = parsed.Rows[index];
                var savedRow = rowsToInsert[index];

                foreach (var cell in parsedRow.Cells)
                {
                    rowValuesToInsert.Add(new ImportBatchRowValue
                    {
                        ImportBatchRowId = savedRow.ImportBatchRowId,
                        FieldId = cell.Field.FieldId,
                        RawValue = cell.RawValue,
                        NormalizedValue = cell.NormalizedValue,
                        ValueType = cell.ValueType,
                        IsValid = cell.IsValid,
                        ValidationMessage = cell.ValidationMessage,
                        CreatedAt = now
                    });
                }
            }

            if (rowValuesToInsert.Count > 0)
            {
                _db.ImportBatchRowValues.AddRange(rowValuesToInsert);
            }

            var batchErrorsToInsert = new List<ImportBatchError>();
            foreach (var header in parsed.UnmappedHeaders)
            {
                batchErrorsToInsert.Add(new ImportBatchError
                {
                    ImportBatchId = batch.ImportBatchId,
                    ErrorCode = "CLIENT_UNKNOWN_COLUMN",
                    ErrorMessage = $"La columna '{header}' no pudo vincularse con FieldCatalog y será ignorada.",
                    Severity = "Warning",
                    CreatedAt = now
                });
            }

            if (batchErrorsToInsert.Count > 0)
            {
                _db.ImportBatchErrors.AddRange(batchErrorsToInsert);
            }

            await _db.SaveChangesAsync(ct);
            return await GetBatchAsync(batch.ImportBatchId, 25, ct) ?? new BulkImportBatchDetailDto();
        }

        public async Task<BulkImportBatchDetailDto?> GetBatchAsync(int batchId, int previewRows = 25, CancellationToken ct = default)
        {
            var batch = await _db.ImportBatches
                .AsNoTracking()
                .Where(x => x.ImportBatchId == batchId)
                .Select(x => new
                {
                    Batch = x,
                    PendingRows = x.Rows.Count(r => r.RowStatus == "Pending"),
                    ErrorRows = x.Rows.Count(r => r.RowStatus == "Error"),
                    ValidRows = x.Rows.Count(r => r.RowStatus == "Valid"),
                    ProcessedRows = x.Rows.Count(r => r.RowStatus == "Processed")
                })
                .FirstOrDefaultAsync(ct);

            if (batch is null)
            {
                return null;
            }

            var previewLimit = Math.Max(1, previewRows);
            var rows = await _db.ImportBatchRows
                .AsNoTracking()
                .Where(x => x.ImportBatchId == batchId)
                .Where(x => x.RowNumber <= previewLimit
                    || x.RowStatus == "Error"
                    || x.Errors.Any(e => e.Severity == "Error"))
                .OrderBy(x => x.RowNumber)
                .Select(x => new BulkImportRowPreviewDto
                {
                    ImportBatchRowId = x.ImportBatchRowId,
                    RowNumber = x.RowNumber,
                    RowStatus = x.RowStatus,
                    TargetArticleId = x.TargetArticleId,
                    TargetParticipantId = x.TargetParticipantId,
                    RawJson = x.RawJson,
                    Cells = x.Values
                        .OrderBy(v => v.Field != null ? v.Field.EntityName : string.Empty)
                        .ThenBy(v => v.Field != null ? v.Field.DisplayOrder : int.MaxValue)
                        .ThenBy(v => v.FieldId)
                        .Select(v => new BulkImportRowCellDto
                        {
                            ImportBatchRowValueId = v.ImportBatchRowValueId,
                            FieldId = v.FieldId,
                            EntityName = v.Field != null ? v.Field.EntityName : string.Empty,
                            FieldKey = v.Field != null ? v.Field.FieldKey : string.Empty,
                            FieldLabel = v.Field != null ? v.Field.FieldLabel : string.Empty,
                            RawValue = v.RawValue,
                            NormalizedValue = v.NormalizedValue,
                            ValueType = v.ValueType,
                            IsValid = v.IsValid,
                            ValidationMessage = v.ValidationMessage
                        })
                        .ToList(),
                    Errors = x.Errors
                        .OrderBy(e => e.ImportBatchErrorId)
                        .Select(e => new BulkImportErrorDto
                        {
                            ImportBatchErrorId = e.ImportBatchErrorId,
                            ImportBatchRowId = e.ImportBatchRowId,
                            FieldId = e.FieldId,
                            FieldKey = e.Field != null ? e.Field.FieldKey : null,
                            FieldLabel = e.Field != null ? e.Field.FieldLabel : null,
                            ErrorCode = e.ErrorCode,
                            ErrorMessage = e.ErrorMessage,
                            Severity = e.Severity,
                            CreatedAt = e.CreatedAt
                        })
                        .ToList()
                })
                .ToListAsync(ct);

            var errors = await _db.ImportBatchErrors
                .AsNoTracking()
                .Where(x => x.ImportBatchId == batchId)
                .OrderByDescending(x => x.ImportBatchErrorId)
                .Select(x => new BulkImportErrorDto
                {
                    ImportBatchErrorId = x.ImportBatchErrorId,
                    ImportBatchRowId = x.ImportBatchRowId,
                    FieldId = x.FieldId,
                    FieldKey = x.Field != null ? x.Field.FieldKey : null,
                    FieldLabel = x.Field != null ? x.Field.FieldLabel : null,
                    ErrorCode = x.ErrorCode,
                    ErrorMessage = x.ErrorMessage,
                    Severity = x.Severity,
                    CreatedAt = x.CreatedAt
                })
                .ToListAsync(ct);

            return new BulkImportBatchDetailDto
            {
                Summary = MapSummary(batch),
                Template = await ParseTemplateAsync(batch.Batch.SourceReference, ct),
                Rows = rows,
                Errors = errors
            };
        }

        private async Task<BulkImportBatchDetailDto?> GetBatchSummaryOnlyAsync(int batchId, CancellationToken ct)
        {
            var batch = await _db.ImportBatches
                .AsNoTracking()
                .Where(x => x.ImportBatchId == batchId)
                .Select(x => new
                {
                    Batch = x,
                    PendingRows = x.Rows.Count(r => r.RowStatus == "Pending"),
                    ErrorRows = x.Rows.Count(r => r.RowStatus == "Error"),
                    ValidRows = x.Rows.Count(r => r.RowStatus == "Valid"),
                    ProcessedRows = x.Rows.Count(r => r.RowStatus == "Processed")
                })
                .FirstOrDefaultAsync(ct);

            return batch is null
                ? null
                : new BulkImportBatchDetailDto
                {
                    Summary = MapSummary(batch)
                };
        }

        public async Task<BulkImportActionResultDto> CreateBatchFromExternalArticleAsync(ExternalArticleImportRequest request, string? userId, CancellationToken ct = default)
        {
            if (request.Article is null || string.IsNullOrWhiteSpace(request.Article.Title))
            {
                throw new InvalidOperationException("Debes enviar un artículo externo válido para crear el lote.");
            }

            await ArticleVenueModelHelper.EnsureVenueCompositeFieldsAsync(_db, ct);

            var articleFields = await ResolveTemplateFieldsAsync("Article", new List<int>(), true, ct);
            var participantFields = await ResolveTemplateFieldsAsync("ArticleParticipant", new List<int>(), true, ct);
            var externalFields = await EnsureExternalArticleDynamicFieldsAsync(request.ProviderKey, ct);
            var allFields = articleFields
                .Concat(participantFields)
                .Concat(externalFields)
                .GroupBy(x => x.FieldKey, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(x => x.Key, x => x.First(), StringComparer.OrdinalIgnoreCase);
            var requiredFieldContract = await BuildRequiredFieldContractAsync(ct);
            var normalizer = await BulkImportNormalizerCache.CreateAsync(_db, ct);
            var now = DateTime.UtcNow;

            var batch = new ImportBatch
            {
                BatchCode = GenerateBatchCode("BATCH-EXT"),
                SourceType = "ExternalApi",
                EntityName = "Article",
                FileName = $"{(request.ProviderKey ?? "external").Trim()}-single-article",
                SourceReference = BuildExternalSourceReference(
                    request.ProviderKey,
                    request.ProviderName,
                    1,
                    new[] { request.Article },
                    requiredFieldContract),
                TotalRows = 1,
                SuccessfulRows = 0,
                ErrorRows = 0,
                Status = "Pending",
                StartedAt = now,
                CreatedBy = string.IsNullOrWhiteSpace(userId) ? "system" : userId,
                Notes = request.Notes
            };

            _db.ImportBatches.Add(batch);
            await _db.SaveChangesAsync(ct);
            await TryCreateAuthorWorkflowAsync(batch, false, userId, ct);
            AddExternalArticleRow(batch.ImportBatchId, 1, request.ProviderName, request.Article, allFields, normalizer, now);
            await _db.SaveChangesAsync(ct);

            if (request.ValidateAfterCreate)
            {
                var validated = await ValidateBatchAsync(batch.ImportBatchId, ct);
                validated.Message = $"Se creó un lote externo desde {request.ProviderName}. {validated.Message}";
                return validated;
            }

            return new BulkImportActionResultDto
            {
                Message = $"Se creó un lote externo desde {request.ProviderName}.",
                Batch = await GetBatchSummaryOnlyAsync(batch.ImportBatchId, ct) ?? new BulkImportBatchDetailDto()
            };
        }

        public async Task<BulkImportActionResultDto> CreateBatchFromExternalArticlesAsync(ExternalArticlesImportRequest request, string? userId, CancellationToken ct = default)
        {
            var validArticles = (request.Articles ?? new List<ExternalArticlePreviewDto>())
                .Where(x => x is not null && !string.IsNullOrWhiteSpace(x.Title))
                .ToList();

            if (validArticles.Count == 0)
            {
                throw new InvalidOperationException("Debes enviar al menos un artículo externo válido para crear el lote.");
            }

            await ArticleVenueModelHelper.EnsureVenueCompositeFieldsAsync(_db, ct);

            var articleFields = await ResolveTemplateFieldsAsync("Article", new List<int>(), true, ct);
            var participantFields = await ResolveTemplateFieldsAsync("ArticleParticipant", new List<int>(), true, ct);
            var externalFields = await EnsureExternalArticleDynamicFieldsAsync(request.ProviderKey, ct);
            var allFields = articleFields
                .Concat(participantFields)
                .Concat(externalFields)
                .GroupBy(x => x.FieldKey, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(x => x.Key, x => x.First(), StringComparer.OrdinalIgnoreCase);
            var requiredFieldContract = await BuildRequiredFieldContractAsync(ct);
            var normalizer = await BulkImportNormalizerCache.CreateAsync(_db, ct);
            var now = DateTime.UtcNow;

            var batch = new ImportBatch
            {
                BatchCode = GenerateBatchCode("BATCH-EXT"),
                SourceType = "ExternalApi",
                EntityName = "Article",
                FileName = $"{(request.ProviderKey ?? "external").Trim()}-multi-article",
                SourceReference = BuildExternalSourceReference(
                    request.ProviderKey,
                    request.ProviderName,
                    validArticles.Count,
                    validArticles,
                    requiredFieldContract),
                TotalRows = validArticles.Count,
                SuccessfulRows = 0,
                ErrorRows = 0,
                Status = "Pending",
                StartedAt = now,
                CreatedBy = string.IsNullOrWhiteSpace(userId) ? "system" : userId,
                Notes = request.Notes
            };

            _db.ImportBatches.Add(batch);
            await _db.SaveChangesAsync(ct);
            await TryCreateAuthorWorkflowAsync(batch, false, userId, ct);

            for (var index = 0; index < validArticles.Count; index++)
            {
                AddExternalArticleRow(batch.ImportBatchId, index + 1, request.ProviderName, validArticles[index], allFields, normalizer, now);
            }

            await _db.SaveChangesAsync(ct);

            if (request.ValidateAfterCreate)
            {
                var validated = await ValidateBatchAsync(batch.ImportBatchId, ct);
                validated.Message = $"Se creó un lote externo con {validArticles.Count} artículos desde {request.ProviderName}. {validated.Message}";
                return validated;
            }

            return new BulkImportActionResultDto
            {
                Message = $"Se creó un lote externo con {validArticles.Count} artículos desde {request.ProviderName}.",
                Batch = await GetBatchSummaryOnlyAsync(batch.ImportBatchId, ct) ?? new BulkImportBatchDetailDto()
            };
        }

        public async Task<BulkImportActionResultDto> CreateBatchFromAuthorSubmissionAsync(RegisterArticleAggregateRequest request, bool validateAfterCreate, string? userId, CancellationToken ct = default)
        {
            if (request is null || request.Article is null)
            {
                throw new InvalidOperationException("Debes enviar un registro de artículo válido para crear el envío del autor.");
            }

            await ArticleVenueModelHelper.EnsureVenueCompositeFieldsAsync(_db, ct);
            var resolvedCreatedByUserId = await ResolveExistingUserIdAsync(userId, ct);

            var articleFields = await ResolveTemplateFieldsAsync("Article", new List<int>(), true, ct);
            var participantFields = await ResolveTemplateFieldsAsync("ArticleParticipant", new List<int>(), true, ct);
            var allFields = articleFields
                .Concat(participantFields)
                .GroupBy(x => x.FieldKey, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(x => x.Key, x => x.First(), StringComparer.OrdinalIgnoreCase);
            var requiredFieldContract = await BuildRequiredFieldContractAsync(ct);
            var normalizer = await BulkImportNormalizerCache.CreateAsync(_db, ct);
            var now = DateTime.UtcNow;
            var primaryParticipant = request.Participants
                .OrderByDescending(x => x.IsPrimaryAuthor)
                .ThenBy(x => x.Index <= 0 ? int.MaxValue : x.Index)
                .FirstOrDefault();

            var batch = new ImportBatch
            {
                BatchCode = GenerateBatchCode("BATCH-AUTH"),
                SourceType = "AuthorSubmission",
                EntityName = "Article",
                FileName = $"author-submission-{DateTime.UtcNow:yyyyMMddHHmmss}",
                SourceReference = SerializeBatchMetadata(new BatchTemplateMetadata
                {
                    Origin = "author-registration",
                    FileName = request.Article.Title,
                    SourceType = "AuthorSubmission",
                    ArticleFieldIds = articleFields.Select(x => x.FieldId).Distinct().ToList(),
                    ParticipantFieldIds = participantFields.Select(x => x.FieldId).Distinct().ToList(),
                    RequiredArticleFieldIds = requiredFieldContract.ArticleFieldIds,
                    RequiredParticipantFieldIds = requiredFieldContract.ParticipantFieldIds
                }, preserveRequiredContractOnlyWhenTrimmed: true),
                TotalRows = 1,
                SuccessfulRows = 0,
                ErrorRows = 0,
                Status = "Pending",
                StartedAt = now,
                CreatedBy = string.IsNullOrWhiteSpace(userId) ? "system" : userId,
                CreatedByUserId = resolvedCreatedByUserId,
                Notes = "Registro enviado por autor para revisión en workflow."
            };

            _db.ImportBatches.Add(batch);
            await _db.SaveChangesAsync(ct);
            await TryCreateAuthorWorkflowAsync(batch, true, userId, ct);

            var row = new ImportBatchRow
            {
                ImportBatchId = batch.ImportBatchId,
                RowNumber = 1,
                RowStatus = "Pending",
                CreatedAt = now
            };

            _db.ImportBatchRows.Add(row);
            await _db.SaveChangesAsync(ct);

            var rawData = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
            var cellsToCreate = new List<ImportBatchRowValue>();

            void AddCell(string fieldKey, string? rawValue)
            {
                if (string.IsNullOrWhiteSpace(rawValue) || !allFields.TryGetValue(fieldKey, out var field))
                {
                    return;
                }

                var cleaned = rawValue.Trim();
                var normalized = NormalizeValue(field, cleaned, normalizer);
                rawData[BuildHeaderKey(field)] = cleaned;
                cellsToCreate.Add(new ImportBatchRowValue
                {
                    ImportBatchRowId = row.ImportBatchRowId,
                    FieldId = field.FieldId,
                    RawValue = cleaned,
                    NormalizedValue = normalized.NormalizedValue,
                    ValueType = normalized.ValueType,
                    IsValid = normalized.IsValid,
                    ValidationMessage = normalized.ValidationMessage,
                    CreatedAt = now
                });
            }

            AddCell("Title", request.Article.Title);
            AddCell("Doi", request.Article.Doi);
            AddCell("Year", request.Article.Year?.ToString(CultureInfo.InvariantCulture));
            AddCell("PublishedAt", request.Article.PublishedAt?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
            AddCell("PageCount", request.Article.PageCount?.ToString(CultureInfo.InvariantCulture));
            AddCell("PublicationUrl", request.Article.PublicationUrl);
            AddCell("IsProjectResult", request.Article.IsProjectResult ? "true" : "false");
            AddCell("HasInterculturalComponent", request.Article.HasInterculturalComponent ? "true" : "false");
            AddCell("ProceedingsName", request.Article.ProceedingsName);
            AddCell("Proceedings", request.Article.Proceedings);
            AddCell("EventName", request.Article.EventName);
            AddCell("GroupName", request.Article.GroupName);
            AddCell("Filiacion", request.Article.Filiacion);
            AddCell("AcademicTermId", request.Article.AcademicTermId?.ToString(CultureInfo.InvariantCulture));
            AddCell("PublicationStatusId", request.Article.PublicationStatusId?.ToString(CultureInfo.InvariantCulture));
            AddCell("ResearchLineId", request.Article.ResearchLineId?.ToString(CultureInfo.InvariantCulture));
            AddCell("FacultyId", request.Article.FacultyId?.ToString(CultureInfo.InvariantCulture));
            AddCell("BroadFieldId", request.Article.BroadFieldId?.ToString(CultureInfo.InvariantCulture));
            AddCell("SpecificFieldId", request.Article.SpecificFieldId?.ToString(CultureInfo.InvariantCulture));
            AddCell("DetailedFieldId", request.Article.DetailedFieldId?.ToString(CultureInfo.InvariantCulture));
            AddCell("IsOpenAccess", request.Article.IsOpenAccess ? "true" : "false");
            AddCell("ExternalSource", request.Article.ExternalSource);
            AddCell("ExternalId", request.Article.ExternalId);

            AddCell("JournalName", request.Venue?.JournalName);
            AddCell("IssnCode", request.Venue?.IssnCode);
            AddCell("IssueNumber", request.Venue?.IssueNumber);
            AddCell("VolumeNumber", request.Venue?.VolumeNumber);
            AddCell("JournalUrl", request.Venue?.JournalUrl);
            AddCell("Sjr", request.VenueMetric?.Sjr?.ToString(CultureInfo.InvariantCulture));
            AddCell("Quartile", request.VenueMetric?.Quartile);

            if (primaryParticipant is not null)
            {
                AddCell("Index", primaryParticipant.Index <= 0 ? "1" : primaryParticipant.Index.ToString(CultureInfo.InvariantCulture));
                AddCell("Identificacion", primaryParticipant.Identificacion);
                AddCell("Nombre", primaryParticipant.Nombre);
                AddCell("Participacion", primaryParticipant.Participacion);
                AddCell("ParticipantType", primaryParticipant.ParticipantType);
                AddCell("InstitutionalPersonId", primaryParticipant.InstitutionalPersonId?.ToString(CultureInfo.InvariantCulture));
                AddCell("IsPrimaryAuthor", primaryParticipant.IsPrimaryAuthor ? "true" : "false");
                AddCell("Email", primaryParticipant.Email);
                AddCell("Orcid", primaryParticipant.Orcid);
                AddCell("Affiliation", primaryParticipant.Affiliation);
                AddCell("ExternalAuthorId", primaryParticipant.ExternalAuthorId);
            }

            AddDynamicCells(request.DynamicFields, "Article", allFields, rawData, cellsToCreate, row.ImportBatchRowId, normalizer, now);
            AddDynamicCells(primaryParticipant?.DynamicFields ?? new List<DynamicFieldValueInputDto>(), "ArticleParticipant", allFields, rawData, cellsToCreate, row.ImportBatchRowId, normalizer, now);

            row.RawJson = JsonSerializer.Serialize(new
            {
                request.FormKey,
                request.Article,
                request.Venue,
                request.VenueMetric,
                request.DynamicFields,
                request.Participants
            });

            if (cellsToCreate.Count > 0)
            {
                _db.ImportBatchRowValues.AddRange(cellsToCreate);
            }

            if (request.Participants.Count > 1)
            {
                _db.ImportBatchErrors.Add(new ImportBatchError
                {
                    ImportBatchId = batch.ImportBatchId,
                    ImportBatchRowId = row.ImportBatchRowId,
                    ErrorCode = "AUTHOR_SUBMISSION_PARTICIPANTS_PENDING",
                    ErrorMessage = $"El envío incluye {request.Participants.Count} participantes. Esta fase inicial del workflow conserva el detalle completo en RawJson y expone como editable el participante principal mientras se completa la fase multiparticipante del staging.",
                    Severity = "Warning",
                    CreatedAt = now
                });
            }

            await _db.SaveChangesAsync(ct);

            if (validateAfterCreate)
            {
                var validated = await ValidateBatchAsync(batch.ImportBatchId, ct);
                validated.Message = $"Se creó el envío del autor para revisión. {validated.Message}";
                return validated;
            }

            return new BulkImportActionResultDto
            {
                Message = "Se creó el envío del autor para revisión.",
                Batch = await GetBatchAsync(batch.ImportBatchId, 25, ct) ?? new BulkImportBatchDetailDto()
            };
        }

        public async Task<BulkImportActionResultDto> CreateBatchFromMatrixAsync(RegistrationMatrixDetailDto matrix, bool validateAfterCreate, bool useAuthorWorkflow, string? userId, IReadOnlyList<RegistrationMatrixRowParticipantDto>? rowParticipants = null, CancellationToken ct = default)
        {
            if (matrix is null || matrix.Summary is null)
            {
                throw new InvalidOperationException("La matriz seleccionada no es válida.");
            }

            var validRows = (matrix.Rows ?? new List<RegistrationMatrixRowDto>())
                .OrderBy(x => x.RowNumber)
                .ToList();

            if (validRows.Count == 0)
            {
                throw new InvalidOperationException("La matriz no tiene filas para enviar a staging.");
            }

            await ArticleVenueModelHelper.EnsureVenueCompositeFieldsAsync(_db, ct);

            var participantFieldIds = (rowParticipants ?? [])
                .SelectMany(x => x.Participants ?? [])
                .SelectMany(x => x.Cells ?? [])
                .Select(x => x.FieldId)
                .Distinct()
                .ToList();

            var columnFieldIds = (matrix.Columns ?? new List<RegistrationMatrixColumnDto>())
                .Select(x => x.FieldId)
                .Concat(participantFieldIds)
                .Distinct()
                .ToList();

            var columnOrder = (matrix.Columns ?? new List<RegistrationMatrixColumnDto>())
                .Select((column, index) => new { column.FieldId, index })
                .GroupBy(x => x.FieldId)
                .ToDictionary(x => x.Key, x => x.First().index);

            var fields = await _db.FieldCatalogEntries
                .AsNoTracking()
                .Include(x => x.Options)
                .Where(x => columnFieldIds.Contains(x.FieldId))
                .Where(x => x.EntityName == "Article" || x.EntityName == "ArticleParticipant")
                .ToListAsync(ct);

            if (fields.Count == 0)
            {
                throw new InvalidOperationException("La matriz no tiene columnas válidas para construir el lote.");
            }

            var fieldsById = fields.ToDictionary(x => x.FieldId);
            var requiredFieldContract = await BuildRequiredFieldContractAsync(ct);
            var now = DateTime.UtcNow;

            var batch = new ImportBatch
            {
                BatchCode = GenerateBatchCode("BATCH-MTX"),
                SourceType = useAuthorWorkflow ? "AuthorMatrixSubmission" : "MatrixDraft",
                EntityName = string.IsNullOrWhiteSpace(matrix.Summary.EntityName) ? "Article" : matrix.Summary.EntityName,
                FileName = $"{matrix.Summary.Name.Trim()}.matrix",
                SourceReference = SerializeBatchMetadata(new BatchTemplateMetadata
                {
                    Origin = useAuthorWorkflow ? "author-registration-matrix" : "registration-matrix",
                    FileName = matrix.Summary.Name,
                    SourceType = useAuthorWorkflow ? "AuthorMatrixSubmission" : "MatrixDraft",
                    ArticleFieldIds = fields.Where(x => x.EntityName == "Article").OrderBy(x => columnOrder.TryGetValue(x.FieldId, out var articleOrder) ? articleOrder : int.MaxValue).Select(x => x.FieldId).ToList(),
                    ParticipantFieldIds = fields.Where(x => x.EntityName == "ArticleParticipant").OrderBy(x => columnOrder.TryGetValue(x.FieldId, out var participantOrder) ? participantOrder : int.MaxValue).ThenBy(x => x.DisplayOrder).Select(x => x.FieldId).ToList(),
                    RequiredArticleFieldIds = requiredFieldContract.ArticleFieldIds,
                    RequiredParticipantFieldIds = useAuthorWorkflow && (rowParticipants?.Count ?? 0) > 0 ? [] : requiredFieldContract.ParticipantFieldIds
                }, preserveRequiredContractOnlyWhenTrimmed: true),
                TotalRows = validRows.Count,
                SuccessfulRows = 0,
                ErrorRows = 0,
                Status = "Pending",
                StartedAt = now,
                CreatedBy = string.IsNullOrWhiteSpace(userId) ? "system" : userId,
                Notes = $"Lote creado desde matriz: {matrix.Summary.Name}"
            };

            _db.ImportBatches.Add(batch);
            await _db.SaveChangesAsync(ct);
            await TryCreateAuthorWorkflowAsync(batch, useAuthorWorkflow, userId, ct);

            var batchRows = validRows.Select(row => new ImportBatchRow
            {
                ImportBatchId = batch.ImportBatchId,
                RowNumber = row.RowNumber,
                RowStatus = "Pending",
                CreatedAt = now
            }).ToList();

            _db.ImportBatchRows.AddRange(batchRows);
            await _db.SaveChangesAsync(ct);

            var normalizer = await BulkImportNormalizerCache.CreateAsync(_db, ct);
            var rowValues = new List<ImportBatchRowValue>();
            var participantsByRowId = (rowParticipants ?? [])
                .GroupBy(x => x.RegistrationMatrixRowId)
                .ToDictionary(x => x.Key, x => x.First().Participants ?? []);

            for (var index = 0; index < validRows.Count; index++)
            {
                var matrixRow = validRows[index];
                var batchRow = batchRows[index];
                var rawData = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

                foreach (var cell in matrixRow.Cells.Where(x => !string.IsNullOrWhiteSpace(x.RawValue)))
                {
                    if (!fieldsById.TryGetValue(cell.FieldId, out var field))
                    {
                        continue;
                    }

                    var cleaned = cell.RawValue!.Trim();
                    var normalized = NormalizeValue(field, cleaned, normalizer);
                    rawData[BuildHeaderKey(field)] = cleaned;
                    rowValues.Add(new ImportBatchRowValue
                    {
                        ImportBatchRowId = batchRow.ImportBatchRowId,
                        FieldId = field.FieldId,
                        RawValue = cleaned,
                        NormalizedValue = normalized.NormalizedValue,
                        ValueType = normalized.ValueType,
                        IsValid = normalized.IsValid,
                        ValidationMessage = normalized.ValidationMessage,
                        CreatedAt = now
                    });
                }

                batchRow.RawJson = useAuthorWorkflow && participantsByRowId.Count > 0
                    ? JsonSerializer.Serialize(BuildAggregateRequestFromMatrixRow(matrixRow, fieldsById, participantsByRowId))
                    : rawData.Count == 0 ? null : JsonSerializer.Serialize(rawData);
            }

            if (rowValues.Count > 0)
            {
                _db.ImportBatchRowValues.AddRange(rowValues);
            }

            await _db.SaveChangesAsync(ct);

            if (validateAfterCreate)
            {
                var validated = await ValidateBatchAsync(batch.ImportBatchId, ct);
                validated.Message = $"Se creó un lote desde la matriz {matrix.Summary.Name}. {validated.Message}";
                return validated;
            }

            return new BulkImportActionResultDto
            {
                Message = $"Se creó un lote desde la matriz {matrix.Summary.Name}.",
                Batch = await GetBatchAsync(batch.ImportBatchId, 25, ct) ?? new BulkImportBatchDetailDto()
            };
        }

        private static RegisterArticleAggregateRequest BuildAggregateRequestFromMatrixRow(
            RegistrationMatrixRowDto matrixRow,
            Dictionary<int, FieldCatalogEntry> fieldsById,
            Dictionary<int, List<RegistrationMatrixParticipantDto>> participantsByRowId)
        {
            var request = new RegisterArticleAggregateRequest
            {
                FormKey = "ArticleManualForm"
            };

            foreach (var cell in matrixRow.Cells.Where(x => !string.IsNullOrWhiteSpace(x.RawValue)))
            {
                if (!fieldsById.TryGetValue(cell.FieldId, out var field))
                {
                    continue;
                }

                ApplyMatrixCellToAggregate(request, field, cell.RawValue!.Trim());
            }

            if (participantsByRowId.TryGetValue(matrixRow.RegistrationMatrixRowId, out var participants))
            {
                var index = 1;
                foreach (var participantDraft in participants.OrderBy(x => x.Index <= 0 ? int.MaxValue : x.Index))
                {
                    var participant = new ArticleParticipantAggregateDto
                    {
                        Index = participantDraft.Index <= 0 ? index : participantDraft.Index,
                        Participacion = index == 1 ? "Autor" : "Coautor",
                        ParticipantType = "Docente",
                        IsPrimaryAuthor = index == 1
                    };

                    foreach (var cell in participantDraft.Cells.Where(x => !string.IsNullOrWhiteSpace(x.RawValue)))
                    {
                        if (!fieldsById.TryGetValue(cell.FieldId, out var field))
                        {
                            continue;
                        }

                        ApplyMatrixParticipantCell(participant, field, cell.RawValue!.Trim());
                    }

                    request.Participants.Add(participant);
                    index++;
                }
            }

            return request;
        }

        private static void ApplyMatrixCellToAggregate(RegisterArticleAggregateRequest request, FieldCatalogEntry field, string value)
        {
            if (!string.Equals(field.EntityName, "Article", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            switch (ResolveMatrixArticleKey(field))
            {
                case "Title": request.Article.Title = value; break;
                case "Doi": request.Article.Doi = value; break;
                case "Year": request.Article.Year = TryParseShort(value); break;
                case "PublishedAt": request.Article.PublishedAt = TryParseDate(value); break;
                case "PageCount": request.Article.PageCount = TryParseInt(value); break;
                case "PublicationUrl": request.Article.PublicationUrl = value; break;
                case "IsProjectResult": request.Article.IsProjectResult = TryParseBool(value); break;
                case "HasInterculturalComponent": request.Article.HasInterculturalComponent = TryParseBool(value); break;
                case "ProceedingsName": request.Article.ProceedingsName = value; break;
                case "Proceedings": request.Article.Proceedings = value; break;
                case "EventName": request.Article.EventName = value; break;
                case "GroupName": request.Article.GroupName = value; break;
                case "Filiacion": request.Article.Filiacion = value; break;
                case "AcademicTermId": request.Article.AcademicTermId = TryParseInt(value); break;
                case "PublicationStatusId": request.Article.PublicationStatusId = TryParseByte(value); break;
                case "ResearchLineId": request.Article.ResearchLineId = TryParseInt(value); break;
                case "FacultyId": request.Article.FacultyId = TryParseInt(value); break;
                case "BroadFieldId": request.Article.BroadFieldId = TryParseInt(value); break;
                case "SpecificFieldId": request.Article.SpecificFieldId = TryParseInt(value); break;
                case "DetailedFieldId": request.Article.DetailedFieldId = TryParseInt(value); break;
                case "IsOpenAccess": request.Article.IsOpenAccess = TryParseBool(value); break;
                case "ExternalSource": request.Article.ExternalSource = value; break;
                case "ExternalId": request.Article.ExternalId = value; break;
                case "JournalName": request.Venue.JournalName = value; break;
                case "IssnCode": request.Venue.IssnCode = value; break;
                case "IssueNumber": request.Venue.IssueNumber = value; break;
                case "VolumeNumber": request.Venue.VolumeNumber = value; break;
                case "JournalUrl": request.Venue.JournalUrl = value; break;
                case "VenueType": request.Venue.Type = value; break;
                case "Sjr": request.VenueMetric.Sjr = TryParseDecimal(value); break;
                case "Quartile": request.VenueMetric.Quartile = value; break;
                default:
                    if (field.IsDynamic)
                    {
                        request.DynamicFields.Add(BuildDynamicValue(field, value));
                    }

                    break;
            }
        }

        private static string ResolveMatrixArticleKey(FieldCatalogEntry field)
        {
            if (string.Equals(field.FieldKey, "VenueId", StringComparison.OrdinalIgnoreCase)
                || ArticleVenueModelHelper.IsCompositeVenueField(field.FieldKey))
            {
                return field.FieldKey;
            }

            var physical = field.PhysicalColumnName ?? string.Empty;
            var physicalTable = field.PhysicalTableName ?? string.Empty;
            var label = field.FieldLabel ?? string.Empty;

            if (IsJournalNameField(field, physicalTable, physical, label))
            {
                return "JournalName";
            }

            if (IsIssnField(field, physicalTable, physical, label))
            {
                return "IssnCode";
            }

            return string.IsNullOrWhiteSpace(physical) ? field.FieldKey : physical;
        }

        private static bool IsJournalNameField(FieldCatalogEntry field, string physicalTable, string physical, string label)
        {
            var key = field.FieldKey ?? string.Empty;
            var isVenueTable = string.Equals(physicalTable, "dbo.Venues", StringComparison.OrdinalIgnoreCase)
                || string.Equals(physicalTable, "Venues", StringComparison.OrdinalIgnoreCase);

            return string.Equals(key, "JournalName", StringComparison.OrdinalIgnoreCase)
                || string.Equals(key, "VenueName", StringComparison.OrdinalIgnoreCase)
                || string.Equals(key, "NombreRevista", StringComparison.OrdinalIgnoreCase)
                || (isVenueTable && string.Equals(physical, "Name", StringComparison.OrdinalIgnoreCase))
                || label.Contains("nombre de revista", StringComparison.OrdinalIgnoreCase)
                || label.Equals("revista", StringComparison.OrdinalIgnoreCase)
                || label.Equals("journal", StringComparison.OrdinalIgnoreCase)
                || label.Contains("journal name", StringComparison.OrdinalIgnoreCase)
                || (label.Contains("revista", StringComparison.OrdinalIgnoreCase)
                    && label.Contains("nombre", StringComparison.OrdinalIgnoreCase));
        }

        private static bool IsIssnField(FieldCatalogEntry field, string physicalTable, string physical, string label)
        {
            var key = field.FieldKey ?? string.Empty;
            var isVenueTable = string.Equals(physicalTable, "dbo.Venues", StringComparison.OrdinalIgnoreCase)
                || string.Equals(physicalTable, "Venues", StringComparison.OrdinalIgnoreCase);

            return string.Equals(key, "IssnCode", StringComparison.OrdinalIgnoreCase)
                || string.Equals(key, "Issn", StringComparison.OrdinalIgnoreCase)
                || string.Equals(key, "EIssn", StringComparison.OrdinalIgnoreCase)
                || (isVenueTable && physical.Contains("issn", StringComparison.OrdinalIgnoreCase))
                || label.Contains("issn", StringComparison.OrdinalIgnoreCase);
        }

        private static void ApplyMatrixParticipantCell(ArticleParticipantAggregateDto participant, FieldCatalogEntry field, string value)
        {
            if (!string.Equals(field.EntityName, "ArticleParticipant", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            switch (ResolveMatrixParticipantKey(field))
            {
                case "Index": participant.Index = TryParseInt(value) ?? participant.Index; break;
                case "Identificacion": participant.Identificacion = value; break;
                case "Nombre": participant.Nombre = value; break;
                case "Participacion": participant.Participacion = value; break;
                case "ParticipantType": participant.ParticipantType = value; break;
                case "InstitutionalPersonId": participant.InstitutionalPersonId = TryParseInt(value); break;
                case "IsPrimaryAuthor": participant.IsPrimaryAuthor = TryParseBool(value); break;
                case "Email": participant.Email = value; break;
                case "Orcid": participant.Orcid = value; break;
                case "Affiliation": participant.Affiliation = value; break;
                case "ExternalAuthorId": participant.ExternalAuthorId = value; break;
                default:
                    if (field.IsDynamic)
                    {
                        participant.DynamicFields.Add(BuildDynamicValue(field, value));
                    }

                    break;
            }
        }

        private static string ResolveMatrixParticipantKey(FieldCatalogEntry field)
            => string.IsNullOrWhiteSpace(field.PhysicalColumnName) ? field.FieldKey : field.PhysicalColumnName;

        private static void ApplyStagingRowValuesToAggregate(
            RegisterArticleAggregateRequest request,
            IEnumerable<ImportBatchRowValue> rowValues)
        {
            request.Article ??= new ArticleAggregateCoreDto();
            request.Venue ??= new ArticleVenueInputDto();
            request.VenueMetric ??= new ArticleVenueMetricInputDto();
            request.DynamicFields ??= new List<DynamicFieldValueInputDto>();
            request.Participants ??= new List<ArticleParticipantAggregateDto>();

            foreach (var value in rowValues
                         .Where(x => x.Field is not null)
                         .OrderBy(x => x.Field!.EntityName)
                         .ThenBy(x => x.Field!.DisplayOrder)
                         .ThenBy(x => x.FieldId))
            {
                var field = value.Field!;
                var effectiveValue = !string.IsNullOrWhiteSpace(value.RawValue)
                    ? value.RawValue!.Trim()
                    : value.NormalizedValue?.Trim();

                if (string.IsNullOrWhiteSpace(effectiveValue))
                {
                    continue;
                }

                if (string.Equals(field.EntityName, "Article", StringComparison.OrdinalIgnoreCase))
                {
                    if (field.IsDynamic)
                    {
                        RemoveDynamicValue(request.DynamicFields, field);
                    }

                    ApplyMatrixCellToAggregate(request, field, effectiveValue);
                    continue;
                }

                if (string.Equals(field.EntityName, "ArticleParticipant", StringComparison.OrdinalIgnoreCase))
                {
                    var participant = request.Participants.FirstOrDefault();
                    if (participant is null)
                    {
                        participant = new ArticleParticipantAggregateDto
                        {
                            Index = 1,
                            Participacion = "Autor",
                            IsPrimaryAuthor = true
                        };
                        request.Participants.Add(participant);
                    }

                    if (field.IsDynamic)
                    {
                        RemoveDynamicValue(participant.DynamicFields, field);
                    }

                    ApplyMatrixParticipantCell(participant, field, effectiveValue);
                }
            }
        }

        private static void RemoveDynamicValue(List<DynamicFieldValueInputDto> values, FieldCatalogEntry field)
        {
            values.RemoveAll(x =>
                (x.FieldId.HasValue && x.FieldId.Value == field.FieldId)
                || (!string.IsNullOrWhiteSpace(x.FieldKey)
                    && string.Equals(x.FieldKey, field.FieldKey, StringComparison.OrdinalIgnoreCase)));
        }

        private static DynamicFieldValueInputDto BuildDynamicValue(FieldCatalogEntry field, string value)
            => new()
            {
                FieldId = field.FieldId,
                FieldKey = field.FieldKey,
                ValueString = value,
                ValueInt = TryParseInt(value),
                ValueDecimal = TryParseDecimal(value),
                ValueDate = TryParseDate(value),
                ValueBit = bool.TryParse(value, out var parsed) ? parsed : null
            };

        private static int? TryParseInt(string value)
            => int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) ? parsed : null;

        private static short? TryParseShort(string value)
            => short.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) ? parsed : null;

        private static byte? TryParseByte(string value)
            => byte.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) ? parsed : null;

        private static decimal? TryParseDecimal(string value)
            => decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed) ? parsed : null;

        private static DateTime? TryParseDate(string value)
            => DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed) ? parsed : null;

        private static bool TryParseBool(string value)
            => bool.TryParse(value, out var parsed) ? parsed : value is "1" or "Si" or "Sí" or "si" or "sí";

        private void AddExternalArticleRow(
            int batchId,
            int rowNumber,
            string providerName,
            ExternalArticlePreviewDto article,
            Dictionary<string, FieldCatalogEntry> allFields,
            BulkImportNormalizerCache normalizer,
            DateTime createdAt)
        {
            var authorNames = article.AuthorNames
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (authorNames.Count == 0 && !string.IsNullOrWhiteSpace(article.Authors))
            {
                authorNames = article.Authors
                    .Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();
            }

            var primaryAuthor = authorNames.FirstOrDefault() ?? "Autor externo";
            var row = new ImportBatchRow
            {
                ImportBatchId = batchId,
                RowNumber = rowNumber,
                RowStatus = "Pending",
                CreatedAt = createdAt
            };

            _db.ImportBatchRows.Add(row);

            var rawData = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
            var cellsToCreate = new List<ImportBatchRowValue>();

            void AddCell(string fieldKey, string? rawValue)
            {
                if (string.IsNullOrWhiteSpace(rawValue) || !allFields.TryGetValue(fieldKey, out var field))
                {
                    return;
                }

                var cleaned = rawValue.Trim();
                var normalized = NormalizeValue(field, cleaned, normalizer);
                rawData[BuildHeaderKey(field)] = cleaned;
                cellsToCreate.Add(new ImportBatchRowValue
                {
                    Row = row,
                    FieldId = field.FieldId,
                    RawValue = cleaned,
                    NormalizedValue = normalized.NormalizedValue,
                    ValueType = normalized.ValueType,
                    IsValid = normalized.IsValid,
                    ValidationMessage = normalized.ValidationMessage,
                    CreatedAt = createdAt
                });
            }

            AddCell("Title", article.Title);
            AddCell("Doi", article.Doi);
            AddCell("Year", article.PublicationYear?.ToString(CultureInfo.InvariantCulture));
            AddCell("PublicationUrl", article.SourceUrl);
            AddCell("ExternalSource", providerName);
            AddCell("ExternalId", article.ExternalId ?? article.ScopusId);
            AddCell("JournalName", article.JournalName);
            AddCell("IssnCode", article.IssnCode);
            AddCell("JournalUrl", article.JournalUrl);
            AddCell("VolumeNumber", article.Volume);
            AddCell("IssueNumber", article.Issue);
            AddCell("ScopusCitationCount", article.CitationCount?.ToString(CultureInfo.InvariantCulture));
            AddCell("ScopusOpenAccessStatus", article.OpenAccessStatus);
            AddCell("ScopusLicenseUrl", article.LicenseUrl);
            AddCell("ScopusDocumentType", article.DocumentType);
            AddCell("ScopusAbstract", article.ArticleAbstract);
            AddCell("ScopusLanguage", article.Language);
            AddCell("ScopusPageRange", article.PageRange);
            AddCell("ScopusPublisher", article.Publisher);
            AddCell("ScopusEIssn", article.EIssnCode);
            AddCell("ScopusKeywords", SerializeExternalList(article.Keywords));
            AddCell("ScopusSubjectAreas", SerializeExternalList(article.SubjectAreas));
            AddCell("ScopusAuthorAffiliations", SerializeExternalList(article.AuthorAffiliations));
            AddCell("ScopusRawPayload", JsonSerializer.Serialize(article));

            AddCell("Index", "1");
            AddCell("Nombre", primaryAuthor);
            AddCell("IsPrimaryAuthor", "true");
            AddCell("ParticipantType", "Autor externo");

            _db.ImportBatchRowValues.AddRange(cellsToCreate);
            row.RawJson = BuildExternalArticleRawJson(providerName, article, authorNames, rawData);
        }

        private async Task<List<FieldCatalogEntry>> EnsureExternalArticleDynamicFieldsAsync(string? providerKey, CancellationToken ct)
        {
            if (!string.Equals(providerKey, "scopus", StringComparison.OrdinalIgnoreCase))
            {
                return new List<FieldCatalogEntry>();
            }

            var keys = ScopusArticleDynamicFields
                .Select(x => x.FieldKey)
                .ToList();

            var existingFields = await _db.FieldCatalogEntries
                .Where(x => x.EntityName == "Article" && keys.Contains(x.FieldKey))
                .ToListAsync(ct);

            var existingByKey = existingFields.ToDictionary(x => x.FieldKey, StringComparer.OrdinalIgnoreCase);
            var now = DateTime.UtcNow;
            var nextOrder = await _db.FieldCatalogEntries
                .Where(x => x.EntityName == "Article")
                .Select(x => (int?)x.DisplayOrder)
                .MaxAsync(ct) ?? 0;

            foreach (var seed in ScopusArticleDynamicFields)
            {
                if (existingByKey.ContainsKey(seed.FieldKey))
                {
                    continue;
                }

                nextOrder += 10;
                var field = new FieldCatalogEntry
                {
                    EntityName = "Article",
                    FieldKey = seed.FieldKey,
                    FieldLabel = seed.FieldLabel,
                    DataType = seed.DataType,
                    SourceType = "ExternalApi",
                    IsSystemField = false,
                    IsDynamic = true,
                    IsRequired = false,
                    IsVisible = false,
                    IsEditable = false,
                    IsFilterable = seed.IsFilterable,
                    IsActive = true,
                    DisplayOrder = nextOrder,
                    HelpText = "Campo creado automaticamente para conservar metadatos externos de Scopus. No altera el formulario de registro.",
                    CreatedAt = now
                };

                _db.FieldCatalogEntries.Add(field);
                existingFields.Add(field);
            }

            await _db.SaveChangesAsync(ct);
            return existingFields;
        }

        private static string? SerializeExternalList(IReadOnlyCollection<string>? values)
        {
            var cleaned = values?
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList() ?? new List<string>();

            return cleaned.Count == 0 ? null : JsonSerializer.Serialize(cleaned);
        }

        private static string? BuildExternalArticleRawJson(
            string providerName,
            ExternalArticlePreviewDto article,
            IReadOnlyCollection<string> authorNames,
            Dictionary<string, string?> mappedFields)
        {
            if (article is null && mappedFields.Count == 0)
            {
                return null;
            }

            var payload = new Dictionary<string, string?>(mappedFields, StringComparer.OrdinalIgnoreCase)
            {
                ["__ExternalProvider"] = providerName,
                ["__ExternalPayloadJson"] = JsonSerializer.Serialize(article),
                ["__ExternalAuthorNamesJson"] = JsonSerializer.Serialize(authorNames),
                ["__ExternalAffiliationsJson"] = JsonSerializer.Serialize(article?.AuthorAffiliations ?? new List<string>()),
                ["__ExternalKeywordsJson"] = JsonSerializer.Serialize(article?.Keywords ?? new List<string>()),
                ["__ExternalSubjectAreasJson"] = JsonSerializer.Serialize(article?.SubjectAreas ?? new List<string>())
            };

            return JsonSerializer.Serialize(payload);
        }

        private static string BuildExternalSourceReference(
            string? providerKey,
            string? providerName,
            int articleCount,
            IEnumerable<ExternalArticlePreviewDto> articles,
            RequiredFieldContract requiredFieldContract)
        {
            var preview = articles
                .Where(x => x is not null)
                .Take(3)
                .Select(x => x.ExternalId ?? x.ScopusId ?? x.Doi ?? x.Title)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x!.Trim())
                .ToList();

            return SerializeBatchMetadata(new BatchTemplateMetadata
            {
                Origin = "external-api-explorer",
                SourceType = "ExternalApi",
                ProviderKey = string.IsNullOrWhiteSpace(providerKey) ? "external" : providerKey.Trim(),
                ProviderName = string.IsNullOrWhiteSpace(providerName) ? "Proveedor externo" : providerName.Trim(),
                ArticleCount = articleCount,
                Preview = preview,
                RequiredArticleFieldIds = new List<int>(),
                RequiredParticipantFieldIds = new List<int>()
            }, preserveRequiredContractOnlyWhenTrimmed: true);
        }

        public async Task<BulkImportActionResultDto> CorrectRowAsync(int batchId, int rowId, BulkImportRowCorrectionRequest request, CancellationToken ct = default)
        {
            await ArticleVenueModelHelper.EnsureVenueCompositeFieldsAsync(_db, ct);

            var row = await _db.ImportBatchRows
                .Include(x => x.Batch)
                .Include(x => x.Values)
                    .ThenInclude(x => x.Field)
                        .ThenInclude(x => x!.Options)
                .FirstOrDefaultAsync(x => x.ImportBatchId == batchId && x.ImportBatchRowId == rowId, ct);

            if (row is null)
            {
                throw new InvalidOperationException("La fila seleccionada no existe dentro del lote actual.");
            }

            var requestedFieldIds = request.Cells
                .Select(x => x.FieldId)
                .Distinct()
                .ToList();

            var editableFields = await _db.FieldCatalogEntries
                .AsNoTracking()
                .Include(x => x.Options)
                .Where(x => requestedFieldIds.Contains(x.FieldId) && (x.EntityName == "Article" || x.EntityName == "ArticleParticipant"))
                .ToDictionaryAsync(x => x.FieldId, ct);

            var normalizer = await BulkImportNormalizerCache.CreateAsync(_db, ct);
            var existingValues = row.Values.ToDictionary(x => x.FieldId);
            var rawJson = ParseRawJson(row.RawJson);
            var now = DateTime.UtcNow;

            foreach (var incomingCell in request.Cells)
            {
                if (!editableFields.TryGetValue(incomingCell.FieldId, out var field))
                {
                    continue;
                }

                var cleanedRawValue = string.IsNullOrWhiteSpace(incomingCell.RawValue) ? null : incomingCell.RawValue.Trim();
                var headerKey = BuildHeaderKey(field);

                if (string.IsNullOrWhiteSpace(cleanedRawValue))
                {
                    if (existingValues.TryGetValue(field.FieldId, out var existingValue))
                    {
                        _db.ImportBatchRowValues.Remove(existingValue);
                    }

                    rawJson.Remove(headerKey);
                    continue;
                }

                var normalized = NormalizeValue(field, cleanedRawValue, normalizer);

                if (existingValues.TryGetValue(field.FieldId, out var rowValue))
                {
                    rowValue.RawValue = cleanedRawValue;
                    rowValue.NormalizedValue = normalized.NormalizedValue;
                    rowValue.ValueType = normalized.ValueType;
                    rowValue.IsValid = normalized.IsValid;
                    rowValue.ValidationMessage = normalized.ValidationMessage;
                    rowValue.UpdatedAt = now;
                }
                else
                {
                    _db.ImportBatchRowValues.Add(new ImportBatchRowValue
                    {
                        ImportBatchRowId = row.ImportBatchRowId,
                        FieldId = field.FieldId,
                        RawValue = cleanedRawValue,
                        NormalizedValue = normalized.NormalizedValue,
                        ValueType = normalized.ValueType,
                        IsValid = normalized.IsValid,
                        ValidationMessage = normalized.ValidationMessage,
                        CreatedAt = now,
                        UpdatedAt = now
                    });
                }

                rawJson[headerKey] = cleanedRawValue;
            }

            if (!IsAuthorWorkflowSubmission(row.Batch?.SourceType))
            {
                row.RawJson = rawJson.Count == 0 ? null : JsonSerializer.Serialize(rawJson);
            }
            row.RowStatus = "Pending";
            row.UpdatedAt = now;

            await _db.SaveChangesAsync(ct);

            if (request.RevalidateBatch)
            {
                var validated = await ValidateBatchAsync(batchId, ct);
                validated.Message = $"Se guardaron las correcciones de la fila {row.RowNumber}. {validated.Message}";
                return validated;
            }

            return new BulkImportActionResultDto
            {
                Message = $"Se guardaron las correcciones de la fila {row.RowNumber}.",
                Batch = await GetBatchAsync(batchId, 25, ct) ?? new BulkImportBatchDetailDto()
            };
        }

        public async Task<BulkImportActionResultDto> ValidateBatchAsync(int batchId, CancellationToken ct = default)
        {
            var importBatch = await _db.ImportBatches.FirstOrDefaultAsync(x => x.ImportBatchId == batchId, ct)
                ?? throw new InvalidOperationException("El lote no existe.");

            var isExternalApi = string.Equals(importBatch.SourceType, "ExternalApi", StringComparison.OrdinalIgnoreCase);
            if (isExternalApi)
            {
                await ApplyExternalApiValidationAsync(batchId, ct);
            }
            else
            {
                await ArticleVenueModelHelper.EnsureVenueCompositeFieldsAsync(_db, ct);
                await PrepareVenueReferencesAsync(batchId, ct);
                await _db.Database.ExecuteSqlInterpolatedAsync($"EXEC dbo.sp_ValidateImportBatch_Article {batchId}", ct);
                await _db.Database.ExecuteSqlInterpolatedAsync($"EXEC dbo.sp_ValidateImportBatch_ArticleParticipant {batchId}", ct);
                await PrepareVenueReferencesAsync(batchId, ct);
                await ApplyClientSideValidationAsync(batchId, ct);
            }

            var batch = isExternalApi
                ? await GetBatchSummaryOnlyAsync(batchId, ct) ?? new BulkImportBatchDetailDto()
                : await GetBatchAsync(batchId, 25, ct) ?? new BulkImportBatchDetailDto();

            return new BulkImportActionResultDto
            {
                Message = BuildBatchActionMessage("validación", batch),
                Batch = batch
            };
        }

        public async Task<BulkImportActionResultDto> ProcessBatchAsync(int batchId, CancellationToken ct = default)
        {
            var validation = await ValidateBatchAsync(batchId, ct);
            var batchSourceType = validation.Batch.Summary.SourceType ?? string.Empty;
            var isAuthorSubmission = IsAuthorWorkflowSubmission(batchSourceType);
            var isExternalApi = string.Equals(batchSourceType, "ExternalApi", StringComparison.OrdinalIgnoreCase);

            if (validation.Batch.Summary.ErrorRows > 0)
            {
                throw new InvalidOperationException($"El lote {validation.Batch.Summary.BatchCode} todavía tiene {validation.Batch.Summary.ErrorRows} fila(s) con error. Completa los campos obligatorios y corrige el staging antes de procesarlo.");
            }

            if (!isAuthorSubmission && validation.Batch.Summary.ValidRows <= 0)
            {
                if (validation.Batch.Summary.ProcessedRows > 0
                    && validation.Batch.Summary.ProcessedRows >= validation.Batch.Summary.TotalRows)
                {
                    validation.Message = BuildBatchActionMessage("procesamiento", validation.Batch);
                    return validation;
                }

                throw new InvalidOperationException($"El lote {validation.Batch.Summary.BatchCode} no tiene filas listas para procesar.");
            }

            var canProcess = await _workflowService.CanProcessBatchAsync(batchId, null, ct);
            if (!canProcess)
            {
                throw new InvalidOperationException("El lote todavía no completó el workflow de revisión requerido. Solo un lote aprobado por la validación técnica final puede procesarse.");
            }

            if (isAuthorSubmission)
            {
                var result = await ProcessAuthorSubmissionBatchAsync(batchId, ct);
                QueueReportingRefreshIfProcessed(result, "Procesamiento de envío de autor");
                return result;
            }

            if (isExternalApi)
            {
                var result = await ProcessExternalApiBatchAsync(batchId, ct);
                QueueReportingRefreshIfProcessed(result, "Procesamiento de ingesta externa");
                return result;
            }

            await _db.Database.ExecuteSqlInterpolatedAsync($"EXEC dbo.sp_ProcessImportBatch_Article {batchId}", ct);

            var batch = await GetBatchAsync(batchId, 25, ct) ?? new BulkImportBatchDetailDto();
            var genericResult = new BulkImportActionResultDto
            {
                Message = BuildBatchActionMessage("procesamiento", batch),
                Batch = batch
            };
            QueueReportingRefreshIfProcessed(genericResult, "Procesamiento de lote de artículos");
            return genericResult;
        }

        private async Task<BulkImportActionResultDto> ProcessExternalApiBatchAsync(int batchId, CancellationToken ct)
        {
            var batch = await _db.ImportBatches
                .Include(x => x.Rows.OrderBy(row => row.RowNumber))
                .FirstOrDefaultAsync(x => x.ImportBatchId == batchId, ct);

            if (batch is null)
            {
                throw new InvalidOperationException("No se encontró el lote solicitado.");
            }

            var externalFields = await EnsureExternalArticleDynamicFieldsAsync("scopus", ct);
            var externalFieldsByKey = externalFields
                .Where(x => x.EntityName == "Article" && x.IsDynamic)
                .ToDictionary(x => x.FieldKey, StringComparer.OrdinalIgnoreCase);

            var processed = 0;
            var inserted = 0;
            var duplicates = 0;
            var failed = 0;
            var now = DateTime.UtcNow;

            foreach (var row in batch.Rows.OrderBy(x => x.RowNumber))
            {
                if (row.RowStatus == "Processed" && row.TargetArticleId.HasValue)
                {
                    processed++;
                    continue;
                }

                try
                {
                    var article = ExtractExternalArticlePayload(row.RawJson)
                        ?? throw new InvalidOperationException("La fila no contiene el payload externo original.");

                    if (string.IsNullOrWhiteSpace(article.Title))
                    {
                        throw new InvalidOperationException("El artículo externo no tiene título.");
                    }

                    var existingArticleId = await FindExistingExternalArticleIdAsync(article, null, ct);
                    if (existingArticleId.HasValue)
                    {
                        row.TargetArticleId = existingArticleId.Value;
                        row.RowStatus = "Processed";
                        row.UpdatedAt = now;
                        processed++;
                        duplicates++;

                        _db.ImportBatchErrors.Add(new ImportBatchError
                        {
                            ImportBatchId = batchId,
                            ImportBatchRowId = row.ImportBatchRowId,
                            ErrorCode = "EXTERNAL_DUPLICATE_SKIPPED",
                            ErrorMessage = "El artículo ya existía en el modelo y se vinculó al registro existente.",
                            Severity = "Warning",
                            CreatedAt = now
                        });

                        continue;
                    }

                    var venueInput = new ArticleVenueInputDto
                    {
                        JournalName = article.JournalName,
                        IssnCode = article.IssnCode,
                        IssueNumber = article.Issue,
                        VolumeNumber = article.Volume,
                        JournalUrl = article.JournalUrl,
                        Type = "Journal"
                    };

                    var venueId = HasExternalVenueIdentity(venueInput)
                        ? await ArticleVenueModelHelper.ResolveVenueIdAsync(
                            _db,
                            venueInput,
                            null,
                            article.PublicationYear.HasValue ? (short?)article.PublicationYear.Value : null,
                            ct)
                        : null;

                    existingArticleId = await FindExistingExternalArticleIdAsync(article, venueId, ct);
                    if (existingArticleId.HasValue)
                    {
                        row.TargetArticleId = existingArticleId.Value;
                        row.RowStatus = "Processed";
                        row.UpdatedAt = now;
                        processed++;
                        duplicates++;

                        _db.ImportBatchErrors.Add(new ImportBatchError
                        {
                            ImportBatchId = batchId,
                            ImportBatchRowId = row.ImportBatchRowId,
                            ErrorCode = "EXTERNAL_DUPLICATE_SKIPPED",
                            ErrorMessage = "El artículo ya existía en el modelo y se vinculó al registro existente.",
                            Severity = "Warning",
                            CreatedAt = now
                        });

                        continue;
                    }

                    await using var rowTransaction = await _db.Database.BeginTransactionAsync(ct);

                    var entity = new Article
                    {
                        Title = Clean(article.Title),
                        Doi = Clean(article.Doi),
                        Year = article.PublicationYear.HasValue ? (short?)article.PublicationYear.Value : null,
                        PublishedAt = NormalizeExternalPublicationDate(article.PublicationDate, article.PublicationYear),
                        PageCount = TryInferPageCount(article.PageRange),
                        PublicationUrl = Clean(article.SourceUrl),
                        Filiacion = "Universidad Técnica de Ambato",
                        ExternalSource = Clean(article.ExternalSource) ?? "Scopus",
                        ExternalId = Clean(article.ExternalId ?? article.ScopusId),
                        VenueId = venueId,
                        IsOpenAccess = article.IsOpenAccess == true,
                        CreatedAt = now
                    };

                    _db.Articles.Add(entity);
                    await _db.SaveChangesAsync(ct);

                    AddExternalArticleDynamicValues(entity.Id, article, externalFieldsByKey, now);

                    var authors = ResolveExternalAuthorNames(article);
                    var affiliations = article.AuthorAffiliations
                        .Where(x => !string.IsNullOrWhiteSpace(x))
                        .Select(x => x.Trim())
                        .ToList();

                    for (var index = 0; index < authors.Count; index++)
                    {
                        _db.ArticleParticipants.Add(new ArticleParticipant
                        {
                            ArticleId = entity.Id,
                            Index = index + 1,
                            Nombre = authors[index],
                            Participacion = index == 0 ? "Autor" : "Coautor",
                            ParticipantType = "Autor externo",
                            IsPrimaryAuthor = index == 0,
                            Affiliation = affiliations.ElementAtOrDefault(index) ?? affiliations.FirstOrDefault(),
                            CreatedAt = now
                        });
                    }

                    row.TargetArticleId = entity.Id;
                    row.RowStatus = "Processed";
                    row.UpdatedAt = now;

                    await _db.SaveChangesAsync(ct);
                    await rowTransaction.CommitAsync(ct);
                    processed++;
                    inserted++;
                }
                catch (Exception ex)
                {
                    DetachExternalProcessingEntities();

                    row.TargetArticleId = null;
                    row.TargetParticipantId = null;
                    row.RowStatus = "Error";
                    row.UpdatedAt = now;
                    failed++;

                    _db.ImportBatchErrors.Add(new ImportBatchError
                    {
                        ImportBatchId = batchId,
                        ImportBatchRowId = row.ImportBatchRowId,
                        ErrorCode = "EXTERNAL_API_PROCESSING_FAILED",
                        ErrorMessage = BuildExternalProcessingErrorMessage(ex),
                        Severity = "Error",
                        CreatedAt = now
                    });

                    await _db.SaveChangesAsync(ct);
                }
            }

            batch.SuccessfulRows = processed;
            batch.ErrorRows = failed;
            batch.Status = failed > 0
                ? (processed > 0 ? "ProcessedWithErrors" : "Error")
                : "Processed";
            batch.FinishedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync(ct);

            var detail = await GetBatchSummaryOnlyAsync(batchId, ct) ?? new BulkImportBatchDetailDto();
            return new BulkImportActionResultDto
            {
                Message = $"{BuildBatchActionMessage("procesamiento externo", detail)} Nuevos insertados: {inserted}. Duplicados omitidos: {duplicates}. Filas con error: {failed}.",
                Batch = detail,
                InsertedRows = inserted,
                DuplicateRows = duplicates,
                FailedRows = failed
            };
        }

        private async Task<BulkImportActionResultDto> ProcessAuthorSubmissionBatchAsync(int batchId, CancellationToken ct)
        {
            var batch = await _db.ImportBatches
                .Include(x => x.Rows)
                .FirstOrDefaultAsync(x => x.ImportBatchId == batchId, ct);

            if (batch is null)
            {
                throw new InvalidOperationException("No se encontró el lote solicitado.");
            }

            var rows = await _db.ImportBatchRows
                .Include(x => x.Values)
                    .ThenInclude(x => x.Field)
                .Where(x => x.ImportBatchId == batchId)
                .OrderBy(x => x.RowNumber)
                .ToListAsync(ct);

            var processed = 0;
            var failed = 0;

            foreach (var row in rows)
            {
                if (row.RowStatus == "Processed" && row.TargetArticleId.HasValue)
                {
                    processed++;
                    continue;
                }

                try
                {
                    if (string.IsNullOrWhiteSpace(row.RawJson))
                    {
                        row.RawJson = "{}";
                    }

                    var request = JsonSerializer.Deserialize<RegisterArticleAggregateRequest>(
                        row.RawJson,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    request ??= new RegisterArticleAggregateRequest();

                    ApplyStagingRowValuesToAggregate(request, row.Values);

                    var persistenceResult = await _articleAggregatePersistenceService.PersistAsync(request, ct);
                    row.TargetArticleId = persistenceResult.ArticleId;
                    row.TargetParticipantId = persistenceResult.ParticipantIds.FirstOrDefault();
                    row.RowStatus = "Processed";
                    row.UpdatedAt = DateTime.UtcNow;
                    processed++;
                }
                catch (Exception ex)
                {
                    row.RowStatus = "Error";
                    row.UpdatedAt = DateTime.UtcNow;
                    failed++;

                    _db.ImportBatchErrors.Add(new ImportBatchError
                    {
                        ImportBatchId = batchId,
                        ImportBatchRowId = row.ImportBatchRowId,
                        ErrorCode = "AUTHOR_SUBMISSION_PROCESSING_FAILED",
                        ErrorMessage = ex.Message,
                        Severity = "Error",
                        CreatedAt = DateTime.UtcNow
                    });
                }
            }

            batch.SuccessfulRows = processed;
            batch.ErrorRows = failed;
            batch.Status = failed > 0
                ? (processed > 0 ? "ProcessedWithErrors" : "Error")
                : "Processed";
            batch.FinishedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync(ct);
            await MarkAuthorSubmissionWorkflowAsProcessedAsync(batchId, failed == 0 && processed > 0, ct);

            var detail = await GetBatchAsync(batchId, 25, ct) ?? new BulkImportBatchDetailDto();
            return new BulkImportActionResultDto
            {
                Message = BuildBatchActionMessage("procesamiento", detail),
                Batch = detail
            };
        }

        private static ExternalArticlePreviewDto? ExtractExternalArticlePayload(string? rawJson)
        {
            var rawData = ParseRawJson(rawJson);
            if (!rawData.TryGetValue("__ExternalPayloadJson", out var payload) || string.IsNullOrWhiteSpace(payload))
            {
                return null;
            }

            try
            {
                return JsonSerializer.Deserialize<ExternalArticlePreviewDto>(
                    payload,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            catch
            {
                return null;
            }
        }

        private async Task<int?> FindExistingExternalArticleIdAsync(ExternalArticlePreviewDto article, int? venueId, CancellationToken ct)
        {
            var doi = Clean(article.Doi);
            if (!string.IsNullOrWhiteSpace(doi))
            {
                var byDoi = await _db.Articles
                    .AsNoTracking()
                    .Where(x => x.Doi != null && x.Doi == doi)
                    .Select(x => (int?)x.Id)
                    .FirstOrDefaultAsync(ct);

                if (byDoi.HasValue)
                {
                    return byDoi;
                }
            }

            var externalSource = Clean(article.ExternalSource) ?? "Scopus";
            var externalId = Clean(article.ExternalId ?? article.ScopusId);
            if (!string.IsNullOrWhiteSpace(externalId))
            {
                var byExternalId = await _db.Articles
                    .AsNoTracking()
                    .Where(x =>
                        x.ExternalSource != null
                        && x.ExternalId != null
                        && x.ExternalSource == externalSource
                        && x.ExternalId == externalId)
                    .Select(x => (int?)x.Id)
                    .FirstOrDefaultAsync(ct);

                if (byExternalId.HasValue)
                {
                    return byExternalId;
                }
            }

            var title = Clean(article.Title);
            var year = article.PublicationYear.HasValue ? (short?)article.PublicationYear.Value : null;
            if (!string.IsNullOrWhiteSpace(title) && year.HasValue && venueId.HasValue)
            {
                return await _db.Articles
                    .AsNoTracking()
                    .Where(x =>
                        x.Doi == null
                        && x.Title == title
                        && x.Year == year
                        && x.VenueId == venueId)
                    .Select(x => (int?)x.Id)
                    .FirstOrDefaultAsync(ct);
            }

            return null;
        }

        private static DateTime? NormalizeExternalPublicationDate(string? value, int? publicationYear)
        {
            var parsed = string.IsNullOrWhiteSpace(value) ? null : TryParseDate(value);
            if (parsed.HasValue && parsed.Value.Year is >= 1900 and <= 2200)
            {
                return parsed.Value.Date;
            }

            return publicationYear is >= 1900 and <= 2200
                ? new DateTime(publicationYear.Value, 1, 1)
                : null;
        }

        private static int? TryInferPageCount(string? pageRange)
        {
            if (string.IsNullOrWhiteSpace(pageRange))
            {
                return null;
            }

            var parts = pageRange
                .Split('-', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(x => new string(x.Where(char.IsDigit).ToArray()))
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => int.TryParse(x, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) ? parsed : (int?)null)
                .Where(x => x.HasValue)
                .Select(x => x!.Value)
                .ToList();

            if (parts.Count >= 2 && parts[1] >= parts[0])
            {
                var count = parts[1] - parts[0] + 1;
                return count is > 0 and <= 1000 ? count : null;
            }

            return null;
        }

        private static bool HasExternalVenueIdentity(ArticleVenueInputDto venueInput)
            => !string.IsNullOrWhiteSpace(venueInput.JournalName)
               || !string.IsNullOrWhiteSpace(venueInput.IssnCode);

        private static string BuildExternalProcessingErrorMessage(Exception ex)
        {
            var messages = new List<string>();
            for (var current = ex; current is not null; current = current.InnerException)
            {
                if (!string.IsNullOrWhiteSpace(current.Message)
                    && !messages.Contains(current.Message, StringComparer.OrdinalIgnoreCase))
                {
                    messages.Add(current.Message.Trim());
                }
            }

            return string.Join(" | ", messages);
        }

        private void AddExternalArticleDynamicValues(
            int articleId,
            ExternalArticlePreviewDto article,
            Dictionary<string, FieldCatalogEntry> fieldsByKey,
            DateTime now)
        {
            void AddText(string fieldKey, string? value)
            {
                if (string.IsNullOrWhiteSpace(value) || !fieldsByKey.TryGetValue(fieldKey, out var field))
                {
                    return;
                }

                _db.DynamicFieldValues.Add(new DynamicFieldValue
                {
                    ArticleId = articleId,
                    FieldId = field.FieldId,
                    ValueString = value.Trim(),
                    CreatedAt = now
                });
            }

            void AddInt(string fieldKey, int? value)
            {
                if (!value.HasValue || !fieldsByKey.TryGetValue(fieldKey, out var field))
                {
                    return;
                }

                _db.DynamicFieldValues.Add(new DynamicFieldValue
                {
                    ArticleId = articleId,
                    FieldId = field.FieldId,
                    ValueInt = value,
                    CreatedAt = now
                });
            }

            void AddJson(string fieldKey, object? value)
            {
                if (value is null || !fieldsByKey.TryGetValue(fieldKey, out var field))
                {
                    return;
                }

                var json = JsonSerializer.Serialize(value);
                if (string.IsNullOrWhiteSpace(json) || json == "[]" || json == "{}")
                {
                    return;
                }

                _db.DynamicFieldValues.Add(new DynamicFieldValue
                {
                    ArticleId = articleId,
                    FieldId = field.FieldId,
                    ValueJson = json,
                    CreatedAt = now
                });
            }

            AddInt("ScopusCitationCount", article.CitationCount);
            AddText("ScopusOpenAccessStatus", article.OpenAccessStatus);
            AddText("ScopusLicenseUrl", article.LicenseUrl);
            AddText("ScopusDocumentType", article.DocumentType);
            AddText("ScopusAbstract", article.ArticleAbstract);
            AddText("ScopusLanguage", article.Language);
            AddText("ScopusPageRange", article.PageRange);
            AddText("ScopusPublisher", article.Publisher);
            AddText("ScopusEIssn", article.EIssnCode);
            AddJson("ScopusKeywords", article.Keywords);
            AddJson("ScopusSubjectAreas", article.SubjectAreas);
            AddJson("ScopusAuthorAffiliations", article.AuthorAffiliations);
            AddJson("ScopusRawPayload", article);
        }

        private void DetachExternalProcessingEntities()
        {
            var entries = _db.ChangeTracker
                .Entries()
                .Where(entry =>
                    entry.Entity is Article
                    || entry.Entity is ArticleParticipant
                    || entry.Entity is DynamicFieldValue
                    || entry.Entity is Venue
                    || entry.Entity is VenueMetric)
                .ToList();

            foreach (var entry in entries)
            {
                entry.State = EntityState.Detached;
            }
        }

        private static List<string> ResolveExternalAuthorNames(ExternalArticlePreviewDto article)
        {
            var authors = article.AuthorNames
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (authors.Count == 0 && !string.IsNullOrWhiteSpace(article.Authors))
            {
                authors = article.Authors
                    .Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();
            }

            return authors.Count == 0 ? new List<string> { "Autor externo" } : authors;
        }

        private static string? Clean(string? value)
            => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

        private async Task<List<FieldCatalogEntry>> ResolveTemplateFieldsAsync(string entityName, List<int> explicitFieldIds, bool useActiveFormsWhenEmpty, CancellationToken ct)
        {
            if (string.Equals(entityName, "Article", StringComparison.OrdinalIgnoreCase))
            {
                await ArticleVenueModelHelper.EnsureVenueCompositeFieldsAsync(_db, ct);
            }

            if (explicitFieldIds.Count > 0)
            {
                return await _db.FieldCatalogEntries
                    .AsNoTracking()
                    .Where(x => x.EntityName == entityName && explicitFieldIds.Contains(x.FieldId))
                    .Where(x => entityName != "Article" || x.FieldKey != "VenueId")
                    .OrderBy(x => x.DisplayOrder)
                    .ThenBy(x => x.FieldId)
                    .ToListAsync(ct);
            }

            if (useActiveFormsWhenEmpty)
            {
                var activeFormId = await _db.FormDefinitions
                    .AsNoTracking()
                    .Where(x => x.EntityName == entityName && x.IsActive)
                    .OrderByDescending(x => x.UpdatedAt ?? x.CreatedAt)
                    .ThenByDescending(x => x.FormId)
                    .Select(x => (int?)x.FormId)
                    .FirstOrDefaultAsync(ct);

                if (activeFormId.HasValue)
                {
                var formFields = await _db.FormFieldDefinitions
                    .AsNoTracking()
                    .Include(x => x.Field)
                    .Where(x => x.FormId == activeFormId.Value && x.IsVisible && x.Field != null && x.Field.IsActive)
                    .Where(x => entityName != "Article" || x.Field!.FieldKey != "VenueId")
                    .OrderBy(x => x.DisplayOrder)
                    .ThenBy(x => x.FieldId)
                    .Select(x => x.Field!)
                    .ToListAsync(ct);

                    if (formFields.Count > 0)
                    {
                        return formFields;
                    }
                }
            }

            return await _db.FieldCatalogEntries
                .AsNoTracking()
                .Where(x => x.EntityName == entityName && x.IsActive && x.IsVisible)
                .Where(x => entityName != "Article" || x.FieldKey != "VenueId")
                .OrderBy(x => x.DisplayOrder)
                .ThenBy(x => x.FieldId)
                .ToListAsync(ct);
        }

        private void AddDynamicCells(
            List<DynamicFieldValueInputDto> values,
            string entityName,
            Dictionary<string, FieldCatalogEntry> allFields,
            Dictionary<string, string?> rawData,
            List<ImportBatchRowValue> cellsToCreate,
            int rowId,
            BulkImportNormalizerCache normalizer,
            DateTime createdAt)
        {
            if (values is null || values.Count == 0)
            {
                return;
            }

            var fieldsById = allFields.Values
                .Where(x => x.EntityName == entityName && x.IsDynamic)
                .ToDictionary(x => x.FieldId);

            foreach (var value in values)
            {
                var field = ResolveDynamicField(value, entityName, allFields, fieldsById);
                if (field is null)
                {
                    continue;
                }

                var raw = ExtractDynamicRawValue(value);
                if (string.IsNullOrWhiteSpace(raw))
                {
                    continue;
                }

                var cleaned = raw.Trim();
                var normalized = NormalizeValue(field, cleaned, normalizer);
                rawData[BuildHeaderKey(field)] = cleaned;
                cellsToCreate.Add(new ImportBatchRowValue
                {
                    ImportBatchRowId = rowId,
                    FieldId = field.FieldId,
                    RawValue = cleaned,
                    NormalizedValue = normalized.NormalizedValue,
                    ValueType = normalized.ValueType,
                    IsValid = normalized.IsValid,
                    ValidationMessage = normalized.ValidationMessage,
                    CreatedAt = createdAt
                });
            }
        }

        private async Task TryCreateAuthorWorkflowAsync(ImportBatch batch, bool useAuthorWorkflow, string? userId, CancellationToken ct)
        {
            if (!useAuthorWorkflow &&
                !string.Equals(batch.SourceType, "AuthorSubmission", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            await _workflowService.CreateWorkflowForBatchAsync(
                batch.ImportBatchId,
                WorkflowService.AuthorArticleSubmissionWorkflowKey,
                userId,
                ct);
        }

        private async Task MarkAuthorSubmissionWorkflowAsProcessedAsync(int batchId, bool markProcessed, CancellationToken ct)
        {
            if (!markProcessed)
            {
                return;
            }

            var workflow = await _db.WorkflowInstances
                .Include(x => x.Batch)
                .FirstOrDefaultAsync(x => x.ImportBatchId == batchId, ct);

            if (workflow is null || string.Equals(workflow.Status, "Processed", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            var now = DateTime.UtcNow;
            var fromStatus = workflow.Status;
            workflow.Status = "Processed";
            workflow.LastActionAt = now;
            workflow.CompletedAt ??= now;

            _db.WorkflowActionLogs.Add(new WorkflowActionLog
            {
                WorkflowInstanceId = workflow.WorkflowInstanceId,
                ActionType = "processed",
                FromStatus = fromStatus,
                ToStatus = "Processed",
                PerformedAt = now,
                Comments = "El envío aprobado fue integrado en la base principal."
            });

            await _db.SaveChangesAsync(ct);
        }

        private static bool IsAuthorWorkflowSubmission(string? sourceType)
            => string.Equals(sourceType, "AuthorSubmission", StringComparison.OrdinalIgnoreCase)
               || string.Equals(sourceType, "AuthorMatrixSubmission", StringComparison.OrdinalIgnoreCase);

        private void QueueReportingRefreshIfProcessed(BulkImportActionResultDto result, string reason)
        {
            var summary = result.Batch?.Summary;
            if (summary is null)
            {
                return;
            }

            if (summary.ProcessedRows > 0 || summary.SuccessfulRows > 0 || result.InsertedRows > 0)
            {
                _reportingRefreshQueue.Enqueue($"{reason}. Lote {summary.BatchCode} ({summary.ImportBatchId}).");
            }
        }

        private static FieldCatalogEntry? ResolveDynamicField(
            DynamicFieldValueInputDto value,
            string entityName,
            Dictionary<string, FieldCatalogEntry> allFields,
            Dictionary<int, FieldCatalogEntry> fieldsById)
        {
            if (value.FieldId.HasValue && fieldsById.TryGetValue(value.FieldId.Value, out var byId))
            {
                return byId;
            }

            if (!string.IsNullOrWhiteSpace(value.FieldKey) &&
                allFields.TryGetValue(value.FieldKey.Trim(), out var byKey) &&
                string.Equals(byKey.EntityName, entityName, StringComparison.OrdinalIgnoreCase) &&
                byKey.IsDynamic)
            {
                return byKey;
            }

            return null;
        }

        private static string? ExtractDynamicRawValue(DynamicFieldValueInputDto value)
        {
            if (!string.IsNullOrWhiteSpace(value.ValueString))
            {
                return value.ValueString;
            }

            if (value.ValueInt.HasValue)
            {
                return value.ValueInt.Value.ToString(CultureInfo.InvariantCulture);
            }

            if (value.ValueDecimal.HasValue)
            {
                return value.ValueDecimal.Value.ToString(CultureInfo.InvariantCulture);
            }

            if (value.ValueDate.HasValue)
            {
                return value.ValueDate.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            }

            if (value.ValueBit.HasValue)
            {
                return value.ValueBit.Value ? "true" : "false";
            }

            if (!string.IsNullOrWhiteSpace(value.ValueJson))
            {
                return value.ValueJson;
            }

            return null;
        }

        private async Task<List<FieldCatalogEntry>> ResolveStoredTemplateFieldsAsync(string entityName, List<int>? fieldIds, CancellationToken ct)
        {
            if (fieldIds is null || fieldIds.Count == 0)
            {
                return new List<FieldCatalogEntry>();
            }

            return await _db.FieldCatalogEntries
                .AsNoTracking()
                .Where(x => x.EntityName == entityName && fieldIds.Contains(x.FieldId))
                .Where(x => entityName != "Article" || x.FieldKey != "VenueId")
                .OrderBy(x => x.DisplayOrder)
                .ThenBy(x => x.FieldId)
                .ToListAsync(ct);
        }

        private static BulkImportTemplateFieldDto MapTemplateField(FieldCatalogEntry field)
        {
            return new BulkImportTemplateFieldDto
            {
                FieldId = field.FieldId,
                EntityName = field.EntityName,
                FieldKey = field.FieldKey,
                FieldLabel = field.FieldLabel,
                DataType = field.DataType,
                SourceType = field.SourceType,
                HeaderKey = BuildHeaderKey(field),
                IsRequired = field.IsRequired,
                IsDynamic = field.IsDynamic,
                HelpText = field.HelpText,
                Placeholder = field.Placeholder,
                ReferenceTableName = field.ReferenceTableName
            };
        }

        private static string BuildHeaderKey(FieldCatalogEntry field) => $"{field.EntityName}.{field.FieldKey}|{field.FieldLabel}";

        private async Task<string> BuildSuggestedValuesAsync(FieldCatalogEntry field, CancellationToken ct)
        {
            if (ArticleVenueModelHelper.IsCompositeVenueField(field.FieldKey))
            {
                return field.FieldKey switch
                {
                    "JournalName" => "Escribe el nombre de la revista. El sistema resolverá o creará el venue.",
                    "IssnCode" => "Opcional. Ayuda a identificar la revista con precisión.",
                    "IssueNumber" => "Opcional. Número o issue.",
                    "VolumeNumber" => "Opcional. Volumen.",
                    "JournalUrl" => "Opcional. URL del venue.",
                    "Sjr" => "Opcional. Valor decimal de la métrica SJR.",
                    "Quartile" => "Opcional. Ej.: Q1, Q2, Q3, Q4.",
                    _ => string.Empty
                };
            }

            if (field.Options.Count > 0)
            {
                return string.Join(" | ", field.Options.Where(x => x.IsActive).OrderBy(x => x.DisplayOrder).Select(x => $"{x.OptionLabel} ({x.OptionValue})"));
            }

            var tableName = field.ReferenceTableName ?? field.PhysicalTableName;
            if (string.IsNullOrWhiteSpace(tableName))
            {
                return string.Empty;
            }

            return tableName switch
            {
                "AcademicTerms" => string.Join(" | ", await _db.AcademicTerms.AsNoTracking().OrderBy(x => x.Name).Select(x => x.Name).Take(25).ToListAsync(ct)),
                "PublicationStatuses" => string.Join(" | ", await _db.PublicationStatuses.AsNoTracking().OrderBy(x => x.Name).Select(x => x.Name).ToListAsync(ct)),
                "ResearchLines" => string.Join(" | ", await _db.ResearchLines.AsNoTracking().OrderBy(x => x.Name).Select(x => x.Name).Take(25).ToListAsync(ct)),
                "BroadFields" => string.Join(" | ", await _db.BroadFields.AsNoTracking().OrderBy(x => x.Name).Select(x => x.Name).Take(25).ToListAsync(ct)),
                "SpecificFields" => string.Join(" | ", await _db.SpecificFields.AsNoTracking().OrderBy(x => x.Name).Select(x => x.Name).Take(25).ToListAsync(ct)),
                "DetailedFields" => string.Join(" | ", await _db.DetailedFields.AsNoTracking().OrderBy(x => x.Name).Select(x => x.Name).Take(25).ToListAsync(ct)),
                "Venues" => string.Join(" | ", await _db.Venues.AsNoTracking().OrderBy(x => x.Name).Select(x => x.Name).Take(25).ToListAsync(ct)),
                _ => string.Empty
            };
        }

        private static string BuildAcceptedInputHint(FieldCatalogEntry field)
        {
            if (ArticleVenueModelHelper.IsCompositeVenueField(field.FieldKey))
            {
                return field.FieldKey switch
                {
                    "JournalName" => "Nombre de la revista.",
                    "IssnCode" => "ISSN exacto o texto equivalente.",
                    "IssueNumber" => "Texto o numero.",
                    "VolumeNumber" => "Texto o numero.",
                    "JournalUrl" => "URL completa.",
                    "Sjr" => "Numero decimal.",
                    "Quartile" => "Etiqueta como Q1, Q2, Q3 o Q4.",
                    _ => "Texto libre."
                };
            }

            if (field.Options.Count > 0)
            {
                return "Valor o etiqueta de una opcion activa.";
            }

            return field.FieldKey switch
            {
                "AcademicTermId" => "ID o nombre del periodo academico.",
                "PublicationStatusId" => "ID o nombre del estado de publicacion.",
                "ResearchLineId" => "ID o nombre de la linea de investigacion.",
                "FacultyId" => "ID o nombre de la facultad.",
                "BroadFieldId" => "ID o nombre del campo amplio.",
                "SpecificFieldId" => "ID, nombre o codigo del campo especifico.",
                "DetailedFieldId" => "ID, nombre o codigo del campo detallado.",
                "VenueId" => "ID o nombre del venue existente.",
                _ when GuessValueType(field) == "bit" => "Si/No, True/False, 1/0.",
                _ when GuessValueType(field) == "datetime" => "Fecha valida, por ejemplo 2026-03-20.",
                _ when GuessValueType(field) == "decimal" => "Numero decimal.",
                _ when GuessValueType(field) == "int" => "Numero entero.",
                _ => "Texto libre."
            };
        }

        private static string BuildNormalizationHint(FieldCatalogEntry field)
        {
            if (ArticleVenueModelHelper.IsCompositeVenueField(field.FieldKey))
            {
                return field.FieldKey switch
                {
                    "JournalName" => "Se usa para resolver o crear Venue y generar VenueId interno.",
                    "IssnCode" => "Se usa para resolver o crear Venue y afinar coincidencias.",
                    "IssueNumber" => "Se guarda como dato del Venue.",
                    "VolumeNumber" => "Se guarda como dato del Venue.",
                    "JournalUrl" => "Se guarda como dato del Venue.",
                    "Sjr" => "Se guarda como decimal en VenueMetric.",
                    "Quartile" => "Se guarda como texto en VenueMetric.",
                    _ => "Se guarda segun el modelo del articulo."
                };
            }

            if (field.Options.Count > 0)
            {
                return "Se convierte al OptionValue interno.";
            }

            return field.FieldKey switch
            {
                "AcademicTermId" => "Se convierte al ID real del catalogo.",
                "PublicationStatusId" => "Se convierte al ID real del catalogo.",
                "ResearchLineId" => "Se convierte al ID real del catalogo.",
                "FacultyId" => "Se convierte al ID real del catalogo.",
                "BroadFieldId" => "Se convierte al ID real del catalogo.",
                "SpecificFieldId" => "Se convierte al ID real del catalogo.",
                "DetailedFieldId" => "Se convierte al ID real del catalogo.",
                "VenueId" => "Se convierte al VenueId resuelto.",
                _ when GuessValueType(field) == "bit" => "Se normaliza a 1 o 0.",
                _ when GuessValueType(field) == "datetime" => "Se normaliza a yyyy-MM-dd.",
                _ when GuessValueType(field) == "decimal" => "Se normaliza con punto decimal.",
                _ when GuessValueType(field) == "int" => "Se normaliza como entero.",
                _ => "Se conserva como texto."
            };
        }

        private static string BuildBatchActionMessage(string actionName, BulkImportBatchDetailDto batch)
        {
            var summary = batch.Summary;
            var baseMessage = $"La {actionName} terminó para el lote {summary.BatchCode}.";

            if (summary.TotalRows == 0)
            {
                return $"{baseMessage} No se detectaron filas en staging.";
            }

            if (summary.ErrorRows > 0)
            {
                return $"{baseMessage} Hay {summary.ErrorRows} fila(s) con error, {summary.ValidRows} lista(s) y {summary.ProcessedRows} procesada(s). Revisa el preview y corrige antes de seguir.";
            }

            if (summary.ProcessedRows > 0)
            {
                return $"{baseMessage} Se procesaron {summary.ProcessedRows} fila(s) y el staging conserva la trazabilidad completa.";
            }

            return $"{baseMessage} Hay {summary.ValidRows} fila(s) lista(s) para continuar.";
        }

        private async Task<ParsedUploadFile> ParseFileAsync(Stream fileStream, string fileName, CancellationToken ct)
        {
            await ArticleVenueModelHelper.EnsureVenueCompositeFieldsAsync(_db, ct);
            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            var fields = await _db.FieldCatalogEntries
                .AsNoTracking()
                .Include(x => x.Options)
                .Where(x => x.IsActive && (x.EntityName == "Article" || x.EntityName == "ArticleParticipant"))
                .ToListAsync(ct);

            var fieldByHeader = fields.ToDictionary(x => $"{x.EntityName}.{x.FieldKey}", StringComparer.OrdinalIgnoreCase);
            var rows = new List<ParsedUploadRow>();
            var unmappedHeaders = new List<string>();
            var detectedHeaders = new List<string>();
            var normalizer = await BulkImportNormalizerCache.CreateAsync(_db, ct);

            if (extension == ".xlsx" || extension == ".xls")
            {
                using var workbook = new XLWorkbook(fileStream);
                var sheet = workbook.Worksheets.First();
                var firstRow = sheet.FirstRowUsed()?.RowNumber() ?? 1;
                var lastRow = sheet.LastRowUsed()?.RowNumber() ?? firstRow;
                var lastColumn = sheet.Row(firstRow).LastCellUsed()?.Address.ColumnNumber ?? 0;
                var columns = new List<(FieldCatalogEntry? Field, string Header)>();

                for (var c = 1; c <= lastColumn; c++)
                {
                    var header = sheet.Cell(firstRow, c).GetValue<string>().Trim();
                    var key = ExtractHeaderFieldKey(header);
                    fieldByHeader.TryGetValue(key, out var field);
                    columns.Add((field, header));
                    if (!string.IsNullOrWhiteSpace(header))
                    {
                        detectedHeaders.Add(header);
                    }
                    if (field is null && !string.IsNullOrWhiteSpace(header))
                    {
                        unmappedHeaders.Add(header);
                    }
                }

                for (var r = firstRow + 2; r <= lastRow; r++)
                {
                    var rawData = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
                    var cells = new List<ParsedUploadCell>();
                    for (var c = 1; c <= columns.Count; c++)
                    {
                        var raw = sheet.Cell(r, c).GetValue<string>();
                        var column = columns[c - 1];
                        if (string.IsNullOrWhiteSpace(raw))
                        {
                            continue;
                        }

                        rawData[column.Header] = raw;
                        if (column.Field is null)
                        {
                            continue;
                        }

                        var normalized = NormalizeValue(column.Field, raw, normalizer);
                        cells.Add(new ParsedUploadCell(column.Field, raw, normalized.NormalizedValue, normalized.ValueType, normalized.IsValid, normalized.ValidationMessage));
                    }

                    if (cells.Count > 0)
                    {
                        rows.Add(new ParsedUploadRow(r, rawData, cells));
                    }
                }
            }
            else if (extension == ".csv")
            {
                using var reader = new StreamReader(fileStream, Encoding.UTF8, true, leaveOpen: true);
                var lines = new List<string>();
                while (!reader.EndOfStream)
                {
                    lines.Add(await reader.ReadLineAsync(ct) ?? string.Empty);
                }

                if (lines.Count == 0)
                {
                    return new ParsedUploadFile(rows, unmappedHeaders, detectedHeaders.Distinct(StringComparer.OrdinalIgnoreCase).ToList());
                }

                var delimiter = InferCsvDelimiter(lines[0]);
                var headers = SplitCsvLine(lines[0], delimiter);
                var columns = headers.Select(h =>
                {
                    var key = ExtractHeaderFieldKey(h);
                    fieldByHeader.TryGetValue(key, out var field);
                    if (!string.IsNullOrWhiteSpace(h))
                    {
                        detectedHeaders.Add(h);
                    }
                    if (field is null && !string.IsNullOrWhiteSpace(h))
                    {
                        unmappedHeaders.Add(h);
                    }
                    return (Field: field, Header: h);
                }).ToList();

                for (var i = 1; i < lines.Count; i++)
                {
                    var values = SplitCsvLine(lines[i], delimiter);
                    var rawData = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
                    var cells = new List<ParsedUploadCell>();
                    for (var c = 0; c < columns.Count && c < values.Count; c++)
                    {
                        var raw = values[c]?.Trim();
                        if (string.IsNullOrWhiteSpace(raw))
                        {
                            continue;
                        }

                        rawData[columns[c].Header] = raw;
                        if (columns[c].Field is null)
                        {
                            continue;
                        }

                        var normalized = NormalizeValue(columns[c].Field!, raw, normalizer);
                        cells.Add(new ParsedUploadCell(columns[c].Field!, raw, normalized.NormalizedValue, normalized.ValueType, normalized.IsValid, normalized.ValidationMessage));
                    }

                    if (cells.Count > 0)
                    {
                        rows.Add(new ParsedUploadRow(i + 1, rawData, cells));
                    }
                }
            }
            else
            {
                throw new InvalidOperationException("Formato no soportado. Utiliza archivos .xlsx, .xls o .csv.");
            }

            return new ParsedUploadFile(rows, unmappedHeaders.Distinct(StringComparer.OrdinalIgnoreCase).ToList(), detectedHeaders.Distinct(StringComparer.OrdinalIgnoreCase).ToList());
        }

        private async Task ApplyClientSideValidationAsync(int batchId, CancellationToken ct, bool skipRelationalConsistency = false)
        {
            var existingClientErrors = await _db.ImportBatchErrors
                .Where(x => x.ImportBatchId == batchId
                    && (x.ErrorCode.StartsWith("CLIENT_INVALID_")
                        || x.ErrorCode.StartsWith("CLIENT_OCDE_")
                        || x.ErrorCode.StartsWith("CLIENT_REQUIRED_")
                        || x.ErrorCode == "REQUIRED_FIELD"
                        || x.ErrorCode == "AUTHOR_SUBMISSION_PROCESSING_FAILED"))
                .ToListAsync(ct);

            if (existingClientErrors.Count > 0)
            {
                _db.ImportBatchErrors.RemoveRange(existingClientErrors);
                await _db.SaveChangesAsync(ct);
            }

            var invalidValues = await _db.ImportBatchRowValues
                .Include(x => x.Row)
                .Where(x => x.Row != null && x.Row.ImportBatchId == batchId && !x.IsValid && x.ValidationMessage != null)
                .ToListAsync(ct);

            var existingSpecificErrorKeys = await _db.ImportBatchErrors
                .AsNoTracking()
                .Where(x => x.ImportBatchId == batchId
                    && x.ImportBatchRowId != null
                    && x.FieldId != null
                    && x.Severity == "Error"
                    && x.ErrorCode != "CLIENT_INVALID_VALUE")
                .Select(x => new { RowId = x.ImportBatchRowId!.Value, FieldId = x.FieldId!.Value })
                .ToListAsync(ct);

            var specificErrorKeys = existingSpecificErrorKeys
                .Select(x => $"{x.RowId}:{x.FieldId}")
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (var invalid in invalidValues)
            {
                if (specificErrorKeys.Contains($"{invalid.ImportBatchRowId}:{invalid.FieldId}"))
                {
                    continue;
                }

                _db.ImportBatchErrors.Add(new ImportBatchError
                {
                    ImportBatchId = batchId,
                    ImportBatchRowId = invalid.ImportBatchRowId,
                    FieldId = invalid.FieldId,
                    ErrorCode = "CLIENT_INVALID_VALUE",
                    ErrorMessage = invalid.ValidationMessage!,
                    Severity = "Error",
                    CreatedAt = DateTime.UtcNow
                });
            }

            await ApplyRequiredFieldValidationAsync(batchId, ct);
            if (!skipRelationalConsistency)
            {
                await ApplyRelationalConsistencyValidationAsync(batchId, ct);
            }
            await _db.SaveChangesAsync(ct);

            var rows = await _db.ImportBatchRows.Where(x => x.ImportBatchId == batchId).ToListAsync(ct);
            var rowErrorIds = await _db.ImportBatchErrors
                .Where(x => x.ImportBatchId == batchId && x.ImportBatchRowId != null && x.Severity == "Error")
                .Select(x => x.ImportBatchRowId!.Value)
                .Distinct()
                .ToListAsync(ct);

            foreach (var row in rows)
            {
                if (row.RowStatus == "Processed")
                {
                    continue;
                }

                row.RowStatus = rowErrorIds.Contains(row.ImportBatchRowId) ? "Error" : "Valid";
                row.UpdatedAt = DateTime.UtcNow;
            }

            var batch = await _db.ImportBatches.FirstAsync(x => x.ImportBatchId == batchId, ct);
            batch.ErrorRows = rows.Count(x => x.RowStatus == "Error");
            batch.SuccessfulRows = rows.Count(x => x.RowStatus == "Processed");
            batch.Status = rows.Any(x => x.RowStatus == "Valid")
                ? "Validated"
                : rows.Any(x => x.RowStatus == "Processed")
                    ? (batch.ErrorRows > 0 ? "ProcessedWithErrors" : "Processed")
                    : "Failed";
            batch.FinishedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync(ct);
        }

        private async Task ApplyExternalApiValidationAsync(int batchId, CancellationToken ct)
        {
            var retryableProcessingErrors = await _db.ImportBatchErrors
                .Where(x =>
                    x.ImportBatchId == batchId
                    && x.ErrorCode == "EXTERNAL_API_PROCESSING_FAILED")
                .ToListAsync(ct);

            if (retryableProcessingErrors.Count > 0)
            {
                _db.ImportBatchErrors.RemoveRange(retryableProcessingErrors);
                await _db.SaveChangesAsync(ct);
            }

            var rows = await _db.ImportBatchRows
                .Where(x => x.ImportBatchId == batchId)
                .ToListAsync(ct);

            var errorRowIds = await _db.ImportBatchErrors
                .Where(x =>
                    x.ImportBatchId == batchId
                    && x.ImportBatchRowId != null
                    && x.Severity == "Error")
                .Select(x => x.ImportBatchRowId!.Value)
                .Distinct()
                .ToListAsync(ct);

            var errorRowSet = errorRowIds.ToHashSet();
            var now = DateTime.UtcNow;

            foreach (var row in rows)
            {
                if (row.RowStatus == "Processed")
                {
                    continue;
                }

                row.RowStatus = errorRowSet.Contains(row.ImportBatchRowId) ? "Error" : "Valid";
                row.UpdatedAt = now;
            }

            var batch = await _db.ImportBatches.FirstAsync(x => x.ImportBatchId == batchId, ct);
            batch.ErrorRows = rows.Count(x => x.RowStatus == "Error");
            batch.SuccessfulRows = rows.Count(x => x.RowStatus == "Processed");
            batch.Status = rows.Any(x => x.RowStatus == "Valid")
                ? "Validated"
                : rows.Any(x => x.RowStatus == "Processed")
                    ? (batch.ErrorRows > 0 ? "ProcessedWithErrors" : "Processed")
                    : "Failed";
            batch.FinishedAt = now;

            await _db.SaveChangesAsync(ct);
        }

        private async Task ApplyRequiredFieldValidationAsync(int batchId, CancellationToken ct)
        {
            var requiredFields = await ResolveRequiredProcessFieldsAsync(batchId, ct);
            if (requiredFields.Count == 0)
            {
                return;
            }

            var existingRequiredErrors = await _db.ImportBatchErrors
                .AsNoTracking()
                .Where(x => x.ImportBatchId == batchId
                    && x.ImportBatchRowId != null
                    && x.FieldId != null
                    && (x.ErrorCode == "REQUIRED_FIELD" || x.ErrorCode == "CLIENT_REQUIRED_FIELD"))
                .Select(x => new { RowId = x.ImportBatchRowId!.Value, FieldId = x.FieldId!.Value })
                .ToListAsync(ct);

            var existingRequiredErrorKeys = existingRequiredErrors
                .Select(x => $"{x.RowId}:{x.FieldId}")
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var rowIds = await _db.ImportBatchRows
                .AsNoTracking()
                .Where(x => x.ImportBatchId == batchId)
                .Select(x => new { x.ImportBatchRowId, x.RowNumber })
                .ToListAsync(ct);

            if (rowIds.Count == 0)
            {
                return;
            }

            var rowValues = await _db.ImportBatchRowValues
                .AsNoTracking()
                .Include(x => x.Field)
                .Where(x => rowIds.Select(r => r.ImportBatchRowId).Contains(x.ImportBatchRowId))
                .ToListAsync(ct);

            var valuesByRow = rowValues
                .GroupBy(x => x.ImportBatchRowId)
                .ToDictionary(
                    group => group.Key,
                    group => group.ToDictionary(x => x.FieldId, x => x));

            var timestamp = DateTime.UtcNow;
            foreach (var row in rowIds)
            {
                valuesByRow.TryGetValue(row.ImportBatchRowId, out var fieldValues);
                fieldValues ??= new Dictionary<int, ImportBatchRowValue>();

                foreach (var requiredField in requiredFields)
                {
                    if (fieldValues.TryGetValue(requiredField.FieldId, out var existingValue)
                        && HasMeaningfulValue(existingValue))
                    {
                        continue;
                    }

                    var errorKey = $"{row.ImportBatchRowId}:{requiredField.FieldId}";
                    if (existingRequiredErrorKeys.Contains(errorKey))
                    {
                        continue;
                    }

                    _db.ImportBatchErrors.Add(new ImportBatchError
                    {
                        ImportBatchId = batchId,
                        ImportBatchRowId = row.ImportBatchRowId,
                        FieldId = requiredField.FieldId,
                        ErrorCode = "CLIENT_REQUIRED_FIELD",
                        ErrorMessage = $"Falta completar el campo obligatorio '{requiredField.FieldLabel}' para poder validar y procesar esta fila.",
                        Severity = "Error",
                        CreatedAt = timestamp
                    });
                    existingRequiredErrorKeys.Add(errorKey);
                }
            }
        }

        private async Task<List<FieldCatalogEntry>> ResolveRequiredProcessFieldsAsync(int batchId, CancellationToken ct)
        {
            var sourceType = await _db.ImportBatches
                .AsNoTracking()
                .Where(x => x.ImportBatchId == batchId)
                .Select(x => x.SourceType)
                .FirstOrDefaultAsync(ct);

            if (string.Equals(sourceType, "ExternalApi", StringComparison.OrdinalIgnoreCase))
            {
                return new List<FieldCatalogEntry>();
            }

            var persistedFieldIds = await ReadRequiredFieldContractAsync(batchId, ct);
            if (persistedFieldIds.ArticleFieldIds.Count > 0 || persistedFieldIds.ParticipantFieldIds.Count > 0)
            {
                var persistedFields = new List<FieldCatalogEntry>();
                persistedFields.AddRange(await ResolveStoredTemplateFieldsAsync("Article", persistedFieldIds.ArticleFieldIds, ct));
                persistedFields.AddRange(await ResolveStoredTemplateFieldsAsync("ArticleParticipant", persistedFieldIds.ParticipantFieldIds, ct));

                if (persistedFields.Count > 0)
                {
                    return persistedFields
                        .GroupBy(x => x.FieldId)
                        .Select(group => group.First())
                        .OrderBy(x => x.EntityName)
                        .ThenBy(x => x.DisplayOrder)
                        .ThenBy(x => x.FieldId)
                        .ToList();
                }
            }

            var metadataFieldIds = await ReadTemplateFieldContractAsync(batchId, ct);
            if (metadataFieldIds.ArticleFieldIds.Count > 0 || metadataFieldIds.ParticipantFieldIds.Count > 0)
            {
                var metadataRequiredFields = new List<FieldCatalogEntry>();
                metadataRequiredFields.AddRange(await ResolveStoredTemplateFieldsAsync("Article", metadataFieldIds.ArticleFieldIds, ct));
                metadataRequiredFields.AddRange(await ResolveStoredTemplateFieldsAsync("ArticleParticipant", metadataFieldIds.ParticipantFieldIds, ct));

                metadataRequiredFields = metadataRequiredFields
                    .Where(x => x.IsRequired)
                    .GroupBy(x => x.FieldId)
                    .Select(group => group.First())
                    .OrderBy(x => x.EntityName)
                    .ThenBy(x => x.DisplayOrder)
                    .ThenBy(x => x.FieldId)
                    .ToList();

                if (metadataRequiredFields.Count > 0)
                {
                    return metadataRequiredFields;
                }
            }

            var rowValueFieldIds = await _db.ImportBatchRowValues
                .AsNoTracking()
                .Where(x => x.Row != null && x.Row.ImportBatchId == batchId)
                .Select(x => x.FieldId)
                .Distinct()
                .ToListAsync(ct);

            if (rowValueFieldIds.Count > 0)
            {
                var rowValueRequiredFields = await _db.FieldCatalogEntries
                    .AsNoTracking()
                    .Include(x => x.Options)
                    .Where(x => rowValueFieldIds.Contains(x.FieldId)
                        && x.IsActive
                        && x.IsVisible
                        && x.IsRequired
                        && (x.EntityName == "Article" || x.EntityName == "ArticleParticipant"))
                    .OrderBy(x => x.EntityName)
                    .ThenBy(x => x.DisplayOrder)
                    .ThenBy(x => x.FieldId)
                    .ToListAsync(ct);

                if (rowValueRequiredFields.Count > 0)
                {
                    return rowValueRequiredFields;
                }
            }

            var requiredFields = new List<FieldCatalogEntry>();
            requiredFields.AddRange(await ResolveRequiredProcessFieldsByEntityAsync("Article", ct));
            requiredFields.AddRange(await ResolveRequiredProcessFieldsByEntityAsync("ArticleParticipant", ct));

            return requiredFields
                .GroupBy(x => x.FieldId)
                .Select(group => group.First())
                .OrderBy(x => x.EntityName)
                .ThenBy(x => x.DisplayOrder)
                .ThenBy(x => x.FieldId)
                .ToList();
        }

        private async Task<List<FieldCatalogEntry>> ResolveRequiredProcessFieldsByEntityAsync(string entityName, CancellationToken ct)
        {
            var preferredFormKey = entityName.Equals("Article", StringComparison.OrdinalIgnoreCase)
                ? "ArticleManualForm"
                : entityName.Equals("ArticleParticipant", StringComparison.OrdinalIgnoreCase)
                    ? "ArticleParticipantForm"
                    : null;

            var formQuery = _db.FormDefinitions
                .AsNoTracking()
                .Where(x => x.EntityName == entityName && x.IsActive);

            var activeFormId = !string.IsNullOrWhiteSpace(preferredFormKey)
                ? await formQuery
                    .Where(x => x.FormKey == preferredFormKey)
                    .Select(x => (int?)x.FormId)
                    .FirstOrDefaultAsync(ct)
                : null;

            activeFormId ??= await formQuery
                .OrderByDescending(x => x.UpdatedAt ?? x.CreatedAt)
                .ThenByDescending(x => x.FormId)
                .Select(x => (int?)x.FormId)
                .FirstOrDefaultAsync(ct);

            if (activeFormId.HasValue)
            {
                var formRequiredFields = await _db.FormFieldDefinitions
                    .AsNoTracking()
                    .Include(x => x.Field)
                        .ThenInclude(x => x!.Options)
                    .Where(x => x.FormId == activeFormId.Value && x.IsVisible && x.IsRequired && x.Field != null && x.Field.IsActive)
                    .Select(x => x.Field!)
                    .ToListAsync(ct);

                if (formRequiredFields.Count > 0)
                {
                    return formRequiredFields;
                }
            }

            return await _db.FieldCatalogEntries
                .AsNoTracking()
                .Include(x => x.Options)
                .Where(x => x.EntityName == entityName && x.IsActive && x.IsVisible && x.IsRequired)
                .OrderBy(x => x.DisplayOrder)
                .ThenBy(x => x.FieldId)
                .ToListAsync(ct);
        }

        private static bool HasMeaningfulValue(ImportBatchRowValue value)
            => !string.IsNullOrWhiteSpace(value.RawValue) || !string.IsNullOrWhiteSpace(value.NormalizedValue);

        private async Task<RequiredFieldContract> BuildRequiredFieldContractAsync(CancellationToken ct)
            => new()
            {
                ArticleFieldIds = (await ResolveRequiredProcessFieldsByEntityAsync("Article", ct)).Select(x => x.FieldId).Distinct().ToList(),
                ParticipantFieldIds = (await ResolveRequiredProcessFieldsByEntityAsync("ArticleParticipant", ct)).Select(x => x.FieldId).Distinct().ToList()
            };

        private async Task<RequiredFieldContract> ReadRequiredFieldContractAsync(int batchId, CancellationToken ct)
        {
            var sourceReference = await _db.ImportBatches
                .AsNoTracking()
                .Where(x => x.ImportBatchId == batchId)
                .Select(x => x.SourceReference)
                .FirstOrDefaultAsync(ct);

            if (string.IsNullOrWhiteSpace(sourceReference))
            {
                return new RequiredFieldContract();
            }

            try
            {
                var metadata = JsonSerializer.Deserialize<BatchTemplateMetadata>(sourceReference);
                if (metadata is null)
                {
                    return new RequiredFieldContract();
                }

                return new RequiredFieldContract
                {
                    ArticleFieldIds = metadata.RequiredArticleFieldIds?.Distinct().ToList() ?? new List<int>(),
                    ParticipantFieldIds = metadata.RequiredParticipantFieldIds?.Distinct().ToList() ?? new List<int>()
                };
            }
            catch
            {
                return new RequiredFieldContract();
            }
        }

        private async Task<RequiredFieldContract> ReadTemplateFieldContractAsync(int batchId, CancellationToken ct)
        {
            var sourceReference = await _db.ImportBatches
                .AsNoTracking()
                .Where(x => x.ImportBatchId == batchId)
                .Select(x => x.SourceReference)
                .FirstOrDefaultAsync(ct);

            if (string.IsNullOrWhiteSpace(sourceReference))
            {
                return new RequiredFieldContract();
            }

            try
            {
                var metadata = JsonSerializer.Deserialize<BatchTemplateMetadata>(sourceReference);
                if (metadata is null)
                {
                    return new RequiredFieldContract();
                }

                return new RequiredFieldContract
                {
                    ArticleFieldIds = metadata.ArticleFieldIds?.Distinct().ToList() ?? new List<int>(),
                    ParticipantFieldIds = metadata.ParticipantFieldIds?.Distinct().ToList() ?? new List<int>()
                };
            }
            catch
            {
                return new RequiredFieldContract();
            }
        }

        private static string SerializeBatchMetadata(BatchTemplateMetadata metadata, bool preserveRequiredContractOnlyWhenTrimmed)
        {
            var options = new JsonSerializerOptions
            {
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };

            var json = JsonSerializer.Serialize(metadata, options);
            if (json.Length <= 300)
            {
                return json;
            }

            if (!preserveRequiredContractOnlyWhenTrimmed)
            {
                return json;
            }

            var compactMetadata = new BatchTemplateMetadata
            {
                Origin = metadata.Origin,
                SourceType = metadata.SourceType,
                FileName = metadata.FileName,
                ProviderKey = metadata.ProviderKey,
                ProviderName = metadata.ProviderName,
                ArticleCount = metadata.ArticleCount,
                RequiredArticleFieldIds = metadata.RequiredArticleFieldIds,
                RequiredParticipantFieldIds = metadata.RequiredParticipantFieldIds
            };

            json = JsonSerializer.Serialize(compactMetadata, options);
            if (json.Length <= 300)
            {
                return json;
            }

            compactMetadata.FileName = null;
            compactMetadata.ProviderName = null;
            json = JsonSerializer.Serialize(compactMetadata, options);
            if (json.Length <= 300)
            {
                return json;
            }

            compactMetadata.ProviderKey = null;
            compactMetadata.ArticleCount = null;
            compactMetadata.SourceType = null;
            json = JsonSerializer.Serialize(compactMetadata, options);
            if (json.Length <= 300)
            {
                return json;
            }

            return JsonSerializer.Serialize(new BatchTemplateMetadata
            {
                Origin = metadata.Origin,
                RequiredArticleFieldIds = metadata.RequiredArticleFieldIds,
                RequiredParticipantFieldIds = metadata.RequiredParticipantFieldIds
            }, options);
        }

        private async Task PrepareVenueReferencesAsync(int batchId, CancellationToken ct)
        {
            var relevantKeys = ArticleVenueModelHelper.CompositeFieldKeys
                .Concat(new[] { "VenueId", "Year" })
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var rowValues = await _db.ImportBatchRowValues
                .Include(x => x.Field)
                .Include(x => x.Row)
                .Where(x => x.Row != null
                    && x.Row.ImportBatchId == batchId
                    && x.Field != null
                    && x.Field.EntityName == "Article"
                    && (relevantKeys.Contains(x.Field.FieldKey)
                        || x.Field.PhysicalTableName == "dbo.Venues"
                        || x.Field.PhysicalTableName == "Venues"
                        || x.Field.PhysicalTableName == "dbo.VenueMetrics"
                        || x.Field.PhysicalTableName == "VenueMetrics"))
                .ToListAsync(ct);

            if (rowValues.Count == 0)
            {
                return;
            }

            var articleFields = await _db.FieldCatalogEntries
                .AsNoTracking()
                .Where(x => x.EntityName == "Article" && relevantKeys.Contains(x.FieldKey))
                .ToDictionaryAsync(x => x.FieldKey, StringComparer.OrdinalIgnoreCase, ct);

            var existingVenueErrors = await _db.ImportBatchErrors
                .Where(x => x.ImportBatchId == batchId && x.ErrorCode.StartsWith("CLIENT_VENUE_"))
                .ToListAsync(ct);

            if (existingVenueErrors.Count > 0)
            {
                _db.ImportBatchErrors.RemoveRange(existingVenueErrors);
                await _db.SaveChangesAsync(ct);
            }

            foreach (var rowGroup in rowValues.GroupBy(x => x.ImportBatchRowId))
            {
                var valuesByKey = rowGroup
                    .Where(x => x.Field != null)
                    .GroupBy(x => ResolveImportArticleFieldKey(x.Field!))
                    .ToDictionary(x => x.Key, x => x.First(), StringComparer.OrdinalIgnoreCase);

                var venueInput = new ArticleVenueInputDto
                {
                    VenueId = TryParseNullableInt(GetCellValue(valuesByKey, "VenueId")),
                    JournalName = GetCellValue(valuesByKey, "JournalName"),
                    IssnCode = GetCellValue(valuesByKey, "IssnCode"),
                    IssueNumber = GetCellValue(valuesByKey, "IssueNumber"),
                    VolumeNumber = GetCellValue(valuesByKey, "VolumeNumber"),
                    JournalUrl = GetCellValue(valuesByKey, "JournalUrl")
                };

                var articleYear = TryParseNullableShort(GetCellValue(valuesByKey, "Year"));
                var metricInput = new ArticleVenueMetricInputDto
                {
                    Year = articleYear,
                    Sjr = TryParseNullableDecimal(GetCellValue(valuesByKey, "Sjr")),
                    Quartile = GetCellValue(valuesByKey, "Quartile")
                };

                if (!ArticleVenueModelHelper.HasVenueContent(venueInput) && !ArticleVenueModelHelper.HasMetricContent(metricInput))
                {
                    continue;
                }

                try
                {
                    var venueId = await ArticleVenueModelHelper.ResolveVenueIdAsync(_db, venueInput, metricInput, articleYear, ct);
                    if (!venueId.HasValue || !articleFields.TryGetValue("VenueId", out var venueField))
                    {
                        continue;
                    }

                    if (valuesByKey.TryGetValue("VenueId", out var venueCell))
                    {
                        venueCell.RawValue ??= venueInput.JournalName ?? venueInput.IssnCode ?? venueId.Value.ToString(CultureInfo.InvariantCulture);
                        venueCell.NormalizedValue = venueId.Value.ToString(CultureInfo.InvariantCulture);
                        venueCell.ValueType = "int";
                        venueCell.IsValid = true;
                        venueCell.ValidationMessage = null;
                    }
                    else
                    {
                        _db.ImportBatchRowValues.Add(new ImportBatchRowValue
                        {
                            ImportBatchRowId = rowGroup.Key,
                            FieldId = venueField.FieldId,
                            RawValue = venueInput.JournalName ?? venueInput.IssnCode ?? venueId.Value.ToString(CultureInfo.InvariantCulture),
                            NormalizedValue = venueId.Value.ToString(CultureInfo.InvariantCulture),
                            ValueType = "int",
                            IsValid = true,
                            ValidationMessage = null,
                            CreatedAt = DateTime.UtcNow
                        });
                    }
                }
                catch (InvalidOperationException ex)
                {
                    valuesByKey.TryGetValue("JournalName", out var journalCell);
                    var journalFieldId = journalCell?.FieldId
                        ?? (articleFields.TryGetValue("JournalName", out var journalField)
                            ? journalField.FieldId
                            : (int?)null);

                    if (journalFieldId.HasValue)
                    {
                        if (journalCell is not null)
                        {
                            journalCell.IsValid = false;
                            journalCell.ValidationMessage = ex.Message;
                            journalCell.UpdatedAt = DateTime.UtcNow;
                        }
                        else
                        {
                            _db.ImportBatchRowValues.Add(new ImportBatchRowValue
                            {
                                ImportBatchRowId = rowGroup.Key,
                                FieldId = journalFieldId.Value,
                                RawValue = string.Empty,
                                NormalizedValue = null,
                                ValueType = "string",
                                IsValid = false,
                                ValidationMessage = ex.Message,
                                CreatedAt = DateTime.UtcNow
                            });
                        }
                    }

                    _db.ImportBatchErrors.Add(new ImportBatchError
                    {
                        ImportBatchId = batchId,
                        ImportBatchRowId = rowGroup.Key,
                        FieldId = journalFieldId,
                        ErrorCode = "CLIENT_VENUE_RESOLUTION",
                        ErrorMessage = ex.Message,
                        Severity = "Error",
                        CreatedAt = DateTime.UtcNow
                    });
                }
            }

            await _db.SaveChangesAsync(ct);
        }

        private static string ExtractHeaderFieldKey(string header)
        {
            var trimmed = (header ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(trimmed))
            {
                return string.Empty;
            }

            var pipeIndex = trimmed.IndexOf('|');
            return pipeIndex >= 0 ? trimmed[..pipeIndex].Trim() : trimmed;
        }

        private static string ResolveImportArticleFieldKey(FieldCatalogEntry field)
        {
            if (string.Equals(field.FieldKey, "VenueId", StringComparison.OrdinalIgnoreCase)
                || ArticleVenueModelHelper.IsCompositeVenueField(field.FieldKey)
                || string.Equals(field.FieldKey, "Year", StringComparison.OrdinalIgnoreCase))
            {
                return field.FieldKey;
            }

            var physical = field.PhysicalColumnName ?? string.Empty;
            var physicalTable = field.PhysicalTableName ?? string.Empty;
            var label = field.FieldLabel ?? string.Empty;

            if (IsJournalNameField(field, physicalTable, physical, label))
            {
                return "JournalName";
            }

            if (IsIssnField(field, physicalTable, physical, label))
            {
                return "IssnCode";
            }

            return field.FieldKey;
        }

        private static Dictionary<string, string?> ParseRawJson(string? rawJson)
        {
            if (string.IsNullOrWhiteSpace(rawJson))
            {
                return new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
            }

            try
            {
                return JsonSerializer.Deserialize<Dictionary<string, string?>>(rawJson)
                    ?? new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
            }
            catch
            {
                return new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
            }
        }

        private static NormalizedValueResult NormalizeValue(FieldCatalogEntry field, string raw, BulkImportNormalizerCache cache)
        {
            var value = (raw ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(value))
            {
                return new NormalizedValueResult(string.Empty, GuessValueType(field), true, null);
            }

            if (field.Options.Count > 0)
            {
                var option = field.Options.FirstOrDefault(x => x.IsActive && (string.Equals(x.OptionValue, value, StringComparison.OrdinalIgnoreCase) || string.Equals(x.OptionLabel, value, StringComparison.OrdinalIgnoreCase)));
                if (option is null)
                {
                    return new NormalizedValueResult(value, "string", false, $"El valor '{value}' no coincide con una opción activa para {field.FieldLabel}.");
                }

                return ApplyConfiguredFieldValidation(field, value, option.OptionValue, "string", true, null);
            }

            if (TryNormalizeReference(field, value, cache, out var referenceResult))
            {
                return ApplyConfiguredFieldValidation(field, value, referenceResult.NormalizedValue, referenceResult.ValueType, referenceResult.IsValid, referenceResult.ValidationMessage);
            }

            var dataType = (field.DataType ?? string.Empty).Trim().ToLowerInvariant();
            if (dataType.Contains("bool") || dataType.Contains("bit"))
            {
                if (TryParseBool(value, out var boolValue))
                {
                    return ApplyConfiguredFieldValidation(field, value, boolValue ? "1" : "0", "bit", true, null);
                }

                return new NormalizedValueResult(value, "bit", false, $"El valor '{value}' no es un booleano válido para {field.FieldLabel}.");
            }

            if (dataType.Contains("date") || dataType.Contains("time"))
            {
                if (DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var dt) || DateTime.TryParse(value, out dt))
                {
                    return ApplyConfiguredFieldValidation(field, value, dt.ToString("yyyy-MM-dd"), "datetime", true, null);
                }

                return new NormalizedValueResult(value, "datetime", false, $"El valor '{value}' no es una fecha válida para {field.FieldLabel}.");
            }

            if (dataType.Contains("decimal") || dataType.Contains("numeric") || dataType.Contains("float") || dataType.Contains("double"))
            {
                if (decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var dec) || decimal.TryParse(value, NumberStyles.Any, new CultureInfo("es-EC"), out dec))
                {
                    return ApplyConfiguredFieldValidation(field, value, dec.ToString(CultureInfo.InvariantCulture), "decimal", true, null);
                }

                return new NormalizedValueResult(value, "decimal", false, $"El valor '{value}' no es numérico para {field.FieldLabel}.");
            }

            if (dataType.Contains("int") || dataType.Contains("short") || dataType.Contains("tiny"))
            {
                if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var iv) || int.TryParse(value, out iv))
                {
                    return ApplyConfiguredFieldValidation(field, value, iv.ToString(CultureInfo.InvariantCulture), "int", true, null);
                }

                return new NormalizedValueResult(value, "int", false, $"El valor '{value}' no es entero para {field.FieldLabel}.");
            }

            return ApplyConfiguredFieldValidation(field, value, value, "string", true, null);
        }

        private static NormalizedValueResult ApplyConfiguredFieldValidation(
            FieldCatalogEntry field,
            string rawValue,
            string? normalizedValue,
            string valueType,
            bool isValid,
            string? validationMessage)
        {
            if (!isValid)
            {
                return new NormalizedValueResult(normalizedValue, valueType, isValid, validationMessage);
            }

            var businessValidationMessage = ValidateKnownRegistrationRule(field, rawValue);
            if (!string.IsNullOrWhiteSpace(businessValidationMessage))
            {
                return new NormalizedValueResult(normalizedValue ?? rawValue, valueType, false, businessValidationMessage);
            }

            var configuredValidationMessage = DynamicFieldValidationEngine.Validate(
                field.FieldLabel,
                field.DataType,
                false,
                field.MaxLength,
                field.ValidationRule,
                new DynamicFieldValidationValue { Text = rawValue });

            return string.IsNullOrWhiteSpace(configuredValidationMessage)
                ? new NormalizedValueResult(normalizedValue, valueType, true, null)
                : new NormalizedValueResult(normalizedValue ?? rawValue, valueType, false, configuredValidationMessage);
        }

        private static string? ValidateKnownRegistrationRule(FieldCatalogEntry field, string rawValue)
        {
            var key = field.PhysicalColumnName ?? field.FieldKey;
            var value = rawValue.Trim();

            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            return key switch
            {
                "PageCount" when int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var pageCount) && pageCount > 1000
                    => "Numero de paginas no debe superar 1000.",
                "Quartile" when !IsAllowedQuartile(value)
                    => "Cuartil debe ser Q1, Q2, Q3 o Q4.",
                "Participacion" when !IsAllowedParticipation(value)
                    => "Participacion debe ser Autor o Coautor.",
                "ParticipantType" when !IsAllowedParticipantType(value)
                    => "Tipo de participante debe ser Docente, Estudiante, Externo u Otro.",
                "Identificacion" when !System.Text.RegularExpressions.Regex.IsMatch(value, @"^\d{10}$")
                    => "Identificacion debe contener exactamente 10 numeros.",
                _ => null
            };
        }

        private static bool IsAllowedQuartile(string value)
            => value.Trim().ToUpperInvariant() is "Q1" or "Q2" or "Q3" or "Q4";

        private static bool IsAllowedParticipation(string value)
            => value.Trim().Equals("Autor", StringComparison.OrdinalIgnoreCase)
               || value.Trim().Equals("Coautor", StringComparison.OrdinalIgnoreCase);

        private static bool IsAllowedParticipantType(string value)
            => value.Trim().Equals("Docente", StringComparison.OrdinalIgnoreCase)
               || value.Trim().Equals("Estudiante", StringComparison.OrdinalIgnoreCase)
               || value.Trim().Equals("Externo", StringComparison.OrdinalIgnoreCase)
               || value.Trim().Equals("Otro", StringComparison.OrdinalIgnoreCase);

        private static bool TryNormalizeReference(FieldCatalogEntry field, string value, BulkImportNormalizerCache cache, out NormalizedValueResult result)
        {
            result = default!;

            if (field.FieldKey == "VenueId")
            {
                result = cache.NormalizeVenue(field, value);
                return true;
            }
            if (field.FieldKey == "AcademicTermId")
            {
                result = cache.NormalizeAcademicTerm(field, value);
                return true;
            }
            if (field.FieldKey == "PublicationStatusId")
            {
                result = cache.NormalizePublicationStatus(field, value);
                return true;
            }
            if (field.FieldKey == "ResearchLineId")
            {
                result = cache.NormalizeLookup(field, value, cache.ResearchLinesByName, cache.ResearchLinesById, "línea de investigación");
                return true;
            }
            if (field.FieldKey == "FacultyId")
            {
                result = cache.NormalizeLookup(field, value, cache.FacultiesByNameOrCode, cache.FacultiesById, "facultad");
                return true;
            }
            if (field.FieldKey == "BroadFieldId")
            {
                result = cache.NormalizeLookup(field, value, cache.BroadFieldsByName, cache.BroadFieldsById, "campo amplio");
                return true;
            }
            if (field.FieldKey == "SpecificFieldId")
            {
                result = cache.NormalizeSpecificField(field, value);
                return true;
            }
            if (field.FieldKey == "DetailedFieldId")
            {
                result = cache.NormalizeDetailedField(field, value);
                return true;
            }

            return false;
        }

        private static string GuessValueType(FieldCatalogEntry field)
        {
            var dataType = (field.DataType ?? string.Empty).ToLowerInvariant();
            if (dataType.Contains("bit") || dataType.Contains("bool")) return "bit";
            if (dataType.Contains("date")) return "datetime";
            if (dataType.Contains("decimal") || dataType.Contains("numeric") || dataType.Contains("float") || dataType.Contains("double")) return "decimal";
            if (dataType.Contains("int") || dataType.Contains("short") || dataType.Contains("tiny")) return "int";
            return "string";
        }

        private static bool TryParseBool(string value, out bool parsed)
        {
            var normalized = value.Trim().ToLowerInvariant();
            parsed = normalized is "1" or "true" or "si" or "sí" or "yes" or "y" or "x";
            if (parsed)
            {
                return true;
            }

            if (normalized is "0" or "false" or "no" or "n")
            {
                parsed = false;
                return true;
            }

            return false;
        }

        private static char InferCsvDelimiter(string line)
        {
            var commaCount = 0;
            var semicolonCount = 0;
            var inQuotes = false;

            for (var i = 0; i < line.Length; i++)
            {
                var c = line[i];
                if (c == '"')
                {
                    if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                    {
                        i++;
                    }
                    else
                    {
                        inQuotes = !inQuotes;
                    }
                }
                else if (!inQuotes)
                {
                    if (c == ',')
                    {
                        commaCount++;
                    }
                    else if (c == ';')
                    {
                        semicolonCount++;
                    }
                }
            }

            return semicolonCount > commaCount ? ';' : ',';
        }

        private static List<string> SplitCsvLine(string line, char delimiter)
        {
            var result = new List<string>();
            var sb = new StringBuilder();
            var inQuotes = false;

            for (var i = 0; i < line.Length; i++)
            {
                var c = line[i];
                if (c == '"')
                {
                    if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                    {
                        sb.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = !inQuotes;
                    }
                }
                else if (c == delimiter && !inQuotes)
                {
                    result.Add(sb.ToString());
                    sb.Clear();
                }
                else
                {
                    sb.Append(c);
                }
            }

            result.Add(sb.ToString());
            return result;
        }

        private static BulkImportBatchSummaryDto MapSummary(dynamic source)
        {
            return new BulkImportBatchSummaryDto
            {
                ImportBatchId = source.Batch.ImportBatchId,
                BatchCode = source.Batch.BatchCode,
                SourceType = source.Batch.SourceType,
                EntityName = source.Batch.EntityName,
                FileName = source.Batch.FileName,
                SourceReference = source.Batch.SourceReference,
                TotalRows = source.Batch.TotalRows,
                SuccessfulRows = source.Batch.SuccessfulRows,
                ErrorRows = source.ErrorRows,
                Status = source.Batch.Status,
                StartedAt = source.Batch.StartedAt,
                FinishedAt = source.Batch.FinishedAt,
                CreatedBy = source.Batch.CreatedBy,
                Notes = source.Batch.Notes,
                PendingRows = source.PendingRows,
                ValidRows = source.ValidRows,
                ProcessedRows = source.ProcessedRows
            };
        }

        private static string GenerateBatchCode(string prefix)
        {
            var stamp = DateTime.UtcNow.ToString("yyyyMMddHHmmssfff", CultureInfo.InvariantCulture);
            var suffix = Guid.NewGuid().ToString("N")[..6].ToUpperInvariant();
            return $"{prefix}-{stamp}-{suffix}";
        }

        private async Task<string?> ResolveExistingUserIdAsync(string? userId, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                return null;
            }

            var trimmed = userId.Trim();
            return await _db.Users
                .AsNoTracking()
                .Where(x => x.Id == trimmed || x.UserName == trimmed || x.Email == trimmed)
                .Select(x => x.Id)
                .FirstOrDefaultAsync(ct);
        }

        private BulkImportTemplateDescriptorDto BuildTemplateFromHeaders(List<string> headers)
        {
            var articleFields = new List<BulkImportTemplateFieldDto>();
            var participantFields = new List<BulkImportTemplateFieldDto>();

            var keys = headers
                .Select(ExtractHeaderFieldKey)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var fields = _db.FieldCatalogEntries
                .AsNoTracking()
                .Where(x => x.EntityName == "Article" || x.EntityName == "ArticleParticipant")
                .ToList()
                .Where(x => keys.Contains($"{x.EntityName}.{x.FieldKey}", StringComparer.OrdinalIgnoreCase))
                .ToList();

            foreach (var field in fields.OrderBy(x => x.EntityName).ThenBy(x => x.DisplayOrder).ThenBy(x => x.FieldId))
            {
                var dto = MapTemplateField(field);
                if (field.EntityName == "Article")
                {
                    articleFields.Add(dto);
                }
                else if (field.EntityName == "ArticleParticipant")
                {
                    participantFields.Add(dto);
                }
            }

            return new BulkImportTemplateDescriptorDto
            {
                TemplateName = "plantilla_desde_lote",
                EntityName = "Article",
                SourceType = "Excel",
                ArticleFields = articleFields,
                ParticipantFields = participantFields
            };
        }

        private async Task<BulkImportTemplateDescriptorDto?> ParseTemplateAsync(string? sourceReference, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(sourceReference))
            {
                return null;
            }

            try
            {
                var metadata = JsonSerializer.Deserialize<BatchTemplateMetadata>(sourceReference);
                if (metadata is null)
                {
                    return null;
                }

                if (metadata.Template is not null)
                {
                    return metadata.Template;
                }

                if ((metadata.ArticleFieldIds?.Count ?? 0) == 0 && (metadata.ParticipantFieldIds?.Count ?? 0) == 0)
                {
                    return null;
                }

                var articleFields = await ResolveStoredTemplateFieldsAsync("Article", metadata.ArticleFieldIds, ct);
                var participantFields = await ResolveStoredTemplateFieldsAsync("ArticleParticipant", metadata.ParticipantFieldIds, ct);

                return new BulkImportTemplateDescriptorDto
                {
                    TemplateName = "plantilla_desde_lote",
                    EntityName = "Article",
                    SourceType = string.IsNullOrWhiteSpace(metadata.SourceType) ? "Excel" : metadata.SourceType!,
                    ArticleFields = articleFields.Select(MapTemplateField).ToList(),
                    ParticipantFields = participantFields.Select(MapTemplateField).ToList()
                };
            }
            catch
            {
                return null;
            }
        }

        private async Task ApplyRelationalConsistencyValidationAsync(int batchId, CancellationToken ct)
        {
            var rowValues = await _db.ImportBatchRowValues
                .AsNoTracking()
                .Include(x => x.Field)
                .Include(x => x.Row)
                .Where(x => x.Row != null && x.Row.ImportBatchId == batchId)
                .ToListAsync(ct);

            var errorsToAdd = new List<ImportBatchError>();

            foreach (var rowGroup in rowValues.GroupBy(x => x.ImportBatchRowId))
            {
                var valueByKey = rowGroup
                    .Where(x => x.Field != null)
                    .ToDictionary(x => x.Field!.FieldKey, x => x, StringComparer.OrdinalIgnoreCase);

                var broadId = TryGetInt(valueByKey, "BroadFieldId");
                var specificId = TryGetInt(valueByKey, "SpecificFieldId");
                var detailedId = TryGetInt(valueByKey, "DetailedFieldId");

                if (specificId.HasValue && broadId.HasValue &&
                    _db.SpecificFields.AsNoTracking().Any(x => x.SpecificFieldId == specificId.Value && x.BroadFieldId != broadId.Value))
                {
                    errorsToAdd.Add(CreateRowError(batchId, rowGroup.Key, valueByKey["SpecificFieldId"].FieldId, "CLIENT_OCDE_SPECIFIC_MISMATCH", "El campo específico no pertenece al campo amplio indicado."));
                }

                if (detailedId.HasValue && specificId.HasValue &&
                    _db.DetailedFields.AsNoTracking().Any(x => x.DetailedFieldId == detailedId.Value && x.SpecificFieldId != specificId.Value))
                {
                    errorsToAdd.Add(CreateRowError(batchId, rowGroup.Key, valueByKey["DetailedFieldId"].FieldId, "CLIENT_OCDE_DETAILED_MISMATCH", "El campo detallado no pertenece al campo específico indicado."));
                }

                if (detailedId.HasValue && !specificId.HasValue)
                {
                    errorsToAdd.Add(CreateRowError(batchId, rowGroup.Key, valueByKey["DetailedFieldId"].FieldId, "CLIENT_OCDE_MISSING_SPECIFIC", "Se informó un campo detallado, pero falta el campo específico correspondiente."));
                }

                if (specificId.HasValue && !broadId.HasValue)
                {
                    errorsToAdd.Add(CreateRowError(batchId, rowGroup.Key, valueByKey["SpecificFieldId"].FieldId, "CLIENT_OCDE_MISSING_BROAD", "Se informó un campo específico, pero falta el campo amplio correspondiente."));
                }
            }

            if (errorsToAdd.Count > 0)
            {
                _db.ImportBatchErrors.AddRange(errorsToAdd);
            }
        }

        private static int? TryGetInt(Dictionary<string, ImportBatchRowValue> valueByKey, string key)
        {
            return valueByKey.TryGetValue(key, out var value) && int.TryParse(value.NormalizedValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
                ? parsed
                : null;
        }

        private static string? GetCellValue(Dictionary<string, ImportBatchRowValue> valuesByKey, string key)
        {
            if (!valuesByKey.TryGetValue(key, out var cell))
            {
                return null;
            }

            return string.IsNullOrWhiteSpace(cell.RawValue) ? cell.NormalizedValue : cell.RawValue;
        }

        private static int? TryParseNullableInt(string? value)
            => int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) ? parsed : null;

        private static short? TryParseNullableShort(string? value)
            => short.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) ? parsed : null;

        private static decimal? TryParseNullableDecimal(string? value)
        {
            if (decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed)
                || decimal.TryParse(value, NumberStyles.Any, new CultureInfo("es-EC"), out parsed))
            {
                return parsed;
            }

            return null;
        }

        private static ImportBatchError CreateRowError(int batchId, int rowId, int fieldId, string code, string message)
            => new()
            {
                ImportBatchId = batchId,
                ImportBatchRowId = rowId,
                FieldId = fieldId,
                ErrorCode = code,
                ErrorMessage = message,
                Severity = "Error",
                CreatedAt = DateTime.UtcNow
            };

        private sealed record ParsedUploadFile(List<ParsedUploadRow> Rows, List<string> UnmappedHeaders, List<string> Headers);
        private sealed record ParsedUploadRow(int RowNumber, Dictionary<string, string?> RawData, List<ParsedUploadCell> Cells);
        private sealed record ParsedUploadCell(FieldCatalogEntry Field, string RawValue, string? NormalizedValue, string ValueType, bool IsValid, string? ValidationMessage);
        private sealed record NormalizedValueResult(string? NormalizedValue, string ValueType, bool IsValid, string? ValidationMessage);
        private sealed record ExternalDynamicFieldSeed(string FieldKey, string FieldLabel, string DataType, bool IsFilterable);
        private sealed class BatchTemplateMetadata
        {
            public string Origin { get; set; } = string.Empty;
            public string? FileName { get; set; }
            public string? SourceType { get; set; }
            public List<int>? ArticleFieldIds { get; set; }
            public List<int>? ParticipantFieldIds { get; set; }
            public List<int>? RequiredArticleFieldIds { get; set; }
            public List<int>? RequiredParticipantFieldIds { get; set; }
            public string? ProviderKey { get; set; }
            public string? ProviderName { get; set; }
            public int? ArticleCount { get; set; }
            public List<string>? Preview { get; set; }
            public BulkImportTemplateDescriptorDto? Template { get; set; }
        }

        private sealed class RequiredFieldContract
        {
            public List<int> ArticleFieldIds { get; set; } = new();
            public List<int> ParticipantFieldIds { get; set; } = new();
        }

        private sealed class BulkImportNormalizerCache
        {
            public Dictionary<string, int> AcademicTermsByName { get; init; } = new(StringComparer.OrdinalIgnoreCase);
            public Dictionary<int, int> AcademicTermsById { get; init; } = new();
            public Dictionary<string, int> PublicationStatusesByName { get; init; } = new(StringComparer.OrdinalIgnoreCase);
            public Dictionary<int, int> PublicationStatusesById { get; init; } = new();
            public Dictionary<string, int> ResearchLinesByName { get; init; } = new(StringComparer.OrdinalIgnoreCase);
            public Dictionary<int, int> ResearchLinesById { get; init; } = new();
            public Dictionary<string, int> FacultiesByNameOrCode { get; init; } = new(StringComparer.OrdinalIgnoreCase);
            public Dictionary<int, int> FacultiesById { get; init; } = new();
            public Dictionary<string, int> BroadFieldsByName { get; init; } = new(StringComparer.OrdinalIgnoreCase);
            public Dictionary<int, int> BroadFieldsById { get; init; } = new();
            public Dictionary<string, int> SpecificFieldsByNameOrCode { get; init; } = new(StringComparer.OrdinalIgnoreCase);
            public Dictionary<int, int> SpecificFieldsById { get; init; } = new();
            public Dictionary<string, int> DetailedFieldsByNameOrCode { get; init; } = new(StringComparer.OrdinalIgnoreCase);
            public Dictionary<int, int> DetailedFieldsById { get; init; } = new();
            public Dictionary<string, int> VenuesByName { get; init; } = new(StringComparer.OrdinalIgnoreCase);
            public Dictionary<int, int> VenuesById { get; init; } = new();

            public static async Task<BulkImportNormalizerCache> CreateAsync(AppDbContext db, CancellationToken ct)
            {
                var cache = new BulkImportNormalizerCache();

                foreach (var item in await db.AcademicTerms.AsNoTracking().ToListAsync(ct))
                {
                    cache.AcademicTermsByName[item.Name.Trim()] = item.AcademicTermId;
                    cache.AcademicTermsById[item.AcademicTermId] = item.AcademicTermId;
                }

                foreach (var item in await db.PublicationStatuses.AsNoTracking().ToListAsync(ct))
                {
                    cache.PublicationStatusesByName[item.Name.Trim()] = item.PublicationStatusId;
                    cache.PublicationStatusesById[item.PublicationStatusId] = item.PublicationStatusId;
                }

                foreach (var item in await db.ResearchLines.AsNoTracking().ToListAsync(ct))
                {
                    cache.ResearchLinesByName[item.Name.Trim()] = item.ResearchLineId;
                    cache.ResearchLinesById[item.ResearchLineId] = item.ResearchLineId;
                }

                foreach (var item in await db.Faculties.AsNoTracking().ToListAsync(ct))
                {
                    cache.FacultiesByNameOrCode[item.Name.Trim()] = item.FacultyId;
                    if (!string.IsNullOrWhiteSpace(item.Code)) cache.FacultiesByNameOrCode[item.Code.Trim()] = item.FacultyId;
                    cache.FacultiesById[item.FacultyId] = item.FacultyId;
                }

                foreach (var item in await db.BroadFields.AsNoTracking().ToListAsync(ct))
                {
                    cache.BroadFieldsByName[item.Name.Trim()] = item.BroadFieldId;
                    cache.BroadFieldsById[item.BroadFieldId] = item.BroadFieldId;
                }

                foreach (var item in await db.SpecificFields.AsNoTracking().ToListAsync(ct))
                {
                    cache.SpecificFieldsByNameOrCode[item.Name.Trim()] = item.SpecificFieldId;
                    if (!string.IsNullOrWhiteSpace(item.Code)) cache.SpecificFieldsByNameOrCode[item.Code.Trim()] = item.SpecificFieldId;
                    cache.SpecificFieldsById[item.SpecificFieldId] = item.SpecificFieldId;
                }

                foreach (var item in await db.DetailedFields.AsNoTracking().ToListAsync(ct))
                {
                    cache.DetailedFieldsByNameOrCode[item.Name.Trim()] = item.DetailedFieldId;
                    if (!string.IsNullOrWhiteSpace(item.Code)) cache.DetailedFieldsByNameOrCode[item.Code.Trim()] = item.DetailedFieldId;
                    cache.DetailedFieldsById[item.DetailedFieldId] = item.DetailedFieldId;
                }

                foreach (var item in await db.Venues.AsNoTracking().ToListAsync(ct))
                {
                    cache.VenuesByName[item.Name.Trim()] = item.VenueId;
                    cache.VenuesById[item.VenueId] = item.VenueId;
                }

                return cache;
            }

            public NormalizedValueResult NormalizeAcademicTerm(FieldCatalogEntry field, string value)
                => NormalizeLookup(field, value, AcademicTermsByName, AcademicTermsById, "periodo académico");

            public NormalizedValueResult NormalizePublicationStatus(FieldCatalogEntry field, string value)
            {
                if (byte.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var id) && PublicationStatusesById.ContainsKey(id))
                {
                    return new NormalizedValueResult(id.ToString(CultureInfo.InvariantCulture), "int", true, null);
                }

                if (PublicationStatusesByName.TryGetValue(value.Trim(), out var found))
                {
                    return new NormalizedValueResult(found.ToString(CultureInfo.InvariantCulture), "int", true, null);
                }

                return new NormalizedValueResult(value, "int", false, $"No se encontró el estado de publicación '{value}' para {field.FieldLabel}.");
            }

            public NormalizedValueResult NormalizeVenue(FieldCatalogEntry field, string value)
                => NormalizeLookup(field, value, VenuesByName, VenuesById, "venue");

            public NormalizedValueResult NormalizeSpecificField(FieldCatalogEntry field, string value)
                => NormalizeLookup(field, value, SpecificFieldsByNameOrCode, SpecificFieldsById, "campo específico");

            public NormalizedValueResult NormalizeDetailedField(FieldCatalogEntry field, string value)
                => NormalizeLookup(field, value, DetailedFieldsByNameOrCode, DetailedFieldsById, "campo detallado");

            public NormalizedValueResult NormalizeLookup(FieldCatalogEntry field, string value, Dictionary<string, int> byName, Dictionary<int, int> byId, string label)
            {
                if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var id) && byId.ContainsKey(id))
                {
                    return new NormalizedValueResult(id.ToString(CultureInfo.InvariantCulture), "int", true, null);
                }

                var normalized = value.Trim();
                if (byName.TryGetValue(normalized, out var found))
                {
                    return new NormalizedValueResult(found.ToString(CultureInfo.InvariantCulture), "int", true, null);
                }

                return new NormalizedValueResult(value, "int", false, $"No se encontró {label} '{value}' para {field.FieldLabel}.");
            }
        }
    }
}
