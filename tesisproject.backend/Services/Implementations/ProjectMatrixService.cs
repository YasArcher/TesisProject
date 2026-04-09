using ClosedXML.Excel;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using tesisproject.backend.Services.Interfaces;
using tesisproject.shared.Common.Utils;
using tesisproject.shared.DTOs.Matrices.Import;
using tesisproject.shared.DTOs.Matrices.Response;
using tesisproject.shared.Errors;
using tesisproject.shared.Responses;

namespace tesisproject.backend.Services.Implementations
{
    public class ProjectMatrixService : IProjectMatrixService
    {
        private readonly IProjectService _projectService;
        private readonly ILogger<ProjectMatrixService> _logger;

        private const string WorkbookMissingThirdWorksheetMessage = "Workbook does not contain the expected third worksheet.";
        private const string WorksheetNoDataMessage = "Worksheet does not contain data.";
        private const string MatrixFileProcessedMessage = "Matrix file processed.";
        private const string ErrorImportingProjectsIntoDatabaseMessage = "Error importing projects into database.";
        private const string DefaultObjectiveHeader = "OBJETIVO";

        private static readonly string[] DateFormats = new[]
        {
            "yyyy",
            "yyyy-MM-dd","yyyy/MM/dd","yyyy-M-d","yyyy/M/d",
            "dd/MM/yyyy","d/M/yyyy","dd-MM-yyyy","d-M-yyyy","dd.MM.yyyy","d.M.yyyy",
            "dd/MM/yy","d/M/yy","dd-MM-yy","d-M-yy",
            "yyyy-MM-dd HH:mm:ss","dd/MM/yyyy HH:mm:ss","dd-MM-yyyy HH:mm:ss"
        };

        public ProjectMatrixService(
            IProjectService projectService,
            ILogger<ProjectMatrixService> logger)
        {
            _projectService = projectService;
            _logger = logger;
        }

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

        private static ServiceResult<T> FailUnexpected<T>()
            => ServiceResult<T>.Fail(
                ErrorMessages.Common.UnexpectedError,
                ErrorType.Unexpected,
                ErrorCodes.Common.UnexpectedError);

        private static ServiceResult<T> FailOperationCanceled<T>()
            => ServiceResult<T>.Fail(
                ErrorMessages.Common.OperationCanceled,
                ErrorType.Unexpected,
                ErrorCodes.Common.OperationCanceled);

        private static ServiceResult<T> ValidationFailure<T>(
            string message,
            string errorCode,
            params string[] fields)
        {
            Dictionary<string, string[]>? validation = null;

            if (fields is { Length: > 0 })
            {
                validation = fields
                    .Distinct(StringComparer.Ordinal)
                    .ToDictionary(
                        field => field,
                        _ => new[] { message },
                        StringComparer.Ordinal);
            }

            return ServiceResult<T>.Fail(
                message,
                ErrorType.Validation,
                errorCode,
                validation);
        }

        private static void AddSummaryError(ProjectMatrixUploadSummaryDTO summary, int rowNumber, string message)
        {
            summary.Errors.Add(new ProjectMatrixUploadErrorDTO
            {
                RowNumber = rowNumber,
                Message = message
            });
        }

        private static bool IsAllEmpty(IReadOnlyList<string> values)
        {
            return values.All(v => string.IsNullOrWhiteSpace(v));
        }

        private static bool IsTransferredRow(IReadOnlyList<string> values, int estadoIndex, string transferredNormalized)
        {
            if (estadoIndex < 0 || estadoIndex >= values.Count)
                return false;

            var estadoRaw = values[estadoIndex];
            if (string.IsNullOrWhiteSpace(estadoRaw))
                return false;

            var estadoNormalized = NormalizeHeader(estadoRaw);
            return estadoNormalized == transferredNormalized;
        }

        private static List<string> GetHeadersByIndexes(IReadOnlyList<string> headers, IReadOnlyList<int> indexes)
        {
            return indexes
                .Select(i => i >= 0 && i < headers.Count
                    ? headers[i]
                    : $"COL{i}")
                .ToList();
        }

        private static List<string> BuildObjectiveHeaders(IReadOnlyList<string> headers, int? objectiveCol)
        {
            var objectiveHeaders = new List<string>();

            if (objectiveCol.HasValue &&
                objectiveCol.Value >= 0 &&
                objectiveCol.Value < headers.Count)
            {
                objectiveHeaders.Add(headers[objectiveCol.Value]);
            }
            else
            {
                objectiveHeaders.Add(DefaultObjectiveHeader);
            }

            return objectiveHeaders;
        }

