using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Options;
using tesisproject.backend.Options;
using tesisproject.backend.Services.Interfaces;
using tesisproject.shared.Entities.External;
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

    public ExternalDirectoryClient(
        HttpClient http,
        IOptions<ExternalApiOptions> opts,
        ILogger<ExternalDirectoryClient> logger)
    {
        _http = http;
        _logger = logger;
        _opts = opts.Value;

        // Safety: allow setting BaseAddress via options if named client didn't set it.
        if (_http.BaseAddress is null && !string.IsNullOrWhiteSpace(_opts.BaseUrl))
            _http.BaseAddress = new Uri(_opts.BaseUrl);
    }

    public async Task<ServiceResult<IReadOnlyList<ExternalProfileDTO>>> GetByEmailsAsync(
        IEnumerable<string> emails,
        CancellationToken ct = default)
    {
        var list = NormalizeDistinct(emails);
        if (list.Count == 0)
            return ServiceResult<IReadOnlyList<ExternalProfileDTO>>.Fail("At least one email is required.", ErrorType.Validation);

        try
        {
            var url = BuildBatchUrl(_opts.UsersEndpoint, _opts.UsersEmailQueryParam, list);
            var api = await _http.GetFromJsonAsync<List<ExternalProfileDTO>>(url, _jsonOpts, ct);

            if (api is null || api.Count == 0)
                return ServiceResult<IReadOnlyList<ExternalProfileDTO>>.Fail("No external profiles found.", ErrorType.NotFound);

            _logger.LogInformation("Retrieved {Count} profiles by email(s).", api.Count);
            return ServiceResult<IReadOnlyList<ExternalProfileDTO>>.Ok(api, "External profiles retrieved by emails");
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            _logger.LogWarning(ex, "Directory 404 for emails query");
            return ServiceResult<IReadOnlyList<ExternalProfileDTO>>.Fail("No external profiles found.", ErrorType.NotFound);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error querying external profiles by emails");
            return ServiceResult<IReadOnlyList<ExternalProfileDTO>>.Fail("Unexpected error.", ErrorType.Unexpected);
        }
    }

    public async Task<ServiceResult<IReadOnlyList<ExternalProfileDTO>>> GetByDocumentsAsync(
        IEnumerable<string> documents,
        CancellationToken ct = default)
    {
        var list = NormalizeDistinct(documents);
        if (list.Count == 0)
            return ServiceResult<IReadOnlyList<ExternalProfileDTO>>.Fail("At least one document is required.", ErrorType.Validation);

        try
        {
            var url = BuildBatchUrl(_opts.UsersEndpoint, _opts.UsersDocumentQueryParam, list);
            var api = await _http.GetFromJsonAsync<List<ExternalProfileDTO>>(url, _jsonOpts, ct);

            if (api is null || api.Count == 0)
                return ServiceResult<IReadOnlyList<ExternalProfileDTO>>.Fail("No external profiles found.", ErrorType.NotFound);

            _logger.LogInformation("Retrieved {Count} profiles by document(s).", api.Count);
            return ServiceResult<IReadOnlyList<ExternalProfileDTO>>.Ok(api, "External profiles retrieved by documents");
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            _logger.LogWarning(ex, "Directory 404 for documents query");
            return ServiceResult<IReadOnlyList<ExternalProfileDTO>>.Fail("No external profiles found.", ErrorType.NotFound);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error querying external profiles by documents");
            return ServiceResult<IReadOnlyList<ExternalProfileDTO>>.Fail("Unexpected error.", ErrorType.Unexpected);
        }
    }

    public async Task<ServiceResult<IReadOnlyList<ExternalProfileDTO>>> GetAllAsync(CancellationToken ct = default)
    {
        try
        {
            var api = await _http.GetFromJsonAsync<List<ExternalProfileDTO>>(_opts.UsersEndpoint, _jsonOpts, ct);

            if (api is null || api.Count == 0)
                return ServiceResult<IReadOnlyList<ExternalProfileDTO>>.Fail("No external profiles found.", ErrorType.NotFound);

            _logger.LogInformation("Retrieved {Count} profiles from directory.", api.Count);
            return ServiceResult<IReadOnlyList<ExternalProfileDTO>>.Ok(api, "All external profiles retrieved");
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            _logger.LogWarning(ex, "Directory 404 on GetAll");
            return ServiceResult<IReadOnlyList<ExternalProfileDTO>>.Fail("No external profiles found.", ErrorType.NotFound);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error retrieving all external profiles");
            return ServiceResult<IReadOnlyList<ExternalProfileDTO>>.Fail("Unexpected error.", ErrorType.Unexpected);
        }
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
}
