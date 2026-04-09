using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;
using tesisproject.backend.Options;
using tesisproject.backend.Services.Interfaces;
using tesisproject.shared.Entities.External;
using tesisproject.shared.Errors;
using tesisproject.shared.Responses;

public sealed class ExternalDirectoryClient : IExternalDirectoryClient
{
    private readonly HttpClient _http;
    private readonly ILogger<ExternalDirectoryClient> _logger;
    private readonly ExternalApiOptions _opts;

    private static readonly JsonSerializerOptions _jsonOpts = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    private const string MsgProfilesRetrievedByEmails = "External profiles retrieved by emails";
    private const string MsgProfilesRetrievedByDocuments = "External profiles retrieved by documents";
    private const string MsgAllProfilesRetrieved = "All external profiles retrieved";

    public ExternalDirectoryClient(
        HttpClient http,
        IOptions<ExternalApiOptions> opts,
        ILogger<ExternalDirectoryClient> logger)
    {
        _http = http;
        _logger = logger;
        _opts = opts.Value;

        if (_http.BaseAddress is null && !string.IsNullOrWhiteSpace(_opts.BaseUrl))
            _http.BaseAddress = new Uri(_opts.BaseUrl);
    }

    public Task<ServiceResult<IReadOnlyList<ExternalUserProfileModel>>> GetByEmailsAsync(
        IEnumerable<string> emails,
        CancellationToken ct = default)
        => QueryByAsync(
            values: emails,
            queryParamName: _opts.UsersEmailQueryParam,
            requiredFailureFactory: AtLeastOneEmailRequired,
            successMessage: MsgProfilesRetrievedByEmails,
            ct: ct);

    public Task<ServiceResult<IReadOnlyList<ExternalUserProfileModel>>> GetByDocumentsAsync(
        IEnumerable<string> documents,
        CancellationToken ct = default)
        => QueryByAsync(
            values: documents,
            queryParamName: _opts.UsersDocumentQueryParam,
            requiredFailureFactory: AtLeastOneDocumentRequired,
            successMessage: MsgProfilesRetrievedByDocuments,
            ct: ct);

    public async Task<ServiceResult<IReadOnlyList<ExternalUserProfileModel>>> GetAllAsync(
        CancellationToken ct = default)
    {
        var cfgFail = ValidateConfig(out var endpoint);
        if (cfgFail is not null)
            return cfgFail;

        return await FetchAsync(
            url: endpoint,
            successMessage: MsgAllProfilesRetrieved,
            logContext: "GetAll",
            ct: ct);
    }

    // ============== Core logic (DRY) ==============

    private async Task<ServiceResult<IReadOnlyList<ExternalUserProfileModel>>> QueryByAsync(
        IEnumerable<string> values,
        string queryParamName,
        Func<ServiceResult<IReadOnlyList<ExternalUserProfileModel>>> requiredFailureFactory,
        string successMessage,
        CancellationToken ct)
    {
        var list = NormalizeDistinct(values);
        if (list.Count == 0)
            return requiredFailureFactory();

        var cfgFail = ValidateConfig(out var endpoint, queryParamName);
        if (cfgFail is not null)
            return cfgFail;

        var url = BuildBatchUrl(endpoint, queryParamName, list);

        return await FetchAsync(
            url: url,
            successMessage: successMessage,
            logContext: $"Batch:{queryParamName}",
            ct: ct);
    }

    private async Task<ServiceResult<IReadOnlyList<ExternalUserProfileModel>>> FetchAsync(
        string url,
        string successMessage,
        string logContext,
        CancellationToken ct)
    {
        try
        {
            var api = await _http.GetFromJsonAsync<List<ExternalUserProfileModel>>(url, _jsonOpts, ct);

            if (api is null || api.Count == 0)
                return NoProfilesFound();

            _logger.LogInformation("Directory {Context}: retrieved {Count} profile(s).", logContext, api.Count);

            return ServiceResult<IReadOnlyList<ExternalUserProfileModel>>.Ok(api, successMessage);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            _logger.LogWarning(ex, "Directory {Context}: 404", logContext);
            return NoProfilesFound();
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.Unauthorized)
        {
            _logger.LogWarning(ex, "Directory {Context}: 401", logContext);
            return UnauthorizedExternalApi();
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.Forbidden)
        {
            _logger.LogWarning(ex, "Directory {Context}: 403", logContext);
            return ForbiddenExternalApi();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Directory {Context}: unexpected error", logContext);
            return Unexpected();
        }
    }

