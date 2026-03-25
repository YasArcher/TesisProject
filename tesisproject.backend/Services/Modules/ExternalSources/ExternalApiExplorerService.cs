using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Options;
using tesisproject.backend.Options;
using tesisproject.backend.Services.Interfaces;
using tesisproject.shared.DTOs.ExternalApis;

namespace tesisproject.backend.Services.Implementations
{
    public class ExternalApiExplorerService : IExternalApiExplorerService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ExternalApiExplorerOptions _options;

        public ExternalApiExplorerService(
            IHttpClientFactory httpClientFactory,
            IOptions<ExternalApiExplorerOptions> options)
        {
            _httpClientFactory = httpClientFactory;
            _options = options.Value;
        }

        public Task<List<ExternalApiProviderDto>> GetProvidersAsync(CancellationToken ct = default)
            => Task.FromResult(new List<ExternalApiProviderDto>
            {
                new()
                {
                    ProviderKey = "scopus",
                    DisplayName = "Scopus",
                    Description = "Proveedor autenticado para búsquedas indexadas y recuperación de metadatos académicos de Elsevier.",
                    BaseUrl = "https://api.elsevier.com/content",
                    RequiresApiKey = true,
                    IsConfigured = !string.IsNullOrWhiteSpace(_options.Scopus.ApiKey),
                    SearchHint = "Prueba con DOI exacto o una consulta Scopus como TITLE-ABS-KEY(machine learning)."
                },
                new()
                {
                    ProviderKey = "crossref",
                    DisplayName = "Crossref",
                    Description = "Ideal para probar DOI, título de artículo y metadatos bibliográficos públicos.",
                    BaseUrl = "https://api.crossref.org",
                    RequiresApiKey = false,
                    IsConfigured = true,
                    SearchHint = "Prueba con DOI exacto o palabras del título."
                },
                new()
                {
                    ProviderKey = "openalex",
                    DisplayName = "OpenAlex",
                    Description = "Buena opción para búsquedas abiertas de works, autores y fuentes.",
                    BaseUrl = "https://api.openalex.org",
                    RequiresApiKey = false,
                    IsConfigured = true,
                    SearchHint = "Prueba con DOI, título o texto libre."
                },
                new()
                {
                    ProviderKey = "semantic-scholar",
                    DisplayName = "Semantic Scholar",
                    Description = "Útil para probar título, DOI y autores en un índice académico amplio.",
                    BaseUrl = "https://api.semanticscholar.org/graph/v1",
                    RequiresApiKey = false,
                    IsConfigured = true,
                    SearchHint = "Prueba con título o DOI. Algunas rutas pueden tener límites más estrictos."
                }
            });

        public async Task<ExternalApiQueryResultDto> QueryAsync(ExternalApiQueryRequest request, CancellationToken ct = default)
        {
            var provider = (await GetProvidersAsync(ct)).FirstOrDefault(x => x.ProviderKey == request.ProviderKey)
                ?? throw new InvalidOperationException("El proveedor solicitado no está configurado.");

            if (string.IsNullOrWhiteSpace(request.QueryText))
            {
                var requiresStructuredAuthorInput = request.QueryMode is "author" or "author-coauthor";
                if (!requiresStructuredAuthorInput)
                {
                    throw new InvalidOperationException("Debes ingresar un criterio de búsqueda antes de consultar la API externa.");
                }
            }

            if (provider.RequiresApiKey && !provider.IsConfigured)
            {
                throw new InvalidOperationException($"El proveedor {provider.DisplayName} todavía no tiene llave configurada en el backend.");
            }

            var client = _httpClientFactory.CreateClient("external-api-explorer");
            ConfigureHeaders(client, provider.ProviderKey);

            var execution = await ExecuteProviderQueryAsync(client, provider.ProviderKey, request, ct);

            return new ExternalApiQueryResultDto
            {
                ProviderKey = provider.ProviderKey,
                ProviderName = provider.DisplayName,
                FinalRequestUrl = execution.FinalRequestUrl,
                Success = execution.Success,
                StatusCode = execution.StatusCode,
                Message = execution.Message,
                RawResponsePreview = execution.RawResponse.Length > 12000 ? execution.RawResponse[..12000] : execution.RawResponse,
                ExecutedAtUtc = DateTime.UtcNow,
                Articles = execution.Success ? ParseArticles(provider.ProviderKey, execution.RawResponse) : new List<ExternalArticlePreviewDto>()
            };
        }

