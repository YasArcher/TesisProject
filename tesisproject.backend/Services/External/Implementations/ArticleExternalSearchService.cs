using System.Net;
using System.Text.Json;
using tesisproject.backend.Services.External.Interfaces;
using tesisproject.shared.DTOs.ExternalApis;
using tesisproject.shared.Responses;

namespace tesisproject.backend.Services.External.Implementations;

// Modulo articulos: consulta de solo lectura; ninguna respuesta externa se persiste aqui.
public sealed class ArticleExternalSearchService(HttpClient http, IConfiguration configuration, ILogger<ArticleExternalSearchService> logger)
    : IArticleExternalSearchService
{
    public async Task<ServiceResult<ExternalApiQueryResultDto>> SearchAsync(ExternalApiQueryRequest request, CancellationToken ct)
    {
        var isScopus = string.Equals(request.ProviderKey, "scopus", StringComparison.OrdinalIgnoreCase);
        if (!isScopus && !string.Equals(request.ProviderKey, "crossref", StringComparison.OrdinalIgnoreCase))
            return ServiceResult<ExternalApiQueryResultDto>.Fail(
                "Este proveedor todavia no esta disponible en la plataforma integrada.",
                ErrorType.Validation, "EXTERNAL_PROVIDER_NOT_AVAILABLE");

        var query = request.QueryText?.Trim() ?? string.Empty;
        if (query.Length is < 2 or > 200 || request.MaxResults is < 1 or > 100 ||
            request.QueryMode is not ("general" or "doi"))
            return ServiceResult<ExternalApiQueryResultDto>.Fail(
                "Revisa el texto, el modo y la cantidad de resultados de la consulta.",
                ErrorType.Validation, "INVALID_EXTERNAL_QUERY");

        var doi = query.Replace("https://doi.org/", "", StringComparison.OrdinalIgnoreCase)
            .Replace("http://doi.org/", "", StringComparison.OrdinalIgnoreCase)
            .Replace("doi:", "", StringComparison.OrdinalIgnoreCase).Trim();
        if (request.QueryMode == "doi" && (!doi.StartsWith("10.", StringComparison.Ordinal) ||
            !doi.Contains('/') || doi.Length > 200))
            return ServiceResult<ExternalApiQueryResultDto>.Fail(
                "Ingresa un DOI valido.", ErrorType.Validation, "INVALID_DOI");

        var apiKey = configuration["ExternalApis:Scopus:ApiKey"]?.Trim();
        if (isScopus && string.IsNullOrWhiteSpace(apiKey))
            return ServiceResult<ExternalApiQueryResultDto>.Fail(
                "Scopus todavia no tiene una llave configurada en el servidor.",
                ErrorType.Validation, "EXTERNAL_PROVIDER_NOT_CONFIGURED");

        var path = request.QueryMode == "doi"
            ? $"works/{Uri.EscapeDataString(doi)}"
            : $"works?query.bibliographic={Uri.EscapeDataString(query)}&rows={request.MaxResults}";
        var scopusQuery = request.QueryMode == "doi"
            ? $"DOI(\"{doi.Replace("\"", "", StringComparison.Ordinal)}\")"
            : $"TITLE-ABS-KEY(\"{query.Replace("\"", "", StringComparison.Ordinal)}\")";
        var target = isScopus
            ? $"https://api.elsevier.com/content/search/scopus?query={Uri.EscapeDataString(scopusQuery)}&count={request.MaxResults}&view=STANDARD"
            : path;

        try
        {
            using var outgoing = new HttpRequestMessage(HttpMethod.Get, target);
            if (isScopus)
            {
                outgoing.Headers.Add("X-ELS-APIKey", apiKey);
                outgoing.Headers.Accept.ParseAdd("application/json");
                var instToken = configuration["ExternalApis:Scopus:InstToken"]?.Trim();
                if (!string.IsNullOrWhiteSpace(instToken)) outgoing.Headers.Add("X-ELS-Insttoken", instToken);
            }
            using var response = await http.SendAsync(outgoing, HttpCompletionOption.ResponseHeadersRead, ct);
            if (response.StatusCode == HttpStatusCode.NotFound)
                return ServiceResult<ExternalApiQueryResultDto>.Ok(NewResult(isScopus, "No se encontraron resultados."));
            if (isScopus && response.StatusCode == HttpStatusCode.TooManyRequests)
                return ServiceResult<ExternalApiQueryResultDto>.Fail(
                    "Scopus alcanzo su limite temporal de consultas. Intentalo mas tarde.",
                    ErrorType.Unexpected, "EXTERNAL_PROVIDER_RATE_LIMITED");
            if (isScopus && response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
                return ServiceResult<ExternalApiQueryResultDto>.Fail(
                    "Scopus rechazo el acceso configurado en el servidor. Contacta a soporte.",
                    ErrorType.Unexpected, "EXTERNAL_PROVIDER_ACCESS_DENIED");
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("External provider {Provider} returned HTTP {StatusCode}", request.ProviderKey, (int)response.StatusCode);
                return ServiceResult<ExternalApiQueryResultDto>.Fail(
                    "La fuente externa no respondio. Intentalo mas tarde.", ErrorType.Unexpected,
                    "EXTERNAL_PROVIDER_UNAVAILABLE");
            }

            await using var stream = await response.Content.ReadAsStreamAsync(ct);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
            if (isScopus)
                return ServiceResult<ExternalApiQueryResultDto>.Ok(MapScopus(document.RootElement, request.MaxResults));
            var message = document.RootElement.GetProperty("message");
            var items = request.QueryMode == "doi"
                ? new[] { message }
                : message.GetProperty("items").EnumerateArray().Take(request.MaxResults).ToArray();
            var result = NewResult(false, "Consulta completada.");
            foreach (var item in items)
            {
                var title = First(item, "title");
                if (string.IsNullOrWhiteSpace(title)) continue;
                var authors = item.TryGetProperty("author", out var authorArray) && authorArray.ValueKind == JsonValueKind.Array
                    ? authorArray.EnumerateArray().Select(a =>
                        string.Join(" ", new[] { Value(a, "given"), Value(a, "family") }
                            .Where(s => !string.IsNullOrWhiteSpace(s)))).Where(s => s.Length > 0).ToList()
                    : new List<string>();
                result.Articles.Add(new ExternalArticlePreviewDto
                {
                    Title = title,
                    Doi = Value(item, "DOI"),
                    JournalName = First(item, "container-title"),
                    IssnCode = First(item, "ISSN"),
                    Publisher = Value(item, "publisher"),
                    Volume = Value(item, "volume"),
                    Issue = Value(item, "issue"),
                    PageRange = Value(item, "page"),
                    DocumentType = Value(item, "type"),
                    PublicationYear = Year(item),
                    Authors = string.Join(", ", authors),
                    AuthorNames = authors,
                    CitationCount = Number(item, "is-referenced-by-count"),
                    SourceUrl = Value(item, "URL"),
                    ExternalId = Value(item, "DOI"),
                    ExternalSource = "Crossref"
                });
            }
            return ServiceResult<ExternalApiQueryResultDto>.Ok(result);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            return ServiceResult<ExternalApiQueryResultDto>.Fail(
                "La consulta tardo demasiado. Intentalo nuevamente.", ErrorType.Unexpected,
                "EXTERNAL_PROVIDER_TIMEOUT");
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "External article search request failed");
            return ServiceResult<ExternalApiQueryResultDto>.Fail(
                "No fue posible conectar con la fuente externa.", ErrorType.Unexpected,
                "EXTERNAL_PROVIDER_UNAVAILABLE");
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "External article search response was invalid");
            return ServiceResult<ExternalApiQueryResultDto>.Fail(
                "La fuente externa devolvio una respuesta que no se pudo interpretar.",
                ErrorType.Unexpected, "EXTERNAL_PROVIDER_INVALID_RESPONSE");
        }
        catch (Exception ex) when (ex is KeyNotFoundException or InvalidOperationException)
        {
            logger.LogWarning(ex, "External article search response had an unexpected shape");
            return ServiceResult<ExternalApiQueryResultDto>.Fail(
                "La fuente externa devolvio una respuesta que no se pudo interpretar.",
                ErrorType.Unexpected, "EXTERNAL_PROVIDER_INVALID_RESPONSE");
        }
    }

    private static ExternalApiQueryResultDto MapScopus(JsonElement root, int maxResults)
    {
        var searchResults = root.GetProperty("search-results");
        if (!searchResults.TryGetProperty("entry", out var entries))
            return NewResult(true, "No se encontraron resultados.");
        if (entries.ValueKind != JsonValueKind.Array) throw new JsonException("Scopus entries are not an array.");
        var result = NewResult(true, "Consulta completada.");
        foreach (var item in entries.EnumerateArray().Take(maxResults))
        {
            var title = Value(item, "dc:title");
            if (string.IsNullOrWhiteSpace(title)) continue;
            var coverDate = Value(item, "prism:coverDate");
            var eid = Value(item, "eid");
            var scopusId = eid?.StartsWith("2-s2.0-", StringComparison.Ordinal) == true ? eid[7..] : eid;
            var author = Value(item, "dc:creator");
            result.Articles.Add(new ExternalArticlePreviewDto
            {
                Title = title,
                Doi = Value(item, "prism:doi"),
                JournalName = Value(item, "prism:publicationName"),
                IssnCode = Value(item, "prism:issn"),
                EIssnCode = Value(item, "prism:eIssn"),
                Volume = Value(item, "prism:volume"),
                Issue = Value(item, "prism:issueIdentifier"),
                PageRange = Value(item, "prism:pageRange"),
                DocumentType = Value(item, "subtypeDescription"),
                PublicationDate = coverDate,
                PublicationYear = coverDate is { Length: >= 4 } && int.TryParse(coverDate[..4], out var year) ? year : null,
                Authors = author,
                AuthorNames = string.IsNullOrWhiteSpace(author) ? [] : [author],
                CitationCount = int.TryParse(Value(item, "citedby-count"), out var citations) ? citations : null,
                SourceUrl = Value(item, "prism:url"),
                ExternalId = eid,
                ScopusId = scopusId,
                ExternalSource = "Scopus"
            });
        }
        return result;
    }

    private static ExternalApiQueryResultDto NewResult(bool isScopus, string message) => new()
    {
        ProviderKey = isScopus ? "scopus" : "crossref", ProviderName = isScopus ? "Scopus" : "Crossref", Success = true,
        Message = message, ExecutedAtUtc = DateTime.UtcNow
    };

    private static string? Value(JsonElement element, string name)
        => element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString() : null;

    private static string? First(JsonElement element, string name)
        => element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.Array &&
           value.GetArrayLength() > 0 && value[0].ValueKind == JsonValueKind.String
            ? value[0].GetString() : null;

    private static int? Number(JsonElement element, string name)
        => element.TryGetProperty(name, out var value) && value.TryGetInt32(out var number) ? number : null;

    private static int? Year(JsonElement item)
    {
        foreach (var name in new[] { "published", "published-print", "published-online", "issued" })
            if (item.TryGetProperty(name, out var date) && date.TryGetProperty("date-parts", out var parts) &&
                parts.ValueKind == JsonValueKind.Array && parts.GetArrayLength() > 0 &&
                parts[0].ValueKind == JsonValueKind.Array && parts[0].GetArrayLength() > 0 &&
                parts[0][0].TryGetInt32(out var year)) return year;
        return null;
    }
}
