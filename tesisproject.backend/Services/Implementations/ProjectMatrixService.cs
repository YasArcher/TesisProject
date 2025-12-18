using ClosedXML.Excel;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using tesisproject.backend.Services.Interfaces;
using tesisproject.shared.Common.Utils;
using tesisproject.shared.DTOs.Matrices.Import;
using tesisproject.shared.DTOs.Matrices.Response;
using tesisproject.shared.Responses;

namespace tesisproject.backend.Services.Implementations
{
    public class ProjectMatrixService : IProjectMatrixService
    {
        private readonly IProjectService _projectService;
        private readonly ILogger<ProjectMatrixService> _logger;

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
            int currentUserId,
            CancellationToken ct = default)
        {
            try
            {
                if (fileStream is null)
                {
                    return ServiceResult<ProjectMatrixUploadSummaryDTO>.Fail(
                        "File stream is required.",
                        ErrorType.Validation);
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
                        summary.Errors.Add(new ProjectMatrixUploadErrorDTO
                        {
                            RowNumber = 0,
                            Message = "Workbook does not contain the expected third worksheet."
                        });

                        return ServiceResult<ProjectMatrixUploadSummaryDTO>.Ok(
                            summary,
                            "Workbook does not contain the expected third worksheet.");
                    }

                    var worksheet = workbook.Worksheet(3); // 3ra hoja (1-based)

                    var usedRange = worksheet.RangeUsed();
                    if (usedRange is null)
                    {
                        summary.Errors.Add(new ProjectMatrixUploadErrorDTO
                        {
                            RowNumber = 0,
                            Message = "Worksheet does not contain data."
                        });

                        return ServiceResult<ProjectMatrixUploadSummaryDTO>.Ok(
                            summary,
                            "Worksheet does not contain data.");
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

                    // Contador simple de proyectos (1,2,3,...) solo para filas con datos
                    var projectIndex = 1;

                    // Filas de datos = todas las filas después del header
                    for (var r = headerRowNumber + 1; r <= lastRow; r++)
                    {
                        ct.ThrowIfCancellationRequested();

                        var row = worksheet.Row(r);
                        var values = row.Cells(1, lastColumn)
                            .Select(c => c.GetString())
                            .ToList();

                        var allEmpty = values.All(v => string.IsNullOrWhiteSpace(v));
                        if (allEmpty)
                        {
                            summary.SkippedRows++;
                            continue;
                        }

                        // ⛔ Filtrar proyectos marcados como TRANSFERIDOS
                        var isTransferred = false;
                        if (estadoIndex >= 0 && estadoIndex < values.Count)
                        {
                            var estadoRaw = values[estadoIndex];
                            if (!string.IsNullOrWhiteSpace(estadoRaw))
                            {
                                var estadoNormalized = NormalizeHeader(estadoRaw);
                                var transferredNormalized = NormalizeHeader("TRANSFERIDO");

                                if (estadoNormalized == transferredNormalized)
                                {
                                    isTransferred = true;
                                }
                            }
                        }

                        if (isTransferred)
                        {
                            summary.SkippedRows++;
                            continue;
                        }

                        summary.TotalRows++;
                        summary.DataRows++;

                        projectIndex++;

                        dataMatrix.Add(values);
                    }

                    // ============================
                    //  MATRIZ FINAL DE PROYECTO
                    // ============================
                    var projectSection = BuildProjectMatrix(
                        summary.Headers,
                        summary.ColumnMap,
                        dataMatrix);

                    var projectHeaders = summary.ColumnMap.ProjectColumns
                        .Select(i => i >= 0 && i < summary.Headers.Count
                            ? summary.Headers[i]
                            : $"COL{i}")
                        .ToList();

                    var projectPreview = PrintMatrixPreview(projectHeaders, projectSection, 30);

                    // ============================
                    //  MATRIZ FINANCIERA
                    // ============================
                    var financialSection = BuildFinancialMatrix(
                        summary.Headers,
                        summary.ColumnMap,
                        dataMatrix);

                    var financialHeaders = summary.ColumnMap.FinancialColumns
                        .Select(i => i >= 0 && i < summary.Headers.Count
                            ? summary.Headers[i]
                            : $"COL{i}")
                        .ToList();

                    var financialPreview = PrintMatrixPreview(financialHeaders, financialSection, 30);

                    // ============================
                    //  MATRIZ DE OBJETIVO (MAYÚSCULAS)
                    // ============================
                    var objectiveSection = BuildObjectiveMatrix(
                        summary.Headers,
                        summary.ColumnMap,
                        dataMatrix);

                    var objectiveHeaders = new List<string>();
                    var objectiveCol = summary.ColumnMap.ObjectiveColumn;

                    if (objectiveCol.HasValue &&
                        objectiveCol.Value >= 0 &&
                        objectiveCol.Value < summary.Headers.Count)
                    {
                        objectiveHeaders.Add(summary.Headers[objectiveCol.Value]);
                    }
                    else
                    {
                        objectiveHeaders.Add("OBJETIVO");
                    }

                    var objectivePreview = PrintMatrixPreview(objectiveHeaders, objectiveSection, 30);

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

                    var fullHeaders = fullColumnIndexes
                        .Select(i => i >= 0 && i < summary.Headers.Count
                            ? summary.Headers[i]
                            : $"COL{i}")
                        .ToList();

                    var fullPreview = PrintMatrixPreview(fullHeaders, fullMatrix, 30);

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
                        currentUserId,
                        ct);

                    if (!importResult.Success)
                    {
                        summary.Errors.Add(new ProjectMatrixUploadErrorDTO
                        {
                            RowNumber = 0,
                            Message = importResult.Message
                                      ?? "Error importing projects into database."
                        });
                    }

                    return ServiceResult<ProjectMatrixUploadSummaryDTO>.Ok(
                        summary,
                        "Matrix file processed.");
                }

                var notSupportedMessage =
                    $"File extension '{extension}' is not supported. Please upload an .xlsx file.";

                summary.Errors.Add(new ProjectMatrixUploadErrorDTO
                {
                    RowNumber = 0,
                    Message = notSupportedMessage
                });

                return ServiceResult<ProjectMatrixUploadSummaryDTO>.Ok(
                    summary,
                    notSupportedMessage);
            }
            catch (Exception ex)
            {
                return ServiceResult<ProjectMatrixUploadSummaryDTO>.Fail(
                    ex.Message,
                    ErrorType.Unexpected);
            }
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
                foreach (var row in dataMatrix)
                {
                    if (projectNameIndex < 0 || projectNameIndex >= row.Count)
                        continue;

                    var value = row[projectNameIndex];
                    if (string.IsNullOrWhiteSpace(value))
                        continue;

                    row[projectNameIndex] = value.ToUpperInvariant();
                }
            }

            // NORMALIZAR ESTADO A MAYÚSCULAS
            var estadoIndex = normalizedHeaders
                .FindIndex(h => h == NormalizeHeader("ESTADO"));

            if (estadoIndex >= 0)
            {
                foreach (var row in dataMatrix)
                {
                    if (estadoIndex < 0 || estadoIndex >= row.Count)
                        continue;

                    var value = row[estadoIndex];
                    if (string.IsNullOrWhiteSpace(value))
                        continue;

                    row[estadoIndex] = value.ToUpperInvariant();
                }
            }

            // EXTRAER SECCIÓN DE PROYECTO
            var projectSection = ExtractColumnsSection(dataMatrix, map.ProjectColumns);
            return projectSection;
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

            int natInstIdx = extCols.Count > 0 ? extCols[0] : -1;
            int natColIdx = extCols.Count > 1 ? extCols[1] : -1;
            int intInstIdx = extCols.Count > 2 ? extCols[2] : -1;
            int intColIdx = extCols.Count > 3 ? extCols[3] : -1;

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

            foreach (var idx in map.ProjectColumns ?? new List<int>())
                indexSet.Add(idx);

            foreach (var idx in map.FinancialColumns ?? new List<int>())
                indexSet.Add(idx);

            AddRangeIfPresent(map.ExtensionRange, indexSet);
            AddRangeIfPresent(map.VisitRange, indexSet);
            AddRangeIfPresent(map.ResearchCategoryRange, indexSet);

            if (map.ObjectiveColumn.HasValue)
                indexSet.Add(map.ObjectiveColumn.Value);

            foreach (var idx in map.DocumentColumns ?? new List<int>())
                indexSet.Add(idx);

            foreach (var idx in map.InternalMemberColumns ?? new List<int>())
                indexSet.Remove(idx);

            foreach (var idx in map.ExternalMemberColumns ?? new List<int>())
                indexSet.Remove(idx);

            fullColumnIndexes = indexSet
                .Where(i => i >= 0 && i < headers.Count)
                .OrderBy(i => i)
                .ToList();

            return ExtractColumnsSection(dataMatrix, fullColumnIndexes);
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
            if (idxCodigo < 0) idxCodigo = IndexOf("CÓDIGO");
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
            if (idxResPrimera < 0) idxResPrimera = IndexOf("RESOLUCION PRIMERA PRORROGA");

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
            if (idxLinea < 0) idxLinea = IndexOf("LINEA DE INVESTIGACION");

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

            for (int rowIndex = 0; rowIndex < dataMatrix.Count; rowIndex++)
            {
                var row = dataMatrix[rowIndex];

                var project = new ImportedProjectDTO
                {
                    CallCode = convocationColumnIndex.HasValue
                        ? SafeGet(row, convocationColumnIndex.Value)
                        : GetByHeader(headers, row, "CONVOCATORIA"),

                    ProjectCode = GetByHeader(headers, row, "CODIGO", "CÓDIGO"),
                    Number = ParseInt(GetByHeader(headers, row, "NRO.", "NRO")),
                    ProjectName = GetByHeader(headers, row, "PROYECTO"),
                    Faculty = GetByHeader(headers, row, "FACULTAD"),
                    State = GetByHeader(headers, row, "ESTADO"),
                    TermMonths = ParseInt(GetByHeader(headers, row, "PLAZO")),
                    StartDate = ParseDate(GetByHeader(headers, row, "FECHA DE INICIO")),
                    EstimatedEndDate = ParseDate(GetByHeader(headers, row,
                        "FECHA DE FINALIZACION ESTIMADA",
                        "FECHA DE FINALIZACIÓN ESTIMADA")),

                    AssignedValue = ParseDecimal(GetByHeader(headers, row, "VALOR ASIGNADO")),
                    ExecutedValue = ParseDecimal(GetByHeader(headers, row, "VALOR EJECUTADO")),
                    RemainingValue = ParseDecimal(GetByHeader(headers, row, "VALOR POR EJECUTAR")),
                    ExecutionPercentage = ParseDecimal(GetByHeader(headers, row,
                        "% EJECUTADO PRESUPUESTARIA",
                        "% EJECUTADO PRESUPUESTARIA ")),
                    ExecutionProgress = ParseDecimal(GetByHeader(headers, row,
                        "PORCENTAJE DE EJECUCION",
                        "PORCENTAJE DE EJECUCIÓN",
                        "PORCENTAJE DE EJECUTACION")),
                };

                project.ResearchLine = GetByHeader(headers, row,
                    "LÍNEA DE INVESTIGACIÓN",
                    "LINEA DE INVESTIGACION");
                project.Domain = GetByHeader(headers, row, "DOMINIO");
                project.BroadField = GetByHeader(headers, row, "CAMPO AMPLIO");
                project.SpecificField = GetByHeader(headers, row, "CAMPO ESPECIFICO");
                project.DetailedField = GetByHeader(headers, row, "CAMPO DETALLADO");
                project.TerritorialScope = GetByHeader(headers, row, "ALCANCE TERRITORIAL");
                project.ExpectedImpact = GetByHeader(headers, row, "IMPACTO ESPERADO");
                project.GeneralObjective = GetByHeader(headers, row, "OBJETIVO GENERAL");

                // ✅ COORDINADOR / COORDINADOR SUBROGANTE
                var coordinatorRaw = GetByHeader(headers, row, "COORDINADOR");
                var alternateCoordinatorRaw = GetByHeader(headers, row, "COORDINADOR SUBROGANTE");

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

            // ==========================================
            // 0) Si hay múltiples fechas (saltos de línea)
            // ==========================================
            if (value.Contains('\n') || value.Contains('\r'))
            {
                var parts = value
                    .Replace("\r\n", "\n")
                    .Replace('\r', '\n')
                    .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

                DateTime? maxDate = null;

                foreach (var part in parts)
                {
                    var parsed = ParseDate(part); // recursivo y seguro
                    if (parsed.HasValue)
                    {
                        if (!maxDate.HasValue || parsed.Value > maxDate.Value)
                            maxDate = parsed.Value;
                    }
                }

                return maxDate;
            }

            // ================================
            // 1) Caso especial: solo año "2017"
            // ================================
            if (Regex.IsMatch(value, @"^\d{4}$"))
            {
                if (int.TryParse(value, out var year) && year >= 1900 && year <= 2100)
                    return new DateTime(year, 1, 1);
            }

            // ==========================================
            // 2) Formatos explícitos
            // ==========================================
            var formats = new[]
            {
        "yyyy",
        "yyyy-MM-dd","yyyy/MM/dd","yyyy-M-d","yyyy/M/d",
        "dd/MM/yyyy","d/M/yyyy","dd-MM-yyyy","d-M-yyyy","dd.MM.yyyy","d.M.yyyy",
        "dd/MM/yy","d/M/yy","dd-MM-yy","d-M-yy",
        "yyyy-MM-dd HH:mm:ss","dd/MM/yyyy HH:mm:ss","dd-MM-yyyy HH:mm:ss"
    };

            if (DateTime.TryParseExact(
                    value,
                    formats,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AllowWhiteSpaces,
                    out var dtExact))
            {
                return dtExact;
            }

            // ==========================================
            // 3) Parse general (culturas)
            // ==========================================
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

            // ==========================================
            // 4) Excel / OA date (solo serial real)
            // ==========================================
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