        public async Task<ExternalArticlePreviewDto> EnrichArticleAsync(string providerKey, ExternalArticlePreviewDto article, CancellationToken ct = default)
        {
            if (article is null)
            {
                throw new InvalidOperationException("No llegó un artículo para enriquecer.");
            }

            if (!string.Equals(providerKey, "scopus", StringComparison.OrdinalIgnoreCase))
            {
                return article;
            }

            var scopusId = ExtractScopusId(article);
            if (string.IsNullOrWhiteSpace(scopusId))
            {
                return article;
            }

            var client = _httpClientFactory.CreateClient("external-api-explorer");
            ConfigureHeaders(client, providerKey);

            foreach (var url in BuildScopusEnrichmentUrls(scopusId))
            {
                using var response = await client.GetAsync(url, ct);
                var raw = await response.Content.ReadAsStringAsync(ct);
                if (!response.IsSuccessStatusCode)
                {
                    continue;
                }

                var enriched = ParseArticles("scopus", raw).FirstOrDefault();
                if (enriched is not null)
                {
                    return MergeArticle(article, enriched);
                }
            }

            return article;
        }

        private async Task<ProviderExecutionResult> ExecuteProviderQueryAsync(HttpClient client, string providerKey, ExternalApiQueryRequest request, CancellationToken ct)
        {
            if (!string.Equals(providerKey, "scopus", StringComparison.OrdinalIgnoreCase))
            {
                var url = BuildProviderUrl(providerKey, request);
                using var response = await client.GetAsync(url, ct);
                var raw = await response.Content.ReadAsStringAsync(ct);
                return new ProviderExecutionResult
                {
                    FinalRequestUrl = url,
                    Success = response.IsSuccessStatusCode,
                    StatusCode = (int)response.StatusCode,
                    Message = BuildResponseMessage(GetProviderName(providerKey), response.IsSuccessStatusCode, response.StatusCode),
                    RawResponse = raw
                };
            }

            return await ExecuteScopusQueryAsync(client, request, ct);
        }

        private async Task<ProviderExecutionResult> ExecuteScopusQueryAsync(HttpClient client, ExternalApiQueryRequest request, CancellationToken ct)
        {
            var attempts = BuildScopusAttemptUrls(request).ToList();
            ProviderExecutionResult? lastResult = null;

            foreach (var attempt in attempts)
            {
                using var response = await client.GetAsync(attempt.Url, ct);
                var raw = await response.Content.ReadAsStringAsync(ct);
                var success = response.IsSuccessStatusCode;
                var statusCode = (int)response.StatusCode;

                if (success)
                {
                    return new ProviderExecutionResult
                    {
                        FinalRequestUrl = attempt.Url,
                        Success = true,
                        StatusCode = statusCode,
                        Message = attempt.Note is null
                            ? "Consulta completada contra Scopus."
                            : $"Consulta completada contra Scopus usando {attempt.Note}.",
                        RawResponse = raw
                    };
                }

                lastResult = new ProviderExecutionResult
                {
                    FinalRequestUrl = attempt.Url,
                    Success = false,
                    StatusCode = statusCode,
                    Message = attempt.Note is null
                        ? BuildResponseMessage("Scopus", false, response.StatusCode)
                        : $"{BuildResponseMessage("Scopus", false, response.StatusCode)} Intento realizado con {attempt.Note}.",
                    RawResponse = raw
                };

                if (response.StatusCode is System.Net.HttpStatusCode.Forbidden or System.Net.HttpStatusCode.Unauthorized)
                {
                    continue;
                }

                break;
            }

            return lastResult ?? new ProviderExecutionResult
            {
                FinalRequestUrl = string.Empty,
                Success = false,
                StatusCode = 500,
                Message = "No fue posible ejecutar la consulta contra Scopus.",
                RawResponse = "{}"
            };
        }

        private static IEnumerable<ScopusAttempt> BuildScopusAttemptUrls(ExternalApiQueryRequest request)
        {
            var query = request.QueryText.Trim();
            var encodedQuery = Uri.EscapeDataString(query);
            var maxResults = Math.Clamp(request.MaxResults, 1, 25);
            var mode = (request.QueryMode ?? "general").Trim().ToLowerInvariant();

            if (mode == "doi")
            {
                yield return new ScopusAttempt($"https://api.elsevier.com/content/abstract/doi/{encodedQuery}?view=META", "vista META");
                yield return new ScopusAttempt($"https://api.elsevier.com/content/abstract/doi/{encodedQuery}", "vista por defecto");
                yield return new ScopusAttempt($"https://api.elsevier.com/content/search/scopus?query={Uri.EscapeDataString($"DOI({query})")}&count=1&view=STANDARD", "búsqueda Scopus por DOI");
                yield break;
            }

            if (mode == "author")
            {
                var authorQuery = BuildScopusAuthorQuery(request.PrimaryAuthor);
                if (string.IsNullOrWhiteSpace(authorQuery))
                {
                    throw new InvalidOperationException("Debes informar el autor principal para buscar por autor.");
                }

                yield return new ScopusAttempt($"https://api.elsevier.com/content/search/scopus?query={Uri.EscapeDataString(authorQuery)}&count={maxResults}&view=STANDARD", "búsqueda por autor");
                yield break;
            }

            if (mode == "author-coauthor")
            {
                var authorQuery = BuildScopusAuthorQuery(request.PrimaryAuthor);
                var coAuthorQuery = BuildScopusAuthorQuery(request.CoAuthor);
                if (string.IsNullOrWhiteSpace(authorQuery) || string.IsNullOrWhiteSpace(coAuthorQuery))
                {
                    throw new InvalidOperationException("Debes informar autor y coautor para este modo de búsqueda.");
                }

                var combinedQuery = $"{authorQuery} AND {coAuthorQuery}";
                yield return new ScopusAttempt($"https://api.elsevier.com/content/search/scopus?query={Uri.EscapeDataString(combinedQuery)}&count={maxResults}&view=STANDARD", "búsqueda por autor y coautor");
                yield break;
            }

            yield return new ScopusAttempt($"https://api.elsevier.com/content/search/scopus?query={encodedQuery}&count={maxResults}&view=STANDARD", "búsqueda STANDARD");
            yield return new ScopusAttempt($"https://api.elsevier.com/content/search/scopus?query={encodedQuery}&count={maxResults}&view=COMPLETE", "búsqueda COMPLETE");
        }

