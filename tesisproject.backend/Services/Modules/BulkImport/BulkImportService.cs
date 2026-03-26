using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Text;
using System.Text.Json;
using tesisproject.backend.Data;
using tesisproject.backend.Data.Entities;
using tesisproject.backend.Services.Interfaces;
using tesisproject.shared.DTOs.Articles;
using tesisproject.shared.DTOs.ExternalApis;
using tesisproject.shared.DTOs.Imports;

namespace tesisproject.backend.Services.Implementations
{
    public class BulkImportService : IBulkImportService
    {
        private readonly AppDbContext _db;

        public BulkImportService(AppDbContext db)
        {
            _db = db;
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
            var batch = new ImportBatch
            {
                BatchCode = $"BATCH-UI-{DateTime.UtcNow:yyyyMMddHHmmss}",
                SourceType = string.IsNullOrWhiteSpace(sourceType) ? "Excel" : sourceType.Trim(),
                EntityName = "Article",
                FileName = fileName,
                SourceReference = JsonSerializer.Serialize(new BatchTemplateMetadata
                {
                    Origin = "ui-dynamic-import",
                    FileName = fileName,
                    SourceType = sourceType,
                    ArticleFieldIds = templateDescriptor.ArticleFields.Select(x => x.FieldId).ToList(),
                    ParticipantFieldIds = templateDescriptor.ParticipantFields.Select(x => x.FieldId).ToList()
                }),
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

            foreach (var parsedRow in parsed.Rows)
            {
                var row = new ImportBatchRow
                {
                    ImportBatchId = batch.ImportBatchId,
                    RowNumber = parsedRow.RowNumber,
                    RowStatus = "Pending",
                    RawJson = JsonSerializer.Serialize(parsedRow.RawData),
                    CreatedAt = now
                };

                _db.ImportBatchRows.Add(row);
                await _db.SaveChangesAsync(ct);

                foreach (var cell in parsedRow.Cells)
                {
                    _db.ImportBatchRowValues.Add(new ImportBatchRowValue
                    {
                        ImportBatchRowId = row.ImportBatchRowId,
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

            foreach (var header in parsed.UnmappedHeaders)
            {
                _db.ImportBatchErrors.Add(new ImportBatchError
                {
                    ImportBatchId = batch.ImportBatchId,
                    ErrorCode = "CLIENT_UNKNOWN_COLUMN",
                    ErrorMessage = $"La columna '{header}' no pudo vincularse con FieldCatalog y será ignorada.",
                    Severity = "Warning",
                    CreatedAt = now
                });
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
                    ValidRows = x.Rows.Count(r => r.RowStatus == "Valid"),
                    ProcessedRows = x.Rows.Count(r => r.RowStatus == "Processed")
                })
                .FirstOrDefaultAsync(ct);

            if (batch is null)
            {
                return null;
            }

            var rows = await _db.ImportBatchRows
                .AsNoTracking()
                .Where(x => x.ImportBatchId == batchId)
                .OrderBy(x => x.RowNumber)
                .Take(Math.Max(1, previewRows))
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

        public async Task<BulkImportActionResultDto> CreateBatchFromExternalArticleAsync(ExternalArticleImportRequest request, string? userId, CancellationToken ct = default)
        {
            if (request.Article is null || string.IsNullOrWhiteSpace(request.Article.Title))
            {
                throw new InvalidOperationException("Debes enviar un artículo externo válido para crear el lote.");
            }

            await ArticleVenueModelHelper.EnsureVenueCompositeFieldsAsync(_db, ct);

            var articleFields = await ResolveTemplateFieldsAsync("Article", new List<int>(), true, ct);
            var participantFields = await ResolveTemplateFieldsAsync("ArticleParticipant", new List<int>(), true, ct);
            var allFields = articleFields.Concat(participantFields).ToDictionary(x => x.FieldKey, StringComparer.OrdinalIgnoreCase);
            var normalizer = await BulkImportNormalizerCache.CreateAsync(_db, ct);
            var now = DateTime.UtcNow;

            var batch = new ImportBatch
            {
                BatchCode = $"BATCH-EXT-{DateTime.UtcNow:yyyyMMddHHmmss}",
                SourceType = "ExternalApi",
                EntityName = "Article",
                FileName = $"{(request.ProviderKey ?? "external").Trim()}-single-article",
                SourceReference = JsonSerializer.Serialize(new
                {
                    Origin = "external-api-explorer",
                    request.ProviderKey,
                    request.ProviderName,
                    ArticleTitle = request.Article.Title,
                    request.Article.Doi,
                    request.Article.ExternalId
                }),
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

            var authorNames = request.Article.AuthorNames
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (authorNames.Count == 0 && !string.IsNullOrWhiteSpace(request.Article.Authors))
            {
                authorNames = request.Article.Authors
                    .Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();
            }

            var primaryAuthor = authorNames.FirstOrDefault() ?? "Autor externo";
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
            AddCell("Year", request.Article.PublicationYear?.ToString(CultureInfo.InvariantCulture));
            AddCell("PublicationUrl", request.Article.SourceUrl);
            AddCell("ExternalSource", request.ProviderName);
            AddCell("ExternalId", request.Article.ExternalId ?? request.Article.ScopusId);
            AddCell("JournalName", request.Article.JournalName);
            AddCell("IssnCode", request.Article.IssnCode);
            AddCell("JournalUrl", request.Article.JournalUrl);
            AddCell("VolumeNumber", request.Article.Volume);
            AddCell("IssueNumber", request.Article.Issue);

            AddCell("Index", "1");
            AddCell("Nombre", primaryAuthor);
            AddCell("IsPrimaryAuthor", "true");
            AddCell("ParticipantType", "Autor externo");

            _db.ImportBatchRowValues.AddRange(cellsToCreate);
            row.RawJson = rawData.Count == 0 ? null : JsonSerializer.Serialize(rawData);

            if (authorNames.Count > 1)
            {
                _db.ImportBatchErrors.Add(new ImportBatchError
                {
                    ImportBatchId = batch.ImportBatchId,
                    ImportBatchRowId = row.ImportBatchRowId,
                    ErrorCode = "CLIENT_EXTERNAL_AUTHORS_PENDING",
                    ErrorMessage = $"El artículo externo trae {authorNames.Count} autores. Por ahora el lote unitario usa el autor principal '{primaryAuthor}' y conserva el resto para revisión manual en registro.",
                    Severity = "Warning",
                    CreatedAt = now
                });
            }

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
                Batch = await GetBatchAsync(batch.ImportBatchId, 25, ct) ?? new BulkImportBatchDetailDto()
            };
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

            row.RawJson = rawJson.Count == 0 ? null : JsonSerializer.Serialize(rawJson);
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
            await ArticleVenueModelHelper.EnsureVenueCompositeFieldsAsync(_db, ct);
            _ = await _db.ImportBatches.FirstOrDefaultAsync(x => x.ImportBatchId == batchId, ct)
                ?? throw new InvalidOperationException("El lote no existe.");

            await PrepareVenueReferencesAsync(batchId, ct);
            await _db.Database.ExecuteSqlInterpolatedAsync($"EXEC dbo.sp_ValidateImportBatch_Article {batchId}", ct);
            await _db.Database.ExecuteSqlInterpolatedAsync($"EXEC dbo.sp_ValidateImportBatch_ArticleParticipant {batchId}", ct);
            await ApplyClientSideValidationAsync(batchId, ct);

            var batch = await GetBatchAsync(batchId, 25, ct) ?? new BulkImportBatchDetailDto();

            return new BulkImportActionResultDto
            {
                Message = BuildBatchActionMessage("validación", batch),
                Batch = batch
            };
        }

        public async Task<BulkImportActionResultDto> ProcessBatchAsync(int batchId, CancellationToken ct = default)
        {
            await ValidateBatchAsync(batchId, ct);
            await _db.Database.ExecuteSqlInterpolatedAsync($"EXEC dbo.sp_ProcessImportBatch_Article {batchId}", ct);

            var batch = await GetBatchAsync(batchId, 25, ct) ?? new BulkImportBatchDetailDto();
            return new BulkImportActionResultDto
            {
                Message = BuildBatchActionMessage("procesamiento", batch),
                Batch = batch
            };
        }

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

        private async Task<List<FieldCatalogEntry>> ResolveStoredTemplateFieldsAsync(string entityName, List<int>? fieldIds, CancellationToken ct)
        {
            if (string.Equals(entityName, "Article", StringComparison.OrdinalIgnoreCase))
            {
                await ArticleVenueModelHelper.EnsureVenueCompositeFieldsAsync(_db, ct);
            }

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

        private async Task ApplyClientSideValidationAsync(int batchId, CancellationToken ct)
        {
            var existingClientErrors = await _db.ImportBatchErrors
                .Where(x => x.ImportBatchId == batchId && (x.ErrorCode.StartsWith("CLIENT_INVALID_") || x.ErrorCode.StartsWith("CLIENT_OCDE_")))
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

            foreach (var invalid in invalidValues)
            {
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

            await ApplyRelationalConsistencyValidationAsync(batchId, ct);
            await _db.SaveChangesAsync(ct);

            var rows = await _db.ImportBatchRows.Where(x => x.ImportBatchId == batchId).ToListAsync(ct);
            var rowErrorIds = await _db.ImportBatchErrors
                .Where(x => x.ImportBatchId == batchId && x.ImportBatchRowId != null)
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
            batch.Status = rows.Any(x => x.RowStatus == "Valid") ? "Validated" : "Failed";
            batch.FinishedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync(ct);
        }

        private async Task PrepareVenueReferencesAsync(int batchId, CancellationToken ct)
        {
            var relevantKeys = ArticleVenueModelHelper.CompositeFieldKeys
                .Concat(new[] { "VenueId", "Year" })
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var rowValues = await _db.ImportBatchRowValues
                .Include(x => x.Field)
                .Include(x => x.Row)
                .Where(x => x.Row != null && x.Row.ImportBatchId == batchId && x.Field != null && relevantKeys.Contains(x.Field.FieldKey))
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
                    .ToDictionary(x => x.Field!.FieldKey, x => x, StringComparer.OrdinalIgnoreCase);

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
                    _db.ImportBatchErrors.Add(new ImportBatchError
                    {
                        ImportBatchId = batchId,
                        ImportBatchRowId = rowGroup.Key,
                        FieldId = valuesByKey.TryGetValue("JournalName", out var journalCell) ? journalCell.FieldId : null,
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

                return new NormalizedValueResult(option.OptionValue, "string", true, null);
            }

            if (TryNormalizeReference(field, value, cache, out var referenceResult))
            {
                return referenceResult;
            }

            var dataType = (field.DataType ?? string.Empty).Trim().ToLowerInvariant();
            if (dataType.Contains("bool") || dataType.Contains("bit"))
            {
                if (TryParseBool(value, out var boolValue))
                {
                    return new NormalizedValueResult(boolValue ? "1" : "0", "bit", true, null);
                }

                return new NormalizedValueResult(value, "bit", false, $"El valor '{value}' no es un booleano válido para {field.FieldLabel}.");
            }

            if (dataType.Contains("date") || dataType.Contains("time"))
            {
                if (DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var dt) || DateTime.TryParse(value, out dt))
                {
                    return new NormalizedValueResult(dt.ToString("yyyy-MM-dd"), "datetime", true, null);
                }

                return new NormalizedValueResult(value, "datetime", false, $"El valor '{value}' no es una fecha válida para {field.FieldLabel}.");
            }

            if (dataType.Contains("decimal") || dataType.Contains("numeric") || dataType.Contains("float") || dataType.Contains("double"))
            {
                if (decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var dec) || decimal.TryParse(value, NumberStyles.Any, new CultureInfo("es-EC"), out dec))
                {
                    return new NormalizedValueResult(dec.ToString(CultureInfo.InvariantCulture), "decimal", true, null);
                }

                return new NormalizedValueResult(value, "decimal", false, $"El valor '{value}' no es numérico para {field.FieldLabel}.");
            }

            if (dataType.Contains("int") || dataType.Contains("short") || dataType.Contains("tiny"))
            {
                if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var iv) || int.TryParse(value, out iv))
                {
                    return new NormalizedValueResult(iv.ToString(CultureInfo.InvariantCulture), "int", true, null);
                }

                return new NormalizedValueResult(value, "int", false, $"El valor '{value}' no es entero para {field.FieldLabel}.");
            }

            return new NormalizedValueResult(value, "string", true, null);
        }

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
                ErrorRows = source.Batch.ErrorRows,
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
        private sealed class BatchTemplateMetadata
        {
            public string Origin { get; set; } = string.Empty;
            public string? FileName { get; set; }
            public string? SourceType { get; set; }
            public List<int>? ArticleFieldIds { get; set; }
            public List<int>? ParticipantFieldIds { get; set; }
            public BulkImportTemplateDescriptorDto? Template { get; set; }
        }

        private sealed class BulkImportNormalizerCache
        {
            public Dictionary<string, int> AcademicTermsByName { get; init; } = new(StringComparer.OrdinalIgnoreCase);
            public Dictionary<int, int> AcademicTermsById { get; init; } = new();
            public Dictionary<string, int> PublicationStatusesByName { get; init; } = new(StringComparer.OrdinalIgnoreCase);
            public Dictionary<int, int> PublicationStatusesById { get; init; } = new();
            public Dictionary<string, int> ResearchLinesByName { get; init; } = new(StringComparer.OrdinalIgnoreCase);
            public Dictionary<int, int> ResearchLinesById { get; init; } = new();
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
