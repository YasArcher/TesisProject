using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;
using tesisproject.backend.Options;
using tesisproject.backend.Services.Unified.Interfaces;
using tesisproject.shared.Entities.External;
using tesisproject.shared.Errors;
using tesisproject.shared.Responses;

namespace tesisproject.backend.Services.Unified.Implementations;

public sealed class UnifiedAcademicCatalogSnapshotClient(HttpClient http, IOptions<ExternalApiOptions> options)
    : IUnifiedAcademicCatalogSnapshotClient
{
    public Task<ServiceResult<List<ExternalFacultyCareerFlatModel>>> GetFacultiesAsync(CancellationToken ct = default)
        => GetAsync<ExternalFacultyCareerFlatModel>(options.Value.AcademicsEndpoint, ct);

    public Task<ServiceResult<List<ExternalAcademicPeriodModel>>> GetAcademicTermsAsync(CancellationToken ct = default)
        => GetAsync<ExternalAcademicPeriodModel>(options.Value.PeriodsEndpoint, ct);

    private async Task<ServiceResult<List<T>>> GetAsync<T>(string endpoint, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(endpoint))
            return ServiceResult<List<T>>.Fail(ErrorMessages.CatalogSynchronization.ProviderUnavailable,
                ErrorType.Unexpected, ErrorCodes.CatalogSynchronization.ProviderUnavailable);
        try
        {
            using var response = await http.GetAsync(endpoint, HttpCompletionOption.ResponseHeadersRead, ct);
            if (!response.IsSuccessStatusCode)
                return ServiceResult<List<T>>.Fail(ErrorMessages.CatalogSynchronization.ProviderUnavailable,
                    ErrorType.Unexpected, ErrorCodes.CatalogSynchronization.ProviderUnavailable);
            var rows = await response.Content.ReadFromJsonAsync<List<T>>(cancellationToken: ct);
            return rows is null
                ? ServiceResult<List<T>>.Fail(ErrorMessages.CatalogSynchronization.InvalidResponse,
                    ErrorType.Validation, ErrorCodes.CatalogSynchronization.InvalidResponse)
                : ServiceResult<List<T>>.Ok(rows);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (JsonException)
        {
            return ServiceResult<List<T>>.Fail(ErrorMessages.CatalogSynchronization.InvalidResponse,
                ErrorType.Validation, ErrorCodes.CatalogSynchronization.InvalidResponse);
        }
        catch (Exception ex) when (ex is HttpRequestException or OperationCanceledException or IOException)
        {
            return ServiceResult<List<T>>.Fail(ErrorMessages.CatalogSynchronization.ProviderUnavailable,
                ErrorType.Unexpected, ErrorCodes.CatalogSynchronization.ProviderUnavailable);
        }
    }
}
