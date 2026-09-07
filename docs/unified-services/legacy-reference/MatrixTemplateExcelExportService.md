# Referencia legacy — MatrixTemplateExcelExportService

## TODO UNIFIED: GenerateExcelAsync

Depende de GetFlatReportAsync pendiente por resolución Faculty local/externa.

Referencia exacta: `tesisproject.backend/Services/Implementations/MatrixTemplateExcelExportService.cs:53-221`. Firma omitida temporalmente del contrato Unified.

```csharp
        public async Task<ServiceResult<byte[]>> GenerateExcelAsync(
            ExportByTemplateRequestDTO request,
            CancellationToken ct = default)
        {
            if (request is null)
            {
                return ValidationFailure<byte[]>(
                    ErrorMessages.Common.RequestRequired,
                    ErrorCodes.Common.RequestRequired,
                    "Request");
            }

            if (request.TemplateId <= 0)
            {
                return ValidationFailure<byte[]>(
                    ErrorMessages.MatrixTemplateExport.InvalidTemplateId,
                    ErrorCodes.MatrixTemplateExport.InvalidTemplateId,
                    nameof(ExportByTemplateRequestDTO.TemplateId));
            }

            var includedColumnIds = request.IncludedTemplateColumnIds?
                .Distinct()
                .ToList() ?? new List<int>();

            if (includedColumnIds.Count == 0)
            {
                return ValidationFailure<byte[]>(
                    ErrorMessages.Export.NoColumnsDefined,
                    ErrorCodes.Export.NoColumnsDefined,
                    nameof(ExportByTemplateRequestDTO.IncludedTemplateColumnIds));
            }

            var templateResult = await _templateService.GetTemplateAsync(request.TemplateId, ct);

            if (!templateResult.Success)
            {
                return RelayFailure<byte[]>(
                    templateResult.Message,
                    templateResult.Error,
                    templateResult.ErrorCode,
                    templateResult.ValidationErrors,
                    ErrorMessages.MatrixTemplateExport.TemplateNotRecovered,
                    ErrorCodes.MatrixTemplateExport.TemplateNotRecovered);
            }

            if (templateResult.Data is null)
            {
                return ServiceResult<byte[]>.Fail(
                    ErrorMessages.ExportTemplate.NotFound,
                    ErrorType.NotFound,
                    ErrorCodes.ExportTemplate.NotFound);
            }

            var template = templateResult.Data;

            if (!template.IsActive)
            {
                return ValidationFailure<byte[]>(
                    ErrorMessages.MatrixTemplateExport.TemplateInactive,
                    ErrorCodes.MatrixTemplateExport.TemplateInactive,
                    nameof(ExportByTemplateRequestDTO.TemplateId));
            }

            if (template.Columns is null || template.Columns.Count == 0)
            {
                return ValidationFailure<byte[]>(
                    ErrorMessages.MatrixTemplateExport.TemplateWithoutColumns,
                    ErrorCodes.MatrixTemplateExport.TemplateWithoutColumns,
                    nameof(ExportTemplateDetailDTO.Columns));
            }

            var templateColumns = template.Columns
                .OrderBy(c => c.OrderIndex)
                .ToList();

            var templateColumnIds = templateColumns
                .Select(c => c.Id)
                .ToHashSet();

            var invalidIncludedIds = includedColumnIds
                .Where(id => !templateColumnIds.Contains(id))
                .ToList();

            if (invalidIncludedIds.Count > 0)
            {
                return ValidationFailure<byte[]>(
                    ErrorMessages.MatrixTemplateExport.IncludedColumnsNotInTemplate,
                    ErrorCodes.MatrixTemplateExport.IncludedColumnsNotInTemplate,
                    nameof(ExportByTemplateRequestDTO.IncludedTemplateColumnIds));
            }

            var requiredNotIncluded = templateColumns
                .Where(c => c.IsRequired && !includedColumnIds.Contains(c.Id))
                .Select(c => c.TargetHeader)
                .ToList();

            if (requiredNotIncluded.Count > 0)
            {
                return ValidationFailure<byte[]>(
                    ErrorMessages.MatrixTemplateExport.MissingRequiredColumns,
                    ErrorCodes.MatrixTemplateExport.MissingRequiredColumns,
                    nameof(ExportByTemplateRequestDTO.IncludedTemplateColumnIds));
            }

            var selectedTemplateColumns = templateColumns
                .Where(c => includedColumnIds.Contains(c.Id))
                .OrderBy(c => c.OrderIndex)
                .ToList();

            if (selectedTemplateColumns.Count == 0)
            {
                return ValidationFailure<byte[]>(
                    ErrorMessages.MatrixTemplateExport.NoValidSelectedColumns,
                    ErrorCodes.MatrixTemplateExport.NoValidSelectedColumns,
                    nameof(ExportByTemplateRequestDTO.IncludedTemplateColumnIds));
            }

            var flatResult = await _flatService.GetFlatReportAsync(request.ProjectIds, ct);

            if (!flatResult.Success)
            {
                return RelayFailure<byte[]>(
                    flatResult.Message,
                    flatResult.Error,
                    flatResult.ErrorCode,
                    flatResult.ValidationErrors,
                    ErrorMessages.Export.FlatReportUnavailable,
                    ErrorCodes.Export.FlatReportUnavailable);
            }

            if (flatResult.Data is null || flatResult.Data.Count == 0)
            {
                return ServiceResult<byte[]>.Fail(
                    ErrorMessages.Export.NoProjectsInFlatReport,
                    ErrorType.NotFound,
                    ErrorCodes.Export.NoProjectsInFlatReport);
            }

            var projects = flatResult.Data.ToList();

            var catResult = await _categoryService.GetTreeAsync(onlyActives: true, ct);
            var categoryTree = catResult.Success && catResult.Data is not null
                ? catResult.Data.ToList()
                : new List<ResearchCategoryTreeItemDTO>();

            var categoryById = BuildCategoryLookups(categoryTree);

            try
            {
                var bytes = GenerateExcelInternal(
                    request,
                    selectedTemplateColumns,
                    projects,
                    categoryById);

                return ServiceResult<byte[]>.Ok(bytes);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error al generar el Excel de matriz basado en plantilla persistida. TemplateId={TemplateId}",
                    request.TemplateId);

                return UnexpectedFailure<byte[]>(
                    ErrorMessages.Export.ExcelGenerationFailed,
                    ErrorCodes.Export.ExcelGenerationFailed);
            }
        }
```
