using System.Net;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using tesisproject.backend.Services.External.Implementations;
using tesisproject.shared.DTOs.ExternalApis;

// Modulo articulos: contrato de consulta externa sin acceso a base de datos.
internal static class ArticleExternalSearchTests
{
    public static async Task RunAsync(Action<bool, string> check)
    {
        var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("""
                {"message":{"items":[{"DOI":"10.1000/example","title":["Scientific article"],
                "container-title":["Journal"],"author":[{"given":"Ana","family":"Perez"}],
                "published":{"date-parts":[[2021,4,1]]},"is-referenced-by-count":7}]}}
                """)
        });
        var service = Create(handler);
        var result = await service.SearchAsync(new ExternalApiQueryRequest
        {
            ProviderKey = "crossref", QueryMode = "general", QueryText = "science", MaxResults = 5
        }, default);
        check(result.Success && result.Data?.Articles.Count == 1 &&
            result.Data.Articles[0].PublicationYear == 2021 &&
            result.Data.Articles[0].Doi == "10.1000/example" &&
            result.Data.Articles[0].Authors == "Ana Perez",
            "Crossref metadata must map to an article preview without persistence.");
        check(handler.LastUri?.AbsolutePath == "/works" &&
            handler.LastUri.Query.Contains("rows=5") && handler.LastUri.Query.Contains("science"),
            "Search must use a fixed Crossref endpoint and bounded rows.");
        check(!(await service.SearchAsync(new ExternalApiQueryRequest
        {
            ProviderKey = "scopus", QueryText = "science", MaxResults = 5
        }, default)).Success && handler.Calls == 1,
            "Scopus without a backend key must fail before a network request.");
        check(!(await service.SearchAsync(new ExternalApiQueryRequest
        {
            ProviderKey = "crossref", QueryText = "science", MaxResults = 101
        }, default)).Success && handler.Calls == 1,
            "Results limit must be enforced by the backend.");

        var notFound = await Create(new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.NotFound)))
            .SearchAsync(new ExternalApiQueryRequest
            { ProviderKey = "crossref", QueryMode = "doi", QueryText = "10.1000/missing", MaxResults = 1 }, default);
        check(notFound.Success && notFound.Data?.Articles.Count == 0,
            "Unknown DOI must return an empty successful search.");

        var malformed = await Create(new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"unexpected\":true}")
        })).SearchAsync(new ExternalApiQueryRequest
        { ProviderKey = "crossref", QueryMode = "general", QueryText = "science", MaxResults = 1 }, default);
        check(!malformed.Success && malformed.ErrorCode == "EXTERNAL_PROVIDER_INVALID_RESPONSE",
            "Malformed provider data must return a stable, nontechnical error.");

        var scopusHandler = new StubHandler(request =>
        {
            check(request.Headers.TryGetValues("X-ELS-APIKey", out var keys) && keys.Single() == "test-key"
                && request.RequestUri?.Host == "api.elsevier.com"
                && request.RequestUri.AbsolutePath == "/content/search/scopus"
                && request.RequestUri.Query.Contains("count=5")
                && !request.RequestUri.Query.Contains("test-key"),
                "Scopus must use its fixed endpoint and send the key only as a header.");
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("""
                {"search-results":{"entry":[{"dc:title":"Scopus article","prism:doi":"10.1000/scopus",
                "prism:publicationName":"Journal","prism:coverDate":"2022-03-15","dc:creator":"Ana Perez",
                "prism:issn":"12345678","citedby-count":"11","eid":"2-s2.0-12345"}]}}
                """) };
        });
        var scopus = await Create(scopusHandler, "test-key").SearchAsync(new ExternalApiQueryRequest
        { ProviderKey = "scopus", QueryMode = "general", QueryText = "science", MaxResults = 5 }, default);
        check(scopus.Success && scopus.Data?.Articles.SingleOrDefault()?.ScopusId == "12345"
            && scopus.Data.Articles[0].PublicationYear == 2022 && scopus.Data.Articles[0].CitationCount == 11,
            "Scopus results must map into a read-only preview.");

        var throttled = await Create(new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.TooManyRequests)), "test-key")
            .SearchAsync(new ExternalApiQueryRequest
            { ProviderKey = "scopus", QueryMode = "general", QueryText = "science", MaxResults = 5 }, default);
        check(!throttled.Success && throttled.ErrorCode == "EXTERNAL_PROVIDER_RATE_LIMITED",
            "Provider rate limits must have a stable user-facing outcome.");

        var empty = await Create(new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        { Content = new StringContent("{\"search-results\":{\"opensearch:totalResults\":\"0\"}}") }), "test-key")
            .SearchAsync(new ExternalApiQueryRequest
            { ProviderKey = "scopus", QueryMode = "general", QueryText = "science", MaxResults = 5 }, default);
        check(empty.Success && empty.Data?.Articles.Count == 0,
            "Scopus zero-result responses must not appear as provider failures.");
    }

    private static ArticleExternalSearchService Create(HttpMessageHandler handler, string? scopusKey = null) => new(
        new HttpClient(handler) { BaseAddress = new Uri("https://api.crossref.org/") },
        new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        { ["ExternalApis:Scopus:ApiKey"] = scopusKey }).Build(),
        NullLogger<ArticleExternalSearchService>.Instance);

    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> response) : HttpMessageHandler
    {
        public int Calls { get; private set; }
        public Uri? LastUri { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            Calls++;
            LastUri = request.RequestUri;
            return Task.FromResult(response(request));
        }
    }
}
