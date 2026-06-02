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

        // =========================================================
        // CREATE
        // =========================================================
        public async Task<int> CreateAsync(CreateArticleRequest request, CancellationToken ct = default)
        {
            if (request is null) throw new ArgumentNullException(nameof(request));

            // POST api/Articles  => devuelve int (Id nuevo)
            var id = await _api.PostAsync<CreateArticleRequest, int>(
                "api/Articles",
                request,
                ct
            );

            return id;
        }

        // =========================================================
        // UPDATE
        // =========================================================
        public async Task UpdateAsync(int id, UpdateArticleRequest request, CancellationToken ct = default)
        {
            if (request is null) throw new ArgumentNullException(nameof(request));

            // Alineamos el Id del body con el de la ruta (aunque el backend usa el de la ruta)
            request.Id = id;

            // PUT api/Articles/{id}  => 204 NoContent
            await _api.PutAsync<UpdateArticleRequest, object>(
                $"api/Articles/{id}",
                request,
                ct
            );
        }

        // =========================================================
        // DELETE
        // =========================================================
        public Task DeleteAsync(int id, CancellationToken ct = default)
            => _api.DeleteAsync($"api/Articles/{id}", ct);

        // =========================================================
        // GET BY ID (DETALLE)
        // =========================================================
        public Task<ArticleDetailDto?> GetByIdAsync(int id, CancellationToken ct = default)
            => _api.GetAsync<ArticleDetailDto>($"api/Articles/{id}", ct);

        // =========================================================
        // LISTADO PAGINADO
        // =========================================================
        public async Task<PagedResult<ArticleListItemDto>> GetListAsync(
            ArticleListQuery query,
            CancellationToken ct = default)
        {
            if (query is null) throw new ArgumentNullException(nameof(query));

            var qs = new List<string>
            {
                $"Page={query.Page}",
                $"PageSize={query.PageSize}"
            };

            // Texto de búsqueda principal
            if (!string.IsNullOrWhiteSpace(query.Search))
                qs.Add($"Search={Uri.EscapeDataString(query.Search)}");

            // Campo alternativo de búsqueda (si lo usas en algún sitio)
            if (!string.IsNullOrWhiteSpace(query.SearchTerm))
                qs.Add($"SearchTerm={Uri.EscapeDataString(query.SearchTerm)}");

            if (!string.IsNullOrWhiteSpace(query.PublicationStatusKey))
                qs.Add($"PublicationStatusKey={Uri.EscapeDataString(query.PublicationStatusKey)}");

            if (query.Year.HasValue)
                qs.Add($"Year={query.Year.Value}");

            if (query.VenueId.HasValue)
                qs.Add($"VenueId={query.VenueId.Value}");

            if (query.PublicationStatusId.HasValue)
                qs.Add($"PublicationStatusId={query.PublicationStatusId.Value}");

            if (query.FacultyId.HasValue)
                qs.Add($"FacultyId={query.FacultyId.Value}");

            if (query.IndexingSourceId.HasValue)
                qs.Add($"IndexingSourceId={query.IndexingSourceId.Value}");

            if (query.ResearchLineId.HasValue)
                qs.Add($"ResearchLineId={query.ResearchLineId.Value}");

            if (query.BroadFieldId.HasValue)
                qs.Add($"BroadFieldId={query.BroadFieldId.Value}");

            if (query.SpecificFieldId.HasValue)
                qs.Add($"SpecificFieldId={query.SpecificFieldId.Value}");

            if (query.DetailedFieldId.HasValue)
                qs.Add($"DetailedFieldId={query.DetailedFieldId.Value}");

            if (!string.IsNullOrWhiteSpace(query.SortBy))
                qs.Add($"SortBy={Uri.EscapeDataString(query.SortBy)}");

            qs.Add($"SortDesc={query.SortDesc}");

            var url = "api/Articles";
            if (qs.Count > 0)
            {
                url += "?" + string.Join("&", qs);
            }

            var result = await _api.GetAsync<PagedResult<ArticleListItemDto>>(url, ct);

            return result ?? new PagedResult<ArticleListItemDto>();
        }

        // =========================================================
        // LISTADO COMPLETO (para Kanban / métricas)
        // =========================================================
        public async Task<List<ArticleListItemDto>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            const int pageSize = 250;
            var page = 1;
            var items = new List<ArticleListItemDto>();

            while (true)
            {
                var result = await GetListAsync(new ArticleListQuery
                {
                    Page = page,
                    PageSize = pageSize,
                    Search = null,
                    SearchTerm = null
                }, cancellationToken);

                if (result.Items is null || result.Items.Count == 0)
                    break;

                items.AddRange(result.Items);

                if (items.Count >= result.TotalCount || result.Items.Count < pageSize)
                    break;

                page++;
            }

            return items;
        }
    }
}
