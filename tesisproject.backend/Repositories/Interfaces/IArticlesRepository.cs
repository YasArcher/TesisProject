using System.Threading;
using System.Threading.Tasks;
using tesisproject.backend.Data.Entities;
using tesisproject.shared.DTOs;
using tesisproject.shared.DTOs.Articles;

namespace tesisproject.backend.Repositories.Interfaces
{
    public interface IArticlesRepository
    {
        Task<PagedResult<ArticleListItemDto>> GetListAsync(ArticleListQuery query, CancellationToken ct);
        Task<Article?> GetByIdWithDetailsAsync(int id, CancellationToken ct);
        Task AddAsync(Article article, CancellationToken ct);
        void Remove(Article article);
    }
}