        // =====================================================
        //   MATRIZ DE PROYECTO: normaliza facultad, proyecto, estado
        // =====================================================
        private static List<List<string>> BuildProjectMatrix(
            List<string> headers,
            ProjectMatrixColumnMapDTO map,
            List<List<string>> dataMatrix)
        {
            var normalizedHeaders = headers
                .Select(NormalizeHeader)
                .ToList();

            // NORMALIZAR FACULTADES (Levenshtein)
            var facultyIndex = normalizedHeaders
                .FindIndex(h => h == NormalizeHeader("FACULTAD"));

            if (facultyIndex >= 0)
            {
                _ = NormalizeAndUnifyTextColumn(
                    dataMatrix,
                    facultyIndex,
                    similarityThreshold: 90.0
                );
            }

            // NORMALIZAR PROYECTO A MAYÚSCULAS
            var projectNameIndex = normalizedHeaders
                .FindIndex(h => h == NormalizeHeader("PROYECTO"));

            if (projectNameIndex >= 0)
            {
                UppercaseColumn(dataMatrix, projectNameIndex);
            }

            // NORMALIZAR ESTADO A MAYÚSCULAS
            var estadoIndex = normalizedHeaders
                .FindIndex(h => h == NormalizeHeader("ESTADO"));

            if (estadoIndex >= 0)
            {
                UppercaseColumn(dataMatrix, estadoIndex);
            }

            // EXTRAER SECCIÓN DE PROYECTO
            var projectSection = ExtractColumnsSection(dataMatrix, map.ProjectColumns);
            return projectSection;
        }

        private static void UppercaseColumn(List<List<string>> matrix, int columnIndex)
        {
            foreach (var row in matrix)
            {
                if (columnIndex < 0 || columnIndex >= row.Count)
                    continue;

                var value = row[columnIndex];
                if (string.IsNullOrWhiteSpace(value))
                    continue;

                row[columnIndex] = value.ToUpperInvariant();
            }
        }

        // =====================================================
        //   MATRIZ DE OBJETIVO (1 columna, mayúsculas)
        // =====================================================
        private static List<List<string>> BuildObjectiveMatrix(
            List<string> headers,
            ProjectMatrixColumnMapDTO map,
            List<List<string>> dataMatrix)
        {
            var objectiveSection = new List<List<string>>();

            if (!map.ObjectiveColumn.HasValue)
                return objectiveSection;

            var col = map.ObjectiveColumn.Value;

            foreach (var row in dataMatrix)
            {
                if (col < 0 || col >= row.Count)
                {
                    objectiveSection.Add(new List<string> { string.Empty });
                    continue;
                }

                var value = row[col] ?? string.Empty;
                value = value.ToUpperInvariant();

                objectiveSection.Add(new List<string> { value });
            }

            return objectiveSection;
        }

        // =====================================================
        //   MATRIZ FINANCIERA (normalizada a decimales)
        // =====================================================
        private static List<List<string>> BuildFinancialMatrix(
            List<string> headers,
            ProjectMatrixColumnMapDTO map,
            List<List<string>> dataMatrix)
        {
            var financialSection = ExtractColumnsSection(dataMatrix, map.FinancialColumns);

            foreach (var row in financialSection)
            {
                for (int c = 0; c < row.Count; c++)
                {
                    row[c] = NormalizeDecimalNumber(row[c]);
                }
            }

            return financialSection;
        }

        // =====================================================
        //   MATRIZ DE COLABORADORES EXTERNOS (FLAGS true/false)
        // =====================================================
        private static List<List<string>> BuildExternalFlagsMatrix(
            ProjectMatrixColumnMapDTO map,
            List<List<string>> dataMatrix)
        {
            var result = new List<List<string>>();

            var extCols = map.ExternalMemberColumns ?? new List<int>();
            if (extCols.Count == 0)
                return result;

            int natInstIdx = GetIndexOrDefault(extCols, 0, -1);
            int natColIdx = GetIndexOrDefault(extCols, 1, -1);
            int intInstIdx = GetIndexOrDefault(extCols, 2, -1);
            int intColIdx = GetIndexOrDefault(extCols, 3, -1);

            foreach (var row in dataMatrix)
            {
                bool hasExternal =
                    HasPositiveValue(row, natInstIdx) ||
                    HasPositiveValue(row, natColIdx) ||
                    HasPositiveValue(row, intInstIdx) ||
                    HasPositiveValue(row, intColIdx);

                result.Add(new List<string>
                {
                    hasExternal ? "true" : "false"
                });
            }

            return result;
        }

        private static int GetIndexOrDefault(IReadOnlyList<int> list, int position, int defaultValue)
        {
            if (list is null)
                return defaultValue;

            return position >= 0 && position < list.Count
                ? list[position]
                : defaultValue;
        }

        // =====================================================
        //   MATRIZ COMPLETA (EXCLUYENDO INTEGRANTES/EXTERNOS)
        // =====================================================
        private static List<List<string>> BuildFullMatrix(
            List<string> headers,
            ProjectMatrixColumnMapDTO map,
            List<List<string>> dataMatrix,
            out List<int> fullColumnIndexes)
        {
            if (dataMatrix is null || dataMatrix.Count == 0)
            {
                fullColumnIndexes = new List<int>();
                return new List<List<string>>();
            }

            var indexSet = new HashSet<int>();

            AddIndexes(indexSet, map.ProjectColumns);
            AddIndexes(indexSet, map.FinancialColumns);

            AddRangeIfPresent(map.ExtensionRange, indexSet);
            AddRangeIfPresent(map.VisitRange, indexSet);
            AddRangeIfPresent(map.ResearchCategoryRange, indexSet);

            if (map.ObjectiveColumn.HasValue)
                indexSet.Add(map.ObjectiveColumn.Value);

            AddIndexes(indexSet, map.DocumentColumns);

            RemoveIndexes(indexSet, map.InternalMemberColumns);
            RemoveIndexes(indexSet, map.ExternalMemberColumns);

            fullColumnIndexes = indexSet
                .Where(i => i >= 0 && i < headers.Count)
                .OrderBy(i => i)
                .ToList();

            return ExtractColumnsSection(dataMatrix, fullColumnIndexes);
        }

