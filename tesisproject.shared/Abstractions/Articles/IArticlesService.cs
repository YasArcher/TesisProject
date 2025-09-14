using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using tesisproject.shared.Abstractions;          // Result / Result<T>
using tesisproject.shared.DTOs.Articles;        // ArticleDto, Create/Update

namespace tesisproject.shared.Abstractions.Articles
{
    public interface IArticlesService
    {
        Task<Result<IReadOnlyList<ArticleDto>>> GetAllAsync(CancellationToken ct = default);
        Task<Result<ArticleDto>> GetByIdAsync(int id, CancellationToken ct = default);
        Task<Result<int>> CreateAsync(CreateArticleRequest req, CancellationToken ct = default);
        Task<Result> UpdateAsync(UpdateArticleRequest req, CancellationToken ct = default);
        Task<Result> DeleteAsync(int id, CancellationToken ct = default);
    }
}
