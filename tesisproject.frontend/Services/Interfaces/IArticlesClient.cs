using System.Threading;
using System.Threading.Tasks;
using tesisproject.shared.DTOs;
using tesisproject.shared.DTOs.Articles;

namespace tesisproject.frontend.Services.Interfaces
{
    public interface IArticlesClient
    {
        Task<int> CreateAsync(CreateArticleRequest request, CancellationToken ct = default);
        Task UpdateAsync(int id, UpdateArticleRequest request, CancellationToken ct = default);
        Task DeleteAsync(int id, CancellationToken ct = default);
        Task<ArticleDetailDto?> GetByIdAsync(int id, CancellationToken ct = default);
        Task<PagedResult<ArticleListItemDto>> GetListAsync(ArticleListQuery query, CancellationToken ct = default);
        Task<List<ArticleListItemDto>> GetAllAsync(CancellationToken cancellationToken = default);
    }
}