        private void ConfigureHeaders(HttpClient client, string providerKey)
        {
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            client.DefaultRequestHeaders.Remove("X-ELS-APIKey");
            client.DefaultRequestHeaders.Remove("X-ELS-Insttoken");

            if (!string.Equals(providerKey, "scopus", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            if (!string.IsNullOrWhiteSpace(_options.Scopus.ApiKey))
            {
                client.DefaultRequestHeaders.Add("X-ELS-APIKey", _options.Scopus.ApiKey);
            }

            if (!string.IsNullOrWhiteSpace(_options.Scopus.InstToken))
            {
                client.DefaultRequestHeaders.Add("X-ELS-Insttoken", _options.Scopus.InstToken);
            }
        }

        private static string BuildResponseMessage(string providerName, bool isSuccess, System.Net.HttpStatusCode statusCode)
        {
            if (isSuccess)
            {
                return $"Consulta completada contra {providerName}.";
            }

            return statusCode switch
            {
                System.Net.HttpStatusCode.Unauthorized => $"{providerName} rechazó la consulta. Revisa la llave API configurada.",
                System.Net.HttpStatusCode.Forbidden => $"{providerName} respondió sin permisos suficientes. Puede requerir entitlements o token institucional.",
                System.Net.HttpStatusCode.NotFound => $"{providerName} no encontró un recurso para la consulta enviada.",
                _ => $"La API respondió con {(int)statusCode} {statusCode}."
            };
        }

        private static string GetProviderName(string providerKey) => providerKey switch
        {
            "scopus" => "Scopus",
            "crossref" => "Crossref",
            "openalex" => "OpenAlex",
            "semantic-scholar" => "Semantic Scholar",
            _ => providerKey
        };

        private static string BuildProviderUrl(string providerKey, ExternalApiQueryRequest request)
        {
            var query = request.QueryText.Trim();
            var encodedQuery = Uri.EscapeDataString(query);
            var maxResults = Math.Clamp(request.MaxResults, 1, 25);
            var mode = (request.QueryMode ?? "general").Trim().ToLowerInvariant();

            return providerKey switch
            {
                "scopus" when mode == "doi" => $"https://api.elsevier.com/content/abstract/doi/{encodedQuery}?view=META",
                "scopus" => $"https://api.elsevier.com/content/search/scopus?query={encodedQuery}&count={maxResults}&view=STANDARD",
                "crossref" when mode == "doi" => $"https://api.crossref.org/works/{encodedQuery}",
                "crossref" => $"https://api.crossref.org/works?query={encodedQuery}&rows={maxResults}",
                "openalex" when mode == "doi" => $"https://api.openalex.org/works/https://doi.org/{encodedQuery}",
                "openalex" => $"https://api.openalex.org/works?search={encodedQuery}&per-page={maxResults}",
                "semantic-scholar" when mode == "doi" => $"https://api.semanticscholar.org/graph/v1/paper/DOI:{encodedQuery}?fields=title,abstract,year,venue,externalIds,url,authors",
                "semantic-scholar" => $"https://api.semanticscholar.org/graph/v1/paper/search?query={encodedQuery}&limit={maxResults}&fields=title,abstract,year,venue,externalIds,url,authors",
                _ => throw new InvalidOperationException("Proveedor externo no soportado.")
            };
        }

        private static List<ExternalArticlePreviewDto> ParseArticles(string providerKey, string rawJson)
        {
            using var document = JsonDocument.Parse(rawJson);
            return providerKey switch
            {
                "scopus" => ParseScopus(document.RootElement),
                "crossref" => ParseCrossref(document.RootElement),
                "openalex" => ParseOpenAlex(document.RootElement),
                "semantic-scholar" => ParseSemanticScholar(document.RootElement),
                _ => new List<ExternalArticlePreviewDto>()
            };
        }

        private static List<ExternalArticlePreviewDto> ParseScopus(JsonElement root)
        {
            var results = new List<ExternalArticlePreviewDto>();

            if (root.TryGetProperty("search-results", out var searchResults)
                && searchResults.TryGetProperty("entry", out var entries)
                && entries.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in entries.EnumerateArray())
                {
                    results.Add(MapScopusSearchItem(item));
                }

                return results;
            }

            if (root.TryGetProperty("abstracts-retrieval-response", out var abstractResponse))
            {
                results.Add(MapScopusAbstractItem(abstractResponse));
            }

            return results;
        }

        private static ExternalArticlePreviewDto MapScopusSearchItem(JsonElement item)
        {
            var authors = item.TryGetProperty("dc:creator", out var creator)
                ? creator.GetString()
                : null;

            var year = TryGetYear(
                item.TryGetProperty("prism:coverDate", out var coverDate) ? coverDate.GetString() : null,
                item.TryGetProperty("prism:coverDisplayDate", out var displayDate) ? displayDate.GetString() : null);

            return new ExternalArticlePreviewDto
            {
                Title = item.TryGetProperty("dc:title", out var title) ? title.GetString() ?? "(sin título)" : "(sin título)",
                Doi = item.TryGetProperty("prism:doi", out var doi) ? doi.GetString() : null,
                JournalName = item.TryGetProperty("prism:publicationName", out var journal) ? journal.GetString() : null,
                IssnCode = item.TryGetProperty("prism:issn", out var issn) ? issn.GetString() : null,
                EIssnCode = item.TryGetProperty("prism:eIssn", out var eIssn) ? eIssn.GetString() : null,
                Volume = item.TryGetProperty("prism:volume", out var volume) ? volume.GetString() : null,
                Issue = item.TryGetProperty("prism:issueIdentifier", out var issue) ? issue.GetString() : null,
                PageRange = BuildPageRange(
                    item.TryGetProperty("prism:startingPage", out var startPage) ? startPage.GetString() : null,
                    item.TryGetProperty("prism:endingPage", out var endPage) ? endPage.GetString() : null),
                DocumentType = item.TryGetProperty("subtypeDescription", out var subtype) ? subtype.GetString() : null,
                PublicationYear = year,
                Authors = authors,
                AuthorNames = SplitAuthors(authors),
                SourceUrl = item.TryGetProperty("prism:url", out var url) ? url.GetString() : null,
                ExternalId = item.TryGetProperty("dc:identifier", out var identifier) ? identifier.GetString() : null,
                ExternalSource = "Scopus",
                ScopusId = ExtractScopusIdFromIdentifier(item.TryGetProperty("dc:identifier", out var idProp) ? idProp.GetString() : null)
            };
        }

        private static ExternalArticlePreviewDto MapScopusAbstractItem(JsonElement item)
        {
            var coredata = item.TryGetProperty("coredata", out var coreDataProp)
                ? coreDataProp
                : default;

            var authors = ExtractScopusAbstractAuthors(item);
            return new ExternalArticlePreviewDto
            {
                Title = coredata.ValueKind == JsonValueKind.Object && coredata.TryGetProperty("dc:title", out var title)
                    ? title.GetString() ?? "(sin título)"
                    : "(sin título)",
                Doi = coredata.ValueKind == JsonValueKind.Object && coredata.TryGetProperty("prism:doi", out var doi) ? doi.GetString() : null,
                JournalName = coredata.ValueKind == JsonValueKind.Object && coredata.TryGetProperty("prism:publicationName", out var journal) ? journal.GetString() : null,
                IssnCode = coredata.ValueKind == JsonValueKind.Object && coredata.TryGetProperty("prism:issn", out var issn) ? issn.GetString() : null,
                EIssnCode = coredata.ValueKind == JsonValueKind.Object && coredata.TryGetProperty("prism:eIssn", out var eIssn) ? eIssn.GetString() : null,
                Publisher = coredata.ValueKind == JsonValueKind.Object && coredata.TryGetProperty("dc:publisher", out var publisher) ? publisher.GetString() : null,
                Volume = coredata.ValueKind == JsonValueKind.Object && coredata.TryGetProperty("prism:volume", out var volume) ? volume.GetString() : null,
                Issue = coredata.ValueKind == JsonValueKind.Object && coredata.TryGetProperty("prism:issueIdentifier", out var issue) ? issue.GetString() : null,
                PageRange = BuildPageRange(
                    coredata.ValueKind == JsonValueKind.Object && coredata.TryGetProperty("prism:startingPage", out var startPage) ? startPage.GetString() : null,
                    coredata.ValueKind == JsonValueKind.Object && coredata.TryGetProperty("prism:endingPage", out var endPage) ? endPage.GetString() : null),
                DocumentType = coredata.ValueKind == JsonValueKind.Object && coredata.TryGetProperty("subtypeDescription", out var subtype) ? subtype.GetString() : null,
                PublicationYear = TryGetYear(coredata.ValueKind == JsonValueKind.Object && coredata.TryGetProperty("prism:coverDate", out var coverDate) ? coverDate.GetString() : null),
                Authors = authors.Any() ? string.Join(", ", authors.Take(5)) : null,
                AuthorNames = authors,
                SourceUrl = coredata.ValueKind == JsonValueKind.Object && coredata.TryGetProperty("prism:url", out var url) ? url.GetString() : null,
                ExternalId = coredata.ValueKind == JsonValueKind.Object && coredata.TryGetProperty("dc:identifier", out var identifier) ? identifier.GetString() : null,
                ArticleAbstract = TryGetNestedString(item, "abstract", "ce:para"),
                Keywords = ExtractScopusKeywords(item),
                ExternalSource = "Scopus",
                ScopusId = ExtractScopusIdFromIdentifier(coredata.ValueKind == JsonValueKind.Object && coredata.TryGetProperty("dc:identifier", out var idProp) ? idProp.GetString() : null)
            };
        }

        private static List<string> ExtractScopusAbstractAuthors(JsonElement item)
        {
            var names = new List<string>();

            if (item.TryGetProperty("authors", out var authorsRoot)
                && authorsRoot.TryGetProperty("author", out var authors)
                && authors.ValueKind == JsonValueKind.Array)
            {
                foreach (var author in authors.EnumerateArray())
                {
                    var given = author.TryGetProperty("ce:given-name", out var givenName) ? givenName.GetString() : null;
                    var surname = author.TryGetProperty("ce:surname", out var surName) ? surName.GetString() : null;
                    var fullName = string.Join(" ", new[] { given, surname }.Where(x => !string.IsNullOrWhiteSpace(x)));
                    if (!string.IsNullOrWhiteSpace(fullName))
                    {
                        names.Add(fullName);
                    }
                }
            }

            return names;
        }

        private static List<string> ExtractScopusKeywords(JsonElement item)
        {
            var keywords = new List<string>();
            if (item.TryGetProperty("authkeywords", out var keywordsRoot)
                && keywordsRoot.TryGetProperty("author-keyword", out var keywordArray))
            {
                if (keywordArray.ValueKind == JsonValueKind.Array)
                {
                    keywords.AddRange(keywordArray.EnumerateArray()
                        .Select(x => x.ValueKind == JsonValueKind.String ? x.GetString() : x.ToString())
                        .Where(x => !string.IsNullOrWhiteSpace(x))!
                        .Cast<string>());
                }
                else if (keywordArray.ValueKind == JsonValueKind.String)
                {
                    keywords.Add(keywordArray.GetString()!);
                }
            }

            return keywords.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        }

        private static List<ExternalArticlePreviewDto> ParseCrossref(JsonElement root)
        {
            var results = new List<ExternalArticlePreviewDto>();
            if (!root.TryGetProperty("message", out var message))
            {
                return results;
            }

            if (message.TryGetProperty("items", out var items) && items.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in items.EnumerateArray())
                {
                    results.Add(MapCrossrefItem(item));
                }
            }
            else
            {
                results.Add(MapCrossrefItem(message));
            }

            return results;
        }