        private static void AddIndexes(HashSet<int> target, IEnumerable<int>? indexes)
        {
            if (indexes is null)
                return;

            foreach (var idx in indexes)
                target.Add(idx);
        }

        private static void RemoveIndexes(HashSet<int> target, IEnumerable<int>? indexes)
        {
            if (indexes is null)
                return;

            foreach (var idx in indexes)
                target.Remove(idx);
        }

        private static bool HasPositiveValue(List<string> row, int index)
        {
            if (index < 0 || index >= row.Count)
                return false;

            var v = row[index];

            if (string.IsNullOrWhiteSpace(v))
                return false;

            var t = v.Trim().ToUpperInvariant();

            if (t == "NO" || t == "0" || t == "NI")
                return false;

            return true;
        }

        private static void AddRangeIfPresent(MatrixRangeDTO? range, HashSet<int> target)
        {
            if (range is null)
                return;

            for (int i = range.Start; i <= range.End; i++)
                target.Add(i);
        }

        // =====================================================
        //               Helpers de mapeo de columnas
        // =====================================================
        private static ProjectMatrixColumnMapDTO BuildColumnMap(IReadOnlyList<string> headers)
        {
            var map = new ProjectMatrixColumnMapDTO();

            var normalized = headers
                .Select(NormalizeHeader)
                .ToList();

            int IndexOf(string expected) =>
                normalized.FindIndex(h => h == NormalizeHeader(expected));

            List<int> AllIndexesOf(string expected)
            {
                var key = NormalizeHeader(expected);
                var list = new List<int>();
                for (int i = 0; i < normalized.Count; i++)
                {
                    if (normalized[i] == key)
                        list.Add(i);
                }
                return list;
            }

            var convIndexes = AllIndexesOf("CONVOCATORIA");
            if (convIndexes.Count >= 2)
            {
                var convRealIdx = convIndexes[1];
                map.ProjectColumns.Add(convRealIdx);
                map.NaturalKeyColumns.Add(convRealIdx);
            }

            var idxCodigo = IndexOf("CODIGO");
            if (idxCodigo >= 0)
            {
                map.ProjectColumns.Add(idxCodigo);
                map.NaturalKeyColumns.Add(idxCodigo);
            }

            var idxNro = IndexOf("NRO.");
            if (idxNro < 0) idxNro = IndexOf("NRO");
            if (idxNro >= 0)
            {
                map.ProjectColumns.Add(idxNro);
                map.NaturalKeyColumns.Add(idxNro);
            }

            AddIfFound(map.ProjectColumns, IndexOf("PROYECTO"));

            var idxFacultad = IndexOf("FACULTAD");
            if (idxFacultad >= 0)
            {
                if (idxCodigo >= 0 && map.ProjectColumns.Contains(idxCodigo))
                {
                    var posCode = map.ProjectColumns.IndexOf(idxCodigo);

                    if (!map.ProjectColumns.Contains(idxFacultad))
                    {
                        map.ProjectColumns.Insert(posCode + 1, idxFacultad);
                    }
                }
                else
                {
                    AddIfFound(map.ProjectColumns, idxFacultad);
                }
            }

            AddIfFound(map.ProjectColumns, IndexOf("PORCENTAJE DE EJECUTACION"));
            AddIfFound(map.ProjectColumns, IndexOf("PORCENTAJE DE EJECUCION"));
            AddIfFound(map.ProjectColumns, IndexOf("FECHA DE FINALIZACIÓN ESTIMADA"));
            AddIfFound(map.ProjectColumns, IndexOf("FECHA DE FINALIZACION ESTIMADA"));
            AddIfFound(map.ProjectColumns, IndexOf("ESTADO"));
            AddIfFound(map.ProjectColumns, IndexOf("PLAZO"));
            AddIfFound(map.ProjectColumns, IndexOf("FECHA DE INICIO"));

            AddIfFound(map.FinancialColumns, IndexOf("VALOR ASIGNADO"));
            AddIfFound(map.FinancialColumns, IndexOf("VALOR EJECUTADO"));
            AddIfFound(map.FinancialColumns, IndexOf("VALOR POR EJECUTAR"));
            AddIfFound(map.FinancialColumns, IndexOf("% EJECUTADO PRESUPUESTARIA"));
            AddIfFound(map.FinancialColumns, IndexOf("% EJECUTADO PRESUPUESTARIA "));

            AddIfFound(map.InternalMemberColumns, IndexOf("COORDINADOR"));
            AddIfFound(map.InternalMemberColumns, IndexOf("COORDINADOR SUBROGANTE"));
            AddIfFound(map.InternalMemberColumns, IndexOf("INVESTIGADORES"));

            AddIfFound(map.ExternalMemberColumns, IndexOf("INSTITUCIÓN EXTERNOS NACIONALES"));
            AddIfFound(map.ExternalMemberColumns, IndexOf("INSTITUCION EXTERNOS NACIONALES"));
            AddIfFound(map.ExternalMemberColumns, IndexOf("COLABORADORES EXTERNOS NACIONALES"));
            AddIfFound(map.ExternalMemberColumns, IndexOf("INSTITUCIÓN EXTERNOS INTERNACIONALES"));
            AddIfFound(map.ExternalMemberColumns, IndexOf("INSTITUCION EXTERNOS INTERNACIONALES"));
            AddIfFound(map.ExternalMemberColumns, IndexOf("COLABORADORES EXTERNOS INTERNACIONALES"));

            var idxResPrimera = IndexOf("RESOLUCIÓN PRIMERA PRORROGA");
            var idxAvances = IndexOf("AVANCES AÑOS 2013/2014");
            if (idxAvances < 0) idxAvances = IndexOf("AVANCES AÑOS 2013 / 2014");

            if (idxResPrimera >= 0 && idxAvances > idxResPrimera)
            {
                map.ExtensionRange = new MatrixRangeDTO
                {
                    Start = idxResPrimera,
                    End = idxAvances - 1
                };
            }

            var idxResolFinal = IndexOf("RESOLUCION INFORME FINAL HCU");
            if (idxResolFinal < 0) idxResolFinal = IndexOf("RESOLUCIÓN INFORME FINAL HCU");

            if (idxAvances >= 0 && idxResolFinal > idxAvances)
            {
                map.VisitRange = new MatrixRangeDTO
                {
                    Start = idxAvances,
                    End = idxResolFinal - 1
                };
            }

            var idxLinea = IndexOf("LÍNEA DE INVESTIGACIÓN");
            var idxObjetivo = IndexOf("OBJETIVO GENERAL");

            if (idxLinea >= 0 && idxObjetivo > idxLinea)
            {
                map.ResearchCategoryRange = new MatrixRangeDTO
                {
                    Start = idxLinea,
                    End = idxObjetivo - 1
                };
            }

            if (idxObjetivo >= 0)
            {
                map.ObjectiveColumn = idxObjetivo;
            }

            AddIfFound(map.DocumentColumns, IndexOf("MEMORANDO DEL INFORME FINAL"));
            AddIfFound(map.DocumentColumns, IndexOf("APROBACION HCU/CONIN"));
            AddIfFound(map.DocumentColumns, IndexOf("APROBACIÓN HCU/CONIN"));
            AddIfFound(map.DocumentColumns, IndexOf("FECHA APROBACION HCU/CONIN"));
            AddIfFound(map.DocumentColumns, IndexOf("FECHA APROBACIÓN HCU/CONIN"));
            AddIfFound(map.DocumentColumns, idxResolFinal);
            AddIfFound(map.DocumentColumns, IndexOf("FECHA RESOLUCION HCH"));
            AddIfFound(map.DocumentColumns, IndexOf("FECHA RESOLUCIÓN HCH"));

            return map;
        }

