using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using tesisproject.frontend.Services.Interfaces;
using tesisproject.shared.DTOs;
using tesisproject.shared.DTOs.Articles;

namespace tesisproject.frontend.Services.Implementations
{
    public class ArticlesClient : IArticlesClient
    {
        private readonly IApiClient _api;
        public ArticlesClient(IApiClient api) => _api = api;

        public async Task<int> CreateAsync(CreateArticleRequest request, CancellationToken ct = default)
        {
            var id = await _api.PostAsync<CreateArticleRequest, int>("/api/Articles", request, ct);
            return id;
        }

        public Task UpdateAsync(int id, UpdateArticleRequest request, CancellationToken ct = default)
            => _api.PutAsync<UpdateArticleRequest, object>($"/api/Articles/{id}", request, ct);

        public Task DeleteAsync(int id, CancellationToken ct = default)
            => _api.DeleteAsync($"/api/Articles/{id}", ct);

        public Task<ArticleDetailDto?> GetByIdAsync(int id, CancellationToken ct = default)
            => _api.GetAsync<ArticleDetailDto>($"/api/Articles/{id}", ct);

        public async Task<PagedResult<ArticleListItemDto>> GetListAsync(
            ArticleListQuery query,
            CancellationToken ct = default)
        {
            // Construir query string para GET
            var queryString = $"?page={query.Page}&pageSize={query.PageSize}";

            if (!string.IsNullOrEmpty(query.SearchTerm))
                queryString += $"&search={Uri.EscapeDataString(query.SearchTerm)}";

            if (query.ProjectId.HasValue)
                queryString += $"&projectId={query.ProjectId.Value}";

            // Usar GET en lugar de POST
            return await _api.GetAsync<PagedResult<ArticleListItemDto>>($"/api/Articles{queryString}", ct)
                   ?? new PagedResult<ArticleListItemDto>();
        }
        public async Task<List<ArticleListItemDto>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            var query = new ArticleListQuery
            {
                Page = 1,
                PageSize = 1000,   // ajusta según el máximo razonable para tu escenario
                SearchTerm = null,
                ProjectId = null
            };

            var result = await GetListAsync(query, cancellationToken);

            return result.Items?.ToList() ?? new List<ArticleListItemDto>();
        }
    }
}