        private static ExternalArticlePreviewDto MapCrossrefItem(JsonElement item)
        {
            var authorNames = item.TryGetProperty("author", out var authors) && authors.ValueKind == JsonValueKind.Array
                ? authors.EnumerateArray().Take(10).Select(GetCrossrefAuthorName).Where(x => !string.IsNullOrWhiteSpace(x)).Cast<string>().ToList()
                : new List<string>();

            return new ExternalArticlePreviewDto
            {
                Title = item.TryGetProperty("title", out var titleArray) && titleArray.ValueKind == JsonValueKind.Array
                    ? titleArray.EnumerateArray().Select(x => x.GetString()).FirstOrDefault() ?? "(sin título)"
                    : "(sin título)",
                Doi = item.TryGetProperty("DOI", out var doi) ? doi.GetString() : null,
                JournalName = item.TryGetProperty("container-title", out var journalArray) && journalArray.ValueKind == JsonValueKind.Array
                    ? journalArray.EnumerateArray().Select(x => x.GetString()).FirstOrDefault()
                    : null,
                IssnCode = item.TryGetProperty("ISSN", out var issnArray) && issnArray.ValueKind == JsonValueKind.Array
                    ? issnArray.EnumerateArray().Select(x => x.GetString()).FirstOrDefault()
                    : null,
                Publisher = item.TryGetProperty("publisher", out var publisher) ? publisher.GetString() : null,
                Volume = item.TryGetProperty("volume", out var volume) ? volume.GetString() : null,
                Issue = item.TryGetProperty("issue", out var issue) ? issue.GetString() : null,
                PageRange = BuildPageRange(
                    item.TryGetProperty("page", out var page) ? page.GetString() : null,
                    null),
                DocumentType = item.TryGetProperty("type", out var type) ? type.GetString() : null,
                PublicationYear = TryGetYearFromCrossref(item),
                Authors = authorNames.Any() ? string.Join(", ", authorNames.Take(5)) : null,
                AuthorNames = authorNames,
                SourceUrl = item.TryGetProperty("URL", out var url) ? url.GetString() : null,
                ExternalId = item.TryGetProperty("DOI", out var extDoi) ? extDoi.GetString() : null,
                ArticleAbstract = item.TryGetProperty("abstract", out var summary) ? summary.GetString() : null,
                Keywords = item.TryGetProperty("subject", out var subjectArray) && subjectArray.ValueKind == JsonValueKind.Array
                    ? subjectArray.EnumerateArray().Select(x => x.GetString()).Where(x => !string.IsNullOrWhiteSpace(x)).Cast<string>().ToList()
                    : new List<string>(),
                ExternalSource = "Crossref"
            };
        }