        private static void AddIfFound(List<int> list, int index)
        {
            if (index >= 0 && !list.Contains(index))
            {
                list.Add(index);
            }
        }

        private static string NormalizeDecimalNumber(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return "0";

            raw = raw.Trim();

            var filtered = new StringBuilder();
            foreach (var ch in raw)
            {
                if (char.IsDigit(ch) || ch == '.' || ch == ',')
                    filtered.Append(ch);
            }

            var s = filtered.ToString();
            if (string.IsNullOrWhiteSpace(s))
                return "0";

            if (s.Contains(',') && s.LastIndexOf(',') > s.LastIndexOf('.'))
            {
                s = s.Replace(".", "");
                s = s.Replace(',', '.');
                return s;
            }

            s = s.Replace(",", "");

            int firstDot = s.IndexOf('.');
            if (firstDot != -1)
            {
                s = s.Substring(0, firstDot + 1) +
                    s.Substring(firstDot + 1).Replace(".", "");
            }

            return s.Length > 0 ? s : "0";
        }

        private static string NormalizeHeader(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            var s = value.Trim().ToUpperInvariant();

            s = s
                .Replace("Á", "A")
                .Replace("É", "E")
                .Replace("Í", "I")
                .Replace("Ó", "O")
                .Replace("Ú", "U")
                .Replace("Ñ", "N");

            while (s.Contains("  "))
                s = s.Replace("  ", " ");

            return s;
        }

        private static List<List<string>> ExtractColumnsSection(
            List<List<string>> matrix,
            List<int> columnIndexes)
        {
            var result = new List<List<string>>();

            if (columnIndexes == null || columnIndexes.Count == 0)
                return result;

            foreach (var row in matrix)
            {
                var sectionRow = new List<string>();

                foreach (var colIndex in columnIndexes)
                {
                    if (colIndex >= 0 && colIndex < row.Count)
                        sectionRow.Add(row[colIndex]);
                    else
                        sectionRow.Add(string.Empty);
                }

                result.Add(sectionRow);
            }

            return result;
        }

