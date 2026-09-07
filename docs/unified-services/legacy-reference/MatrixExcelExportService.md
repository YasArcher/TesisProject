# Referencia legacy — MatrixExcelExportService

## TODO UNIFIED: GenerateExcelAsync

Depende de GetFlatReportAsync pendiente por resolución Faculty local/externa.

Referencia exacta: `tesisproject.backend/Services/Implementations/MatrixExcelExportService.cs:37-117`. Firma omitida temporalmente del contrato Unified.

```csharp
        public async Task<ServiceResult<byte[]>> GenerateExcelAsync(IEnumerable<int>? projectIds = null, CancellationToken ct = default)
        {
            // 1) Cargar dataset plano
            var flatResult = await _flatService.GetFlatReportAsync(projectIds, ct);

            if (!flatResult.Success)
            {
                return ServiceResult<byte[]>.Fail(
                    flatResult.Message ?? ErrorMessages.Export.FlatReportUnavailable,
                    flatResult.Error == ErrorType.None ? ErrorType.Unexpected : flatResult.Error,
                    flatResult.ErrorCode ?? ErrorCodes.Export.FlatReportUnavailable,
                    flatResult.ValidationErrors);
            }

            if (flatResult.Data is null || flatResult.Data.Count == 0)
            {
                return ServiceResult<byte[]>.Fail(
                    ErrorMessages.Export.NoProjectsInFlatReport,
                    ErrorType.Validation,
                    ErrorCodes.Export.NoProjectsInFlatReport);
            }

            var projects = flatResult.Data.ToList();

            // 2) Cargar árbol de categorías
            var catResult = await _categoryService.GetTreeAsync(onlyActives: true, ct);
            var categoryTree = catResult.Success && catResult.Data is not null
                ? catResult.Data.ToList()
                : new List<ResearchCategoryTreeItemDTO>();

            // 3) Preparar lookups de categorías (igual que en el front)
            var categoryById = BuildCategoryLookups(categoryTree);

            // 4) Construir metadata de columnas (base + dinámicas)
            var columns = new List<ColumnDef>();

            // 4.1) Columnas base (copiadas de tu MatrixConfigTab)
            columns.AddRange(GetBaseColumns());

            // 4.2) Columnas dinámicas de categorías
            columns.AddRange(BuildCategoryColumnsMetadata(categoryById));

            // 4.3) Columnas dinámicas de objetivos
            columns.AddRange(BuildObjectiveColumnsMetadata(projects));

            // Ordenar columnas una sola vez (mismo resultado, menos duplicación)
            var orderedColumns = columns.OrderBy(c => c.Order).ToList();

            // 5) Generar Excel
            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add(WorksheetName);

            var row = 1;
            var col = 1;

            // 5.1) Encabezados
            foreach (var column in orderedColumns)
            {
                worksheet.Cells[row, col].Value = column.Header;
                col++;
            }

            // 5.2) Filas de datos
            row = 2;
            foreach (var project in projects)
            {
                col = 1;
                foreach (var column in orderedColumns)
                {
                    worksheet.Cells[row, col].Value = column.Selector(project);
                    col++;
                }
                row++;
            }

            // Ajustar ancho
            worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

            var bytes = package.GetAsByteArray();
            return ServiceResult<byte[]>.Ok(bytes);
        }
```
