using System.Net.Http.Json;

using tesisproject.frontend.Services.Interfaces;
using tesisproject.shared.DTOs.Articles;
using tesisproject.shared.Abstractions;

namespace tesisproject.frontend.Services.Implementations
{
    public class ArticlesClient : IArticlesClient
    {
        private readonly HttpClient _http;
        public ArticlesClient(HttpClient http) => _http = http;

        public async Task<Result<int>> CreateAsync(CreateArticleRequest req)
        {
            var resp = await _http.PostAsJsonAsync("/api/articles", req);
            return await resp.Content.ReadFromJsonAsync<Result<int>>()
                   ?? Result<int>.Fail("Sin respuesta");
        }

        public Task<Result<IReadOnlyList<ArticleDto>>?> GetAllAsync()
            => _http.GetFromJsonAsync<Result<IReadOnlyList<ArticleDto>>>("/api/articles");
    }
}