        private static List<string> NormalizeAndUnifyTextColumn(
            List<List<string>> matrix,
            int columnIndex,
            double similarityThreshold = 90.0)
        {
            var canonicalByGroupKey = new Dictionary<string, string>();
            var groupKeyByNormalized = new Dictionary<string, string>();

            foreach (var row in matrix)
            {
                if (columnIndex < 0 || columnIndex >= row.Count)
                    continue;

                var raw = row[columnIndex]?.Trim();
                if (string.IsNullOrWhiteSpace(raw))
                    continue;

                var normalized = Levenshtein.NormalizeForComparison(raw);
                if (string.IsNullOrEmpty(normalized))
                    continue;

                if (groupKeyByNormalized.ContainsKey(normalized))
                    continue;

                string? matchedGroupKey = null;

                foreach (var existingGroupKey in canonicalByGroupKey.Keys)
                {
                    var sim = Levenshtein.SimilarityPercentage(
                        existingGroupKey,
                        normalized,
                        normalize: false);

                    if (sim >= similarityThreshold)
                    {
                        matchedGroupKey = existingGroupKey;
                        break;
                    }
                }

                if (matchedGroupKey is null)
                {
                    matchedGroupKey = normalized;
                    canonicalByGroupKey[matchedGroupKey] = raw;
                }

                groupKeyByNormalized[normalized] = matchedGroupKey;
            }

            foreach (var row in matrix)
            {
                if (columnIndex < 0 || columnIndex >= row.Count)
                    continue;

                var raw = row[columnIndex];
                if (string.IsNullOrWhiteSpace(raw))
                    continue;

                var normalized = Levenshtein.NormalizeForComparison(raw);
                if (string.IsNullOrEmpty(normalized))
                    continue;

                if (groupKeyByNormalized.TryGetValue(normalized, out var groupKey) &&
                    canonicalByGroupKey.TryGetValue(groupKey, out var canonicalValue))
                {
                    row[columnIndex] = canonicalValue;
                }
            }

            return canonicalByGroupKey
                .Values
                .Distinct()
                .OrderBy(x => x)
                .ToList();
        }

        // =====================================================
        //   MAPEO A IMPORTED PROJECTS
        // =====================================================
        private static List<ImportedProjectDTO> BuildImportedProjects(
            List<string> headers,
            ProjectMatrixColumnMapDTO map,
            List<List<string>> dataMatrix)
        {
            var result = new List<ImportedProjectDTO>();

            int? convocationColumnIndex = null;
            if (map.NaturalKeyColumns is not null && map.NaturalKeyColumns.Count > 0)
            {
                convocationColumnIndex = map.NaturalKeyColumns[0];
            }

            var normalizedHeaders = headers
                .Select(NormalizeHeader)
                .ToList();

            for (int rowIndex = 0; rowIndex < dataMatrix.Count; rowIndex++)
            {
                var row = dataMatrix[rowIndex];

                var project = new ImportedProjectDTO
                {
                    CallCode = convocationColumnIndex.HasValue
                        ? SafeGet(row, convocationColumnIndex.Value)
                        : GetByHeader(normalizedHeaders, row, "CONVOCATORIA"),

                    ProjectCode = GetByHeader(normalizedHeaders, row, "CODIGO", "CÓDIGO"),
                    Number = ParseInt(GetByHeader(normalizedHeaders, row, "NRO.", "NRO")),
                    ProjectName = GetByHeader(normalizedHeaders, row, "PROYECTO"),
                    Faculty = GetByHeader(normalizedHeaders, row, "FACULTAD"),
                    State = GetByHeader(normalizedHeaders, row, "ESTADO"),
                    TermMonths = ParseInt(GetByHeader(normalizedHeaders, row, "PLAZO")),
                    StartDate = ParseDate(GetByHeader(normalizedHeaders, row, "FECHA DE INICIO")),
                    EstimatedEndDate = ParseDate(GetByHeader(normalizedHeaders, row,
                        "FECHA DE FINALIZACION ESTIMADA",
                        "FECHA DE FINALIZACIÓN ESTIMADA")),

                    AssignedValue = ParseDecimal(GetByHeader(normalizedHeaders, row, "VALOR ASIGNADO")),
                    ExecutedValue = ParseDecimal(GetByHeader(normalizedHeaders, row, "VALOR EJECUTADO")),
                    RemainingValue = ParseDecimal(GetByHeader(normalizedHeaders, row, "VALOR POR EJECUTAR")),
                    ExecutionPercentage = ParseDecimal(GetByHeader(normalizedHeaders, row,
                        "% EJECUTADO PRESUPUESTARIA",
                        "% EJECUTADO PRESUPUESTARIA ")),
                    ExecutionProgress = ParseDecimal(GetByHeader(normalizedHeaders, row,
                        "PORCENTAJE DE EJECUCION",
                        "PORCENTAJE DE EJECUCIÓN",
                        "PORCENTAJE DE EJECUTACION")),
                };

                project.ResearchLine = GetByHeader(normalizedHeaders, row,
                    "LÍNEA DE INVESTIGACIÓN",
                    "LINEA DE INVESTIGACION");
                project.Domain = GetByHeader(normalizedHeaders, row, "DOMINIO");
                project.BroadField = GetByHeader(normalizedHeaders, row, "CAMPO AMPLIO");
                project.SpecificField = GetByHeader(normalizedHeaders, row, "CAMPO ESPECIFICO");
                project.DetailedField = GetByHeader(normalizedHeaders, row, "CAMPO DETALLADO");
                project.TerritorialScope = GetByHeader(normalizedHeaders, row, "ALCANCE TERRITORIAL");
                project.ExpectedImpact = GetByHeader(normalizedHeaders, row, "IMPACTO ESPERADO");
                project.GeneralObjective = GetByHeader(normalizedHeaders, row, "OBJETIVO GENERAL");

                var coordinatorRaw = GetByHeader(normalizedHeaders, row, "COORDINADOR");
                var alternateCoordinatorRaw = GetByHeader(normalizedHeaders, row, "COORDINADOR SUBROGANTE");

                var coordinators = ParsePeopleFromCell(coordinatorRaw, out var coordinatorDiscarded);
                var alternates = ParsePeopleFromCell(alternateCoordinatorRaw, out var alternateDiscarded);

                project.Coordinators = coordinators.ToList();
                project.AlternateCoordinators = alternates.ToList();
                project.CoordinatorDiscardedTokens = coordinatorDiscarded;
                project.AlternateCoordinatorDiscardedTokens = alternateDiscarded;

                project.Documents.AddRange(
                    BuildDocuments(headers, row, map)
                );

                project.Extensions.AddRange(
                    BuildExtensions(headers, row, map.ExtensionRange)
                );

                project.VisitPeriods.AddRange(
                    BuildVisitPeriods(headers, row, map.VisitRange)
                );

                result.Add(project);
            }

            return result;
        }