        private static int? TryGetYearFromCrossref(JsonElement item)
        {
            if (item.TryGetProperty("published-print", out var print)
                && print.TryGetProperty("date-parts", out var printDateParts)
                && printDateParts.ValueKind == JsonValueKind.Array)
            {
                var first = printDateParts.EnumerateArray().FirstOrDefault();
                if (first.ValueKind == JsonValueKind.Array)
                {
                    var year = first.EnumerateArray().FirstOrDefault();
                    if (year.ValueKind == JsonValueKind.Number && year.TryGetInt32(out var parsed))
                    {
                        return parsed;
                    }
                }
            }

            if (item.TryGetProperty("published-online", out var online)
                && online.TryGetProperty("date-parts", out var onlineDateParts)
                && onlineDateParts.ValueKind == JsonValueKind.Array)
            {
                var first = onlineDateParts.EnumerateArray().FirstOrDefault();
                if (first.ValueKind == JsonValueKind.Array)
                {
                    var year = first.EnumerateArray().FirstOrDefault();
                    if (year.ValueKind == JsonValueKind.Number && year.TryGetInt32(out var parsed))
                    {
                        return parsed;
                    }
                }
            }

            return null;
        }

        private static string? GetCrossrefAuthorName(JsonElement author)
        {
            var given = author.TryGetProperty("given", out var givenProp) ? givenProp.GetString() : null;
            var family = author.TryGetProperty("family", out var familyProp) ? familyProp.GetString() : null;
            return string.Join(" ", new[] { given, family }.Where(x => !string.IsNullOrWhiteSpace(x)));
        }

