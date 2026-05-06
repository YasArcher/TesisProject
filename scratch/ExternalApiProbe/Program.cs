using Microsoft.Extensions.Options;
using tesisproject.backend.Options;
using tesisproject.backend.Services.Implementations;
using tesisproject.shared.DTOs.ExternalApis;

var httpClient = new HttpClient();
var httpClientFactory = new SimpleHttpClientFactory(httpClient);
var options = Options.Create(new ExternalApiExplorerOptions
{
    Scopus = new ExternalApiProviderAuthOptions
    {
        ApiKey = "7558370cb7acf175cc66d2a98534a595",
        InstToken = ""
    }
});

var service = new ExternalApiExplorerService(httpClientFactory, options);
var result = await service.QueryAsync(new ExternalApiQueryRequest
{
    ProviderKey = "scopus",
    QueryMode = "author",
    PrimaryAuthor = "Ruben Nogales",
    InstitutionName = "Universidad Técnica de Ambato",
    MaxResults = 10
});

Console.WriteLine($"Success: {result.Success}");
Console.WriteLine($"Status: {result.StatusCode}");
Console.WriteLine($"URL: {result.FinalRequestUrl}");
Console.WriteLine($"Message: {result.Message}");
Console.WriteLine($"Articles: {result.Articles.Count}");
foreach (var article in result.Articles.Take(5))
{
    Console.WriteLine($"- {article.Title} || {article.Authors}");
}

file sealed class SimpleHttpClientFactory(HttpClient client) : IHttpClientFactory
{
    public HttpClient CreateClient(string name) => client;
}