        private static string PrintMatrixPreview(
            List<string> headers,
            List<List<string>> matrix,
            int maxRows = 5)
        {
            var sb = new StringBuilder();

            sb.AppendLine("=== MATRIX PREVIEW ===");
            sb.AppendLine($"Total rows: {matrix.Count}");
            sb.AppendLine();

            sb.AppendLine("HEADERS:");
            sb.AppendLine(string.Join(" | ", headers));
            sb.AppendLine(new string('-', 80));

            var rowsToShow = Math.Min(maxRows, matrix.Count);

            for (int i = 0; i < rowsToShow; i++)
            {
                var row = matrix[i];
                sb.AppendLine(string.Join(" | ", row));
            }

            sb.AppendLine("======================");

            return sb.ToString();
        }

        // =====================================================
        //   PRÓRROGAS DINÁMICAS
        // =====================================================
        private static IEnumerable<ProjectExtensionDTO> BuildExtensions(
            List<string> headers,
            List<string> row,
            MatrixRangeDTO? range)
        {
            var result = new List<ProjectExtensionDTO>();

            if (range is null)
                return result;

            int index = 1;

            for (int col = range.Start; col <= range.End; col += 2)
            {
                var resolutionCode = SafeGet(row, col);
                var endDateRaw = (col + 1 <= range.End) ? SafeGet(row, col + 1) : null;

                if (string.IsNullOrWhiteSpace(resolutionCode) &&
                    string.IsNullOrWhiteSpace(endDateRaw))
                    continue;

                result.Add(new ProjectExtensionDTO
                {
                    Index = index++,
                    ResolutionCode = resolutionCode,
                    NewEndDate = ParseDate(endDateRaw)
                });
            }

            return result;
        }

        // =====================================================
        //   VISITAS / PERIODOS DINÁMICOS
        // =====================================================
        private static IEnumerable<ProjectVisitPeriodDTO> BuildVisitPeriods(
            List<string> headers,
            List<string> row,
            MatrixRangeDTO? range)
        {
            var result = new List<ProjectVisitPeriodDTO>();

            if (range is null)
                return result;

            for (int col = range.Start; col <= range.End; col++)
            {
                var label = headers[col];
                var raw = SafeGet(row, col);

                if (string.IsNullOrWhiteSpace(label) &&
                    string.IsNullOrWhiteSpace(raw))
                    continue;

                var hasResolution = LooksLikeResolutionCode(raw);
                var value = hasResolution ? raw : string.Empty;

                result.Add(new ProjectVisitPeriodDTO
                {
                    PeriodKey = NormalizeHeader(label),
                    PeriodLabel = label,
                    RawValue = value,
                    HasReport = hasResolution
                });
            }

            return result;
        }

