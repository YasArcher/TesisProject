# Referencia legacy — ExportTemplateExcelService

## TODO UNIFIED: GenerateExcelAsync

Depende de GetFlatReportAsync pendiente por resolución Faculty local/externa.

Referencia exacta: `tesisproject.backend/Services/Implementations/ExportTemplateExcelService.cs:87-146`. Firma omitida temporalmente del contrato Unified.

```csharp
        public async Task<ServiceResult<byte[]>> GenerateExcelAsync(
            ExportRequestDTO request,
            CancellationToken ct = default)
        {
            try
            {
                if (request is null)
                {
                    return ValidationFailure<byte[]>(
                        NullRequestMessage,
                        ErrorCodes.Common.InvalidRequest,
                        "Request");
                }

                if (request.Columns is null || request.Columns.Count == 0)
                {
                    return ValidationFailure<byte[]>(
                        NoColumnsDefinedMessage,
                        ErrorCodes.Export.NoColumnsDefined,
                        nameof(ExportRequestDTO.Columns));
                }

                var flatResult = await _flatService.GetFlatReportAsync(null, ct);
                if (!flatResult.Success || flatResult.Data is null)
                {
                    return ServiceResult<byte[]>.Fail(
                        flatResult.Message ?? FlatReportUnavailableMessage,
                        flatResult.Error == ErrorType.None ? ErrorType.Unexpected : flatResult.Error,
                        flatResult.ErrorCode ?? ErrorCodes.Export.FlatReportUnavailable,
                        flatResult.ValidationErrors);
                }

                var projects = flatResult.Data.ToList();
                if (projects.Count == 0)
                {
                    return ServiceResult<byte[]>.Fail(
                        NoProjectsInFlatReportMessage,
                        ErrorType.Validation,
                        ErrorCodes.Export.NoProjectsInFlatReport);
                }

                var bytes = GenerateExcelInternal(request, projects);
                return ServiceResult<byte[]>.Ok(bytes);
            }
            catch (OperationCanceledException)
            {
                return ServiceResult<byte[]>.Fail(
                    OperationCanceledMessage,
                    ErrorType.Unexpected,
                    ErrorCodes.Common.OperationCanceled);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al generar el Excel para la exportación solicitada.");
                return ServiceResult<byte[]>.Fail(
                    ErrorGeneratingExcelMessage,
                    ErrorType.Unexpected,
                    ErrorCodes.Export.ExcelGenerationFailed);
            }
        }
```
