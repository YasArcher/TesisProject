# Referencia legacy — ProjectMatrixService

## TODO UNIFIED: UploadAsync

Depende de ImportFromMatrixAsync pendiente por Identity y IDs externos/locales.

Referencia exacta: `tesisproject.backend/Services/Implementations/ProjectMatrixService.cs:43-310`. Firma omitida temporalmente del contrato Unified.

```csharp
        public async Task<ServiceResult<ProjectMatrixUploadSummaryDTO>> UploadAsync(
            Stream fileStream,
            string fileName,
            string contentType,
            CancellationToken ct = default)
        {
            try
            {
                if (fileStream is null)
                {
                    return ValidationFailure<ProjectMatrixUploadSummaryDTO>(
                        ErrorMessages.ProjectMatrix.FileStreamRequired,
                        ErrorCodes.ProjectMatrix.FileStreamRequired,
                        nameof(fileStream));
                }

                using var memory = new MemoryStream();
                await fileStream.CopyToAsync(memory, ct);
                memory.Position = 0;

                var summary = new ProjectMatrixUploadSummaryDTO();
                var extension = Path.GetExtension(fileName).ToLowerInvariant();

                if (extension == ".xlsx" || extension == ".xls")
                {
                    using var workbook = new XLWorkbook(memory);

                    // Tomar directamente la 3ra hoja
                    if (workbook.Worksheets.Count < 3)
                    {
                        AddSummaryError(summary, 0, WorkbookMissingThirdWorksheetMessage);

                        return ServiceResult<ProjectMatrixUploadSummaryDTO>.Ok(
                            summary,
                            WorkbookMissingThirdWorksheetMessage);
                    }

                    var worksheet = workbook.Worksheet(3); // 3ra hoja (1-based)

                    var usedRange = worksheet.RangeUsed();
                    if (usedRange is null)
                    {
                        AddSummaryError(summary, 0, WorksheetNoDataMessage);

                        return ServiceResult<ProjectMatrixUploadSummaryDTO>.Ok(
                            summary,
                            WorksheetNoDataMessage);
                    }

                    // Primera fila usada = encabezados
                    var headerRow = usedRange.FirstRowUsed();
                    var lastColumn = worksheet.LastColumnUsed().ColumnNumber();
                    var dataMatrix = new List<List<string>>();

                    summary.Headers = headerRow
                        .Cells(1, lastColumn)
                        .Select(c => c.GetString().Trim())
                        .ToList();

                    // Mapa de columnas/secciones
                    summary.ColumnMap = BuildColumnMap(summary.Headers);

                    var headerRowNumber = headerRow.RowNumber();
                    var lastRow = worksheet.LastRowUsed().RowNumber();

                    // Buscar índice de columna ESTADO para filtrar TRANSFERIDOS
                    var normalizedHeaderList = summary.Headers
                        .Select(NormalizeHeader)
                        .ToList();

                    var estadoIndex = normalizedHeaderList
                        .FindIndex(h => h == NormalizeHeader("ESTADO"));

                    var transferredNormalized = NormalizeHeader("TRANSFERIDO");

                    // Filas de datos = todas las filas después del header
                    for (var r = headerRowNumber + 1; r <= lastRow; r++)
                    {
                        ct.ThrowIfCancellationRequested();

                        var row = worksheet.Row(r);
                        var values = row.Cells(1, lastColumn)
                            .Select(c => c.GetString())
                            .ToList();

                        if (IsAllEmpty(values))
                        {
                            summary.SkippedRows++;
                            continue;
                        }

                        // ⛔ Filtrar proyectos marcados como TRANSFERIDOS
                        if (IsTransferredRow(values, estadoIndex, transferredNormalized))
                        {
                            summary.SkippedRows++;
                            continue;
                        }

                        summary.TotalRows++;
                        summary.DataRows++;

                        dataMatrix.Add(values);
                    }

                    // ============================
                    //  MATRIZ FINAL DE PROYECTO
                    // ============================
                    var projectSection = BuildProjectMatrix(
                        summary.Headers,
                        summary.ColumnMap,
                        dataMatrix);

                    var projectHeaders = GetHeadersByIndexes(summary.Headers, summary.ColumnMap.ProjectColumns);

                    // Preview solo de debug (sin uso funcional)
                    _ = PrintMatrixPreview(projectHeaders, projectSection, 30);

                    // ============================
                    //  MATRIZ FINANCIERA
                    // ============================
                    var financialSection = BuildFinancialMatrix(
                        summary.Headers,
                        summary.ColumnMap,
                        dataMatrix);

                    var financialHeaders = GetHeadersByIndexes(summary.Headers, summary.ColumnMap.FinancialColumns);

                    // Preview solo de debug (sin uso funcional)
                    _ = PrintMatrixPreview(financialHeaders, financialSection, 30);

                    // ============================
                    //  MATRIZ DE OBJETIVO (MAYÚSCULAS)
                    // ============================
                    var objectiveSection = BuildObjectiveMatrix(
                        summary.Headers,
                        summary.ColumnMap,
                        dataMatrix);

                    var objectiveHeaders = BuildObjectiveHeaders(summary.Headers, summary.ColumnMap.ObjectiveColumn);

                    // Preview solo de debug (sin uso funcional)
                    _ = PrintMatrixPreview(objectiveHeaders, objectiveSection, 30);

                    // ============================
                    //  MATRIZ DE COLABORADORES EXTERNOS (FLAGS)
                    // ============================
                    var externalFlagsSection = BuildExternalFlagsMatrix(
                        summary.ColumnMap,
                        dataMatrix);

                    // ============================
                    //  MATRIZ COMPLETA SIN INTEGRANTES
                    // ============================
                    var fullMatrix = BuildFullMatrix(
                        summary.Headers,
                        summary.ColumnMap,
                        dataMatrix,
                        out var fullColumnIndexes);

                    var fullHeaders = GetHeadersByIndexes(summary.Headers, fullColumnIndexes);

                    // Preview solo de debug (sin uso funcional)
                    _ = PrintMatrixPreview(fullHeaders, fullMatrix, 30);

                    // ============================
                    //  MAPEO A IMPORTED PROJECTS
                    // ============================
                    var importedProjects = BuildImportedProjects(
                        summary.Headers,
                        summary.ColumnMap,
                        dataMatrix);

                    if (externalFlagsSection is not null && externalFlagsSection.Count > 0)
                    {
                        for (int i = 0;
                             i < importedProjects.Count && i < externalFlagsSection.Count;
                             i++)
                        {
                            var rowFlags = externalFlagsSection[i];

                            bool hasExternal = false;

                            if (rowFlags != null && rowFlags.Count > 0)
                            {
                                var raw = rowFlags[0];

                                if (bool.TryParse(raw, out var parsed))
                                {
                                    hasExternal = parsed;
                                }
                            }

                            importedProjects[i].HasExternalParticipants = hasExternal;
                        }
                    }

                    summary.ImportedProjects = importedProjects;

                    _logger.LogInformation(
                        "[ProjectMatrixService] Projects to import: {Count}",
                        summary.ImportedProjects?.Count ?? 0);

                    // 🔍 DUMP DEBUG DEL SUMMARY COMPLETO A JSON
                    try
                    {
                        var dumpOptions = new JsonSerializerOptions
                        {
                            WriteIndented = true
                        };

                        var json = JsonSerializer.Serialize(summary, dumpOptions);

                        var dumpDir = @"C:\temp";
                        Directory.CreateDirectory(dumpDir);

                        var dumpPath = Path.Combine(
                            dumpDir,
                            $"matrix-summary-{DateTime.Now:yyyyMMdd-HHmmss}.json");

                        File.WriteAllText(dumpPath, json, Encoding.UTF8);

                        Console.WriteLine($"[Matrix] Summary dumped to: {dumpPath}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[Matrix] Error dumping summary: {ex.Message}");
                    }

                    // ============================
                    //  LLAMAR A ProjectService.ImportFromMatrixAsync
                    // ============================
                    var importResult = await _projectService.ImportFromMatrixAsync(
                        summary,
                        ct);

                    if (!importResult.Success)
                    {
                        summary.Errors.Add(new ProjectMatrixUploadErrorDTO
                        {
                            RowNumber = 0,
                            Message = importResult.Message
                                      ?? ErrorImportingProjectsIntoDatabaseMessage
                        });
                    }

                    return ServiceResult<ProjectMatrixUploadSummaryDTO>.Ok(
                        summary,
                        MatrixFileProcessedMessage);
                }

                var notSupportedMessage =
                    $"File extension '{extension}' is not supported. Please upload an .xlsx file.";

                AddSummaryError(summary, 0, notSupportedMessage);

                return ServiceResult<ProjectMatrixUploadSummaryDTO>.Ok(
                    summary,
                    notSupportedMessage);
            }
            catch (OperationCanceledException)
            {
                return FailOperationCanceled<ProjectMatrixUploadSummaryDTO>();
            }
            catch (Exception)
            {
                return FailUnexpected<ProjectMatrixUploadSummaryDTO>();
            }
        }
```