        // =====================================================
        //   DOCUMENTOS (GENÉRICO) – EMPAREJA CÓDIGO + FECHA
        // =====================================================
        private static IEnumerable<ProjectDocumentDTO> BuildDocuments(
            List<string> headers,
            List<string> row,
            ProjectMatrixColumnMapDTO map)
        {
            var result = new List<ProjectDocumentDTO>();

            if (map.DocumentColumns is null || map.DocumentColumns.Count == 0)
                return result;

            var normalizedHeaders = headers
                .Select(NormalizeHeader)
                .ToList();

            var resolFinalKey = NormalizeHeader("RESOLUCION INFORME FINAL HCU");
            var resolFinalKeyAlt = NormalizeHeader("RESOLUCIÓN INFORME FINAL HCU");
            var fechaFinalKey = NormalizeHeader("FECHA RESOLUCION HCH");
            var fechaFinalKeyAlt = NormalizeHeader("FECHA RESOLUCIÓN HCH");

            var approvalKey = NormalizeHeader("APROBACION HCU/CONIN");
            var approvalKeyAlt = NormalizeHeader("APROBACIÓN HCU/CONIN");
            var approvalDateKey = NormalizeHeader("FECHA APROBACION HCU/CONIN");
            var approvalDateKeyAlt = NormalizeHeader("FECHA APROBACIÓN HCU/CONIN");

            var finalDateColIndex = normalizedHeaders.FindIndex(h =>
                h == fechaFinalKey || h == fechaFinalKeyAlt);

            var approvalDateColIndex = normalizedHeaders.FindIndex(h =>
                h == approvalDateKey || h == approvalDateKeyAlt);

            foreach (var colIndex in map.DocumentColumns)
            {
                if (colIndex < 0 || colIndex >= row.Count)
                    continue;

                var headerNorm = normalizedHeaders[colIndex];

                if (headerNorm == approvalDateKey || headerNorm == approvalDateKeyAlt ||
                    headerNorm == fechaFinalKey || headerNorm == fechaFinalKeyAlt)
                {
                    continue;
                }

                var isApproval = headerNorm == approvalKey || headerNorm == approvalKeyAlt;
                var isFinalRes = headerNorm == resolFinalKey || headerNorm == resolFinalKeyAlt;

                var value = SafeGet(row, colIndex);
                DateTime? dateValue = null;

                if (isApproval)
                {
                    string? dateRaw = null;

                    if (approvalDateColIndex >= 0 && approvalDateColIndex < row.Count)
                    {
                        dateRaw = SafeGet(row, approvalDateColIndex);
                    }

                    if (string.IsNullOrWhiteSpace(value) && string.IsNullOrWhiteSpace(dateRaw))
                        continue;

                    dateValue = ParseDate(dateRaw);

                    if (string.IsNullOrWhiteSpace(value))
                    {
                        value = "CODIGO NO EXISTENTE";
                    }
                }
                else if (isFinalRes)
                {
                    if (string.IsNullOrWhiteSpace(value))
                        continue;

                    if (finalDateColIndex >= 0 && finalDateColIndex < row.Count)
                    {
                        var dateRaw = SafeGet(row, finalDateColIndex);
                        dateValue = ParseDate(dateRaw);
                    }
                }
                else
                {
                    if (string.IsNullOrWhiteSpace(value))
                        continue;

                    dateValue = ParseDate(value);
                }

                result.Add(new ProjectDocumentDTO
                {
                    DocumentType = headerNorm,
                    Code = value,
                    Date = dateValue
                });
            }

            return result;
        }

        private static bool LooksLikeResolutionCode(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return false;

            var s = value.Trim().ToUpperInvariant();

            if (!s.Contains('-'))
                return false;

            bool hasDigit = false;

            foreach (var ch in s)
            {
                if (char.IsDigit(ch))
                    hasDigit = true;

                if (!((ch >= 'A' && ch <= 'Z') || char.IsDigit(ch) || ch == '-'))
                    return false;
            }

            return hasDigit;
        }

        // =====================================================
        //   Parser de personas (coordinador/subrogante)
        // =====================================================
        private static IReadOnlyList<string> ParsePeopleFromCell(
            string? raw,
            out List<string> discardedTokens)
        {
            discardedTokens = new List<string>();

            if (string.IsNullOrWhiteSpace(raw))
                return Array.Empty<string>();

            var text = raw.Trim();

            text = text.Replace("\r\n", "\n").Replace("\r", "\n");

            var normUpper = NormalizeHeader(text);
            if (normUpper == "NO APLICA" || normUpper == "NOAPLICA" || normUpper == "N/A" || normUpper == "NA")
                return Array.Empty<string>();

            text = Regex.Replace(text, @"\([^)]*\)", " ");
            text = RemoveLeadingTitles(text);
            text = CollapseSpaces(text);

            if (string.IsNullOrWhiteSpace(text))
                return Array.Empty<string>();

            text = text.Replace(";", "\n").Replace("|", "\n");

            var strongParts = text
                .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToList();

            var candidates = new List<string>();

            foreach (var part in strongParts)
            {
                var p = part.Trim();
                if (string.IsNullOrWhiteSpace(p))
                    continue;

                if (p.Contains(" y ", StringComparison.OrdinalIgnoreCase))
                {
                    var pieces = Regex.Split(p, @"\s+y\s+", RegexOptions.IgnoreCase)
                                      .Select(x => x.Trim())
                                      .Where(x => !string.IsNullOrWhiteSpace(x))
                                      .ToList();

                    if (pieces.Count >= 2 && pieces.All(LooksLikeRealName))
                    {
                        candidates.AddRange(pieces);
                        continue;
                    }
                }

                if (p.Count(ch => ch == ',') == 1)
                {
                    var parts = p.Split(',', 2, StringSplitOptions.TrimEntries);
                    if (parts.Length == 2)
                    {
                        var leftWords = parts[0].Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
                        var rightWords = parts[1].Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;

                        if (leftWords >= 1 && rightWords >= 1)
                        {
                            var rebuilt = $"{parts[1]} {parts[0]}";
                            candidates.Add(rebuilt.Trim());
                            continue;
                        }
                    }
                }

                candidates.Add(p);
            }

            var cleaned = new List<string>();

            foreach (var c in candidates)
            {
                var x = c.Trim();
                x = RemoveTrailingPunctuation(x);
                x = CollapseSpaces(x);

                if (!LooksLikeRealName(x))
                {
                    discardedTokens.Add(c);
                    continue;
                }

                cleaned.Add(x);
            }

            var seen = new HashSet<string>();
            var final = new List<string>();

            foreach (var name in cleaned)
            {
                var key = NormalizePersonKey(name);
                if (seen.Add(key))
                    final.Add(name);
            }

            return final;
        }