    /// <summary>
    /// Valida configuración requerida. Devuelve null si OK; si no, devuelve ServiceResult.Fail listo.
    /// </summary>
    private ServiceResult<IReadOnlyList<ExternalUserProfileModel>>? ValidateConfig(
        out string endpoint,
        string? queryParamName = null)
    {
        endpoint = _opts.UsersEndpoint ?? string.Empty;

        if (string.IsNullOrWhiteSpace(endpoint))
            return ConfigEndpointMissing();

        if (queryParamName is not null && string.IsNullOrWhiteSpace(queryParamName))
            return ConfigParamMissing();

        return null;
    }

    // ============== Helpers ==============

    private static List<string> NormalizeDistinct(IEnumerable<string> source) =>
        source?
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Select(s => s.Trim())
            .Where(s => s.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList()
        ?? new List<string>();

    private static string BuildBatchUrl(string baseEndpoint, string queryParamName, IReadOnlyList<string> values)
    {
        var joined = string.Join(",", values.Select(Uri.EscapeDataString));
        var sep = baseEndpoint.Contains('?') ? "&" : "?";
        return $"{baseEndpoint}{sep}{queryParamName}={joined}";
    }

    // ============== Error helpers ==============

    private static ServiceResult<IReadOnlyList<ExternalUserProfileModel>> NoProfilesFound()
        => ServiceResult<IReadOnlyList<ExternalUserProfileModel>>.Fail(
            ErrorMessages.ExternalDirectory.NoProfilesFound,
            ErrorType.NotFound,
            ErrorCodes.ExternalDirectory.NoProfilesFound);

    private static ServiceResult<IReadOnlyList<ExternalUserProfileModel>> ConfigEndpointMissing()
        => ServiceResult<IReadOnlyList<ExternalUserProfileModel>>.Fail(
            ErrorMessages.ExternalDirectory.ConfigEndpointMissing,
            ErrorType.Unexpected,
            ErrorCodes.ExternalDirectory.ConfigEndpointMissing);

    private static ServiceResult<IReadOnlyList<ExternalUserProfileModel>> ConfigParamMissing()
        => ServiceResult<IReadOnlyList<ExternalUserProfileModel>>.Fail(
            ErrorMessages.ExternalDirectory.ConfigParamMissing,
            ErrorType.Unexpected,
            ErrorCodes.ExternalDirectory.ConfigParamMissing);

    private static ServiceResult<IReadOnlyList<ExternalUserProfileModel>> UnauthorizedExternalApi()
        => ServiceResult<IReadOnlyList<ExternalUserProfileModel>>.Fail(
            ErrorMessages.ExternalDirectory.UnauthorizedExternalApi,
            ErrorType.Unauthorized,
            ErrorCodes.ExternalDirectory.UnauthorizedExternalApi);

    private static ServiceResult<IReadOnlyList<ExternalUserProfileModel>> ForbiddenExternalApi()
        => ServiceResult<IReadOnlyList<ExternalUserProfileModel>>.Fail(
            ErrorMessages.ExternalDirectory.ForbiddenExternalApi,
            ErrorType.Forbidden,
            ErrorCodes.ExternalDirectory.ForbiddenExternalApi);

    private static ServiceResult<IReadOnlyList<ExternalUserProfileModel>> AtLeastOneEmailRequired()
        => ServiceResult<IReadOnlyList<ExternalUserProfileModel>>.Fail(
            ErrorMessages.ExternalDirectory.AtLeastOneEmailRequired,
            ErrorType.Validation,
            ErrorCodes.ExternalDirectory.AtLeastOneEmailRequired);

    private static ServiceResult<IReadOnlyList<ExternalUserProfileModel>> AtLeastOneDocumentRequired()
        => ServiceResult<IReadOnlyList<ExternalUserProfileModel>>.Fail(
            ErrorMessages.ExternalDirectory.AtLeastOneDocumentRequired,
            ErrorType.Validation,
            ErrorCodes.ExternalDirectory.AtLeastOneDocumentRequired);

    private static ServiceResult<IReadOnlyList<ExternalUserProfileModel>> Unexpected()
        => ServiceResult<IReadOnlyList<ExternalUserProfileModel>>.Fail(
            ErrorMessages.Common.UnexpectedError,
            ErrorType.Unexpected,
            ErrorCodes.Common.UnexpectedError);
}