        private static List<ExternalArticlePreviewDto> ParseOpenAlex(JsonElement root)
        {
            var results = new List<ExternalArticlePreviewDto>();

            if (root.TryGetProperty("results", out var array) && array.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in array.EnumerateArray())
                {
                    results.Add(MapOpenAlexItem(item));
                }
            }
            else
            {
                results.Add(MapOpenAlexItem(root));
            }

            return results;
        }

        private static ExternalArticlePreviewDto MapOpenAlexItem(JsonElement item)
        {
            var authors = item.TryGetProperty("authorships", out var authorships) && authorships.ValueKind == JsonValueKind.Array
                ? authorships.EnumerateArray().Take(10)
                    .Select(x => x.TryGetProperty("author", out var author) && author.TryGetProperty("display_name", out var displayName)
                        ? displayName.GetString()
                        : null)
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Cast<string>()
                    .ToList()
                : new List<string>();

            return new ExternalArticlePreviewDto
            {
                Title = item.TryGetProperty("display_name", out var title) ? title.GetString() ?? "(sin título)" : "(sin título)",
                Doi = item.TryGetProperty("doi", out var doi) ? doi.GetString() : null,
                JournalName = item.TryGetProperty("primary_location", out var location)
                    && location.TryGetProperty("source", out var source)
                    && source.TryGetProperty("display_name", out var journal)
                    ? journal.GetString()
                    : null,
                IssnCode = item.TryGetProperty("primary_location", out var locationIssn)
                    && locationIssn.TryGetProperty("source", out var sourceIssn)
                    && sourceIssn.TryGetProperty("issn_l", out var issn)
                    ? issn.GetString()
                    : null,
                Publisher = item.TryGetProperty("primary_location", out var locationPublisher)
                    && locationPublisher.TryGetProperty("source", out var sourcePublisher)
                    && sourcePublisher.TryGetProperty("host_organization_name", out var hostOrg)
                    ? hostOrg.GetString()
                    : null,
                JournalUrl = item.TryGetProperty("primary_location", out var locationUrl)
                    && locationUrl.TryGetProperty("landing_page_url", out var landingUrl)
                    ? landingUrl.GetString()
                    : null,
                Volume = item.TryGetProperty("biblio", out var biblio) && biblio.TryGetProperty("volume", out var volume) ? volume.GetString() : null,
                Issue = item.TryGetProperty("biblio", out var biblioIssue) && biblioIssue.TryGetProperty("issue", out var issue) ? issue.GetString() : null,
                PageRange = item.TryGetProperty("biblio", out var biblioPages)
                    ? BuildPageRange(
                        biblioPages.TryGetProperty("first_page", out var firstPage) ? firstPage.GetString() : null,
                        biblioPages.TryGetProperty("last_page", out var lastPage) ? lastPage.GetString() : null)
                    : null,
                DocumentType = item.TryGetProperty("type", out var type) ? type.GetString() : null,
                PublicationYear = item.TryGetProperty("publication_year", out var year) && year.TryGetInt32(out var parsedYear) ? parsedYear : null,
                Authors = authors.Any() ? string.Join(", ", authors.Take(5)) : null,
                AuthorNames = authors,
                SourceUrl = item.TryGetProperty("id", out var id) ? id.GetString() : null,
                ExternalId = item.TryGetProperty("id", out var extId) ? extId.GetString() : null,
                Keywords = item.TryGetProperty("concepts", out var concepts) && concepts.ValueKind == JsonValueKind.Array
                    ? concepts.EnumerateArray()
                        .Take(10)
                        .Select(x => x.TryGetProperty("display_name", out var conceptName) ? conceptName.GetString() : null)
                        .Where(x => !string.IsNullOrWhiteSpace(x))
                        .Cast<string>()
                        .ToList()
                    : new List<string>(),
                ExternalSource = "OpenAlex"
            };
        }

        private static List<ExternalArticlePreviewDto> ParseSemanticScholar(JsonElement root)
        {
            var results = new List<ExternalArticlePreviewDto>();

            if (root.TryGetProperty("data", out var array) && array.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in array.EnumerateArray())
                {
                    results.Add(MapSemanticScholarItem(item));
                }
            }
            else
            {
                results.Add(MapSemanticScholarItem(root));
            }

            return results;
        }

        private static ExternalArticlePreviewDto MapSemanticScholarItem(JsonElement item)
        {
            string? doi = null;
            if (item.TryGetProperty("externalIds", out var externalIds)
                && externalIds.ValueKind == JsonValueKind.Object
                && externalIds.TryGetProperty("DOI", out var doiValue))
            {
                doi = doiValue.GetString();
            }

            var authors = item.TryGetProperty("authors", out var authorsArray) && authorsArray.ValueKind == JsonValueKind.Array
                ? authorsArray.EnumerateArray().Take(10)
                    .Select(x => x.TryGetProperty("name", out var name) ? name.GetString() : null)
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Cast<string>()
                    .ToList()
                : new List<string>();

            return new ExternalArticlePreviewDto
            {
                Title = item.TryGetProperty("title", out var title) ? title.GetString() ?? "(sin título)" : "(sin título)",
                Doi = doi,
                JournalName = item.TryGetProperty("venue", out var venue) ? venue.GetString() : null,
                DocumentType = item.TryGetProperty("publicationTypes", out var publicationTypes) && publicationTypes.ValueKind == JsonValueKind.Array
                    ? publicationTypes.EnumerateArray().Select(x => x.GetString()).FirstOrDefault()
                    : null,
                PublicationYear = item.TryGetProperty("year", out var year) && year.TryGetInt32(out var parsedYear) ? parsedYear : null,
                Authors = authors.Any() ? string.Join(", ", authors.Take(5)) : null,
                AuthorNames = authors,
                SourceUrl = item.TryGetProperty("url", out var url) ? url.GetString() : null,
                ExternalId = item.TryGetProperty("paperId", out var paperId) ? paperId.GetString() : null,
                ArticleAbstract = item.TryGetProperty("abstract", out var summary) ? summary.GetString() : null,
                Keywords = item.TryGetProperty("fieldsOfStudy", out var fieldsOfStudy) && fieldsOfStudy.ValueKind == JsonValueKind.Array
                    ? fieldsOfStudy.EnumerateArray().Select(x => x.GetString()).Where(x => !string.IsNullOrWhiteSpace(x)).Cast<string>().ToList()
                    : new List<string>(),
                ExternalSource = "Semantic Scholar"
            };
        }

        private static string? TryGetNestedString(JsonElement root, params string[] path)
        {
            var current = root;
            foreach (var segment in path)
            {
                if (current.ValueKind != JsonValueKind.Object || !current.TryGetProperty(segment, out current))
                {
                    return null;
                }
            }

            return current.ValueKind == JsonValueKind.String ? current.GetString() : current.ToString();
        }

        private static int? TryGetYear(params string?[] candidates)
        {
            foreach (var candidate in candidates.Where(x => !string.IsNullOrWhiteSpace(x)))
            {
                if (DateTime.TryParse(candidate, out var date))
                {
                    return date.Year;
                }

                if (candidate!.Length >= 4 && int.TryParse(candidate[..4], out var year))
                {
                    return year;
                }
            }

            return null;
        }

        private static List<string> SplitAuthors(string? authors)
        {
            if (string.IsNullOrWhiteSpace(authors))
            {
                return new List<string>();
            }

            return authors
                .Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static string? BuildPageRange(string? start, string? end)
        {
            if (!string.IsNullOrWhiteSpace(start) && !string.IsNullOrWhiteSpace(end))
            {
                return $"{start}-{end}";
            }

            return !string.IsNullOrWhiteSpace(start) ? start : end;
        }

        private static IEnumerable<string> BuildScopusEnrichmentUrls(string scopusId)
        {
            yield return $"https://api.elsevier.com/content/abstract/scopus_id/{Uri.EscapeDataString(scopusId)}?view=META";
            yield return $"https://api.elsevier.com/content/abstract/scopus_id/{Uri.EscapeDataString(scopusId)}";
        }

        private static string? ExtractScopusId(ExternalArticlePreviewDto article)
        {
            return !string.IsNullOrWhiteSpace(article.ScopusId)
                ? article.ScopusId
                : ExtractScopusIdFromIdentifier(article.ExternalId);
        }

        private static string? ExtractScopusIdFromIdentifier(string? identifier)
        {
            if (string.IsNullOrWhiteSpace(identifier))
            {
                return null;
            }

            const string prefix = "SCOPUS_ID:";
            return identifier.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
                ? identifier[prefix.Length..]
                : identifier;
        }

        private static string? BuildScopusAuthorQuery(string? authorInput)
        {
            if (string.IsNullOrWhiteSpace(authorInput))
            {
                return null;
            }

            var tokens = authorInput
                .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .ToList();

            if (tokens.Count == 0)
            {
                return null;
            }

            var lastName = tokens.Last();
            var firstNames = tokens.Take(tokens.Count - 1).ToList();

            if (firstNames.Count == 0)
            {
                return $"AUTHLASTNAME({lastName})";
            }

            var firstNameExpression = string.Join(" AND ", firstNames.Select(name => $"AUTHFIRST({name})"));
            return $"AUTHLASTNAME({lastName}) AND {firstNameExpression}";
        }

        private static ExternalArticlePreviewDto MergeArticle(ExternalArticlePreviewDto original, ExternalArticlePreviewDto enriched)
        {
            return new ExternalArticlePreviewDto
            {
                Title = string.IsNullOrWhiteSpace(enriched.Title) ? original.Title : enriched.Title,
                Doi = FirstNonEmpty(enriched.Doi, original.Doi),
                JournalName = FirstNonEmpty(enriched.JournalName, original.JournalName),
                IssnCode = FirstNonEmpty(enriched.IssnCode, original.IssnCode),
                EIssnCode = FirstNonEmpty(enriched.EIssnCode, original.EIssnCode),
                Publisher = FirstNonEmpty(enriched.Publisher, original.Publisher),
                JournalUrl = FirstNonEmpty(enriched.JournalUrl, original.JournalUrl),
                Volume = FirstNonEmpty(enriched.Volume, original.Volume),
                Issue = FirstNonEmpty(enriched.Issue, original.Issue),
                PageRange = FirstNonEmpty(enriched.PageRange, original.PageRange),
                DocumentType = FirstNonEmpty(enriched.DocumentType, original.DocumentType),
                ArticleAbstract = FirstNonEmpty(enriched.ArticleAbstract, original.ArticleAbstract),
                PublicationYear = enriched.PublicationYear ?? original.PublicationYear,
                Authors = FirstNonEmpty(enriched.Authors, original.Authors),
                AuthorNames = enriched.AuthorNames.Any() ? enriched.AuthorNames : original.AuthorNames,
                Keywords = enriched.Keywords.Any() ? enriched.Keywords : original.Keywords,
                SourceUrl = FirstNonEmpty(enriched.SourceUrl, original.SourceUrl),
                ExternalId = FirstNonEmpty(enriched.ExternalId, original.ExternalId),
                ExternalSource = FirstNonEmpty(enriched.ExternalSource, original.ExternalSource),
                ScopusId = FirstNonEmpty(enriched.ScopusId, original.ScopusId)
            };
        }

        private static string? FirstNonEmpty(string? primary, string? fallback)
            => !string.IsNullOrWhiteSpace(primary) ? primary : fallback;

        private sealed class ProviderExecutionResult
        {
            public string FinalRequestUrl { get; set; } = string.Empty;
            public bool Success { get; set; }
            public int StatusCode { get; set; }
            public string Message { get; set; } = string.Empty;
            public string RawResponse { get; set; } = string.Empty;
        }

        private sealed record ScopusAttempt(string Url, string? Note);
    }
}