        private static string RemoveLeadingTitles(string input)
        {
            var s = (input ?? string.Empty).Trim();

            while (true)
            {
                var before = s;

                s = Regex.Replace(
                    s,
                    @"^\s*(DR\.?|DRA\.?|ING\.?|MGS\.?|MG\.?|MSC\.?|PH\.?D\.?|PHD\.?)\s+",
                    "",
                    RegexOptions.IgnoreCase);

                s = s.TrimStart();

                if (s == before)
                    break;
            }

            return s.Trim();
        }

        private static string CollapseSpaces(string input)
        {
            return Regex.Replace(input ?? "", @"\s+", " ").Trim();
        }

        private static string RemoveTrailingPunctuation(string input)
        {
            return (input ?? "").Trim().TrimEnd('.', ',', ';', ':', '-', '–', '—');
        }

        private static bool LooksLikeRealName(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return false;

            if (!input.Any(char.IsLetter))
                return false;

            var words = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return words.Length >= 2;
        }

        private static string NormalizePersonKey(string name)
        {
            var s = (name ?? "").Trim().ToLowerInvariant();

            s = s
                .Replace("á", "a").Replace("é", "e").Replace("í", "i")
                .Replace("ó", "o").Replace("ú", "u").Replace("ñ", "n");

            var sb = new StringBuilder();
            foreach (var ch in s)
            {
                if (char.IsLetterOrDigit(ch) || char.IsWhiteSpace(ch))
                    sb.Append(ch);
            }

            return CollapseSpaces(sb.ToString());
        }

        // =====================================================
        //   HELPERS PARA LECTURA DE CELDAS Y PARSING
        // =====================================================
        private static string? GetByHeader(
            List<string> headers,
            List<string> row,
            params string[] candidates)
        {
            var normalizedHeaders = headers
                .Select(NormalizeHeader)
                .ToList();

            foreach (var candidate in candidates)
            {
                var norm = NormalizeHeader(candidate);
                var idx = normalizedHeaders.FindIndex(h => h == norm);
                if (idx >= 0 && idx < row.Count)
                    return SafeGet(row, idx);
            }

            return null;
        }

        private static string? GetByHeader(
            IReadOnlyList<string> normalizedHeaders,
            List<string> row,
            params string[] candidates)
        {
            foreach (var candidate in candidates)
            {
                var norm = NormalizeHeader(candidate);
                var idx = IndexOfFirst(normalizedHeaders, norm);
                if (idx >= 0 && idx < row.Count)
                    return SafeGet(row, idx);
            }

            return null;
        }

        private static int IndexOfFirst(IReadOnlyList<string> list, string value)
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] == value)
                    return i;
            }
            return -1;
        }

        private static string SafeGet(List<string> row, int index)
        {
            if (index < 0 || index >= row.Count)
                return string.Empty;

            return row[index] ?? string.Empty;
        }

        private static int? ParseInt(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            if (int.TryParse(value, out var n))
                return n;

            return null;
        }

        private static decimal? ParseDecimal(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            var normalized = NormalizeDecimalNumber(value);
            if (decimal.TryParse(
                normalized,
                NumberStyles.Any,
                CultureInfo.InvariantCulture,
                out var d))
            {
                return d;
            }

            return null;
        }

        private static DateTime? ParseDate(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            value = value.Trim();

            if (value.Contains('\n') || value.Contains('\r'))
            {
                var parts = value
                    .Replace("\r\n", "\n")
                    .Replace('\r', '\n')
                    .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

                DateTime? maxDate = null;

                foreach (var part in parts)
                {
                    var parsed = ParseDate(part);
                    if (parsed.HasValue)
                    {
                        if (!maxDate.HasValue || parsed.Value > maxDate.Value)
                            maxDate = parsed.Value;
                    }
                }

                return maxDate;
            }

            if (Regex.IsMatch(value, @"^\d{4}$"))
            {
                if (int.TryParse(value, out var year) && year >= 1900 && year <= 2100)
                    return new DateTime(year, 1, 1);
            }

            if (DateTime.TryParseExact(
                    value,
                    DateFormats,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AllowWhiteSpaces,
                    out var dtExact))
            {
                return dtExact;
            }

            if (DateTime.TryParse(
                    value,
                    CultureInfo.CurrentCulture,
                    DateTimeStyles.AllowWhiteSpaces,
                    out var dtCurrent))
            {
                return dtCurrent;
            }

            try
            {
                var esEc = CultureInfo.GetCultureInfo("es-EC");
                if (DateTime.TryParse(
                        value,
                        esEc,
                        DateTimeStyles.AllowWhiteSpaces,
                        out var dtEsEc))
                {
                    return dtEsEc;
                }
            }
            catch { }

            if (double.TryParse(
                    value.Replace(",", "."),
                    NumberStyles.Any,
                    CultureInfo.InvariantCulture,
                    out var oaNumber))
            {
                if (oaNumber >= 20000)
                {
                    try
                    {
                        return DateTime.FromOADate(oaNumber);
                    }
                    catch { }
                }
            }

            return null;
        }
    }
}